namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Flow", "Once")]
	public class FlowOnce : CustomNode {
		[System.NonSerialized]
		public FlowInput input = new FlowInput("In");
		[System.NonSerialized]
		public FlowInput reset = new FlowInput("Reset");

		[Hide, FieldConnection("", true, isFinishedFlow =true)]
		public MemberData output = new MemberData();

		private bool hasEnter = false;

		public override void RegisterPort() {
			input.onExecute = () => {
				if(!hasEnter) {
					hasEnter = true;
					Finish(output);
				}
			};
			reset.onExecute = () => {
				hasEnter = false;
			};
			if(CodeGenerator.isGenerating) {
				CodeGenerator.RegisterFlowNode(this);
				string varName = CodeGenerator.AddVariable(new VariableData("hasEnter", typeof(bool), false) {
					modifier = FieldModifier.PrivateModifier,
				});
				input.codeGeneration = () => {
					return CodeGenerator.GenerateIfStatement(varName.NotOperation(), varName.Set(true.ToCode()).AddStatement(CodeGenerator.GetFinishCode(this, true, output)));
				};
				reset.codeGeneration = () => {
					return varName.Set(false.ToCode());
				};
			}
		}

		public override bool IsCoroutine() {
			return HasCoroutineInFlow(output);
		}
	}
}
