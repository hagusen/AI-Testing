using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Other", "StopFlowNode", HideOnFlow = true)]
	public class StopFlowNode : Node {
		[HideInInspector, FieldConnection(true)]
		public List<MemberData> flowNodes = new List<MemberData>() { new MemberData() };
		[Hide, FieldConnection("Next", true)]
		public MemberData nextNode = new MemberData();

		public override void OnExecute() {
			foreach(var flow in flowNodes) {
				if(!flow.isAssigned)
					continue;
				Node node = flow.GetTargetNode();
				if(node) {
					node.Stop();
					if(uNodeUtility.isInEditor && uNodeUtility.useDebug) {
						int integer = int.Parse(flow.startName);
						uNodeUtility.InvokeFlowTransition(node.owner, node.owner.GetInstanceID(), node.GetInstanceID(), integer);
					}
				}
			}
			Finish(nextNode);
		}

		public override string GenerateCode() {
			string data = null;
			for(int i = 0; i < flowNodes.Count; i++) {
				if(!flowNodes[i].isAssigned)
					continue;
				data += CodeGenerator.StopEvent(flowNodes[i].GetTargetNode(), false).AddLineInFirst();
			}
			return data + CodeGenerator.GetFinishCode(this, true, false, nextNode).AddLineInFirst();
		}

		public override bool IsCoroutine() {
			return HasCoroutineInFlow(nextNode);
		}
	}
}
