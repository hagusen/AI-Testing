using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using NodeView = UnityEditor.Experimental.GraphView.Node;

namespace MaxyGames.uNode.Editors {
	[NodeCustomEditor(typeof(BaseEventNode))]
	public class EventNodeView : BaseNodeView {
		protected override void InitializeView() {
			base.InitializeView();
			BaseEventNode node = targetNode as BaseEventNode;

			#region Title
			if(node is EventNode) {
				if((node as EventNode).eventType != EventNode.EventType.Custom) {
					title = ObjectNames.NicifyVariableName((node as EventNode).eventType.ToString());
				}
			} else if(node is StateEventNode) {
				title = ObjectNames.NicifyVariableName((node as StateEventNode).eventType.ToString());
			}
			#endregion

			var flows = node.GetFlows();
			if(flows.Count == 0) {
				flows.Add(MemberData.none);
				MarkRepaint();
			}
			for(int x = 0; x < flows.Count; x++) {
				int index = x;
				AddOutputFlowPort(
					new PortData() {
						getPortValue = () => flows[index],
						onValueChanged = (val) => {
							flows[index] = val as MemberData;
						},
					}
				);
			}
			ControlView control = new ControlView();
			control.style.alignSelf = Align.Center;
			control.Add(new Button(() => {
				if(flows.Count > 1) {
					RegisterUndo();
					flows.RemoveAt(flows.Count - 1);
					MarkRepaint();
				}
			}) { text = "-" });
			control.Add(new Button(() => {
				RegisterUndo();
				flows.Add(MemberData.none);
				MarkRepaint();
			}) { text = "+" });
			AddControl(Direction.Input, control);
		}
	}
}