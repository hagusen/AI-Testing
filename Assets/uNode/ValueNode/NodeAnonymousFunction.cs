using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Data", "AnonymousFunction", typeof(System.Delegate))]
	public class NodeAnonymousFunction : ValueNode {
		[HideInInspector, FieldConnection("Body", true)]
		public MemberData body = new MemberData();
		[HideInInspector, FieldDrawer("Return Type"), Filter(OnlyGetType = true, VoidType = true)]
		public MemberData returnType = new MemberData(typeof(void), MemberData.TargetType.Type);
		[HideInInspector]
		public List<MemberData> parameterTypes = new List<MemberData>();
		[HideInInspector]
		public List<object> parameterValues = new List<object>();

		public override void RegisterPort() {
			while(parameterValues.Count != parameterTypes.Count) {
				if(parameterValues.Count > parameterTypes.Count) {
					parameterValues.RemoveAt(parameterValues.Count - 1);
				} else {
					parameterValues.Add(null);
				}
			}
		}

		protected override object Value() {
			System.Type type = returnType.Get<System.Type>();
			if(type != null) {
				if(type == typeof(void)) {
					return CustomDelegate.CreateActionDelegate((obj) => {
						if(owner == null)
							return;
						for(int i = 0; i < parameterValues.Count; i++) {
							parameterValues[i] = obj[i];
						}
						body.InvokeFlow();
					}, parameterTypes.Select((item) => item.Get<System.Type>()).ToArray());
				} else {
					System.Type[] types = new System.Type[parameterTypes.Count + 1];
					for(int x = 0; x < parameterTypes.Count; x++) {
						types[x] = parameterTypes[x].Get<System.Type>();
					}
					types[types.Length - 1] = type;
					return CustomDelegate.CreateFuncDelegate((obj) => {
						if(owner == null)
							return null;
						for(int i = 0; i < parameterValues.Count; i++) {
							parameterValues[i] = obj[i];
						}
						Node n;
						WaitUntil w;
						if(!body.ActivateFlowNode(out n, out w)) {
							throw new System.Exception("Coroutine aren't supported by anonymous function in runtime.");
						}
						if(n == null) {
							throw new System.Exception("No return value");
						}
						JumpStatement js = n.GetJumpState();
						if(js == null || js.jumpType != JumpStatementType.Return || !(js.from is NodeReturn)) {
							throw new System.Exception("No return value");
						}
						return (js.from as NodeReturn).GetReturnValue();
					}, types);
				}
			}
			return null;
		}

		public override System.Type ReturnType() {
			System.Type rType = null;
			if(returnType.isAssigned) {
				rType = returnType.Get<System.Type>();
			}
			if(rType != null && parameterTypes.All(item => item.isAssigned && item.Get<System.Type>() != null)) {
				if(rType == typeof(void)) {
					return CustomDelegate.GetActionDelegateType(parameterTypes.Select((item) => item.Get<System.Type>()).ToArray());
				} else {
					System.Type[] types = new System.Type[parameterTypes.Count + 1];
					for(int x = 0; x < parameterTypes.Count; x++) {
						types[x] = parameterTypes[x].Get<System.Type>();
					}
					types[types.Length - 1] = rType;
					return CustomDelegate.GetFuncDelegateType(types);
				}
			}
			return typeof(object);
		}

		public override string GenerateValueCode() {
			System.Type rType = null;
			if(returnType.isAssigned) {
				rType = returnType.Get<System.Type>();
			}
			if(rType != null && parameterTypes.All(item => item.isAssigned && item.Get<System.Type>() != null)) {
				string contents = null;
				System.Type[] types = parameterTypes.Select(m => m.isAssigned ? m.startType : null).ToArray();
				if(!CodeGenerator.IsInStateGraph(this) && CodeGenerator.CanSimplifyToLambda(body, rType, types)) {
					var bodyNode = body.GetTargetNode();
					if(bodyNode as MultipurposeNode) {
						string result = bodyNode.GenerateValueCode();
						if(result.EndsWith(")")) {
							int deep = 0;
							for(int i = result.Length - 1; i > 0; i--) {
								var c = result[i];
								if(c == '(') {
									if(deep == 0) {
										result = result.Remove(i);
										break;
									} else {
										deep--;
									}
								} else if(c == ')' && i != result.Length - 1) {
									deep++;
								}
							}
						}
						return result;
					}
				}
				List<string> parameterNames = new List<string>();
				var field = this.GetType().GetField("parameterValues");
				for(int i = 0; i < types.Length; i++) {
					string varName = null;
					System.Type type = types[i];
					if(type != null) {
						if(CodeGenerator.NeedInstanceVariable(this, field, i, body)) {//Auto generate instance variable for parameter.
							varName = CodeGenerator.GenerateVariableName("tempVar", this.GetInstanceID().ToString() + i);
							contents = CodeGenerator.AddVariable(this, field, i, type, true) + " = " + varName + ";" + contents.AddLineInFirst();
						} else {
							varName = CodeGenerator.GetVariableName(this, field, i);
						}
						parameterNames.Add(varName);
					}
				}
				CodeGenerator.BeginBlock(allowYield: false);//Ensure there's no yield statements
				contents += CodeGenerator.GenerateFlowCode(body, this, false).AddLineInFirst();
				CodeGenerator.EndBlock();//Ensure to restore to previous block
				return CodeGenerator.GenerateAnonymousMethod(types, parameterNames, contents);
			}
			return null;
		}

		public override string GetNodeName() {
			return "AnonymousFunction";
		}
	}
}