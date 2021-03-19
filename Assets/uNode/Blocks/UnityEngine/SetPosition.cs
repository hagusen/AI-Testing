using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine/Transform", "SetPosition")]
	public class SetPosition : Action {
		[ObjectType(typeof(Transform))]
		public MemberData transform = MemberData.empty;
		public bool SetX = true;
		[ObjectType(typeof(float))]
		public MemberData XValue = new MemberData(0f);
		public bool SetY = true;
		[ObjectType(typeof(float))]
		public MemberData YValue = new MemberData(0f);
		public bool SetZ = true;
		[ObjectType(typeof(float))]
		public MemberData ZValue = new MemberData(0f);

		protected override void OnExecute() {
			if(transform != null) {
				Transform tr = transform.GetValue<Transform>();
				var vector = tr.position;
				if(SetX) {
					vector.x = XValue.GetValue<float>();
				}
				if(SetY) {
					vector.y = YValue.GetValue<float>();
				}
				if(SetZ) {
					vector.z = ZValue.GetValue<float>();
				}
			}
		}

		public override string GenerateCode(Object obj) {
			string data = null;
			if(transform.isAssigned) {
				string name = CodeGenerator.ParseValue((object)transform) + ".position";
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
					} else if(SetZ) {
						code += CodeGenerator.ParseValue((object)XValue) + ", " + name + ".y, " + CodeGenerator.ParseValue((object)ZValue) + ")";
					} else {
						code += CodeGenerator.ParseValue((object)XValue) + ", " + name + ".y, " + name + ".z)";
					}
				} else if(SetY) {
					code += name + ".x, " + CodeGenerator.ParseValue((object)YValue) + ", " + name + ".z)";
				} else if(SetZ) {
					code += name + ".x, " + name + ".y, " + CodeGenerator.ParseValue((object)ZValue) + ")";
				}
				data += CodeGenerator.GenerateSetCode(name, code);
			}
			return data;
		}
	}
}