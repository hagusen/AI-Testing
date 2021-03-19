using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

namespace MaxyGames.uNode.Editors {
	public static class GraphUtility {
		internal static class TempGraphManager {
			private static GameObject _managerObject;
			public static GameObject managerObject {
				get {
					if(_managerObject == null) {
						var go = GameObject.Find("[uNode_Temp_GraphManager]");
						if(go == null) {
							go = new GameObject("[uNode_Temp_GraphManager]");
							//go.SetActive(false);
						}
						_managerObject = go;
						OnSaved();
					}
					return _managerObject;
				}
			}

			internal static void OnSaving() {
				if(managerObject == null)
					return;
				if(managerObject != null) {
					managerObject.hideFlags = HideFlags.HideAndDontSave;
				}
			}

			internal static void OnSaved() {
				if(managerObject == null)
					return;
#if UNODE_DEBUG
				managerObject.hideFlags = HideFlags.None;
#else
				managerObject.hideFlags = HideFlags.HideInHierarchy;
#endif
			}
		}

		public const string KEY_TEMP_OBJECT = "[uNode_Temp_";

		private static List<GraphSystemAttribute> _graphSystems;
		/// <summary>
		/// Find all graph system attributes.
		/// </summary>
		/// <returns></returns>
		public static List<GraphSystemAttribute> FindGraphSystemAttributes() {
			if(_graphSystems == null) {
				_graphSystems = new List<GraphSystemAttribute>();
				foreach(var assembly in EditorReflectionUtility.GetAssemblies()) {
					try {
						foreach(System.Type type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
							if(type.IsDefined(typeof(GraphSystemAttribute), false)) {
								var menuItem = (GraphSystemAttribute)type.GetCustomAttributes(typeof(GraphSystemAttribute), false)[0];
								menuItem.type = type;
								_graphSystems.Add(menuItem);
							}
						}
					}
					catch { continue; }
				}
				_graphSystems.Sort((x, y) => CompareUtility.Compare(x.menu, x.order, y.menu, y.order));
			}
			return _graphSystems;
		}

		private static List<GraphConverter> _graphConverters;
		/// <summary>
		/// Find all available graph converters
		/// </summary>
		/// <returns></returns>
		public static List<GraphConverter> FindGraphConverters() {
			if(_graphConverters == null) {
				_graphConverters = new List<GraphConverter>();
				foreach(var assembly in EditorReflectionUtility.GetAssemblies()) {
					try {
						foreach(System.Type type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
							if(!type.IsAbstract && type.IsSubclassOf(typeof(GraphConverter))) {
								var converter = System.Activator.CreateInstance(type, true);
								_graphConverters.Add(converter as GraphConverter);
							}
						}
					}
					catch { continue; }
				}
				_graphConverters.Sort((x, y) => Comparer<int>.Default.Compare(x.order, y.order));
			}
			return _graphConverters;
		}

		/// <summary>
		/// Get a graph system from a type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static GraphSystemAttribute GetGraphSystem(System.Type type) {
			var graphs = FindGraphSystemAttributes();
			return graphs.FirstOrDefault((g) => g.type == type);
		}

		/// <summary>
		/// Get a graph system from a graph object
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static GraphSystemAttribute GetGraphSystem(uNodeRoot graph) {
			if(graph == null)
				return null;
			return GetGraphSystem(graph.GetType());
		}

		/// <summary>
		/// Save All temporary graph objects into prefabs
		/// </summary>
		/// <param name="destroyRoot"></param>
		public static void SaveAllGraph(bool destroyRoot = true) {
			if(TempGraphManager.managerObject.transform.childCount > 0) {
				foreach(Transform tr in TempGraphManager.managerObject.transform) {
					if(tr.childCount == 0)
						continue;
					int id;
					if(int.TryParse(tr.gameObject.name, out id)) {
						var graph = EditorUtility.InstanceIDToObject(id) as GameObject;
						if(graph != null) {
							SaveGraphAsset(graph, tr.GetChild(0).gameObject);
						}
					}
				}
			}
			if(destroyRoot) {
				Object.DestroyImmediate(TempGraphManager.managerObject);
			}
		}

		public static GameObject GetTempManager() {
			return TempGraphManager.managerObject;
		}

		/// <summary>
		/// Destroy all the temporary graphs
		/// </summary>
		public static void DestroyTempGraph() {
			if(TempGraphManager.managerObject)
				Object.DestroyImmediate(TempGraphManager.managerObject);
		}

		public static void PreventDestroyOnPlayMode() {
			if(Application.isPlaying)
				Object.DontDestroyOnLoad(TempGraphManager.managerObject);
		}

		// static bool hasCompilingInit;
		internal static void Initialize() {
			// EditorBinding.onFinishCompiling += () => {
			// 	if(!hasCompilingInit) {
			// 		SaveAllGraph(false);
			// 		hasCompilingInit = true;
			// 	}
			// };
			CodeGenerator.OnSuccessGeneratingGraph += (generatedData, settings) => {
				if(settings.isPreview)
					return;
				foreach(var graph in generatedData.graphs) {
					if(generatedData.classNames.TryGetValue(graph, out var className)) {
						graph.graphData.typeName = settings.nameSpace.Add(".") + className;
						uNodeEditorUtility.MarkDirty(graph);//this will ensure the graph will be saved
						if(HasTempGraphObject(graph.gameObject)) {
							var tempGraph = GraphUtility.GetTempGraphObject(graph);
							if(tempGraph != null) {
								tempGraph.graphData.typeName = graph.graphData.typeName;
							}
						}
						// if (!settings.isAsync) { // Skip on generating in background
						// 	graph.graphData.lastCompiled = UnityEngine.Random.Range(1, int.MaxValue);
						// 	graph.graphData.lastSaved = graph.graphData.lastCompiled;
						// }
					}
				}
			};
			EditorBinding.onSceneSaving += (UnityEngine.SceneManagement.Scene scene, string path) => {
				//Save all graph.
				SaveAllGraph(false);
				TempGraphManager.OnSaving();
			};
			EditorBinding.onSceneSaved += (UnityEngine.SceneManagement.Scene scene) => {
				//After scene is saved, back the temp graph flag to hide in hierarchy.
				// TempGraphManager.managerObject.hideFlags = HideFlags.HideInHierarchy;
				uNodeUtility.TempManagement.DestroyTempObjets();
				TempGraphManager.OnSaved();
			};
			EditorBinding.onSceneClosing += (UnityEngine.SceneManagement.Scene scene, bool removingScene) => {
				SaveAllGraph();
				uNodeUtility.TempManagement.DestroyTempObjets();
			};
			EditorBinding.onSceneOpening += (string path, UnityEditor.SceneManagement.OpenSceneMode mode) => {
				SaveAllGraph();
				uNodeUtility.TempManagement.DestroyTempObjets();
			};
			EditorBinding.onSceneOpened += (UnityEngine.SceneManagement.Scene scene, UnityEditor.SceneManagement.OpenSceneMode mode) => {
				DestroyTempGraph();
				uNodeUtility.TempManagement.DestroyTempObjets();
			};
			EditorApplication.quitting += () => {
				SaveAllGraph(true);
				uNodeUtility.TempManagement.DestroyTempObjets();
				uNodeEditorUtility.SaveEditorData("", "EditorTabData");
			};
		}

		/// <summary>
		/// Find the original object from the temporary object
		/// </summary>
		/// <param name="tempObject"></param>
		/// <returns></returns>
		public static GameObject GetOriginalObject(GameObject tempObject) {
			TemporaryGraph temp = uNodeHelper.GetComponentInParent<TemporaryGraph>(tempObject);
			if(temp != null) {
				return temp.prefab;
			}
			return null;
		}

		/// <summary>
		/// Find the original object from the temporary object
		/// </summary>
		/// <param name="tempObject"></param>
		/// <param name="root"></param>
		/// <returns></returns>
		public static GameObject GetOriginalObject(GameObject tempObject, out Transform root) {
			TemporaryGraph temp = uNodeHelper.GetComponentInParent<TemporaryGraph>(tempObject);
			if(temp != null) {
				root = temp.transform.GetChild(0);
				return temp.prefab;
			}
			root = null;
			return null;
		}

		/// <summary>
		/// Is the graph is temporary object?
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static bool IsTempGraphObject(GameObject graph) {
			return graph != null && graph.transform.root == TempGraphManager.managerObject.transform;
		}

		/// <summary>
		/// Are the graph has temporary objects?
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static bool HasTempGraphObject(GameObject graph) {
			if(graph != null && uNodeEditorUtility.IsPrefab(graph)) {
				var tr = TempGraphManager.managerObject.transform.Find(graph.GetInstanceID().ToString());
				return tr != null && tr.childCount == 1;
			}
			return false;
		}

		/// <summary>
		/// Destroy the temporary graph objects
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static bool DestroyTempGraphObject(GameObject graph) {
			if(graph != null && uNodeEditorUtility.IsPrefab(graph)) {
				var tr = TempGraphManager.managerObject.transform.Find(graph.GetInstanceID().ToString());
				if(tr != null && tr.childCount == 1) {
					Object.DestroyImmediate(tr.gameObject);
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Get the temporary graph object and create it if not have
		/// </summary>
		/// <param name="graph"></param>
		/// <returns></returns>
		public static GameObject GetTempGraphObject(GameObject graph) {
			if(graph != null && uNodeEditorUtility.IsPrefab(graph)) {
				var tr = TempGraphManager.managerObject.transform.Find(graph.GetInstanceID().ToString());
				if(tr == null) {
					tr = new GameObject(graph.GetInstanceID().ToString()).transform;
					tr.SetParent(TempGraphManager.managerObject.transform);
				}
				if(tr.childCount == 1) {
					return tr.GetChild(0).gameObject;
				}
				GameObject go = PrefabUtility.InstantiatePrefab(graph, tr) as GameObject;
				go.SetActive(false);
				tr.gameObject.AddComponent<TemporaryGraph>().prefab = graph;
				return go;
			}
			return null;
		}

		/// <summary>
		/// Get the temporary graph object and create it if not have
		/// </summary>
		/// <param name="graph"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T GetTempGraphObject<T>(T graph) where T : uNodeComponentSystem {
			if(graph != null && uNodeEditorUtility.IsPrefab(graph.gameObject)) {
				var temp = GetTempGraphObject(graph.gameObject);
				var comps = temp.GetComponents<T>();
				if(comps.Length == 1) {
					return comps[0];
				}
				for(int i = 0; i < comps.Length; i++) {
					var correspondingObj = uNodeEditorUtility.GetComponentSource(comps[i], graph.gameObject);
					if(correspondingObj != null && correspondingObj == graph) {
						return correspondingObj;
					}
				}
				return comps.Length > 0 ? comps[0] : null;
			}
			return null;
		}

		/// <summary>
		/// Get the temporary graph object and create it if not have
		/// </summary>
		/// <param name="node"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static INode<T> GetTempGraphObject<T>(INode<T> node) where T : uNodeRoot {
			if(node != null && uNodeEditorUtility.IsPrefab(node.GetOwner())) {
				var temp = GetTempGraphObject(node.GetOwner().gameObject);
				var nodeComp = node as Component;
				var obj = uNodeEditorUtility.GetPrefabTransform(nodeComp.transform, node.GetOwner().transform, temp.transform);
				return obj.GetComponent<uNodeComponent>() as INode<T>;
			}
			return null;
		}

		/// <summary>
		/// Get the temporary variable from the graph object
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static VariableData GetTempGraphVariable(string variable, uNodeRoot owner) {
			if(uNodeEditorUtility.IsPrefab(owner.gameObject)) {
				var graph = GetTempGraphObject(owner);
				if(graph == null)
					return null;
				return graph.GetVariableData(variable);
			}
			return null;
		}

		/// <summary>
		/// Save the graph into prefab.
		/// Note: this only work on not in play mode as it for auto save.
		/// </summary>
		/// <param name="graphAsset"></param>
		/// <param name="graph"></param>
		public static void AutoSaveGraph(GameObject graph) {
			if(Application.isPlaying)
				return;
			// EditorUtility.DisplayProgressBar("Saving", "Saving graph assets.", 1);
			SaveGraph(graph);
			// EditorUtility.ClearProgressBar();
		}

		/// <summary>
		/// Save the runtime graph to a prefab
		/// </summary>
		/// <param name="runtimeGraph"></param>
		/// <param name="graphAsset"></param>
		public static void SaveRuntimeGraph(uNodeRuntime runtimeGraph) {
			if(!Application.isPlaying)
				throw new System.Exception("Saving runtime graph can only be done in playmode");
			if(runtimeGraph.originalGraph == null)
				throw new System.Exception("Cannot save runtime graph because the original graph was null / missing");
			var graph = runtimeGraph.originalGraph;
			if(!EditorUtility.IsPersistent(graph))
				throw new System.Exception("Cannot save graph to unpersistent asset");
			var prefabContent = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(graph));
			var originalGraph = uNodeHelper.GetGraphComponent(prefabContent, graph.GraphName);
			if(originalGraph != null) {
				if(runtimeGraph.RootObject != null) {
					//Duplicate graph data
					var tempRoot = Object.Instantiate(runtimeGraph.RootObject);
					tempRoot.name = "Root";
					//Move graph data to original graph
					tempRoot.transform.SetParent(originalGraph.transform);
					//Retarget graph data owner
					AnalizerUtility.RetargetNodeOwner(runtimeGraph, originalGraph, tempRoot.GetComponentsInChildren<MonoBehaviour>(true));
					if(originalGraph.RootObject != null) {
						//Destroy old graph data
						Object.DestroyImmediate(originalGraph.RootObject);
					}
					//Update graph data to new
					originalGraph.RootObject = tempRoot;
					//Save the graph to prefab
					uNodeEditorUtility.SavePrefabAsset(prefabContent, graph);
					//GraphUtility.DestroyTempGraphObject(originalGraph.gameObject);

					//This will update the original graph
					GraphUtility.DestroyTempGraphObject(graph.gameObject);
					//Refresh uNode Editor window
					uNodeEditor.window?.Refresh();
				}
			} else {
				Debug.LogError("Cannot save instanced graph because the cannot find original graph with id:" + graph.GraphName);
			}
			PrefabUtility.UnloadPrefabContents(prefabContent);
		}

		public static void SaveGraph(GameObject graph) {
			if(IsTempGraphObject(graph)) {
				SaveTemporaryGraph(graph);
			} else {
				uNodeEditorUtility.MarkDirty(graph);
			}
		}

		/// <summary>
		/// Save the temporary graph object into the original prefab
		/// </summary>
		/// <param name="graph"></param>
		public static void SaveTemporaryGraph(GameObject graph) {
			var asset = GraphUtility.GetOriginalObject(graph);
			if(asset != null) {
				SaveGraphAsset(asset, graph);
			} else {
				Debug.Log("Cannot save temporary graph: " + graph.name + " because the original graph cannot be found.");
			}
		}

		/// <summary>
		/// Save the temporary graph object into the original prefab
		/// </summary>
		/// <param name="graphAsset"></param>
		/// <param name="graph"></param>
		public static void SaveGraphAsset(GameObject graphAsset, GameObject graph) {
			if(graph != null && graphAsset != null) {
				uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(graph) as GameObject);
				if(graphAsset.name != graph.name) { //Ensure the name is same.
					graph.name = graphAsset.name;
				}
				{//Reset cache data
					var roots = (graphAsset as GameObject).GetComponents<uNodeRoot>();
					var tempRoots = (graph as GameObject).GetComponents<uNodeRoot>();
					if(roots.Length != tempRoots.Length) {
						UGraphView.ClearCache();
					} else {
						for(int i = 0; i < roots.Length; i++) {
							if(roots[i].Name != tempRoots[i].Name) {
								UGraphView.ClearCache();
								break;
							}
						}
					}
				}
				uNodeEditorUtility.SavePrefabAsset(graph, graphAsset);
				var graphs = (graphAsset as GameObject).GetComponents<uNodeRoot>();
				//Reset the cache data
				foreach(var r in graphs) {
					if(r == null)
						continue;
					var rType = ReflectionUtils.GetRuntimeType(r);
					if(rType is RuntimeGraphType graphType) {
						graphType.RebuildMembers();
					}
				}
			}
		}
	}
}