using System.Collections;
using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "Animation.PlayAnimation", isCoroutine = true)]
	public class AnimationPlay : CoroutineAction {
		[ObjectType(typeof(Animation))]
		public MemberData animation;
		[ObjectType(typeof(AnimationClip))]
		public MemberData clip;
		[Tooltip("Wrapping mode of the animation")]
		public WrapMode wrapMode = WrapMode.Loop;
		[ObjectType(typeof(float))]
		public MemberData crossFadeTime = new MemberData(0.25f);
		[Tooltip("If true, play and wait animation till finish.")]
		public bool waitUntilFinish = true;

		protected override void OnExecute() {
			if(waitUntilFinish) {
				base.OnExecute();//Throw an exception.
			}
			AnimationClip animClip = clip.GetValue<AnimationClip>();
			var anim = animation.GetValue<Animation>();
			anim[animClip.name].wrapMode = wrapMode;
			anim.CrossFade(animClip.name, crossFadeTime.GetValue<float>());
		}

		protected override IEnumerator ExecuteCoroutine() {
			AnimationClip animClip = clip.GetValue<AnimationClip>();
			var anim = animation.GetValue<Animation>();
			anim[animClip.name].wrapMode = wrapMode;
			anim.CrossFade(animClip.name, crossFadeTime.GetValue<float>());
			if(waitUntilFinish) {
				yield return new WaitForSeconds(animClip.length);
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
				variables[0] = new uNode.VariableData("animClip", typeof(AnimationClip));
				variables[1] = new uNode.VariableData("anim", typeof(Animation));
				CodeGenerator.RegisterUserObject(variables, this);
			}
			string animClip = CodeGenerator.GetVariableName(variables[0]);
			string anim = CodeGenerator.GetVariableName(variables[1]);
			result += CodeGenerator.GenerateVariableDeclaration(variables[0], clip);
			result += CodeGenerator.GenerateVariableDeclaration(variables[1], animation).AddLineInFirst();
			result += CodeGenerator.GenerateSetCode(anim + "[" + animClip + ".name].wrapMode", 
				CodeGenerator.ParseValue(wrapMode)).AddLineInFirst();
			result += CodeGenerator.GenerateInvokeCode(anim, "CrossFade", animClip + ".name", 
				CodeGenerator.ParseValue(crossFadeTime)).AddLineInFirst();
			if(waitUntilFinish) {
				result += CodeGenerator.GenerateYieldReturn(CodeGenerator.GenerateNewObjectCode(typeof(WaitForSeconds), animClip + ".length")).AddLineInFirst();
			}
			return result;
		}

		public override void CheckError(Object owner) {
			base.CheckError(owner);
			uNode.uNodeUtility.CheckError(animation, owner, Name + " - animation");
			uNode.uNodeUtility.CheckError(clip, owner, Name + " - clip");
			uNode.uNodeUtility.CheckError(crossFadeTime, owner, Name + " - crossFadeTime");
		}
	}
}