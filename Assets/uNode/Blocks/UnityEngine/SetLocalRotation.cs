using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine/Transform", "SetLocalRotation")]
	public class SetLocalRotation : Action {
		[ObjectType(typeof(Transform))]
		public MemberData transform;
		public bool SetX = true;
		[ObjectType(typeof(float)), Hide(nameof(SetX), false)]
		public MemberData XValue;
		public bool SetY = true;
		[ObjectType(typeof(float)), Hide(nameof(SetY), false)]
		public MemberData YValue;
		public bool SetZ = true;
		[ObjectType(typeof(float)), Hide(nameof(SetZ), false)]
		public MemberData ZValue;

		protected override void OnExecute() {
			if(transform != null) {
				Transform tr = transform.GetValue<Transform>();
				var vector = tr.localEulerAngles;
				if(SetX) {
					vector.x = XValue.GetValue<float>();
				}
				if(SetY) {
					vector.y = YValue.GetValue<float>();
				}
				if(SetZ) {
					vector.z = ZValue.GetValue<float>();
				}
				tr.localEulerAngles = vector;
			}
		}

		public override string GenerateCode(Object obj) {
			string data = null;
			if(transform.isAssigned) {
				string name = CodeGenerator.ParseValue((object)transform) + ".localEulerAngles";
				string code = "new " + CodeGenerator.ParseType(typeof(Vector3)) + "(";
				if(SetX) {
					if(SetY) {
						if(SetZ) {
							code += CodeGenerator.ParseValue((object)XValue) + ", " +
								CodeGenerator.ParseValue((object)YValue) + ", " +
								CodeGenerator.ParseValue((object)ZValue) + ")";
						} else {
							code += CodeGenerator.ParseValue((object)XValue) + ", " + CodeGenerator.ParseValue((object)YValue) + ", " + name + ".z)";
						}
					} else if(SetZ){
						code += CodeGenerator.ParseValue((object)XValue) + ", " + name + ".y, " + CodeGenerator.ParseValue((object)YValue) + ", " + name + ".z)";
					} else {
						code += CodeGenerator.ParseValue((object)XValue) + ", " + name + ".y, " + name + ".z)";
					}
				} else if(SetY) {
					code += name + ".x, " + CodeGenerator.ParseValue((object)YValue) + ", " + name + ".z)";
				} else if(SetZ) {
					code += name + ".x, " + name + ".y, " + CodeGenerator.ParseValue((object)ZValue) + ")";
				} else {
					return null;
				}
				data += CodeGenerator.GenerateSetCode(name, code);
			}
			return data;
		}
	}
}