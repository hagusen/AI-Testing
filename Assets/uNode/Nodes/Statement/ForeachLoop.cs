using System.Collections;
using UnityEngine;

namespace MaxyGames.uNode.Nodes {
    [NodeMenu("Statement", "Foreach")]
	[Description("The foreach statement repeats a body of embedded statements for each element in an array or a generic type.")]
	public class ForeachLoop : Node {
		[Hide, FieldConnection("Collection", false, false), Filter(typeof(IEnumerable))]
		[Tooltip("The target array list or generic list for the loop")]
		public MemberData target = new MemberData();

		[HideInInspector, FieldConnection("Body", true, displayFlowInHierarchy =false)]
		public MemberData body = new MemberData();
		[Hide, Output("Value"), ObjectType("target", isElementType = true)]
		public object loopObject;

		[Hide, FieldConnection("Next", true)]
		public MemberData onFinished = new MemberData();

		public override void OnExecute() {
			if(!HasCoroutineInFlow(body)) {
				IEnumerable lObj = target.Get() as IEnumerable;
				if(lObj != null) {
					foreach(object obj in lObj) {
						if(body == null || !body.isAssigned) continue;
						loopObject = obj;
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
					}
				} else {
					Debug.LogError("The target must be IEnumerable");
				}
				Finish(onFinished);
			} else {
				owner.StartCoroutine(OnUpdate(), this);
			}
		}

		IEnumerator OnUpdate() {
			IEnumerable lObj = target.Get() as IEnumerable;
			if(lObj != null) {
				foreach(object obj in lObj) {
					if(body == null || !body.isAssigned) continue;
					loopObject = obj;
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
				}
			} else {
				Debug.LogError("The target must be IEnumerable");
			}
			Finish(onFinished);
		}

		public override bool IsCoroutine() {
			return HasCoroutineInFlow(body, onFinished);
		}

		public override string GenerateCode() {
			string ta = CodeGenerator.ParseValue((object)target);
			if(!string.IsNullOrEmpty(ta)) {
				string vName = null;
				var field = this.GetType().GetField("loopObject");
				string contents = null;
				if(CodeGenerator.NeedInstanceVariable(this, field, body)) {
					vName = CodeGenerator.GenerateVariableName("tempVar", this);
					contents = CodeGenerator.AddVariable(this, field, target.type.ElementType(), true) + " = " + vName + ";";
				} else {
					vName = CodeGenerator.GetVariableName(this, field);
				}
				contents = contents + CodeGenerator.GenerateFlowCode(body, this).AddLineInFirst();
				string result = CodeGenerator.GenerateCondition("foreach", CodeGenerator.ParseType(target.type.ElementType()) +
					" " + vName + " in " + ta, contents);
				return result + CodeGenerator.GetFinishCode(this, true, false, false, onFinished).AddLineInFirst();
			}
			return null;
		}

		public override string GetRichName() {
			return uNodeUtility.WrapTextWithKeywordColor("foreach:") + target.GetNicelyDisplayName(richName:true);
		}

		public override void CheckError() {
			base.CheckError();
			uNodeUtility.CheckError(target, this, "Collection");
			uNodeUtility.CheckError(body, this, "body");
		}
	}
}

#if UNITY_EDITOR
namespace MaxyGames.uNode.Editors.Commands {
	using System.Collections.Generic;
	using MaxyGames.uNode.Nodes;

	public class CustomInputFEItem : CustomInputPortItem {
		public override IList<ItemSelector.CustomItem> GetItems(Node source, MemberData data, System.Type type) {
			var items = new List<ItemSelector.CustomItem>();
			items.Add(new ItemSelector.CustomItem("Foreach", () => {
				NodeEditorUtility.AddNewNode(graph.editorData, null, null, mousePositionOnCanvas, (ForeachLoop n) => {
					n.target = data;
					graph.Refresh();
				});
			}, "Flows") { icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon)) });
			return items;
		}

		public override bool IsValidPort(System.Type type) {
			return type.IsArray || type.IsCastableTo(typeof(IEnumerable));
		}
	}
}
#endif