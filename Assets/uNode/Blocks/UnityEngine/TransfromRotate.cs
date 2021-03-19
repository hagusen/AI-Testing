using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine/Transform", "Rotate")]
	public class TransfromRotate : Action {
		[ObjectType(typeof(Transform))]
		public MemberData transform = MemberData.empty;
		[ObjectType(typeof(Vector3))]
		public MemberData eulers = MemberData.empty;
		[ObjectType(typeof(float))]
		public MemberData speed = new MemberData(1f);

		protected override void OnExecute() {
			if(transform != null) {
				transform.GetValue<Transform>().Rotate(eulers.GetValue<Vector3>() * speed.GetValue<float>() * Time.deltaTime);
			}
		}

		public override string GenerateCode(Object obj) {
			if(transform.isAssigned && eulers.isAssigned) {
				return CodeGenerator.GenerateInvokeCode(transform, "Translate",
					CodeGenerator.ParseValue((object)eulers) + " * " +
					CodeGenerator.ParseValue((object)speed) + " * " +
					CodeGenerator.ParseType(typeof(Time)) + ".deltaTime");
			}
			throw new System.Exception("transform or eulers is unassigned");
		}
	}
}