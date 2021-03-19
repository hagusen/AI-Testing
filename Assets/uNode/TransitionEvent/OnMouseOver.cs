namespace MaxyGames.uNode.Transition {
	[UnityEngine.AddComponentMenu("")]
	[TransitionMenu("OnMouseOver", "OnMouseOver")]
	public class OnMouseOver : TransitionEvent {
		public override void OnEnter() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onMouseOver += Execute;
				uNodeHelper.InitializeRuntimeFunctionCallers(graph, owner.gameObject);
			}
		}

		void Execute() {
			Finish();
		}

		public override void OnExit() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onMouseOver -= Execute;
			}
		}

		public override string GenerateOnEnterCode() {
			if(GetTargetNode() == null)
				return null;
			if(!CodeGenerator.HasInitialized(this)) {
				CodeGenerator.SetInitialized(this);
				CodeGenerator.InsertMethodCode(
					"OnMouseOver",
					typeof(void),
					CodeGenerator.GenerateCondition("if", CodeGenerator.CompareNodeState(node, null), CodeGenerator.GetFinishCode(this)));
			}
			return null;
		}
	}
}
