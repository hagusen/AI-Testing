using MaxyGames.uNode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MaxyGames {
	public static partial class CodeGenerator {
		/// <summary>
		/// Begin a new block statement ( use this for generating lambda block )
		/// </summary>
		/// <param name="allowYield"></param>
		public static void BeginBlock(bool allowYield) {
			generatorData.blockStacks.Add(new BlockStack() {
				allowYield = allowYield
			});
		}

		/// <summary>
		/// End the previous block statment
		/// </summary>
		public static void EndBlock() {
			if(generatorData.blockStacks.Count > 0) {
				generatorData.blockStacks.RemoveAt(generatorData.blockStacks.Count - 1);
			}
		}

		/// <summary>
		/// Register node as flow node.
		/// Note: Call this inside RegisterPin in node. 
		/// </summary>
		/// <param name="node"></param>
		public static void RegisterFlowNode(NodeComponent node) {
			if(!generatorData.registeredFlowNodes.Contains(node)) {
				generatorData.registeredFlowNodes.Add(node);
			}
		}

        private static void ConnectNode(NodeComponent node) {
			if(node != null && !generatorData.connectedNode.Contains(node)) {
				generatorData.connectedNode.Add(node);
				var nodes = GetConnections(node);
				if(nodes != null) {
					foreach(NodeComponent n in nodes) {
						if(n) {
							ConnectNode(n);
						}
					}
				}
				if(node is ISuperNode) {
					ISuperNode superNode = node as ISuperNode;
					foreach(var n in superNode.nestedFlowNodes) {
						if(n != null) {
							ConnectNode(n);
						}
					}
				}
				Func<object, bool> validation = delegate (object obj) {
					if(obj is MemberData) {
						MemberData member = obj as MemberData;
						if(member.IsTargetingPinOrNode) {
							ConnectNode(member.GetTargetNode());
						}
					}
					return false;
				};
				AnalizerUtility.AnalizeObject(node, validation);
			}
		}

		/// <summary>
		/// Are the node has flow ports?
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static bool HasFlowPort(Node node) {
			return generatorData.registeredFlowNodes.Contains(node) || node.IsFlowNode();
		}

		public static bool IsContainOperatorCode(string value) {
			if(value.Contains("op_")) {
				switch(value) {
					case "op_Addition":
					case "op_Subtraction":
					case "op_Division":
					case "op_Multiply":
					case "op_Modulus":
					case "op_Equality":
					case "op_Inequality":
					case "op_LessThan":
					case "op_GreaterThan":
					case "op_LessThanOrEqual":
					case "op_GreaterThanOrEqual":
					case "op_BitwiseAnd":
					case "op_BitwiseOr":
					case "op_LeftShift":
					case "op_RightShift":
					case "op_ExclusiveOr":
					case "op_UnaryNegation":
					case "op_UnaryPlus":
					case "op_LogicalNot":
					case "op_OnesComplement":
					case "op_Increment":
					case "op_Decrement":
						return true;
				}
			}
			return false;
		}

		#region Grouped
		/// <summary>
		/// Register node as grouped node.
		/// </summary>
		/// <param name="node"></param>
		public static void RegisterAsGroupedNode(NodeComponent node) {
			if(!generatorData.groupedNode.Contains(node)) {
				generatorData.groupedNode.Add(node);
				generatorData.ungroupedNode.Remove(node);
			}
		}

		/// <summary>
		/// Register node as coroutine state node ( non-coroutine state node doesn't need this ).
		/// </summary>
		/// <param name="node"></param>
		public static void RegisterAsStateNode(NodeComponent node) {
			if(!generatorData.ungroupedNode.Contains(node)) {
				//generatorData.groupedNode.Remove(node);
				generatorData.ungroupedNode.Add(node);
			}
		}

		/// <summary>
		/// Register flow node as coroutine node
		/// </summary>
		/// <param name="node"></param>
		public static void RegisterAsCoroutineNode(NodeComponent node) {

		}

		/// <summary>
		/// Is the node are identified as coroutine node?
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static bool IsCoroutineFlow(NodeComponent node) {
			return node.IsSelfCoroutine();
		}

		/// <summary>
		/// Are the flow connection identifiead as coroutine?
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static bool IsCoroutineFlow(MemberData member) {
			var node = member.GetTargetNode();
			return IsCoroutineFlow(node);
		}

		/// <summary>
		/// Is the node is grouped?
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static bool IsGroupedNode(NodeComponent node) {
			return generatorData.groupedNode.Contains(node);
		}

		/// <summary>
		/// Is the node is ungrouped?
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static bool IsUngroupedNode(NodeComponent node) {
			return generatorData.ungroupedNode.Contains(node) || !generatorData.groupedNode.Contains(node);
		}

		/// <summary>
		/// If true indicate the node can return state success or failure
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static bool CanReturnState(NodeComponent node) {
			return IsUngroupedNode(node) || uNodeUtility.IsInStateGraph(node);
		}

		/// <summary>
		/// Is the node is in state graph?
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static bool IsInStateGraph(Node node) {
			return uNodeUtility.IsInStateGraph(node);
		}
		#endregion

        #region GetFlowConnection
		/// <summary>
		/// Get flow nodes from node.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static HashSet<NodeComponent> GetFlowConnection(NodeComponent node) {
			HashSet<NodeComponent> allNodes;
			if(generatorData.flowNodeConnectionsMap.TryGetValue(node, out allNodes)) {
				return allNodes;
			}
			allNodes = new HashSet<NodeComponent>();
			if(node is StateNode) {
				StateNode eventNode = node as StateNode;
				TransitionEvent[] TE = eventNode.GetTransitions();
				foreach(TransitionEvent transition in TE) {
					if(transition.GetTargetNode() != null && !allNodes.Contains(transition.GetTargetNode())) {
						allNodes.Add(transition.GetTargetNode());
					}
				}
			}
			Func<object, bool> validation = delegate (object obj) {
				if(obj is MemberData) {
					MemberData member = obj as MemberData;
					if(member != null && member.isAssigned &&
						(member.targetType == MemberData.TargetType.FlowNode ||
						member.targetType == MemberData.TargetType.FlowInput) &&
						!allNodes.Contains(member.GetTargetNode())) {
						allNodes.Add(member.GetTargetNode());
					}
				}
				return false;
			};
			AnalizerUtility.AnalizeObject(node, validation);
			generatorData.flowNodeConnectionsMap[node] = allNodes;
			return allNodes;
		}

		
		public static HashSet<NodeComponent> GetConnections(NodeComponent node) {
			HashSet<NodeComponent> allNodes;
			if(generatorData.nodeConnectionsMap.TryGetValue(node, out allNodes)) {
				return allNodes;
			}
			allNodes = new HashSet<NodeComponent>();
			if(node is StateNode) {
				StateNode eventNode = node as StateNode;
				TransitionEvent[] TE = eventNode.GetTransitions();
				foreach(TransitionEvent transition in TE) {
					var tNode = transition.GetTargetNode();
					if(tNode != null) {
						allNodes.Add(tNode);
					}
				}
			} else if(node is BaseEventNode) {
				BaseEventNode stateEvent = node as BaseEventNode;
				foreach(var member in stateEvent.GetFlows()) {
					var tNode = member.GetTargetNode();
					if(tNode != null) {
						allNodes.Add(tNode);
					}
				}
			}
			Func<object, bool> validation = delegate (object obj) {
				if(obj is MemberData) {
					MemberData member = obj as MemberData;
					if(member != null && member.IsTargetingPinOrNode) {
						allNodes.Add(member.GetTargetNode());
					}
				}
				return false;
			};
			AnalizerUtility.AnalizeObject(node, validation);
			generatorData.nodeConnectionsMap[node] = allNodes;
			return allNodes;
		}

		/// <summary>
		/// Find all node connection include first node.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="allNode"></param>
		/// <param name="includeSuperNode"></param>
		public static void FindAllNodeConnection(NodeComponent node, ref HashSet<NodeComponent> allNode, bool includeSuperNode = true) {
			if(node != null && !allNode.Contains(node)) {
				allNode.Add(node);
				var nodes = GetFlowConnection(node);
				if(nodes != null) {
					foreach(var n in nodes) {
						if(n) {
							FindAllNodeConnection(n, ref allNode, includeSuperNode);
						}
					}
				}
				if(includeSuperNode && node is ISuperNode) {
					ISuperNode superNode = node as ISuperNode;
					foreach(var n in superNode.nestedFlowNodes) {
						FindAllNodeConnection(n, ref allNode, includeSuperNode);
					}
				}
			}
		}

		/// <summary>
		/// Find all node connection after coroutine node.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="allNode"></param>
		/// <param name="includeSuperNode"></param>
		/// <param name="includeCoroutineEvent"></param>
		public static void FindFlowConnectionAfterCoroutineNode(NodeComponent node, ref HashSet<NodeComponent> allNode,
			bool includeSuperNode = true,
			bool includeCoroutineEvent = true,
			bool passCoroutine = false) {
			if(node != null && !allNode.Contains(node)) {
				bool isCoroutineNode = node.IsSelfCoroutine();
				if(!passCoroutine && isCoroutineNode) {
					passCoroutine = true;
				}
				if(passCoroutine && (!isCoroutineNode || includeCoroutineEvent)) {
					allNode.Add(node);
				}
				var nodes = GetFlowConnection(node);
				if(nodes != null) {
					foreach(Node n in nodes) {
						if(n) {
							FindFlowConnectionAfterCoroutineNode(n, ref allNode, includeSuperNode, includeCoroutineEvent, passCoroutine);
						}
					}
				}
				if(includeSuperNode && node is ISuperNode) {
					ISuperNode superNode = node as ISuperNode;
					foreach(var n in superNode.nestedFlowNodes) {
						FindFlowConnectionAfterCoroutineNode(n, ref allNode, includeSuperNode, includeCoroutineEvent, passCoroutine);
					}
				}
			}
		}
		#endregion

        #region InstanceVariable
		/// <summary>
		/// True if instance variable from node has used in flow nodes.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="field"></param>
		/// <param name="flows"></param>
		/// <returns></returns>
		public static bool NeedInstanceVariable(Node from, FieldInfo field, params MemberData[] flows) {
			return NeedInstanceVariable(from, field, flows.Select((m) => m.GetTargetNode()).ToList());
		}

		/// <summary>
		/// True if instance variable from node has used in flow nodes.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="field"></param>
		/// <param name="flows"></param>
		/// <returns></returns>
		public static bool NeedInstanceVariable(Node from, FieldInfo field, IList<MemberData> flows) {
			return NeedInstanceVariable(from, field, flows.Select((m) => m.GetTargetNode()).ToList());
		}

		/// <summary>
		/// True if instance variable from node has used in flow nodes.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="field"></param>
		/// <param name="flows"></param>
		/// <returns></returns>
		public static bool NeedInstanceVariable(Node from, FieldInfo field, IList<Node> flows) {
			if(from && flows != null && flows.Count > 0) {
				var allConnection = GetAllNode(from.transform.parent);
				var allFlows = new HashSet<NodeComponent>();
				foreach(var flow in flows) {
					if(flow == null)
						continue;
					FindAllNodeConnection(flow, ref allFlows);
				}
				bool check = false;
				Func<object, bool> validation = null;
				validation = delegate (object obj) {
					if(obj is MemberData) {
						MemberData member = obj as MemberData;
						if(member.isAssigned) {
							Node n = member.GetTargetNode();
							if(n) {
								if(member.targetType == MemberData.TargetType.NodeField && n == from && member.startName == field.Name) {
									check = true;
								} else if(member.targetType == MemberData.TargetType.ValueNode) {
									AnalizerUtility.AnalizeObject(n, validation);
								}
							}
						}
					}
					return check;
				};
				foreach(Node node in allConnection) {
					if(node == null || !isInUngrouped && IsGroupedNode(from) /*&& allFlows.Contains(node)*/)
						continue;
					if(!check)
						AnalizerUtility.AnalizeObject(node, validation);
					if(check) {
						var nodes = GetFlowConnectedTo(node);
						if(nodes.Count > 0) {
							return nodes.Any(n => !allFlows.Contains(n));
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// True if instance variable from node has used in flow nodes.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="field"></param>
		/// <param name="index"></param>
		/// <param name="flows"></param>
		/// <returns></returns>
		public static bool NeedInstanceVariable(Node from, FieldInfo field, int index, params MemberData[] flows) {
			return NeedInstanceVariable(from, field, index, flows.Select((m) => m.GetTargetNode()).ToList());
		}

		/// <summary>
		/// True if instance variable from node has used in flow nodes.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="field"></param>
		/// <param name="index"></param>
		/// <param name="flows"></param>
		/// <returns></returns>
		public static bool NeedInstanceVariable(Node from, FieldInfo field, int index, IList<Node> flows) {
			if(from && flows != null && flows.Count > 0) {
				var allConnection = GetAllNode(from.transform.parent);
				var allFlows = new HashSet<NodeComponent>();
				foreach(var flow in flows) {
					if(flow == null)
						continue;
					FindAllNodeConnection(flow, ref allFlows);
				}
				bool check = false;
				Func<object, bool> validation = null;
				validation = delegate (object obj) {
					if(obj is MemberData) {
						MemberData member = obj as MemberData;
						if(member.isAssigned) {
							Node n = member.GetTargetNode();
							if(n) {
								int tes;
								if(member.targetType == MemberData.TargetType.NodeFieldElement && n == from &&
									int.TryParse(member.startName.Split('#')[1], out tes) && tes == index &&
									member.startName.Split('#')[0] == field.Name) {
									check = true;
								} else if(member.targetType == MemberData.TargetType.ValueNode) {
									AnalizerUtility.AnalizeObject(n, validation);
								}
							}
						}
					}
					return check;
				};
				foreach(Node node in allConnection) {
					if(node == null || !isInUngrouped && IsGroupedNode(from) /*&& allFlows.Contains(node)*/)
						continue;
					AnalizerUtility.AnalizeObject(node, validation);
					if(check) {
						var nodes = GetFlowConnectedTo(node);
						if(nodes.Count > 0) {
							return nodes.Any(n => !allFlows.Contains(n));
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// True if group node has instance variable.
		/// </summary>
		/// <param name="groupNode"></param>
		/// <returns></returns>
		public static bool NeedInstanceVariable<T>(T superNode) where T : ISuperNode {
			if(superNode != null) {
				var flows = new HashSet<NodeComponent>();
				foreach(var n in superNode.nestedFlowNodes) {
					FindFlowConnectionAfterCoroutineNode(n, ref flows);
				}
				bool check = false;
				Func<object, bool> validation = null;
				validation = delegate (object obj) {
					if(obj is MemberData) {
						MemberData member = obj as MemberData;
						Node n = member.GetInstance() as Node;
						if(n != null) {
							if(n is IVariableSystem && n is T && n == superNode as UnityEngine.Object &&
								(member.targetType == MemberData.TargetType.uNodeVariable ||
								member.targetType == MemberData.TargetType.uNodeGroupVariable ||
								member.targetType == MemberData.TargetType.uNodeLocalVariable)) {
								check = true;
							} else if(member.targetType == MemberData.TargetType.ValueNode) {
								AnalizerUtility.AnalizeObject(n, validation);
							}
						}
					}
					return check;
				};
				foreach(Node node in flows) {
					if(node == null)
						continue;
					AnalizerUtility.AnalizeObject(node, validation);
					if(check) {
						return true;
					}
				}
				var allConnection = new HashSet<NodeComponent>();
				foreach(var n in superNode.nestedFlowNodes) {
					FindAllNodeConnection(n, ref allConnection);
				}
				foreach(Node node in allConnection) {
					if(node == null)
						continue;
					AnalizerUtility.AnalizeObject(node, validation);
					if(check) {
						var nodes = GetFlowConnectedTo(node);
						if(nodes.Count > 0) {
							if(nodes.Any(n => !flows.Contains(n))) {
								return true;
							}
						}
						if((generatorData.ungroupedNode.Contains(node) || generatorData.portableActionInNode.Contains(node))) {
							return true;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// True if the variable is instanced variable.
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		public static bool IsInstanceVariable(VariableData variable) {
			foreach(VData vdata in generatorData.GetVariables()) {
				if(vdata.variableRef == variable) {
					return vdata.isInstance;
				}
			}
			//throw new Exception("The variable is not registered.");
			return false;
		}
		#endregion
        
		#region GetAllNode
		/// <summary>
		/// Get all nodes.
		/// </summary>
		/// <returns></returns>
		public static IEnumerable<NodeComponent> GetAllNode() {
			return generatorData.allNode;
		}

		/// <summary>
		/// Get all nodes in child of parent.
		/// </summary>
		/// <param name="parent"></param>
		/// <returns></returns>
		public static HashSet<NodeComponent> GetAllNode(Transform parent) {
			if(parent == graph.transform) {
				parent = graph.RootObject.transform;
			}
			HashSet<NodeComponent> nodes;
			if(generatorData.nodesMap.TryGetValue(parent, out nodes)) {
				return nodes;
			}
			nodes = new HashSet<NodeComponent>();
			foreach(NodeComponent node in GetAllNode()) {
				if(node == null)
					continue;
				if(node.transform.parent == parent) {
					nodes.Add(node);
				}
			}
			generatorData.nodesMap[parent] = nodes;
			return nodes;
		}
		#endregion

		#region ActionData
		private static int actionID = 0;
		private static Dictionary<Block, int> actionDataID = new Dictionary<Block, int>();

		/// <summary>
		/// Get invoke action code.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public static string GetInvokeActionCode(Block target) {
			if(target == null)
				throw new System.Exception("target can't null");
			if(!actionDataID.ContainsKey(target)) {
				actionDataID.Add(target, ++actionID);
			}
			return GetInvokeCode(_activateActionCode, false, actionDataID[target]).RemoveSemicolon();
		}

		[System.NonSerialized]
		private static int coNum = 0;

		private static CoroutineData GetOrRegisterCoroutineEvent(object owner) {
			CoroutineData data;
			if(!generatorData.coroutineEvent.TryGetValue(owner, out data)) {
				data = new CoroutineData();
				data.variableName = "coroutine" + (++coNum).ToString();
				generatorData.coroutineEvent[owner] = data;
			}
			return data;
		}

		/// <summary>
		/// Used to Set OnStop action in EventCoroutine.
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="contents"></param>
		public static void SetStopAction(object owner, string contents) {
			var data = GetOrRegisterCoroutineEvent(owner);
			data.onStop = contents;
		}

		public static void SetCoroutineAction(object owner, string contents) {
			var data = GetOrRegisterCoroutineEvent(owner);
			data.contents = contents;
		}

		public static void SetCoroutineExecution(object owner, string contents) {
			var data = GetOrRegisterCoroutineEvent(owner);
			data.customExecution = contents;
		}

		/// <summary>
		/// Register a Coroutine Event
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="generator"></param>
		/// <returns></returns>
		public static string RegisterCoroutineEvent(object obj, Func<string> generator, bool customExecution = false) {
			if(CodeGenerator.generatorData.coroutineEvent.ContainsKey(obj)) {
				return CodeGenerator.RunEvent(obj);
			} else {
				if(customExecution) {
					CodeGenerator.SetCoroutineExecution(obj, generator());
				} else {
					CodeGenerator.SetCoroutineAction(obj, generator());
				}
				return CodeGenerator.RunEvent(obj);
			}
		}

		/// <summary>
		/// Get the variable name of coroutine event
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public static string GetCoroutineName(object target) {
			if(target != null) {
				return GetOrRegisterCoroutineEvent(target).variableName;
			}
			return null;
		}
		#endregion
        
		#region Unused / Other
		/// <summary>
		/// Return true on flow body can be simplify to lambda expression code.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="returnType"></param>
		/// <param name="parameterTypes"></param>
		/// <returns></returns>
		public static bool CanSimplifyToLambda(MemberData target, Type returnType, IList<Type> parameterTypes) {
			if(target.IsTargetingNode) {
				var bodyNode = target.GetTargetNode();
				if(bodyNode is MultipurposeNode) {
					var node = bodyNode as MultipurposeNode;
					if(node.target.target.isAssigned && !node.onFinished.isAssigned) {
						System.Type[] memberTypes = null;
						if(node.target.target.targetType == MemberData.TargetType.Method) {
							var members = node.target.target.GetMembers(false);
							if(members != null && members.Length > 0) {
								var lastMember = members.LastOrDefault() as System.Reflection.MethodInfo;
								if(lastMember != null && lastMember.ReturnType == returnType) {
									memberTypes = lastMember.GetParameters().Select(i => i.ParameterType).ToArray();
								}
							}
						} else if(node.target.target.targetType == MemberData.TargetType.uNodeFunction) {
							uNodeFunction func = node.target.target.GetUnityObject() as uNodeFunction;
							if(func != null && func.ReturnType() == returnType) {
								memberTypes = func.parameters.Select(i => i.Type).ToArray();
							}
						}
						if(memberTypes != null) {
							if(parameterTypes.Count == memberTypes.Length && node.target.parameters.Length == memberTypes.Length) {
								bool flag = true;
								for(int x = 0; x < parameterTypes.Count; x++) {
									if(parameterTypes[x] != memberTypes[x]) {
										flag = false;
										break;
									}
								}
								if(flag) {
									for(int x = 0; x < parameterTypes.Count; x++) {
										var p = node.target.parameters[x];
										if(p.targetType != MemberData.TargetType.NodeFieldElement || p.GetAccessIndex() != x) {
											flag = false;
											break;
										}
									}
									if(flag) {
										return true;
									}
								}
							}
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Are the event can be grouped?
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		public static bool CanGroupEvent(EventNode method) {
			if(method != null) {
				foreach(var flow in method.GetFlows()) {
					if(flow != null && flow.GetTargetNode() != null && flow.GetTargetNode().IsSelfCoroutine()) {
						return false;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Are the node only connected to from node.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="from"></param>
		/// <returns></returns>
		public static bool IsOnlyConnectedFrom(Node target, NodeComponent from) {
			if(target != null && from != null) {
				var comp = GetNodeConnectedTo(target);
				if(comp != null && comp.Count == 1) {
					return comp[0] == from;
				}
			}
			return false;
		}

		/// <summary>
		/// Get the node connection count
		/// </summary>
		/// <param name="target"></param>
		/// <param name="from"></param>
		/// <returns></returns>
		public static int GetConnectionCount(Node target, NodeComponent from) {
			int num = 0;
			if(target != null && from != null) {
				if(from is StateNode) {
					StateNode node = from as StateNode;
					TransitionEvent[] TE = node.GetTransitions();
					foreach(TransitionEvent transition in TE) {
						if(transition != null && transition.GetTargetNode() == target) {
							num++;
						}
					}
				} else if(from is EventNode) {
					EventNode method = from as EventNode;
					foreach(var node in method.GetFlows()) {
						if(node.GetTargetNode() == target) {
							num++;
						}
					}
				}
			}
			return num;
		}

		/// <summary>
		/// Get the node connection count
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public static int GetConnectionCount(Node target) {
			int num = 0;
			if(target != null) {
				if(generatorData.ConnectedInNodeCount.TryGetValue(target, out num)) {
					return num;
				}
				List<NodeComponent> comp = new List<NodeComponent>();
				foreach(Node node in generatorData.allNode) {
					if(target == node)
						continue;
					if(node is StateNode) {
						StateNode stateNode = node as StateNode;
						TransitionEvent[] TE = stateNode.GetTransitions();
						foreach(TransitionEvent transition in TE) {
							if(transition != null && transition.GetTargetNode() == target) {
								if(!comp.Contains(transition.GetTargetNode()))
									comp.Add(transition.GetTargetNode());
								num++;
							}
						}
					}
				}
				generatorData.ConnectedInNodeCount.Add(target, num);
				generatorData.EventConnectedInNode.Add(target, comp);
			}
			return num;
		}

		public static HashSet<NodeComponent> GetFlowConnectedTo(Node target) {
			if(target != null) {
				if(generatorData.FlowConnectedTo.TryGetValue(target, out var comp)) {
					return comp;
				}
				comp = new HashSet<NodeComponent>();
				Node currNode = null;
				Func<object, bool> validation = delegate (object obj) {
					if(obj is MemberData) {
						MemberData member = obj as MemberData;
						Node targetNode = member.GetTargetNode();
						if(targetNode == target) {
							if(member.targetType == MemberData.TargetType.FlowNode || member.targetType == MemberData.TargetType.FlowInput) {
								comp.Add(currNode);
								return true;
							} else if(member.targetType == MemberData.TargetType.ValueNode ||
								member.targetType == MemberData.TargetType.NodeField ||
								member.targetType == MemberData.TargetType.NodeFieldElement) {
								var connectedNodes = GetFlowConnectedTo(currNode);
								if(connectedNodes != null) {
									foreach(var n in connectedNodes) {
										comp.Add(n);
									}
								}
							}
						}
					}
					return false;
				};
				foreach(Node node in generatorData.allNode) {
					if(target == node)
						continue;
					if(node is StateNode) {
						StateNode eventNode = node as StateNode;
						TransitionEvent[] TE = eventNode.GetTransitions();
						foreach(TransitionEvent transition in TE) {
							if(transition != null && transition.GetTargetNode() == target) {
								comp.Add(node);
								break;
							}
						}
					} else if(node is IMacro) {
						IMacro macro = node as IMacro;
						var outputFlows = macro.OutputFlows;
						foreach(var flow in outputFlows) {
							if(flow != null && flow.target.GetTargetNode() == target) {
								comp.Add(node);
								break;
							}
						}
					}
					currNode = node;
					AnalizerUtility.AnalizeObject(node, validation);
				}
				foreach(EventNode method in generatorData.eventNodes) {
					if(method == null)
						continue;
					foreach(var flow in method.GetFlows()) {
						if(flow.GetTargetNode() == target) {
							comp.Add(method);
							break;
						}
					}
				}
				generatorData.FlowConnectedTo[target] = comp;
				return comp;
			}
			return null;
		}

		/// <summary>
		/// Get all connection node from target.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public static IList<NodeComponent> GetNodeConnectedTo(Node target) {
			if(target != null) {
				if(generatorData.EventConnectedInNode.ContainsKey(target)) {
					return generatorData.EventConnectedInNode[target];
				}
				int num = 0;
				List<NodeComponent> comp = new List<NodeComponent>();
				foreach(Node node in generatorData.allNode) {
					if(target == node)
						continue;
					if(node is StateNode) {
						StateNode eventNode = node as StateNode;
						TransitionEvent[] TE = eventNode.GetTransitions();
						foreach(TransitionEvent transition in TE) {
							if(transition != null && transition.GetTargetNode() == target) {
								if(!comp.Contains(node))
									comp.Add(node);
								num++;
							}
						}
					}
				}
				foreach(EventNode method in generatorData.eventNodes) {
					if(method == null)
						continue;
					foreach(var flow in method.GetFlows()) {
						if(flow.GetTargetNode() == target) {
							if(!comp.Contains(method))
								comp.Add(method);
							num++;
						}
					}
				}
				var compArray = comp;
				generatorData.ConnectedInNodeCount.Add(target, num);
				generatorData.EventConnectedInNode.Add(target, compArray);
				return compArray;
			}
			return null;
		}
		#endregion
	}
}