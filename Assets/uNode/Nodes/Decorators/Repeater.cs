using UnityEngine;
using System.Collections;
using System;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Decorators", "Repeater", IsCoroutine = true)]
	[Description("Repeat target node until target node is run in specified number of time." +
		"\nThis will always return success.")]
	public class Repeater : Node {
		[Tooltip("The number repeat time.")]
		public int RepeatCount = 1;
		[Tooltip("If true, this will repeat forever except StopEventOnFailure is true and called event is failure.")]
		public bool RepeatForever = false;
		[Tooltip("If called event Failure, this will stop to repeat event\n" +
		"This will always return success.")]
		public bool StopEventOnFailure = false;

		[Hide, FieldConnection("", true)]
		public MemberData targetNode = new MemberData();

		private int repeatNumber;
		private bool canExecuteEvent;

		public IEnumerator OnUpdate() {
			while(state == StateType.Running) {
				if(!targetNode.isAssigned) {
					Debug.LogError("Unassigned target node", this);
					Finish();
					yield break;
				}
				Node n;
				if(canExecuteEvent && (RepeatForever || RepeatCount > repeatNumber)) {
					WaitUntil w;
					if(!targetNode.ActivateFlowNode(out n, out w)) {
						yield return w;
					}
					repeatNumber++;
					canExecuteEvent = false;
				} else {
					n = targetNode.GetTargetNode();
				}
				if(n.IsFinished()) {
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
					if(StopEventOnFailure && n.currentState == StateType.Failure) {
						Finish();
						yield break;
					}
					if(!RepeatForever && RepeatCount <= repeatNumber) {
						Finish();
					}
					canExecuteEvent = true;
				}
				yield return null;
			}
		}

		public override void OnExecute() {
			canExecuteEvent = true;
			repeatNumber = 0;
			owner.StartCoroutine(OnUpdate(), this);
		}

		public override bool IsSelfCoroutine() {
			return true;
		}

		//public override void RegisterPort() {
		//	base.RegisterPort();
		//	if(CodeGenerator.isGenerating) {
		//		//Register this node as state node, because this is coroutine node with state.
		//		CodeGenerator.RegisterAsStateNode(this);
		//		var node = targetNode.GetTargetNode();
		//		if(node != null) {
		//			//Register each target node as state node, because this node need to compare the target state.
		//			CodeGenerator.RegisterAsStateNode(node);
		//		}
		//	}
		//}

		public override string GenerateCode() {
			if(targetNode.isAssigned) {
				string tData = CodeGenerator.GenerateFlowCode(targetNode, this);
				string data = "while(";
				if(RepeatForever) {
					data += "true){" + (tData + "\nyield return null;").AddLineInFirst().AddLineInEnd().AddTabAfterNewLine() + "\n}";
				} else {
					string varName = CodeGenerator.GenerateVariableName("variable", this);
					string var = "int " + varName + " = 0;";
					data = data.AddFirst(var.AddLineInEnd());
					data += varName + " < " + CodeGenerator.ParseValue(RepeatCount) + "){" + (tData.AddLineInEnd() + varName + "++;\nyield return null;").AddLineInFirst().AddTabAfterNewLine() + "\n}";
				}
				data += CodeGenerator.GetFinishCode(this, true, false, false).AddLineInFirst();
				return data;
			}
			throw new System.Exception("Target node is unassigned.");
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.RepeatIcon);
		}
	}
}
