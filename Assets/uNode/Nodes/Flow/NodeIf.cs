using System;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Flow", "If")]
	public class NodeIf : Node {
		[Hide, FieldConnection("", false, true), Filter(typeof(bool)), UnityEngine.Tooltip("The condition to compare")]
		public MemberData condition = new MemberData(true);
		[Hide, FieldConnection("True", true), UnityEngine.Tooltip("Executed if condition is true")]
		public MemberData onTrue = new MemberData();
		[Hide, FieldConnection("False", true), UnityEngine.Tooltip("Executed if condition is false")]
		public MemberData onFalse = new MemberData();
		[Hide, FieldConnection("Finished", true)]
		public MemberData onFinished = new MemberData();

		public override void OnExecute() {
			if(condition.GetValue<bool>()) {
				state = StateType.Success;
				ExecuteFlow(onTrue, onFinished);
			} else {
				state = StateType.Failure;
				ExecuteFlow(onFalse, onFinished);
			}
		}

		public override string GenerateCode() {
			if(condition != null && condition.isAssigned) {
				string data = CodeGenerator.ParseValue((object)condition);
				if(!string.IsNullOrEmpty(data)) {
					if(CodeGenerator.debugScript || CodeGenerator.CanReturnState(this)) {
						return CodeGenerator.GenerateIfStatement(data,
							CodeGenerator.GetFinishCode(this, true, true, false, onTrue, onFinished),
							CodeGenerator.GetFinishCode(this, false, true, false, onFalse, onFinished));
					}
					if(onTrue.isAssigned) {
						if(onFalse.isAssigned) {
							data = CodeGenerator.GenerateIfStatement(data, CodeGenerator.GenerateFlowCode(onTrue, this));
							string failure = CodeGenerator.GenerateFlowCode(onFalse, this);
							if(!string.IsNullOrEmpty(failure)) {
								var flag = onFalse.targetType == MemberData.TargetType.FlowNode && 
									onFalse.GetTargetNode() is NodeIf && 
									!(onFalse.GetTargetNode() as NodeIf).onFinished.isAssigned;
								if(flag && CodeGenerator.IsGroupedNode(this)) {
									data += " else " + failure.RemoveLineAndTabOnFirst();
								} else {
									data += " else {" + failure.AddLineInFirst().AddTabAfterNewLine(1) + "\n}";
								}
							}
						} else {
							data = CodeGenerator.GenerateIfStatement(data, CodeGenerator.GenerateFlowCode(onTrue, this));
						}
						data += CodeGenerator.GetFinishCode(this, true, false, false, onFinished).AddLineInFirst();
						return data;
					} else if(onFalse.isAssigned) {
						return 
							CodeGenerator.GenerateIfStatement(
								data.AddFirst("!(").Add(")"), 
								CodeGenerator.GenerateFlowCode(onFalse, this)) + 
							CodeGenerator.GetFinishCode(this, false, true, false, onFinished).AddLineInFirst();
					}
					return 
						CodeGenerator.GenerateIfStatement(data, "") + 
						CodeGenerator.GetFinishCode(this, true, false, false, onFinished).AddLineInFirst();
				} else {
					throw new Exception("Condition generates empty code");
				}
			}
			throw new System.Exception("Unassigned condition");
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.BranchIcon);
		}

		public override string GetRichName() {
			if(condition.isAssigned) {
				return $"If: {condition.GetNicelyDisplayName(richName:true)}";
			}
			return base.GetRichName();
		}

		public override void CheckError() {
			base.CheckError();
			uNodeUtility.CheckError(condition, this, "condition");
		}

		public override bool IsCoroutine() {
			return HasCoroutineInFlow(onFinished, onTrue, onFalse);
		}
	}
}

#if UNITY_EDITOR
namespace MaxyGames.uNode.Editors.Commands {
	using UnityEngine;
	using System.Collections.Generic;
	using MaxyGames.uNode.Nodes;
	using MaxyGames.Events;

	public class ConvertIfToValidationCommands : NodeMenuCommand {
		public override string name {
			get {
				return "Convert to Validation";
			}
		}

		public override void OnClick(Node source, Vector2 mousePosition) {
			NodeIf node = source as NodeIf;
			NodeEditorUtility.AddNewNode(graph.editorData, null, null, mousePositionOnCanvas, (NodeValidation n) => {
				var action = new EqualityCompare();
				action.target = new MultipurposeMember(node.condition);
				action.value = new MemberData(true);
				n.Validation.AddBlock(action, EventActionData.EventType.Event);
				n.onTrue = node.onTrue;
				n.onFalse = node.onFalse;
				n.onFinished = node.onFinished;
				n.editorRect = node.editorRect;
				RefactorUtility.RetargetNode(node, n);
			});
			NodeEditorUtility.RemoveNode(graph.editorData, node);
			graph.Refresh();
		}

		public override bool IsValidNode(Node source) {
			if(source is NodeIf) {
				return true;
			}
			return false;
		}
	}

	public class CustomInputIFItem : CustomInputPortItem {
		public override IList<ItemSelector.CustomItem> GetItems(Node source, MemberData data, System.Type type) {
			var items = new List<ItemSelector.CustomItem>();
			items.Add(new ItemSelector.CustomItem("IF", () => {
				NodeEditorUtility.AddNewNode(graph.editorData, null, null, mousePositionOnCanvas, (NodeIf n) => {
					n.condition = data;
					graph.Refresh();
				});
			}, "Flows") { icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon)) });
			return items;
		}

		public override bool IsValidPort(Type type) {
			return type == typeof(bool);
		}
	}
}
#endif