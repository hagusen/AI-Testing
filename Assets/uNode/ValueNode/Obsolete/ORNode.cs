namespace MaxyGames.uNode.Nodes {
	public class ORNode : ValueNode {
		[Hide, FieldConnection(), Filter(typeof(bool))]
		public MemberData targetA;
		[Hide, FieldConnection(), Filter(typeof(bool))]
		public MemberData targetB;

		public override System.Type ReturnType() {
			return typeof(bool);
		}

		protected override object Value() {
			return targetA.GetValue<bool>() || targetB.GetValue<bool>();
		}

		public override string GenerateValueCode() {
			if(targetA.isAssigned && targetB.isAssigned) {
				return "(" + CodeGenerator.ParseValue((object)targetA) + " || " + CodeGenerator.ParseValue((object)targetB) + ")";
			}
			throw new System.Exception("Target is unassigned");
		}

		public override string GetNodeName() {
			return "OR";
		}

		public override System.Type GetNodeIcon() {
			return typeof(TypeIcons.OrIcon2);
		}

		public override void CheckError() {
			base.CheckError();
			uNodeUtility.CheckError(targetA, this, "targetA");
			uNodeUtility.CheckError(targetB, this, "targetB");
		}
	}
}