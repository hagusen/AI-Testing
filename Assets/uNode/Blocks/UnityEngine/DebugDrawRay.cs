using UnityEngine;

namespace MaxyGames.Events {
	//[EventMenu("UnityEngine/Debug/DrawRay", "DebugDrawRay")]
	public class DebugDrawRay : Action {
		[Tooltip("Point in world space where the ray should start.")]
		[ObjectType(typeof(Vector3))]
		public MemberData start;
		[Tooltip("Direction and length of the ray.")]
		[ObjectType(typeof(Vector3))]
		public MemberData dir;
		[Tooltip("Color of the drawn line.")]
		[ObjectType(typeof(Color))]
		public MemberData color;
		[Tooltip("How long the line will be visible for (in seconds).")]
		[ObjectType(typeof(float))]
		public MemberData duration;

		protected override void OnExecute() {
			Debug.DrawRay(start.GetValue<Vector3>(), dir.GetValue<Vector3>(), color.GetValue<Color>(), duration.GetValue<float>());
		}

		public override string GenerateCode(Object obj) {
			return CodeGenerator.GetInvokeCode(typeof(Debug), "DrawRay", obj, dir, color, duration).ToString();
		}

		public override string GetDescription() {
			return "Draws a line from start to start + dir in world coordinates.";
		}

		public override void CheckError(Object owner) {
			base.CheckError(owner);
			uNode.uNodeUtility.CheckError(start, owner, Name + " - start");
			uNode.uNodeUtility.CheckError(dir, owner, Name + " - dir");
			uNode.uNodeUtility.CheckError(color, owner, Name + " - color");
			uNode.uNodeUtility.CheckError(duration, owner, Name + " - duration");
		}
	}
}