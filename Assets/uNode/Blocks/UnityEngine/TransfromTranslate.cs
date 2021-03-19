using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine/Transform", "Translate")]
	public class TransfromTranslate : Action {
		[ObjectType(typeof(Transform))]
		public MemberData transform = MemberData.empty;
		[ObjectType(typeof(Vector3))]
		public MemberData translation = new MemberData(Vector3.forward);
		[ObjectType(typeof(float))]
		public MemberData speed = new MemberData(1f);

		protected override void OnExecute() {
			if(transform != null) {
				transform.GetValue<Transform>().Translate(translation.GetValue<Vector3>() * speed.GetValue<float>() * Time.deltaTime);
			}
		}

		public override string GenerateCode(Object obj) {
			if(transform.isAssigned && translation.isAssigned) {
				return CodeGenerator.GenerateInvokeCode(transform, "Translate",
					CodeGenerator.ParseValue((object)translation) + " * " +
					CodeGenerator.ParseValue((object)speed) + " * " +
					CodeGenerator.ParseType(typeof(Time)) + ".deltaTime");
			}
			throw new System.Exception("transform or translation is unassigned");
		}
	}
}