using System;
using System.Collections;
using System.Collections.Generic;
using MaxyGames.uNode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MaxyGames.Events {
	public class HLAction : Action {
		[Hide]
		public MemberData type = MemberData.none;
		[Hide, Filter(typeof(bool), SetMember = true)]
		public MemberData storeResult = MemberData.none;
		[HideInInspector]
		public List<FieldValueData> initializers = new List<FieldValueData>();

		private new object instance;

		private void Init() {
			if(instance != null) return;
			var instanceType = type.startType;
			if (instanceType != null) {
				instance = ReflectionUtils.CreateInstance(instanceType);
			}
		}

		protected override void OnExecute() {
			Init();
			if(initializers.Count > 0 && instance != null) {
				var instanceType = instance.GetType();
				foreach(var init in initializers) {
					var field = instanceType.GetField(init.name);
					if(field != null) {
						field.SetValueOptimized(instance, init.value.Get());
					}
				}
			}
			if(instance is IFlowNode) {
				(instance as IFlowNode).Execute(base.instance);
			} else if(instance is IStateNode) {
				var result = (instance as IStateNode).Execute(base.instance);
				if(storeResult.isAssigned) {
					storeResult.Set(result);
				}
			} else {
				throw new InvalidOperationException();
			}
		}

		protected override IEnumerator ExecuteCoroutine() {
			Init();
			IEnumerator iterator;
			if(instance is ICoroutineNode) {
				iterator = (instance as ICoroutineNode).Execute(base.instance).GetEnumerator();
			} else if(instance is IStateCoroutineNode) {
				iterator = (instance as IStateCoroutineNode).Execute(base.instance).GetEnumerator();
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
				if(storeResult.isAssigned) {
					storeResult.Set(resultState == StateType.Success);
				}
			}
		}

		public override bool IsCoroutine() {
			var type = this.type.startType;
			if(type != null) {
				if(type.IsCastableTo(typeof(ICoroutineNode)) || type.IsCastableTo(typeof(IStateCoroutineNode))) {
					return true;
				}
			}
			return false;
		}

		public override string GenerateCode(Object obj) {
			if(obj is INode) {
				//Ensure we are targeting graph so the generated code will using 'this' keyword
				obj = (obj as INode).GetNodeOwner() as Object;
			}
			Init();
			if(!CodeGenerator.HasUserObject(this)) {
				foreach(var init in initializers) {
					if(init.value.CanSafeGetValue()) {
						var field = instance.GetType().GetField(init.name);
						if(field != null) {
							field.SetValueOptimized(instance, init.value.Get());
						}
					}
				}
				CodeGenerator.RegisterUserObject(new VariableData(Name, instance.GetType(), instance) {
					modifier = FieldModifier.PrivateModifier,
				}, this);
			}
			var variable = CodeGenerator.GetUserObject<VariableData>(this);
			string generatedInstanceName = CodeGenerator.AddVariable(variable);
			//Initialize instance
			System.Text.StringBuilder builder = new System.Text.StringBuilder();
			foreach(var init in initializers) {
				if(init.value.isAssigned && !init.value.CanSafeGetValue()) { //Ensure we are only set the dynamic value
					builder.Append(generatedInstanceName.Access(init.name).Set(init.value.ToCode()).AddLineInFirst());
				}
			}
			string initCode = builder.ToString();
			if (instance is IFlowNode || instance is IStateNode && !storeResult.isAssigned) {
				return CodeGenerator.GenerateFlowStatement(
					initCode,
					generatedInstanceName.InvokeCode(nameof(IFlowNode.Execute), obj.ToCode())
				);
			} else if (instance is IStateNode) {
				return CodeGenerator.GenerateFlowStatement(
					initCode,
					CodeGenerator.GenerateSetCode(
						storeResult, 
						generatedInstanceName.InvokeCode(nameof(IStateNode.Execute), obj.ToCode()).RemoveSemicolon())
				);
			} else if (instance is ICoroutineNode || instance is IStateCoroutineNode && !storeResult.isAssigned) {
				CodeGenerator.RegisterCoroutineEvent(
					instance, 
					() => generatedInstanceName.InvokeCode(nameof(ICoroutineNode.Execute), obj.ToCode()).RemoveLast(), true);
				return CodeGenerator.GenerateFlowStatement(
					initCode,
					CodeGenerator.WaitEvent(instance)
				);
			} else if (instance is IStateCoroutineNode) {
				CodeGenerator.RegisterCoroutineEvent(
					instance,
					() => generatedInstanceName.InvokeCode(nameof(IStateCoroutineNode.Execute), obj.ToCode()).RemoveLast(), true);
				return CodeGenerator.GenerateFlowStatement(
					initCode,
					CodeGenerator.WaitEvent(instance),
					CodeGenerator.GenerateSetCode(storeResult, CodeGenerator.CompareEventState(instance, true))
				);
			}
			return null;
		}

		public override string Name {
			get {
				Type instancecType = type.startType;
				if (instancecType != null) {
					if (instancecType.IsDefined(typeof(BlockMenuAttribute), true)) {
						return (instancecType.GetCustomAttributes(typeof(BlockMenuAttribute), true)[0] as BlockMenuAttribute).name;
					}
				} else {
					return "Missing Type";
				}
				return type.DisplayName(false, false);
			}
		}

		public override string ToolTip {
			get {
				
				return base.ToolTip;
			}
		}
	}
}