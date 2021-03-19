using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "Vector3.SetVector3XYZ")]
	public class SetVector3XYZ : Action {
		[Filter(typeof(Vector3))]
		public MemberData vector3Variable;
		[ObjectType(typeof(float))]
		public MemberData setX;
		[ObjectType(typeof(float))]
		public MemberData setY;
		[ObjectType(typeof(float))]
		public MemberData setZ;

		protected override void OnExecute() {
			if(vector3Variable.isAssigned) {
				Vector3 result = (Vector3)vector3Variable.Get();
				if(setX.isAssigned) {
					result.x = setX.GetValue<float>();
				}
				if(setY.isAssigned) {
					result.y = setY.GetValue<float>();
				}
				if(setZ.isAssigned) {
					result.z = setZ.GetValue<float>();
				}
				vector3Variable.Set(result);
			}
		}

		public override string GenerateCode(Object obj) {
			string data = null;
			if(vector3Variable.isAssigned) {
				string name = CodeGenerator.ParseValue((object)vector3Variable);
				if(setX.isAssigned) {
					string code = "new " + CodeGenerator.ParseType(typeof(Vector3)) + "(" + CodeGenerator.ParseValue((object)setX) + ", " + name + ".y, " + name + ".z)";
					data += CodeGenerator.GenerateSetCode(name, code);
				}
				if(setY.isAssigned) {
					string code = "new " + CodeGenerator.ParseType(typeof(Vector3)) + "(" + name + ".x, " + CodeGenerator.ParseValue((object)setY) + ", " + name + ".z)";
					data += CodeGenerator.GenerateSetCode(name, code);
				}
				if(setZ.isAssigned) {
					string code = "new " + CodeGenerator.ParseType(typeof(Vector3)) + "(" + name + ".x, " + name + ".y, " + CodeGenerator.ParseValue((object)setZ) + ")";
					data += CodeGenerator.GenerateSetCode(name, code);
				}
			}
			return data;
		}
	}
}