using System.Collections;
using UnityEngine;

namespace MaxyGames.uNode.Transition {
	[UnityEngine.AddComponentMenu("")]
	[TransitionMenu("OnTimerElapsed", "OnTimerElapsed")]
	public class OnTimerElapsed : TransitionEvent {
		[Filter(typeof(float))]
		public MemberData delay = new MemberData(1f);
		public bool unscaled;

		public override void OnEnter() {
			owner.StartCoroutine(Wait(), this);
		}

		IEnumerator Wait() {
			if(unscaled) {
				yield return new WaitForSecondsRealtime(delay.GetValue<float>());
			} else {
				yield return new WaitForSeconds(delay.GetValue<float>());
			}
			Finish();
		}

		public override void OnExit() {
			owner.StopCoroutine(Wait());
		}

		public override string GenerateOnEnterCode() {
			if(unscaled) {
				CodeGenerator.generatorData.AddEventCoroutineData(this, CodeGenerator.GenerateYieldReturn(CodeGenerator.GenerateNewObjectCode(typeof(WaitForSecondsRealtime), CodeGenerator.ParseValue(delay))).AddLineInFirst() + CodeGenerator.GetFinishCode(this).AddLineInFirst());
			} else {
				CodeGenerator.generatorData.AddEventCoroutineData(this, CodeGenerator.GenerateYieldReturn(CodeGenerator.GenerateNewObjectCode(typeof(WaitForSeconds), CodeGenerator.ParseValue(delay))).AddLineInFirst() + CodeGenerator.GetFinishCode(this).AddLineInFirst());
			}
			return CodeGenerator.RunEvent(this);
		}

		public override string GenerateOnExitCode() {
			return CodeGenerator.StopEvent(this);
		}
	}
}
