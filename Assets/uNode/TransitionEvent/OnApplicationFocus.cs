namespace MaxyGames.uNode.Transition {
	[UnityEngine.AddComponentMenu("")]
	[TransitionMenu("OnApplicationFocus", "OnApplicationFocus")]
	public class OnApplicationFocus : TransitionEvent {
		[Filter(typeof(bool), SetMember =true)]
		public MemberData storeValue = new MemberData();

		public override void OnEnter() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onApplicationFocus += Execute;
				uNodeHelper.InitializeRuntimeFunctionCallers(graph, owner.gameObject);
			}
		}

		void Execute(bool val) {
			if(storeValue.isAssigned) {
				storeValue.Set(val);
			}
			Finish();
		}

		public override void OnExit() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onApplicationFocus -= Execute;
			}
		}

		public override string GenerateOnEnterCode() {
			if(GetTargetNode() == null)
				return null;
			if(!CodeGenerator.HasInitialized(this)) {
				CodeGenerator.SetInitialized(this);
				var mData = CodeGenerator.generatorData.GetMethodData("OnApplicationFocus");
				if(mData == null) {
					mData = CodeGenerator.generatorData.AddMethod(
						"OnApplicationFocus",
						CodeGenerator.ParseType(typeof(void)),
						CodeGenerator.ParseType(typeof(bool)));
				}
				string set = null;
				if(storeValue.isAssigned) {
					set = CodeGenerator.GenerateSetCode(CodeGenerator.ParseValue(storeValue), mData.parameters[0].name).AddLineInEnd();
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
