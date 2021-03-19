namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Flow", "Toggle")]
	[Description("When In is called, calls On or Off depending on the current toggle state. Whenever Toggle input is called the state changes.")]
	public class FlowToggle : CustomNode {
		[System.NonSerialized]
		public FlowInput input = new FlowInput("In");
		[Hide, FieldConnection("On", true)]
		public MemberData onOpen = new MemberData();
		[System.NonSerialized]
		public FlowInput toggle = new FlowInput("Toggle");
		[Hide, FieldConnection("Off", true)]
		public MemberData onClosed = new MemberData();
		public bool open = false;

		public override void RegisterPort() {
			input = new FlowInput("In", () => {
				if(open) {
					Finish(onOpen);
				} else {
					Finish(onClosed);
				}
			});
			toggle = new FlowInput("Toggle", () => {
				open = !open;
			});
			if(CodeGenerator.isGenerating) {
				CodeGenerator.RegisterFlowNode(this);
				string varName = CodeGenerator.AddVariable(new VariableData("open", typeof(bool), open));
				input.codeGeneration = () => {
					return CodeGenerator.GenerateIfStatement(varName,
						CodeGenerator.GetFinishCode(this, true, onOpen), 
						CodeGenerator.GetFinishCode(this, true, onClosed));
				};
				toggle.codeGeneration = () => {
					return CodeGenerator.GenerateSetCode(varName, "!" + varName);
				};
			}
		}

		public override bool IsCoroutine() {
			return HasCoroutineInFlow(onOpen, onClosed);
		}
	}
}
