using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using MaxyGames.uNode;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "NavMeshAgent.Seek", true)]
	public class NavMeshAgentSeek : CoroutineAction {
		[ObjectType(typeof(NavMeshAgent))]
		public MemberData agent;
		[ObjectType(typeof(GameObject))]
		public MemberData target;

		protected override IEnumerator ExecuteCoroutine() {
			var navAgent = agent.GetValue<NavMeshAgent>();
			while(Vector3.Distance(navAgent.transform.position, target.GetValue<GameObject>().transform.position) >= navAgent.stoppingDistance) {
				navAgent.SetDestination(target.GetValue<GameObject>().transform.position);
				if(!navAgent.pathPending && navAgent.remainingDistance <= navAgent.stoppingDistance) {
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
			string result = null;
			result += CodeGenerator.GenerateVariableDeclaration(navAgent, typeof(NavMeshAgent), agent);
			string condition = typeof(Vector3).InvokeCode("Distance", 
				navAgent.Access("transform", "position"), 
				target.ToCode().Access("transform", "position")).RemoveSemicolon().
				Compare(navAgent.Access("stoppingDistance"), ComparisonType.GreaterThanOrEqual);
			string contents = navAgent.InvokeCode("SetDestination", 
				target.ToCode().Access("transform", "position"));
			contents += CodeGenerator.GenerateIfStatement(
				navAgent.Access("pathPending").NotOperation().
				AndOperation(navAgent.Access("remainingDistance")).
				Compare(navAgent.Access("stoppingDistance"), ComparisonType.LessThanOrEqual), 
				CodeGenerator.GenerateBreak()).AddLineInFirst();
			contents += CodeGenerator.GetYieldReturn(null).AddLineInFirst();
			result += CodeGenerator.GenerateCondition("while", condition, contents).AddLineInFirst();
			return result;
		}

		public override string GenerateStopCode(Object obj) {
			return CodeGenerator.GetInvokeCode(agent, "ResetPath");
		}

		public override string GetDescription() {
			return "Move the agent to target until reached.";
		}
	}
}