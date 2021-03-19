using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.uNode.Editors {
	public class PortView : Port, IEdgeConnectorListener {
		public new Type portType;
		public UNodeView owner { get; private set; }

		public new string portName {
			get {
				return base.portName;
			}
			set {
				base.portName = value;
				m_ConnectorText.EnableInClassList("ui-hidden", string.IsNullOrEmpty(value) && controlView == null);
			}
		}

		public event Action<PortView, Edge> OnConnected;
		public event Action<PortView, Edge> OnDisconnected;
		public PortData portData;

		protected ControlView controlView;
		protected Image portIcon;
		protected bool displayProxyTitle = true;

		private static CustomStyleProperty<int> s_ProxyOffsetX = new CustomStyleProperty<int>("--proxy-offset-x");
		private static CustomStyleProperty<int> s_ProxyOffsetY = new CustomStyleProperty<int>("--proxy-offset-y");

		List<EdgeView> edges = new List<EdgeView>();

		#region Initialization
		public PortView(Orientation portOrientation, Direction direction, PortData portData)
			: base(portOrientation, direction, Capacity.Multi, typeof(object)) {

			this.AddStyleSheet("uNodeStyles/NativePortStyle");
			this.AddStyleSheet(UIElementUtility.Theme.portStyle);
			if(portOrientation == Orientation.Vertical) {
				AddToClassList("flow-port");
			} else {
				AddToClassList("value-port");
			}

			m_EdgeConnector = new EdgeConnector<EdgeView>(this);
			this.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
			this.AddManipulator(m_EdgeConnector);
			DoUpdate();
			this.ExecuteAndScheduleAction(DoUpdate, 1000);
		}

		public virtual void Initialize(UNodeView nodeView, PortData portData) {
			this.owner = nodeView;
			this.portData = portData;
			ReloadView(true);
		}

		public virtual void ReloadView(bool refreshName = false) {
			if(portData != null) {
				portType = portData.GetPortType();
				if(refreshName) {
					portName = ObjectNames.NicifyVariableName(portData.GetPortName());
					if(orientation == Orientation.Vertical) {
						tooltip = "Flow";
					} else {
						tooltip = portType.PrettyName(true);
					}
				}
			}
			if(orientation == Orientation.Horizontal) {
				//portColor = new Color(0.09f, 0.7f, 0.4f);
				portColor = uNodePreference.GetColorForType(portType);
				if (portIcon == null) {
					portIcon = new Image();
					Insert(1, portIcon);
				}
				portIcon.image = uNodeEditorUtility.GetTypeIcon(portType);
				// portIcon.style.width = 16;
				// portIcon.style.height = 16;
				portIcon.pickingMode = PickingMode.Ignore;
			}
			UpdatePortClass();
		}

		private void UpdatePortClass() {
			if(connected)
				AddToClassList("connected");
			else
				RemoveFromClassList("connected");

			switch(direction) {
				case Direction.Input: {
					EnableInClassList("input", true);
					EnableInClassList("output", false);
				}
				break;
				case Direction.Output:
					EnableInClassList("input", false);
					EnableInClassList("output", true);
					break;
			}
		}
		#endregion

		public virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt) {

		}

		public void DisplayProxyTitle(bool value) {
			displayProxyTitle = value;
			proxyContainer?.RemoveFromHierarchy();
			proxyContainer = null;
			DoUpdate();
		}

		void DoUpdate() {
			if(portData != null && owner.IsVisible()) {
				bool isProxy = false;
				if(orientation == Orientation.Vertical) {
					if(direction == Direction.Input) {
						var edges = GetEdges();
						if(edges != null && edges.Count > 0) {
							if(edges.Any(e => e != null && e.isProxy)) {
								isProxy = true;
							}
						}
					} else {
						if(connected) {
							var edges = GetEdges();
							if(edges != null && edges.Count > 0) {
								if(edges.Any(e => e != null && e.isProxy)) {
									isProxy = true;
								}
							}
						}
					}
				} else {
					if(direction == Direction.Input) {
						if(connected) {
							var edges = GetEdges();
							if(edges != null && edges.Count > 0) {
								if(edges.Any(e => e != null && e.isProxy)) {
									isProxy = true;
								}
							}
						}
					} else {
						var edges = GetEdges();
						if(edges != null && edges.Count > 0) {
							if(edges.Any(e => e != null && e.isProxy)) {
								isProxy = true;
							}
						}
					}
				}
				ToggleProxy(isProxy);
				if(isProxy) {
					var color = portColor;
					proxyContainer.style.backgroundColor = color;
					proxyCap.style.backgroundColor = color;
					proxyLine.style.backgroundColor = color;
					if(proxyTitleBox != null) {
						var edges = GetEdges();
						if(edges != null && edges.Count > 0) {
							var edge = edges.FirstOrDefault(e => e != null && e.isValid && e.isProxy);
							if(edge != null) {
								PortView port = edge.input != this ? edge.input as PortView : edge.output as PortView;
								if(port != null && proxyTitleLabel != null) {
									proxyTitleLabel.text = uNodeEditorUtility.RemoveHTMLTag(port.GetProxyName());
									if(orientation == Orientation.Horizontal) {
										proxyTitleBox.style.SetBorderColor(port.portColor);
										proxyTitleIcon.image = port.portIcon?.image;
									}
								}
							}
						}
						MarkRepaintProxyTitle();
					}
				}
			}
		}

		private bool flagRepaintProxy;
		void MarkRepaintProxyTitle() {
			if(proxyTitleBox != null && !flagRepaintProxy) {
				flagRepaintProxy = true;
				if (orientation == Orientation.Vertical) {
					proxyTitleBox.ScheduleOnce(() => {
						flagRepaintProxy = false;
						proxyTitleBox.style.left = -((proxyTitleBox.layout.width / 2) - 6);
					}, 0);
				} else {
					proxyTitleBox.ScheduleOnce(() => {
						flagRepaintProxy = false;
						proxyTitleBox.style.left = -proxyTitleBox.layout.width;
					}, 0);
				}
			}
		}

		public override bool ContainsPoint(Vector2 localPoint) {
			if(orientation == Orientation.Vertical) {
				return new Rect(0.0f, 0.0f, layout.width, layout.height).Contains(localPoint);
			} else {
				Rect layout = m_ConnectorBox.layout;
				Rect rect;
				if(direction == Direction.Input) {
					rect = new Rect(0f - layout.xMin, 0f - layout.yMin, layout.width + layout.xMin, this.layout.height);
				} else {
					rect = new Rect(-5, 0f - layout.yMin, this.layout.width - layout.xMin, this.layout.height);
				}
				rect.width += 5;
				return rect.Contains(this.ChangeCoordinatesTo(m_ConnectorBox, localPoint));
			}
		}

		private int oldOffsetX, oldOffsetY;
		protected override void OnCustomStyleResolved(ICustomStyle styles) {
			if(orientation == Orientation.Horizontal) {
				portColor = uNodePreference.GetColorForType(portType);
			}
			base.OnCustomStyleResolved(styles);
		}

		private VisualElement proxyContainer;
		private VisualElement proxyCap;
		private VisualElement proxyLine;
		private VisualElement proxyTitleBox;
		private Label proxyTitleLabel;
		private Image proxyTitleIcon;
		private IMGUIContainer proxyDebug;
		protected void ToggleProxy(bool enable) {
			if(enable) {
				if(proxyContainer == null) {
					VisualElement connector = this.Q("connector");
					proxyContainer = new VisualElement { name = "connector" };
					proxyContainer.EnableInClassList("proxy", true);
					{
						proxyCap = new VisualElement() { name = "cap" };
						proxyCap.Add(proxyLine = new VisualElement() { name = "proxy-line" });
						proxyContainer.Add(proxyCap);
					}
					if(orientation == Orientation.Vertical) {
						proxyContainer.style.left = connector.ChangeCoordinatesTo(this, new Vector2(-3, 0)).x;
					} else {
						if(direction == Direction.Input) {
							proxyContainer.style.left = connector.ChangeCoordinatesTo(this, new Vector2(-25, 0)).x;
						} else {
							proxyContainer.style.left = connector.ChangeCoordinatesTo(this, new Vector2(25, 0)).x;
						}
					}
					if(displayProxyTitle && (orientation == Orientation.Horizontal && direction == Direction.Input || orientation == Orientation.Vertical && direction == Direction.Output)) {
						proxyTitleBox = new VisualElement() {
							name = "proxy-title",
						};
						proxyTitleBox.pickingMode = PickingMode.Ignore;
						proxyTitleLabel = new Label();
						proxyTitleLabel.pickingMode = PickingMode.Ignore;
						proxyTitleBox.Add(proxyTitleLabel);
						proxyContainer.Add(proxyTitleBox);
						if(orientation == Orientation.Horizontal) {
							proxyTitleBox.AddToClassList("proxy-horizontal");
							proxyTitleIcon = new Image();
							proxyTitleIcon.pickingMode = PickingMode.Ignore;
							proxyTitleBox.Add(proxyTitleIcon);
						} else {
							proxyTitleBox.AddToClassList("proxy-vertical");
						}
						MarkRepaintProxyTitle();
					}
					if(Application.isPlaying && orientation == Orientation.Horizontal && direction == Direction.Input) {
						if(proxyDebug != null) {
							proxyDebug.RemoveFromHierarchy();
						}
						proxyDebug = new IMGUIContainer(DebugGUI);
						proxyDebug.style.position = Position.Absolute;
						proxyDebug.style.overflow = Overflow.Visible;
						proxyDebug.pickingMode = PickingMode.Ignore;
						proxyContainer.Add(proxyDebug);
					}
					Add(proxyContainer);
					MarkDirtyRepaint();
				}
			} else if(proxyContainer != null) {
				proxyContainer.RemoveFromHierarchy();
				proxyContainer = null;
			}
		}

		void DebugGUI() {
			if(Application.isPlaying && uNodeUtility.useDebug && proxyContainer != null) {
				uNodeUtility.DebugData debugData = owner.owner.graph.GetDebugInfo();
				if(debugData != null) {
					if(orientation == Orientation.Horizontal && direction == Direction.Input) {
						MemberData member = portData.GetPortValue() as MemberData;
						if(member != null) {
							GUIContent debugContent = null;
							switch(member.targetType) {
								case MemberData.TargetType.ValueNode: {
									int ID = uNodeUtility.GetObjectID(member.startTarget as MonoBehaviour);
									if(debugData != null && debugData.valueTransitionDebug.ContainsKey(ID)) {
										if(debugData.valueTransitionDebug[ID].ContainsKey(int.Parse(member.startName))) {
											var vData = debugData.valueTransitionDebug[ID][int.Parse(member.startName)];
											if(vData.value != null) {
												debugContent = new GUIContent
													(uNodeUtility.GetDebugName(vData.value),
													uNodeEditorUtility.GetTypeIcon(vData.value.GetType()));
											} else {
												debugContent = new GUIContent("null");
											}
										}
									}
									break;
								}
								case MemberData.TargetType.NodeField: {

									break;
								}
								case MemberData.TargetType.NodeFieldElement: {

									break;
								}
							}
							if(debugContent != null) {
								Vector2 pos;
								if(proxyTitleLabel != null) {
									pos = proxyTitleLabel.ChangeCoordinatesTo(proxyDebug, Vector2.zero);
								} else {
									pos = this.ChangeCoordinatesTo(proxyDebug, new Vector2(-proxyContainer.layout.width, 0));
								}
								Vector2 size = EditorStyles.helpBox.CalcSize(new GUIContent(debugContent.text));
								size.x += 25;
								GUI.Box(
									new Rect(pos.x - size.x, pos.y, size.x - 5, 20),
									debugContent,
									EditorStyles.helpBox);
							}
						}
					}
				}
			}
		}

		#region Connect & Disconnect
		public override void Connect(Edge edge) {
			OnConnected?.Invoke(this, edge);

			base.Connect(edge);

			edges.Add(edge as EdgeView);
			owner.OnPortConnected(this);
			UpdatePortClass();
		}

		public override void Disconnect(Edge edge) {
			OnDisconnected?.Invoke(this, edge);

			base.Disconnect(edge);

			edges.Remove(edge as EdgeView);
			owner.OnPortDisconnected(this);
			UpdatePortClass();
		}
		#endregion

		#region Drop Port
		public void OnDropOutsidePort(Edge edge, Vector2 position) {
			var input = edge.input as PortView;
			var output = edge.output as PortView;
			var screenRect = owner.owner.graph.window.GetMousePositionForMenu(position);
			Vector2 pos = owner.owner.graph.window.rootVisualElement.ChangeCoordinatesTo(
				owner.owner.graph.window.rootVisualElement.parent,
				screenRect - owner.owner.graph.window.position.position);
			position = owner.owner.contentViewContainer.WorldToLocal(pos);
			if(input != null && output != null) {
				var draggedPort = input.edgeConnector?.edgeDragHelper?.draggedPort ?? output.edgeConnector?.edgeDragHelper?.draggedPort;
				if(draggedPort == input) {
					output = null;
				} else if(draggedPort == output) {
					input = null;
				}
			}
			if(input != null) {//Process if source is input port.
				PortView portView = input as PortView;
				foreach(var node in owner.owner.nodeViews) {
					if(node != null && node != portView.owner) {
						if(node.layout.Contains(position)) {
							if(edge.input.orientation == Orientation.Vertical) {//Flow
								foreach(var port in node.outputPorts) {
									if(port.orientation == Orientation.Vertical) {//Find the first flow port and connect it.
										uNodeThreadUtility.Queue(() => {
											edge.input = portView;
											edge.output = port;
											owner.owner.Connect(edge as EdgeView);
											owner.owner.MarkRepaint();
										});
										return;
									}
								}
							} else {//Value
								FilterAttribute filter = portView.GetFilter();
								bool flag = true;
								if(filter.SetMember) {
									var tNode = portView.GetNode() as Node;
									if(tNode == null || !tNode.CanSetValue()) {
										flag = false;
									}
								}
								if(flag) {
									foreach(var port in node.outputPorts) {
										if(port.orientation == Orientation.Horizontal && portView.IsValidTarget(port)) {
											uNodeThreadUtility.Queue(() => {
												edge.input = portView;
												edge.output = port;
												OnDrop(owner.owner, edge);
											});
											return;
										}
									}
								}
							}
							break;
						}
					}
				}
				if(input.orientation == Orientation.Horizontal) {//Value
					owner.owner.graph.ShowNodeMenu(position, portData.GetFilter(), (n) => {
						if(n.CanGetValue()) {
							portView.portData.OnValueChanged(new MemberData(n, MemberData.TargetType.ValueNode));
						}
					}, false);
				} else {//Flow
					owner.owner.graph.ShowNodeMenu(position, new FilterAttribute() {
						ValidTargetType =
								MemberData.TargetType.Constructor |
								MemberData.TargetType.Method |
								MemberData.TargetType.uNodeFunction |
								MemberData.TargetType.FlowNode,
					}, (n) => {
						var fields = NodeEditorUtility.GetFieldNodes(n.GetType());
						foreach(var field in fields) {
							if(field.field.FieldType == typeof(MemberData)) {
								if(field.attribute is FieldConnectionAttribute) {
									var FCA = field.attribute as FieldConnectionAttribute;
									if(FCA.isFlowConnection) {
										if(portView.portData.portID == UGraphView.SelfPortID) {
											field.field.SetValueOptimized(n, portView.portData.GetConnection());
										} else {
											field.field.SetValueOptimized(n,
												new MemberData(
													new object[] {
														portView.owner.targetNode,
														portView.portData.portID
													},
													MemberData.TargetType.FlowInput));
										}
										break;
									}
								}
							}
						}
					});
				}
			} else if(output != null) {//Process if source is output port.
				PortView portView = output as PortView;
				foreach(var node in owner.owner.nodeViews) {
					if(node != null && node != portView.owner) {
						if(node.layout.Contains(position)) {
							if(output.orientation == Orientation.Vertical) {//Flow
								foreach(var port in node.inputPorts) {
									if(port.orientation == Orientation.Vertical) {
										uNodeThreadUtility.Queue(() => {
											edge.output = portView;
											edge.input = port;
											owner.owner.Connect(edge as EdgeView);
											owner.owner.MarkRepaint();
										});
										return;
									}
								}
							} else {//Value
								FilterAttribute filter = portView.GetFilter();
								bool flag = true;
								if(filter.SetMember) {
									var tNode = portView.GetNode() as Node;
									if(tNode == null || !tNode.CanSetValue()) {
										flag = false;
									}
								}
								if(flag) {
									foreach(var port in node.inputPorts) {
										if(port.orientation == Orientation.Horizontal && portView.IsValidTarget(port)) {
											uNodeThreadUtility.Queue(() => {
												edge.output = portView;
												edge.input = port;
												OnDrop(owner.owner, edge);
											});
											return;
										}
									}
								}
							}
							break;
						}
					}
				}
				if(output.orientation == Orientation.Vertical) {//Flow
					owner.owner.graph.ShowNodeMenu(position, new FilterAttribute() {
						ValidTargetType =
								MemberData.TargetType.Constructor |
								MemberData.TargetType.Method |
								MemberData.TargetType.uNodeFunction |
								MemberData.TargetType.FlowNode,
					}, (n) => {
						if(n.IsFlowNode()) {
							portView.portData.OnValueChanged(new MemberData(n, MemberData.TargetType.FlowNode));
						} else {

						}

					});
				} else {//Value
					Type type = portView.GetPortType();
					bool canSetValue = false;
					bool canGetValue = true;
					if(portView.GetPortID() == UGraphView.SelfPortID && portView.GetNode() is Node) {
						canSetValue = (portView.GetNode() as Node).CanSetValue();
						canGetValue = (portView.GetNode() as Node).CanGetValue();
					}
					bool onlySet = canSetValue && !canGetValue;
					FilterAttribute FA = new FilterAttribute {
						VoidType = true,
						MaxMethodParam = int.MaxValue,
						Public = true,
						Instance = true,
						Static = false,
						UnityReference = false,
						InvalidTargetType = MemberData.TargetType.Null | MemberData.TargetType.Values,
						ValidateMember = (member) => {
							if (member is System.Reflection.MethodInfo) {
								var parameters = (member as System.Reflection.MethodInfo).GetParameters();
								for (int i = 0; i < parameters.Length; i++) {
									if (type.IsCastableTo(parameters[i].ParameterType)) {
										return true;
									}
								}
							}
							return false;
						},
						// DisplayDefaultStaticType = false
					};
					List<ItemSelector.CustomItem> customItems = null;
					if(!onlySet && GetNode() is MultipurposeNode && type.IsCastableTo(typeof(uNodeRoot))) {
						MultipurposeNode multipurposeNode = GetNode() as MultipurposeNode;
						if(multipurposeNode.target != null && multipurposeNode.target.target != null && (multipurposeNode.target.target.targetType == MemberData.TargetType.SelfTarget || multipurposeNode.target.target.targetType == MemberData.TargetType.Values)) {
							var sTarget = multipurposeNode.target.target.startTarget;
							if(sTarget is uNodeRoot) {
								customItems = ItemSelector.MakeCustomItems(sTarget as uNodeRoot);
								customItems.AddRange(ItemSelector.MakeCustomItems(typeof(uNodeRoot), sTarget, FA, "Inherit Member"));
							}
						}
					}
					if(customItems == null) {
						if(type is RuntimeType) {
							customItems = ItemSelector.MakeCustomItems((type as RuntimeType).GetRuntimeMembers(), FA);
							if (type.BaseType != null) 
								customItems.AddRange(ItemSelector.MakeCustomItems(type.BaseType, FA, "Inherit Member"));
						} else {
							customItems = onlySet ? new List<ItemSelector.CustomItem>() : ItemSelector.MakeCustomItems(type, FA);
						}
						var data = portView?.owner?.targetNode?.owner?.GetComponent<uNodeData>();
						if(data != null) {
							var usingNamespaces = new HashSet<string>(data.GetNamespaces());
							customItems.AddRange(ItemSelector.MakeExtensionItems(type, usingNamespaces, FA, "Extensions"));
						}

						var customInputItems = NodeEditorUtility.FindCustomInputPortItems();
						if(customInputItems != null && customInputItems.Count > 0) {
							var mData = portView.portData.GetConnection();
							var portNode = GetNode() as Node;
							if(portNode != null) {
								var portType = GetPortType();
								foreach(var c in customInputItems) {
									c.graph = owner.owner.graph;
									c.mousePositionOnCanvas = position;
									if(c.IsValidPort(portType, 
										canSetValue && canGetValue ? 
											PortAccessibility.GetSet : 
											canGetValue ? PortAccessibility.OnlyGet : PortAccessibility.OnlySet)) {
										var items = c.GetItems(portNode, mData, portType);
										if(items != null) {
											customItems.AddRange(items);
										}
									}
								}
							}
						}
					}
					owner.owner.graph.ShowNodeMenu(position, portData.GetFilter(), (n) => {
						if(n.CanGetValue()) {
							portView.portData.OnValueChanged(new MemberData(n, MemberData.TargetType.ValueNode));
						}
					}, false);
					if(customItems != null) {
						FA.Static = true;
						customItems.Sort((x, y) => {
							if(x.category != y.category) {
								return string.Compare(x.category, y.category);
							}
							return string.Compare(x.name, y.name);
						});
						ItemSelector w = ItemSelector.ShowWindow(portView.owner.targetNode, MemberData.none, FA, (MemberData mData) => {
							bool flag = mData.targetType == MemberData.TargetType.Method && !type.IsCastableTo(mData.startType);
							if(!flag && !mData.isStatic) {
								mData.instance = portView.portData.GetConnection();
							}
							NodeEditorUtility.AddNewNode(owner.owner.graph.editorData, null, null, position, (MultipurposeNode nod) => {
								if(nod.target == null) {
									nod.target = new MultipurposeMember();
								}
								nod.target.target = mData;
								MemberDataUtility.UpdateMultipurposeMember(nod.target);
								if(flag) {
									var pTypes = mData.ParameterTypes;
									if(pTypes != null) {
										int paramIndex = 0;
										MemberData param = null;
										for (int i = 0; i < pTypes.Length;i++){
											var types = pTypes[i];
											if(types != null) {
												for (int y = 0; y < types.Length;y++) {
													if(type.IsCastableTo(types[y])) {
														param = portView.portData.GetConnection();
														break;
													}
													paramIndex++;
												}
												if(param != null) break;
											}
										}
										if(nod.target.parameters.Length > paramIndex && param != null) {
											nod.target.parameters[paramIndex] = param;
										}
									}
								}
								portView.owner.owner.MarkRepaint();
							});
						}, customItems).ChangePosition(owner.owner.graph.GetMenuPosition());
						w.displayRecentItem = false;
						w.displayNoneOption = false;
					}
				}
			}
		}

		private void AutoConvertPin(UGraphView graphView, Edge edge) {
			var leftPort = edge.output as PortView;
			var rightPort = edge.input as PortView;

			Type leftType = leftPort.portData.GetFilter().GetActualType();
			Type rightType = rightPort.portData.GetFilter().GetActualType();
			if(rightType != typeof(object)) {
				bool flag2 = false;
				var autoConverts = NodeEditorUtility.FindAutoConvertPorts();
				foreach(var c in autoConverts) {
					c.filter = rightPort.portData.GetFilter();
					c.leftType = leftType;
					c.leftNode = leftPort.owner.targetNode as Node;
					c.rightType = rightType;
					c.rightNode = rightPort.owner.targetNode as Node;
					if(c.IsValid()) {
						flag2 = true;
						var nod = c.CreateNode();
						if(nod != null) {
							rightPort.portData.OnValueChanged(new MemberData(nod, MemberData.TargetType.ValueNode));
						}
						break;
					}
				}
				if(!flag2 && leftType == typeof(string)) {
					if(rightType == typeof(float) ||
						rightType == typeof(int) ||
						rightType == typeof(double) ||
						rightType == typeof(decimal) ||
						rightType == typeof(short) ||
						rightType == typeof(ushort) ||
						rightType == typeof(uint) ||
						rightType == typeof(long) ||
						rightType == typeof(byte) ||
						rightType == typeof(sbyte)) {

						NodeEditorUtility.AddNewNode<MultipurposeNode>(
							NodeGraph.openedGraph.editorData,
							new Vector2(rightPort.owner.targetNode.editorRect.x - 250, rightPort.owner.targetNode.editorRect.y),
							(nod) => {
								flag2 = true;
								nod.target.target = new MemberData(rightType.GetMethod("Parse", new Type[] { typeof(string) }));
								nod.target.parameters = new MemberData[] {
									leftPort.portData.GetConnection()
								};
								rightPort.portData.OnValueChanged(new MemberData(nod, MemberData.TargetType.ValueNode));
							});
					}
				}
			}
			graphView.MarkRepaint();
		}

		public void OnDrop(GraphView graphView, Edge edge) {
			var edgeView = edge as EdgeView;
			var graph = graphView as UGraphView;
			if(graph == null || edgeView == null || edgeView.input == null || edgeView.output == null)
				return;
			if(edgeView.input.orientation == Orientation.Horizontal) {
				if(edgeView.input == this) {
					if(!IsValidTarget(edge.output)) {
						int option = EditorUtility.DisplayDialogComplex("Do you want to continue?",
							"The source pin and destination pin type is not match.",
							"Convert if possible", "Continue", "Cancel");
						if(option == 0) {
							AutoConvertPin(graph, edge);
							return;
						} else if(option != 1) {
							return;
						}
					}
				} else {
					if(!IsValidTarget(edge.input)) {
						int option = EditorUtility.DisplayDialogComplex("Do you want to continue?",
							"The source pin and destination pin type is not match.",
							"Convert if possible", "Continue", "Cancel");
						if(option == 0) {
							AutoConvertPin(graph, edge);
							return;
						} else if(option != 1) {
							return;
						}
					}
				}
			}
			graph.Connect(edgeView);
		}
		#endregion

		#region Functions
		public void SetControl(VisualElement visualElement, bool autoLayout = false) {
			ControlView control = new ControlView();
			control.Add(visualElement);
			SetControl(control, autoLayout);
		}

		public void SetControl(ControlView control, bool autoLayout = false) {
			if(controlView != null) {
				controlView.RemoveFromHierarchy();
				controlView = null;
			}
			if(control != null) {
				control.EnableInClassList("output_port", true);
				m_ConnectorText.Add(control);
				controlView = control;
			}
			m_ConnectorText.EnableInClassList("Layout", autoLayout);
			portName = portName;
		}

		public List<EdgeView> GetEdges() {
			return edges;
		}

		public HashSet<UNodeView> GetEdgeOwners() {
			HashSet<UNodeView> nodes = new HashSet<UNodeView>();
			foreach (var e in edges) {
				var sender = e.GetSenderPort()?.owner;
				if(sender != null) {
					nodes.Add(sender);
				}
				var receiver = e.GetReceiverPort()?.owner;
				if(receiver != null) {
					nodes.Add(receiver);
				}
			}
			return nodes;
		}

		public HashSet<UNodeView> GetConnectedNodes() {
			HashSet<UNodeView> nodes = new HashSet<UNodeView>();
			if(edges.Count > 0) {
				foreach(var e in edges) {
					if(direction == Direction.Input) {
						var targetPort = e.output as PortView;
						var targetView = targetPort.owner;
						if(targetView != null && !nodes.Contains(targetView)) {
							nodes.Add(targetView);
						}
					} else {
						var targetPort = e.input as PortView;
						var targetView = targetPort.owner;
						if(targetView != null && !nodes.Contains(targetView)) {
							nodes.Add(targetView);
						}
					}
				}
			}
			return nodes;
		}

		public Type GetPortType() {
			return portType;
		}

		public string GetPortID() {
			return portData.portID;
		}

		public FilterAttribute GetFilter() {
			return portData.GetFilter();
		}

		public object GetValue() {
			return portData.GetPortValue();
		}

		public void OnValueChanged(object value) {
			portData.OnValueChanged(value);
		}

		public MemberData GetConnection() {
			return portData.GetConnection();
		}

		public string GetName() {
			return portName;
		}

		public string GetTooltip() {
			var str = portData.GetPortTooltip();
			if(string.IsNullOrEmpty(str)) {
				if(orientation == Orientation.Vertical) {
					if(direction == Direction.Input) {
						if(GetPortID() == UGraphView.SelfPortID) {
							return "Input flow to execute this node";
						}
					} else {
						if(GetPortID() == "onFinished") {
							return "Flow to execute on finish";
						}
					}
				} else {
					if(direction == Direction.Input) {
						
					} else {
						if(GetPortID() == UGraphView.SelfPortID) {
							return "The result value";
						}
					}
				}
			}
			return str;
		}

		public string GetPrettyName() {
			var str = GetName();
			if(string.IsNullOrEmpty(str)) {
				if(orientation == Orientation.Vertical) {
					if(direction == Direction.Input) {
						if(GetPortID() == UGraphView.SelfPortID) {
							return "Input";
						}
					} else {

					}
				} else {
					if(direction == Direction.Input) {
						
					} else {
						if(GetPortID() == UGraphView.SelfPortID) {
							return "Result";
						}
					}
				}
				if(portData.portID.StartsWith("$")) {
					return "Port";
				}
				return ObjectNames.NicifyVariableName(portData.portID);
			}
			return str;
		}

		private string GetProxyName() {
			var str = GetName();
			if(string.IsNullOrEmpty(str)) {
				if(orientation == Orientation.Vertical) {
					if(direction == Direction.Input) {
						if(GetPortID() == UGraphView.SelfPortID) {
							return owner.targetNode.GetNodeName();
						}
					} else {

					}
				} else {
					if(direction == Direction.Input) {

					} else {
						if(GetPortID() == UGraphView.SelfPortID) {
							return owner.targetNode.GetNodeName();
						}
					}
				}
				if(portData.portID.StartsWith("$")) {
					return "Port";
				}
				return ObjectNames.NicifyVariableName(portData.portID);
			}
			return str;
		}

		public void SetName(string str) {
			portName = ObjectNames.NicifyVariableName(str);
			portData.getPortName = () => str;
		}

		public NodeComponent GetNode() {
			return owner.targetNode;
		}

		public bool IsValidTarget(Port port) {
			var portView = port as PortView;
			if(portView != null) {
				if(portData.owner == portView.portData.owner || orientation != portView.orientation)
					return false;
				if(orientation == Orientation.Horizontal) {
					var inputPort = portView.direction == Direction.Input ? portView : this;
					var outputPort = portView.direction == Direction.Output ? portView : this;

					var filter = inputPort.portData.GetFilter();
					var outputType = outputPort.portData.GetPortType();
					if(filter.IsValidType(outputType) || outputType.IsCastableTo(inputPort.portType)) {
						return true;
					} else {
						var inputType = inputPort.portData.GetPortType();
						if(inputType == outputType ||
							inputType == typeof(MemberData) ||
							inputType.IsCastableTo(outputType) ||
							outputType.IsCastableTo(inputType)) {
							return true;
						} else if(inputType is RuntimeType && (
							inputType.IsCastableTo(typeof(Component)) || 
							inputType.IsInterface)) {
							if(outputType == typeof(GameObject) || outputType.IsCastableTo(typeof(Component))) {
								return true;
							}
						}
					}
					return false;
				}
				return true;
			}
			return false;
		}

		public bool IsProxy() {
			if(edges.Count == 0)
				return false;
			return edges.All(e => e.isProxy);
		}

		public void ResetPortValue() {
			if(orientation == Orientation.Vertical) {
				portData.OnValueChanged(MemberData.none);
			} else {
				if(direction == Direction.Input) {
					var val = portData.GetPortValue() as MemberData;
					if(val != null) {
						Type valType = val.type;
						if(valType == null) {
							valType = GetPortType();
						}
						if(ReflectionUtils.CanCreateInstance(valType)) {
							if(valType == typeof(object)) {
								portData.OnValueChanged(MemberData.CreateFromValue(""));
							} else {
								portData.OnValueChanged(MemberData.CreateFromValue(ReflectionUtils.CreateInstance(valType)));
							}
						} else {
							portData.OnValueChanged(MemberData.CreateFromValue(null, valType));
						}
					} else {
						object value = null;
						if(ReflectionUtils.CanCreateInstance(GetPortType())) {
							if(GetPortType() == typeof(object)) {
								portData.OnValueChanged(MemberData.CreateFromValue(""));
							} else {
								portData.OnValueChanged(MemberData.CreateFromValue(ReflectionUtils.CreateInstance(GetPortType())));
							}
						} else {
							portData.OnValueChanged(MemberData.CreateFromValue(value, GetPortType()));
						}
					}
				} else {
					portData.OnValueChanged(MemberData.empty);
				}
			}
		}
		#endregion
	}
}