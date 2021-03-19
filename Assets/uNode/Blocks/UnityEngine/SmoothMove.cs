using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine/Transform", "SmoothMove")]
	public class SmoothMove : Action {
		[ObjectType(typeof(Transform))]
		public MemberData transform = MemberData.empty;
		[ObjectType(typeof(Vector3))]
		public MemberData destination = MemberData.empty;
		[ObjectType(typeof(float))]
		public MemberData speed = new MemberData(0.1f);

		protected override void OnExecute() {
			if(transform != null) {
				Transform tr = transform.GetValue<Transform>();
				tr.position = Vector3.Lerp(tr.position, destination.GetValue<Vector3>(), speed.GetValue<float>() * Time.deltaTime);
			}
		}

		public override string GenerateCode(Object obj) {
			if(transform.isAssigned && destination.isAssigned) {
				uNode.VariableData variable = CodeGenerator.GetOrRegisterUserObject(new uNode.VariableData("tr", transform.type), this);
				string left = CodeGenerator.GetVariableName(variable) + ".position";
				string right = CodeGenerator.GenerateInvokeCode(typeof(Vector3), "Lerp", left, CodeGenerator.ParseValue(destination), 
					CodeGenerator.ParseType(typeof(Time))+ ".deltaTime * " + CodeGenerator.ParseValue(speed)).RemoveSemicolon();
				return CodeGenerator.GenerateVariableDeclaration(variable, transform).AddLineInEnd() +  CodeGenerator.GenerateSetCode(left, right);
			}
			throw new System.Exception("transform or destination is unassigned");
		}
	}
}