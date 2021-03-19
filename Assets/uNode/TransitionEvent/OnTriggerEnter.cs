﻿namespace MaxyGames.uNode.Transition {
	[UnityEngine.AddComponentMenu("")]
	[TransitionMenu("OnTriggerEnter", "OnTriggerEnter")]
	public class OnTriggerEnter : TransitionEvent {
		[Filter(typeof(UnityEngine.Collider), SetMember = true)]
		public MemberData storeCollider = new MemberData();

		public override void OnEnter() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onTriggerEnter += Execute;
				uNodeHelper.InitializeRuntimeFunctionCallers(graph, owner.gameObject);
			}
		}

		void Execute(UnityEngine.Collider collider) {
			if(storeCollider.isAssigned) {
				storeCollider.Set(collider);
			}
			Finish();
		}

		public override void OnExit() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onTriggerEnter -= Execute;
			}
		}

		public override string GenerateOnEnterCode() {
			if(GetTargetNode() == null)
				return null;
			if(!CodeGenerator.HasInitialized(this)) {
				CodeGenerator.SetInitialized(this);
				var mData = CodeGenerator.generatorData.GetMethodData("OnTriggerEnter");
				if(mData == null) {
					mData = CodeGenerator.generatorData.AddMethod(
						"OnTriggerEnter",
						CodeGenerator.ParseType(typeof(void)),
						CodeGenerator.ParseType(typeof(UnityEngine.Collider)));
				}
				string set = null;
				if(storeCollider.isAssigned) {
					set = CodeGenerator.GenerateSetCode(CodeGenerator.ParseValue(storeCollider), mData.parameters[0].name).AddLineInEnd();
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
