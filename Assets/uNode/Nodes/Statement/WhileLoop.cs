using UnityEngine;
using System.Collections;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Statement", "While")]
	[Description("The while statement executes a body until a Condition evaluates to false.")]
	public class WhileLoop : Node {
		[Hide, FieldConnection("Condition", false, false), Filter(typeof(bool))]
		public MemberData condition = new MemberData();

		[HideInInspector, FieldConnection("Body", true)]
		public MemberData body = new MemberData();
		[Hide, FieldConnection("Next", true)]
		public MemberData onFinished = new MemberData();

		public override void OnExecute() {
			if(!body.isAssigned) {
				throw new System.Exception("body is unassigned");
			}
			if(HasCoroutineInFlow(body)) {
				owner.StartCoroutine(OnUpdate(), this);
			} else {
				while(condition.GetValue<bool>()) {
					if(body == null || !body.isAssigned) continue;
					Node n;
					WaitUntil w;
					if(!body.ActivateFlowNode(out n, out w)) {
						throw new System.Exception("body is not coroutine but body is not finished.");
					}
					if(n != null) {
						JumpStatement js = n.GetJumpState();
						if(js != null) {
							if(js.jumpType == JumpStatementType.Continue) {
								continue;
							} else {
								if(js.jumpType == JumpStatementType.Return) {
									jumpState = js;
								}
								break;
							}
						}
					}
				}
				Finish(onFinished);
			}
		}

		IEnumerator OnUpdate() {
			while(condition.GetValue<bool>()) {
				if(body == null || !body.isAssigned) continue;
				Node n;
				WaitUntil w;
				if(!body.ActivateFlowNode(out n, out w)) {
					yield return w;
				}
				if(n != null) {
					JumpStatement js = n.GetJumpState();
					if(js != null) {
						if(js.jumpType == JumpStatementType.Continue) {
							continue;
						} else {
							if(js.jumpType == JumpStatementType.Return) {
								jumpState = js;
							}
							break;
						}
					}
				}
				yield return null;
			}
			Finish(onFinished);
		}

		public override bool IsCoroutine() {
			return HasCoroutineInFlow(body, onFinished);
		}

		public override string GenerateCode() {
			if(!body.isAssigned) {
				throw new System.Exception("body is unassigned");
			}
			string data = CodeGenerator.GenerateCondition("while", CodeGenerator.ParseValue((object)condition), CodeGenerator.GenerateFlowCode(body, this));
			return data + CodeGenerator.GetFinishCode(this, true, false, false, onFinished);
		}

		public override void CheckError() {
			base.CheckError();
			uNodeUtility.CheckError(condition, this, "condition");
			uNodeUtility.CheckError(body, this, "body");
		}
	}
}

#if UNITY_EDITOR
namespace MaxyGames.uNode.Editors.Commands {
	using System.Collections.Generic;
	using MaxyGames.uNode.Nodes;

	public class CustomInputWLItem : CustomInputPortItem {
		public override IList<ItemSelector.CustomItem> GetItems(Node source, MemberData data, System.Type type) {
			var items = new List<ItemSelector.CustomItem>();
			items.Add(new ItemSelector.CustomItem("While", () => {
				NodeEditorUtility.AddNewNode(graph.editorData, null, null, mousePositionOnCanvas, (WhileLoop n) => {
					n.condition = data;
					graph.Refresh();
				});
			}, "Flows") { icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon)) });
			return items;
		}

		public override bool IsValidPort(System.Type type) {
			return type == typeof(bool);
		}
	}
}
#endif