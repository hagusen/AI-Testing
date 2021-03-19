using UnityEngine;
using System.Collections;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Other", "AnimateFloat", IsCoroutine = true)]
	public class NodeAnimateFloat : Node {
		[Hide, FieldConnection("", true)]
		public MemberData onFinished = new MemberData();
		[Hide, FieldConnection("curve", false, true), Filter(typeof(AnimationCurve), InvalidTargetType = MemberData.TargetType.Null)]
		public MemberData curve = new MemberData(new AnimationCurve());
		[Hide, FieldConnection("time", false, true), Filter(typeof(float), SetMember = true)]
		public MemberData time = new MemberData();
		[Hide, FieldConnection("speed", false, true), Filter(typeof(float))]
		public MemberData speed = new MemberData(1f);

		float currentTime;
		float endTime;

		public override void OnExecute() {
			currentTime = 0;
			AnimationCurve c = curve.Get<AnimationCurve>();
			endTime = c.keys[c.length - 1].time;
			owner.StartCoroutine(OnCall(), this);
		}

		public IEnumerator OnCall() {
			while(currentTime < endTime) {
				currentTime += Time.deltaTime * speed.GetValue<float>();
				time.Set(curve.Get<AnimationCurve>().Evaluate(currentTime));
				yield return null;
			}
			Finish(onFinished);
		}

		public override string GenerateCode() {
			VariableData[] variables = CodeGenerator.GetUserObject(this) as VariableData[];
			if(variables == null) {
				variables = CodeGenerator.RegisterUserObject(new VariableData[] {
					new VariableData("_currentTime", typeof(float), 0),
					new VariableData("_endTime", typeof(float), 0),
					new VariableData("curve", typeof(AnimationCurve), 0),
				}, this);
			}
			string curTime = CodeGenerator.AddVariable(variables[0]);
			string enTime = CodeGenerator.AddVariable(variables[1]);
			string data = CodeGenerator.GenerateSetCode(curTime, 0);
			data += CodeGenerator.GenerateSetCode(enTime, (object)(CodeGenerator.ParseValue(curve) + ".keys[" + CodeGenerator.ParseValue(curve) + ".length - 1].time")).AddLineInFirst();
			string contents = CodeGenerator.GenerateSetCode(curTime, (object)(CodeGenerator.ParseType(typeof(Time)) + ".deltaTime * " + CodeGenerator.ParseValue((object)speed)), SetType.Add);
			contents += CodeGenerator.GenerateSetCode(time, CodeGenerator.GetInvokeCode(curve, "Evaluate", variables[0]).RemoveSemicolon()).AddLineInFirst();
			contents += CodeGenerator.GetYieldReturn(null).AddLineInFirst();
			data += CodeGenerator.GenerateCondition("while", CodeGenerator.GenerateCompareCode(curTime, enTime, ComparisonType.LessThan), contents).AddLineInFirst();
			data += CodeGenerator.GetFinishCode(this, true, onFinished).AddLineInFirst();
			return data;
		}

		public override bool IsSelfCoroutine() {
			return true;
		}
	}
}