namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Operator", "Coalescing {??}")]
	public class CoalescingNode : ValueNode {
		[Hide, FieldConnection()]
		public MemberData targetA = MemberData.none;
		[Hide, FieldConnection(), ObjectType("targetA")]
		public MemberData targetB = MemberData.none;

		public override System.Type ReturnType() {
			if(targetA.isAssigned || targetB.isAssigned) {
				try {
					if(targetA.isAssigned) {
						return targetA.type;
					} else {
						return targetB.type;
					}
				}
				catch { }
			}
			return typeof(object);
		}

		protected override object Value() {
			return targetA.Get() ?? targetB.Get();
		}

		public override string GenerateValueCode() {
			return CodeGenerator.ParseValue((object)targetA) + " ?? " + CodeGenerator.ParseValue((object)targetB);
		}

		public override string GetNodeName() {
			return "??";
		}

		public override string GetRichName() {
			return targetA.GetNicelyDisplayName(richName:true) + " ?? " + targetB.GetNicelyDisplayName(richName:true);
		}

		public override void CheckError() {
			base.CheckError();
			uNodeUtility.CheckError(targetA, this, "targetA");
			uNodeUtility.CheckError(targetB, this, "targetB");
		}
	}
}