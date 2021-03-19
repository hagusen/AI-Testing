using UnityEngine;
using System.Collections;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Decorators", "Failer", HideOnFlow = true)]
	[Description("Always failure regardless of whether the targetNode success or failure.")]
	public class Failer : Node {
		[Hide, FieldConnection("", true)]
		public MemberData targetNode = new MemberData();

		public override void OnExecute() {
			owner.StartCoroutine(Do(), this);
		}

		IEnumerator Do() {
			if(!targetNode.isAssigned) {
				Debug.LogError("Unassigned target node", this);
				Finish();
				yield break;
			}
			Node n;
			WaitUntil w;
			if(!targetNode.ActivateFlowNode(out n, out w)) {
				yield return w;
			}
			if(n == null) {
				throw new System.Exception("targetNode must be FlowNode");
			}
			JumpStatement js = n.GetJumpState();
			if(js != null) {
				jumpState = js;
			}
			state = StateType.Failure;
			Finish();
		}

		public override bool IsSelfCoroutine() {
			return true;
		}

		public override void RegisterPort() {
			base.RegisterPort();
			if(CodeGenerator.isGenerating) {
				//Register this node as state node, because this is coroutine node with state.
				CodeGenerator.RegisterAsStateNode(this);
				var node = targetNode.GetTargetNode();
				if(node != null) {
					//Register each target node as state node, because this node need to compare the target state.
					CodeGenerator.RegisterAsStateNode(node);
				}
			}
		}

		public override string GenerateCode() {
			if(targetNode.isAssigned) {
				Node n = targetNode.GetTargetNode();
				return CodeGenerator.WaitEvent(n) + CodeGenerator.GetFinishCode(this, false).AddLineInFirst();
			}
			return null;
		}
	}
}
