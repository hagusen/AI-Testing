using System.Collections;
using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "Animator.PlayAnimation", isCoroutine = true)]
	public class AnimatorPlayAnimation : CoroutineAction {
		[ObjectType(typeof(Animator))]
		public MemberData animator;
		[ObjectType(typeof(int))]
		public MemberData layerIndex;
		[ObjectType(typeof(string))]
		public MemberData stateName;
		[Tooltip("If true, play and wait animation until finish.")]
		public bool waitUntilFinish = true;

		protected override void OnExecute() {
			if(waitUntilFinish) {
				base.OnExecute();//Throw an exception.
			}
			var anim = animator.GetValue<Animator>();
			var stateInfo = anim.GetCurrentAnimatorStateInfo(layerIndex.GetValue<int>());
			anim.CrossFade(stateName.GetValue<string>(), stateInfo.length, layerIndex.GetValue<int>());
		}

		protected override IEnumerator ExecuteCoroutine() {
			var anim = animator.GetValue<Animator>();
			var stateInfo = anim.GetCurrentAnimatorStateInfo(layerIndex.GetValue<int>());
			anim.CrossFade(stateName.GetValue<string>(), stateInfo.length, layerIndex.GetValue<int>());
			while(waitUntilFinish) {
				if(stateInfo.IsName(stateName.GetValue<string>())) {
					yield return new WaitForSeconds(stateInfo.length / anim.playbackTime);
					break;
				}
				yield return null;
			}
		}

		public override bool IsCoroutine() {
			return waitUntilFinish;
		}

		public override string GenerateCode(Object obj) {
			string result = null;
			uNode.VariableData[] variables = CodeGenerator.GetUserObject(this) as uNode.VariableData[];
			if(variables == null) {
				variables = new uNode.VariableData[2];
				variables[0] = new uNode.VariableData("anim", typeof(Animator));
				variables[1] = new uNode.VariableData("stateInfo", typeof(AnimatorStateInfo));
				CodeGenerator.RegisterUserObject(variables, this);
			}
			string anim = CodeGenerator.GetVariableName(variables[0]);
			string stateInfo = CodeGenerator.GetVariableName(variables[1]);
			result += CodeGenerator.GenerateVariableDeclaration(variables[0], animator);
			result += CodeGenerator.GenerateVariableDeclaration(variables[1],
				CodeGenerator.GetInvokeCode(variables[0], "GetCurrentAnimatorStateInfo", layerIndex).RemoveSemicolon(), false).AddLineInFirst();
			result += CodeGenerator.GenerateInvokeCode(anim, "CrossFade", CodeGenerator.ParseValue(stateName),
				stateInfo + ".length",
				CodeGenerator.ParseValue(layerIndex)).AddLineInFirst();
			if(waitUntilFinish) {
				string contents = CodeGenerator.GenerateYieldReturn(CodeGenerator.GenerateArithmetiCode(stateInfo + ".length", anim + ".playbackTime", ArithmeticType.Divide));
				contents += CodeGenerator.GenerateBreak().AddLineInFirst();
				string code = CodeGenerator.GenerateIfStatement(CodeGenerator.GenerateInvokeCode(stateInfo, "IsName", CodeGenerator.ParseValue(stateName)).RemoveSemicolon(), contents);
				code += CodeGenerator.GetYieldReturn(null).AddLineInFirst();
				result += CodeGenerator.GenerateCondition("while", "true", code).AddLineInFirst();
			}
			return result;
		}

		public override void CheckError(Object owner) {
			base.CheckError(owner);
			uNode.uNodeUtility.CheckError(animator, owner, Name + " - animator");
			uNode.uNodeUtility.CheckError(layerIndex, owner, Name + " - layerIndex");
			uNode.uNodeUtility.CheckError(stateName, owner, Name + " - stateName");
		}
	}
}