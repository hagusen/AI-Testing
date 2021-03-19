namespace MaxyGames.uNode.Transition {
	[UnityEngine.AddComponentMenu("")]
	[TransitionMenu("OnBecameVisible", "OnBecameVisible")]
	public class OnBecameVisible : TransitionEvent {
		public override void OnEnter() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onBecameVisible += Execute;
				uNodeHelper.InitializeRuntimeFunctionCallers(graph, owner.gameObject);
			}
		}

		void Execute() {
			Finish();
		}

		public override void OnExit() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onBecameVisible -= Execute;
			}
		}

		public override string GenerateOnEnterCode() {
			if(GetTargetNode() == null)
				return null;
			if(!CodeGenerator.HasInitialized(this)) {
				CodeGenerator.SetInitialized(this);
				CodeGenerator.InsertMethodCode(
					"OnBecameVisible",
					typeof(void),
					CodeGenerator.GenerateCondition("if", CodeGenerator.CompareNodeState(node, null), CodeGenerator.GetFinishCode(this)));
			}
			return null;
		}
	}
}
