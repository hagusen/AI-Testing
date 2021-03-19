using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using MaxyGames.uNode;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "NavMeshAgent.Flee", true)]
	public class NavMeshAgentFlee : CoroutineAction {
		[ObjectType(typeof(NavMeshAgent))]
		public MemberData agent;
		[ObjectType(typeof(GameObject))]
		public MemberData target;
		[ObjectType(typeof(float))]
		public MemberData fledDistance = new MemberData(10f);
		[ObjectType(typeof(float))]
		public MemberData lookAhead = new MemberData(2f);

		protected override IEnumerator ExecuteCoroutine() {
			var navAgent = agent.GetValue<NavMeshAgent>();
			var targetPos = target.GetValue<GameObject>().transform.position;
			while((navAgent.transform.position - targetPos).magnitude >= fledDistance.GetValue<float>()) {
				var fleePos = targetPos + (navAgent.transform.position - targetPos).normalized * (fledDistance.GetValue<float>() +  lookAhead.GetValue<float>() + navAgent.stoppingDistance);
				if(!navAgent.SetDestination(fleePos)) {
					break;
				}
				yield return null;
			}
		}

		protected override void OnStop() {
			agent.GetValue<NavMeshAgent>().ResetPath();
		}

		public override string GenerateCode(Object obj) {
			var navAgent = "navAgent".ToVariableName(this);
			var targetPos = "targetPos".ToVariableName(this);
			string result = null;
			result += CodeGenerator.GenerateVariableDeclaration(navAgent, typeof(NavMeshAgent), agent);
			result += CodeGenerator.GenerateVariableDeclaration(targetPos, typeof(Vector3),
				target.Access("transform", "position"), false).AddLineInFirst();
			{//While
				var fleePos = "fleePos".ToVariableName(this);
				string fleePosContents = targetPos.AddOperation(navAgent.Access("transform", "position").SubtractOperation(targetPos).Wrap().Access("normalized"));
				fleePosContents = fleePosContents.MultiplyOperation(
					fledDistance.ToCode().AddOperation(lookAhead.ToCode()).AddOperation(navAgent.Access("stoppingDistance")).Wrap());
				string contents = CodeGenerator.GenerateVariableDeclaration(fleePos, typeof(Vector3), fleePosContents, false);
				contents += 
					CodeGenerator.GenerateIfStatement(
						navAgent.InvokeCode("SetDestination", fleePos).RemoveSemicolon().Wrap(),
						CodeGenerator.GenerateBreak()).AddLineInFirst() + 
					CodeGenerator.GenerateYieldReturn(null).AddLineInFirst();
				result += CodeGenerator.GenerateCondition("while",
					CodeGenerator.GenerateArithmetiCode(
							navAgent.Access("transform", "position"),
							targetPos,
							ArithmeticType.Subtract).Wrap().
						Access("magnitude").
						Compare(
							fledDistance.ToCode(),
							ComparisonType.GreaterThanOrEqual),
					contents).AddLineInFirst();
			}
			return result;
		}

		public override string GenerateStopCode(Object obj) {
			return CodeGenerator.GetInvokeCode(agent, "ResetPath");
		}

		public override string GetDescription() {
			return "Flees away from the target";
		}
	}
}