using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "NavMeshAgent.Wander", true)]
	public class NavMeshAgentWander : CoroutineAction {
		[ObjectType(typeof(NavMeshAgent))]
		public MemberData agent;
		[ObjectType(typeof(float))]
		public MemberData minWanderDistance = new MemberData(5f);
		[ObjectType(typeof(float))]
		public MemberData maxWanderDistance = new MemberData(10f);
		[ObjectType(typeof(int))]
		public MemberData layer = new MemberData(-1);
		public bool repeat = true;

		protected override IEnumerator ExecuteCoroutine() {
			var navAgent = agent.GetValue<NavMeshAgent>();
			if(repeat) {
				while(true) {
					if(!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance) {
						var randomPosition = Runtime.RuntimeUtility.RandomNavSphere(navAgent.transform.position, 
							minWanderDistance.GetValue<float>(), 
							maxWanderDistance.GetValue<float>(), 
							layer.GetValue<int>());
						navAgent.SetDestination(randomPosition);
					}
					yield return null;
				}
			} else {
				var randomPosition = Runtime.RuntimeUtility.RandomNavSphere(navAgent.transform.position, 
					minWanderDistance.GetValue<float>(), 
					maxWanderDistance.GetValue<float>(), 
					layer.GetValue<int>());
				navAgent.SetDestination(randomPosition);
			}
		}

		protected override void OnStop() {
			agent.GetValue<NavMeshAgent>().ResetPath();
		}

		public override string GenerateCode(Object obj) {
			var navAgent = CodeGenerator.GenerateVariableName("navAgent", this);
			var randomPosition = CodeGenerator.GenerateVariableName("randomPosition", this);
			string result = null;
			result += CodeGenerator.GenerateVariableDeclaration(navAgent, typeof(NavMeshAgent), agent);
			string content = CodeGenerator.GenerateVariableDeclaration(randomPosition, typeof(Vector3), 
				CodeGenerator.GetInvokeCode(typeof(Runtime.RuntimeUtility), "RandomNavSphere", 
				CodeGenerator.WrapString(navAgent + ".transform.position"), 
				minWanderDistance, 
				maxWanderDistance, 
				layer).RemoveSemicolon(), false).AddLineInFirst();
			content += CodeGenerator.GenerateInvokeCode(navAgent, "SetDestination", randomPosition).AddLineInFirst();
			if(repeat) {
				string contents = CodeGenerator.GenerateIfStatement(
					CodeGenerator.GenerateCompareCode(
					CodeGenerator.GenerateAndCode("!" + navAgent + ".pathPending",  navAgent + ".remainingDistance"), 
					navAgent + ".stoppingDistance", 
					ComparisonType.LessThanOrEqual), content);
				result += CodeGenerator.GenerateCondition("while", "true", contents).AddLineInFirst();
			} else {
				result += content.AddLineInFirst();
			}
			return result;
		}

		public override string GenerateStopCode(Object obj) {
			return CodeGenerator.GetInvokeCode(agent, "ResetPath");
		}

		public override string GetDescription() {
			return "Makes the agent wander randomly within the navigation map";
		}
	}
}