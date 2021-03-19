namespace MaxyGames.uNode.Nodes {
    [NodeMenu("Data", "IS", typeof(bool))]
	public class ISNode : ValueNode {
		[Hide, FieldDrawer("Type"), Filter(OnlyGetType = true)]
		public MemberData type = new MemberData(typeof(object), MemberData.TargetType.Type);
		[Hide, FieldConnection()]
		public MemberData target;

		public override System.Type ReturnType() {
			return typeof(bool);
		}

		protected override object Value() {
			return Operator.TypeIs(target.Get(), type.Get<System.Type>());
		}

		public override string GenerateValueCode() {
			if(target.isAssigned && type.isAssigned) {
				return CodeGenerator.GenerateIsCode(target, type.startType);
			}
			throw new System.Exception();
		}
		public override string GetNodeName() {
			return "IS";
		}

		public override string GetRichName() {
			return target.GetNicelyDisplayName(richName:true) + uNodeUtility.WrapTextWithKeywordColor(" is ") + target.GetNicelyDisplayName(richName:true, typeTargetWithTypeof:false);
		}

		public override System.Type GetNodeIcon() {
			if(type.isAssigned) {
				return type.startType;
			}
			return typeof(bool);
		}

		public override void CheckError() {
			base.CheckError();
			uNodeUtility.CheckError(target, this, "target");
		}
	}
}