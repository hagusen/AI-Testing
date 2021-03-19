using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Data", "Lambda", typeof(System.Delegate))]
	public class NodeLambda : ValueNode {
		[Filter(typeof(System.Delegate), OnlyGetType=true)]
		public MemberData delegateType = MemberData.CreateFromType(typeof(System.Action));

		[HideInInspector]
		public MemberData body = new MemberData();
		[HideInInspector]
		public MemberData input = new MemberData();

		[HideInInspector]
		public List<object> parameterValues = new List<object>();

		private System.Delegate m_Delegate;
		private System.Reflection.MethodInfo methodInfo;

		private void InitDelegate() {
			if(!delegateType.isAssigned) return;
			var type = delegateType.Get<System.Type>();
			methodInfo = type.GetMethod("Invoke");
			if(methodInfo.ReturnType == typeof(void)) {
				m_Delegate = CustomDelegate.CreateActionDelegate((obj) => {
					if(owner == null)
						return;
					if(obj != null) {
						while(parameterValues.Count < obj.Length) {
							parameterValues.Add(null);
						}
						for(int i = 0; i < obj.Length; i++) {
							parameterValues[i] = obj[i];
						}
					}
					body.InvokeFlow();
				}, methodInfo.GetParameters().Select(i => i.ParameterType).ToArray());
			} else {
				var types = methodInfo.GetParameters().Select(i => i.ParameterType).ToList();
				types.Add(methodInfo.ReturnType);
				m_Delegate = CustomDelegate.CreateFuncDelegate((obj) => {
					if(owner == null)
						return null;
					if(obj != null) {
						while(parameterValues.Count < obj.Length) {
							parameterValues.Add(null);
						}
						for(int i = 0; i < obj.Length; i++) {
							parameterValues[i] = obj[i];
						}
					}
					return input.Get(methodInfo.ReturnType);
				}, types.ToArray());
			}
			// m_Delegate = ReflectionUtils.ConvertDelegate(m_Delegate, e.EventHandlerType);
		}

		protected override object Value() {
			if(m_Delegate == null) {
				InitDelegate();
			}
			return m_Delegate;
		}

		public override bool CanGetValue() {
			return true;
		}

		public override System.Type ReturnType() {
			if(!delegateType.isAssigned) return typeof(object);
			var type = delegateType.Get<System.Type>();
			methodInfo = type.GetMethod("Invoke");
			if(methodInfo != null) {
				if(methodInfo.ReturnType == typeof(void)) {
					return CustomDelegate.GetActionDelegateType(methodInfo.GetParameters().Select(p => p.ParameterType).ToArray());
				} else {
					var types = methodInfo.GetParameters().Select(i => i.ParameterType).ToList();
					types.Add(methodInfo.ReturnType);
					return CustomDelegate.GetFuncDelegateType(types.ToArray());
				}
			}
			return typeof(object);
		}

		public override string GenerateValueCode() {
			if(!delegateType.isAssigned) throw new System.Exception("Delegate Type is not assigned");
			var type = delegateType.Get<System.Type>();
			var methodInfo = type.GetMethod("Invoke");
			var paramTypes = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
			string contents = null;
			List<string> parameterNames = new List<string>();
			var field = this.GetType().GetField("parameterValues");
			for(int i = 0; i < paramTypes.Length; i++) {
				string varName = null;
				System.Type pType = paramTypes[i];
				if(pType != null) {
					if(CodeGenerator.NeedInstanceVariable(this, field, i, body)) {//Auto generate instance variable for parameter.
						varName = CodeGenerator.GenerateVariableName("tempVar", this.GetInstanceID().ToString() + i);
						contents = CodeGenerator.AddVariable(this, field, i, pType, true) + " = " + varName + ";" + contents.AddLineInFirst();
					} else {
						varName = CodeGenerator.GetVariableName(this, field, i);
					}
					parameterNames.Add(varName);
				}
			}
			if(methodInfo.ReturnType == typeof(void)) {
				CodeGenerator.BeginBlock(allowYield:false); //Ensure that there is no yield statement
				contents += CodeGenerator.GenerateFlowCode(body, this, false).AddLineInFirst();
				CodeGenerator.EndBlock();
				return CodeGenerator.GenerateAnonymousMethod(paramTypes, parameterNames, contents);
			} else {
				contents += CodeGenerator.GenerateReturn(input.ParseValue());
				CodeGenerator.EndBlock();
				return CodeGenerator.GenerateAnonymousMethod(paramTypes, parameterNames, contents);
			}
		}

		public override string GetNodeName() {
			return "Lambda";
		}

		public override void CheckError() {
			uNodeUtility.CheckError(delegateType, this, nameof(delegateType), false);
		}
	}
}