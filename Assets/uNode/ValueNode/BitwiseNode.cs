namespace MaxyGames.uNode.Nodes {
    [NodeMenu("Operator", "Bitwise {|} {&} {^}")]
	public class BitwiseNode : ValueNode {
		public BitwiseType operatorType = BitwiseType.Or;
		[Hide, FieldConnection(false)]
		public MemberData targetA = MemberData.none;
		[Hide, FieldConnection(false), Filter(typeof(int))]
		public MemberData targetB = MemberData.none;

		public override System.Type ReturnType() {
			if(targetA.isAssigned && targetB.isAssigned) {
				try {
					object obj = uNodeHelper.BitwiseOperator(
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
			return uNodeHelper.BitwiseOperator(targetA.Get(), targetB.GetValue<int>(), operatorType);
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
					if(targetA.type != null && targetA.type != typeof(object)) {
						uNodeHelper.BitwiseOperator(
							ReflectionUtils.CreateInstance(targetA.type),
							default(int), 
							operatorType);
					}
				}
				catch(System.Exception ex) {
					RegisterEditorError(ex.Message);
				}
			}
		}
	}
}