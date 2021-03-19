using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.uNode.Nodes {
	public class HLNode : ValueNode, IRefreshable {
		[Hide]
		public MemberData type = MemberData.none;
		[Hide]
		public MemberData onFinished = new MemberData();
		[Hide]
		public MemberData onSuccess = new MemberData();
		[Hide]
		public MemberData onFailure = new MemberData();
	
		[HideInInspector]
		public List<FieldValueData> initializers = new List<FieldValueData>();
		
		public object instance;

		private string generatedInstanceName;

		public override void RegisterPort() {
			var instanceType = type.startType;
			if(instanceType != null) {
				instance = ReflectionUtils.CreateInstance(instanceType);
				if(initializers.Count > 0) {
					foreach(var init in initializers) {
						if(init.value.CanSafeGetValue()) {
							var field = instanceType.GetField(init.name);
							if(field != null) {
								field.SetValueOptimized(instance, init.value.Get());
							}
						}
					}
				}
				if(CodeGenerator.isGenerating) {
					var variable = CodeGenerator.GetOrRegisterUserObject<VariableData>(new VariableData(gameObject.name, instance.GetType(), instance) {
						modifier = FieldModifier.PrivateModifier,
					}, this);
					generatedInstanceName = CodeGenerator.AddVariable(variable);
					if (IsFlowNode()) {
						CodeGenerator.RegisterFlowNode(this);
					}
				}
			}
		}

		#region Reflection
		IEnumerator ExecuteCoroutine() {
			IEnumerator iterator;
			if(instance is ICoroutineNode) {
				iterator = (instance as ICoroutineNode).Execute(owner).GetEnumerator();
			} else if(instance is IStateCoroutineNode) {
				iterator = (instance as IStateCoroutineNode).Execute(owner).GetEnumerator();
			} else {
				throw new InvalidOperationException();
			}
			StateType resultState = StateType.Running;
			object result = null;
			while(iterator.MoveNext()) {
				result = iterator.Current;
				if(result is string) {
					string r = result as string;
					if(r == "Success" || r == "Failure") {
						resultState = r == "Success" ? StateType.Success : StateType.Failure;
						break;
					}
				} else if(result is bool) {
					bool r = (bool)result;
					resultState = r ? StateType.Success : StateType.Failure;
					break;
				}
				yield return result;
			}
			if(resultState == StateType.Running && instance is IStateCoroutineNode) {
				resultState = StateType.Success;
			}
			if(resultState != StateType.Running) {
				state = resultState;
				switch(resultState) {
					case StateType.Success:
						Finish(onSuccess, onFinished);
						break;
					case StateType.Failure:
						Finish(onFailure, onFinished);
						break;
					default:
						throw new InvalidOperationException();
				}
			} else Finish(onFinished);
		}

		private void InitField() {
			if(initializers.Count > 0 && instance != null) {
				var instanceType = instance.GetType();
				foreach(var init in initializers) {
					var field = instanceType.GetField(init.name);
					if(field != null) {
						field.SetValueOptimized(instance, init.value.Get());
					}
				}
			}
		}

		public override void OnExecute() {
			InitField();
			if(instance is IFlowNode) {
				(instance as IFlowNode).Execute(owner);
				Finish(onFinished);
				return;
			} else if(instance is IStateNode) {
				if((instance as IStateNode).Execute(owner)) {
					Finish(onSuccess, onFinished);
				} else {
					Finish(onFailure, onFinished);
				}
				return;
			} else if(instance is ICoroutineNode || instance is IStateCoroutineNode) {
				StartCoroutine(ExecuteCoroutine());
				return;
			} else if(instance == null) {
				throw new NullReferenceException("The reflected instance is null");
			}
			throw new InvalidOperationException();
		}

		protected override object Value() {
			if(instance is IDataNode) {
				InitField();
				return (instance as IDataNode).GetValue(owner);
			}
			return null;
		}
		#endregion

		public override System.Type ReturnType() {
			if(instance is IDataNode) {
				return (instance as IDataNode).ReturnType();
			}
			return typeof(object);
		}

		public override bool IsFlowNode() {
			if(instance is IFlowNode || instance is IStateNode || instance is IStateCoroutineNode || instance is ICoroutineNode) {
				return true;
			}
			return false;
		}

		public override bool IsSelfCoroutine() {
			if(instance is IStateCoroutineNode || instance is ICoroutineNode) {
				return true;
			}
			//  else if(IsFlowNode()) {
			// 	return HasCoroutineInFlow(onFinished, onSuccess, onFailure);
			// }
			return false;
		}

		public override bool IsCoroutine() {
			if(IsSelfCoroutine())
				return true;
			if(IsFlowNode()) {
				return HasCoroutineInFlow(onFinished, onSuccess, onFailure);
			}
			return false;
		}

		public override void SetValue(object value) {
			
			base.SetValue(value);
		}

		public override bool CanSetValue() {
			
			return base.CanSetValue();
		}

		public override bool CanGetValue() {
			if(instance is IDataNode) {
				return true;
			}
			return false;
		}

		#region Code Generator
		public override string GenerateValueCode() {
			if(instance == null) throw null;
			string init = GenerateInitializerCode();
			string invoke = null;
			if(instance is DataNode) {
				invoke = generatedInstanceName.InvokeCode(
					nameof(DataNode.GetValue), 
					new Type[] { instance.GetType() }, 
					owner.ToCode())
				.RemoveSemicolon();
			} else if(instance.GetType().HasImplementInterface(typeof(IDataNode<>))) {
				invoke = generatedInstanceName.InvokeCode(nameof(IDataNode<bool>.GetValue), owner.ToCode()).RemoveSemicolon();
			} else {
				invoke = generatedInstanceName.InvokeCode(nameof(IDataNode.GetValue), owner.ToCode()).RemoveSemicolon().ConvertCode(instance.GetType());
			}
			if(string.IsNullOrEmpty(init)) {
				return invoke;
			} else {
				return typeof(uNodeUtility).ParseType().InvokeCode(
					nameof(uNodeUtility.RuntimeGetValue),
					CodeGenerator.GenerateAnonymousMethod(null, null,
						CodeGenerator.GenerateFlowStatement(
							init,
							CodeGenerator.GenerateReturn(invoke)
						))).RemoveLast();
			}
		}

		private string GenerateInitializerCode() {
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			foreach(var init in initializers) {
				if(init.value.isAssigned && !init.value.CanSafeGetValue()) { //Ensure we are only set the dynamic value
					builder.Append(generatedInstanceName.Access(init.name).Set(init.value.ToCode()).AddLineInFirst());
				}
			}
			return builder.ToString();
		}

		public override string GenerateCode() {
			if(IsFlowNode()) {
				string init = GenerateInitializerCode();
				if (instance is IFlowNode) {
					return CodeGenerator.GenerateFlowStatement(
						init,
						generatedInstanceName.InvokeCode(nameof(IFlowNode.Execute), owner.ToCode()),
						CodeGenerator.GetFinishCode(this, true, false, false, onFinished)
					);
				} else if (instance is IStateNode) {
					if(CodeGenerator.debugScript || CodeGenerator.CanReturnState(this)) {
						//If debug are on or return state is supported
						return CodeGenerator.GenerateFlowStatement(
							init,
							CodeGenerator.GenerateIfStatement(
								generatedInstanceName.InvokeCode(nameof(IStateNode.Execute), owner.ToCode()).RemoveSemicolon(),
								CodeGenerator.GetFinishCode(this, true, true, false, onSuccess, onFinished),
								CodeGenerator.GetFinishCode(this, false, true, false, onFailure, onFinished)
							)
						);
					}
					return CodeGenerator.GenerateFlowStatement(
						init,
						CodeGenerator.GenerateIfStatement(
							generatedInstanceName.InvokeCode(nameof(IStateNode.Execute), owner.ToCode()).RemoveSemicolon(),
							CodeGenerator.GenerateFlowCode(onSuccess, this),
							CodeGenerator.GenerateFlowCode(onFailure, this)
						),
						CodeGenerator.GetFinishCode(this, true, false, false, onFinished)
					);
				} else if (instance is ICoroutineNode) {
					CodeGenerator.RegisterCoroutineEvent(
						instance, 
						() => generatedInstanceName.InvokeCode(nameof(ICoroutineNode.Execute), owner.ToCode()).RemoveLast(), true);
					return CodeGenerator.GenerateFlowStatement(
						init,
						CodeGenerator.WaitEvent(instance),
						CodeGenerator.GetFinishCode(this, true, false, false, onFinished)
					);
				} else if (instance is IStateCoroutineNode) {
					CodeGenerator.RegisterCoroutineEvent(
						instance, 
						() => generatedInstanceName.InvokeCode(nameof(IStateCoroutineNode.Execute), owner.ToCode()).RemoveLast(), true);
					if(CodeGenerator.debugScript || CodeGenerator.CanReturnState(this)) {
						//If debug are on or return state is supported
						return CodeGenerator.GenerateFlowStatement(
							init,
							CodeGenerator.WaitEvent(instance),
							CodeGenerator.GenerateIfStatement(
								CodeGenerator.CompareEventState(instance, true),
								CodeGenerator.GetFinishCode(this, true, true, false, onSuccess, onFinished),
								CodeGenerator.GetFinishCode(this, false, true, false, onFailure, onFinished)
							)
						);
					}
					return CodeGenerator.GenerateFlowStatement(
						init,
						CodeGenerator.WaitEvent(instance),
						CodeGenerator.GenerateIfStatement(
							CodeGenerator.CompareEventState(instance, true),
							CodeGenerator.GenerateFlowCode(onSuccess, this),
							CodeGenerator.GenerateFlowCode(onFailure, this)),
						CodeGenerator.GetFinishCode(this, true, false, false, onFinished)
					);
				}
			}
			return null;
		}
		#endregion

		public override string GetNodeName() {
			Type instancecType = type.startType;
			if (instancecType != null) {
				if (instancecType.IsDefined(typeof(NodeMenu), true)) {
					return (instancecType.GetCustomAttributes(typeof(NodeMenu), true)[0] as NodeMenu).name;
				}
			} else {
				return "Missing Type";
			}
			return type.DisplayName(false, false);
		}

		public override void CheckError() {
			base.CheckError();
			// uNodeUtility.CheckError(type, this, nameof(type));
		}

		public override Type GetNodeIcon() {
			if(instance is IIcon) {
				return (instance as IIcon).GetIcon();
			}
			return base.GetNodeIcon();
		}

		public void Refresh() {
			RegisterPort();
		}
	}
}