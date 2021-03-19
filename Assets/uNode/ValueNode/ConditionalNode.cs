namespace MaxyGames.uNode.Nodes {
    [NodeMenu("Operator", "Conditional {?:}")]
	public class ConditionalNode : ValueNode {
		[Hide, FieldConnection(false), Filter(typeof(bool))]
		public MemberData condition;
		[Hide, FieldConnection()]
		public MemberData onTrue = MemberData.none;
		[Hide, FieldConnection(), ObjectType("onTrue")]
		public MemberData onFalse = MemberData.none;

		public override System.Type ReturnType() {
			if(onTrue.isAssigned || onFalse.isAssigned) {
				try {
					if(onTrue.isAssigned) {
						return onTrue.type;
					} else {
						return onFalse.type;
					}
				}
				catch { }
			}
			return typeof(object);
		}

		protected override object Value() {
			if(condition.isAssigned) {
				return condition.GetValue<bool>() ? onTrue.Get() : onFalse.Get();
			}
			throw new System.Exception();
		}

		public override string GenerateValueCode() {
			if(condition.isAssigned) {
				return "(" + CodeGenerator.ParseValue((object)condition) + " ? " + CodeGenerator.ParseValue((object)onTrue) + " : " + CodeGenerator.ParseValue((object)onFalse) + ")";
			}
			throw new System.Exception();
		}

		public override string GetNodeName() {
			return "?:";
		}

		public override string GetRichName() {
			return $"? {condition.GetNicelyDisplayName(richName:true)} {onTrue.GetNicelyDisplayName(richName:true)} : {onFalse.GetNicelyDisplayName(richName:true)}";
		}

		public override void CheckError() {
			base.CheckError();
			uNodeUtility.CheckError(condition, this, "condition");
			uNodeUtility.CheckError(onTrue, this, "onTrue");
			uNodeUtility.CheckError(onFalse, this, "onFalse");
		}
	}
}