namespace MaxyGames.uNode.Transition {
	[UnityEngine.AddComponentMenu("")]
	[TransitionMenu("OnMouseExit", "OnMouseExit")]
	public class OnMouseExit : TransitionEvent {
		public override void OnEnter() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onMouseExit += Execute;
				uNodeHelper.InitializeRuntimeFunctionCallers(graph, owner.gameObject);
			}
		}

		void Execute() {
			Finish();
		}

		public override void OnExit() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onMouseExit -= Execute;
			}
		}

		public override string GenerateOnEnterCode() {
			if(GetTargetNode() == null)
				return null;
			if(!CodeGenerator.HasInitialized(this)) {
				CodeGenerator.SetInitialized(this);
				CodeGenerator.InsertMethodCode(
					"OnMouseExit",
					typeof(void),
					CodeGenerator.GenerateCondition("if", CodeGenerator.CompareNodeState(node, null), CodeGenerator.GetFinishCode(this)));
			}
			return null;
		}
	}
}
