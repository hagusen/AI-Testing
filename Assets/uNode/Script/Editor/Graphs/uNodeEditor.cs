//#define UseProfiler
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_5_5_OR_NEWER && UseProfiler
using UnityEngine.Profiling;
#endif

namespace MaxyGames.uNode.Editors {
	/// <summary>
	/// The main editor window for editing uNode.
	/// </summary>
	public class uNodeEditor : EditorWindow {
		#region Classes
		public static class EditorDataSerializer {
			[Serializable]
			class Data {
				public byte[] data;
				public DataReference[] references;
				public string type;

				public OdinSerializedData Load() {
					var data = new OdinSerializedData();
					data.data = this.data;
					data.serializedType = type;
					data.references = new List<UnityEngine.Object>();
					for(int i=0;i<references.Length;i++) {
						data.references.Add(references[i].GetObject());
					}
					return data;
				}

				public static Data Create(OdinSerializedData serializedData) {
					var data = new Data();
					data.data = serializedData.data;
					data.type = serializedData.serializedType;
					data.references = new DataReference[serializedData.references.Count];
					for(int i=0;i< data.references.Length;i++) {
						data.references[i] = DataReference.Create(serializedData.references[i]);
					}
					return data;
				}
			}

			[Serializable]
			class DataReference {
				public string path;
				public int uid;

				public UnityEngine.Object GetObject() {
					var obj = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
					if(uNodeUtility.GetObjectID(obj) == uid) {
						return obj;
					} else {
						var objs = AssetDatabase.LoadAllAssetsAtPath(path);
						for(int i=0;i< objs.Length;i++) {
							if(uNodeUtility.GetObjectID(objs[i]) == uid) {
								return objs[i];
							}
						}
						//if(obj is GameObject gameObject) {
						//	var comps = gameObject.GetComponentsInChildren<Component>(true);
						//	for(int i=0;i<comps.Length;i++) {
						//		if(uNodeUtility.GetObjectID(comps[i]) == uid) {
						//			return comps[i];
						//		}
						//	}
						//}
					}
					return null;
				}

				public static DataReference Create(UnityEngine.Object obj) {
					if(obj == null)
						return null;
					var path = AssetDatabase.GetAssetPath(obj);
					if(!string.IsNullOrEmpty(path)) {
						DataReference data = new DataReference();
						data.path = path;
						data.uid = uNodeUtility.GetObjectID(obj);
						return data;
					}
					return null;
				} 
			}

			public static void Save<T>(T value, string fileName) {
				Directory.CreateDirectory("uNode2Data");
				char separator = Path.DirectorySeparatorChar;
				string path = "uNode2Data" + separator + fileName + ".json";
				File.WriteAllText(path, JsonUtility.ToJson(Data.Create(SerializerUtility.SerializeValue(value))));
			}

			public static T Load<T>(string fieldName) {
				char separator = Path.DirectorySeparatorChar;
				string path = "uNode2Data" + separator + fieldName + ".json";
				if(File.Exists(path)) {
					var data = JsonUtility.FromJson<Data>(File.ReadAllText(path));
					if(data != null) {
						return SerializerUtility.Deserialize<T>(data.Load());
					}
				}
				return default;
			}
		}

		[System.Serializable]
		public class uNodeEditorData {
			public List<EditorScriptInfo> scriptInformations = new List<EditorScriptInfo>();

			/// <summary>
			/// Are the left panel is visible?
			/// </summary>
			public bool leftVisibility = true;
			/// <summary>
			/// Are the right panel is visible?
			/// </summary>
			public bool rightVisibility = true;
			/// <summary>
			/// The heigh of variable editor.
			/// </summary>
			public float variableEditorHeight = 150f;

			#region Panel
			[SerializeField]
			private float _rightPanelWidth = 300;
			[SerializeField]
			private float _leftPanelWidth = 250;
			public List<string> lastOpenedFile;

			/// <summary>
			/// The width of right panel.
			/// </summary>
			public float rightPanelWidth {
				get {
					if(!rightVisibility)
						return 0;
					return _rightPanelWidth;
				}
				set {
					_rightPanelWidth = value;
				}
			}

			/// <summary>
			/// The width of left panel.
			/// </summary>
			public float leftPanelWidth {
				get {
					if(!leftVisibility)
						return 0;
					return _leftPanelWidth;
				}
				set {
					_leftPanelWidth = value;
				}
			}
			#endregion

			#region Recent
			[Serializable]
			public class RecentItem {
				[SerializeField]
				private MemberData memberData;

				private MemberInfo _info;
				public MemberInfo info {
					get {
						if(_info == null && memberData != null) {
							switch(memberData.targetType) {
								case MemberData.TargetType.Type:
								case MemberData.TargetType.uNodeType:
									_info = memberData.startType;
									break;
								case MemberData.TargetType.Field:
								case MemberData.TargetType.Constructor:
								case MemberData.TargetType.Event:
								case MemberData.TargetType.Method:
								case MemberData.TargetType.Property:
									var members = memberData.GetMembers();
									if(members != null) {
										_info = members[members.Length - 1];
									}
									break;
							}
						}
						return _info;
					}
					set {
						_info = value;
						memberData = MemberData.CreateFromMember(_info);
					}
				}
				public bool isStatic {
					get {
						if(info == null)
							return false;
						return ReflectionUtils.GetMemberIsStatic(info);
					}
				}
			}

			/// <summary>
			/// The recent items data.
			/// </summary>
			public List<RecentItem> recentItems = new List<RecentItem>();

			public void AddRecentItem(RecentItem recentItem) {
				while(recentItems.Count >= 50) {
					recentItems.RemoveAt(recentItems.Count - 1);
				}
				recentItems.RemoveAll(item => item.info == recentItem.info);
				recentItems.Insert(0, recentItem);
				SaveOptions();
			}
			#endregion

			#region Favorites
			public List<RecentItem> favoriteItems;

			public void AddFavorite(MemberInfo member) {
				if(favoriteItems == null)
					favoriteItems = new List<RecentItem>();
				if(!HasFavorite(member)) {
					favoriteItems.Add(new RecentItem() {
						info = member
					});
					SaveOptions();
				}
			}

			public void RemoveFavorite(MemberInfo member) {
				if(favoriteItems == null)
					return;
				if(HasFavorite(member)) {
					favoriteItems.Remove(favoriteItems.First(item => item != null && item.info == member));
					SaveOptions();
				}
			}

			public bool HasFavorite(MemberInfo member) {
				if(favoriteItems == null)
					return false;
				return favoriteItems.Any(item => item != null && item.info == member);
			}

			[SerializeField]
			HashSet<string> _favoriteNamespaces;
			public HashSet<string> favoriteNamespaces {
				get {
					if(_favoriteNamespaces == null) {
						_favoriteNamespaces = new HashSet<string>() {
							"System",
							"System.Collections",
							"UnityEngine.AI",
							"UnityEngine.Events",
							"UnityEngine.EventSystems",
							"UnityEngine.SceneManagement",
							"UnityEngine.UI",
							"UnityEngine.UIElements",
						};
					}
					return _favoriteNamespaces;
				}
			}
			#endregion

			#region Graph Infos
			public void RegisterGraphInfos(IEnumerable<ScriptInformation> informations, UnityEngine.Object owner, string scriptPath) {
				if(informations != null) {
					EditorScriptInfo scriptInfo = new EditorScriptInfo() {
						guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(owner)),
						path = scriptPath,
					};
					foreach(var info in informations) {
						if(int.TryParse(info.id, out var id)) {
							var obj = EditorUtility.InstanceIDToObject(id);
							if(obj != null) {
								info.ghostID = info.id;
								info.id = uNodeUtility.GetObjectID(obj).ToString();
							}
						}
					}
					scriptInfo.informations = informations.ToArray();
					var prevInfo = scriptInformations.FirstOrDefault(g => g.guid == scriptInfo.guid);
					if(prevInfo != null) {
						scriptInformations.Remove(prevInfo);
					}
					scriptInformations.Add(scriptInfo);
					uNodeThreadUtility.ExecuteOnce(uNodeEditor.SaveOptions, "unode_save_informations");
				}
			}

			public bool UnregisterGraphInfo(UnityEngine.Object owner) {
				var guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(owner));
				var prevInfo = scriptInformations.FirstOrDefault(g => g.guid == guid);
				if(prevInfo != null) {
					return scriptInformations.Remove(prevInfo);
				}
				uNodeThreadUtility.ExecuteOnce(uNodeEditor.SaveOptions, "unode_save_informations");
				return false;
			}
			#endregion
		}

		[Serializable]
		public class EditorScriptInfo {
			public string guid;
			public string path;
			public ScriptInformation[] informations;
		}

		[Serializable]
		public class GraphData {
			[SerializeField]
			private UnityEngine.Object _graph;
			[SerializeField]
			private string graphPath;
			
			public GameObject graph {
				get {
					if(_graph as GameObject == null && !string.IsNullOrEmpty(graphPath)) {
						_graph = AssetDatabase.LoadAssetAtPath(graphPath, typeof(GameObject));
					}
					return _graph as GameObject;
				}
				set {
					_graph = value;
					if(value is GameObject) {
						graphPath = AssetDatabase.GetAssetPath(value);
					} else {
						graphPath = null;
					}
				}
			}
			
			public GameObject graphPrefab {
				get {
					return graph as GameObject;
				}
			}
			
			public List<GraphEditorData> data = new List<GraphEditorData>();

			[SerializeField]
			private int selectedIndex;
			/// <summary>
			/// The current selected graph editor data
			/// </summary>
			/// <value></value>
			public GraphEditorData selectedData {
				get {
					if(data.Count == 0 || selectedIndex >= data.Count || selectedIndex < 0) {
						data.Add(new GraphEditorData());
						selectedIndex = data.Count - 1;
						return data[selectedIndex];
					}
					return data[selectedIndex];
				}
				set {
					if(value == null) {
						selectedIndex = -1;
						return;
					}
					for(int i = 0; i < data.Count; i++) {
						if(data[i] == value) {
							selectedIndex = i;
							return;
						}
					}
					data.Add(value);
					selectedIndex = data.Count - 1;
				}
			}

			public string displayName {
				get {
					if(graph != null) {
						return graph.name;
					}
					try {
						if(owner == null || !owner || owner.hideFlags == HideFlags.HideAndDontSave) {
							owner = null;
							return "";
						}
						return owner.name;
					}
					catch {
						//To fix sometime error at editor startup.
						owner = null;
						return "";
					}
				}
			}

			[SerializeField]
			private GameObject _owner;
			/// <summary>
			/// This is the persistence graph
			/// </summary>
			/// <value></value>
			public GameObject owner {
				get {
					if(_owner == null || !_owner) {
						if(graph != null && graph) {
							_owner = LoadTempGraphObject(graph);
							if(_owner != null) {
								var root = _owner.GetComponent<uNodeRoot>();
								if(root != null) {
									selectedData.SetOwner(root);
								} else {
									var data = _owner.GetComponent<uNodeData>();
									if(data != null) {
										selectedData.SetOwner(data);
									} else {
										selectedData.SetOwner(_owner);
									}
								}
							} else {
								graph = null;
							}
						} else {
							foreach(var d in data) {
								if(d.owner != null) {
									_owner = d.owner;
								}
							}
						}
					}
					return _owner;
				}
				set {
					_owner = value;
				}
			}
		}
		
		[Serializable]
		public class ValueInspector {
			public UnityEngine.Object owner;
			public object value;

			public ValueInspector() {

			}

			public ValueInspector(object value, UnityEngine.Object owner) {
				this.value = value;
				this.owner = owner;
			}
		}
		
		public class EditorInteraction {
			public string name;
			public object userObject;
			public object userObject2;
			//public object userObject3;
			//public object userObject4;
			public InteractionKind interactionKind = InteractionKind.Drag;
			public Action onClick;
			public Action onDrag;

			public bool hasDragged;

			public enum InteractionKind {
				Drag,
				Click,
				ClickOrDrag
			}

			public EditorInteraction(string name) {
				this.name = name;
			}

			public EditorInteraction(string name, object userObject) {
				this.name = name;
				this.userObject = userObject;
			}

			public static implicit operator EditorInteraction(string name) {
				return new EditorInteraction(name);
			}

			public static bool operator ==(EditorInteraction x, string y) {
				if(ReferenceEquals(x, null)) {
					return y == null;
				} else if(y == null) {
					return ReferenceEquals(x, null);
				}
				return x.name == y;
			}

			public static bool operator !=(EditorInteraction x, string y) {
				return !(x == y);
			}

			public override bool Equals(object obj) {
				var interaction = obj as EditorInteraction;
				return !ReferenceEquals(interaction, null) && name == interaction.name;
			}

			public override int GetHashCode() {
				return 363513814 + EqualityComparer<string>.Default.GetHashCode(name);
			}
		}
		#endregion

		#region Const
		public const string Key_Recent_Item = "unode_show_recent_item";
		private const string MESSAGE_PATCH_WARNING = "Patching should be done in 'Compatibility' generation mode or there will be visible/invisible errors, please use 'Compatibility' mode when trying to make live changes to compiled code.";
		#endregion

		#region Variable & Function
		/// <summary>
		/// The uNode editor instance
		/// </summary>
		public static uNodeEditor window;
		private static HashSet<NodeComponent> dimmedNode;
		#region Private Fields
		private Event currentEvent;

		private int limitMultiEdit = 10, dimRefreshTime, errorRefreshTime;
		private float oldCanvasWidth;
		[NonSerialized]
		private Rect canvasArea = new Rect(0, 57, 35000, 35000);
		private Vector2 scrollView = new Vector2(30000, 30000),
			scrollPos2,
			leftPanelScroll,
			clickPos;
		private Rect inspectorRect;
		#endregion

		#region Properties
		/// <summary>
		/// The editor data
		/// </summary>
		public GraphEditorData editorData {
			get {
				return selectedGraph.selectedData;
			}
		}

		/// <summary>
		/// The graph editor.
		/// </summary>
		public NodeGraph graphEditor {
			get {
				return uNodePreference.nodeGraph;
			}
		}

		public static Dictionary<UnityEngine.Object, List<uNodeUtility.ErrorMessage>> editorErrors {
			get {
				return uNodeUtility.editorErrorMap;
			}
		}

		public bool isZoom {
			get {
				return graphEditor.zoomScale != 1;
			}
		}
		
		/// <summary>
		/// The debug object.
		/// </summary>
		public object debugObject {
			get {
				return editorData.debugTarget;
			}
			set {
				editorData.debugTarget = value;
			}
		}

		static uNodePreference.PreferenceData _preferenceData;
		/// <summary>
		/// The preference data.
		/// </summary>
		public static uNodePreference.PreferenceData preferenceData {
			get {
				if(_preferenceData != uNodePreference.GetPreference()) {
					_preferenceData = uNodePreference.GetPreference();
				}
				return _preferenceData;
			}
		}
		/// <summary>
		/// Are the main selection is locked?
		/// </summary>
		public static bool isLocked {
			get {
				return preferenceData.isLocked;
			}
			set {
				if(preferenceData.isLocked != value) {
					preferenceData.isLocked = value;
					uNodePreference.SavePreference();
				}
			}
		}
		/// <summary>
		/// Are the node is dimmed?
		/// </summary>
		public static bool isDim {
			get {
				return preferenceData.isDim;
			}
			set {
				if(preferenceData.isDim != value) {
					preferenceData.isDim = value;
					uNodePreference.SavePreference();
				}
			}
		}

		
		private EditorInteraction interaction { get; set; }

		private bool haveInteraction {
			get {
				return interaction != null;
			}
		}

		/// <summary>
		/// The height of the status bar.
		/// </summary>
		public float statusBarHeight {
			get {
				if(preferenceData.showStatusBar) {
					return 18;
				}
				return 0;
			}
		}
		#endregion

		#region EditorData
		[SerializeField]
		public GraphData mainGraph = new GraphData();
		[SerializeField]
		public List<GraphData> graphs = new List<GraphData>();

		public GraphData selectedGraph {
			get {
				if(_selectedDataIndex > 0 && graphs.Count >= _selectedDataIndex) {
					return graphs[_selectedDataIndex - 1];
				} else {
					_selectedDataIndex = 0;
				}
				return mainGraph;
			}
			set {
				if(value != null && value != mainGraph && graphs.Contains(value)) {
					_selectedDataIndex = graphs.IndexOf(value) + 1;
				} else {
					_selectedDataIndex = 0;
				}
			}
		}
		#endregion

		/// <summary>
		/// An event to be called on GUIChanged.
		/// </summary>
		public static event Action onChanged;
		/// <summary>
		/// An event to be called on Selection is changed.
		/// </summary>
		public static event Action<GraphEditorData> onSelectionChanged;

		static uNodeEditor() {
			Undo.undoRedoPerformed -= UndoRedoCallback;
			Undo.undoRedoPerformed += UndoRedoCallback;
		}

		/// <summary>
		/// Show the uNodeEditor.
		/// </summary>
		[MenuItem("Tools/uNode/uNode Editor", false, 0)]
		public static void ShowWindow() {
			window = (uNodeEditor)GetWindow(typeof(uNodeEditor), false);
			window.minSize = new Vector2(300, 250);
			window.autoRepaintOnSceneChange = true;
			window.wantsMouseMove = true;
			window.titleContent = new GUIContent("uNode Editor"/*, Resources.Load<Texture2D>("uNODE_Logo")*/);
			window.Show();
		}

		#endregion

		#region UnityEvent
		private GameObject oldTarget = null;
		void Update() {
			if(!EditorApplication.isPaused) {
				uNodeUtility.debugLinesTimer = Mathf.Repeat(uNodeUtility.debugLinesTimer += 0.03f, 1f);
			}
			if(Selection.activeGameObject != null && (oldTarget != Selection.activeGameObject)) {
				OnSelectionChange();
				oldTarget = Selection.activeGameObject;
			}
			if(preferenceData.enableErrorCheck) {
				int nowSecond = System.DateTime.Now.Second;
				//if(nowSecond % 2 != 0 && nowSecond != errorRefreshTime)
				if(nowSecond != errorRefreshTime) {
					CheckErrors();
					errorRefreshTime = nowSecond;
					Repaint();
				}
			}
			if(selectedGraph != null && selectedGraph.graph != null) {
				if(selectedGraph.owner != null) {
					//
				}
			}
			InitGraph();
		}

		private bool? prevShowGraph;
		void OnGUI() {
			if(Event.current.type != EventType.Repaint && Event.current.type != EventType.Repaint) {
				prevShowGraph = editorData.graphData || editorData.owner;
			}
			if(prevShowGraph == null) return;
			bool showGraph = prevShowGraph.Value;
			window = this;
			graphEditor.window = this;
			GUI.color = Color.white;
			#region Validation
			try {
				if(!showGraph && mainGraph == selectedGraph) {
					if (Selection.activeGameObject == null) {
						GUIContent info = new GUIContent("To start creating a uNode graph, select a GameObject");
						Vector2 size = EditorStyles.label.CalcSize(info);
						GUI.Label(new Rect((window.position.width / 2) - (size.x / 2), (window.position.height / 2) - (size.y / 2), size.x, size.y), info);
					} else {
						GUIContent info = new GUIContent(string.Format("To begin a new uNode graph with \'{0}\', create a uNode component", Selection.activeGameObject.name));
						Vector2 size = EditorStyles.label.CalcSize(info);
						Rect labelRect = new Rect((window.position.width / 2) - (size.x / 2), (window.position.height / 2) - (size.y / 2), size.x, size.y);
						GUI.Label(labelRect, info);
						if(GUI.Button(new Rect((window.position.width / 2) - 35, labelRect.y + labelRect.height, 70, 20), new GUIContent("Create"))) {
							ShowAddNewRootMenu(Selection.activeGameObject, (root) => {
								ChangeTarget(root);
								Refresh();
							});
							Event.current.Use();
						}
					}
				}
			}
			catch {
				if(selectedGraph == mainGraph) {
					//selectedData.data = new EditorData();
				} else {
					graphs.Remove(selectedGraph);
					ChangeEditorTarget(null);
				}
				ChangeEditorTarget(mainGraph);
			}
			#endregion

			DrawToolbar();

			#region Init
			bool displayInspectorPanel = SavedData.rightVisibility;
			canvasArea.position = graphEditor.canvasPosition;
			Rect areaZoom = canvasArea;
			areaZoom.x += SavedData.leftPanelWidth;
			Rect canvasRect = new Rect(SavedData.leftPanelWidth, areaZoom.y, position.width - SavedData.leftPanelWidth, position.height - areaZoom.y - statusBarHeight);
			inspectorRect = Rect.zero;
			if(displayInspectorPanel) {
				inspectorRect = new Rect((canvasRect.x + canvasRect.width) - SavedData.rightPanelWidth, canvasRect.y, SavedData.rightPanelWidth, canvasRect.height);
				canvasRect.width -= SavedData.rightPanelWidth;
			}
			#endregion

			#region Tabbar
			graphEditor.DrawTabbar(new Vector2(showGraph ? SavedData.leftPanelWidth : 0, 20));
			#endregion

			#region StatusBar
			if(showGraph && statusBarHeight > 0) {//Status bar
				DrawStatusBar(new Rect(
					showGraph ? areaZoom.x : 0,
					position.height - statusBarHeight,
					(showGraph ? 0 : areaZoom.x) + canvasRect.width + inspectorRect.width,
					statusBarHeight));
			}
			#endregion

			#region DrawCanvas
			if(showGraph) {
				if(isDim && System.DateTime.Now.Second != dimRefreshTime) {
					RefreshDimmedNode();
					dimRefreshTime = System.DateTime.Now.Second;
				}
				Rect backgroundRect = new Rect(areaZoom.x, areaZoom.y - 2, position.width - SavedData.leftPanelWidth - (displayInspectorPanel ? SavedData.rightPanelWidth : 0), position.height - areaZoom.y + 2 - statusBarHeight);
				if(editorData.graph != null) {
					TopEventHandler();
				}
				graphEditor.DrawCanvas(this, new Editors.GraphData() {
					editorData = editorData,
					canvasArea = areaZoom,
					canvasRect = canvasRect,
					backgroundRect = backgroundRect,
					dimmedNodes = dimmedNode,
					isDim = isDim,
					isDisableEdit = IsDisableEdit(),
				});
			} else {
				graphEditor.OnNoTarget();
			}
			#endregion

			if(haveInteraction &&
					(interaction.interactionKind == EditorInteraction.InteractionKind.ClickOrDrag ||
					interaction.interactionKind == EditorInteraction.InteractionKind.Click) &&
					(Event.current.type == EventType.MouseUp ||
					Event.current.type == EventType.Ignore)) {

				if(interaction.onClick != null) {
					interaction.onClick();
				}
				interaction = null;
			} else if(haveInteraction &&
				(interaction.interactionKind == EditorInteraction.InteractionKind.ClickOrDrag ||
				interaction.interactionKind == EditorInteraction.InteractionKind.Drag) &&
				Event.current.type == EventType.MouseDrag) {
				if(interaction.onDrag != null) {
					interaction.onDrag();
				}
				interaction.hasDragged = true;
			}
			if(haveInteraction && (Event.current.type == EventType.MouseUp || Event.current.type == EventType.Ignore)) {
				interaction = null;
			}
			if(showGraph && SavedData.leftVisibility) {
				DrawLeftPanel(canvasRect);
			} else {
				graphEditor.HideGraphPanel();
			}
			if(showGraph && displayInspectorPanel) {
				DrawRightPanel(inspectorRect);
			}
			if(GUI.changed) {
				GUIChanged();
			}
		}

		public static void GUIChanged(object obj) {
			if(window != null && window.graphEditor != null) {
				window.graphEditor.GUIChanged(obj);
			}
			GUIChanged();
		}

		public static void GUIChanged() {
			if(window != null) {
				try {
					EditorUtility.SetDirty(window);
					if(window.selectedGraph != null && window.selectedGraph.owner != null)
						EditorUtility.SetDirty(window.selectedGraph.owner);
				}
				catch {
					if(window.selectedGraph != null) {
						window.selectedGraph.owner = null;
					}
				}
			}
			if(onChanged != null) {
				onChanged();
			}
		}

		[NonSerialized]
		NodeGraph initedGraph;
		void InitGraph() {
			if(initedGraph != graphEditor) {
				graphEditor.window = this;
				graphEditor.OnEnable();
				initedGraph = graphEditor;
			}
		}

		void OnEnable() {
			uNodeGUIUtility.onGUIChanged -= GUIChanged;
			uNodeGUIUtility.onGUIChanged += GUIChanged;
			EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;

			LoadEditorData();
			InitGraph();
			if(selectedGraph != null && selectedGraph.graph != null) {
				if(selectedGraph.owner != null) {
					//
				}
			}
			Refresh();
			if(!hasLoad) {
				LoadOptions();
				hasLoad = true;
			}
		}

		bool hasLoad = false;
		void OnDisable() {
			uNodeGUIUtility.onGUIChanged -= GUIChanged;
			EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
			SaveEditorData();
			if(!hasLoad) {
				LoadOptions();
				hasLoad = true;
			}
			SaveOptions();
			graphEditor.OnDisable();
			initedGraph = null;
		}

		void OnSelectionChange() {
			if(mainGraph.selectedData.graph != null && (isLocked || mainGraph.selectedData.graph.gameObject == Selection.activeGameObject))
				return;
			if(Selection.activeGameObject != null) {
				var selected = Selection.activeObject;
				ChangeMainSelection(Selection.activeGameObject);
				Selection.activeObject = selected;
			}
			if (selectedGraph == mainGraph) {
				GUIChanged();
			}
		}
		#endregion

		#region GUI
		#region Panels
		private void DrawLeftPanel(Rect ScrollRect) {
			Rect resizeLRect = new Rect(SavedData.leftPanelWidth - 3, ScrollRect.y, 5, ScrollRect.height);
			EditorGUIUtility.AddCursorRect(resizeLRect, MouseCursor.ResizeHorizontal);
			if(currentEvent.button == 0 && currentEvent.type == EventType.MouseDown && resizeLRect.Contains(currentEvent.mousePosition)) {
				if(!haveInteraction) {
					interaction = new EditorInteraction("ResizeLeftCanvas");
					clickPos = currentEvent.mousePosition;
					oldCanvasWidth = SavedData.leftPanelWidth;
					currentEvent.Use();
				}
			}
			if(interaction == "ResizeLeftCanvas") {
				SavedData.leftPanelWidth = oldCanvasWidth - (clickPos - currentEvent.mousePosition).x;
				if(SavedData.leftPanelWidth > 400) {
					SavedData.leftPanelWidth = 400;
				}
				if(SavedData.leftPanelWidth < 150) {
					SavedData.leftPanelWidth = 150;
				}
				SaveOptions();
				Repaint();
			}
			graphEditor.DrawGraphPanel(new Rect(0, 18, resizeLRect.x, 0));
		}

		private void DrawRightPanel(Rect areaRect) {
			Rect resizeRRect = new Rect(areaRect.x - 2, areaRect.y, 6, areaRect.height);
			EditorGUIUtility.AddCursorRect(resizeRRect, MouseCursor.ResizeHorizontal);
			if((currentEvent.button == 0 && currentEvent.type == EventType.MouseDown) && resizeRRect.Contains(currentEvent.mousePosition)) {
				if(!haveInteraction) {
					interaction = "ResizeRightCanvas";
					clickPos = currentEvent.mousePosition;
					oldCanvasWidth = SavedData.rightPanelWidth;
					//isDragging = true;
				}
			}
			GUILayout.BeginArea(areaRect, "", "Box");
			scrollPos2 = EditorGUILayout.BeginScrollView(scrollPos2);
#if UseProfiler
			Profiler.BeginSample("Draw Inspector");
#endif
			CustomInspector.ShowInspector(editorData, limitMultiEdit);
#if UseProfiler
			Profiler.EndSample();
#endif
			EditorGUILayout.EndScrollView();
			GUILayout.EndArea();
			if(interaction == "ResizeRightCanvas") {
				SavedData.rightPanelWidth = oldCanvasWidth + (clickPos - currentEvent.mousePosition).x;
				if(SavedData.rightPanelWidth > 400) {
					SavedData.rightPanelWidth = 400;
				}
				if(SavedData.rightPanelWidth < 250) {
					SavedData.rightPanelWidth = 250;
				}
				SaveOptions();
				Repaint();
			}
		}
		#endregion

		public void ChangeEditorSelection(object value) {
			editorData.selected = value;
			if(value != editorData.selectedNodes)
				editorData.selectedNodes.Clear();
			if(value is NodeComponent) {
				editorData.selectedNodes.Add(value as NodeComponent);
				editorData.selected = editorData.selectedNodes;
			}
			EditorSelectionChanged();
		}

		CustomInspector inspectorWrapper;
		internal void EditorSelectionChanged() {
			if(onSelectionChanged != null) {
				onSelectionChanged(editorData);
			}
			if(editorData.selected == null || !uNodePreference.GetPreference().inspectorIntegration)
				return;
			inspectorWrapper = CreateInstance<CustomInspector>();
			inspectorWrapper.editorData = editorData;
			//We changed the selection using instanceIDs to prevent undo record.
			Selection.instanceIDs = new int[] { inspectorWrapper.GetInstanceID() };
		}

		public void ResetView() {
			UpdatePosition();
		}

		//#region Progress Bar
		//class ProgressBar {
		//	public string title;
		//	public string info;
		//	public float progress;

		//	public bool display => container != null;

		//	public IMGUIContainer container;
		//}
		//private ProgressBar progressBar = new ProgressBar();

		//public void DisplayProgressBar(string title, string info, float progress) {
		//	progressBar.title = title;
		//	progressBar.info = info;
		//	progressBar.progress = progress;
		//	if(!progressBar.display) {
		//		progressBar.container = new IMGUIContainer(() => {
		//			GUI.Box(new Rect(0, 0, position.width, position.height), GUIContent.none);
		//			GUILayout.FlexibleSpace();
		//			EditorGUI.ProgressBar(uNodeGUIUtility.GetRect(), progressBar.progress, progressBar.info);
		//			GUILayout.FlexibleSpace();
		//		});
		//		rootVisualElement.Add(progressBar.container);
		//		progressBar.container.StretchToParentSize();
		//	} else {
		//		progressBar.container?.BringToFront();
		//	}
		//}

		//public void ClearProgressBar() {
		//	if(progressBar.container != null) {
		//		progressBar.container.RemoveFromHierarchy();
		//		progressBar.container = null;
		//	}
		//}
		//#endregion

		public void DrawToolbar() {
			currentEvent = Event.current;
			GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(100));
			{
				Rect leftVisibility = GUILayoutUtility.GetRect(13.4f, 20);
				GUI.DrawTexture(leftVisibility, uNodeGUIUtility.Styles.GetVisiblilityTexture(SavedData.leftVisibility), ScaleMode.ScaleToFit);
				if(currentEvent.button == 0 && currentEvent.type == EventType.MouseUp && leftVisibility.Contains(currentEvent.mousePosition)) {
					SavedData.leftVisibility = !SavedData.leftVisibility;
					Repaint();
					currentEvent.Use();
				}
			}
			if(editorData.graph != null) {//Debug
				string names = "None";
				if(!uNodeUtility.useDebug) {
					names = "Disable";
				} else if(debugObject != null) {
					if(debugObject is UnityEngine.Object) {
						if(debugObject as UnityEngine.Object == editorData.graph) {
							if(editorData.graph is uNodeRuntime) {
								names = "self";
							}
						} else if(debugObject as MonoBehaviour && (debugObject as MonoBehaviour).gameObject) {
							names = (debugObject as MonoBehaviour).gameObject.name;
						} else {
							names = debugObject.GetType().Name;
						}
					} else if(editorData.debugAnyScript) {
						names = "AnyScriptInstance";
					} else {
						names = debugObject.GetType().Name;
					}
				} else if(editorData.debugAnyScript) {
					names = "AnyScriptInstance";
				}
				if(editorData.graph is uNodeRuntime && (editorData.graph as uNodeRuntime).originalGraph != null) {
					names = "@" + editorData.graph.DisplayName;
				}
				GUIContent content = new GUIContent("Debug : " + names, "");
				Vector2 size = ((GUIStyle)"Button").CalcSize(content);
				if(GUILayout.Button(content, EditorStyles.toolbarButton, GUILayout.Width(size.x), GUILayout.Height(15))) {
					GenericMenu menu = new GenericMenu();
					if(editorData.graph is uNodeRuntime) {
						menu.AddItem(new GUIContent("Self"), false, delegate () {
							debugObject = null;
							uNodeUtility.useDebug = true;
							if(editorData.graph is uNodeRuntime && (editorData.graph as uNodeRuntime).originalGraph != null) {
								ChangeTarget((editorData.graph as uNodeRuntime).originalGraph);
							}
						});
					}
					menu.AddItem(new GUIContent("None"), false, delegate () {
						debugObject = null;
						editorData.debugSelf = false;
						editorData.debugAnyScript = false;
						uNodeUtility.useDebug = true;
					});
					menu.AddItem(new GUIContent("Disable"), false, delegate () {
						uNodeUtility.useDebug = false;
					});
					menu.AddSeparator("");
					menu.AddDisabledItem(new GUIContent("Script Debugger"), false);
					if(editorData.graph is IIndependentGraph graph) {
						menu.AddItem(new GUIContent("Debug Mode " + (editorData.graph.graphData.debug ? " (Enabled) " : " (Disabled) ")), editorData.graph.graphData.debug, delegate () {
							editorData.graph.graphData.debug = !editorData.graph.graphData.debug;
						});
						if(editorData.graph.graphData.debug) {
							menu.AddItem(new GUIContent("Debug Value" + (editorData.graph.graphData.debugValueNode ? " (Enabled) " : " (Disabled) ")), editorData.graph.graphData.debugValueNode, delegate () {
								editorData.graph.graphData.debugValueNode = !editorData.graph.graphData.debugValueNode;
							});
						}
					} else {
						menu.AddItem(new GUIContent("Debug Mode " + (generatorSettings.debug ? " (Enabled) " : " (Disabled) ")), generatorSettings.debug, delegate () {
							generatorSettings.debug = !generatorSettings.debug;
						});
						if(generatorSettings.debug) {
							menu.AddItem(new GUIContent("Debug Value" + (generatorSettings.debugValueNode ? " (Enabled) " : " (Disabled) ")), generatorSettings.debugValueNode, delegate () {
								generatorSettings.debugValueNode = !generatorSettings.debugValueNode;
							});
						}
					}
					menu.AddSeparator("");
					menu.AddItem(new GUIContent("AnyScriptInstance"), false, delegate () {
						debugObject = null;
						editorData.debugAnyScript = true;
						uNodeUtility.useDebug = true;
					});
					Type type = uNodeEditorUtility.GetFullScriptName(editorData.graph).ToType(false);
					if(type != null && type.IsSubclassOf(typeof(MonoBehaviour))) {
						UnityEngine.Object[] obj = FindObjectsOfType(type);
						if(obj.Length == 0) {
							List<UnityEngine.Object> objs = new List<UnityEngine.Object>();
							var assemblies = ReflectionUtils.GetRuntimeAssembly();
							for(int i=assemblies.Length-1;i>0;i--) {
								var t = assemblies[i].GetType(type.FullName, false);
								if(t != null) {
									objs.AddRange(FindObjectsOfType(t));
								}
							}
							obj = objs.ToArray();
						}
						for(int i = 0; i < obj.Length; i++) {
							if(obj[i] is MonoBehaviour) {
								menu.AddItem(new GUIContent(i + "-" + type.PrettyName()), false, delegate (object reference) {
									uNodeUtility.useDebug = true;
									UnityEngine.Object o = reference as UnityEngine.Object;
									EditorGUIUtility.PingObject(o);
									debugObject = o;
								}, obj[i]);
							}
							if(i > 250) {
								break;
							}
						}
					}
					if(Application.isPlaying && editorData.graph != null) {
						if(uNodeUtility.debugData.ContainsKey(uNodeUtility.GetObjectID(editorData.graph))) {
							Dictionary<object, uNodeUtility.DebugData> debugMap = null;
							debugMap = uNodeUtility.debugData[uNodeUtility.GetObjectID(editorData.graph)];
							if(debugMap.Count > 0) {
								int count = 0;
								foreach(KeyValuePair<object, uNodeUtility.DebugData> pair in debugMap) {
									if(count > 250)
										break;
									if(pair.Key != null && pair.Key as uNodeRoot != editorData.graph.GetPersistenceObject()) {
										menu.AddItem(new GUIContent("Script/" + count + "-" + pair.Key.GetType().PrettyName()), false, delegate (object reference) {
											KeyValuePair<object, uNodeUtility.DebugData> objPair = (KeyValuePair<object, uNodeUtility.DebugData>)reference;
											UnityEngine.Object o = objPair.Key as UnityEngine.Object;
											if(o != null) {
												EditorGUIUtility.PingObject(o);
											}
											debugObject = objPair.Value;
											uNodeUtility.useDebug = true;
										}, pair);
										count++;
									}
								}
							}
						}
						var objs = GameObject.FindObjectsOfType<uNodeRuntime>();
						int counts = 0;
						foreach(var obj in objs) {
							if(counts > 250)
								break;
							if(obj.originalGraph != null && obj.originalGraph.gameObject == selectedGraph.graph) {
								menu.AddItem(new GUIContent("Runtime/" + counts + "-" + obj.gameObject.name), false, (reference) => {
									uNodeRoot o = reference as uNodeRoot;
									if(o != null) {
										EditorGUIUtility.PingObject(o);
									}
									debugObject = null;
									uNodeUtility.useDebug = true;
									ChangeMainTarget(o, null, true);
								}, obj);
								counts++;
							}
						}
					}
					menu.ShowAsContext();
				}
			}
			if(editorData.isGraphOpen && graphEditor != null) {
				if(GUILayout.Button(new GUIContent("Frame Graph", "Frame the graph\nHotkey: F"), EditorStyles.toolbarButton)) {
					graphEditor.FrameGraph();
				}
			}
			if(editorData.graph != null || editorData.graphData != null) {
				//Handle graph asset.
				if(selectedGraph != null && (selectedGraph.graph != null || Application.isPlaying && editorData.graph is uNodeRuntime runtime && runtime.originalGraph != null)) {
					if(GUILayout.Button(new GUIContent("Save"), EditorStyles.toolbarButton, GUILayout.Height(15))) {
						SaveCurrentGraph();
						if(Application.isPlaying) {
							EditorPrefs.SetBool("unode_graph_saved_in_playmode", true);
						}
					}
				}
			}
			GUILayout.FlexibleSpace();
			if(editorData.graph != null || editorData.graphData != null) {
				if(editorData.graph == null || editorData.graph is IClass) {
					if (editorData.graphSystem == null || editorData.graphSystem.allowPreviewScript) {
						if (GUILayout.Button(new GUIContent("Preview", "Preview C# Script\nHotkey: F9"), EditorStyles.toolbarButton, GUILayout.Width(55), GUILayout.Height(15))) {
							if (selectedGraph != null && selectedGraph.graph != null) {
								AutoSaveCurrentGraph();
							}
							PreviewSource();
							EditorUtility.ClearProgressBar();
						}
					}
					if (editorData.graphSystem == null || editorData.graphSystem.allowCompileToScript) {
						if(GUILayout.Button(new GUIContent("Compile", "Generate C# Script\nHotkey: F10 ( compile current graph )"), EditorStyles.toolbarDropDown, GUILayout.Width(60), GUILayout.Height(15))) {
							GenericMenu menu = new GenericMenu();
							if(Application.isPlaying && EditorBinding.patchType != null) {
								if(editorData.graph != null) {
									var type = TypeSerializer.Deserialize(uNodeEditorUtility.GetFullScriptName(editorData.graph), false);
									if(type != null) {
										menu.AddItem(new GUIContent("Patch Current Graph"), false, () => {
											if(preferenceData.generatorData.generationMode != GenerationKind.Compatibility) {
												if(EditorUtility.DisplayDialog(
													"Warning!",
													MESSAGE_PATCH_WARNING + $"\n\nDo you want to ignore and patch in '{preferenceData.generatorData.generationMode}' mode?",
													"Yes", "No")) {
													PatchScript(type);
													EditorUtility.ClearProgressBar();
												}
											} else {
												PatchScript(type);
												EditorUtility.ClearProgressBar();
											}
										});
									}
									//menu.AddItem(new GUIContent("Patch Project Graphs"), false, () => {
									//	GenerationUtility.CompileAndPatchProjectGraphs();
									//});
									menu.AddSeparator("");
								}
							}
							menu.AddSeparator("");
							if(Application.isPlaying) {
								menu.AddDisabledItem(new GUIContent("Compile Current Graph"), false);
								menu.AddDisabledItem(new GUIContent("Compile All C# Graph"), false);
								menu.AddSeparator("");
								menu.AddDisabledItem(new GUIContent("Compile Graphs (Project)"), false);
								menu.AddDisabledItem(new GUIContent("Compile Graphs (Project + Scenes)"), false);
							} else {
								menu.AddItem(new GUIContent("Compile Current Graph"), false, () => {
									GenerateSource();
									EditorUtility.ClearProgressBar();
								});
								menu.AddItem(new GUIContent("Compile All C# Graph"), false, () => {
									if(Application.isPlaying) {
										uNodeEditorUtility.DisplayErrorMessage("Cannot compile all graph on playmode");
										return;
									}
									AutoSaveCurrentGraph();
									GenerationUtility.CompileNativeGraphInProject();
								});
								menu.AddSeparator("");
								menu.AddItem(new GUIContent("Compile Graphs (Project)"), false, () => {
									AutoSaveCurrentGraph();
									GenerationUtility.GenerateCSharpScript();
								});
								menu.AddItem(new GUIContent("Compile Graphs (Project + Scenes)"), false, () => {
									AutoSaveCurrentGraph();
									GenerationUtility.GenerateCSharpScriptIncludingSceneGraphs();
								});
							}
							menu.ShowAsContext();
							if (selectedGraph != null && selectedGraph.graph != null) {
								AutoSaveCurrentGraph();
							}
						}
					} else if(editorData.graphSystem.allowAutoCompile) {
						if(GUILayout.Button(new GUIContent("Compile", "Generate C# Script"), EditorStyles.toolbarDropDown, GUILayout.Width(60), GUILayout.Height(15))) {
							//if(Application.isPlaying) {
							//	uNodeEditorUtility.DisplayErrorMessage("Cannot generate project scripts on playmode");
							//	return;
							//}
							GenericMenu menu = new GenericMenu();
							if(Application.isPlaying) {
								if(editorData.graph != null && EditorBinding.patchType != null) {
									var type = TypeSerializer.Deserialize(uNodeEditorUtility.GetFullScriptName(editorData.graph), false);
									if(type != null) {
										menu.AddItem(new GUIContent("Patch Current Graph"), false, () => {
											if(preferenceData.generatorData.generationMode != GenerationKind.Compatibility) {
												if(EditorUtility.DisplayDialog(
													"Warning!",
													MESSAGE_PATCH_WARNING + $"\n\nDo you want to ignore and patch in '{preferenceData.generatorData.generationMode}' mode?",
													"Yes", "No")) {
													PatchScript(type);
													EditorUtility.ClearProgressBar();
												}
											} else {
												PatchScript(type);
												EditorUtility.ClearProgressBar();
											}
										});
									}
									menu.AddItem(new GUIContent("Patch Project Graphs"), false, () => {
										if(preferenceData.generatorData.generationMode != GenerationKind.Compatibility) {
											if(EditorUtility.DisplayDialog(
												"Warning!",
												MESSAGE_PATCH_WARNING + $"\n\nDo you want to ignore and patch in '{preferenceData.generatorData.generationMode}' mode?",
												"Yes", "No")) {
												GenerationUtility.CompileAndPatchProjectGraphs();
											}
										} else {
											GenerationUtility.CompileAndPatchProjectGraphs();
										}
									});
								}
								menu.AddDisabledItem(new GUIContent("Compile Graphs (Project)"), false);
								menu.AddDisabledItem(new GUIContent("Compile Graphs (Project + Scenes)"), false);
							} else {
								menu.AddItem(new GUIContent("Compile Graphs (Project)"), false, () => {
									AutoSaveCurrentGraph();
									GenerationUtility.GenerateCSharpScript();
								});
								menu.AddItem(new GUIContent("Compile Graphs (Project + Scenes)"), false, () => {
									AutoSaveCurrentGraph();
									GenerationUtility.GenerateCSharpScriptIncludingSceneGraphs();
								});
							}
							menu.ShowAsContext();
						}
					}
				}
				if(GUILayout.Button(new GUIContent("Select", "Select uNode GameObject"), EditorStyles.toolbarButton, GUILayout.Width(50), GUILayout.Height(15))) {
					if(selectedGraph.graph != null) {
						EditorGUIUtility.PingObject(selectedGraph.graph);
					} else {
						EditorGUIUtility.PingObject(editorData.graph);
						Selection.activeObject = editorData.graph.gameObject;
					}
				}
			}
			//isDim = GUILayout.Toggle(isDim, new GUIContent("Dim"), EditorStyles.toolbarButton, GUILayout.Width(30), GUILayout.Height(15));
			if(selectedGraph == mainGraph) {
				isLocked = GUILayout.Toggle(isLocked, new GUIContent("Lock", "Keep this object selected."), EditorStyles.toolbarButton, GUILayout.Width(35), GUILayout.Height(15));
			}
			if(GUILayout.Button(new GUIContent("Refresh", "Refresh the graph.\nHotkey: F5"), EditorStyles.toolbarButton, GUILayout.Width(50), GUILayout.Height(15))) {
				if(editorData.graph != null) {
					Refresh(true);
					CheckErrors();
				}
			}
			GUILayout.Space(5);
			SavedData.rightVisibility = GUILayout.Toggle(SavedData.rightVisibility, new GUIContent("Inspector", "View to edit selected node, method or transition"), EditorStyles.toolbarButton);
			if(GUILayout.Button("~", EditorStyles.toolbarButton)) {
				GenericMenu menu = new GenericMenu();
				menu.AddItem(new GUIContent("Preference Editor"), false, () => {
					ActionWindow.ShowWindow(() => {
						uNodePreference.PreferencesGUI();
					});
				});
				menu.AddItem(new GUIContent("Code Generator Options"), false, () => {
					ActionWindow.ShowWindow(() => {
						ShowGenerateCSharpGUI();
					});
				});
				menu.AddItem(new GUIContent("Graph Explorer"), false, () => {
					ExplorerWindow.ShowWindow();
				});
				menu.AddItem(new GUIContent("Graph Hierarchy"), false, () => {
					GraphHierarchy.ShowWindow();
				});
				menu.AddItem(new GUIContent("Node Browser"), false, () => {
					NodeBrowserWindow.ShowWindow();
				});
				menu.AddSeparator("");
				menu.AddItem(new GUIContent("Import"), false, () => {
					ActionWindow.ShowWindow(() => {
						ShowImportGUI();
					});
				});
				menu.AddItem(new GUIContent("Export"), false, () => {
					ActionWindow.ShowWindow(() => {
						ShowExportGUI();
					});
				});
				menu.AddSeparator("");
				menu.AddItem(new GUIContent("Fix missing members"), false, () => {
					RefactorWindow.Refactor(editorData.graph);
				});
				menu.AddItem(new GUIContent("Refresh All Graphs"), false, () => {
					UGraphView.ClearCache();
				});
				menu.ShowAsContext();
			}
			GUILayout.EndHorizontal();
		}
		
		#region StatusBar
		[SerializeField]
		private string searchNode = "";
		private void DrawStatusBar(Rect rect) {
			currentEvent = Event.current;
			GUILayout.BeginArea(rect, EditorStyles.toolbar);
			GUILayout.BeginHorizontal();
			//if(preferenceData.enableErrorCheck) 
			{
				int errorCount = ErrorsCount();
				if(GUILayout.Button(new GUIContent(errorCount + " errors", errorCount > 0 ? uNodeGUIUtility.Styles.errorIcon : null), EditorStyles.toolbarButton)) {
					ErrorCheckWindow.Init();
				}
			}
			bool enableSnapping = preferenceData.enableSnapping && (preferenceData.graphSnapping || preferenceData.gridSnapping || preferenceData.spacingSnapping || preferenceData.nodePortSnapping);
			if(GUILayout.Toggle(enableSnapping, new GUIContent("Snap", "Snap the node to the port or grid"), EditorStyles.toolbarButton) != enableSnapping) {
				GenericMenu menu = new GenericMenu();
				menu.AddItem(new GUIContent("Enable Snapping"), preferenceData.enableSnapping, () => {
					preferenceData.enableSnapping = !preferenceData.enableSnapping;
					uNodePreference.SavePreference();
				});
				menu.AddSeparator("");
				menu.AddItem(new GUIContent("Graph Snapping"), preferenceData.graphSnapping, () => {
					preferenceData.graphSnapping = !preferenceData.graphSnapping;
					uNodePreference.SavePreference();
				});
				menu.AddItem(new GUIContent("Node Port Snapping"), preferenceData.nodePortSnapping, () => {
					preferenceData.nodePortSnapping = !preferenceData.nodePortSnapping;
					uNodePreference.SavePreference();
				});
				menu.AddItem(new GUIContent("Grid Snapping"), preferenceData.gridSnapping, () => {
					preferenceData.gridSnapping = !preferenceData.gridSnapping;
					uNodePreference.SavePreference();
				});
				menu.AddItem(new GUIContent("Spacing Snapping"), preferenceData.spacingSnapping, () => {
					preferenceData.spacingSnapping = !preferenceData.spacingSnapping;
					uNodePreference.SavePreference();
				});
				menu.ShowAsContext();
			}
			bool carry = GUILayout.Toggle(preferenceData.carryNodes, new GUIContent("Carry", "Carry the connected node when moving (CTRL)"), EditorStyles.toolbarButton);
			if(carry != preferenceData.carryNodes) {
				preferenceData.carryNodes = carry;
				uNodePreference.SavePreference();
			}
			GUILayout.FlexibleSpace();
			GUILayout.Label("Zoom : " + graphEditor.zoomScale.ToString("F2"));
			string search = uNodeGUIUtility.DrawSearchBar(searchNode, GUIContent.none, "uNodeSearchBar", GUILayout.MaxWidth(150));
			if(!search.Equals(searchNode) || currentEvent.isKey && currentEvent.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "uNodeSearchBar") {
				searchNode = search;
				graphEditor.OnSearchChanged(searchNode);
			}
			EditorGUI.BeginDisabledGroup(searchNode == null || string.IsNullOrEmpty(searchNode.Trim()));
			if(GUILayout.Button("Prev", EditorStyles.toolbarButton)) {
				graphEditor.OnSearchPrev();
			}
			if(GUILayout.Button("Next", EditorStyles.toolbarButton)) {
				graphEditor.OnSearchNext();
			}
			EditorGUI.EndDisabledGroup();
			GUILayout.EndHorizontal();
			GUILayout.EndArea();
		}

		private int ErrorsCount() {
			if(editorErrors != null) {
				int count = 0;
				foreach(var pair in editorErrors) {
					if(pair.Key != null && pair.Value != null) {
						count += pair.Value.Count;
					}
				}
				return count;
			}
			return 0;
		}
		#endregion
		#endregion

		#region Save & Load Setting
		private static uNodeEditorData _savedData;
		public static uNodeEditorData SavedData {
			get {
				if(_savedData == null) {
					LoadOptions();
					if(_savedData == null) {
						_savedData = new uNodeEditorData();
					}
				}
				return _savedData;
			}
			set {
				_savedData = value;
			}
		}

		/// <summary>
		/// Save the current graph
		/// Note: this function will not work on playmode use SaveCurrentGraph if need to save either on editor or in playmode.
		/// </summary>
		public static void AutoSaveCurrentGraph() {
			if(Application.isPlaying)
				return;
			SaveCurrentGraph();
		}

		/// <summary>
		/// Save the current graph
		/// </summary>
		public static void SaveCurrentGraph() {
			if(window == null) return;
			if(window.selectedGraph != null && window.selectedGraph.graph != null && window.editorData.owner != null) {
				GraphUtility.SaveGraph(window.editorData.owner);
			} else if(Application.isPlaying && window.editorData.graph is uNodeRuntime runtime && runtime.originalGraph != null) {
				GraphUtility.SaveRuntimeGraph(runtime);
			}
		}

		public static void SaveOptions() {
			EditorDataSerializer.Save(_savedData, "EditorData");
		}

		public static void LoadOptions() {
			_savedData = EditorDataSerializer.Load<uNodeEditorData>("EditorData");
			if(_savedData == null) {
				_savedData = new uNodeEditorData();
				SaveOptions();
			}
		}
		#endregion

		#region Tools
		public class ToolSettings {
			public GlobalVariable target_GlobalVariable;
			public uNodeRoot target_uNodeObject;
			public GameObject target_GameObject;
			public int i_oType = 0;
			public int exportTo = 0;
			public bool toChild;
			public bool includeOtherComponent;
			public bool overwrite = true;
		}
		private ToolSettings toolSetting = new ToolSettings();
		private uNodeData.GeneratorSettings generatorSettings {
			get {
				if(editorData.graph) {
					var obj = editorData.owner.GetComponent<uNodeData>();
					if(obj) {
						return obj.generatorSettings;
					} else if(!IsDisableEdit() && !(editorData.graph is IIndependentGraph)) {
						return editorData.owner.AddComponent<uNodeData>().generatorSettings;
					}
				} else if(editorData.graphData) {
					return editorData.graphData.generatorSettings;
				}
				return null;
			}
		}

		public void PreviewSource() {
			//string nameSpace;
			//IList<string> usingNamespace;
			//bool debug, debugValue;
			//if(generatorSettings != null) {
			//	nameSpace = generatorSettings.Namespace;
			//	usingNamespace = generatorSettings.usingNamespace;
			//	debug = generatorSettings.debug;
			//	debugValue = generatorSettings.debugValueNode;
			//} else if(editorData.graph is IIndependentGraph graph) {
			//	nameSpace = graph.Namespace;
			//	usingNamespace = graph.UsingNamespaces;
			//	debug = editorData.graph.graphData.debug;
			//	debugValue = editorData.graph.graphData.debugValueNode;
			//} else {
			//	throw new InvalidOperationException();
			//}
			Directory.CreateDirectory(GenerationUtility.tempFolder);
			char separator = Path.DirectorySeparatorChar;
			try {
				System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
				watch.Start();
				//var script = CodeGenerator.Generate(new CodeGenerator.GeneratorSetting(editorData.graphs, nameSpace, usingNamespace) {
				//	forceGenerateAllNode = preferenceData.generatorData.forceGenerateAllNode,
				//	resolveUnityObject = preferenceData.generatorData.resolveUnityObject,
				//	fullTypeName = preferenceData.generatorData.fullTypeName,
				//	fullComment = preferenceData.generatorData.fullComment,
				//	enableOptimization = preferenceData.generatorData.enableOptimization,
				//	generateTwice = preferenceData.generatorData.generateNodeTwice,
				//	//debugScript = true,
				//	//debugValueNode = true,
				//	debugScript = debug,
				//	debugValueNode = debugValue,
				//	debugPreprocessor = false,
				//	includeGraphInformation = true,
				//	targetData = editorData.graphData,
				//	generationMode = preferenceData.generatorData.generationMode,
				//	updateProgress = (progress, text) => {
				//		EditorUtility.DisplayProgressBar("Loading", text, progress);
				//	},
				//});
				var script = GenerationUtility.GenerateCSharpScript(new uNodeRoot[] { editorData.graph }, editorData.graphData, true, (progress, text) => {
					EditorUtility.DisplayProgressBar($"Generating C# Scripts", text, progress);
				});
				var generatedScript = script.ToScript(out var informations);
				string path = GenerationUtility.tempFolder + separator + script.fileName + ".cs";
				using(StreamWriter sw = new StreamWriter(path)) {
					sw.Write(generatedScript);
					sw.Close();
				}
				watch.Stop();
				string originalScript = generatedScript;
				//Debug.LogFormat("Generating C# took {0,8:N3} s.", watch.Elapsed.TotalSeconds);
				if(preferenceData.generatorData != null && preferenceData.generatorData.analyzeScript && preferenceData.generatorData.formatScript) {
					var codeFormatter = TypeSerializer.Deserialize("MaxyGames.uNode.Editors.CSharpFormatter", false);
					if(codeFormatter != null) {
						var str = codeFormatter.
							GetMethod("FormatCode").
							Invoke(null, new object[] { originalScript }) as string;
						originalScript = str;
						generatedScript = str;
					}
				}
				var syntaxHighlighter = TypeSerializer.Deserialize("MaxyGames.SyntaxHighlighter.CSharpSyntaxHighlighter", false);
				if(syntaxHighlighter != null) {
					string highlight = syntaxHighlighter.GetMethod("GetRichText").Invoke(null, new object[] { generatedScript }) as string;
					if(!string.IsNullOrEmpty(highlight)) {
						generatedScript = highlight;
					}
				}
				PreviewSourceWindow.ShowWindow(generatedScript, originalScript).informations = informations?.ToArray();
				EditorUtility.ClearProgressBar();
#if UNODE_DEBUG
				uNodeEditorUtility.CopyToClipboard(script.ToRawScript());
#endif
			}
			catch {
				EditorUtility.ClearProgressBar();
				Debug.LogError("Aborting Generating C# Script because of errors.");
				throw;
			}
		}

		private void PatchScript(Type scriptType) {
			if(editorData.graph == null || EditorBinding.patchType == null)
				return;
			try {
				System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
				watch.Start();
				var script = GenerationUtility.GenerateCSharpScript(new uNodeRoot[] { editorData.graph }, editorData.graphData, true, (progress, text) => {
					EditorUtility.DisplayProgressBar($"Generating C# Scripts", text, progress);
				});
				//var script = CodeGenerator.Generate(new CodeGenerator.GeneratorSetting(new uNodeRoot[] { editorData.graph }, nameSpace, usingNamespace) {
				//	forceGenerateAllNode = preferenceData.generatorData.forceGenerateAllNode,
				//	resolveUnityObject = preferenceData.generatorData.resolveUnityObject,
				//	enableOptimization = preferenceData.generatorData.enableOptimization,
				//	fullTypeName = true,
				//	fullComment = false,
				//	generateTwice = preferenceData.generatorData.generateNodeTwice,
				//	debugScript = true, //Enable debug on patching script.
				//	debugValueNode = true, //Enable debug on patching script.
				//	debugPreprocessor = false, //Prevent debug preprocessor to be included in generated code.
				//	includeGraphInformation = false, //Don't include graph information as we don't need it.
				//	targetData = editorData.graphData,
				//	generationMode = preferenceData.generatorData.generationMode,
				//	updateProgress = (progress, text) => {
				//		EditorUtility.DisplayProgressBar("Loading", text, progress);
				//	},
				//});
				var dir = "TempScript" + Path.DirectorySeparatorChar + "Patched";
				Directory.CreateDirectory(dir);
				var path = Path.GetFullPath(dir) + Path.DirectorySeparatorChar + script.fileName + ".cs";
				using(StreamWriter sw = new StreamWriter(path)) {
					var generatedScript = script.ToScript(out var informations);
					SavedData.UnregisterGraphInfo(script.graphOwner);
					if(informations != null) {
						SavedData.RegisterGraphInfos(informations, script.graphOwner, path);
					}
					sw.Write(generatedScript);
					sw.Close();
				}
				var db = GenerationUtility.GetDatabase();
				foreach(var root in script.graphs) {
					if(db.graphDatabases.Any(g => g.graph == root)) {
						continue;
					}
					db.graphDatabases.Add(new uNodeResourceDatabase.RuntimeGraphDatabase() {
						graph = root,
					});
					EditorUtility.SetDirty(db);
				}
				//if(generatorSettings.convertLineEnding) {
				//	generatedScript = ConvertLineEnding(generatedScript,
				//		Application.platform != RuntimePlatform.WindowsEditor);
				//}
				EditorUtility.DisplayProgressBar("Compiling", "", 1);
				var assembly = GenerationUtility.CompileFromFile(path);
				if (assembly != null) {
					string typeName;
					if(string.IsNullOrEmpty(script.Namespace)) {
						typeName = script.classNames.First().Value;
					} else {
						typeName = script.Namespace + "." + script.classNames.First().Value;
					}
					var type = assembly.GetType(typeName);
					if (type != null) {
						EditorUtility.DisplayProgressBar("Patching", "Patch generated c# into existing script.", 1);
						EditorBinding.patchType(scriptType, type);
						ReflectionUtils.RegisterRuntimeAssembly(assembly);
						ReflectionUtils.UpdateAssemblies();
						ReflectionUtils.GetAssemblies();
						watch.Stop();
						Debug.LogFormat("Generating & Patching type: {1} took {0,8:N3} s.", watch.Elapsed.TotalSeconds, scriptType);
					} else {
						watch.Stop();
						Debug.LogError($"Error on patching script because type: {typeName} is not found.");
					}
				}
				EditorUtility.ClearProgressBar();
			}
			catch {
				EditorUtility.ClearProgressBar();
				Debug.LogError("Aborting Generating C# Script because have error.");
				throw;
			}
		}

		public void GenerateSource() {
			GenerationUtility.CompileNativeGraph(editorData.owner);
		}

		private void ShowGenerateCSharpGUI(){
			if(!(editorData.graph is IIndependentGraph) && (generatorSettings == null || editorData.graph == null)) {
				return;
			}
			if(editorData.graph is IIndependentGraph graph) {
				VariableEditorUtility.DrawNamespace("Using Namespaces", graph.UsingNamespaces, editorData.graphData, (arr) => {
					graph.UsingNamespaces = arr as List<string> ?? arr.ToList();
					uNodeEditorUtility.MarkDirty(editorData.graphData);
				});
				uNodeGUIUtility.ShowField("debug", editorData.graph.graphData, null);
				if(editorData.graph.graphData.debug)
					uNodeGUIUtility.ShowField("debugValueNode", editorData.graph.graphData, null);
			} else {
				EditorGUI.BeginChangeCheck();
				uNodeGUIUtility.ShowField("Namespace", generatorSettings, null);
				VariableEditorUtility.DrawNamespace("Using Namespaces", generatorSettings.usingNamespace.ToList(), editorData.graphData, (arr) => {
					generatorSettings.usingNamespace = arr.ToArray();
					uNodeEditorUtility.MarkDirty(editorData.graphData);
				});
				uNodeGUIUtility.ShowField("debug", generatorSettings, null);
				if(generatorSettings.debug)
					uNodeGUIUtility.ShowField("debugValueNode", generatorSettings, null);
				if(EditorGUI.EndChangeCheck()) {
					SaveOptions();
				}
			}
			if (editorData.graphSystem == null || editorData.graphSystem.allowCompileToScript) {
				if (GUILayout.Button(new GUIContent("Generate"))) {
					GenerateSource();
				}
			}
		}

		private void ShowImportGUI(){
			toolSetting.i_oType = EditorGUILayout.Popup("Import Type", toolSetting.i_oType, new string[] {
				"Variable and Node",
				"Node",
				"Variable"});
			if(toolSetting.i_oType != 1) {
				toolSetting.exportTo = EditorGUILayout.Popup("Import From", toolSetting.exportTo, new string[] {
				"uNodeRoot",
				"Global Variable"});
				if(toolSetting.exportTo == 0) {
					toolSetting.target_uNodeObject = EditorGUILayout.ObjectField(new GUIContent("Target uNode"), toolSetting.target_uNodeObject, typeof(uNodeRoot), true) as uNodeRoot;
					toolSetting.overwrite = EditorGUILayout.Toggle(new GUIContent("Overwrite"), toolSetting.overwrite);
				} else {
					toolSetting.target_GlobalVariable = EditorGUILayout.ObjectField(new GUIContent("Target Global Variable"), toolSetting.target_GlobalVariable, typeof(GlobalVariable), true) as GlobalVariable;
				}
			} else {
				toolSetting.target_uNodeObject = EditorGUILayout.ObjectField(new GUIContent("Target uNode"), toolSetting.target_uNodeObject, typeof(uNodeRoot), true) as uNodeRoot;
			}
			if(GUILayout.Button(new GUIContent("Import"))) {
				if(toolSetting.exportTo == 0) {
					if(toolSetting.target_uNodeObject != null &&
						EditorUtility.DisplayDialog("Are you sure?", "Do you wish to import it\nNote: this can't be undone", "Import", "Cancel")) {
						if(toolSetting.i_oType == 2) {
							if(toolSetting.overwrite) {
								editorData.graph.Variables.Clear();
							}
							foreach(VariableData variable in toolSetting.target_uNodeObject.Variables) {
								editorData.graph.Variables.Add(new VariableData(variable));
							}
							return;
						}
						GameObject go = Instantiate(NodeEditorUtility.GetNodeRoot(toolSetting.target_uNodeObject), Vector3.zero, Quaternion.identity) as GameObject;
						uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(go) as GameObject);
						if(toolSetting.overwrite) {
							if(toolSetting.i_oType != 1)
								editorData.graph.Variables.Clear();
							if(toolSetting.i_oType != 2)
								DestroyImmediate(NodeEditorUtility.GetNodeRoot(editorData.graph));
						}
						if(toolSetting.i_oType != 1) {
							foreach(VariableData variable in toolSetting.target_uNodeObject.Variables) {
								editorData.graph.Variables.Add(new VariableData(variable));
							}
						}
						if(toolSetting.i_oType != 2) {
							uNodeRoot root = editorData.graph;
							if(uNodeEditorUtility.IsPrefab(root)) {
								root = PrefabUtility.InstantiatePrefab(root) as uNodeRoot;
								uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(go) as GameObject);
							}
							List<uNodeComponent> needReflectedComponent = new List<uNodeComponent>();
							for(int i = 0; i < go.transform.childCount; i++) {
								uNodeComponent comp = go.transform.GetChild(i).GetComponent<uNodeComponent>();
								if(comp != null) {
									if(comp is NodeComponent && root is IClass cls && 
										(cls.IsStruct || typeof(MonoBehaviour).IsAssignableFrom(cls.GetInheritType()))) {
										continue;
									}
									uNodeComponent[] comps = comp.GetComponentsInChildren<uNodeComponent>(true);
									for(int x = 0; x < comps.Length; x++) {
										if(NodeEditorUtility.NeedReflectComponent(comps[x], toolSetting.target_uNodeObject, root)) {
											needReflectedComponent.Add(comps[x]);
										}
									}
									uNodeEditorUtility.SetParent(comp.transform, NodeEditorUtility.GetNodeRoot(root).transform);
									i--;
								}
							}
							if(needReflectedComponent.Count > 0) {
								NodeEditorUtility.PerformReflectComponent(needReflectedComponent, toolSetting.target_uNodeObject, root);
							}
							if(uNodeEditorUtility.IsPrefab(editorData.graph)) {
								uNodeEditorUtility.SavePrefabAsset(root.transform.root.gameObject, editorData.graph.transform.root.gameObject);
								DestroyImmediate(root.transform.root.gameObject);
							}
						}
						DestroyImmediate(go);
						Refresh();
					} else if(toolSetting.target_uNodeObject == null) {
						Debug.LogError("Target uNode must exist");
					}
				} else if(toolSetting.exportTo == 1) {
					if(toolSetting.target_GlobalVariable != null &&
						EditorUtility.DisplayDialog("Are you sure?", "Do you wish to import it\nNote: this can't be undone", "Import", "Cancel")) {
						foreach(VariableData variable in toolSetting.target_GlobalVariable.variable) {
							editorData.graph.Variables.Add(new VariableData(variable));
						}
					} else if(toolSetting.target_GlobalVariable == null) {
						Debug.LogError("Target GlobalVariable must exist");
					}
				}
			}
		}

		private void ShowExportGUI() {
			toolSetting.i_oType = EditorGUILayout.Popup("Export Type", toolSetting.i_oType, new string[] {
				"Variable and Node",
				"Node",
				"Variable"});
			toolSetting.exportTo = EditorGUILayout.Popup("Export To", toolSetting.exportTo, new string[] {
				"Prefab",
				"Game Object",
				"Exist uNodeRoot",
				"Global Variable",
				"Asset"});
			if(toolSetting.exportTo == 1) {
				toolSetting.target_GameObject = EditorGUILayout.ObjectField(new GUIContent("Target GameObject"), toolSetting.target_GameObject, typeof(GameObject), true) as GameObject;
				toolSetting.toChild = EditorGUILayout.Toggle(new GUIContent("Export To Child"), toolSetting.toChild);
				if(toolSetting.toChild) {
					toolSetting.includeOtherComponent = EditorGUILayout.Toggle(new GUIContent("Include Other Component"), toolSetting.includeOtherComponent);
				}
			} else if(toolSetting.exportTo == 2) {
				toolSetting.target_uNodeObject = EditorGUILayout.ObjectField(new GUIContent("Target uNode"), toolSetting.target_uNodeObject, typeof(uNodeRoot), true) as uNodeRoot;
				toolSetting.overwrite = EditorGUILayout.Toggle(new GUIContent("Overwrite"), toolSetting.overwrite);
			}
			if(GUILayout.Button(new GUIContent("Export"))) {
				if(toolSetting.exportTo == 0) {//Export To Prefab
					string path = EditorUtility.SaveFilePanelInProject("Export uNode to Prefab",
						editorData.graph.gameObject.name + ".prefab",
						"prefab",
						"Please enter a file name to save the prefab to");
					if(path.Length != 0) {
						GameObject go = Instantiate(editorData.graph.gameObject, Vector3.zero, Quaternion.identity) as GameObject;
						uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(go) as GameObject);
						go.name = "uNode";
						go.transform.parent = null;
						foreach(Component comp in go.GetComponents<Component>()) {
							if(comp is Transform || comp is uNodeRoot)
								continue;
							DestroyImmediate(comp);
						}
						uNodeRoot UNR = go.GetComponent<uNodeRoot>();
						for(int i = 0; i < go.transform.childCount; i++) {
							Transform t = go.transform.GetChild(i);
							if(t.gameObject != NodeEditorUtility.GetNodeRoot(UNR)) {
								DestroyImmediate(t.gameObject);
								i--;
							}
						}
						if(toolSetting.i_oType == 1) {
							UNR.Variables.Clear();
						}
						if(toolSetting.i_oType == 2) {
							DestroyImmediate(NodeEditorUtility.GetNodeRoot(UNR));
							UNR.Refresh();
						}
						PrefabUtility.SaveAsPrefabAsset(go, path);
						DestroyImmediate(go);
					}
				} else if(toolSetting.exportTo == 1) {//Export To Game Object
					if(toolSetting.target_GameObject != null &&
						EditorUtility.DisplayDialog("Are you sure?", "Do you wish to export the uNode\nNote: this can't be undone", "Export", "Cancel")) {
						if(toolSetting.i_oType == 2) {
							uNodeRoot ES = toolSetting.target_GameObject.AddComponent<uNodeRoot>();
							foreach(VariableData variable in editorData.graph.Variables) {
								ES.Variables.Add(new VariableData(variable));
							}
							return;
						}
						if(!toolSetting.toChild) {
							GameObject go = Instantiate(NodeEditorUtility.GetNodeRoot(editorData.graph), Vector3.zero, Quaternion.identity) as GameObject;
							string s = go.name.Replace("(Clone)", "");
							go.name = s;
							uNodeRoot ES = NodeEditorUtility.CopyComponent(editorData.graph, toolSetting.target_GameObject) as uNodeRoot;
							if(toolSetting.i_oType != 1) {
								foreach(VariableData variable in editorData.graph.Variables) {
									ES.Variables.Add(new VariableData(variable));
								}
							}
							if(toolSetting.i_oType != 2) {
								ES.RootObject = go;
								List<uNodeComponent> needReflectedComponent = new List<uNodeComponent>();
								uNodeComponent[] comps = go.GetComponentsInChildren<uNodeComponent>(true);
								for(int i = 0; i < comps.Length; i++) {
									uNodeComponent comp = comps[i];
									if(comp != null) {
										if(NodeEditorUtility.NeedReflectComponent(comp, editorData.graph, ES)) {
											needReflectedComponent.Add(comp);
										}
									}
								}
								if(needReflectedComponent.Count > 0) {
									NodeEditorUtility.PerformReflectComponent(needReflectedComponent, editorData.graph, ES);
								}
								uNodeEditorUtility.SetParent(go.transform, toolSetting.target_GameObject.transform);
							}
							ES.Refresh();
						} else {
							GameObject go = Instantiate(editorData.graph.gameObject, Vector3.zero, Quaternion.identity) as GameObject;
							string s = go.name.Replace("(Clone)", "");
							go.name = s;
							uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(go) as GameObject);
							if(!toolSetting.includeOtherComponent) {
								foreach(Component comp in go.GetComponents<Component>()) {
									if(comp is Transform || comp is uNodeRoot)
										continue;
									DestroyImmediate(comp);
								}
							}
							uNodeRoot UNR = go.GetComponent<uNodeRoot>();
							for(int i = 0; i < go.transform.childCount; i++) {
								Transform t = go.transform.GetChild(i);
								if(t.gameObject != NodeEditorUtility.GetNodeRoot(UNR)) {
									DestroyImmediate(t.gameObject);
									i--;
								}
							}
							if(toolSetting.i_oType == 1) {
								UNR.Variables.Clear();
							}
							uNodeEditorUtility.SetParent(go.transform, toolSetting.target_GameObject.transform);
						}
					} else if(toolSetting.target_GameObject == null) {
						Debug.LogError("Target GameObject must exist");
					}
				} else if(toolSetting.exportTo == 2) {//Export To uNode
					if(toolSetting.target_uNodeObject != null &&
						EditorUtility.DisplayDialog("Are you sure?", "Do you wish to export the uNode\nNote: this can't be undone", "Export", "Cancel")) {
						if(toolSetting.i_oType == 2) {
							if(toolSetting.overwrite) {
								toolSetting.target_uNodeObject.Variables.Clear();
							}
							foreach(VariableData variable in editorData.graph.Variables) {
								toolSetting.target_uNodeObject.Variables.Add(new VariableData(variable));
							}
							return;
						}
						GameObject go = Instantiate(NodeEditorUtility.GetNodeRoot(editorData.graph), Vector3.zero, Quaternion.identity) as GameObject;
						string s = go.name.Replace("(Clone)", "");
						go.name = s;
						if(toolSetting.overwrite) {
							if(toolSetting.i_oType != 1)
								toolSetting.target_uNodeObject.Variables.Clear();
							if(toolSetting.i_oType != 2)
								DestroyImmediate(NodeEditorUtility.GetNodeRoot(toolSetting.target_uNodeObject));
						}
						if(toolSetting.i_oType != 1) {
							foreach(VariableData variable in editorData.graph.Variables) {
								toolSetting.target_uNodeObject.Variables.Add(new VariableData(variable));
							}
						}
						if(toolSetting.i_oType != 2) {
							uNodeRoot root = toolSetting.target_uNodeObject;
							if(uNodeEditorUtility.IsPrefab(root)) {
								root = PrefabUtility.InstantiatePrefab(root) as uNodeRoot;
								uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(go) as GameObject);
							}
							List<uNodeComponent> needReflectedComponent = new List<uNodeComponent>();
							for(int i = 0; i < go.transform.childCount; i++) {
								uNodeComponent comp = go.transform.GetChild(i).GetComponent<uNodeComponent>();
								if(comp != null) {
									uNodeComponent[] comps = comp.GetComponentsInChildren<uNodeComponent>(true);
									for(int x = 0; x < comps.Length; x++) {
										if(NodeEditorUtility.NeedReflectComponent(comps[x], editorData.graph, root)) {
											needReflectedComponent.Add(comps[x]);
										}
									}
									comp.transform.parent = NodeEditorUtility.GetNodeRoot(root).transform;
									i--;
								}
							}
							if(needReflectedComponent.Count > 0) {
								NodeEditorUtility.PerformReflectComponent(needReflectedComponent, editorData.graph, root);
							}
							if(uNodeEditorUtility.IsPrefab(toolSetting.target_uNodeObject)) {
								uNodeEditorUtility.SavePrefabAsset(root.transform.root.gameObject, toolSetting.target_uNodeObject.transform.root.gameObject);
								DestroyImmediate(root.transform.root.gameObject);
							}
						}
						DestroyImmediate(go);
						toolSetting.target_uNodeObject.Refresh();
					} else if(toolSetting.target_uNodeObject == null) {
						Debug.LogError("Target uNode must exist");
					}
				} else if(toolSetting.exportTo == 3) {//Export To Global Variable
					string path = EditorUtility.SaveFilePanelInProject("Export uNode to GlobalVariable",
						editorData.graph.gameObject.name + ".asset",
						"asset",
						"Please enter a file name to save the variable to");
					if(path.Length != 0) {
						GlobalVariable asset = GlobalVariable.CreateInstance(typeof(GlobalVariable)) as GlobalVariable;
						foreach(VariableData variable in editorData.graph.Variables) {
							asset.variable.Add(new VariableData(variable));
						}
						AssetDatabase.CreateAsset(asset, path);
						AssetDatabase.SaveAssets();
					}
				}
			}
		}
		#endregion

		#region Useful Function
		/// <summary>
		/// Is the editor are not allowed to edit
		/// </summary>
		/// <returns></returns>
		public bool IsDisableEdit() {
			return (!Application.isPlaying || uNodePreference.GetPreference().preventEditingPrefab) && uNodeEditorUtility.IsPrefab(editorData.owner);
		}

		public void OpenNewGraphTab() {
			string path = EditorUtility.OpenFilePanelWithFilters("Open uNode", "Assets", new string[] { "uNode files", "prefab,asset" });
			if(path.StartsWith(Application.dataPath)) {
				path = "Assets" + path.Substring(Application.dataPath.Length);
			} else {
				path = null;
			}
			if(!string.IsNullOrEmpty(path)) {
				if(path.EndsWith(".prefab")) {
					GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
					if(go != null) {
						var root = go.GetComponent<uNodeRoot>();
						if(root != null) {
							ChangeTarget(root, true);
							RegisterOpenedFile(path);
						} else {
							var data = go.GetComponent<uNodeData>();
							if(data != null) {
								ChangeTarget(data, true);
								RegisterOpenedFile(path);
							}
						}
					}
				}
			}
		}

		public static List<UnityEngine.Object> FindLastOpenedGraphs() {
			List<UnityEngine.Object> lastOpenedObjects = new List<UnityEngine.Object>();
			if(SavedData.lastOpenedFile == null) {
				return lastOpenedObjects;
			}
			for(int i = 0; i < SavedData.lastOpenedFile.Count; i++) {
				string path = SavedData.lastOpenedFile[i];
				if(!File.Exists(path))
					continue;
				if(path.EndsWith(".prefab")) {
					GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
					if(go != null) {
						var root = go.GetComponent<uNodeRoot>();
						if(root != null) {
							lastOpenedObjects.Add(root);
						} else {
							var data = go.GetComponent<uNodeData>();
							if(data != null) {
								lastOpenedObjects.Add(data);
							}
						}
					}
				}
				if(lastOpenedObjects.Count >= 10) {
					break;
				}
			}
			return lastOpenedObjects;
		}

		public static void ClearLastOpenedGraphs() {
			SavedData.lastOpenedFile = null;
			SaveOptions();
		}

		/// <summary>
		/// Clear the graph cached data so the graph will have fresh datas
		/// </summary>
		public static void ClearGraphCache() {
			UGraphView.ClearCache();
		}

		/// <summary>
		/// Check the editor errors.
		/// </summary>
		public void CheckErrors() {
			if(editorData == null)
				return;
#if UseProfiler
			Profiler.BeginSample("Check Errors");
#endif
			if(editorErrors != null) {
				var map = new Dictionary<UnityEngine.Object, List<uNodeUtility.ErrorMessage>>(editorErrors);
				editorErrors.Clear();
				foreach(var pair in map) {
					if(pair.Key != null && pair.Key as UnityEngine.Object) {
						editorErrors.Add(pair.Key, pair.Value);
					}
				}
			}
			var roots = editorData.graphs;
			if(roots != null) {
				for(int i = 0; i < roots.Length; i++) {
					uNodeUtility.ClearEditorError(roots[i]);
					var nodes = NodeEditorUtility.FindAllNode(roots[i]);
					if(nodes != null) {
						foreach(var node in nodes) {
#if UseProfiler
							Profiler.BeginSample("Check Node Error");
#endif
							try {
								uNodeUtility.ClearEditorError(node);
								node.CheckError();
							}
							catch(System.Exception ex) {
								Debug.LogException(ex);
							}
#if UseProfiler
							Profiler.EndSample();
#endif
						}
					}
					if(roots[i].RootObject != null) {
						FindMissingScripts(roots[i].RootObject.transform, roots[i]);
					}
				}
			}
#if UseProfiler
			Profiler.EndSample();
#endif
		}

		private void FindMissingScripts(Transform transform, uNodeRoot owner) {
			var comps = transform.GetComponents<MonoBehaviour>();
			for(int i=0;i<comps.Length;i++) {
				if(comps[i] == null) {
					uNodeUtility.RegisterEditorError(owner, transform.gameObject, "Missing script found on object: " + transform.gameObject.name, (position) => {
						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent("Remove object"), false, () => {
							NodeEditorUtility.RemoveObject(owner.gameObject, transform.gameObject);
						});
						menu.ShowAsContext();
					});
					break;
				}
			}
			if(transform.childCount > 0) {
				foreach(Transform tr in transform) {
					FindMissingScripts(tr, owner);
				}
			}

		}
		#endregion

		#region EventHandler
		private void DragToVariableHandler(Rect rect, List<VariableData> variables, UnityEngine.Object owner) {
			if(rect.Contains(currentEvent.mousePosition)) {
				if((currentEvent.type == EventType.DragPerform || currentEvent.type == EventType.DragUpdated)) {
					bool isPrefab = uNodeEditorUtility.IsPrefab(editorData.owner);
					if(DragAndDrop.GetGenericData("uNode") == null &&
						DragAndDrop.visualMode == DragAndDropVisualMode.None &&
						DragAndDrop.objectReferences.Length == 1) {
						if(isPrefab) {
							if(uNodeEditorUtility.IsSceneObject(DragAndDrop.objectReferences[0])) {
								DragAndDrop.visualMode = DragAndDropVisualMode.None;
								return;
							}
						}
						DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					}
					if(currentEvent.type == EventType.DragPerform) {
						DragAndDrop.AcceptDrag();
						if(DragAndDrop.GetGenericData("uNode") != null) {
							//Nothing todo.
						} else if(DragAndDrop.objectReferences.Length == 1) {//Dragging UnityObject
							var dragObject = DragAndDrop.objectReferences[0];
							//rightClickPos = currentEvent.mousePosition;
							//var iPOS = GetMousePositionForMenu();
							GenericMenu menu = new GenericMenu();
							menu.AddDisabledItem(new GUIContent("Add variable"));
							menu.AddSeparator("");
							menu.AddItem(new GUIContent(dragObject.GetType().Name), false, () => {
								var variable = uNodeEditorUtility.AddVariable(dragObject.GetType(), variables, owner);
								variable.variable = dragObject;
								variable.Serialize();
							});
							menu.AddSeparator("");
							if(dragObject is GameObject) {
								Component[] components = (dragObject as GameObject).GetComponents<Component>();
								foreach(var c in components) {
									menu.AddItem(new GUIContent(c.GetType().Name), false, (comp) => {
										var variable = uNodeEditorUtility.AddVariable(comp.GetType(), variables, owner);
										variable.variable = comp;
										variable.Serialize();
									}, c);
								}
							} else if(dragObject is Component) {
								menu.AddItem(new GUIContent("GameObject"), false, () => {
									var variable = uNodeEditorUtility.AddVariable(dragObject.GetType(), variables, owner);
									variable.variable = (dragObject as Component).gameObject;
									variable.Serialize();
								});
								Component[] components = (dragObject as Component).GetComponents<Component>();
								foreach(var c in components) {
									menu.AddItem(new GUIContent(c.GetType().Name), false, (comp) => {
										var variable = uNodeEditorUtility.AddVariable(comp.GetType(), variables, owner);
										variable.variable = comp;
										variable.Serialize();
									}, c);
								}
							}
							menu.ShowAsContext();
						}
						Event.current.Use();
					}
				}
			} else if((currentEvent.type == EventType.DragPerform ||
				currentEvent.type == EventType.DragUpdated) &&
				DragAndDrop.GetGenericData("uNode") != null) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			}
		}

		private void TopEventHandler() {
			var e = Event.current;
			if(e.type == EventType.KeyUp) {
				if(e.keyCode == KeyCode.F10) {
					if (editorData.graphSystem == null || editorData.graphSystem.allowCompileToScript) {
						GenerateSource();
					}
				} else if(e.keyCode == KeyCode.F9) {
					if (editorData.graphSystem == null || editorData.graphSystem.allowPreviewScript) {
						PreviewSource();
					}
				} else if(e.keyCode == KeyCode.F5) {
					Refresh(true);
				}
			}
		}
		#endregion

		#region Menu Functions
		static void ShowAddNewRootMenu(GameObject owner, Action<uNodeRoot> onAdded = null) {
			GenericMenu menu = new GenericMenu();
			var graphSystem = GraphUtility.FindGraphSystemAttributes();
			int lastOrder = int.MinValue;
			for (int i = 0; i < graphSystem.Count; i++) {
				var g = graphSystem[i];
				if(!g.allowCreateInScene) continue;
				if (lastOrder != int.MinValue && Mathf.Abs(g.order - lastOrder) >= 10) {
					menu.AddSeparator("");
				}
				lastOrder = g.order;
				menu.AddItem(new GUIContent(g.menu), false, delegate () {
					var comp = owner.AddComponent(g.type);
					if(onAdded != null)
						onAdded(comp as uNodeRoot);
				});
			}
			menu.AddSeparator("");
			menu.AddItem(new GUIContent("Import From JSON"), false, delegate () {
				string path = EditorUtility.OpenFilePanel("Import from JSON", "", "json");
				if(File.Exists(path)) {
					string json = File.ReadAllText(path);
					var graph = uNodeHelper.ImportGraphFromJson(json, owner);
					if(onAdded != null)
						onAdded(graph);
				}
			});
			var templates = uNodeEditorUtility.FindAssetsByType<uNodeTemplate>();
			if(templates != null && templates.Count > 0) {
				menu.AddSeparator("");
				foreach(var t in templates) {
					string path = t.name;
					if(!string.IsNullOrEmpty(t.path)) {
						path = t.path;
					}
					menu.AddItem(new GUIContent(path), false, (temp) => {
						var tmp = temp as uNodeTemplate;
						Serializer.Serializer.Deserialize(tmp.serializedData, owner);
						var comp = owner.GetComponents<uNodeRoot>();
						if(comp.Length > 0) {
							if(onAdded != null)
								onAdded(comp.Last());
						}
					}, t);
				}
			}
			menu.ShowAsContext();
		}

		void RegisterOpenedFile(string path) {
			if(SavedData.lastOpenedFile == null) {
				SavedData.lastOpenedFile = new List<string>();
			}
			if(SavedData.lastOpenedFile.Contains(path)) {
				SavedData.lastOpenedFile.Remove(path);
			}
			SavedData.lastOpenedFile.Add(path);
			SaveOptions();
		}
		#endregion

		#region Others
		/// <summary>
		/// Change the editor target.
		/// </summary>
		/// <param name="data"></param>
		public void ChangeEditorTarget(GraphData data) {
			bool needRefresh = data == null || selectedGraph != data || selectedGraph.selectedData.currentCanvas != data.selectedData.currentCanvas;
			selectedGraph = data;
			OnMainTargetChange();
			if (needRefresh) {
				Refresh();
			}
			UpdatePosition();
		}

		/// <summary>
		/// Change the editor main target.
		/// </summary>
		/// <param name="target"></param>
		public static void ChangeMainTarget(uNodeComponentSystem target, GameObject graph = null, bool forceSelect = false) {
			if (window == null) {
				ShowWindow();
			}
			if (graph == null) {
				if (uNodeEditorUtility.IsPrefab(target)) {
					if(target is uNodeRuntime) {
						return;
					}
					graph = target.gameObject;
					target = LoadTempGraph(target);
				} else {
					graph = GraphUtility.GetOriginalObject(target.gameObject);
				}
			}
			bool isGraph = target as uNodeRoot;
			for (int i = 0; i < window.mainGraph.data.Count; i++) {
				var d = window.mainGraph.data[i];
				if (isGraph ? d.graph == target : d.graph == null && d.graphData == target) {
					if (isGraph ? !d.graph : !d.graphData) {
						window.mainGraph.data.RemoveAt(i);
						i--;
						continue;
					}
					bool needRefresh = window.selectedGraph != window.mainGraph ||
						window.selectedGraph.selectedData.currentCanvas != d.currentCanvas;
					window.mainGraph.selectedData = d;
					if (forceSelect) {
						window.ChangeEditorTarget(window.mainGraph);
					} else {
						window.OnMainTargetChange();
					}
					if (needRefresh) {
						window.Refresh();
					}
					return;
				}
			}
			window.mainGraph.selectedData = new GraphEditorData(target);
			window.mainGraph.graph = graph;
			if (forceSelect) {
				window.ChangeEditorTarget(window.mainGraph);
			} else {
				window.OnMainTargetChange();
			}
			window.Refresh();
		}

		/// <summary>
		/// Change the editor target.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="forceNonMain"></param>
		/// <param name="graph"></param>
		public static void ChangeTarget(uNodeRoot target, bool forceNonMain = false, GameObject graph = null) {
			if(window == null) {
				ShowWindow();
			}
			if (graph == null) {
				if (uNodeEditorUtility.IsPrefab(target)) {
					if(target is uNodeRuntime) {
						return;
					}
					graph = target.gameObject;
					target = LoadTempGraph(target);
				} else {
					graph = GraphUtility.GetOriginalObject(target.gameObject);
				}
			}
			if(!forceNonMain && window.selectedGraph == window.mainGraph) {
				ChangeMainTarget(target);
				window.selectedGraph.graph = graph;
				window.ChangeEditorSelection(window.editorData.graph);
				return;
			}
			foreach(var data in window.graphs) {
				if(data == null)
					continue;
				if(data.owner == target.gameObject || graph != null && data.graph == graph) {
					for(int i = 0; i < data.data.Count; i++) {
						var d = data.data[i];
						if(d.graph == target) {
							if(!d.graph) {
								data.data.RemoveAt(i);
								i--;
								continue;
							}
							window.selectedGraph = data;
							window.selectedGraph.selectedData = d;
							window.OnMainTargetChange();
							window.ChangeEditorSelection(window.editorData.graph);
							window.Refresh();
							return;
						}
					}
					var ED = new GraphEditorData(target);
					data.data.Add(ED);
					data.selectedData = ED;
					window.selectedGraph = data;
					window.OnMainTargetChange();
					window.ChangeEditorSelection(window.editorData.graph);
					window.Refresh();
					return;
				}
			}
			window.graphs.Add(new GraphData() { data = new List<GraphEditorData> { new GraphEditorData(target) } });
			window.selectedGraph = window.graphs.Last();
			window.selectedGraph.graph = graph;
			window.OnMainTargetChange();
			window.ChangeEditorSelection(window.editorData.graph);
			window.Refresh();
		}

		/// <summary>
		/// Change the editor target.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="forceNonMain"></param>
		/// <param name="graph"></param>
		public static void ChangeTarget(uNodeData target, bool forceNonMain = false, GameObject graph = null) {
			if(window == null) {
				ShowWindow();
			}
			if (graph == null) {
				if (uNodeEditorUtility.IsPrefab(target)) {
					graph = target.gameObject;
					target = LoadTempGraph(target);
				} else {
					graph = GraphUtility.GetOriginalObject(target.gameObject);
				}
			}
			if(!forceNonMain && window.selectedGraph == window.mainGraph) {
				ChangeMainTarget(target);
				return;
			}
			foreach(var data in window.graphs) {
				if(data == null)
					continue;
				if(data.owner == target.gameObject || graph != null && data.graph == graph) {
					for(int i = 0; i < data.data.Count; i++) {
						var d = data.data[i];
						if(d.graph == null && d.graphData == target) {
							if(!d.graphData) {
								data.data.RemoveAt(i);
								i--;
								continue;
							}
							window.selectedGraph = data;
							window.selectedGraph.selectedData = d;
							window.OnMainTargetChange();
							window.Refresh();
							return;
						}
					}
					var ED = new GraphEditorData(target);
					data.data.Add(ED);
					data.selectedData = ED;
					window.selectedGraph = data;
					window.OnMainTargetChange();
					window.Refresh();
					return;
				}
			}
			window.graphs.Add(new GraphData() { data = new List<GraphEditorData> { new GraphEditorData(target) } });
			window.selectedGraph = window.graphs.Last();
			window.selectedGraph.graph = graph;
			window.OnMainTargetChange();
			window.Refresh();
		}

		public static void ChangeTarget(INode<uNodeRoot> nodeComponent, bool forceNonMain = false, GameObject graph = null) {
			if(window == null) {
				ShowWindow();
			}
			var target = nodeComponent.GetOwner();
			if (graph == null) {
				if (uNodeEditorUtility.IsPrefab(target)) {
					if(target is uNodeRuntime) {
						return;
					}
					graph = target.gameObject;
					nodeComponent = LoadTempGraphNode(nodeComponent);
					if (nodeComponent == null)
						return;
					target = nodeComponent.GetOwner();
					if (target == null)
						return;
				} else {
					graph = GraphUtility.GetOriginalObject(target.gameObject);
				}
			}
			if(!forceNonMain && window.selectedGraph == window.mainGraph) {
				ChangeMainTarget(target);
				ChangeTarget(nodeComponent, window.mainGraph, window);
				return;
			}
			foreach(var data in window.graphs) {
				if(data == null)
					continue;
				if(data.owner == target.gameObject || graph != null && data.graph == graph) {
					for(int i = 0; i < data.data.Count; i++) {
						var d = data.data[i];
						if(d.graph == null && d.graphData == target) {
							if(!d.graphData) {
								data.data.RemoveAt(i);
								i--;
								continue;
							}
							window.selectedGraph = data;
							window.selectedGraph.selectedData = d;
							ChangeTarget(nodeComponent, data, window);
							window.OnMainTargetChange();
							return;
						}
					}
					var ED = new GraphEditorData(target);
					data.data.Add(ED);
					data.selectedData = ED;
					window.selectedGraph = data;
					ChangeTarget(nodeComponent, data, window);
					window.OnMainTargetChange();
					return;
				}
			}
			window.graphs.Add(new GraphData() { data = new List<GraphEditorData> { new GraphEditorData(target) } });
			window.selectedGraph = window.graphs.Last();
			window.selectedGraph.graph = graph;
			ChangeTarget(nodeComponent, window.selectedGraph, window);
			window.OnMainTargetChange();
		}

		private static void ChangeTarget(INode<uNodeRoot> nodeComponent, GraphData data, uNodeEditor editor) {
			if(nodeComponent != null && data != null) {
				data.selectedData.selected = nodeComponent;
				data.selectedData.selectedNodes.Clear();
				if(nodeComponent is RootObject) {
					data.selectedData.selectedRoot = nodeComponent as RootObject;
				} else if(nodeComponent is Node) {
					data.selectedData.selectedNodes.Add(nodeComponent as Node);
					data.selectedData.selectedRoot = (nodeComponent as Node).rootObject;
					data.selectedData.selectedGroup = (nodeComponent as Node).parentComponent as Node;
					if(nodeComponent is ISuperNode) {
						data.selectedData.selectedGroup = nodeComponent as Node;
					}
				}
				editor.graphEditor.MoveCanvas(data.selectedData.GetPosition(nodeComponent as UnityEngine.Object));
				editor.EditorSelectionChanged();
			}
		}

		private static void OnSelectionChanged(NodeComponent component) {
			if(component == null)
				return;
			if(component.transform.parent != null) {
				if(component.transform.parent.gameObject == window.mainGraph.selectedData.graph.RootObject) {
					window.mainGraph.selectedData.selectedGroup = null;
					window.mainGraph.selectedData.selectedRoot = null;
					window.mainGraph.selectedData.GetPosition(window.mainGraph.selectedData.graph);
				} else {
					RootObject root = uNodeHelper.GetComponentInParent<RootObject>(component.transform);
					if(root != null) {
						window.mainGraph.selectedData.selectedRoot = root;
						window.mainGraph.selectedData.GetPosition(root);
					} else {
						window.mainGraph.selectedData.selectedRoot = null;
					}
					NodeComponent parentComp = component.transform.parent.GetComponent<NodeComponent>();
					if(parentComp is ISuperNode) {
						window.mainGraph.selectedData.selectedGroup = parentComp as Node;
						window.mainGraph.selectedData.GetPosition(parentComp);
					}
				}
			}
			NodeEditorUtility.SelectNode(window.mainGraph, component);
			window.graphEditor.MoveCanvas(new Vector2(component.editorRect.x - 200, component.editorRect.y - 200));
		}

		private static void OnSelectionChanged(RootObject rootObject) {
			if(rootObject != null) {
				if(rootObject.transform.parent != null && window.mainGraph.selectedData.graph != null) {
					if(rootObject.transform.parent.gameObject == window.mainGraph.selectedData.graph.RootObject) {
						NodeEditorUtility.SelectRoot(window.mainGraph, rootObject);
					}
				}
				window.mainGraph.selectedData.GetPosition(rootObject);
			}
		}

		private static GameObject LoadTempGraphObject(UnityEngine.Object graph) {
			GameObject go = null;
			if(graph is GameObject) {
				go = GraphUtility.GetTempGraphObject(graph as GameObject);
			} else if(graph is uNodeRoot) {
				go = GraphUtility.GetTempGraphObject((graph as uNodeRoot).gameObject);
			} else if(graph is INode<uNodeRoot>) {
				go = GraphUtility.GetTempGraphObject((graph as INode<uNodeRoot>).GetOwner().gameObject);
			}
			return go;
		}

		private static T LoadTempGraph<T>(T graph) where T : uNodeComponentSystem {
			var go = GraphUtility.GetTempGraphObject(graph);
			return go;
		}

		private static INode<T> LoadTempGraphNode<T>(INode<T> node) where T : uNodeRoot {
			var go = GraphUtility.GetTempGraphObject(node);
			return go;
		}

		/// <summary>
		/// Change the editor selection.
		/// </summary>
		/// <param name="component"></param>
		/// <param name="forceChange"></param>
		public static void ChangeMainSelection(Component component, bool forceChange = false) {
			if(component == null)
				return;
			ChangeMainSelection(component.gameObject, forceChange);
		}

		/// <summary>
		/// Change the editor selection.
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="forceChange"></param>
		public static void ChangeMainSelection(GameObject gameObject, bool forceChange = false) {
			if(window == null) {
				ShowWindow();
			}
			if(uNodeEditorUtility.IsPrefab(gameObject)) {
				bool flag = gameObject.GetComponent<uNodeRoot>() != null || gameObject.GetComponent<uNodeData>() != null;
				if(flag) {
					if(gameObject.GetComponent<uNodeRuntime>() != null) {
						return;
					}
					window.mainGraph.graph = gameObject;
					gameObject = LoadTempGraphObject(gameObject);
				} else {
					flag = false;
					NodeComponent comp = gameObject.GetComponent<NodeComponent>();
					if(comp != null) {
						flag = true;
						window.mainGraph.graph = comp.owner.gameObject;
						gameObject = LoadTempGraphObject(comp.owner);
					} else {
						RootObject root = gameObject.GetComponent<RootObject>();
						if(root != null) {
							flag = true;
							window.mainGraph.graph = root.owner.gameObject;
							gameObject = LoadTempGraphObject(root.owner);
						}
					}
					if(!flag) {
						window.mainGraph.graph = null;
					}
				}
			} else {
				window.mainGraph.graph = GraphUtility.GetOriginalObject(gameObject.gameObject);
				// if(window.mainGraph.graph is GameObject) {
				// 	gameObject = window.mainGraph.graph as GameObject;
				// }
			}
			window.UpdateMainSelection(gameObject);
			bool needRefresh = window.selectedGraph == window.mainGraph;
			if(forceChange) {
				window.ChangeEditorTarget(window.mainGraph);
			}
			if(!isLocked && window.mainGraph.selectedData.graph != null) {
				NodeComponent comp = gameObject.GetComponent<NodeComponent>();
				if(comp != null) {
					OnSelectionChanged(comp);
					if (forceChange) {
						window.ChangeEditorSelection(comp);
					}
				} else {
					RootObject root = gameObject.GetComponent<RootObject>();
					if(root != null) {
						OnSelectionChanged(root);
						if (forceChange) {
							window.ChangeEditorSelection(root);
						}
					}
				}
			}
			window.OnMainTargetChange();
			if(needRefresh)
				window.Refresh();
		}

		/// <summary>
		/// Called by uNodeInitializer on compiling script.
		/// </summary>
		public static void OnCompiling() {
			if(window != null) {
				window._isCompiling = false;
				window.SaveEditorData();
				window._isCompiling = true;
			}
		}

		/// <summary>
		/// Called by uNodeInitializer on finished compiling script.
		/// </summary>
		public static void OnFinishedCompiling() {
			if(window != null) {
				window._isCompiling = false;
				window.LoadEditorData();
				GUIChanged();
			}
		}

		private void OnMainTargetChange() {
			if(editorData == mainGraph.selectedData) {
				if(editorData.graph != null) {
					UpdatePosition();
					// editorData.selectedNodes.Clear();
				}
			}
		}

		/// <summary>
		/// Refresh uNode Editor
		/// </summary>
		public void Refresh() {
			Refresh(false);
		}

		/// <summary>
		/// Refresh uNode Editor
		/// </summary>
		public void Refresh(bool fullRefresh) {
			graphEditor.window = this;
			editorData.Refresh();
			RefreshDimmedNode();
			graphEditor.ReloadView(fullRefresh);
			GUIChanged();
		}

		/// <summary>
		/// Highlight the node for a second.
		/// </summary>
		/// <param name="node"></param>
		public static void HighlightNode(NodeComponent node) {
			ShowWindow();
			if(uNodeEditorUtility.IsPrefab(node)) {
				node = GraphUtility.GetTempGraphObject(node) as NodeComponent;
			}
			ChangeTarget(node);
			window.Refresh();
			window.graphEditor.HighlightNode(node);
		}

		/// <summary>
		/// Highlight the node from a Script Information data with the given line number and column number
		/// </summary>
		/// <param name="scriptInfo"></param>
		/// <param name="line"></param>
		/// <param name="column"></param>
		/// <returns></returns>
		public static bool HighlightNode(EditorScriptInfo scriptInfo, int line, int column = -1) {
			if(scriptInfo == null) return false;
			if(scriptInfo.informations == null) return false;
			var path = AssetDatabase.GUIDToAssetPath(scriptInfo.guid);
			if(string.IsNullOrEmpty(path)) return false;
			var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
			if(asset is GameObject gameObject) {
				HighlightNode(gameObject, scriptInfo.informations, line, column);
			}
			return false;
		}

		/// <summary>
		/// Highlight the node from a Script Information data with the given line number and column number
		/// </summary>
		/// <param name="informations"></param>
		/// <param name="line"></param>
		/// <param name="column"></param>
		/// <returns></returns>
		public static bool HighlightNode(IEnumerable<ScriptInformation> informations, int line, int column = -1) {
			return HighlightNode(null, informations, line, column);
		}

		/// <summary>
		/// Highlight the node from a Script Information data with the given line number and column number
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="informations"></param>
		/// <param name="line"></param>
		/// <param name="column"></param>
		/// <returns></returns>
		public static bool HighlightNode(GameObject graph, IEnumerable<ScriptInformation> informations, int line, int column = -1) {
			if(informations == null) return false;
			List<ScriptInformation> information = new List<ScriptInformation>();
			foreach(var info in informations) {
				if(info == null) continue;
				if (info.startLine <= line && info.endLine >= line) {
					information.Add(info);
				}
			}
			if(column > 0) {
				information.Sort((x, y) => {
					int result = CompareUtility.Compare(x.lineRange, y.lineRange);
					if(result == 0) {
						int xColumn = int.MaxValue;
						if(x.startColumn <= column && x.endColumn >= column) {
							xColumn = x.columnRange;
						}
						int yColumn = int.MaxValue;
						if(y.startColumn <= column && y.endColumn >= column) {
							yColumn = y.columnRange;
						}
						return CompareUtility.Compare(xColumn, yColumn);
					}
					return result;
				});
			} else {
				information.Sort((x, y) => {
					int result = CompareUtility.Compare(x.lineRange, y.lineRange);
					if(result == 0) {
						return CompareUtility.Compare(y.columnRange, x.columnRange);
					}
					return result;
				});
			}
			foreach(var info in information) {
				if (info != null) {
					// Debug.Log(line + ":" + column);
					// Debug.Log(info.startLine + "-" + info.endLine);
					// Debug.Log(info.startColumn + "-" + info.endColumn);
					// Debug.Log(info.lineRange + ":" + info.columnRange);
					if (int.TryParse(info.id, out var id)) {
						UnityEngine.Object obj;
						if (graph != null) {
							obj = uNodeEditorUtility.FindObjectByUniqueIdentifier(graph.transform, id);
						} else {
							obj = EditorUtility.InstanceIDToObject(id);
						}
						if (obj is GameObject) {
							GameObject go = obj as GameObject;
							obj = go.GetComponent<NodeComponent>();
							if (obj == null) {
								obj = go.GetComponent<RootObject>();
							}
						} else if (obj == null) {
							obj = EditorUtility.InstanceIDToObject(id);
						}
						if (obj is NodeComponent) {
							uNodeEditor.HighlightNode(obj as NodeComponent);
							return true;
						} else if (obj is RootObject) {
							var root = obj as RootObject;
							if (root.startNode != null) {
								uNodeEditor.HighlightNode(root.startNode);
							} else {
								uNodeEditor.ChangeMainSelection(root);
							}
							return true;
						}
					} else if (info.id.StartsWith(CodeGenerator.KEY_INFORMATION_VARIABLE)) {

					}
				}
		}
			return false;
		}

		void RefreshDimmedNode() {
			dimmedNode = new HashSet<NodeComponent>();
			//var nodes = new HashSet<NodeComponent>();
			//if(editorData.selectedGroup is ISuperNode) {
			//	ISuperNode superNode = editorData.selectedGroup as ISuperNode;
			//	foreach(var n in superNode.nestedFlowNodes) {
			//		if(n == null)
			//			continue;
			//		if(!nodes.Contains(n))
			//			nodes.Add(n);
			//		NodeEditorUtility.FindConnectedNode(n, nodes);
			//	}
			//} else if(editorData.selectedRoot) {
			//	if(editorData.selectedRoot.startNode) {
			//		if(!nodes.Contains(editorData.selectedRoot.startNode))
			//			nodes.Add(editorData.selectedRoot.startNode);
			//		NodeEditorUtility.FindConnectedNode(editorData.selectedRoot.startNode, nodes);
			//	}
			//} else if(graphEditor.eventNodes != null) {
			//	foreach(var method in graphEditor.eventNodes) {
			//		if(method == null)
			//			continue;
			//		NodeEditorUtility.FindConnectedNode(method, nodes);
			//	}
			//}
			//foreach(var n in editorData.nodes) {
			//	if(!nodes.Contains(n)) {
			//		dimmedNode.Add(n);
			//	}
			//}
		}

		[Serializable]
		private class TempEditorData {
			public List<GraphData> editorsData;
			public GraphData mainEditorData;
			public int selectionIndex;
		}

		public void SaveEditorData() {
			//if(_isCompiling)
			//	return;
			//TempEditorData temp = new TempEditorData();
			//temp.editorsData = editorsData;
			//temp.mainEditorData = mainEditorData;
			//temp.selectionIndex = _selectedDataIndex;
			//uNodeEditorUtility.SaveEditorData(temp, "EditorTabData", out tempObjects);
			try {
				if(_isCompiling)
					return;
				TempEditorData temp = new TempEditorData();
				temp.editorsData = graphs;
				temp.mainEditorData = mainGraph;
				temp.selectionIndex = _selectedDataIndex;
				string json = JsonUtility.ToJson(temp, false);
				if(_serializedJSON != json) {
					_serializedJSON = json;
					uNodeEditorUtility.SaveEditorData(_serializedJSON, "EditorTabData");
				}
			}
			catch { }
		}

		void LoadEditorData() {
			//if(tempObjects == null || tempObjects.Count == 0)
			//	return;
			//var loadedData = uNodeEditorUtility.LoadEditorData<TempEditorData>("EditorTabData", tempObjects);
			//if(loadedData != null) {
			//	editorsData = loadedData.editorsData;
			//	mainEditorData = loadedData.mainEditorData;
			//	_selectedDataIndex = loadedData.selectionIndex;
			//	ChangeEditorTarget(selectedData);
			//}
			//_isCompiling = false;
			var loadedData = JsonUtility.FromJson<TempEditorData>(!string.IsNullOrEmpty(_serializedJSON) ? _serializedJSON : uNodeEditorUtility.LoadEditorData<string>("EditorTabData"));
			if(loadedData != null) {
				graphs = loadedData.editorsData;
				mainGraph = loadedData.mainEditorData;
				_selectedDataIndex = loadedData.selectionIndex;
				if(!preferenceData.saveGraphPosition) {
					if(graphs != null) {
						for(int i = 0; i < graphs.Count; i++) {
							var g = graphs[i];
							if(g != null && g.data != null && g.owner != null) {
								foreach(var d in g.data) {
									d.ResetPositionData();
								}
							} else {
								graphs.RemoveAt(i);
								i--;
							}
						}
					}
					if(mainGraph != null && mainGraph.data != null) {
						foreach(var d in mainGraph.data) {
							d.ResetPositionData();
						}
					}
				}
				ChangeEditorTarget(selectedGraph);
			}
			_isCompiling = false;
		}

		int _selectedDataIndex;
		[SerializeField]
		string _serializedJSON;
		bool _useDebug, _isCompiling;
		void OnPlaymodeStateChanged(PlayModeStateChange state) {
			switch (state) {
				case PlayModeStateChange.EnteredPlayMode:
					LoadEditorData();
					uNodeUtility.useDebug = _useDebug;
					Refresh();
					break;
				case PlayModeStateChange.EnteredEditMode:
					CustomInspector.Editors.Clear();
					LoadEditorData();
					uNodeUtility.useDebug = _useDebug;
					break;
				case PlayModeStateChange.ExitingEditMode:
				case PlayModeStateChange.ExitingPlayMode:
					_isCompiling = false;
					_useDebug = uNodeUtility.useDebug;
					SaveEditorData();
					break;
			}
		}

		public void UpdatePosition() {
			graphEditor.MoveCanvas(editorData.GetPosition(editorData.currentCanvas));
		}

		private GameObject targetSelection;
		void UpdateMainSelection(GameObject gameObject) {
			if(selectedGraph == mainGraph && mainGraph.selectedData.graph != null && isLocked)
				return;
			if(gameObject != null && targetSelection != gameObject) {
				bool needRefresh = selectedGraph != mainGraph || selectedGraph.selectedData.currentCanvas != mainGraph.selectedData.currentCanvas;
				targetSelection = gameObject;
				uNodeComponentSystem root = null;
				if(gameObject.GetComponent<uNodeComponentSystem>() != null) {
					root = gameObject.GetComponent<uNodeComponentSystem>();
				} else if(gameObject.GetComponent<NodeComponent>() != null) {
					root = gameObject.GetComponent<NodeComponent>().owner;
				} else if(gameObject.GetComponent<RootObject>() != null) {
					root = gameObject.GetComponent<RootObject>().owner;
				} else {
					targetSelection = null;
				}
				if(root != null) {
					if(root is uNodeRoot && mainGraph.selectedData.graph != root) {
						mainGraph.selectedData = null;
						needRefresh = true;
					} else if(root is uNodeData && mainGraph.selectedData.graphData != root) {
						mainGraph.selectedData = null;
						needRefresh = true;
					}
					ChangeMainTarget(root);
				} else {
					mainGraph.selectedData = null;
				}
				if(needRefresh) {
					Refresh();
				}
			}
		}

		void UpdateMainSelection(Component component) {
			if(selectedGraph == mainGraph && mainGraph.selectedData.graph != null && isLocked)
				return;
			if(component != null && targetSelection != component.gameObject) {
				targetSelection = component.gameObject;
				uNodeRoot root = null;
				if(component is uNodeRoot) {
					root = component as uNodeRoot;
				} else if(component is NodeComponent) {
					root = (component as NodeComponent).owner;
				} else if(component is RootObject) {
					root = (component as RootObject).owner;
				} else if(component.GetComponent<uNodeRoot>() != null) {
					root = component.GetComponent<uNodeRoot>();
				} else if(component.GetComponent<NodeComponent>() != null) {
					root = component.GetComponent<NodeComponent>().owner;
				} else if(component.GetComponent<uNodeData>() != null) {
					ChangeMainTarget(component.GetComponent<uNodeData>());
				} else if(component.GetComponent<RootObject>() != null) {
					root = component.GetComponent<RootObject>().owner;
				} else {
					targetSelection = null;
				}
				if(root != null) {
					ChangeMainTarget(root);
				}
			}
		}

		static void UndoRedoCallback() {
			if(window == null)
				return;
			window.Refresh(true);
			window.Repaint();
		}

		public static void ForceRepaint() {
			if(window != null) {
				window.Repaint();
				EditorApplication.RepaintHierarchyWindow();
				GUIChanged();
			}
		}
		#endregion
	}
}