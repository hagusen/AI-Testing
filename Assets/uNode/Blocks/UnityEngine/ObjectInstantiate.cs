using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "Object.Instantiate", hideOnBlock =true)]
	public class ObjectInstantiate : Action {
		[ObjectType(typeof(Object))]
		public MemberData targetObject;
		public bool useTransform = true;
		[Hide("useTransform", false)]
		[ObjectType(typeof(Transform))]
		public MemberData transform;
		[Hide("useTransform", true)]
		[ObjectType(typeof(Vector3))]
		public MemberData position;
		[Hide("useTransform", true)]
		[ObjectType(typeof(Vector3))]
		public MemberData rotation;
		[ObjectType(typeof(Transform))]
		public MemberData parent;
		[Filter(SetMember = true)]
		[ObjectType("targetObject")]
		public MemberData storeResult;

		protected override void OnExecute() {
			if(targetObject.isAssigned) {
				Vector3 pos;
				Vector3 rot;
				if(useTransform) {
					pos = transform.GetValue<Transform>().position;
					rot = transform.GetValue<Transform>().eulerAngles;
				} else {
					pos = position.GetValue<Vector3>();
					rot = rotation.GetValue<Vector3>();
				}
				Object obj = Object.Instantiate(targetObject.GetValue<Object>(), pos, Quaternion.Euler(rot), parent.GetValue<Transform>());
				if(storeResult.isAssigned) {
					storeResult.Set(obj);
				}
			}
		}

		public override string GenerateCode(Object obj) {
			if(targetObject.isAssigned) {
				string data = CodeGenerator.ParseValue((object)storeResult);
				string pos = null;
				string rot = null;
				if(useTransform) {
					pos = CodeGenerator.ParseValue((object)transform) + ".position";
					rot = CodeGenerator.ParseValue((object)transform) + ".eulerAngles";
				} else {
					pos = CodeGenerator.ParseValue((object)position);
					rot = CodeGenerator.ParseValue((object)rotation);
				}
				rot = CodeGenerator.GenerateInvokeCode(typeof(Quaternion), "Euler", rot).Replace(";", "");
				if(string.IsNullOrEmpty(data)) {
					return CodeGenerator.GenerateInvokeCode(typeof(Object), "Instantiate", CodeGenerator.ParseValue((object)targetObject), pos, rot, CodeGenerator.ParseValue((object)parent));
				} else {
					return data + " = " + CodeGenerator.GenerateInvokeCode(typeof(Object), "Instantiate", CodeGenerator.ParseValue((object)targetObject), pos, rot, CodeGenerator.ParseValue((object)parent)).Replace(";", "") + " as " + CodeGenerator.ParseType(storeResult.type) + ";";
				}
			}
			return null;
		}
	}
}