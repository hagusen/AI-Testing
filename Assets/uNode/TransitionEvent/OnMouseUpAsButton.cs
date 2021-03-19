namespace MaxyGames.uNode.Transition {
	[UnityEngine.AddComponentMenu("")]
	[TransitionMenu("OnMouseUpAsButton", "OnMouseUpAsButton")]
	public class OnMouseUpAsButton : TransitionEvent {
		public override void OnEnter() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onMouseUpAsButton += Execute;
				uNodeHelper.InitializeRuntimeFunctionCallers(graph, owner.gameObject);
			}
		}

		void Execute() {
			Finish();
		}

		public override void OnExit() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onMouseUpAsButton -= Execute;
			}
		}

		public override string GenerateOnEnterCode() {
			if(GetTargetNode() == null)
				return null;
			if(!CodeGenerator.HasInitialized(this)) {
				CodeGenerator.SetInitialized(this);
				CodeGenerator.InsertMethodCode(
					"OnMouseUpAsButton",
					typeof(void),
					CodeGenerator.GenerateCondition("if", CodeGenerator.CompareNodeState(node, null), CodeGenerator.GetFinishCode(this)));
			}
			return null;
		}
	}
}
