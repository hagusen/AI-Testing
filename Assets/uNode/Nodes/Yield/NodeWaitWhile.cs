using UnityEngine;
using System.Collections;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Yield", "WaitWhile", IsCoroutine = true)]
	[Description("Waits until condition evaluate to false.")]
	public class NodeWaitWhile : Node {
		[EventType(EventData.EventType.Condition)]
		public EventData Condition;
		[Hide, FieldConnection("", true)]
		public MemberData onFinished = new MemberData();

		public override void OnExecute() {
			owner.StartCoroutine(OnCall(), this);
		}

		public IEnumerator OnCall() {
			yield return new WaitWhile(() => Condition.Validate(owner));
			Finish(onFinished);
		}

		public override bool IsSelfCoroutine() {
			return true;
		}

		public override string GenerateCode() {
			return "yield return new " + CodeGenerator.ParseType(typeof(WaitWhile)) + "(() => " + Condition.GenerateCode(this, EventData.EventType.Condition) + ");"
				+ CodeGenerator.GetFinishCode(this, true, onFinished).AddLineInFirst();
		}

		public override void CheckError() {
			base.CheckError();
			if(Condition != null)
				Condition.CheckError(this);
		}
	}
}
