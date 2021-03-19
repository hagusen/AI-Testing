﻿using UnityEngine;

namespace MaxyGames.uNode.Nodes {
    [NodeMenu("Data", "AS-Convert")]
	public class ASNode : ValueNode {
		[FieldDrawer(""), Filter(typeof(Component), AllowInterface =true, OnlyGetType = true, ArrayManipulator = true)]
		public MemberData type = new MemberData(typeof(object), MemberData.TargetType.Type);
		[Hide, FieldConnection("Value")]
		public MemberData target;
		public bool useASWhenPossible = true;
		public bool compactDisplay;

		public override System.Type ReturnType() {
			if(type.isAssigned) {
				try {
					System.Type t = type.Get<System.Type>();
					if(!object.ReferenceEquals(t, null)) {
						return t;
					}
				}
				catch { }
			}
			return typeof(object);
		}

		protected override object Value() {
			var value = target.Get();
			System.Type t = type.Get<System.Type>();
			if (value != null) {
				if(value.GetType() == t) return value;
				if(t == typeof(string)) {
					return value.ToString();
				} else if (t == typeof(GameObject)) {
					if(value is Component component) {
						return component.gameObject;
					}
				} else if (t.IsCastableTo(typeof(Component))) {
					if(value is GameObject gameObject) {
						if(t is RuntimeType) {
							return gameObject.GetGeneratedComponent(t as RuntimeType);
						}
						return gameObject.GetComponent(t);
					} else if(value is Component component) {
						if(t is RuntimeType) {
							return component.GetGeneratedComponent(t as RuntimeType);
						}
						return component.GetComponent(t);
					}
				}
			}
			if(!useASWhenPossible || t is RuntimeType || t.IsValueType) {
				value = Operator.Convert(value, t);
			} else {
				value = Operator.TypeAs(value, t);
			}
			return value;
		}

		public override string GenerateValueCode() {
			if(target.isAssigned && type.isAssigned) {
				System.Type t = type.startType;
				System.Type targetType = target.type;
				if(t != null && targetType != null) {
					if(!targetType.IsCastableTo(t) && !t.IsCastableTo(targetType)) {
						if(t == typeof(string)) {
							return CodeGenerator.ParseValue(target).InvokeCode(nameof(object.ToString)).RemoveLast();
						} else if(t == typeof(GameObject)) {
							if(targetType.IsCastableTo(typeof(Component))) {
								return CodeGenerator.ParseValue((object)target).Access(nameof(Component.gameObject));
							}
						} else if (t.IsCastableTo(typeof(Component))) {
							if (targetType.IsCastableTo(typeof(Component)) || targetType == typeof(GameObject)) {
								if(t == typeof(Transform)) {
									return CodeGenerator.ParseValue((object)target).Access(nameof(Component.transform));
								} else {
									return CodeGenerator.ParseValue(target).InvokeCode(nameof(Component.GetComponent), new System.Type[] { t }, null).RemoveLast();
								}
							}
						}
					}
				}
				if(!useASWhenPossible || t.IsValueType) {
					return CodeGenerator.GenerateConvertCode(target, t);
				}
				return CodeGenerator.GenerateAsCode(target, t);
			}
			throw new System.Exception();
		}

		public override string GetNodeName() {
			if(useASWhenPossible && type.isAssigned && type.startType != null && !type.startType.IsValueType) {
				return "AS";
			}
			return "Convert";
		}

		public override string GetRichName() {
			return $"({type.GetNicelyDisplayName(richName:true, typeTargetWithTypeof:false)})" + target.GetNicelyDisplayName(richName:true);
		}

		public override void CheckError() {
			uNodeUtility.CheckError(type, this, nameof(type));
			uNodeUtility.CheckError(target, this, nameof(target));
			if(type.isAssigned && target.isAssigned) {
				System.Type t = type.startType;
				System.Type targetType = target.type;
				if(t != null && targetType != null) {
					if(!targetType.IsCastableTo(t, true) && !t.IsCastableTo(targetType)) {
						bool valid = false;
						if(t == typeof(string)) {
							valid = true;
						} else if(t == typeof(GameObject)) {
							if(targetType.IsCastableTo(typeof(Component))) {
								valid = true;
							}
						} else if (t.IsCastableTo(typeof(Component))) {
							if (targetType.IsCastableTo(typeof(Component)) || targetType == typeof(GameObject)) {
								valid = true;
							}
						} else if(t.IsEnum && targetType.IsPrimitive) {
							valid = true;
						}
						if(!valid) {
							RegisterEditorError($"The target type:{targetType.PrettyName()} is not castable to type:{t.PrettyName()}");
						}
					}
				}
			}
		}
	}
}