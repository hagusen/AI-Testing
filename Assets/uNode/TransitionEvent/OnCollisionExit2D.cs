﻿namespace MaxyGames.uNode.Transition {
	[UnityEngine.AddComponentMenu("")]
	[TransitionMenu("OnCollisionExit2D", "OnCollisionExit2D")]
	public class OnCollisionExit2D : TransitionEvent {
		[Filter(typeof(UnityEngine.Collision2D), SetMember = true)]
		public MemberData storeCollision = new MemberData();

		public override void OnEnter() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onCollisionExit2D += Execute;
				uNodeHelper.InitializeRuntimeFunctionCallers(graph, owner.gameObject);
			}
		}

		void Execute(UnityEngine.Collision2D collision) {
			if(storeCollision.isAssigned) {
				storeCollision.Set(collision);
			}
			Finish();
		}

		public override void OnExit() {
			if(owner is IGraphWithUnityEvent graph) {
				graph.onCollisionExit2D -= Execute;
			}
		}

		public override string GenerateOnEnterCode() {
			if(GetTargetNode() == null)
				return null;
			if(!CodeGenerator.HasInitialized(this)) {
				CodeGenerator.SetInitialized(this);
				var mData = CodeGenerator.generatorData.GetMethodData("OnCollisionExit2D");
				if(mData == null) {
					mData = CodeGenerator.generatorData.AddMethod(
						"OnCollisionExit2D",
						CodeGenerator.ParseType(typeof(void)),
						CodeGenerator.ParseType(typeof(UnityEngine.Collision2D)));
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
