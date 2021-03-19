using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Composites", "Selector", IsCoroutine = true)]
	[Description("Execute node until one of node Success or all node Failure."
		+ "\nit similar to an \"Or\" operator."
		+ "\nif one of the node success return success \nif no node Success return Failure.")]
	public class Selector : Node {
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
				if(n.currentState == StateType.Success) {
					state = StateType.Success;
					Finish();
					yield break;
				}
			}
			state = StateType.Failure;
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
				string successReturnVal = CodeGenerator.GetFinishCode(this, true);
				for(int i = 0; i < targetNodes.Count; i++) {
					var t = targetNodes[i];
					if(t == null || !t.isAssigned)
						continue;
					data += CodeGenerator.GenerateFlowCode(t, this).AddFirst("\n", data);
					if(CodeGenerator.debugScript) {
						data += CodeGenerator.GenerateDebugCode(t).AddLineInEnd();
					}
					data += "if(".AddFirst("\n", data) + CodeGenerator.CompareNodeState(t.GetTargetNode(), true, false) + ") {" +
						successReturnVal.AddFirst("\n").AddTabAfterNewLine(1).AddLineInEnd() + "}";
				}
				data += CodeGenerator.GetFinishCode(this, false, true, false);
			}
			return data;
		}

		public override System.Type GetNodeIcon() {
			return typeof(TypeIcons.BranchIcon);
		}
	}
}
