using UnityEngine;
using System.Collections.Generic;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Flow", "Try-Catch-Finally", HideOnStateMachine = true)]
	public class NodeTry : Node {
		[Hide]
		public MemberData onFinished = new MemberData();
		[HideInInspector, FieldConnection("Try", true)]
		public MemberData Try = new MemberData();
		[HideInInspector]
		public List<MemberData> Flows = new List<MemberData>();
		[HideInInspector]
		public List<MemberData> ExceptionTypes = new List<MemberData>();
		[HideInInspector]
		public MemberData Finally = new MemberData();

		[HideInInspector]
		public List<System.Exception> Exceptions = new List<System.Exception>();

		public override void RegisterPort() {
			while(Exceptions.Count != ExceptionTypes.Count) {
				if(Exceptions.Count > ExceptionTypes.Count) {
					Exceptions.RemoveAt(Exceptions.Count - 1);
				} else {
					Exceptions.Add(null);
				}
			}
		}

		public override void OnExecute() {
			if(Finally.isAssigned) {
				if(Flows.Count > 0) {
					try {
						ExecuteFlow(Try);
					}
					catch(System.Exception ex) {
						if(jumpState != null)
							return;
						int index = 0;
						foreach(var member in ExceptionTypes) {
							if(member.isAssigned) {
								if(member.startType.IsAssignableFrom(ex.GetType())) {
									Exceptions[index] = ex;
									ExecuteFlow(Flows[index]);
									break;
								}
							} else {
								ExecuteFlow(Flows[index]);
								break;
							}
							index++;
						}
					}
					finally {
						if(jumpState == null) {
							ExecuteFlow(Finally);
						}
					}
				} else {
					try {
						ExecuteFlow(Try);
					}
					finally {
						if(jumpState == null) {
							ExecuteFlow(Finally);
						}
					}
				}
			} else if(Flows.Count > 0) {
				try {
					ExecuteFlow(Try);
				}
				catch(System.Exception ex) {
					if(jumpState != null)
						return;
					int index = 0;
					foreach(var member in ExceptionTypes) {
						if(member.isAssigned) {
							if(member.startType.IsAssignableFrom(ex.GetType())) {
								Exceptions[index] = ex;
								ExecuteFlow(Flows[index]);
								break;
							}
						} else {
							ExecuteFlow(Flows[index]);
							break;
						}
						index++;
					}
				}
			}
			if(jumpState == null) {
				Finish(onFinished);
			}
		}

		public override string GenerateCode() {
			string T = null;
			string F = null;
			if(Try.isAssigned) {
				T = CodeGenerator.GenerateFlowCode(Try, this);
			}
			if(Finally.isAssigned) {
				F = CodeGenerator.GenerateFlowCode(Finally, this);
			}
			string data = "try " + CodeGenerator.GenerateBlock(T);
			for(int i = 0; i < ExceptionTypes.Count; i++) {
				var member = ExceptionTypes[i];
				var field = this.GetType().GetField("Exceptions");
				string varName = null;
				System.Type type = null;
				if(member.isAssigned) {
					type = member.startType;
				}
				if(type != null) {
					string contents = CodeGenerator.GenerateFlowCode(Flows[i], this);
					if(CodeGenerator.NeedInstanceVariable(this, field, i, Flows[i])) {
						varName = CodeGenerator.GenerateVariableName("tempVar", this);
						contents = CodeGenerator.AddVariable(this, field, i, type, true) + " = " + varName + ";" + contents.AddLineInFirst();
					} else {
						varName = CodeGenerator.GetVariableName(this, field, i);
					}
					string declaration = CodeGenerator.ParseType(type) + " " + varName;
					data += "\n" + CodeGenerator.GenerateCondition("catch", declaration, contents);
				} else {
					data += "\ncatch " + CodeGenerator.GenerateBlock(CodeGenerator.GenerateFlowCode(Flows[i], this));
					break;
				}
			}
			data += "\nfinally " + CodeGenerator.GenerateBlock(F);
			return data;
		}

		public override string GetNodeName() {
			return "Try-Catch-Finally";
		}

		public override bool IsCoroutine() {
			return HasCoroutineInFlow(Try, Finally, onFinished);
		}
	}
}