using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using MaxyGames.uNode;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "NavMeshAgent.Patrol", true)]
	public class NavMeshAgentPatrol : CoroutineAction {
		[ObjectType(typeof(NavMeshAgent))]
		public MemberData agent;
		[Filter(typeof(IList<GameObject>), InvalidTargetType = MemberData.TargetType.Null)]
		public MemberData targets = new MemberData(new List<GameObject>());
		public bool randomPatrol;

		private int patrolIndex = -1;

		protected override IEnumerator ExecuteCoroutine() {
			var targetPatrols = targets.GetValue<IList<GameObject>>();
			if(targetPatrols.Count == 0) {
				yield break;
			} else if(targetPatrols.Count == 1) {
				patrolIndex = 0;
			} else {
				if(randomPatrol) {
					var oldIndex = patrolIndex;
					while(patrolIndex == oldIndex) {
						patrolIndex = Random.Range(0, targetPatrols.Count);
					}
				} else {
					patrolIndex = (int)Mathf.Repeat(patrolIndex + 1, targetPatrols.Count);
				}
			}
			var navAgent = agent.GetValue<NavMeshAgent>();
			var target = targetPatrols[patrolIndex];
			while(Vector3.Distance(navAgent.transform.position, target.transform.position) >= navAgent.stoppingDistance) {
				navAgent.SetDestination(target.transform.position);
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
			string result = null;
			uNode.VariableData[] variables = CodeGenerator.GetUserObject(this) as uNode.VariableData[];
			if(variables == null) {
				variables = new uNode.VariableData[1];
				variables[0] = new uNode.VariableData("patrolIndex", typeof(int), -1);
				variables[0].modifier.SetPrivate();
				CodeGenerator.RegisterUserObject(variables, this);
				CodeGenerator.AddVariable(variables[0]);
			}
			string patrolIndex = CodeGenerator.GetVariableName(variables[0]);
			var targetPatrols = "targetPatrols".ToVariableName(this);
			result += CodeGenerator.GenerateVariableDeclaration(targetPatrols, typeof(IList<GameObject>), targets).AddLineInFirst();
			bool flag = false;
			if(targets.targetType == MemberData.TargetType.Values) {
				var targetList = targets.GetValue<IList<GameObject>>();
				if(targetList.Count == 0) {
					return null;
				} else if(targetList.Count == 1) {
					result += patrolIndex.Set(0.ToCode()).AddLineInFirst();
					flag = true;
				}
			}
			if(!flag) {
				if(randomPatrol) {
					var oldIndex = "oldIndex".ToVariableName(this);
					result += CodeGenerator.GenerateVariableDeclaration(oldIndex, typeof(int), patrolIndex, false).AddLineInFirst();
					result += CodeGenerator.GenerateCondition("while", patrolIndex.Compare(oldIndex),
						patrolIndex.Set(typeof(Random).InvokeCode("Range",
						0.ToCode(),
						targetPatrols.Access("Count")).
						RemoveSemicolon())).AddLineInFirst();
				} else {
					result += patrolIndex.Set(typeof(Mathf).
						InvokeCode("Repeat",
						patrolIndex.AddOperation(1.ToCode()),
						targetPatrols.Access("Count")).
						RemoveSemicolon().ConvertCode(typeof(int))).AddLineInFirst();
				}
			}
			var navAgent = "navAgent".ToVariableName(this);
			var target = "target".ToVariableName(this);
			result += CodeGenerator.GenerateVariableDeclaration(navAgent, typeof(NavMeshAgent), agent).AddLineInFirst();
			result += CodeGenerator.GenerateVariableDeclaration(target, typeof(GameObject), targetPatrols.AccessElement(patrolIndex), false).AddLineInFirst();
			string condition = typeof(Vector3).InvokeCode("Distance",
				navAgent.Access("transform", "position"),
				target.Access("transform", "position")).RemoveSemicolon().
				Compare(navAgent.Access("stoppingDistance"), ComparisonType.GreaterThanOrEqual);
			string contents = navAgent.InvokeCode("SetDestination",
				target.Access("transform", "position"));
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