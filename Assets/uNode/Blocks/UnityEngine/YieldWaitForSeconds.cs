using System.Collections;
using UnityEngine;
using MaxyGames.uNode;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "Yield.WaitForSeconds", true)]
	public class YieldWaitForSeconds : CoroutineAction {
		[ObjectType(typeof(float))]
		public MemberData waitTime;
		[Tooltip("If true, suspends the execution using unscaled time.")]
		public bool realTime;

		protected override IEnumerator ExecuteCoroutine() {
			if(!realTime) {
				yield return new WaitForSeconds(waitTime.GetValue<float>());
			} else {
				yield return new WaitForSecondsRealtime(waitTime.GetValue<float>());
			}
		}

		public override string GenerateCode(Object obj) {
			if(!realTime) {
				return CodeGenerator.GenerateYieldReturn(CodeGenerator.GenerateNewObjectCode(typeof(WaitForSeconds), waitTime.ToCode()));
			} else {
				return CodeGenerator.GenerateYieldReturn(CodeGenerator.GenerateNewObjectCode(typeof(WaitForSecondsRealtime), waitTime.ToCode()));
			}
		}

		public override string GetDescription() {
			return "Suspends the execution for the given amount of seconds.";
		}
	}
}