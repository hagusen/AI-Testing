using System;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.uNode.Editors {
	public static class PortUtility {
		public static PortView GetInputPort(MemberData outputValue, UGraphView graphView) {
			var inputNode = outputValue.GetTargetNode();
			if(inputNode != null) {
				if(outputValue.IsTargetingNode) {
					switch(outputValue.targetType) {
						case MemberData.TargetType.FlowNode:
							PortView port;
							if(graphView.portFlowNodeAliases.TryGetValue(inputNode, out port) && port != null) {
								return port;
							}
							return GetSelfPort(inputNode, Orientation.Vertical, graphView);
						case MemberData.TargetType.ValueNode:
							return GetSelfPort(inputNode, Orientation.Horizontal, graphView);
					}
				}
				UNodeView nodeView;
				if(graphView.nodeViewsPerNode.TryGetValue(inputNode, out nodeView)) {
					foreach(var p in nodeView.inputPorts) {
						if(p.portData.portID == outputValue.startName) {
							return p;
						}
					}
				}
			}
			return null;
		}

		public static PortView GetOutputPort(MemberData inputValue, UGraphView graphView) {
			var outputNode = inputValue.GetTargetNode();
			if(outputNode != null) {
				if(inputValue.IsTargetingNode) {
					switch(inputValue.targetType) {
						case MemberData.TargetType.FlowNode:
							return GetSelfPort(outputNode, Orientation.Vertical, graphView);
						case MemberData.TargetType.ValueNode:
							PortView port;
							if(graphView.portValueNodeAliases.TryGetValue(outputNode, out port) && port != null) {
								return port;
							}
							return GetSelfPort(outputNode, Orientation.Horizontal, graphView);
					}
				}
				UNodeView nodeView;
				if(graphView.nodeViewsPerNode.TryGetValue(outputNode, out nodeView)) {
					foreach(var p in nodeView.outputPorts) {
						if(p.portData.portID == inputValue.startName) {
							return p;
						}
					}
				}
			}
			return null;
		}

		public static PortView GetSelfPort(NodeComponent node, Orientation orientation, UGraphView graphView) {
			UNodeView nodeView;
			if(graphView.nodeViewsPerNode.TryGetValue(node, out nodeView)) {
				if(orientation == Orientation.Horizontal) {
					foreach(var p in nodeView.outputPorts) {
						if(p.orientation == orientation && p.portData.portID == UGraphView.SelfPortID) {
							return p;
						}
					}
				} else {
					foreach(var p in nodeView.inputPorts) {
						if(p.orientation == orientation && p.portData.portID == UGraphView.SelfPortID) {
							return p;
						}
					}
				}
			}
			return null;
		}
	}
}