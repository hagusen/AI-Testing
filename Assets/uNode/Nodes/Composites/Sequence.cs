using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Composites", "Sequence", IsCoroutine = true)]
	[Description("Execute each node and return Success if all event Success"
		+ "\nit similar to an \"And\" operator."
		+ "\nIt will return Failure when one of the event Failure")]
	public class Sequence : Node {
		[HideInInspector, FieldConnection(true)]
		public List<MemberData> targetNodes = new List<MemberData>() { new MemberData() };

		IEnumerator OnUpdate() {
			for(int i = 0; i < targetNodes.Count; i++) {
				var t = targetNodes[i];
				if(!t.isAssigned)
					continue;
				Node n;
				WaitUntil w;
				if(!t.ActivateFlowNode(out n, out w)) {
					yield return w;
				}
				if(n == null) {
					throw new System.Exception("targetNode must be FlowNode");
				}
				JumpStatement js = n.GetJumpState();
				if(js != null) {
					jumpState = js;
					Finish();
					yield break;
				}
				if(n.currentState == StateType.Failure) {
					state = StateType.Failure;
					Finish();
					yield break;
				}
			}
			Finish();
		}

		public override void OnExecute() {
			owner.StartCoroutine(OnUpdate(), this);
		}

		public override bool IsSelfCoroutine() {
			return true;
		}

		public override void RegisterPort() {
			base.RegisterPort();
			if(CodeGenerator.isGenerating) {
				//Register this node as state node, because this is coroutine node with state.
				CodeGenerator.RegisterAsStateNode(this);
				for(int i = 0; i < targetNodes.Count; i++) {
					var node = targetNodes[i].GetTargetNode();
					if(node != null) {
						//Register each target node as state node, because this node need to compare the target state.
						CodeGenerator.RegisterAsStateNode(node);
					}
				}
			}
		}

		public override string GenerateCode() {
			string data = null;
			if(targetNodes.Count > 0) {
				string failReturnVal = CodeGenerator.GetFinishCode(this, false);
				for(int i = 0; i < targetNodes.Count; i++) {
					var t = targetNodes[i];
					if(t == null || !t.isAssigned)
						continue;
					
					data += CodeGenerator.GenerateFlowCode(t, this).AddFirst("\n", data);
					if(CodeGenerator.debugScript) {
						data += CodeGenerator.GenerateDebugCode(t).AddLineInEnd();
					}
					data += "if(".AddFirst("\n", data) + CodeGenerator.CompareNodeState(t.GetTargetNode(), false, false) + "){" +
						failReturnVal.AddLineInFirst().AddTabAfterNewLine(1).AddLineInEnd() + "}";
				}
				data += CodeGenerator.GetFinishCode(this, true, false, false);
			}
			return data;
		}

		public override System.Type GetNodeIcon() {
			return typeof(TypeIcons.BranchIcon);
		}
	}
}
