using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using NodeView = UnityEditor.Experimental.GraphView.Node;
using Object = UnityEngine.Object;

namespace MaxyGames.uNode.Editors {
	public class UGraphView : GraphView, IDisposable {
		#region Fields
		public UIElementGraph graph;
		public MinimapView miniMap;

		public List<UNodeView> nodeViews = new List<UNodeView>();
		public Dictionary<TransitionEvent, TransitionView> transitionViewMaps = new Dictionary<TransitionEvent, TransitionView>(EqualityComparer<TransitionEvent>.Default);
		public Dictionary<NodeComponent, UNodeView> nodeViewsPerNode = new Dictionary<NodeComponent, UNodeView>(EqualityComparer<NodeComponent>.Default);
		public List<EdgeView> edgeViews = new List<EdgeView>();

		public Dictionary<uNodeComponent, UNodeView> cachedNodeMap = new Dictionary<uNodeComponent, UNodeView>(EqualityComparer<uNodeComponent>.Default);

		public static readonly bool autoHideNodes = true;

		public GraphEditorData editorData => graph.editorData;
		public uNodeEditor window => graph.window;

		private GraphDragger graphDragger = new GraphDragger();
		private GridBackground gridBackground;
		#endregion

		public const string SelfPortID = "[self]";

		#region Initialization
		public UGraphView() {
			//Add(new MiniMap());
			graphViewChanged = GraphViewChangedCallback;
			viewTransformChanged = ViewTransformChangedCallback;
			elementResized = ElementResizedCallback;
			nodeCreationRequest = OnCreateNode;
			unserializeAndPaste += (op, data) => {
				if(op == "Paste") {
					graph.Repaint();
					var clickedPos = GetMousePosition(graph.topMousePos);
					graph.PasteNode(clickedPos);
					graph.Refresh();
				}
			};

			InitializeManipulators();
			RegisterCallback<ExecuteCommandEvent>(ExecuteCommand);
			RegisterCallback<KeyDownEvent>(KeyDownCallback);
			RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
			RegisterCallback<DragPerformEvent>(OnDragPerformEvent);

			SetupZoom(0.05f, 4f);
			uNodeGUIUtility.onGUIChanged += GUIChanged;
			this.StretchToParentSize();
			if(autoHideNodes)
				Add(new AutoHideGraphElement(this));
		}

		protected virtual void InitializeManipulators() {
			if(uNodeUtility.isOSXPlatform) {
				this.AddManipulator(new ContentDragger());
			} else {
				this.AddManipulator(graphDragger);
			}
			// this.AddManipulator(new ClickSelector());
			this.AddManipulator(new SelectionDragger());
			this.AddManipulator(new RectangleSelector());
			this.AddManipulator(new FreehandSelector());
		}

		protected void GUIChanged(UnityEngine.Object obj) {
			if(obj is uNodeComponent) {
				var comp = obj as uNodeComponent;
				if(cachedNodeMap.TryGetValue(comp, out var node)) {
					if(nodeViews.Contains(node)) {
						//Ensure to reload node view when the GUI changed
						if(comp is NodeComponent) {
							MarkRepaint(comp as NodeComponent);
						} else if(comp is TransitionEvent) {
							MarkRepaint(comp as TransitionEvent);
						}
					} else {
						//This will ensure the node will create up to date views when the GUI changed.
						nodeViews.Remove(node);
						cachedNodeMap.Remove(comp);
					}
				}
			}
		}

		void IDisposable.Dispose() {
			uNodeGUIUtility.onGUIChanged -= GUIChanged;
			cachedNodeMap.Clear();
		}

		public Event currentEvent;
		public void IMGUIEvent(Event @event) {
			currentEvent = @event;
			switch(@event.type) {
				case EventType.DragUpdated:
					OnDragUpdatedEvent(MouseEventBase<DragUpdatedEvent>.GetPooled(@event));
					break;
				case EventType.DragPerform:
					OnDragPerformEvent(MouseEventBase<DragPerformEvent>.GetPooled(@event));
					break;
			}
		}

		public void Initialize(UIElementGraph graph) {
			this.graph = graph;

			ToggleMinimap(UIElementUtility.Theme.enableMinimap);
			if(gridBackground == null && uNodePreference.GetPreference().showGrid) {
				var grid = new GridBackground();
				Insert(0, grid);
				grid.StretchToParentSize();
				grid.style.left = 1;
				gridBackground = grid;
			} else if(gridBackground != null) {
				gridBackground.RemoveFromHierarchy();
				gridBackground = null;
			}
			UpdatePosition();
			MarkRepaint(false);
		}

		public void UpdatePosition() {
			UpdateViewTransform(editorData.position * -1, new Vector3(1, 1, 1));
			SetZoomScale(graph.zoomScale);
		}

		void InitializeNodeViews(bool reloadExistingNodes) {
			graph.RefreshEventNodes();
			// if(!reloadExistingNodes && _needReloadedViews.Count > 0) {
			// 	reloadExistingNodes = true;
			// }
			float count = (graph.eventNodes != null ? graph.eventNodes.Count : 0) + (graph.nodes != null ? graph.nodes.Count : 0);
			float currentCount = 0;
			if(graph.eventNodes != null) {
				foreach(var node in graph.eventNodes) {
					if(node != null) {
						try {
							RepaintNode(node, reloadExistingNodes, currentCount / count);
						}
						catch(Exception ex) {
							Debug.LogException(ex, node.owner);
							uNodeDebug.LogException(ex, node);
						}
						currentCount++;
					}
				}
			}
			if(graph.nodes != null) {
				foreach(var node in graph.nodes) {
					if(node != null) {
						try {
							RepaintNode(node, reloadExistingNodes, currentCount / count);
						}
						catch(Exception ex) {
							Debug.LogException(ex, node.owner);
							uNodeDebug.LogException(ex, node);
						}
						currentCount++;
					}
				}
			}
			if(graph.regions != null) {
				foreach(var region in graph.regions) {
					if(region != null) {
						try {
							RepaintNode(region, reloadExistingNodes);
						}
						catch(Exception ex) {
							Debug.LogException(ex, region.owner);
							uNodeDebug.LogException(ex, region);
						}
					}
				}
			}
			_needReloadedViews.Clear();
		}

		void RepaintNode(NodeComponent node, bool fullReload, float progress = -1) {
			if(cachedNodeMap.TryGetValue(node, out var view) && view != null && view.owner == this) {
				try {
					if(!nodeViewsPerNode.ContainsKey(node)) {
						//Ensure to add element when the node is not in the current scope
						AddElement(view);
						nodeViews.Add(view);
						nodeViewsPerNode[node] = view;
					}
					if(view is IRefreshable) {
						(view as IRefreshable).Refresh();
					}
					if(fullReload || _needReloadedViews.Contains(view) || view.autoReload) {
						if(progress >= 0)
							EditorUtility.DisplayProgressBar("Loading graph", "Initialize Node", progress);
						view.ReloadView();
						view.MarkDirtyRepaint();
					} else {
						view.expanded = view.targetNode.nodeExpanded;
					}
				}
				catch(Exception ex) {
					Debug.LogException(ex, node);
					if(!nodeViewsPerNode.ContainsKey(node)) {
						if(progress >= 0)
							EditorUtility.DisplayProgressBar("Loading graph", "Initialize Node", progress);
						//Add a view using default settings
						AddNodeView(node);
					}
				}
			} else {
				if(progress >= 0)
					EditorUtility.DisplayProgressBar("Loading graph", "Initialize Node", progress);
				AddNodeView(node);
			}
		}

		void InitializeEdgeViews() {
			foreach(var nodeView in nodeViews) {
				if(nodeView.targetNode == null)
					continue;
				foreach(var port in nodeView.inputPorts) {
					if(!port.enabledSelf)
						continue;
					var portValue = port.portData?.GetPortValue();
					if(portValue is MemberData) {
						MemberData member = portValue as MemberData;
						if(member.IsTargetingPinOrNode) {
							var edgeView = new EdgeView(port, PortUtility.GetOutputPort(member, this));
							if(edgeView.input != null && edgeView.output != null) {
								Connect(edgeView, false);
							}
						}
					}
				}
				foreach(var port in nodeView.outputPorts) {
					if(port.orientation != Orientation.Vertical || !port.enabledSelf)
						continue;
					var portValue = port.portData?.GetPortValue();
					if(portValue is NodeComponent) {
						var edgeView = new EdgeView(port, PortUtility.GetSelfPort(portValue as NodeComponent, port.orientation, this));
						if(edgeView.input != null && edgeView.output != null) {
							Connect(edgeView, false);
						}
					} else if(portValue is MemberData) {
						MemberData member = portValue as MemberData;
						if(member.IsTargetingPinOrNode) {
							var edgeView = new EdgeView(PortUtility.GetInputPort(member, this), port);
							if(edgeView.input != null && edgeView.output != null) {
								Connect(edgeView, false);
							}
						}
					}
				}
				nodeView.InitializeEdge();
			}
			foreach(var pair in transitionViewMaps) {
				var transition = pair.Key;
				if(transition != null && transition.GetTargetNode() != null) {
					var edgeView = new EdgeView(PortUtility.GetInputPort(transition.target, this), pair.Value.output);
					if(edgeView.input != null && edgeView.output != null) {
						Connect(edgeView, false);
					}
				}
				pair.Value.InitializeEdge();
			}
		}
		#endregion

		#region Drag & Drop
		private void OnDragUpdatedEvent(DragUpdatedEvent evt) {
			if(DragAndDrop.GetGenericData("uNode") != null ||
				DragAndDrop.visualMode == DragAndDropVisualMode.None &&
				DragAndDrop.objectReferences.Length > 0) {

				if(!uNodeEditorUtility.IsSceneObject(editorData.owner)) {
					if(DragAndDrop.GetGenericData("uNode") != null) {
						var generic = DragAndDrop.GetGenericData("uNode");
						if(generic is UnityEngine.Object && uNodeEditorUtility.IsSceneObject(generic as UnityEngine.Object)) {
							DragAndDrop.visualMode = DragAndDropVisualMode.None;
							return;
						}
					} else if(DragAndDrop.objectReferences.Length > 0) {
						if(uNodeEditorUtility.IsSceneObject(DragAndDrop.objectReferences[0])) {
							DragAndDrop.visualMode = DragAndDropVisualMode.None;
							return;
						}
					}
				}
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			}
		}

		#region Drag Handler
		private void DragHandleVariable(UnityEngine.Object owner, VariableData variable, Vector2 position) {
			if(owner is RootObject) {
				if((owner as RootObject).owner != editorData.graph) {
					EditorUtility.DisplayDialog("Error", "The graph of the variable must same with the current graph", "Ok");
					return;
				}
			} else if(owner != editorData.graph) {
				EditorUtility.DisplayDialog("Error", "The graph of the variable must same with the current graph", "Ok");
				return;
			}
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Get"), false, (() => {
				NodeEditorUtility.AddNewNode(editorData, variable.Name, null, position, delegate (MultipurposeNode n) {
					var mData = MemberData.CreateFromValue(variable, owner);
					n.target.target = mData;
					MemberDataUtility.UpdateMultipurposeMember(n.target);
				});
				graph.Refresh();
			}));
			menu.AddItem(new GUIContent("Set"), false, (() => {
				NodeEditorUtility.AddNewNode(editorData, variable.Name, null, position, delegate (NodeSetValue n) {
					var mData = MemberData.CreateFromValue(variable, owner);
					n.target = mData;
					if(mData.type != null) {
						if(ReflectionUtils.CanCreateInstance(mData.type)) {
							n.value = new MemberData(ReflectionUtils.CreateInstance(mData.type));
						} else {
							n.value = MemberData.CreateFromValue(null, mData.type);
						}
					}
				});
				graph.Refresh();
			}));
			menu.ShowAsContext();
		}

		private void DragHandleObject(Object obj, Vector2 position, Vector2 menuPosition) {
			if(obj is GameObject go) {
				var ucomp = go.GetComponent<uNodeComponentSystem>();
				if(ucomp != null) {
					//Create Liked macro from dragable macros.
					if(ucomp is uNodeMacro) {
						uNodeMacro macro = go.GetComponent<uNodeMacro>();
						CreateLinkedMacro(macro, position);
						return;
					}
					if(ucomp is IIndependentGraph) {
						obj = ucomp;
						DragHandleType(ReflectionUtils.GetRuntimeType(ucomp), position, menuPosition);
						return;
					} else {
						var comps = go.GetComponents<uNodeRoot>();
						GenericMenu cmenu = new GenericMenu();
						for(int i = 0; i < comps.Length; i++) {
							var graph = comps[i];
							cmenu.AddItem(new GUIContent(graph.DisplayName), false, () => {
								var type = graph.GeneratedTypeName.ToType(false);
								if(type != null) {
									DragHandleType(type, position, menuPosition);
								} else {
									uNodeEditorUtility.DisplayErrorMessage($"Type: {graph.GeneratedTypeName} not found, compile the graph to c# should fix it.");
								}
							});
						}
						cmenu.ShowAsContext();
						return;
					}
				}
			}
			if(!(editorData.graph is IIndependentGraph)) {
				EditorUtility.DisplayDialog("Error", "The c# graph cannot reference project and scene object.", "Ok");
				return;
			} else if(!EditorUtility.IsPersistent(obj) && !uNodeEditorUtility.IsSceneObject(editorData.graph)) {
				EditorUtility.DisplayDialog("Error", "The project graph cannot reference scene object.", "Ok");
				return;
			}
			GenericMenu menu = new GenericMenu();

			#region Dragged Action
			Action<UnityEngine.Object, string> action = (dOBJ, startName) => {
				menu.AddItem(new GUIContent(startName + "Get"), false, () => {
					FilterAttribute filter = new FilterAttribute();
					filter.MaxMethodParam = int.MaxValue;
					filter.VoidType = true;
					filter.Public = true;
					filter.Instance = true;
					filter.Static = false;
					filter.DisplayDefaultStaticType = false;
					var type = dOBJ.GetType();
					if(dOBJ is uNodeAssetInstance asset) {
						type = ReflectionUtils.GetRuntimeType(asset.target);
					} else if(dOBJ is uNodeInterface iface) {
						type = ReflectionUtils.GetRuntimeType(iface);
					} else if(dOBJ is uNodeRoot graph) {
						type = ReflectionUtils.GetRuntimeType(graph);
					}
					string category = type.PrettyName();
					var customItems = ItemSelector.MakeCustomItems(type, filter, category, "Inherit Members");
					if(customItems != null) {
						if(!(dOBJ is uNodeInterface)) {
							customItems.Insert(0, new ItemSelector.CustomItem("this", () => {
								var value = new MemberData(dOBJ, MemberData.TargetType.Values);
								value.startType = type;
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData, null, null, position, delegate (MultipurposeNode n) {
									if(n.target == null) {
										n.target = new MultipurposeMember();
									}
									n.target.target = value;
									MemberDataUtility.UpdateMultipurposeMember(n.target);
								});
								graph.Refresh();
							}, category));
						}
						ItemSelector w = ItemSelector.ShowWindow(dOBJ, MemberData.none, filter, delegate (MemberData value) {
							if(dOBJ is uNodeInterface) {
								dOBJ = null;//Will make the instance null for graph interface
							}
							var mData = new MemberData(dOBJ, MemberData.TargetType.Values);
							mData.startType = type;
							value.startType = type;
							value.instance = mData;
							NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData, null, null, position, delegate (MultipurposeNode n) {
								if(n.target == null) {
									n.target = new MultipurposeMember();
								}
								n.target.target = value;
								MemberDataUtility.UpdateMultipurposeMember(n.target);
							});
							graph.Refresh();
						}, customItems).ChangePosition(menuPosition);
						w.displayDefaultItem = false;
					}
				});
				menu.AddItem(new GUIContent(startName + "Set"), false, () => {
					FilterAttribute filter = new FilterAttribute();
					filter.SetMember = true;
					filter.MaxMethodParam = int.MaxValue;
					//filter.VoidType = true;
					filter.Public = true;
					filter.Instance = true;
					filter.Static = false;
					filter.DisplayDefaultStaticType = false;
					var type = dOBJ.GetType();
					if(dOBJ is uNodeAssetInstance asset) {
						type = ReflectionUtils.GetRuntimeType(asset.target);
					} else if(dOBJ is uNodeInterface iface) {
						type = ReflectionUtils.GetRuntimeType(iface);
					} else if(dOBJ is uNodeRoot graph) {
						type = ReflectionUtils.GetRuntimeType(graph);
					}
					var customItems = ItemSelector.MakeCustomItems(type, filter, type.PrettyName(), "Inherit Members");
					if(customItems != null) {
						ItemSelector w = ItemSelector.ShowWindow(dOBJ, MemberData.none, filter, delegate (MemberData value) {
							if(dOBJ is uNodeInterface) {
								dOBJ = null;//Will make the instance null for graph interface
							}
							value.instance = dOBJ;
							value.startType = type;
							NodeEditorUtility.AddNewNode<NodeSetValue>(editorData, null, null, position, delegate (NodeSetValue n) {
								n.target = value;
							});
							graph.Refresh();
						}, customItems).ChangePosition(menuPosition);
						w.displayDefaultItem = false;
					}
				});
			};
			#endregion

			action(obj, "");
			if(obj is GameObject) {
				menu.AddSeparator("");
				foreach(var comp in (obj as GameObject).GetComponents<Component>()) {
					action(comp, comp.GetType().Name + "/");
				}
			} else if(obj is Component) {
				menu.AddSeparator("");
				foreach(var comp in (obj as Component).GetComponents<Component>()) {
					action(comp, comp.GetType().Name + "/");
				}
			}
			menu.ShowAsContext();
		}

		private void DragHandleProperty(uNodeProperty property, Vector2 position) {
			if(property.owner != editorData.graph) {
				EditorUtility.DisplayDialog("Error", "The graph of the property must same with the current graph", "Ok");
				return;
			}
			GenericMenu menu = new GenericMenu();
			if(property.CanGetValue()) {
				menu.AddItem(new GUIContent("Get"), false, () => {
					NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData, property.Name, null, position, delegate (MultipurposeNode n) {
						var mData = MemberData.CreateFromValue(property);
						n.target.target = mData;
						MemberDataUtility.UpdateMultipurposeMember(n.target);
					});
					graph.Refresh();
				});
			}
			if(property.CanSetValue()) {
				menu.AddItem(new GUIContent("Set"), false, () => {
					NodeEditorUtility.AddNewNode<NodeSetValue>(editorData, property.Name, null, position, delegate (NodeSetValue n) {
						var mData = MemberData.CreateFromValue(property);
						n.target = mData;
						if(mData.type != null) {
							if(ReflectionUtils.CanCreateInstance(mData.type)) {
								n.value = new MemberData(ReflectionUtils.CreateInstance(mData.type));
							} else {
								n.value = MemberData.CreateFromValue(null, mData.type);
							}
						}
					});
					graph.Refresh();
				});
			}
			menu.ShowAsContext();
		}

		private void DragHandleFunction(uNodeFunction function, Vector2 position) {
			if(function.owner != editorData.graph) {
				EditorUtility.DisplayDialog("Error", "The graph of the function must same with the current graph", "Ok");
				return;
			}
			NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData, function.Name, null, position, (n) => {
				n.target.target = MemberData.CreateFromValue(function);
				MemberDataUtility.UpdateMultipurposeMember(n.target);
			});
			graph.Refresh();
			DragAndDrop.SetGenericData("uNode", null);
		}

		private void DragHandleMember(FieldInfo member, Vector2 position) {
			if(member.IsPrivate) {
				if(!EditorUtility.DisplayDialog("Variable is Private", "The variable you're drop is private, it may give error on compile to script.\n\nDo you want to continue?", "Continue", "Cancel")) {
					return;
				}
			}
			GenericMenu menu = new GenericMenu();
			menu.AddItem(new GUIContent("Get"), false, (() => {
				NodeEditorUtility.AddNewNode(editorData, member.Name, null, position, delegate (MultipurposeNode n) {
					var mData = new MemberData(member);
					n.target.target = new MemberData(member);
					MemberDataUtility.UpdateMultipurposeMember(n.target);
				});
				graph.Refresh();
			}));
			menu.AddItem(new GUIContent("Set"), false, (() => {
				NodeEditorUtility.AddNewNode(editorData, member.Name, null, position, delegate (MultipurposeNode n) {
					var mData = new MemberData(member);
					n.target.target = new MemberData(member);
					MemberDataUtility.UpdateMultipurposeMember(n.target);
					NodeEditorUtility.AddNewNode(editorData, "Set", null, position, delegate (NodeSetValue setNode) {
						setNode.target = MemberData.CreateConnection(n, false);
						if(mData.type != null) {
							if(ReflectionUtils.CanCreateInstance(member.FieldType)) {
								setNode.value = new MemberData(ReflectionUtils.CreateInstance(member.FieldType));
							} else {
								setNode.value = MemberData.CreateFromValue(null, member.FieldType);
							}
						}
					});
				});
				graph.Refresh();
			}));
			menu.ShowAsContext();
		}

		private void DragHandleMember(PropertyInfo member, Vector2 position) {
			bool nonPublic = false;
			if(member.GetGetMethod(false) == null && member.GetSetMethod(false) == null) {
				if(!EditorUtility.DisplayDialog("Property is Private", "The property you're drop is private, it may give error on compile to script.\n\nDo you want to continue?", "Continue", "Cancel")) {
					return;
				}
				nonPublic = true;
			}
			GenericMenu menu = new GenericMenu();
			if(member.GetGetMethod(nonPublic) != null) {
				menu.AddItem(new GUIContent("Get"), false, (() => {
					NodeEditorUtility.AddNewNode(editorData, member.Name, null, position, delegate (MultipurposeNode n) {
						var mData = new MemberData(member);
						n.target.target = new MemberData(member);
						MemberDataUtility.UpdateMultipurposeMember(n.target);
					});
					graph.Refresh();
				}));
			}
			if(member.GetSetMethod(nonPublic) != null) {
				menu.AddItem(new GUIContent("Set"), false, (() => {
					NodeEditorUtility.AddNewNode(editorData, member.Name, null, position, delegate (MultipurposeNode n) {
						var mData = new MemberData(member);
						n.target.target = new MemberData(member);
						MemberDataUtility.UpdateMultipurposeMember(n.target);
						NodeEditorUtility.AddNewNode(editorData, "Set", null, position, delegate (NodeSetValue setNode) {
							setNode.target = MemberData.CreateConnection(n, false);
							if(mData.type != null) {
								if(ReflectionUtils.CanCreateInstance(member.PropertyType)) {
									setNode.value = new MemberData(ReflectionUtils.CreateInstance(member.PropertyType));
								} else {
									setNode.value = MemberData.CreateFromValue(null, member.PropertyType);
								}
							}
						});
					});
					graph.Refresh();
				}));
			}
			menu.ShowAsContext();
		}

		private void DragHandleMember(MethodInfo member, Vector2 position, Vector2 screenPosition = default(Vector2)) {
			if(member.IsPrivate) {
				if(!EditorUtility.DisplayDialog("Function is Private", "The function you're drop is private, it may give error on compile to script.\n\nDo you want to continue?", "Continue", "Cancel")) {
					return;
				}
			}
			if(member.ContainsGenericParameters) {
				var args = member.GetGenericArguments();
				TypeItem[] typeItems = new TypeItem[args.Length];
				for(int i = 0; i < args.Length; i++) {
					var fil = new FilterAttribute(args[i].BaseType);
					fil.ToFilterGenericConstraints(args[i]);
					typeItems[i] = new TypeItem(args[i].BaseType, fil);
				}
				if(args.Length == 1) {
					ItemSelector w = null;
					Action<MemberData> action = delegate (MemberData m) {
						if(w != null) {
							w.Close();
						}
						TypeSelectorWindow.ShowAsNew(Rect.zero, typeItems[0].filter, delegate (MemberData[] members) {
							member = member.MakeGenericMethod(members.Select(item => item.Get<Type>()).ToArray());
							DragHandleMember(member, position, screenPosition);
						}, new TypeItem(m, typeItems[0].filter));
					};
					w = ItemSelector.ShowAsNew(null, typeItems[0].filter, action, true).ChangePosition(screenPosition.ToScreenPoint());
				} else {
					TypeSelectorWindow.ShowAsNew(screenPosition, new FilterAttribute() { OnlyGetType = true }, (members) => {
						member = member.MakeGenericMethod(members.Select(item => item.Get<Type>()).ToArray());
						DragHandleMember(member, position, screenPosition);
					}, typeItems);
				}
			} else {
				NodeEditorUtility.AddNewNode(editorData, member.Name, null, position, delegate (MultipurposeNode n) {
					n.target.target = new MemberData(member);
					MemberDataUtility.UpdateMultipurposeMember(n.target);
					graph.Refresh();
				});
			}
			DragAndDrop.SetGenericData("uNode", null);
		}

		private void DragHandleMember(ConstructorInfo ctor, Vector2 position) {
			if(ctor.IsPrivate) {
				if(!EditorUtility.DisplayDialog("Constructor is Private", "The constructor you're drop is private, it may give error on compile to script.\n\nDo you want to continue?", "Continue", "Cancel")) {
					return;
				}
			}
			NodeEditorUtility.AddNewNode(editorData, ctor.Name, null, position, delegate (MultipurposeNode n) {
				n.target.target = new MemberData(ctor);
				MemberDataUtility.UpdateMultipurposeMember(n.target);
				graph.Refresh();
			});
			DragAndDrop.SetGenericData("uNode", null);
		}

		private void DragHandleType(Type type, Vector2 position, Vector2 menuPosition) {
			FilterAttribute filter = new FilterAttribute();
			filter.MaxMethodParam = int.MaxValue;
			filter.VoidType = true;
			filter.Public = true;
			filter.Instance = true;
			filter.Static = false;
			filter.DisplayDefaultStaticType = false;
			string category = type.PrettyName();
			var customItems = ItemSelector.MakeCustomItems(type, filter, category, "Inherit Members");
			if(customItems != null) {
				ItemSelector w = ItemSelector.ShowWindow(null, MemberData.none, filter, delegate (MemberData value) {
					NodeEditorUtility.AddNewNode(editorData, null, null, position, delegate (MultipurposeNode n) {
						if(n.target == null) {
							n.target = new MultipurposeMember();
						}
						n.target.target = value;
						MemberDataUtility.UpdateMultipurposeMember(n.target);
					});
					graph.Refresh();
				}, customItems).ChangePosition(menuPosition);
				w.displayDefaultItem = false;
			}
			DragAndDrop.SetGenericData("uNode", null);
		}

		private void DragHandleMember(Type member, Vector2 position) {
			if(member.IsNotPublic) {
				if(!EditorUtility.DisplayDialog("Type is Private", "The type you're drop is private, it may give error on compile to script.\n\nDo you want to continue?", "Continue", "Cancel")) {
					return;
				}
			}
			NodeEditorUtility.AddNewNode(editorData, member.Name, null, position, delegate (MultipurposeNode n) {
				n.target.target = new MemberData(member);
				MemberDataUtility.UpdateMultipurposeMember(n.target);
				graph.Refresh();
			});
			DragAndDrop.SetGenericData("uNode", null);
		}

		private void DragHandleMember(NodeMenu menu, Vector2 position) {
			NodeEditorUtility.AddNewNode(menu.type, editorData, position, (NodeComponent node) => {
				graph.Refresh();
			});
			DragAndDrop.SetGenericData("uNode", null);
		}

		private void DragHandleMember(INodeItemCommand command, Vector2 position) {
			command.graph = graph;
			command.Setup(position);
			DragAndDrop.SetGenericData("uNode", null);
		}
		#endregion

		private void OnDragPerformEvent(DragPerformEvent evt) {
			Vector2 topMPos;
			Vector2 mPos = GetMousePosition(evt, out topMPos);
			if(DragAndDrop.GetGenericData("uNode") != null) {
				var generic = DragAndDrop.GetGenericData("uNode");

				#region Function
				if(generic is uNodeFunction) {//Drag functions.
					var function = generic as uNodeFunction;
					DragHandleFunction(function, mPos);
				} else
				#endregion

				#region Property
				if(generic is uNodeProperty) {//Drag property
					var property = generic as uNodeProperty;
					DragHandleProperty(property, mPos);
				} else
				#endregion

				#region Variable
				if(generic is VariableData) {//Drag variable.
					var varData = generic as VariableData;
					var UO = DragAndDrop.GetGenericData("uNode-Target") as UnityEngine.Object;
					DragHandleVariable(UO, varData, mPos);
				} else
				#endregion

				#region Visual Element
				if(generic is VisualElement) {
					#region Variable
					if(generic is TreeViews.VariableView) {
						var view = generic as TreeViews.VariableView;
						var variable = view.variable;
						var root = view.owner as uNodeRoot;
						if(root != editorData.graph) {
							if(uNodeEditorUtility.IsPrefab(root)) {
								root = GraphUtility.GetTempGraphObject(root);
								if(root == editorData.graph) {
									variable = root.GetVariableData(variable.Name);
								} else {
									if(view.owner is IClassIdentifier) {
										var runtimeType = ReflectionUtils.GetRuntimeType(view.owner as uNodeRoot);
										var field = runtimeType.GetField(variable.Name);
										if(field != null) {
											DragHandleMember(field, mPos);
										} else {
											uNodeEditorUtility.DisplayErrorMessage();
										}
										return;
									}
									var type = uNodeEditorUtility.GetFullScriptName(view.owner as uNodeRoot).ToType(false);
									if(type != null) {
										var field = type.GetField(variable.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
										if(field != null) {
											if(field.IsPublic) {
												DragHandleMember(field, mPos);
											} else {
												EditorUtility.DisplayDialog("Variable is Private", "Can't access the variable because the variable is not public.", "OK");
											}
											return;
										}
									}
									EditorUtility.DisplayDialog("Type not found", "You need to compile graph to script in order to use it on another graph.", "OK");
									return;
								}
							} else
								return;
						}
						if(variable != null && root != null) {
							DragHandleVariable(root, variable, mPos);
						}
					} else
					#endregion

					#region Property
					if(generic is TreeViews.PropertyView) {
						var view = generic as TreeViews.PropertyView;
						var property = view.property;
						var root = property.owner as uNodeRoot;
						if(root != editorData.graph) {
							if(uNodeEditorUtility.IsPrefab(root)) {
								root = GraphUtility.GetTempGraphObject(root);
								if(root == editorData.graph) {
									property = root.GetPropertyData(property.Name);
								} else {
									if(property.owner is IClassIdentifier) {
										var runtimeType = ReflectionUtils.GetRuntimeType(property.owner as uNodeRoot);
										var member = runtimeType.GetProperty(property.Name);
										if(member != null) {
											DragHandleMember(member, mPos);
										} else {
											uNodeEditorUtility.DisplayErrorMessage();
										}
										return;
									}
									var type = uNodeEditorUtility.GetFullScriptName(property.owner).ToType(false);
									if(type != null) {
										var member = type.GetProperty(property.Name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
										if(member != null) {
											DragHandleMember(member, mPos);
											return;
										}
									}
									EditorUtility.DisplayDialog("Type not found", "You need to compile graph to script in order to use it on another graph.", "OK");
									return;
								}
							} else
								return;
						}
						if(property != null && root != null) {
							DragHandleProperty(property, mPos);
						}
					} else
					#endregion

					#region Function
					if(generic is TreeViews.FunctionView) {
						var view = generic as TreeViews.FunctionView;
						var function = view.function;
						var root = function.owner as uNodeRoot;
						if(root != editorData.graph) {
							if(uNodeEditorUtility.IsPrefab(root)) {
								root = GraphUtility.GetTempGraphObject(root);
								if(root == editorData.graph) {
									function = root.GetFunction(function.Name, function.GenericParameters.Count, function.Parameters.Select(p => p.Type).ToArray());
								} else {
									if(function.owner is IClassIdentifier) {
										var runtimeType = ReflectionUtils.GetRuntimeType(function.owner as uNodeRoot);
										var member = runtimeType.GetMethod(function.Name, function.Parameters.Select(p => p.Type).ToArray());
										if(member != null) {
											DragHandleMember(member, mPos);
										} else {
											uNodeEditorUtility.DisplayErrorMessage();
										}
										return;
									}
									var type = uNodeEditorUtility.GetFullScriptName(function.owner).ToType(false);
									if(type != null) {
										var member = type.GetMethod(function.Name, function.Parameters.Select(p => p.Type).ToArray());
										if(member != null) {
											if(member.IsPublic) {
												DragHandleMember(member, mPos, topMPos);
											} else {
												EditorUtility.DisplayDialog("Function is Private", "Can't access the function because the function is not public.", "OK");
											}
											return;
										}
									}
									EditorUtility.DisplayDialog("Type not found", "You need to compile graph to script in order to use it on another graph.", "OK");
									return;
								}
							} else
								return;
						}
						if(function != null && root != null) {
							DragHandleFunction(function, mPos);
						}
					} else
					#endregion

					#region Graph & Macro
					if(generic is TreeViews.GraphTreeView) {
						var view = generic as TreeViews.GraphTreeView;
						var root = view.root;
						if(root != editorData.graph) {
							if(uNodeEditorUtility.IsPrefab(root)) {
								if(root is uNodeMacro) {
									CreateLinkedMacro(root as uNodeMacro, mPos);
									return;
								}
								root = GraphUtility.GetTempGraphObject(root);
								if(root == editorData.graph) {
									NodeEditorUtility.AddNewNode(editorData, "this", null, mPos, delegate (MultipurposeNode n) {
										n.target.target = new MemberData(root, MemberData.TargetType.SelfTarget);
										MemberDataUtility.UpdateMultipurposeMember(n.target);
									});
									graph.Refresh();
								} else {
									if(root is IClassIdentifier) {
										EditorUtility.DisplayDialog("Error", "Unsupported graph type.", "OK");
										return;
									}
									var type = uNodeEditorUtility.GetFullScriptName(root).ToType(false);
									if(type != null) {
										DragHandleMember(type, mPos);
									} else {
										EditorUtility.DisplayDialog("Type not found", "You need to compile graph to script in order to use it on another graph.", "OK");
									}
									return;
								}
							} else
								return;
						}
					}
					#endregion
				} else
				#endregion

				#region MemberInfo
				if(generic is MemberInfo) {
					if(generic is Type) {
						DragHandleMember(generic as Type, mPos);
					} else if(generic is FieldInfo) {
						DragHandleMember(generic as FieldInfo, mPos);
					} else if(generic is PropertyInfo) {
						DragHandleMember(generic as PropertyInfo, mPos);
					} else if(generic is MethodInfo) {
						DragHandleMember(generic as MethodInfo, mPos, topMPos);
					} else if(generic is ConstructorInfo) {
						DragHandleMember(generic as ConstructorInfo, mPos);
					}
				}
				#endregion

				#region Menu
				if(generic is NodeMenu) {
					DragHandleMember(generic as NodeMenu, mPos);
				} else if(generic is INodeItemCommand) {
					DragHandleMember(generic as INodeItemCommand, mPos);
				}
				#endregion
			} else if(DragAndDrop.objectReferences.Length == 1) {//Dragging UnityObject
				var dragObject = DragAndDrop.objectReferences[0];
				var iPOS = graph.window.GetMousePositionForMenu(topMPos);
				DragHandleObject(dragObject, mPos, iPOS);
			} else if(DragAndDrop.objectReferences.Length > 1) {
				var iPOS = graph.window.GetMousePositionForMenu(topMPos);
				GenericMenu menu = new GenericMenu();
				foreach(var o in DragAndDrop.objectReferences) {
					menu.AddItem(new GUIContent("Get/" + o.name), false, (dOBJ) => {
						FilterAttribute filter = new FilterAttribute();
						filter.MaxMethodParam = int.MaxValue;
						filter.VoidType = true;
						filter.Public = true;
						filter.Instance = true;
						filter.Static = false;
						filter.DisplayDefaultStaticType = false;
						string category = dOBJ.GetType().PrettyName();
						var customItems = ItemSelector.MakeCustomItems(dOBJ.GetType(), filter, category, "Inherit Members");
						if(customItems != null) {
							customItems.Insert(0, new ItemSelector.CustomItem("this", () => {
								var value = new MemberData(dOBJ, MemberData.TargetType.Values);
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData, null, null, mPos, delegate (MultipurposeNode n) {
									n.target.target = value;
									MemberDataUtility.UpdateMultipurposeMember(n.target);
								});
								graph.Refresh();
							}, category));
							ItemSelector w = ItemSelector.ShowWindow(editorData.selectedGroup ?? editorData.selectedRoot as UnityEngine.Object ?? editorData.graph, MemberData.none, filter, delegate (MemberData value) {
								value.instance = new MemberData(dOBJ, MemberData.TargetType.Values);
								NodeEditorUtility.AddNewNode<MultipurposeNode>(editorData, null, null, mPos, delegate (MultipurposeNode n) {
									n.target.target = value;
									MemberDataUtility.UpdateMultipurposeMember(n.target);
								});
								graph.Refresh();
							}, customItems).ChangePosition(iPOS);
							w.displayDefaultItem = false;
						}
					}, o);
					menu.AddItem(new GUIContent("Set/" + o.name), false, (dOBJ) => {
						FilterAttribute filter = new FilterAttribute();
						filter.SetMember = true;
						filter.MaxMethodParam = int.MaxValue;
						//filter.VoidType = true;
						filter.Public = true;
						filter.Instance = true;
						filter.Static = false;
						filter.DisplayDefaultStaticType = false;
						var customItems = ItemSelector.MakeCustomItems(dOBJ.GetType(), filter, dOBJ.GetType().PrettyName(), "Inherit Members");
						if(customItems != null) {
							ItemSelector w = ItemSelector.ShowWindow(dOBJ as UnityEngine.Object, MemberData.none, filter, delegate (MemberData value) {
								value.instance = dOBJ;
								NodeEditorUtility.AddNewNode<NodeSetValue>(editorData, null, null, mPos, delegate (NodeSetValue n) {
									n.target = value;
								});
								graph.Refresh();
							}, customItems).ChangePosition(iPOS);
							w.displayDefaultItem = false;
						}
					}, o);
				}
				menu.ShowAsContext();
			}
		}
		#endregion

		#region Callbacks
		protected override bool canPaste => graph.nodeToCopy.Count > 0;

		public override EventPropagation DeleteSelection() {
			int removedCount = 0;
			selection.ForEach((s) => {
				if(s is BaseNodeView) {
					BaseNodeView view = s as BaseNodeView;
					if(view.targetNode != null) {
						NodeEditorUtility.RemoveNode(editorData, view.targetNode);
						OnNodeRemoved(view);
						removedCount++;
					}
				} else if(s is EdgeView) {
					EdgeView view = s as EdgeView;
					var inPort = view.input as PortView;
					var outPort = view.output as PortView;
					if(inPort != null && inPort.orientation == Orientation.Horizontal) {
						inPort.ResetPortValue();
					} else if(outPort != null && outPort.orientation == Orientation.Vertical) {
						outPort.ResetPortValue();
					}
					MarkRepaint(inPort?.owner, outPort?.owner);
					removedCount++;
				} else if(s is TransitionView) {
					TransitionView view = s as TransitionView;
					if(view.transition != null) {
						NodeEditorUtility.RemoveTransition(view.transition);
						OnNodeRemoved(view);
						removedCount++;
					}
				} else if(s is BlockView) {
					BlockView block = s as BlockView;
					if(!selection.Contains(block.owner.nodeView) && block.owner.blocks.blocks.Contains(block.data)) {
						block.owner.nodeView.RegisterUndo();
						block.owner.blocks.RemoveBlock(block.data);
						OnNodeRemoved(block);
						removedCount++;
					}
				}
			});
			selection.Clear();
			if(removedCount > 0) {
				graph.Refresh();
				return EventPropagation.Stop;
			}
			return EventPropagation.Continue;
		}

		protected bool animateFrame = true;
		[NonSerialized]
		public List<UNodeView> searchedNodes = new List<UNodeView>();
		[NonSerialized]
		public UNodeView[] highlightedNodes = new UNodeView[0];
		private int frameSearchIndex = 0;
		private int currentAnimateFrame = 0;

		public void OnSearchNext() {
			frameSearchIndex++;
			if(frameSearchIndex >= searchedNodes.Count) {
				frameSearchIndex = 0;
			}
			if(searchedNodes.Count > 0) {
				Frame(new UNodeView[] { searchedNodes[frameSearchIndex] });
			}
		}

		public void OnSearchPrev() {
			frameSearchIndex--;
			if(frameSearchIndex < 0) {
				frameSearchIndex = searchedNodes.Count - 1;
			}
			if(searchedNodes.Count > 0) {
				Frame(new UNodeView[] { searchedNodes[frameSearchIndex] });
			}
		}

		public void OnSearchChanged(GraphSearchQuery query) {
			List<UNodeView> matched = new List<UNodeView>();
			searchedNodes = matched;
			frameSearchIndex = 0;
			switch(query.type) {
				case GraphSearchQuery.SearchType.Node: {
					foreach(var n in nodeViews) {
						string title = n.GetTitle();
						List<string> strs = new List<string>();
						string str = "";
						for(int i = 0; i < title.Length; i++) {
							char c = title[i];
							if(!string.IsNullOrEmpty(str)) {
								if(char.IsUpper(c) || !char.IsLetter(c) && c != '$') {
									strs.Add(str.ToLower());
									str = "";
								}
							}
							str += c;
							if(i + 1 == title.Length && !string.IsNullOrEmpty(str)) {
								strs.Add(str.ToLower());
							}
						}
						if(strs.Count > 0) {
							foreach(var q in query.query) {
								foreach(var s in strs) {
									if(s.StartsWith(q.ToLower())) {
										matched.Add(n);
										goto VALID;
									}
								}
							}
						}
					VALID:
						continue;
					}
					break;
				}
				case GraphSearchQuery.SearchType.NodeType: {
					List<string> types = new List<string>();
					foreach(var q in query.query) {
						string str = q.ToLower();
						if("event".StartsWith(str)) {
							types.Add("event");
						}
						if("variable".StartsWith(str)) {
							types.Add("variable");
						}
						if("field".StartsWith(str)) {
							types.Add("field");
						}
						if("property".StartsWith(str)) {
							types.Add("property");
						}
						if("function".StartsWith(str) || "method".StartsWith(str)) {
							types.Add("function");
						}
						if("constructor".StartsWith(str)) {
							types.Add("constructor");
						}
						if("literal".StartsWith(str)) {
							types.Add("literal");
						}
						if("value".StartsWith(str)) {
							types.Add("value");
						}
						if("proxy".StartsWith(str)) {
							types.Add("proxy");
						}
						if("block".StartsWith(str)) {
							types.Add("block");
						}
					}
					types = types.Distinct().ToList();
					foreach(var n in nodeViews) {
						foreach(var t in types) {
							switch(t) {
								case "event":
									if(n.targetNode is EventNode) {
										matched.Add(n);
										goto VALID;
									}
									break;
								case "variable":
									if(n.targetNode is MultipurposeNode) {
										var node = n.targetNode as MultipurposeNode;
										var tType = node.target.target.targetType;
										if(tType == MemberData.TargetType.uNodeVariable ||
											tType == MemberData.TargetType.uNodeGroupVariable ||
											tType == MemberData.TargetType.uNodeLocalVariable ||
											tType == MemberData.TargetType.Field) {
											matched.Add(n);
											goto VALID;
										}
									}
									break;
								case "property":
									if(n.targetNode is MultipurposeNode) {
										var node = n.targetNode as MultipurposeNode;
										var tType = node.target.target.targetType;
										if(tType == MemberData.TargetType.Property ||
											tType == MemberData.TargetType.uNodeProperty) {
											matched.Add(n);
											goto VALID;
										}
									}
									break;
								case "function":
									if(n.targetNode is MultipurposeNode) {
										var node = n.targetNode as MultipurposeNode;
										var tType = node.target.target.targetType;
										if(tType == MemberData.TargetType.uNodeFunction ||
											tType == MemberData.TargetType.Method) {
											matched.Add(n);
											goto VALID;
										}
									}
									break;
								case "constructor":
									if(n.targetNode is MultipurposeNode) {
										var node = n.targetNode as MultipurposeNode;
										var tType = node.target.target.targetType;
										if(tType == MemberData.TargetType.Constructor) {
											matched.Add(n);
											goto VALID;
										}
									}
									break;
								case "literal":
									if(n.targetNode is MultipurposeNode) {
										var node = n.targetNode as MultipurposeNode;
										var tType = node.target.target.targetType;
										if(tType == MemberData.TargetType.Values) {
											Type type = node.target.target.type;
											if(type != null && type.IsPrimitive || type == typeof(string)) {
												matched.Add(n);
												goto VALID;
											}
										}
									}
									break;
								case "value":
									if(n.targetNode is MultipurposeNode) {
										var node = n.targetNode as MultipurposeNode;
										var tType = node.target.target.targetType;
										if(tType == MemberData.TargetType.Values) {
											matched.Add(n);
											goto VALID;
										}
									}
									break;
								case "proxy":
									var ports = n.inputPorts;
									ports.AddRange(n.outputPorts);
									foreach(var p in ports) {
										if(p.IsProxy()) {
											matched.Add(n);
											goto VALID;
										}
									}
									break;
							}
						}
					VALID:
						continue;
					}
					break;
				}
				case GraphSearchQuery.SearchType.Port: {
					foreach(var n in nodeViews) {
						var ports = n.inputPorts;
						ports.AddRange(n.outputPorts);
						foreach(var p in ports) {
							string str = p.portName.ToLower();
							foreach(var t in query.query) {
								if(str.StartsWith(t.ToLower())) {
									matched.Add(n);
									goto VALID;
								} else if(p.orientation == Orientation.Horizontal && p.direction == Direction.Input && GraphSearchQuery.csharpKeyword.Contains(t)) {
									switch(t.ToLower()) {
										case "false": {
											object val = p.GetValue();
											if(val is bool && (bool)val == false || val is IGetValue) {
												matched.Add(n);
												goto VALID;
											} else if(val is IGetValue) {
												var obj = (val as IGetValue).Get();
												if(obj is bool && (bool)obj == false) {
													matched.Add(n);
													goto VALID;
												}
											}
											break;
										}
										case "true": {
											object val = p.GetValue();
											if(val is bool && (bool)val == true || val is IGetValue) {
												matched.Add(n);
												goto VALID;
											} else if(val is IGetValue) {
												var obj = (val as IGetValue).Get();
												if(obj is bool && (bool)obj == true) {
													matched.Add(n);
													goto VALID;
												}
											}
											break;
										}
										case "null": {
											object val = p.GetValue();
											if(val == null) {
												matched.Add(n);
												goto VALID;
											} else if(val is IGetValue) {
												var obj = (val as IGetValue).Get();
												if(obj == null) {
													matched.Add(n);
													goto VALID;
												}
											}
											break;
										}
										case "bool":
											if(p.GetPortType() == typeof(bool)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "byte":
											if(p.GetPortType() == typeof(byte)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "char":
											if(p.GetPortType() == typeof(bool)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "decimal":
											if(p.GetPortType() == typeof(decimal)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "double":
											if(p.GetPortType() == typeof(double)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "float":
											if(p.GetPortType() == typeof(float)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "int":
											if(p.GetPortType() == typeof(int)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "long":
											if(p.GetPortType() == typeof(long)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "object":
											if(p.GetPortType() == typeof(object)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "sbyte":
											if(p.GetPortType() == typeof(sbyte)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "short":
											if(p.GetPortType() == typeof(short)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "string":
											if(p.GetPortType() == typeof(string)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "uint":
											if(p.GetPortType() == typeof(uint)) {
												matched.Add(n);
												goto VALID;
											}
											break;
										case "ulong":
											if(p.GetPortType() == typeof(ulong)) {
												matched.Add(n);
												goto VALID;
											}
											break;
									}
								} else {
									Type type = t.ToType(false);
									if(type != null) {
										if(p.GetPortType() == type) {
											matched.Add(n);
											goto VALID;
										}
									}
								}
								//else if(t.Length > 0 && (char.IsNumber(t[0]) || t[0] == '"')) {

								//}
							}
						}
					VALID:
						continue;
					}
					break;
				}
			}

			HighlightNodes(matched);
			if(matched.Count > 0) {
				Frame(matched);
			}
		}

		public void HighlightNodes(IList<NodeComponent> nodes, float duration = 1) {
			if(nodes == null || nodes.Count == 0)
				return;
			if(requiredReload) {
				executeAfterReload += () => HighlightNodes(GetNodeViews(nodes), duration);
			} else {
				HighlightNodes(GetNodeViews(nodes), duration);
			}
		}

		protected void HighlightNodes(IList<UNodeView> nodeViews, float duration = 0) {
			var nodes = nodeViews.ToArray();
			if(highlightedNodes?.Length > 0) {
				//Ensure remove previously highligted nodes
				RemoveHighlightedNodes(highlightedNodes);
			}
			highlightedNodes = nodes;
			foreach(var n in highlightedNodes) {
				if(n != null) {
					var visualElement = n.Q("highlighted-border");
					if(visualElement == null) {
						visualElement = new VisualElement() {
							name = "highlighted-border",
							pickingMode = PickingMode.Ignore,
						};
						n.Add(visualElement);
						visualElement.EnableInClassList("highlighted", true);
					}
					visualElement.SetLayout(new Rect(-10, -10, n.layout.width + 20, n.layout.height + 20));
				}
			}
			if(duration > 0) {
				var time = Time.realtimeSinceStartup;
				uNodeThreadUtility.ExecuteAfterDuration(duration, () => {
					uNodeThreadUtility.ExecuteWhileDuration(1, () => {
						if(highlightedNodes == nodes) {
							foreach(var n in nodes) {
								var visualElement = n.Q("highlighted-border");
								if(visualElement != null) {
									visualElement.SetOpacity(visualElement.resolvedStyle.opacity - uNodeThreadUtility.deltaTime);
								}
							}
						}
					});
					uNodeThreadUtility.ExecuteAfterDuration(1, () => {
						if(highlightedNodes == nodes) {
							RemoveHighlightedNodes(nodes);
						} else {
							RemoveHighlightedNodes(nodes.Where(node => !highlightedNodes.Contains(node)));
						}
					});
				});
			}
		}

		protected void RemoveHighlightedNodes(IEnumerable<UNodeView> nodes) {
			foreach(var n in nodes) {
				if(n != null) {
					var visualElement = n.Q("highlighted-border");
					if(visualElement != null) {
						visualElement.RemoveFromHierarchy();
					}
				}
			}
		}

		public void Frame(IEnumerable<NodeComponent> nodes) {
			if(nodes == null)
				return;
			List<UNodeView> nodeViews = new List<UNodeView>();
			foreach(var node in nodes) {
				if(node == null)
					continue;
				if(nodeViewsPerNode.TryGetValue(node, out var view)) {
					nodeViews.Add(view);
				}
			}
			Frame(nodeViews);
		}

		public void Frame(IList<UNodeView> nodeViews) {
			if(nodeViews == null || nodeViews.Count == 0)
				return;
			Rect rect = contentViewContainer.layout;
			Vector3 frameTranslation = Vector3.zero;
			Vector3 frameScaling = Vector3.one;
			if(nodeViews[0] != null) {
				rect = nodeViews[0].ChangeCoordinatesTo(contentViewContainer, new Rect(0f, 0f, nodeViews[0].layout.width, nodeViews[0].layout.height));
			}
			rect = nodeViews.Aggregate(rect, (current, currentGraphElement) => {
				VisualElement currentElement = currentGraphElement;
				return RectUtils.Encompass(current, currentElement.ChangeCoordinatesTo(contentViewContainer, new Rect(0f, 0f, currentElement.layout.width, currentElement.layout.height)));
			});
			CalculateFrameTransform(rect, layout, 30, out frameTranslation, out frameScaling);
			if(animateFrame) {
				if(nodeViews.Count == 1)
					SetPixelCachedOnBoundChanged(true);
				currentAnimateFrame++;
				int frame = currentAnimateFrame;
				float time = 0;
				this.ScheduleActionUntil(t => {
					time += t.deltaTime / 1000f;
					Vector3 position = Vector3.Lerp(contentViewContainer.transform.position, frameTranslation, time);
					Vector3 scale = Vector3.Lerp(contentViewContainer.transform.scale, frameScaling, time);
					UpdateViewTransform(position, scale);
					contentViewContainer.MarkDirtyRepaint();
				}, () => {
					bool result = currentAnimateFrame != frame ||
						Vector3.Distance(contentViewContainer.transform.position, frameTranslation) < 5 &&
						Vector3.Distance(contentViewContainer.transform.scale, frameScaling) < 5;
					if(result) {
						SetPixelCachedOnBoundChanged(false);
					}
					return result;
				});
			} else {
				Matrix4x4.TRS(frameTranslation, Quaternion.identity, frameScaling);
				UpdateViewTransform(frameTranslation, frameScaling);
				contentViewContainer.MarkDirtyRepaint();
			}
		}

		public void SetGraphPosition(Vector2 position) {
			UpdateViewTransform(-position * scale, new Vector3(scale, scale, 1));
			contentViewContainer.MarkDirtyRepaint();
		}

		private void ExecuteCommand(ExecuteCommandEvent evt) {
			if(!MouseCaptureController.IsMouseCaptured()) {
				if(evt.commandName == "Copy") {
					graph.CopySelectedNode();
					evt.StopPropagation();
				} else if(evt.commandName == "Delete" || evt.commandName == "SoftDelete") {
					DeleteSelectionCallback(AskUser.DontAskUser);
					evt.StopPropagation();
				} else if(evt.commandName == "FrameSelected") {
					FrameSelection();
					evt.StopPropagation();
				} else if(evt.commandName == "Duplicate") {
					graph.CopySelectedNode();
					graph.Repaint();
					var clickedPos = GetMousePosition(graph.topMousePos);
					graph.PasteNode(clickedPos);
					graph.Refresh();
					evt.StopPropagation();
				}
				//else if(evt.commandName == "Cut") {
				//	CutSelectionCallback();
				//	evt.StopPropagation();
				//} 

				if(evt.isPropagationStopped && evt.imguiEvent != null) {
					evt.imguiEvent.Use();
				}
			}
		}

		public override void AddToSelection(ISelectable selectable) {
			base.AddToSelection(selectable);
			if(selectable is BaseNodeView) {
				graph.SelectNode((selectable as BaseNodeView).targetNode, false);
			} else if(selectable is TransitionView) {
				graph.Select((selectable as TransitionView).transition);
			} else if(selectable is BlockView) {
				var block = (selectable as BlockView);
				if(block.data != null && block.data.block != null && editorData.selected != editorData.selectedNodes) {
					graph.Select(new uNodeEditor.ValueInspector(block.data.block, block.owner.nodeView.targetNode));
				}
			}
		}

		public override void RemoveFromSelection(ISelectable selectable) {
			base.RemoveFromSelection(selectable);
			if(selectable is BaseNodeView) {
				graph.UnselectNode((selectable as BaseNodeView).targetNode);
			}
		}

		public override void ClearSelection() {
			base.ClearSelection();
			graph.ClearSelection();
		}

		GraphViewChange GraphViewChangedCallback(GraphViewChange changes) {
			if(changes.elementsToRemove != null) {

				//Handle ourselves the edge and node remove
				changes.elementsToRemove.RemoveAll(e => {
					var edge = e as EdgeView;
					var node = e as BaseNodeView;

					if(edge != null) {
						Disconnect(edge);
						return true;
					} else if(node != null) {
						graph.RemoveNode(node.targetNode);
						RemoveElement(node);
						return true;
					}
					return false;
				});
			}

			return changes;
		}

		void ViewTransformChangedCallback(GraphView view) {
			if(graph != null) {
				graph.position = -viewTransform.position / scale;
				graph.scale = viewTransform.scale;
				editorData.position = graph.position;
				if(editorData.position != (Vector2)graph.position) {
					UpdatePosition();
				}
			}
		}

		void ElementResizedCallback(VisualElement elem) {

		}

		public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
			var compatiblePorts = new List<Port>();
			var startPortView = startPort as PortView;

			compatiblePorts.AddRange(ports.ToList().Where(p => {
				if(!p.enabledSelf || p.direction == startPort.direction || p.orientation != startPort.orientation /*|| !startPortView.IsValidTarget(p)*/)
					return false;
				if(p.orientation == Orientation.Horizontal) {
					if(p.direction == Direction.Output) {
						FilterAttribute filter = startPortView.GetFilter();
						if(filter != null && filter.SetMember) {
							Node node = (p as PortView)?.GetNode() as Node;
							return node != null && node.CanSetValue();
						}
					} else {
						FilterAttribute filter = (p as PortView)?.GetFilter();
						if(filter != null && filter.SetMember) {
							Node node = startPortView.GetNode() as Node;
							return node != null && node.CanSetValue();
						}
					}
				}
				return true;
			}));

			return compatiblePorts;
		}

		public override void HandleEvent(EventBase evt) {
			if(evt is MouseUpEvent && graphDragger.isActive) {
				graphDragger.OnMouseUp(evt as MouseUpEvent);
				this.ReleaseMouse();
				return;
			}
			base.HandleEvent(evt);
		}

		private bool isCapturing = false;
		public void CaptureGraphScreenshot(float zoomScale = 2) {
			if(isCapturing)
				return;
			isCapturing = true;
			Vector2 offset = new Vector2(2, 2);//The offset from position to capture
			Vector2 position = graph.window.rootVisualElement.ChangeCoordinatesTo(
				graph.window.rootVisualElement.parent,
				graph.window.position.position);
			position.x += worldBound.x;
			position.y += worldBound.y - graph.window.rootVisualElement.layout.y;
			position += offset;
			SetZoomScale(zoomScale, true);
			int layoutWidth = (int)layout.width - 10;
			int layoutHeight = (int)layout.height - 10;
			int width = (int)(layoutWidth / zoomScale);
			int height = (int)(layoutHeight / zoomScale);

			EditorUtility.DisplayProgressBar("Capturing", "", 0);
			Rect graphRect = CalculateVisibleGraphRect();
			SetGraphPosition(new Vector2(graphRect.x, graphRect.y));
			float xCount = 1;
			float yCount = 1;
			while((xCount * width) + graphRect.x < graphRect.x + graphRect.width) {
				xCount++;
			}
			while((yCount * height) + graphRect.y < graphRect.y + graphRect.height) {
				yCount++;
			}
			uNodeThreadUtility.CreateThread(() => {
				if(miniMap != null)
					miniMap.SetOpacity(false);
				uNodeThreadUtility.WaitFrame(50);
				SetPixelCachedOnBoundChanged(false);
				float progress = 0;
				Texture2D[,] textures = new Texture2D[(int)yCount, (int)xCount];
				for(int y = 0; y < textures.GetLength(0); y++) {
					for(int x = 0; x < textures.GetLength(1); x++) {
						uNodeThreadUtility.QueueAndWait(() => {
							SetGraphPosition(new Vector2(graphRect.x + (x * width) - offset.x, graphRect.y + (y * height) - offset.x));
						});
						uNodeThreadUtility.Queue(() => {
							EditorUtility.ClearProgressBar();
						});
						uNodeThreadUtility.ExecuteAfter(3, () => {
							var pixels = UnityEditorInternal.InternalEditorUtility.ReadScreenPixel(position, layoutWidth, layoutHeight);
							var texture = new Texture2D(layoutWidth, layoutHeight, TextureFormat.RGB24, false);
							texture.SetPixels(pixels);
							textures[y, x] = texture;
							EditorUtility.DisplayProgressBar("Capturing", "", progress);
						});
						uNodeThreadUtility.WaitFrame(3);
						progress = (y * x) / (yCount * xCount);
						//GC.KeepAlive(textures);
					}
				}
				uNodeThreadUtility.QueueAndWait(() => {
					var canvasTexture = new Texture2D((int)xCount * layoutWidth, (int)yCount * layoutHeight, TextureFormat.RGB24, false);
					int realY = (int)yCount;
					for(int y = 0; y < yCount; y++) {
						realY--;
						for(int x = 0; x < xCount; x++) {
							Vector2 offet = new Vector2(x * layoutWidth, y * layoutHeight);
							canvasTexture.SetPixels((int)offet.x, (int)offet.y, layoutWidth, layoutHeight, textures[realY, x].GetPixels());
						}
					}
					//Color[] colors = canvasTexture.GetPixels(0, 0, (int)(graphRect.width * zoomScale), (int)(graphRect.height * zoomScale));
					//canvasTexture = new Texture2D((int)(graphRect.width * zoomScale), (int)(graphRect.height * zoomScale));
					//canvasTexture.SetPixels(colors);
					//canvasTexture.Apply();
					var bytes = canvasTexture.EncodeToPNG();
					EditorUtility.ClearProgressBar();
					isCapturing = false;
					if(miniMap != null)
						miniMap.SetOpacity(true);
					SetZoomScale(1, false);

					string path = EditorUtility.SaveFilePanel("Save Screenshot", "", "Capture", "png");
					File.WriteAllBytes(path, bytes);
				});
			}).Start();
		}

		private void CreateLinkedMacro(uNodeMacro macro, Vector2 position) {
			NodeEditorUtility.AddNewNode<Nodes.LinkedMacroNode>(editorData, null, null, position, (node) => {
				node.macroAsset = macro;
				NodeEditorUtility.AddNewObject(editorData.graph, "pins", node.transform, (go) => {
					node.pinObject = go;
					macro.Refresh(); //Make sure the data are up to date.
				});
				node.Refresh();
			});
			graph.Refresh();
		}

		private void SelectionToMacro(Vector2 mousePosition) {
			NodeEditorUtility.AddNewNode<Nodes.MacroNode>(graph.editorData, "Macro", null, mousePosition, (node) => {
				HashSet<UNodeView> nodeViews = new HashSet<UNodeView>();
				foreach(var n in graph.editorData.selectedNodes) {
					uNodeEditorUtility.RegisterUndoSetTransformParent(n.transform, node.transform);
					nodeViews.Add(GetNodeView(n));
				}
				HashSet<PortView> portHash = new HashSet<PortView>();
				Dictionary<PortView, Nodes.MacroPortNode> portMacros = new Dictionary<PortView, Nodes.MacroPortNode>();
				foreach(var view in nodeViews.Distinct()) {
					if(view != null) {
						var ports = view.inputPorts;
						ports.AddRange(view.outputPorts);
						foreach(var port in ports) {
							if(!port.connected || !port.IsVisible() || !portHash.Add(port))
								continue;
							var edges = port.GetEdges();
							foreach(var e in edges) {
								if(!e.isValid)
									continue;
								if(port.direction == Direction.Output) {
									var ePort = e.input as PortView;
									if(ePort != null && !nodeViews.Contains(ePort.owner)) {
										if(port.orientation == Orientation.Vertical) {
											if(!portMacros.TryGetValue(port, out var mNode)) {
												NodeEditorUtility.AddNewNode<Nodes.MacroPortNode>(
													graph.editorData, port.GetName(),
													null,
													new Vector2(
														view.targetNode.editorRect.x, view.targetNode.editorRect.y +
														view.targetNode.editorRect.height + 150),
													(macro) => {
														macro.gameObject.name = port.GetName();
														macro.kind = PortKind.FlowOutput;
														macro.transform.SetParent(node.transform);
														macro.target = ePort.GetConnection();
														portMacros[port] = macro;
														mNode = macro;
													});
											}
											port.OnValueChanged(MemberData.CreateConnection(mNode, true));

										} else {
											if(!portMacros.TryGetValue(port, out var mNode)) {
												NodeEditorUtility.AddNewNode<Nodes.MacroPortNode>(
												graph.editorData,
												null,
												null,
												new Vector2(
													view.targetNode.editorRect.x + view.targetNode.editorRect.width + 150,
													view.targetNode.editorRect.y),
												(macro) => {
													macro.gameObject.name = port.GetName();
													macro.kind = PortKind.ValueOutput;
													macro.type = new MemberData(port.GetPortType(), MemberData.TargetType.Type);
													macro.transform.SetParent(node.transform);
													macro.target = port.GetConnection();
													portMacros[port] = macro;
													mNode = macro;
												});
											}
											ePort.OnValueChanged(MemberData.CreateConnection(mNode, false));
										}
									}
								} else {
									var ePort = e.output as PortView;
									if(ePort != null && !nodeViews.Contains(ePort.owner)) {
										if(port.orientation == Orientation.Vertical) {
											if(!portMacros.TryGetValue(port, out var mNode)) {
												NodeEditorUtility.AddNewNode<Nodes.MacroPortNode>(graph.editorData, null, null,
												new Vector2(view.targetNode.editorRect.x, view.targetNode.editorRect.y - 150),
												(macro) => {
													macro.gameObject.name = port.GetName();
													macro.kind = PortKind.FlowInput;
													macro.transform.SetParent(node.transform);
													macro.target = port.GetConnection();
													portMacros[port] = macro;
													mNode = macro;
												});
											}
											ePort.OnValueChanged(MemberData.CreateConnection(mNode, true));
										} else {
											if(!portMacros.TryGetValue(ePort, out var mNode)) {
												NodeEditorUtility.AddNewNode<Nodes.MacroPortNode>(graph.editorData, null, null,
												new Vector2(view.targetNode.editorRect.x - 150, view.targetNode.editorRect.y),
												(macro) => {
													macro.gameObject.name = port.GetName();
													macro.kind = PortKind.ValueInput;
													macro.type = new MemberData(ePort.GetPortType(), MemberData.TargetType.Type);
													macro.transform.SetParent(node.transform);
													macro.target = ePort.GetConnection();
													portMacros[ePort] = macro;
													mNode = macro;
												});
											}
											port.OnValueChanged(MemberData.CreateConnection(mNode, false));
										}
									}
								}
							}
						}
					}
				}
			});
			MarkRepaint(true);
		}

		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
			var screenRect = graph.window.GetMousePositionForMenu(evt.mousePosition);
			var clickedPos = GetMousePosition(evt, out var position);
			graph.topMousePos = position;

			if(evt.target is RegionNodeView) {
				evt.target = this;
			}

			#region Graph
			if(evt.target is GraphView && editorData.canAddNode) {
				#region Add Node
				evt.menu.AppendAction("Add Node", (e) => {
					graph.ShowNodeMenu(clickedPos);
				}, DropdownMenuAction.AlwaysEnabled);
				evt.menu.AppendAction("Add Node (Set)", (e) => {
					graph.ShowNodeMenu(clickedPos, new FilterAttribute() { SetMember = true, VoidType = false }, (node) => {
						NodeEditorUtility.AddNewNode(editorData, null, null, new Vector2(node.editorRect.x, node.editorRect.y), delegate (NodeSetValue n) {
							n.target = new MemberData(node, MemberData.TargetType.ValueNode);
							n.value = MemberData.empty;
						});
						node.editorRect.x -= 150;
						node.editorRect.y -= 100;
					});
				}, DropdownMenuAction.AlwaysEnabled);
				evt.menu.AppendAction("Add Linked Macro", (e) => {
					var macros = uNodeEditorUtility.FindComponentInPrefabs<uNodeMacro>();
					List<ItemSelector.CustomItem> customItems = new List<ItemSelector.CustomItem>();
					for(int i = 0; i < macros.Count; i++) {
						var m = macros[i];
						customItems.Add(new ItemSelector.CustomItem(m.DisplayName, () => {
							CreateLinkedMacro(m, clickedPos);
						}, m.category) {
							tooltip = new GUIContent(m.summary)
						});
					}
					ItemSelector.ShowWindow(null, null, null, null, customItems).ChangePosition(graph.GetMenuPosition()).displayDefaultItem = false;
				}, DropdownMenuAction.AlwaysEnabled);
				#endregion

				#region Event State
				if(editorData.selectedGroup as StateNode) {
					evt.menu.AppendSeparator("");
					#region State
					evt.menu.AppendAction("Add Event/State/OnEnter", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnEnter,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/State/OnExit", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnExit,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Behavior
					evt.menu.AppendAction("Add Event/Behavior/OnDestroy", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnDestroy,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Behavior/OnDisable", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnDisable,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Behavior/OnEnable", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnEnable,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Gameloop
					evt.menu.AppendAction("Add Event/Gameloop/Update", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.Update,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Gameloop/FixedUpdate", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.FixedUpdate,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Gameloop/LateUpdate", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.LateUpdate,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Gameloop/OnGUI", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnGUI,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Game Event
					evt.menu.AppendAction("Add Event/Game Event/OnApplicationFocus", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnApplicationFocus,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Game Event/OnApplicationPause", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnApplicationPause,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Game Event/OnApplicationQuit", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnApplicationQuit,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Physics
					evt.menu.AppendAction("Add Event/Physics/OnCollisionEnter2D", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnCollisionEnter2D,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnCollisionStay", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnCollisionStay,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnCollisionStay2D", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnCollisionStay2D,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnCollisionExit", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnCollisionExit,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnCollisionExit2D", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnCollisionExit2D,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnTriggerEnter", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnTriggerEnter,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnTriggerEnter2D", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnTriggerEnter2D,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnTriggerStay", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnTriggerStay,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnTriggerStay2D", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnTriggerStay2D,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnTriggerExit", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnTriggerExit,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnTriggerExit2D", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnTriggerExit2D,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Mouse
					evt.menu.AppendAction("Add Event/Mouse/OnMouseDown", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnMouseDown,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Mouse/OnMouseDrag", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnMouseDrag,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Mouse/OnMouseEnter", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnMouseEnter,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Mouse/OnMouseOver", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnMouseOver,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Mouse/OnMouseExit", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnMouseExit,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Mouse/OnMouseUp", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnMouseUp,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Mouse/OnMouseUpAsButton", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnMouseUpAsButton,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Transfrom
					evt.menu.AppendAction("Add Event/Transfrom/OnTransformChildrenChanged", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnTransformChildrenChanged,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Transfrom/OnTransformParentChanged", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnTransformParentChanged,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Animator
					evt.menu.AppendAction("Add Event/Animator/OnAnimatorIK", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnAnimatorIK,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Animator/OnAnimatorMove", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnAnimatorMove,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Renderer
					evt.menu.AppendAction("Add Event/Renderer/OnBecameInvisible", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnBecameInvisible,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Renderer/OnBecameVisible", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnBecameVisible,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Renderer/OnCollisionEnter", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnCollisionEnter,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Renderer/OnPostRender", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnPostRender,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Renderer/OnPreCull", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnPreCull,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Renderer/OnPreRender", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnPreRender,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Renderer/OnRenderObject", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnRenderObject,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Renderer/OnWillRenderObject", (e) => {
						NodeEditorUtility.AddNewTransitionNode(editorData.graph,
							"Event",
							StateEventNode.EventType.OnWillRenderObject,
							editorData.selectedGroup.transform,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion
				}
				#endregion

				evt.menu.AppendSeparator("");
				if(editorData.selectedGroup == null && !editorData.selectedRoot && editorData.graph is IStateGraph state && state.canCreateGraph) {//State Graph
					evt.menu.AppendAction("Add State", (e) => {
						NodeEditorUtility.AddNewNode<StateNode>(editorData,
							"State" + editorData.targetStateGraph.eventNodes.Count,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);

					#region Add Event
					evt.menu.AppendAction("Add Event/Start", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.Start,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Update", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.Update,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Custom", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.Custom,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);

					#region Behavior
					evt.menu.AppendAction("Add Event/Behavior/Awake", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.Awake,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Behavior/OnDisable", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnDisable,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Behavior/OnEnable", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnEnable,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Behavior/OnDestroy", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnDestroy,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Gameloop
					evt.menu.AppendAction("Add Event/Gameloop/FixedUpdate", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.FixedUpdate,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Gameloop/LateUpdate", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.LateUpdate,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Gameloop/OnGUI", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnGUI,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Game Event
					evt.menu.AppendAction("Add Event/Game Event/OnApplicationFocus", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnApplicationFocus,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Game Event/OnApplicationPause", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnApplicationPause,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Game Event/OnApplicationQuit", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnApplicationQuit,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Physics
					evt.menu.AppendAction("Add Event/Physics/OnCollisionEnter", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnCollisionEnter,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnCollisionEnter2D", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnCollisionEnter2D,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnCollisionStay", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnCollisionStay,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnCollisionStay2D", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnCollisionStay2D,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnCollisionExit", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnCollisionExit,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnCollisionExit2D", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnCollisionExit2D,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnTriggerEnter", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnTriggerEnter,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnTriggerEnter2D", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnTriggerEnter2D,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnTriggerStay", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnTriggerStay,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnTriggerStay2D", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnTriggerStay2D,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnTriggerExit", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnTriggerExit,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Physics/OnTriggerExit2D", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnTriggerExit2D,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Mouse
					evt.menu.AppendAction("Add Event/Mouse/OnMouseDown", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnMouseDown,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Mouse/OnMouseDrag", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnMouseDrag,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Mouse/OnMouseEnter", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnMouseEnter,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Mouse/OnMouseOver", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnMouseOver,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Mouse/OnMouseExit", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnMouseExit,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Mouse/OnMouseUp", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnMouseUp,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Mouse/OnMouseUpAsButton", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnMouseUpAsButton,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Transfrom
					evt.menu.AppendAction("Add Event/Transfrom/OnTransformChildrenChanged", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnTransformChildrenChanged,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Transfrom/OnTransformParentChanged", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnTransformParentChanged,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Animator
					evt.menu.AppendAction("Add Event/Animator/OnAnimatorIK", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnAnimatorIK,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Animator/OnAnimatorMove", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnAnimatorMove,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Renderer
					evt.menu.AppendAction("Add Event/Renderer/OnBecameInvisible", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnBecameInvisible,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Renderer/OnBecameVisible", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnBecameVisible,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Renderer/OnPostRender", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnPostRender,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Renderer/OnPreCull", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnPreCull,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Renderer/OnPreRender", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnPreRender,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Renderer/OnRenderObject", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnRenderObject,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Add Event/Renderer/OnWillRenderObject", (e) => {
						NodeEditorUtility.AddNewEvent(editorData.graph,
							"Event" + editorData.targetStateGraph.eventNodes.Count,
							EventNode.EventType.OnWillRenderObject,
							clickedPos);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion
					#endregion

					#region Add Region
					evt.menu.AppendAction("Add Region", (e) => {
						Rect rect = editorData.selectedNodes.Count > 0 ?
										NodeEditorUtility.GetNodeRect(editorData.selectedNodes) :
										new Rect(clickedPos.x, clickedPos.y, 200, 130);
						NodeEditorUtility.AddNewNode<NodeRegion>(editorData, clickedPos, (node) => {
							rect.x -= 30;
							rect.y -= 50;
							rect.width += 60;
							rect.height += 70;
							node.editorRect = rect;
							node.nodeColor = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
						});
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Add Notes
					evt.menu.AppendAction("Add Note", (e) => {
						Rect rect = new Rect(clickedPos.x, clickedPos.y, 200, 130);
						NodeEditorUtility.AddNewNode<Nodes.StickyNote>(editorData, clickedPos, (node) => {
							node.gameObject.name = "Title";
							node.comment = "type something here";
							node.editorRect = rect;
						});
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Goto
					{//Goto
						int index = 0;
						bool hasAddSeparator = false;
						if(editorData.nodes != null && editorData.nodes.Count > 0) {
							foreach(Node n in editorData.nodes) {
								if(n == null)
									continue;
								if(n is ISuperNode || n is IMacro) {
									if(!hasAddSeparator) {
										evt.menu.AppendSeparator("");
										hasAddSeparator = true;
									}
									evt.menu.AppendAction("goto/Group/[" + index + "]" + n.gameObject.name, (e) => {
										Frame(new Node[] { n });
									}, DropdownMenuAction.AlwaysEnabled);
									index++;
								}
							}
						}
						if(graph.eventNodes != null && graph.eventNodes.Count > 0) {
							index = 0;
							if(!hasAddSeparator) {
								evt.menu.AppendSeparator("");
							} else {
								evt.menu.AppendSeparator("goto/");
							}
							List<EventNode> method = new List<EventNode>();
							graph.eventNodes.ForEach((item) => {
								if(item is EventNode) {
									method.Add(item as EventNode);
								}
							});
							method.Sort((x, y) => string.Compare(((int)x.eventType).ToString(), ((int)y.eventType).ToString()));
							string lastMType = null;
							foreach(EventNode m in method) {
								if(m == null)
									continue;
								if(lastMType != m.eventType.ToString()) {
									lastMType = m.eventType.ToString();
									evt.menu.AppendAction("goto/" + lastMType, null, DropdownMenuAction.AlwaysDisabled);
								}
								evt.menu.AppendAction("goto/[" + index + "]", (e) => {
									Frame(new NodeComponent[] { m });
								}, DropdownMenuAction.AlwaysEnabled);
								index++;
							}
						}
					}
					#endregion

					#region Place Fit
					if(editorData.selectedGroup != null) {
						if(editorData.selectedGroup is ISuperNode) {
							ISuperNode superNode = editorData.selectedGroup as ISuperNode;
							foreach(var n in superNode.nestedFlowNodes) {
								if(n == null)
									continue;
								UNodeView view;
								if(nodeViewsPerNode.TryGetValue(n, out view)) {
									evt.menu.AppendAction("Place fit nodes", (e) => {
										foreach(var node in superNode.nestedFlowNodes) {
											if(node == null)
												continue;
											UNodeView nView;
											if(nodeViewsPerNode.TryGetValue(node, out nView)) {
												UIElementUtility.PlaceFitNodes(nView);
											}
										}
									}, DropdownMenuAction.AlwaysEnabled);
								}
							}
						}
					} else if(graph.eventNodes != null && graph.eventNodes.Count > 0) {
						List<UNodeView> views = new List<UNodeView>();
						graph.eventNodes.ForEach((item) => {
							if(item is EventNode) {
								if(nodeViewsPerNode.TryGetValue(item, out var nView)) {
									views.Add(nView);
								}
							}
						});
						if(views.Count > 0) {
							evt.menu.AppendAction("Place fit nodes", (e) => {
								for(int i = 0; i < views.Count; i++) {
									UIElementUtility.PlaceFitNodes(views[i]);
								}
							}, DropdownMenuAction.AlwaysEnabled);
						}
					}
					#endregion
				} else {
					#region Add Region
					evt.menu.AppendAction("Add Region", (e) => {
						Rect rect = editorData.selectedNodes.Count > 0 ?
										NodeEditorUtility.GetNodeRect(editorData.selectedNodes) :
										new Rect(clickedPos.x, clickedPos.y, 200, 130);
						NodeEditorUtility.AddNewNode<NodeRegion>(editorData, clickedPos, (node) => {
							rect.x -= 30;
							rect.y -= 50;
							rect.width += 60;
							rect.height += 70;
							node.editorRect = rect;
						});
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Add Notes
					evt.menu.AppendAction("Add Note", (e) => {
						Rect rect = new Rect(clickedPos.x, clickedPos.y, 200, 130);
						NodeEditorUtility.AddNewNode<Nodes.StickyNote>(editorData, clickedPos, (node) => {
							node.editorRect = rect;
						});
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
					#endregion

					#region Goto
					bool hasAddSeparator = false;
					if(editorData.selectedGroup is GroupNode && (editorData.selectedGroup as GroupNode).nodeToExecute) {
						hasAddSeparator = true;
						evt.menu.AppendSeparator("");
						evt.menu.AppendAction("goto/[Start]" + (editorData.selectedGroup as GroupNode).nodeToExecute.gameObject.name, (e) => {
							Frame(new Node[] { (editorData.selectedGroup as GroupNode).nodeToExecute });
							// graph.MoveCanvas(new Vector2((editorData.selectedGroup as GroupNode).nodeToExecute.editorRect.x - 200,
							// 	(editorData.selectedGroup as GroupNode).nodeToExecute.editorRect.y - 200));
						}, DropdownMenuAction.AlwaysEnabled);
					} else if(editorData.selectedRoot && editorData.selectedRoot.startNode) {
						hasAddSeparator = true;
						evt.menu.AppendSeparator("");
						evt.menu.AppendAction("goto/[Start]" + editorData.selectedRoot.startNode.gameObject.name, (e) => {
							Frame(new Node[] { editorData.selectedRoot.startNode });
							// graph.MoveCanvas(new Vector2(editorData.selectedRoot.startNode.editorRect.x - 200,
							// 	editorData.selectedRoot.startNode.editorRect.y - 200));
						}, DropdownMenuAction.AlwaysEnabled);
					}
					if(editorData.nodes != null && editorData.nodes.Count > 0) {
						int index = 0;
						foreach(Node n in editorData.nodes) {
							if(n == null)
								continue;
							if(n is ISuperNode || n is IMacro) {
								if(!hasAddSeparator) {
									evt.menu.AppendSeparator("");
									hasAddSeparator = true;
								}
								evt.menu.AppendAction("goto/Group/[" + index + "]" + n.gameObject.name, (e) => {
									Frame(new Node[] { n });
									// graph.MoveCanvas(new Vector2(n.editorRect.x - 200, n.editorRect.y - 200));
								}, DropdownMenuAction.AlwaysEnabled);
								index++;
							}
						}
					}
					#endregion

					#region Place Fit
					if(editorData.selectedGroup != null) {
						if(editorData.selectedGroup is ISuperNode) {
							ISuperNode superNode = editorData.selectedGroup as ISuperNode;
							foreach(var n in superNode.nestedFlowNodes) {
								if(n == null)
									continue;
								UNodeView view;
								if(nodeViewsPerNode.TryGetValue(n, out view)) {
									evt.menu.AppendAction("Place fit nodes", (e) => {
										foreach(var node in superNode.nestedFlowNodes) {
											if(node == null)
												continue;
											UNodeView nView;
											if(nodeViewsPerNode.TryGetValue(node, out nView)) {
												UIElementUtility.PlaceFitNodes(nView);
											}
										}
									}, DropdownMenuAction.AlwaysEnabled);
								}
							}
						}
					} else if(editorData.selectedRoot != null && editorData.selectedRoot.startNode != null) {
						UNodeView view;
						if(nodeViewsPerNode.TryGetValue(editorData.selectedRoot.startNode, out view)) {
							evt.menu.AppendAction("Place fit nodes", (e) => {
								UIElementUtility.PlaceFitNodes(view);
							}, DropdownMenuAction.AlwaysEnabled);
						}
					}
					#endregion
				}

				if(selection.Count > 0) {
					evt.menu.AppendAction("Selection to macro", (e) => {
						SelectionToMacro(clickedPos);
					}, DropdownMenuAction.AlwaysEnabled);
				}

				evt.menu.AppendAction("Take Screenshot", (e) => {
					CaptureGraphScreenshot();
				}, DropdownMenuAction.AlwaysEnabled);

				#region Graph Commands
				evt.menu.AppendSeparator("");
				var commands = NodeEditorUtility.FindGraphCommands();
				if(commands != null && commands.Count > 0) {
					bool addSeparator = false;
					foreach(var c in commands) {
						c.graph = graph;
						c.mousePositionOnCanvas = clickedPos;
						if(c.IsValid()) {
							if(c.name == "") {
								evt.menu.AppendSeparator("");
							} else {
								evt.menu.AppendAction(c.name, (e) => {
									c.OnClick(position);
								}, DropdownMenuAction.AlwaysEnabled);
							}
							addSeparator = true;
						}
					}
					if(addSeparator) {
						evt.menu.AppendSeparator("");
					}
				}
				#endregion

				evt.menu.AppendSeparator("");
				if(editorData.selectedNodes.Count > 0) {
					evt.menu.AppendAction("Copy", (e) => {
						graph.CopySelectedNode();
					}, DropdownMenuAction.AlwaysEnabled);
				} else {
					evt.menu.AppendAction("Copy", null, DropdownMenuAction.AlwaysDisabled);
				}
				graph.nodeToCopy.RemoveAll(item => item == null);

				if(graph.nodeToCopy.Count > 0) {
					evt.menu.AppendAction("Paste", (e) => {
						graph.PasteNode(clickedPos);
					}, DropdownMenuAction.AlwaysEnabled);
				} else {
					evt.menu.AppendAction("Paste", null, DropdownMenuAction.AlwaysDisabled);
				}
				if(editorData.selectedNodes.Count > 0) {
					evt.menu.AppendAction("Delete", (e) => {
						NodeEditorUtility.RemoveSelectedNode(editorData);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
				} else {
					evt.menu.AppendAction("Delete", null, DropdownMenuAction.AlwaysDisabled);
				}
			}
			#endregion

			#region Node
			if(evt.target is BaseNodeView) {
				var nodeView = evt.target as BaseNodeView;
				if(nodeView.targetNode is Node) {
					Node node = nodeView.targetNode as Node;
					if(node == null)
						return;

					#region Set as start
					if(editorData.selectedGroup is GroupNode && (editorData.selectedGroup as GroupNode).nodeToExecute != node && node.IsFlowNode()) {
						evt.menu.AppendAction("Set as Start", (e) => {
							(editorData.selectedGroup as GroupNode).nodeToExecute = node;
							uNodeGUIUtility.GUIChanged(editorData.selectedGroup);
							graph.GUIChanged();
							FullReload();
						}, DropdownMenuAction.AlwaysEnabled);
					} else if(editorData.selectedGroup == null && editorData.selectedRoot && editorData.selectedRoot != node && node.IsFlowNode()) {
						if(editorData.selectedRoot.startNode != node) {
							evt.menu.AppendAction("Set as Start", (e) => {
								editorData.selectedRoot.startNode = node;
								uNodeGUIUtility.GUIChanged(editorData.selectedRoot);
								graph.GUIChanged();
								FullReload();
							}, DropdownMenuAction.AlwaysEnabled);
						}
					}
					#endregion

					evt.menu.AppendAction("Inspect...", (e) => {
						ActionPopupWindow.ShowWindow(position, () => {
							graph.Select(node);
							CustomInspector.ShowInspector(editorData);
						}, 300, 400);
					}, DropdownMenuAction.AlwaysEnabled);

					if(nodeView is INodeBlock) {
						INodeBlock blockView = nodeView as INodeBlock;
						evt.menu.AppendSeparator("");
						switch(blockView.blockType) {
							case BlockType.Action:
								evt.menu.AppendAction("Add new block", (e) => {
									BlockUtility.ShowAddActionMenu(
										graph.topMousePos,
										(act) => {
											blockView.nodeView.RegisterUndo("");
											blockView.blocks.AddBlock(act);
											blockView.nodeView.MarkRepaint();
											uNodeGUIUtility.GUIChanged(blockView.nodeView.targetNode);
										},
										editorData.graph);
								}, DropdownMenuAction.AlwaysEnabled);
								break;
							case BlockType.Condition:
								evt.menu.AppendAction("Add new block", (e) => {
									BlockUtility.ShowAddEventMenu(
										graph.topMousePos,
										editorData.graph,
										(act) => {
											blockView.nodeView.RegisterUndo("");
											blockView.blocks.AddBlock(act);
											blockView.nodeView.MarkRepaint();
											uNodeGUIUtility.GUIChanged(blockView.nodeView.targetNode);
										});
								}, DropdownMenuAction.AlwaysEnabled);
								evt.menu.AppendAction("Add 'OR' block", (e) => {
									blockView.nodeView.RegisterUndo("");
									blockView.blocks.AddBlock(null, EventActionData.EventType.Or);
									blockView.nodeView.MarkRepaint();
									uNodeGUIUtility.GUIChanged(blockView.nodeView.targetNode);
								}, DropdownMenuAction.AlwaysEnabled);
								break;
							case BlockType.CoroutineAction:
								evt.menu.AppendAction("Add new block", (e) => {
									BlockUtility.ShowAddActionMenu(
									graph.topMousePos,
									(act) => {
										blockView.nodeView.RegisterUndo("");
										blockView.blocks.AddBlock(act);
										blockView.nodeView.MarkRepaint();
										uNodeGUIUtility.GUIChanged(blockView.nodeView.targetNode);
									},
									editorData.graph,
									true);
								}, DropdownMenuAction.AlwaysEnabled);
								break;
						}
						evt.menu.AppendSeparator("");
						if(blockView.blocks != null && blockView.blocks.blocks != null && blockView.blocks.blocks.Count > 0) {
							evt.menu.AppendAction("Expand blocks", (e) => {
								blockView.nodeView.RegisterUndo("");
								foreach(var b in blockView.blocks.blocks) {
									b.expanded = true;
								}
								blockView.nodeView.MarkRepaint();
							}, DropdownMenuAction.AlwaysEnabled);
							evt.menu.AppendAction("Colapse blocks", (e) => {
								blockView.nodeView.RegisterUndo("");
								foreach(var b in blockView.blocks.blocks) {
									b.expanded = false;
								}
								blockView.nodeView.MarkRepaint();
							}, DropdownMenuAction.AlwaysEnabled);
						}
					}

					#region Transition
					if(node as StateNode) {
						evt.menu.AppendSeparator("");
						foreach(TransitionMenu menuItem in NodeEditorUtility.FindTransitionMenu()) {
							object[] eventObject = new object[]{
							menuItem.type,
							menuItem.name,
						};
							evt.menu.AppendAction("Add Transition/" + menuItem.path, (Action<DropdownMenuAction>)((e) => {
								object[] objToArray = e.userData as object[];
								System.Type type = (System.Type)objToArray[0];
								if(node as StateNode) {
									StateNode stateNode = node as StateNode;
									if(stateNode.TransitionEventObject == null) {
										if(uNodeEditorUtility.IsPrefab(node.gameObject)) {
											StateNode n = (StateNode)PrefabUtility.InstantiatePrefab(node);
											n.TransitionEventObject = new GameObject("TransitionEvent");
											n.TransitionEventObject.transform.parent = n.transform;
											uNodeEditorUtility.SavePrefabAsset(n.transform.root.gameObject, (UnityEngine.Object)node.owner);
											UnityEngine.Object.DestroyImmediate(n.transform.root.gameObject, true);
										} else {
											stateNode.TransitionEventObject = new GameObject("TransitionEvent");
											stateNode.TransitionEventObject.transform.parent = node.transform;
										}
									}
									GameObject eventObj = stateNode.TransitionEventObject;
									Component comp = eventObj.AddComponent(type) as Component;
									if(eventObj.GetComponent<TransitionEvent>() != null) {
										TransitionEvent transition = comp as TransitionEvent;
										transition.Name = (string)objToArray[1];
										if(node != null) {
											transition.node = node;
											if(node.owner != null) {
												transition.owner = node.owner;
											} else if(node.transform.parent.parent.GetComponent<uNodeRoot>() != null) {
												node.owner = node.transform.parent.parent.GetComponent<uNodeRoot>();
												transition.owner = node.owner;
											}
										}
										transition.editorPosition = new Rect(stateNode.editorRect.width / 2, (stateNode.editorRect.height / 2) + 50, 0, 0);
									}
									MarkRepaint();
								}
							}), DropdownMenuAction.AlwaysEnabled, eventObject);
						}
						evt.menu.AppendSeparator("");
					}
					#endregion

					#region Node commands
					evt.menu.AppendSeparator("");
					var commands = NodeEditorUtility.FindNodeCommands();
					if(commands != null && commands.Count > 0) {
						bool addSeparator = false;
						foreach(var c in commands) {
							c.graph = graph;
							c.mousePositionOnCanvas = clickedPos;
							if(c.IsValidNode(node)) {
								if(c.name == "") {
									evt.menu.AppendSeparator("");
								} else {
									evt.menu.AppendAction(c.name, (e) => {
										c.OnClick(node, position);
									}, DropdownMenuAction.AlwaysEnabled);
								}
								addSeparator = true;
							}
						}
						if(addSeparator) {
							evt.menu.AppendSeparator("");
						}
					}
					#endregion

					#region MultipurposeNode
					if(node is MultipurposeNode) {
						MultipurposeNode mNode = node as MultipurposeNode;
						if(mNode.target.target.isAssigned) {
							if(mNode.target.target.targetType == MemberData.TargetType.Method) {
								var members = mNode.target.target.GetMembers(false);
								if(members != null && members.Length == 1) {
									var member = members[members.Length - 1];
									BindingFlags flag = BindingFlags.Public;
									if(mNode.target.target.isStatic) {
										flag |= BindingFlags.Static;
									} else {
										flag |= BindingFlags.Instance;
									}
									var memberName = member.Name;
									var mets = member.ReflectedType.GetMember(memberName, flag);
									List<MethodInfo> methods = new List<MethodInfo>();
									foreach(var m in mets) {
										if(m is MethodInfo) {
											methods.Add(m as MethodInfo);
										}
									}
									foreach(var m in methods) {
										evt.menu.AppendAction("Change Methods/" + EditorReflectionUtility.GetOverloadingMethodNames(m), (e) => {
											object[] objs = e.userData as object[];
											MultipurposeNode nod = objs[0] as MultipurposeNode;
											MethodInfo method = objs[1] as MethodInfo;
											if(member != m) {
												if(method.IsGenericMethodDefinition) {
													TypeSelectorWindow.ShowAsNew(graph.topMousePos, new FilterAttribute() { UnityReference = false }, delegate (MemberData[] types) {
														uNodeEditorUtility.RegisterUndo(nod);
														method = method.MakeGenericMethod(types.Select(i => i.Get<Type>()).ToArray());
														MemberData d = new MemberData(method);
														nod.target.target.CopyFrom(d);
														MarkRepaint(nod);
													}, new TypeItem[method.GetGenericArguments().Length]).targetObject = nod;
												} else {
													uNodeEditorUtility.RegisterUndo(nod);
													MemberData d = new MemberData(method);
													nod.target.target.CopyFrom(d);
													uNodeGUIUtility.GUIChanged(nod);
												}
											}
										}, (e) => {
											if(member == m) {
												return DropdownMenuAction.Status.Checked;
											}
											return DropdownMenuAction.Status.Normal;
										}, new object[] { node, m });
									}
								}
							} else if(mNode.target.target.targetType == MemberData.TargetType.Constructor) {
								var members = mNode.target.target.GetMembers(false);
								if(members != null && members.Length == 1) {
									var member = members[members.Length - 1];
									if(member != null) {
										BindingFlags flag = BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
										var ctors = member.ReflectedType.GetConstructors(flag);
										foreach(var m in ctors) {
											if(!EditorReflectionUtility.IsPublicMember(m))
												continue;
											evt.menu.AppendAction("Change Constructors/" + EditorReflectionUtility.GetOverloadingConstructorNames(m), (e) => {
												object[] objs = e.userData as object[];
												MultipurposeNode nod = objs[0] as MultipurposeNode;
												ConstructorInfo ctor = objs[1] as ConstructorInfo;
												if(member != m) {
													uNodeEditorUtility.RegisterUndo(nod);
													MemberData d = new MemberData(ctor);
													nod.target.target.CopyFrom(d);
													uNodeGUIUtility.GUIChanged(nod);
												}
											}, (e) => {
												if(member == m) {
													return DropdownMenuAction.Status.Checked;
												}
												return DropdownMenuAction.Status.Normal;
											}, new object[] { node, m });
										}
									}
								}
							} else if(mNode.target.target.targetType == MemberData.TargetType.uNodeFunction) {
								var root = mNode.target.target.startTarget as uNodeRoot;
								if(root != null && root.Functions != null) {
									var currMethod = mNode.target.target.GetUnityObject() as uNodeFunction;
									var methods = root.Functions.Where(f => f.Name == mNode.target.target.startName);
									var paramTypes = mNode.target.target.ParameterTypes.FirstOrDefault();
									foreach(var m in methods) {
										evt.menu.AppendAction("Change Functions/" + EditorReflectionUtility.GetOverloadingFunctionNames(m), (e) => {
											object[] objs = e.userData as object[];
											MultipurposeNode nod = objs[0] as MultipurposeNode;
											uNodeFunction method = objs[1] as uNodeFunction;
											uNodeEditorUtility.RegisterUndo(nod);
											MemberData d = MemberData.CreateFromValue(method);
											nod.target.target.CopyFrom(d);
											uNodeGUIUtility.GUIChanged(nod);
										}, (e) => {
											if(currMethod == m) {
												return DropdownMenuAction.Status.Checked;
											}
											return DropdownMenuAction.Status.Normal;
										}, new object[] { node, m });
									}
								}
							}
						}
					}
					#endregion

					#region Store instance
					if(node.CanGetValue()) {
						evt.menu.AppendAction("Store instance to variable", (e) => {
							Node val = e.userData as Node;
							Type type = val.ReturnType();
							if(val.owner) {
								Undo.SetCurrentGroupName("Store instance to variable");
								Undo.RegisterFullObjectHierarchyUndo(val.owner, "Store instance to variable");
							}
							VariableData var = uNodeEditorUtility.AddVariable(type, editorData.graph.Variables, editorData.graph);
							NodeEditorUtility.AddNewNode<NodeSetValue>(editorData, null, null, new Vector2(val.editorRect.x + 100 + val.editorRect.width, val.editorRect.y), (nod) => {
								nod.target = new MemberData(var, val.owner);
								nod.value = new MemberData(val, MemberData.TargetType.ValueNode);
							});
							graph.Refresh();
						}, DropdownMenuAction.AlwaysEnabled, node);
						if(editorData.selectedRoot) {
							evt.menu.AppendAction("Store instance to local variable", (e) => {
								Node val = e.userData as Node;
								Type type = val.ReturnType();
								if(val.owner) {
									Undo.SetCurrentGroupName("Store instance to local variable");
									Undo.RegisterFullObjectHierarchyUndo(val.owner, "Store instance to local variable");
								}
								VariableData var = uNodeEditorUtility.AddVariable(type, editorData.selectedRoot.localVariable, editorData.selectedRoot);
								NodeEditorUtility.AddNewNode<NodeSetValue>(editorData, null, null, new Vector2(val.editorRect.x + 100 + val.editorRect.width, val.editorRect.y), (n) => {
									n.target = new MemberData(var, editorData.selectedRoot);
									n.value = new MemberData(val, MemberData.TargetType.ValueNode);
								});
								graph.Refresh();
							}, DropdownMenuAction.AlwaysEnabled, node);
						}
						evt.menu.AppendSeparator("");
					}
					#endregion

					#region Split node
					if(node is MultipurposeNode) {
						bool flag = false;
						MultipurposeNode nod = node as MultipurposeNode;
						if(nod.target.target != null && nod.target.target.isAssigned && nod.target.target.isDeepTarget) {
							evt.menu.AppendAction("Split Node", (e) => {
								int choice = EditorUtility.DisplayDialogComplex("", "Did you want to replace the node?", "Yes", "No", "Cancel");
								if(choice != 2) {
									Undo.RegisterFullObjectHierarchyUndo(editorData.graph, "Split Node");
									var members = nod.target.target.GetMembers(false);
									if(members != null && members.Length > 0) {
										List<Node> nodes = new List<Node>();
										int index = 0;
										if(nod.target.target.IsTargetingUNode) {
											NodeEditorUtility.AddNewNode<MultipurposeNode>(
												editorData, clickedPos,
												n => {
													n.target.target = new MemberData(
														nod.target.target.startName,
														nod.target.target.startType,
														nod.target.target.instance,
														nod.target.target.targetType);
													if(n.target.target.SerializedItems.Length - 1 > 0) {
														n.target.parameters = new MemberData[n.target.target.SerializedItems.Length - 1];
														for(int i = 0; i < n.target.parameters.Length; i++) {
															if(nod.target.parameters.Length > i + index) {
																n.target.parameters[i] = nod.target.parameters[i + index];
															}
														}
														index += n.target.target.SerializedItems.Length - 1;
													}
													MemberDataUtility.UpdateMultipurposeMember(n.target);
													nodes.Add(n);
												});
										}
										foreach(var member in members) {
											NodeEditorUtility.AddNewNode<MultipurposeNode>(
												editorData, clickedPos,
												n => {
													n.target.target = new MemberData(member);
													if(n.target.target.SerializedItems.Length - 1 > 0) {
														n.target.parameters = new MemberData[n.target.target.SerializedItems.Length - 1];
														for(int i = 0; i < n.target.parameters.Length; i++) {
															if(nod.target.parameters.Length > i + index) {
																n.target.parameters[i] = nod.target.parameters[i + index];
															}
														}
														index += n.target.target.SerializedItems.Length - 1;
													}
													if(nodes.Count > 0) {
														n.target.target.instance = new MemberData(nodes.Last(), MemberData.TargetType.ValueNode);
														if(member == members.Last()) {
															n.onFinished = nod.onFinished;
														}
													} else {
														n.target.target.instance = nod.target.target.instance;
													}
													MemberDataUtility.UpdateMultipurposeMember(n.target);
													nodes.Add(n);
												});
										}
										for(int i = 0; i < nodes.Count; i++) {
											nodes[(nodes.Count - i - 1)].editorRect = node.editorRect;
											nodes[(nodes.Count - i - 1)].editorRect.x -= i * (nodes[i].editorRect.width + 50);
											nodes[(nodes.Count - i - 1)].editorRect.y += i * (nodes[i].editorRect.height + 50);
										}
										if(choice == 0) {
											var nodesConnect = NodeEditorUtility.FindNodeConnectedToNode(nod, editorData.nodes);
											if(nodesConnect != null) {
												foreach(var n in nodesConnect) {
													if(n is Node) {
														Node nodeC = n as Node;
														if(nodeC is StateNode) {
															StateNode eventNode = nodeC as StateNode;
															TransitionEvent[] TE = eventNode.GetTransitions();
															foreach(TransitionEvent t in TE) {
																if(t.GetTargetNode() == node) {
																	t.target = MemberData.CreateConnection(nodes[0], true);
																}
															}
														}
													} else if(n is BaseEventNode) {
														BaseEventNode nodeC = n as BaseEventNode;
														var flows = nodeC.GetFlows();
														for(int i = 0; i < flows.Count; i++) {
															if(flows[i].GetTargetNode() == node) {
																flows[i] = new MemberData(nodes[0], MemberData.TargetType.FlowNode);
															}
														}
													}
													Func<object, bool> validation = delegate (object o) {
														if(o is MemberData) {
															MemberData member = o as MemberData;
															if(member.targetType == MemberData.TargetType.FlowNode ||
																member.targetType == MemberData.TargetType.ValueNode) {
																Node N = member.GetInstance() as Node;
																if(N == node) {
																	member.instance = nodes.Last();
																	return true;
																}
															}
														}
														return false;
													};
													AnalizerUtility.AnalizeObject(n, validation, (obj, field, type, val) => {
														field.SetValueOptimized(obj, val);
													});

												}
											}
											NodeEditorUtility.RemoveNode(editorData, node);
										}
										graph.Refresh();
									}
								}
								return;
							}, DropdownMenuAction.AlwaysEnabled);
							flag = true;
						}
						if(flag) {
							evt.menu.AppendSeparator("");
						}
					}
					#endregion

					MonoScript ms = uNodeEditorUtility.GetMonoScript(node);
					if(ms != null) {
						evt.menu.AppendAction("Find Script", (e) => {
							EditorGUIUtility.PingObject(ms);
						}, DropdownMenuAction.AlwaysEnabled);
						evt.menu.AppendAction("Edit Script", (e) => {
							AssetDatabase.OpenAsset(ms);
						}, DropdownMenuAction.AlwaysEnabled);
					}
					MonoScript ec = uNodeEditorUtility.GetMonoScript(nodeView);
					if(ec != null) {
						evt.menu.AppendAction("Edit Editor Script", (e) => {
							AssetDatabase.OpenAsset(ec);
						}, DropdownMenuAction.AlwaysEnabled);
					}
					if(!graph.preferenceData.hideChildObject) {
						evt.menu.AppendAction("Find GameObject", (e) => {
							EditorGUIUtility.PingObject(node);
						}, DropdownMenuAction.AlwaysEnabled);
					}
					if(!uNodeUtility.HasBreakpoint(uNodeUtility.GetObjectID(node))) {
						evt.menu.AppendAction("Add Breakpoint", (e) => {
							uNodeUtility.AddBreakpoint(uNodeUtility.GetObjectID(node));
							MarkRepaint(node);
							uNodeGUIUtility.GUIChanged(node);
						}, DropdownMenuAction.AlwaysEnabled);
					} else {
						evt.menu.AppendAction("Remove Breakpoint", (e) => {
							uNodeUtility.RemoveBreakpoint(uNodeUtility.GetObjectID(node));
							MarkRepaint(node);
							uNodeGUIUtility.GUIChanged(node);
						}, DropdownMenuAction.AlwaysEnabled);
					}
					evt.menu.AppendSeparator("");
					if(nodeView.inputPorts.Any(p => p.connected && !p.IsProxy()) || nodeView.outputPorts.Any(p => p.connected && !p.IsProxy())) {
						evt.menu.AppendAction("Place fit nodes", (e) => {
							UIElementUtility.PlaceFitNodes(nodeView);
						}, DropdownMenuAction.AlwaysEnabled);
						//if(nodeView.outputPorts.Any(p => p.connected && p.orientation == Orientation.Vertical)) {
						//	evt.menu.AppendAction("Place fit flow nodes", (e) => {
						//		UIElementUtility.PlaceFitNodes(nodeView);
						//	}, DropdownMenuAction.AlwaysEnabled);
						//}
						//if(nodeView.inputPorts.Any(p => p.connected && p.orientation == Orientation.Horizontal && !p.IsProxy()) ||
						//	nodeView.outputPorts.Any(p => p.connected && p.orientation == Orientation.Horizontal && !p.IsProxy())) {
						//	evt.menu.AppendAction("Place fit value nodes", (e) => {
						//		UIElementUtility.PlaceFitNodes(nodeView);
						//	}, DropdownMenuAction.AlwaysEnabled);
						//}
					}
					if(selection.Count > 0) {
						evt.menu.AppendAction("Selection to macro", (e) => {
							SelectionToMacro(clickedPos);
						}, DropdownMenuAction.AlwaysEnabled);
					}
					{
						bool hasConnectedNode = false;
						if(node as StateNode) {
							StateNode eventNode = node as StateNode;
							TransitionEvent[] TE = eventNode.GetTransitions();
							foreach(TransitionEvent T in TE) {
								if(T.GetTargetNode() != null) {
									hasConnectedNode = true;
									break;
								}
							}
						}
						if(!hasConnectedNode) {
							AnalizerUtility.AnalizeObject(node, delegate (object o) {
								if(o is MemberData) {
									MemberData member = o as MemberData;
									if(member.targetType == MemberData.TargetType.FlowNode ||
										member.targetType == MemberData.TargetType.ValueNode) {
										if(member.isAssigned && member.instance is Node) {
											hasConnectedNode = true;
											return true;
										}
									}
								}
								return false;
							});
						}
						if(hasConnectedNode) {
							evt.menu.AppendSeparator("");
							evt.menu.AppendAction("Select Connected Node", (e) => {
								graph.SelectConnectedNode(node, false, (n) => {
									UNodeView view;
									if(nodeViewsPerNode.TryGetValue(n, out view)) {
										base.AddToSelection(view);
									}
								});
							}, DropdownMenuAction.AlwaysEnabled);
							evt.menu.AppendAction("Select All Connected Node", (e) => {
								graph.SelectConnectedNode(node, true, (n) => {
									UNodeView view;
									if(nodeViewsPerNode.TryGetValue(n, out view)) {
										base.AddToSelection(view);
									}
								});
							}, DropdownMenuAction.AlwaysEnabled);
						}
					}
					evt.menu.AppendSeparator("");
					evt.menu.AppendAction("Copy", (e) => {
						graph.CopyNode(node);
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Remove", (e) => {
						OnNodeRemoved(nodeView);
						NodeEditorUtility.RemoveNode(editorData, node);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);

				} else if(nodeView.targetNode is EventNode) {
					var node = nodeView.targetNode as EventNode;
					if(node == null)
						return;
					evt.menu.AppendAction("Inspect...", (e) => {
						ActionPopupWindow.ShowWindow(position, () => {
							graph.Select(node);
							CustomInspector.ShowInspector(editorData);
						}, 300, 400);
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendSeparator("");
					MonoScript ms = uNodeEditorUtility.GetMonoScript(node);
					if(ms != null) {
						evt.menu.AppendAction("Find Script", (e) => {
							EditorGUIUtility.PingObject(ms);
						}, DropdownMenuAction.AlwaysEnabled);
						evt.menu.AppendAction("Edit Script", (e) => {
							AssetDatabase.OpenAsset(ms);
						}, DropdownMenuAction.AlwaysEnabled);
					}
					MonoScript ec = uNodeEditorUtility.GetMonoScript(nodeView);
					if(ec != null) {
						evt.menu.AppendAction("Edit Editor Script", (e) => {
							AssetDatabase.OpenAsset(ec);
						}, DropdownMenuAction.AlwaysEnabled);
					}
					if(!graph.preferenceData.hideChildObject) {
						evt.menu.AppendAction("Find GameObject", (e) => {
							EditorGUIUtility.PingObject(node);
						}, DropdownMenuAction.AlwaysEnabled);
					}
					evt.menu.AppendSeparator("");
					evt.menu.AppendAction("Copy", (e) => {
						graph.CopyNode(node);
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Remove", (e) => {
						OnNodeRemoved(nodeView);
						NodeEditorUtility.RemoveNode(editorData, node);
						graph.Refresh();
					}, DropdownMenuAction.AlwaysEnabled);
				}
			}
			#endregion

			#region Block
			if(evt.target is BlockView) {
				BlockView blockView = evt.target as BlockView;
				if(blockView.data != null && blockView.data.block != null) {
					evt.menu.AppendAction("Edit", (e) => {
						FieldsEditorWindow window = FieldsEditorWindow.ShowWindow();
						window.titleContent = new GUIContent(blockView.data.block.GetType().Name);
						window.targetField = blockView.data.block;
						window.targetObject = blockView.ownerNode.targetNode;
					}, DropdownMenuAction.AlwaysEnabled);
				}
				evt.menu.AppendSeparator("");
				switch(blockView.owner.blockType) {
					case BlockType.Action:
						evt.menu.AppendAction("Insert new block above", (e) => {
							BlockUtility.ShowAddActionMenu(
								graph.topMousePos,
								(act) => {
									int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
									blockView.RegisterUndo("");
									blockView.owner.blocks.InsertBlock(index, act);
									blockView.ownerNode.MarkRepaint();
									uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
								},
								editorData.graph);
						}, DropdownMenuAction.AlwaysEnabled);
						evt.menu.AppendAction("Insert new block below", (e) => {
							BlockUtility.ShowAddActionMenu(
								graph.topMousePos,
								(act) => {
									int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
									blockView.RegisterUndo("");
									blockView.owner.blocks.InsertBlock(index + 1, act);
									blockView.ownerNode.MarkRepaint();
									uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
								},
								editorData.graph);
						}, DropdownMenuAction.AlwaysEnabled);
						break;
					case BlockType.Condition:
						evt.menu.AppendAction("Insert new block above", (e) => {
							BlockUtility.ShowAddEventMenu(
								graph.topMousePos,
								editorData.graph,
								(act) => {
									int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
									blockView.RegisterUndo("");
									blockView.owner.blocks.InsertBlock(index, act);
									blockView.ownerNode.MarkRepaint();
									uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
								});
						}, DropdownMenuAction.AlwaysEnabled);
						evt.menu.AppendAction("Insert new block below", (e) => {
							BlockUtility.ShowAddActionMenu(
							graph.topMousePos,
							(act) => {
								int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
								blockView.RegisterUndo("");
								blockView.owner.blocks.InsertBlock(index + 1, act);
								blockView.ownerNode.MarkRepaint();
								uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
							},
							editorData.graph,
							true);
						}, DropdownMenuAction.AlwaysEnabled);
						evt.menu.AppendSeparator("");
						evt.menu.AppendAction("Insert 'OR' block above", (e) => {
							int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
							blockView.RegisterUndo("");
							blockView.owner.blocks.InsertBlock(index, null, EventActionData.EventType.Or);
							blockView.ownerNode.MarkRepaint();
							uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
						}, DropdownMenuAction.AlwaysEnabled);
						evt.menu.AppendAction("Insert 'OR' block below", (e) => {
							int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
							blockView.RegisterUndo("");
							blockView.owner.blocks.InsertBlock(index + 1, null, EventActionData.EventType.Or);
							blockView.ownerNode.MarkRepaint();
							uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
						}, DropdownMenuAction.AlwaysEnabled);
						break;
					case BlockType.CoroutineAction:
						evt.menu.AppendAction("Insert new block above", (e) => {
							BlockUtility.ShowAddActionMenu(
							graph.topMousePos,
							(act) => {
								int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
								blockView.RegisterUndo("");
								blockView.owner.blocks.InsertBlock(index, act);
								blockView.ownerNode.MarkRepaint();
								uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
							},
							editorData.graph,
							true);
						}, DropdownMenuAction.AlwaysEnabled);
						evt.menu.AppendAction("Insert new block below", (e) => {
							BlockUtility.ShowAddActionMenu(
							graph.topMousePos,
							(act) => {
								int index = blockView.owner.blocks.blocks.IndexOf(blockView.data);
								blockView.RegisterUndo("");
								blockView.owner.blocks.InsertBlock(index + 1, act);
								blockView.ownerNode.MarkRepaint();
								uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
							},
							editorData.graph,
							true);
						}, DropdownMenuAction.AlwaysEnabled);
						break;
				}
				evt.menu.AppendSeparator("");
				MonoScript ms = uNodeEditorUtility.GetMonoScript(blockView.data.block);
				if(ms != null) {
					evt.menu.AppendAction("Find Script", (e) => {
						EditorGUIUtility.PingObject(ms);
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Edit Script", (e) => {
						AssetDatabase.OpenAsset(ms);
					}, DropdownMenuAction.AlwaysEnabled);
				}
				var es = uNodeEditorUtility.GetMonoScript(blockView);
				if(es != null) {
					evt.menu.AppendAction("Edit Editor Script", (e) => {
						AssetDatabase.OpenAsset(es);
					}, DropdownMenuAction.AlwaysEnabled);
				}
				evt.menu.AppendSeparator("");
				evt.menu.AppendAction("Duplicate", (e) => {
					blockView.RegisterUndo("Duplicate");
					int idx = blockView.owner.blocks.blocks.IndexOf(blockView.data);
					var data = SerializerUtility.Duplicate(blockView.data);
					blockView.owner.blocks.blocks.Insert(idx, data);
					blockView.ownerNode.MarkRepaint();
					uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
				}, DropdownMenuAction.AlwaysEnabled);
				evt.menu.AppendAction("Remove", (e) => {
					OnNodeRemoved(blockView);
					blockView.owner.RemoveBlock(blockView);
					uNodeGUIUtility.GUIChanged(blockView.ownerNode.targetNode);
				}, DropdownMenuAction.AlwaysEnabled);
			}
			#endregion

			#region Transition
			if(evt.target is TransitionView) {
				TransitionView view = evt.target as TransitionView;

				MonoScript ms = uNodeEditorUtility.GetMonoScript(view.transition);
				if(ms != null) {
					evt.menu.AppendAction("Find Script", (e) => {
						EditorGUIUtility.PingObject(ms);
					}, DropdownMenuAction.AlwaysEnabled);
					evt.menu.AppendAction("Edit Script", (e) => {
						AssetDatabase.OpenAsset(ms);
					}, DropdownMenuAction.AlwaysEnabled);
				}
				MonoScript ec = uNodeEditorUtility.GetMonoScript(view);
				if(ec != null) {
					evt.menu.AppendAction("Edit Editor Script", (e) => {
						AssetDatabase.OpenAsset(ec);
					}, DropdownMenuAction.AlwaysEnabled);
				}
				if(!graph.preferenceData.hideChildObject) {
					evt.menu.AppendAction("Find GameObject", (e) => {
						EditorGUIUtility.PingObject(view.transition);
					}, DropdownMenuAction.AlwaysEnabled);
				}
				evt.menu.AppendSeparator("");
				evt.menu.AppendAction("Remove", (e) => {
					OnNodeRemoved(view);
					NodeEditorUtility.RemoveTransition(view.transition);
					graph.Refresh();
				}, DropdownMenuAction.AlwaysEnabled);
			}
			#endregion

			#region Edge
			if(evt.target is EdgeView) {
				EdgeView edge = evt.target as EdgeView;
				evt.menu.AppendAction("Convert to proxy", (e) => {
					MemberData member = null;
					if(edge.input.orientation == Orientation.Horizontal) {
						if(edge.input.direction == Direction.Input) {
							PortView port = edge.input as PortView;
							member = port.portData.GetPortValue() as MemberData;
							if(member != null) {
								port.owner.RegisterUndo("Convert to proxy");
								member.controlPoints = new ControlPoint[] { new ControlPoint() };
								port.portData.OnValueChanged(member);
							}
						}
					} else {
						PortView port = edge.GetSenderPort();
						if(port != null) {
							member = port.portData.GetPortValue() as MemberData;
							if(member != null) {
								port.owner.RegisterUndo("Convert to proxy");
								member.controlPoints = new ControlPoint[] { new ControlPoint() };
								port.portData.OnValueChanged(member);
							}
						}
					}
					if(member != null) {
						graph.GUIChanged();
						edge.GetSenderPort()?.owner.MarkRepaint();
						edge.GetReceiverPort()?.owner.MarkRepaint();
						// graph.Refresh();
					}
				}, DropdownMenuAction.AlwaysEnabled);
				evt.menu.AppendAction("Remove", (e) => {
					PortView port = null;
					if(edge.input.orientation == Orientation.Horizontal) {
						port = edge.Input;
					} else {
						port = edge.Output;
					}
					port.owner.RegisterUndo("");
					port.ResetPortValue();
					MarkRepaint(edge.Input.owner, edge.Output.owner);
				}, DropdownMenuAction.AlwaysEnabled);
			}
			#endregion

			#region Ports
			if(evt.target is PortView) {
				PortView port = evt.target as PortView;
				if(port.orientation == Orientation.Horizontal) {//Value port
					if(port.direction == Direction.Input) {//Input port
						MemberData member = port.portData.GetPortValue() as MemberData;
						if(member != null) {
							var commands = NodeEditorUtility.FindPortCommands();
							if(commands != null && commands.Count > 0) {
								PortCommandData commandData = new PortCommandData() {
									member = member,
									portName = port.GetName(),
									portType = port.GetPortType(),
								};
								var m = member;
								foreach(var c in commands) {
									c.graph = NodeGraph.openedGraph;
									c.mousePositionOnCanvas = clickedPos;
									c.filter = port.portData.GetFilter();
									if(c.ValidPort().HasFlags(PortMenuCommand.PortKind.ValueInput) && c.IsValidPort(port.owner.targetNode as Node, commandData)) {
										evt.menu.AppendAction(c.name, (e) => {
											object[] objs = e.userData as object[];
											c.OnClick(objs[0] as Node, objs[1] as PortCommandData, position);
											port.portData.OnValueChanged(m);
											MarkRepaint(objs[0] as Node);
											// c.graph.Refresh();
										}, DropdownMenuAction.AlwaysEnabled, new object[] { port.owner.targetNode, commandData });
									}
								}
								evt.menu.AppendSeparator();
							}
							evt.menu.AppendAction("Reset", (e) => {
								port.owner.RegisterUndo("Reset");
								var type = port.portData.GetPortType();
								object val;
								if(type != null && ReflectionUtils.CanCreateInstance(type) && !port.portData.GetFilter().SetMember) {
									val = new MemberData(ReflectionUtils.CreateInstance(type));
								} else if(type is RuntimeType) {
									val = MemberData.CreateFromValue(null, type);
								} else {
									val = MemberData.none;
								}
								port.portData.OnValueChanged(val);
								MarkRepaint(port.owner.targetNode);
							}, DropdownMenuAction.AlwaysEnabled);
						}
					} else if(port.GetNode() is Node) {
						//Output port
						var commands = NodeEditorUtility.FindPortCommands();
						if(commands != null && commands.Count > 0) {
							PortCommandData commandData = new PortCommandData() {
								portType = port.portType,
								portName = port.GetPortID() == UGraphView.SelfPortID ? UGraphView.SelfPortID : port.GetName(),
								getConnection = port.portData.GetConnection,
							};
							foreach(var c in commands) {
								c.graph = NodeGraph.openedGraph;
								c.mousePositionOnCanvas = clickedPos;
								c.filter = port.portData.GetFilter();
								if(c.ValidPort().HasFlags(PortMenuCommand.PortKind.ValueOutput) && c.IsValidPort(port.owner.targetNode as Node, commandData)) {
									evt.menu.AppendAction(c.name, (e) => {
										object[] objs = e.userData as object[];
										c.OnClick(objs[0] as Node, objs[1] as PortCommandData, position);
										MarkRepaint(objs[0] as Node);
										// c.graph.Refresh();
									}, DropdownMenuAction.AlwaysEnabled, new object[] { port.owner.targetNode, commandData });
								}
							}
						}
					}
				} else {//Flow port
					if(port.direction == Direction.Output) {
						MemberData member = port.portData.GetPortValue() as MemberData;
						if(member != null) {
							Node node = port.owner.targetNode as Node;
							if(node != null) {
								var commands = NodeEditorUtility.FindPortCommands();
								if(commands != null && commands.Count > 0) {
									PortCommandData commandData = new PortCommandData() {
										portName = port.GetName(),
										member = member,
									};
									foreach(var c in commands) {
										c.graph = NodeGraph.openedGraph;
										if(c.ValidPort().HasFlags(PortMenuCommand.PortKind.FlowOutput) && c.IsValidPort(node, commandData)) {
											evt.menu.AppendAction(c.name, (e) => {
												object[] objs = e.userData as object[];
												c.OnClick(objs[0] as Node, objs[1] as PortCommandData, clickedPos);
												MarkRepaint(objs[0] as Node);
												// c.graph.Refresh();
											}, DropdownMenuAction.AlwaysEnabled, new object[] { port.owner.targetNode, commandData });
										}
									}
								}
							}
							evt.menu.AppendSeparator();
							evt.menu.AppendAction("Reset", (e) => {
								port.owner.RegisterUndo("Reset");
								port.ResetPortValue();
								var edges = port.GetEdges();
								foreach(var edge in edges) {
									if(edge.input != null) {
										PortView p = edge.input as PortView;
										p.owner?.MarkRepaint();
									}
									if(edge.output != null) {
										PortView p = edge.output as PortView;
										p.owner?.MarkRepaint();
									}
								}
							}, DropdownMenuAction.AlwaysEnabled);
						}
					} else {
						var commands = NodeEditorUtility.FindPortCommands();
						if(commands != null && commands.Count > 0) {
							PortCommandData commandData = new PortCommandData() {
								portType = port.portType,
								portName = port.GetName(),
								getConnection = port.portData.GetConnection,
							};
							foreach(var c in commands) {
								c.graph = NodeGraph.openedGraph;
								c.mousePositionOnCanvas = clickedPos;
								c.filter = port.portData.GetFilter();
								if(c.ValidPort().HasFlags(PortMenuCommand.PortKind.FlowInput) && c.IsValidPort(port.owner.targetNode as Node, commandData)) {
									evt.menu.AppendAction(c.name, (e) => {
										object[] objs = e.userData as object[];
										c.OnClick(objs[0] as Node, objs[1] as PortCommandData, position);
										MarkRepaint(objs[0] as Node);
										// c.graph.Refresh();
									}, DropdownMenuAction.AlwaysEnabled, new object[] { port.owner.targetNode, commandData });
								}
							}
						}
						var edges = port.GetEdges();
						if(edges != null && edges.Count > 0 && edges.Any(e => e.enabledSelf)) {
							evt.menu.AppendAction("Disconnect all", (e) => {
								port.owner.RegisterUndo("Disconnect all");
								foreach(var edge in edges) {
									if(!edge.isValid)
										continue;
									var inPort = edge.input as PortView;
									var outPort = edge.output as PortView;
									if(inPort != null && outPort != null) {
										if(inPort.orientation == Orientation.Horizontal) {
											inPort.ResetPortValue();
										} else if(outPort.orientation == Orientation.Vertical) {
											outPort.ResetPortValue();
										}
										inPort.owner.MarkRepaint();
										outPort.owner.MarkRepaint();
									}
								}
							}, DropdownMenuAction.AlwaysEnabled);
						}
					}
				}
			}
			#endregion
		}

		void KeyDownCallback(KeyDownEvent e) {
			if(e.keyCode == KeyCode.S) {

				// e.StopPropagation();
			} else if(e.altKey && e.keyCode == KeyCode.Space) {
				// graph.window.maximized = !graph.window.maximized;
			} else if(e.keyCode == KeyCode.F10) {
				if(editorData.graphSystem == null || editorData.graphSystem.allowCompileToScript) {
					graph.window.GenerateSource();
				}
			} else if(e.keyCode == KeyCode.F9) {
				if(editorData.graphSystem == null || editorData.graphSystem.allowPreviewScript) {
					graph.window.PreviewSource();
				}
			} else if(e.keyCode == KeyCode.F5) {
				uNodeEditor.window?.Refresh(true);
			}
		}

		//This will ensure after the node removed, the conencted nodes will be refreshed.
		void OnNodeRemoved(NodeView view) {
			if(view is UNodeView) {
				var node = view as UNodeView;
				foreach(var p in node.inputPorts) {
					if(p == null)
						continue;
					var connections = p.GetConnectedNodes();
					if(connections.Count > 0) {
						MarkRepaint(connections.ToArray());
					}
				}
				foreach(var p in node.outputPorts) {
					if(p == null)
						continue;
					var connections = p.GetConnectedNodes();
					if(connections.Count > 0) {
						MarkRepaint(connections.ToArray());
					}
				}
			} else if(view is BlockView) {
				var block = view as BlockView;
				block.ownerNode.MarkRepaint();
				foreach(var p in block.portViews) {
					if(p == null)
						continue;
					var connections = p.GetConnectedNodes();
					if(connections.Count > 0) {
						MarkRepaint(connections.ToArray());
					}
				}
			}
		}

		void OnCreateNode(NodeCreationContext ctx) {
			graph.Repaint();
			Vector2 point = graph.window.rootVisualElement.ChangeCoordinatesTo(
				contentViewContainer,
				graph.topMousePos);

			INodeBlock blockView = nodeViews.Select(view => view as INodeBlock).FirstOrDefault(view => view != null && view.nodeView.GetPosition().Contains(point));
			if(blockView == null) {
				blockView = transitionViewMaps.Select(pair => pair.Value as INodeBlock).FirstOrDefault(view => view != null && view.nodeView.GetPosition().Contains(point));
			}
			if(blockView != null) {
				switch(blockView.blockType) {
					case BlockType.Action:
						BlockUtility.ShowAddActionMenu(
							ctx.screenMousePosition,
							(act) => {
								blockView.nodeView.RegisterUndo("");
								blockView.blocks.AddBlock(act);
								blockView.nodeView.MarkRepaint();
								uNodeGUIUtility.GUIChanged(blockView.nodeView.targetNode);
							},
							editorData.selectedGroup as UnityEngine.Object ??
							editorData.selectedRoot as UnityEngine.Object ??
							editorData.graph as UnityEngine.Object);
						break;
					case BlockType.Condition:
						BlockUtility.ShowAddEventMenu(
							ctx.screenMousePosition,
							editorData.selectedGroup as UnityEngine.Object ??
							editorData.selectedRoot as UnityEngine.Object ??
							editorData.graph as UnityEngine.Object,
							(act) => {
								blockView.nodeView.RegisterUndo("");
								blockView.blocks.AddBlock(act);
								blockView.nodeView.MarkRepaint();
								uNodeGUIUtility.GUIChanged(blockView.nodeView.targetNode);
							});
						break;
					case BlockType.CoroutineAction:
						BlockUtility.ShowAddActionMenu(
							ctx.screenMousePosition,
							(act) => {
								blockView.nodeView.RegisterUndo("");
								blockView.blocks.AddBlock(act);
								blockView.nodeView.MarkRepaint();
								uNodeGUIUtility.GUIChanged(blockView.nodeView.targetNode);
							},
							editorData.selectedGroup as UnityEngine.Object ??
							editorData.selectedRoot as UnityEngine.Object ??
							editorData.graph as UnityEngine.Object,
							true);
						break;
				}
			} else {
				var nodeView = nodeViews.Where(view => !(view is RegionNodeView)).FirstOrDefault(view => view != null && view.GetPosition().Contains(point));
				if(nodeView == null && editorData.canAddNode) {
					IEnumerable<string> namespaces = null;
					if(editorData.graphData != null) {
						namespaces = editorData.graphData.GetNamespaces();
					}
					CommandWindow.CreateWindow(Vector2.zero, (items) => {
						var nodes = CompletionEvaluator.CompletionsToGraphs(CompletionEvaluator.SimplifyCompletions(items), editorData, point);
						if(nodes != null && nodes.Count > 0) {
							graph.Refresh();
							return true;
						}
						return false;
					}, new CompletionEvaluator.CompletionSetting() {
						owner = editorData.currentCanvas,
						namespaces = namespaces,
						allowExpression = true,
						allowStatement = true,
						allowSymbolKeyword = true,
					}).ChangePosition(ctx.screenMousePosition);
				}
			}
		}

		public override Rect CalculateRectToFitAll(VisualElement container) {
			Rect rectToFit = container.layout;
			bool reachedFirstChild = false;
			graphElements.ForEach(delegate (GraphElement ge) {
				if(ge is NodeView) {
					if(!reachedFirstChild) {
						rectToFit = ge.ChangeCoordinatesTo(contentViewContainer, new Rect(0, 0, ge.layout.width, ge.layout.height));
						reachedFirstChild = true;
					} else {
						rectToFit = RectUtils.Encompass(rectToFit, ge.ChangeCoordinatesTo(contentViewContainer, new Rect(0, 0, ge.layout.width, ge.layout.height)));
					}
				}
			});
			return rectToFit;
		}

		public Rect CalculateVisibleGraphRect() {
			Rect contentRect = contentViewContainer.layout;
			bool reachedFirstChild = false;
			nodeViews.ForEach((node) => {
				Rect nodeRect = node.ChangeCoordinatesTo(contentViewContainer, new Rect(0, 0, node.layout.width, node.layout.height));
				nodeRect.xMin -= 10;
				nodeRect.xMax += 10;
				nodeRect.yMin -= 20;
				nodeRect.yMax += 20;
				if(node.inputPorts.Count > 0) {
					float inputWidth = 0;
					foreach(var p in node.inputPorts) {
						if(p != null && p.orientation == Orientation.Horizontal && !p.connected) {
							inputWidth = Mathf.Max(p.contentContainer.layout.width, inputWidth);
							break;
						}
					}
					nodeRect.xMin -= inputWidth;
				}
				if(!reachedFirstChild) {
					contentRect = nodeRect;
					reachedFirstChild = true;
				} else {
					contentRect = RectUtils.Encompass(contentRect, nodeRect);
				}
			});
			return contentRect;
		}
		#endregion

		#region ReloadView
		void ReloadView() {
			if(uNodePreference.GetPreference().forceReloadGraph) {
				_fullReload = true;
				cachedNodeMap.Clear();
			} else if(_reloadedGraphScope != editorData.currentCanvas) {
				_reloadedGraphScope = editorData.currentCanvas;
				if(!_fullReload) {
					//Ensure to remove all node views when the graph scope is different
					RemoveNodeViews(false);
				}
			}
			//{//Update IMGUI Handler
			//	graphIMGUIHandler.Clear();
			//	if(_iMGUIContainer != null) {
			//		_iMGUIContainer.RemoveFromHierarchy();
			//		_iMGUIContainer = null;
			//	}
			//}
			//Ensure we get the up to date datas
			editorData.Refresh();
			// System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
			// watch.Start();
			//Remove all edges
			RemoveEdges();
			if(_fullReload) {
				// Remove everything node view
				RemoveNodeViews(true);
			} else {
				foreach(var nodeView in nodeViews) {
					if(nodeView.targetNode == null) {
						RemoveElement(nodeView);
						cachedNodeMap.Remove(nodeView.targetNode);
						nodeViewsPerNode.Remove(nodeView.targetNode);
					}
				}
				nodeViews.RemoveAll(n => n.targetNode == null);
				List<TransitionEvent> removedTransitions = new List<TransitionEvent>();
				foreach(var pair in transitionViewMaps) {
					if(pair.Key == null) {
						RemoveElement(pair.Value);
						cachedNodeMap.Remove(pair.Key);
						removedTransitions.Add(pair.Key);
					}
				}
				foreach(var tr in removedTransitions) {
					transitionViewMaps.Remove(tr);
				}
			}
			// re-add with new up to date datas
			InitializeNodeViews(_fullReload);
			EditorUtility.DisplayProgressBar("Loading graph", "Initialize Edges", 1);
			InitializeEdgeViews();

			ToggleMinimap(UIElementUtility.Theme.enableMinimap);
			//Mark full reload to false for more faster reload after full reload
			_fullReload = false;
			// watch.Stop();
			// Debug.LogFormat("Refreshing {1} nodes took {0,8:N3} s.", watch.Elapsed.TotalSeconds, nodeViews.Count);
		}

		private PropertyInfo _keepPixelCacheProperty;
		protected void SetPixelCachedOnBoundChanged(bool value) {
			if(panel != null) {
				if(_keepPixelCacheProperty == null) {
					_keepPixelCacheProperty = panel.GetType().GetProperty("keepPixelCacheOnWorldBoundChange", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				}
				if(_keepPixelCacheProperty != null) {
					_keepPixelCacheProperty.SetValue(panel, value);
				}
			}
		}

		public bool requiredReload { get; private set; }
		protected bool _fullReload = true;
		protected UnityEngine.Object _reloadedGraphScope;
		protected Action executeAfterReload;

		public void MarkRepaint() {
			MarkRepaint(false);
		}

		public void FullReload() {
			MarkRepaint(true);
		}

		public void MarkRepaint(bool fullReload) {
			if(fullReload) {
				_fullReload = true;
			}
			if(!requiredReload) {
				requiredReload = true;
				uNodeThreadUtility.Queue(() => {
					requiredReload = false;
					EditorUtility.DisplayProgressBar("Loading graph", "", 0);
					float currentZoom = graph.zoomScale;
					SetZoomScale(1, true);
					contentViewContainer.SetOpacity(0);

					try {
						ReloadView();
					}
					catch(System.Exception ex) {
						Debug.LogException(ex);
					}
					EditorUtility.ClearProgressBar();

					uNodeThreadUtility.ExecuteAfter(3, () => {
						SetZoomScale(currentZoom, false);
						contentViewContainer.SetOpacity(1);
						executeAfterReload?.Invoke();
						executeAfterReload = null;
					});
				});
			}
		}

		protected bool needReloadNodes;
		protected HashSet<UNodeView> _needReloadedViews = new HashSet<UNodeView>();


		public void MarkRepaint(IEnumerable<UNodeView> views) {
			if(views == null)
				return;
			if(requiredReload) {
				needReloadNodes = false;
				// _needReloadedViews.Clear();
				foreach(var v in views) {
					_needReloadedViews.Add(v);
				}
				return;
			}
			if(!needReloadNodes) {
				needReloadNodes = true;
				uNodeThreadUtility.Queue(() => {
					if(needReloadNodes && !requiredReload) {
						Repaint(_needReloadedViews.ToArray());
						_needReloadedViews.Clear();
					}
					needReloadNodes = false;
				});
			}
			foreach(var v in views) {
				_needReloadedViews.Add(v);
			}
		}

		public void MarkRepaint(params UNodeView[] views) {
			MarkRepaint(views as IEnumerable<UNodeView>);
		}

		public void MarkRepaint(NodeComponent node) {
			if(node == null)
				return;
			UNodeView view;
			if(nodeViewsPerNode.TryGetValue(node, out view)) {
				MarkRepaint(view);
			}
		}

		public void MarkRepaint(TransitionEvent transition) {
			if(transition == null)
				return;
			TransitionView view;
			if(transitionViewMaps.TryGetValue(transition, out view)) {
				MarkRepaint(view);
			}
		}

		public void MarkRepaintEdges() {
			MarkRepaint(new UNodeView[0]);
		}

		void Repaint(params UNodeView[] views) {
			for(int i = 0; i < views.Length; i++) {
				if(views[i] == null)
					continue;
				views[i].ReloadView();
			}
			//Update edges.
			RemoveEdges();
			InitializeEdgeViews();
		}

		void Repaint(params NodeComponent[] nodes) {
			for(int i = 0; i < nodes.Length; i++) {
				if(nodeViewsPerNode.TryGetValue(nodes[i], out var view) && view != null) {
					view.ReloadView();
				}
			}
			//Update edges.
			RemoveEdges();
			InitializeEdgeViews();
		}
		#endregion

		#region Graph Functions
		public Dictionary<NodeComponent, PortView> portFlowNodeAliases = new Dictionary<NodeComponent, PortView>(EqualityComparer<NodeComponent>.Default);
		public void RegisterFlowPortAliases(PortView port, NodeComponent targetAlias) {
			portFlowNodeAliases[targetAlias] = port;
		}

		public Dictionary<NodeComponent, PortView> portValueNodeAliases = new Dictionary<NodeComponent, PortView>(EqualityComparer<NodeComponent>.Default);
		public void RegisterValuePortAliases(PortView port, NodeComponent targetAlias) {
			portValueNodeAliases[targetAlias] = port;
		}

		/// <summary>
		/// Clear the graph cached data so the graph will have fresh datas
		/// </summary>
		public static void ClearCache() {
			if(uNodeEditor.window != null && uNodeEditor.window.graphEditor is UIElementGraph graph) {
				if(graph != null && graph.graphView != null) {
					graph.graphView.cachedNodeMap.Clear();
					graph.graphView.FullReload();
				}
			}
		}

		/// <summary>
		/// Clear the specific graph cached data so the graph will have fresh datas
		/// </summary>
		/// <param name="graphRef"></param>
		public static void ClearCache(uNodeRoot graphRef) {
			if(uNodeEditor.window != null && uNodeEditor.window.graphEditor is UIElementGraph graph) {
				if(graph != null && graph.graphView != null) {
					List<uNodeComponent> keys = new List<uNodeComponent>();
					foreach(var pair in graph.graphView.cachedNodeMap) {
						if(pair.Key != null && pair.Key is INode<uNodeRoot> node) {
							if(node.GetOwner() == graphRef) {
								keys.Add(pair.Key);
							}
						}
					}
					foreach(var key in keys) {
						graph.graphView.cachedNodeMap.Remove(key);
					}
					if(graph.graphView.editorData?.graph == graphRef) {
						graph.graphView.FullReload();
					}
				}
			}
		}

		public static void ClearCache(NodeComponent nodeRef) {
			if(uNodeEditor.window != null && uNodeEditor.window.graphEditor is UIElementGraph graph) {
				if(graph != null && graph.graphView != null) {
					graph.graphView.cachedNodeMap.Remove(nodeRef);
					if(graph.graphView.editorData?.graph == nodeRef.owner) {
						graph.graphView.FullReload();
					}
				}
			}
		}

		/// <summary>
		/// Set graph zoom scales
		/// </summary>
		/// <param name="zoomScale"></param>
		public void SetZoomScale(float zoomScale) {
			if(scale != zoomScale && graph != null) {
				UpdateViewTransform(-(graph.position * zoomScale), new Vector3(zoomScale, zoomScale, 1));
			}
		}

		/// <summary>
		/// Set graph zoom scales
		/// </summary>
		/// <param name="zoomScale"></param>
		/// <param name="cachedPixel"></param>
		public void SetZoomScale(float zoomScale, bool cachedPixel) {
			if(scale != zoomScale && graph != null) {
				UpdateViewTransform(-(graph.position * zoomScale), new Vector3(zoomScale, zoomScale, 1));
			}
			SetPixelCachedOnBoundChanged(cachedPixel);
		}

		/// <summary>
		/// Toogle minimap on / off
		/// </summary>
		/// <param name="value"></param>
		public void ToggleMinimap(bool value) {
			if(value) {
				if(miniMap == null) {
					miniMap = new MinimapView();
				}
				if(miniMap.parent == null) {
					Add(miniMap);
				}
			} else {
				if(miniMap != null && miniMap.parent != null) {
					miniMap.RemoveFromHierarchy();
				}
			}
		}

		public Vector2 GetTopMousePosition(Vector2 mPos) {
			var screenRect = graph.window.GetMousePositionForMenu(mPos);
			Vector2 position = graph.window.rootVisualElement.ChangeCoordinatesTo(
				graph.window.rootVisualElement.parent,
				screenRect - graph.window.position.position);
			return position;
		}

		public Vector2 GetMousePosition(Vector2 mPos) {
			var position = GetTopMousePosition(mPos);
			var result = contentViewContainer.WorldToLocal(position);
			//result.x -= contentContainer.layout.width;//for right node aligment
			return result;
		}

		public Vector2 GetMousePosition(Vector2 mPos, out Vector2 topMousePosition) {
			topMousePosition = GetTopMousePosition(mPos);
			var result = contentViewContainer.WorldToLocal(topMousePosition);
			//result.x -= contentContainer.layout.width;//for right node aligment
			return result;
		}

		public Vector2 GetTopMousePosition(IMouseEvent evt) {
			var screenRect = graph.window.GetMousePositionForMenu(evt.mousePosition);
			Vector2 position = graph.window.rootVisualElement.ChangeCoordinatesTo(
				graph.window.rootVisualElement.parent,
				screenRect - graph.window.position.position);
			return position;
		}

		public Vector2 GetMousePosition(IMouseEvent evt) {
			var position = GetTopMousePosition(evt);
			var result = contentViewContainer.WorldToLocal(position);
			//result.x -= contentContainer.layout.width;//for right node aligment
			return result;
		}

		public Vector2 GetMousePosition(IMouseEvent evt, out Vector2 topMousePosition) {
			topMousePosition = GetTopMousePosition(evt);
			var result = contentViewContainer.WorldToLocal(topMousePosition);
			//result.x -= contentContainer.layout.width;//for right node aligment
			return result;
		}

		protected void AddNode(NodeComponent node) {
			if(node == null)
				return;
			AddNodeView(node);

			graph.AddNode(node);
		}

		protected void AddNodeView(NodeComponent node) {
			if(node == null)
				return;
			var viewType = UIElementUtility.GetNodeViewTypeFromType(node.GetType());

			if(viewType == null)
				viewType = typeof(BaseNodeView);

			var nodeView = Activator.CreateInstance(viewType) as UNodeView;
			nodeView.Initialize(this, node);
			AddElement(nodeView);

			nodeViews.Add(nodeView);
			nodeViewsPerNode[node] = nodeView;
			cachedNodeMap[node] = nodeView;
		}

		public TransitionView AddTransition(TransitionEvent transition) {
			if(transition == null)
				return null;
			TransitionView transitionView;
			if(!transitionViewMaps.TryGetValue(transition, out transitionView)) {
				var viewType = UIElementUtility.GetNodeViewTypeFromType(transition.GetType());

				if(viewType == null)
					viewType = typeof(TransitionView);

				transitionView = Activator.CreateInstance(viewType) as TransitionView;
				transitionView.Initialize(this, transition);
				transitionViewMaps[transition] = transitionView;
				cachedNodeMap[transition] = transitionView;

				AddElement(transitionView);
			}
			return transitionView;
		}

		public UNodeView GetNodeView(NodeComponent node) {
			if(node == null)
				return null;
			UNodeView n;
			if(nodeViewsPerNode.TryGetValue(node, out n)) {
				return n;
			}
			return n;
		}

		public List<UNodeView> GetNodeViews(IEnumerable<NodeComponent> nodes) {
			List<UNodeView> views = new List<UNodeView>();
			if(nodes == null)
				return views;
			foreach(var node in nodes) {
				if(node == null)
					continue;
				if(nodeViewsPerNode.TryGetValue(node, out var view)) {
					views.Add(view);
				}
			}
			return views;
		}

		public TransitionView GetTransitionView(TransitionEvent transition) {
			if(transition == null)
				return null;
			TransitionView transitionView;
			if(transitionViewMaps.TryGetValue(transition, out transitionView)) {
				return transitionView;
			}
			return transitionView;
		}

		void RemoveNodeViews(bool includingCache) {
			foreach(var nodeView in nodeViews) {
				RemoveElement(nodeView);
				if(includingCache)
					cachedNodeMap.Remove(nodeView.targetNode);
			}
			nodeViews.Clear();
			nodeViewsPerNode.Clear();
			foreach(var pair in transitionViewMaps) {
				RemoveElement(pair.Value);
				if(includingCache)
					cachedNodeMap.Remove(pair.Key);
			}
			transitionViewMaps.Clear();
		}

		public void Connect(EdgeView e, bool serialize = true) {
			if(e.input == null || e.output == null)
				return;
			AddElement(e);

			e.input.Connect(e);
			e.output.Connect(e);

			var inputNodeView = e.input.node;
			var outputNodeView = e.output.node;

			if(inputNodeView == null || outputNodeView == null) {
				edgeViews.Add(e);//Register edge so it will be removed in the next frame
				Debug.LogError("Connect aborted !");
				return;
			}

			if(serialize) {
				var inPort = e.input as PortView;
				var outPort = e.output as PortView;
				if(e.input.orientation == Orientation.Horizontal) {
					//if(!editorData.supportCoroutine && uNodeUtility.IsStackOverflow(inPort.GetNode(), outPort.GetNode())) {
					//	uNodeEditorUtility.DisplayErrorMessage("Cannot connect because of Stackoverflow / Recursive connections");
					//	MarkRepaint(true);
					//	edgeViews.Add(e);
					//	return;
					//}
					inPort.portData.OnValueChanged(outPort.portData.GetConnection());
				} else if(e.output.orientation == Orientation.Vertical) {
					if(!editorData.supportCoroutine && uNodeUtility.IsStackOverflow(inPort.GetNode(), outPort.GetNode())) {
						uNodeEditorUtility.DisplayErrorMessage("Cannot connect because of Stackoverflow / Recursive connections");
						MarkRepaint(true);
						edgeViews.Add(e);
						return;
					}
					outPort.portData.OnValueChanged(inPort.portData.GetConnection());
				}
				uNodeEditor.GUIChanged();
				//Code below will ensure that the port are up to date
				if(inputNodeView is UNodeView inputNode) {
					inputNode.MarkRepaint();
				}
				if(outputNodeView is UNodeView outputNode) {
					outputNode.MarkRepaint();
				}
				MarkRepaint(inPort.GetEdgeOwners());
				MarkRepaint(outPort.GetEdgeOwners());
				// MarkRepaint();
			}
			edgeViews.Add(e);
			inputNodeView.RefreshPorts();
			outputNodeView.RefreshPorts();
		}

		public void Disconnect(EdgeView e, bool serialize = true) {
			RemoveElement(e);

			if(serialize) {
				var inPort = e.input as PortView;
				var outPort = e.output as PortView;
				if(e.input.orientation == Orientation.Horizontal) {
					inPort.ResetPortValue();
				} else if(e.output.orientation == Orientation.Vertical) {
					outPort.ResetPortValue();
				}
				//Code below will ensure that the port are up to date
				MarkRepaint(inPort.GetEdgeOwners());
				MarkRepaint(outPort.GetEdgeOwners());
				// if(inPort.owner != null) {
				// 	inPort.owner.MarkRepaint();
				// }
				// if(outPort.owner != null) {
				// 	outPort.owner.MarkRepaint();
				// }
			}

			if(e?.input?.node != null) {
				e.input.Disconnect(e);
				var inputNodeView = e.input.node as UNodeView;
				if(inputNodeView != null) {
					inputNodeView.RefreshPorts();
				}
			}
			if(e?.output?.node != null) {
				e.output.Disconnect(e);
				var outputNodeView = e.output.node as UNodeView;
				if(outputNodeView != null) {
					outputNodeView.RefreshPorts();
				}
			}
		}

		public void RemoveEdges() {
			foreach(var edge in edgeViews)
				RemoveElement(edge);
			edgeViews.Clear();
		}
		#endregion

		#region IMGUI
		//Dictionary<GraphElement, Action> graphIMGUIHandler = new Dictionary<GraphElement, Action>();
		//private IMGUIContainer _iMGUIContainer;

		//internal IMGUIContainer IMGUIContainer {
		//	get {
		//		if(_iMGUIContainer == null) {
		//			_iMGUIContainer = new IMGUIContainer(OnGUI);
		//			contentViewContainer.Add(_iMGUIContainer);
		//			_iMGUIContainer.style.overflow = Overflow.Visible;
		//			_iMGUIContainer.StretchToParentSize();
		//		}
		//		return _iMGUIContainer;
		//	}
		//}

		//internal void RegisterIMGUI(GraphElement element, Action onGUI) {
		//	if(IMGUIContainer != null) {
		//		graphIMGUIHandler[element] = onGUI;
		//	}
		//}

		//internal void UnRegisterIMGUI(GraphElement element) {
		//	if(_iMGUIContainer != null) {
		//		graphIMGUIHandler.Remove(element);
		//	}
		//}

		//private void OnGUI() {
		//	foreach(var pair in graphIMGUIHandler) {
		//		if(pair.Key != null && pair.Key.parent != null && pair.Key.IsVisible() && pair.Value != null) {
		//			pair.Value();
		//		}
		//	}
		//}
		#endregion
	}
}