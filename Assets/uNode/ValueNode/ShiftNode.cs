namespace MaxyGames.uNode.Nodes {
    [NodeMenu("Operator", "Shift {<<} {>>}")]
	public class ShiftNode : ValueNode {
		public ShiftType operatorType = ShiftType.LeftShift;
		[Hide, FieldConnection(false)]
		public MemberData targetA = new MemberData();
		[Hide, FieldConnection(false), Filter(typeof(int))]
		public MemberData targetB = new MemberData();

		public override System.Type ReturnType() {
			if(targetA.isAssigned && targetB.isAssigned) {
				try {
					object obj = uNodeHelper.ShiftOperator(
						ReflectionUtils.CreateInstance(targetA.type),
						default(int), operatorType);
					if(!object.ReferenceEquals(obj, null)) {
						return obj.GetType();
					}
				}
				catch { }
			}
			return typeof(object);
		}

		protected override object Value() {
			return uNodeHelper.ShiftOperator(targetA.Get(), targetB.GetValue<int>(), operatorType);
		}

		public override string GenerateValueCode() {
			if(targetA.isAssigned && targetB.isAssigned) {
				return CodeGenerator.GetOperatorCode(CodeGenerator.ParseValue(targetA),
					CodeGenerator.ParseValue(targetB), operatorType).AddFirst("(").Add(")");
			}
			throw new System.Exception();
		}

		public override string GetNodeName() {
			return operatorType.ToString();
		}

		public override string GetRichName() {
			return CodeGenerator.GetOperatorCode(targetA.GetNicelyDisplayName(richName:true), targetB.GetNicelyDisplayName(richName:true), operatorType);
		}

		public override void CheckError() {
			base.CheckError();
			uNodeUtility.CheckError(targetA, this, "targetA");
			uNodeUtility.CheckError(targetB, this, "targetB");
			if(targetA.isAssigned && targetB.isAssigned) {
				try {
					if(targetA.type != null) {
						uNodeHelper.ShiftOperator(
						ReflectionUtils.CreateInstance(targetA.type),
						default(int), operatorType);
					}
				}
				catch(System.Exception ex) {
					RegisterEditorError(ex.Message);
				}
			}
		}
	}
}