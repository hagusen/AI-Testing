using UnityEngine;
using System.Collections;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Statement", "Do While")]
	[Description("The do while statement executes first 1x body and will loop calling a body until a Condition evaluates to false.")]
	public class DoWhileLoop : Node {
		[Hide, FieldConnection("Condition", false, false), Filter(typeof(bool))]
		public MemberData condition = new MemberData(true);

		[HideInInspector, FieldConnection("Body", true, displayFlowInHierarchy =false)]
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
				do {
					if(body == null || !body.isAssigned)
						continue;
					Node n;
					WaitUntil w;
					if(!body.ActivateFlowNode(out n, out w)) {
						throw new System.Exception("body is not coroutine but body is not finished.");
					}
					if(n == null)//Skip on executing flow input pin.
						continue;
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
				} while(condition.GetValue<bool>());
				Finish(onFinished);
			}
		}

		IEnumerator OnUpdate() {
			do {
				if(body == null || !body.isAssigned)
					continue;
				Node n;
				WaitUntil w;
				if(!body.ActivateFlowNode(out n, out w)) {
					yield return w;
				}
				if(n == null)//Skip on executing flow input pin.
					continue;
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
			} while(condition.GetValue<bool>());
			Finish(onFinished);
		}

		public override bool IsCoroutine() {
			return HasCoroutineInFlow(body, onFinished);
		}

		public override string GenerateCode() {
			string data = CodeGenerator.ParseValue((object)condition);
			if(!string.IsNullOrEmpty(data)) {
				data = CodeGenerator.GenerateCondition("do", data, CodeGenerator.GenerateFlowCode(body, this));
			}
			return data + CodeGenerator.GetFinishCode(this, true, false, false, onFinished).AddLineInFirst();
		}

		public override string GetRichName() {
			return uNodeUtility.WrapTextWithKeywordColor("do while: ") + condition.GetNicelyDisplayName(richName:true);
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

	public class CustomInputDWLItem : CustomInputPortItem {
		public override IList<ItemSelector.CustomItem> GetItems(Node source, MemberData data, System.Type type) {
			var items = new List<ItemSelector.CustomItem>();
			items.Add(new ItemSelector.CustomItem("Do While", () => {
				NodeEditorUtility.AddNewNode(graph.editorData, null, null, mousePositionOnCanvas, (DoWhileLoop n) => {
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