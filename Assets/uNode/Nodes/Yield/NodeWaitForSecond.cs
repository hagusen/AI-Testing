using UnityEngine;
using System.Collections;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Yield", "WaitForSecond", IsCoroutine = true)]
	public class NodeWaitForSecond : Node {
		[Hide, FieldConnection(), Filter(typeof(float))]
		public MemberData waitTime = new MemberData(1f);

		[Hide, FieldConnection("", true)]
		public MemberData onFinished = new MemberData();

		public override void OnExecute() {
			owner.StartCoroutine(OnCall(), this);
		}

		public IEnumerator OnCall() {
			yield return new WaitForSeconds(waitTime.GetValue<float>());
			Finish(onFinished);
		}

		public override bool IsSelfCoroutine() {
			return true;
		}

		public override string GenerateCode() {
			return "yield return new " + CodeGenerator.ParseType(typeof(WaitForSeconds)) + "(" + CodeGenerator.ParseValue((object)waitTime) + ");" + CodeGenerator.GetFinishCode(this, true, false, false, onFinished).AddLineInFirst();
		}

		public override string GetRichName() {
			return "Wait For Second:" + waitTime.GetNicelyDisplayName(richName:true);
		}

		public override void CheckError() {
			base.CheckError();
			uNodeUtility.CheckError(waitTime, this, "waitTime");
		}
	}
}
