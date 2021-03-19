using System.Collections;
using UnityEngine;
namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "Yield.WaitForFixedUpdate", true)]
	public class YieldWaitForFixedUpdate : CoroutineAction {
		protected override IEnumerator ExecuteCoroutine() {
			yield return new WaitForFixedUpdate();
		}

		public override string GenerateCode(Object obj) {
			return CodeGenerator.GenerateYieldReturn(CodeGenerator.GenerateNewObjectCode(typeof(WaitForFixedUpdate)));
		}

		public override string GetDescription() {
			return "Waits until next fixed frame rate update function.";
		}
	}
}