using System.Collections;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Flow", "YieldReturn", IsCoroutine=true)]
	public class NodeYieldReturn : Node {
		[Hide, FieldConnection("", true)]
		public MemberData onFinished = new MemberData();
		[Hide, FieldConnection(false)]
		public MemberData value = new MemberData(null, MemberData.TargetType.Null);

		public override void OnExecute() {
			if(value.isAssigned) {
				owner.StartCoroutine(DoYield(), this);
			} else {
				throw new System.Exception("Target is unassigned.");
			}
		}

		IEnumerator DoYield() {
			yield return value.Get();
			Finish(onFinished);
		}

		public override string GetRichName() {
			return uNodeUtility.WrapTextWithKeywordColor("yield return ") + value.GetNicelyDisplayName(richName:true);
		}

		public override bool IsSelfCoroutine() {
			return true;
		}

		public override string GenerateCode() {
			if(!value.isAssigned) throw new System.Exception("Unassigned value");
			return CodeGenerator.ParseValue(value).AddFirst("yield return ").Add(";") + CodeGenerator.GetFinishCode(this, true, false, false, onFinished);
		}
	}
}
