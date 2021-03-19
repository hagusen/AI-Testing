using UnityEngine;
using System.Collections;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Decorators", "Until Success", IsCoroutine = true)]
	[DescriptionAttribute("Will execute target until target Success" +
		"\nThis will always return success.")]
	public class UntilSuccess : Node {
		[Hide, FieldConnection("", true)]
		public MemberData targetNode = new MemberData();

		public IEnumerator OnUpdate() {
			while(state == StateType.Running) {
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
					if(js.jumpType == JumpStatementType.Continue) {
						continue;
					} else if(js.jumpType == JumpStatementType.Break) {
						Finish();
						yield break;
					}
					jumpState = js;
					Finish();
					yield break;
				}
				if(n.currentState == StateType.Success) {
					Finish();
					yield break;
				}
				yield return null;
			}
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
				string data = "while(";
				string condition = CodeGenerator.CompareNodeState(n, true, true);
				if(!string.IsNullOrEmpty(condition)) {
					string invokeCode = CodeGenerator.GetInvokeNodeCode(n);
					data = data.Insert(0, invokeCode + "\n");
					data += condition + "){\n" + invokeCode + "\nyield return null;\n}";
				}
				data += CodeGenerator.GetFinishCode(this, true, false, false);
				return data;
			}
			return null;
		}
	}
}
