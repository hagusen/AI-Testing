using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "GameObject.CreateEmpty")]
	public class GameObjectCreateEmpty : Action {
		[ObjectType(typeof(string))]
		public MemberData objectName = new MemberData("");
		public bool useTransform = true;
		[Hide("useTransform", false)]
		[ObjectType(typeof(Transform))]
		public MemberData transform;
		[Hide("useTransform", true)]
		[ObjectType(typeof(Vector3))]
		public MemberData position = new MemberData(Vector3.zero);
		[Hide("useTransform", true)]
		[ObjectType(typeof(Vector3))]
		public MemberData rotation = new MemberData(Vector3.zero);
		[Filter(typeof(GameObject), SetMember = true)]
		public MemberData storeResult;

		protected override void OnExecute() {
			GameObject go = null;
			if(objectName.isAssigned) {
				go = new GameObject(objectName.Get<string>());
			} else {
				go = new GameObject();
			}
			if(useTransform) {
				go.transform.position = transform.GetValue<Transform>().position;
				go.transform.eulerAngles = transform.GetValue<Transform>().eulerAngles;
			} else {
				go.transform.position = position.GetValue<Vector3>();
				go.transform.eulerAngles = rotation.GetValue<Vector3>();
			}
			if(storeResult.isAssigned) {
				storeResult.Set(go);
			}
		}

		public override string GenerateCode(Object obj) {
			if(useTransform && !transform.isAssigned || !useTransform && (!position.isAssigned || !rotation.isAssigned)) return null;
			string go = CodeGenerator.GenerateVariableName("go", this);
			string data = null;
			data += CodeGenerator.ParseType(typeof(GameObject)) + " " + go + " = new GameObject(" + 
				(objectName.isAssigned ? CodeGenerator.ParseValue((object)objectName) : "") + ");";
			data += go + ".transform.position = " +
				(useTransform ? CodeGenerator.ParseValue((object)transform) + ".position" : CodeGenerator.ParseValue((object)position)) + ";";
			data += go + ".transform.eulerAngles = " +
				(useTransform ? CodeGenerator.ParseValue((object)transform) + ".eulerAngles" : CodeGenerator.ParseValue((object)rotation)) + ";";
			if(storeResult.isAssigned) {
				data += CodeGenerator.GenerateSetCode(go, storeResult);
			}
			return data;
		}

		public override string GetDescription() {
			return "Creates a new game object.";
		}
	}
}