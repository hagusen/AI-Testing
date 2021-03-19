using System;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("★General", "Validation", order = 10)]
	public class NodeValidation : Node {
		[EventType(EventData.EventType.Condition)]
		public EventData Validation = new EventData();
		[Hide, FieldConnection("True", true)]
		public MemberData onTrue = new MemberData();
		[Hide, FieldConnection("False", true)]
		public MemberData onFalse = new MemberData();
		[Hide, FieldConnection("Finished", true)]
		public MemberData onFinished = new MemberData();

		public override void OnExecute() {
			if(Validation.Validate(owner)) {
				state = StateType.Success;
				Finish(onFinished, onTrue);
			} else {
				state = StateType.Failure;
				Finish(onFinished, onFalse);
			}
		}

		public override string GenerateCode() {
			if(Validation != null) {
				if(CodeGenerator.debugScript || CodeGenerator.CanReturnState(this)) {
					return Validation.GenerateConditionCode(this,
						CodeGenerator.GetFinishCode(this, true, true, false, onTrue, onFinished),
						CodeGenerator.GetFinishCode(this, false, true, false, onFalse, onFinished)
					);
				}
				if(onTrue.isAssigned) {
					if(onFalse.isAssigned) {
						//True and False is assigned
						return CodeGenerator.GenerateFlowStatement(
							Validation.GenerateConditionCode(this, CodeGenerator.GenerateFlowCode(onTrue, this), CodeGenerator.GenerateFlowCode(onFalse, this)),
							CodeGenerator.GetFinishCode(this, true, false, false, onFinished)
						);
					} else {
						//True only
						return CodeGenerator.GenerateFlowStatement(
							Validation.GenerateConditionCode(this, CodeGenerator.GenerateFlowCode(onTrue, this)),
							CodeGenerator.GetFinishCode(this, true, false, false, onFinished)
						);
					}
				} else if(onFalse.isAssigned) {
					//False only
					return CodeGenerator.GenerateFlowStatement(
						CodeGenerator.GenerateIfStatement(
							Validation.GenerateCode(this, EventData.EventType.Condition).NotOperation(true),
							CodeGenerator.GenerateFlowCode(onFalse, this)),
						CodeGenerator.GetFinishCode(this, false, true, false, onFinished)
					);
				} else {
					//No true and False
					return CodeGenerator.GenerateFlowStatement(
						Validation.GenerateConditionCode(this, null),
						CodeGenerator.GetFinishCode(this, true, false, false, onFinished)
					);
				}
			}
			return CodeGenerator.GetFinishCode(this, true, false, false, onFinished);
		}

		public override void CheckError() {
			base.CheckError();
			Validation.CheckError(this);
		}
		
		public override string GetNodeName() {
			return gameObject.name;
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.ValidationIcon);
		}

		public override bool IsCoroutine() {
			return HasCoroutineInFlow(onFinished, onFalse, onTrue);
		}
	}
}
