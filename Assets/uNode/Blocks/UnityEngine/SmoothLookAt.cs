using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine/Transform", "SmoothLookAt")]
	public class SmoothLookAt : Action {
		[ObjectType(typeof(Transform))]
		public MemberData transform = MemberData.empty;
		[ObjectType(typeof(Transform))]
		public MemberData target = MemberData.empty;
		[ObjectType(typeof(float))]
		public MemberData damping = new MemberData(6f);

		protected override void OnExecute() {
			if(transform != null) {
				Transform tr = transform.GetValue<Transform>();
				tr.rotation = Quaternion.Slerp(tr.rotation, 
					Quaternion.LookRotation(target.GetValue<Transform>().position - tr.position), 
					Time.deltaTime * damping.GetValue<float>());
			}
		}

		public override string GenerateCode(Object obj) {
			if(transform.isAssigned && target.isAssigned) {
				uNode.VariableData variable = CodeGenerator.GetOrRegisterUserObject(new uNode.VariableData("tr", transform.type), this);
				string lookRotation = CodeGenerator.GenerateInvokeCode(typeof(Quaternion), "LookRotation",
					CodeGenerator.ParseValue((object)target) + ".position - " +
					CodeGenerator.GetVariableName(variable) + ".position");
				string left = CodeGenerator.GetVariableName(variable) + ".rotation";
				string right = CodeGenerator.GenerateInvokeCode(typeof(Quaternion), "Slerp", left, lookRotation, 
					CodeGenerator.ParseType(typeof(Time))+ ".deltaTime * " + CodeGenerator.ParseValue(damping)).RemoveSemicolon();
				return CodeGenerator.GenerateVariableDeclaration(variable, transform).AddLineInEnd() +  CodeGenerator.GenerateSetCode(left, right);
			}
			throw new System.Exception("transform or target is unassigned");
		}
	}
}