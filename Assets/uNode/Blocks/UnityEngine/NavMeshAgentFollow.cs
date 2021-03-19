using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using MaxyGames.uNode;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "NavMeshAgent.Follow", true)]
	public class NavMeshAgentFollow : CoroutineAction {
		[ObjectType(typeof(NavMeshAgent))]
		public MemberData agent;
		[ObjectType(typeof(GameObject))]
		public MemberData target;
		[ObjectType(typeof(float))]
		public MemberData followDistance = new MemberData(5f);
		public bool repeat = true;

		protected override IEnumerator ExecuteCoroutine() {
			var navAgent = agent.GetValue<NavMeshAgent>();
			float distance = Vector3.Distance(navAgent.transform.position, target.GetValue<GameObject>().transform.position);
			if(repeat) {
				while(true) {
					if(followDistance.GetValue<float>() <= distance) {
						navAgent.SetDestination(target.GetValue<GameObject>().transform.position);
					}
					yield return null;
				}
			} else {
				if(followDistance.GetValue<float>() <= distance) {
					navAgent.SetDestination(target.GetValue<GameObject>().transform.position);
				}
			}
		}

		protected override void OnStop() {
			agent.GetValue<NavMeshAgent>().ResetPath();
		}

		public override string GenerateCode(Object obj) {
			var navAgent = "navAgent".ToVariableName(this);
			var distance = "distance".ToVariableName(this);
			string result = null;
			result += CodeGenerator.GenerateVariableDeclaration(navAgent, typeof(NavMeshAgent), agent);
			result += CodeGenerator.GenerateVariableDeclaration(distance, typeof(float),
				typeof(Vector3).InvokeCode("SetDestination", target.ToCode().Access("transform", "position")).RemoveSemicolon(), false).AddLineInFirst();
			string contents = CodeGenerator.GenerateIfStatement(followDistance.ToCode().
				Compare(distance, ComparisonType.LessThanOrEqual),
				navAgent.InvokeCode("SetDestination", target.ToCode().
				Access("transform", "position")));
			if(repeat) {
				result += CodeGenerator.GenerateCondition("while", "true", contents + CodeGenerator.GetYieldReturn(null).AddLineInFirst()).AddLineInFirst();
			} else {
				result += contents.AddLineInFirst();
			}
			return result;
		}

		public override string GenerateStopCode(Object obj) {
			return CodeGenerator.GetInvokeCode(agent, "ResetPath");
		}

		public override string GetDescription() {
			return "Follow the target";
		}
	}
}