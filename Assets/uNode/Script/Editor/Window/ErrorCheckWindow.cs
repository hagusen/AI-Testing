using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MaxyGames.uNode.Editors {
	public class ErrorCheckWindow : EditorWindow {
		public static ErrorCheckWindow window;
		private Vector2 scrollPos;
		[SerializeField]
		private string selectedID;
		private GUIStyle selectedStyle;
		[SerializeField]
		private bool onlySelectedUNode = true;

		[MenuItem("Tools/uNode/Error Check")]
		public static void Init() {
			window = (ErrorCheckWindow)GetWindow(typeof(ErrorCheckWindow));
			window.titleContent = new GUIContent("Error Check");
			window.minSize = new Vector2(250, 250);
			window.selectedStyle = new GUIStyle(EditorStyles.label);
			window.selectedStyle.normal.textColor = Color.white;
			window.Show();
		}

		void OnGUI() {
			if(window == null) {
				Init();
			}
			var editor = uNodeEditor.window;
			Event currentEvent = Event.current;
			EditorGUI.BeginDisabledGroup(editor == null);
			GUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(100));
			if(GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(50))) {
				uNodeEditor.window.CheckErrors();
			}
			GUILayout.FlexibleSpace();
			onlySelectedUNode = GUILayout.Toggle(onlySelectedUNode, "Only Selected uNode", EditorStyles.toolbarButton);
			GUILayout.EndHorizontal();
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			var errorMap = uNodeEditor.editorErrors;
			if(errorMap != null && editor != null) {
				foreach(var errors in errorMap) {
					if(errors.Value != null && errors.Key != null) {
						if(errors.Key is NodeComponent comp) {
							if(comp == null)
								continue;
							if(onlySelectedUNode && (editor.editorData == null || comp.owner != editor.editorData.graph)) {
								continue;
							}
							string path = GetNodePath(comp, !onlySelectedUNode);
							int index = 0;
							foreach(var error in errors.Value) {
								string id = comp.GetInstanceID() + "_" + index;
								Rect rect = uNodeGUIUtility.GetRect(2);
								var style = EditorStyles.label;
								if(selectedID == id) {
									GUI.DrawTexture(rect, uNodeGUIUtility.Styles.selectedTexture);
									style = selectedStyle;
								}
								if(currentEvent.type == EventType.MouseDown && rect.Contains(currentEvent.mousePosition)) {
									if(currentEvent.button == 1) {
										//GenericMenu menu = new GenericMenu();
										//menu.AddItem(new GUIContent("Highlight Node"), false, () => {
										//	uNodeEditor.HighlightNode(errors.Key);
										//});
										//menu.AddItem(new GUIContent("Select Node"), false, () => {
										//	uNodeEditor.ChangeSelection(errors.Key, true);
										//});
										//menu.ShowAsContext();
									} else if(currentEvent.button == 0) {
										selectedID = id;
										uNodeEditor.HighlightNode(comp);
										if(error.onClicked != null) {
											error.onClicked(GUIUtility.GUIToScreenPoint(currentEvent.mousePosition));
										}
									}
								}
								rect.x += 5;
								rect.width -= 5;
								EditorGUI.LabelField(rect, uNodeEditorUtility.RemoveHTMLTag(error.message) + "\n" + path, style);
								index++;
							}
						} else if(errors.Key is uNodeRoot graph) {
							if(graph == null)
								continue;
							if(onlySelectedUNode && (editor.editorData == null || graph != editor.editorData.graph)) {
								continue;
							}
							int index = 0;
							foreach(var error in errors.Value) {
								if(error.obj == null)
									continue;
								string path = GetNodePath(error.obj, graph, !onlySelectedUNode);
								string id = error.obj.GetInstanceID() + "_" + index;
								Rect rect = uNodeGUIUtility.GetRect(2);
								var style = EditorStyles.label;
								if(selectedID == id) {
									GUI.DrawTexture(rect, uNodeGUIUtility.Styles.selectedTexture);
									style = selectedStyle;
								}
								if(currentEvent.type == EventType.MouseDown && rect.Contains(currentEvent.mousePosition)) {
									if(currentEvent.button == 1) {
										//GenericMenu menu = new GenericMenu();
										//menu.AddItem(new GUIContent("Highlight Node"), false, () => {
										//	uNodeEditor.HighlightNode(errors.Key);
										//});
										//menu.AddItem(new GUIContent("Select Node"), false, () => {
										//	uNodeEditor.ChangeSelection(errors.Key, true);
										//});
										//menu.ShowAsContext();
									} else if(currentEvent.button == 0) {
										selectedID = id;
										if(error.onClicked != null) {
											error.onClicked(GUIUtility.GUIToScreenPoint(currentEvent.mousePosition));
										}
									}
								}
								rect.x += 5;
								rect.width -= 5;
								EditorGUI.LabelField(rect, uNodeEditorUtility.RemoveHTMLTag(error.message) + "\n" + path, style);
								index++;
							}
						}
					}
				}
			}
			EditorGUILayout.EndScrollView();
			EditorGUI.EndDisabledGroup();
			Repaint();
		}

		string GetNodePath(Object obj, uNodeRoot owner, bool fullPath = false) {
			string path = "";
			if(obj != null && owner != null) {
				path = obj.name;
				Transform transform = null;
				if(obj is GameObject) {
					transform = (obj as GameObject).transform;
				} else if(obj is Component) {
					transform = (obj as Component).transform;
				}
				if(transform != null) {
					Transform parent = transform.parent;
					while(parent != owner.transform && parent.parent != null) {
						if(parent.gameObject == owner.RootObject) {
							if(transform.parent == parent) {
								path = path.Insert(0, "StateFlow : ");
							}
							if(fullPath) {
								path = path.Insert(0, owner.DisplayName + " : ");
								path = path.Insert(0, owner.gameObject.name + " : ");
							}
							break;
						}
						NodeComponent nc = parent.GetComponent<NodeComponent>();
						if(nc != null) {
							path = path.Insert(0, nc.GetNodeName() + " : ");
						} else {
							RootObject ro = parent.GetComponent<RootObject>();
							if(ro != null) {
								path = path.Insert(0, ro.Name + " : ");
							} else {
								path = path.Insert(0, parent.gameObject.name + " : ");
							}
						}
						parent = parent.parent;
					}
				}
			}
			return path;
		}

		string GetNodePath(NodeComponent node, bool fullPath = false) {
			string path = "";
			if(node != null && node.owner != null) {
				path = node.GetNodeName();
				Transform parent = node.transform.parent;
				while(parent != node.owner.transform && parent.parent != null) {
					if(parent.gameObject == node.owner.RootObject) {
						if(node.transform.parent == parent) {
							path = path.Insert(0, "StateFlow : ");
						}
						if(fullPath) {
							path = path.Insert(0, node.owner.DisplayName + " : ");
							path = path.Insert(0, node.owner.gameObject.name + " : ");
						}
						break;
					}
					NodeComponent nc = parent.GetComponent<NodeComponent>();
					if(nc != null) {
						path = path.Insert(0, nc.GetNodeName() + " : ");
					} else {
						RootObject ro = parent.GetComponent<RootObject>();
						if(ro != null) {
							path = path.Insert(0, ro.Name + " : ");
						} else {
							path = path.Insert(0, parent.gameObject.name + " : ");
						}
					}
					parent = parent.parent;
				}
			}
			return path;
		}
	}
}