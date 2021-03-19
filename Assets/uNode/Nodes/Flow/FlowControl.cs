using UnityEngine;
using System.Collections.Generic;
using System;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Flow", "FlowControl")]
	public class FlowControl : Node {
		[HideInInspector, FieldConnection(true, isFinishedFlow = true)]
		public List<MemberData> nextNode = new List<MemberData>() { new MemberData() };

		public override void OnExecute() {
			Finish(nextNode);
		}

		public override string GenerateCode() {
			string data = null;
			for(int i = 0; i < nextNode.Count; i++) {
				if(!nextNode[i].isAssigned) continue;
				data += CodeGenerator.GenerateFlowCode(nextNode[i], this).AddLineInFirst();
			}
			return data;
		}

		public override bool IsCoroutine() {
			return HasCoroutineInFlow(nextNode);
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.BranchIcon);
		}
	}
}
