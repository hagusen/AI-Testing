namespace MaxyGames.uNode.Transition {
	[UnityEngine.AddComponentMenu("")]
	[TransitionMenu("OnCollisionEnter", "OnCollisionEnter")]
	public class OnCollisionEnter : TransitionEvent {
		[Filter(typeof(UnityEngine.Collision), SetMember = true)]
		public MemberData storeCollision = new MemberData();

		public override void OnEnter() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onCollisionEnter += Execute;
				uNodeHelper.InitializeRuntimeFunctionCallers(graph, owner.gameObject);
			}
		}

		void Execute(UnityEngine.Collision collision) {
			if(storeCollision.isAssigned) {
				storeCollision.Set(collision);
			}
			Finish();
		}

		public override void OnExit() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onCollisionEnter -= Execute;
			}
		}

		public override string GenerateOnEnterCode() {
			if(GetTargetNode() == null)
				return null;
			if(!CodeGenerator.HasInitialized(this)) {
				CodeGenerator.SetInitialized(this);
				var mData = CodeGenerator.generatorData.GetMethodData("OnCollisionEnter");
				if(mData == null) {
					mData = CodeGenerator.generatorData.AddMethod(
						"OnCollisionEnter", 
						CodeGenerator.ParseType(typeof(void)), 
						CodeGenerator.ParseType(typeof(UnityEngine.Collision)));
				}
				string set = null;
				if(storeCollision.isAssigned) {
					set = CodeGenerator.GenerateSetCode(CodeGenerator.ParseValue(storeCollision), mData.parameters[0].name).AddLineInEnd();
				}
				mData.AddCode(
					CodeGenerator.GenerateCondition(
						"if", 
						CodeGenerator.CompareNodeState(node, null), 
						set + CodeGenerator.GetFinishCode(this)
					)
				);
			}
			return null;
		}
	}
}
