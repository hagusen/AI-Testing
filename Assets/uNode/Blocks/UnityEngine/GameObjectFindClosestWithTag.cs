using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "GameObject.FindClosestWithTag")]
	public class GameObjectFindClosestWithTag : Action {
		[ObjectType(typeof(Transform))]
		public MemberData from;
		[ObjectType(typeof(string))]
		public MemberData searchTag;
		[Filter(SetMember = true)]
		[ObjectType(typeof(GameObject))]
		public MemberData storeObject;
		[Filter(SetMember = true)]
		[ObjectType(typeof(float))]
		public MemberData storeDistance;

		protected override void OnExecute() {
			var gameObjects = GameObject.FindGameObjectsWithTag(searchTag.GetValue<string>());
			GameObject closest = null;
			var dis = Mathf.Infinity;
			foreach(var go in gameObjects) {
				var newDis = Vector3.Distance(go.transform.position, from.GetValue<Transform>().position);
				if(newDis < dis) {
					dis = newDis;
					closest = go;
				}
			}
			if(storeObject.isAssigned) {
				storeObject.Set(closest);
			}
			if(storeDistance.isAssigned) {
				storeDistance.Set(dis);
			}
		}

		public override string GenerateCode(Object obj) {
			string gameObjects = CodeGenerator.GenerateVariableName("gameObjects", this);
			string closest = CodeGenerator.GenerateVariableName("closest", this);
			string dis = CodeGenerator.GenerateVariableName("dis", this);
			string result = null;
			//GameObject[] gameObjects = GameObject.FindGameObjectsWithTag(searchTag);
			result += CodeGenerator.GenerateVariableDeclaration(gameObjects, typeof(GameObject[]), 
				CodeGenerator.GetInvokeCode(typeof(GameObject), "FindGameObjectsWithTag", searchTag).RemoveSemicolon(), false);
			//GameObject closest = null;
			result += CodeGenerator.GenerateVariableDeclaration(closest, typeof(GameObject), null).AddLineInFirst();
			//float dis = Mathf.Infinity;
			result += CodeGenerator.GenerateVariableDeclaration(dis, typeof(float), CodeGenerator.ParseType(typeof(Mathf)) + ".Infinity", false).AddLineInFirst();
			string go = CodeGenerator.GenerateVariableName("go", this);
			string newDis = CodeGenerator.GenerateVariableName("newDis", this);
			{//foreach contents
				string contents = CodeGenerator.GenerateVariableDeclaration(newDis, typeof(float),
					CodeGenerator.GenerateInvokeCode(typeof(Vector3), "Distance",
					go + ".transform.position", CodeGenerator.ParseValue(from) + ".position").RemoveSemicolon(), false);
				{//If contents
					string ifContents = CodeGenerator.GenerateSetCode(dis, newDis);
					ifContents += CodeGenerator.GenerateSetCode(closest, go).AddLineInFirst();
					contents += CodeGenerator.GenerateIfStatement(CodeGenerator.GenerateCompareCode(newDis, dis, ComparisonType.LessThan), ifContents).AddLineInFirst();
				}
				result += CodeGenerator.GenerateForeachStatement(typeof(GameObject), go, gameObjects, contents).AddLineInFirst();
			}
			{//Store result
				if(storeObject.isAssigned) {
					result += CodeGenerator.GenerateSetCode(storeObject, CodeGenerator.WrapString(closest)).AddLineInFirst();
				}
				if(storeDistance.isAssigned) {
					result += CodeGenerator.GenerateSetCode(storeDistance, CodeGenerator.WrapString(dis)).AddLineInFirst();
				}
			}
			return result;
		}

		public override string GetDescription() {
			return "Find the closest game object of tag to the from.";
		}
	}
}