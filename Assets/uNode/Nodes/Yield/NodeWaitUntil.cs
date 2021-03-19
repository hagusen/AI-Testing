using UnityEngine;
using System.Collections;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Yield", "WaitUntil", IsCoroutine = true)]
	[Description("Waits until condition evaluate to true.")]
	public class NodeWaitUntil : Node {
		[EventType(EventData.EventType.Condition)]
		public EventData Condition;
		[Hide, FieldConnection("", true)]
		public MemberData onFinished = new MemberData();

		public override void OnExecute() {
			owner.StartCoroutine(OnCall(), this);
		}

		public IEnumerator OnCall() {
			yield return new WaitUntil(() => Condition.Validate(owner));
			Finish(onFinished);
		}

		public override bool IsSelfCoroutine() {
			return true;
		}

		public override string GenerateCode() {
			return "yield return new " + CodeGenerator.ParseType(typeof(WaitUntil)) + "(() => " + Condition.GenerateCode(this, EventData.EventType.Condition) + ");"
				+ CodeGenerator.GetFinishCode(this, true, onFinished).AddLineInFirst();
		}

		public override void CheckError() {
			base.CheckError();
			if(Condition != null)
				Condition.CheckError(this);
		}
	}
}
