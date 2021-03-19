namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Yield", "Yield Break", IsCoroutine = true, HideOnStateMachine =true)]
	public class NodeYieldBreak : Node {
		public override void OnExecute() {
			jumpState = new JumpStatement(JumpStatementType.Return, this);
			Finish();
		}

		public override string GenerateCode() {
			return CodeGenerator.GenerateYieldBreak();
		}

		public override string GetNodeName() {
			return "YieldBreak";
		}
	}
}