using System;

namespace MaxyGames.uNode {
	[NodeMenu("Flow", "SetValue")]
	public class NodeSetValue : Node {
		public SetType setType;

		[Hide, FieldConnection("", true)]
		public MemberData onFinished = new MemberData();
		[Hide, FieldConnection(false), Filter(SetMember = true), UnityEngine.Tooltip("The target to set its value")]
		public MemberData target = new MemberData();
		[Hide, FieldConnection(false), ObjectType("target"), UnityEngine.Tooltip("The value to set the target value")]
		public MemberData value = new MemberData();

		public override void OnExecute() {
			if(target.isAssigned && value.isAssigned) {
				object obj = value.Get();
				switch(setType) {
					case SetType.Change:
						target.Set(obj);
						break;
					case SetType.Add:
						if(obj is Delegate) {
							object targetVal = target.Get();
							if(targetVal is MemberData.Event) {
								MemberData.Event e = targetVal as MemberData.Event;
								if(e.eventInfo != null) {
									e.eventInfo.AddEventHandler(e.instance, ReflectionUtils.ConvertDelegate(obj as Delegate, e.eventInfo.EventHandlerType));
								}
							} else if(targetVal == null) {
								target.Set(obj);
							} else {
								target.Set(Operator.Add(targetVal, obj));
							}
						} else {
							target.Set(Operator.Add(target.Get(), obj));
						}
						break;
					case SetType.Divide:
						target.Set(Operator.Divide(target.Get(), obj));
						break;
					case SetType.Subtract:
						if(obj is Delegate) {
							object targetVal = target.Get();
							if(targetVal is MemberData.Event) {
								MemberData.Event e = targetVal as MemberData.Event;
								if(e.eventInfo != null) {
									e.eventInfo.RemoveEventHandler(e.instance, ReflectionUtils.ConvertDelegate(obj as Delegate, e.eventInfo.EventHandlerType));
								}
							} else if(targetVal != null) {
								target.Set(Operator.Add(targetVal, obj));
							}
						} else {
							target.Set(Operator.Subtract(target.Get(), obj));
						}
						break;
					case SetType.Multiply:
						target.Set(Operator.Multiply(target.Get(), obj));
						break;
					case SetType.Modulo:
						target.Set(Operator.Modulo(target.Get(), obj));
						break;
				}
			} else {
				throw new System.Exception("Target is unassigned.");
			}
			Finish(onFinished);
		}

		public override void CheckError() {
			base.CheckError();
			uNodeUtility.CheckError(target, this, "target");
			uNodeUtility.CheckError(value, this, "value");
			if(target.isAssigned && value.isAssigned && target.type != null) {
				try {
					bool isDelegate = target.type.IsSubclassOf(typeof(Delegate));
					if(isDelegate) {
						if(setType != SetType.Add && setType != SetType.Subtract) {
							if(target.targetType == MemberData.TargetType.Event) {
								RegisterEditorError("Event can only be Add or Remove");
							} else if(target.targetType == MemberData.TargetType.ValueNode && target.instance is MultipurposeNode) {
								var node = target.instance as MultipurposeNode;
								if(node.target.target.targetType == MemberData.TargetType.Event) {
									RegisterEditorError("Event can only be Add or Remove");
								}
							}
						}
					} else if(value.type != null) {
						if(setType == SetType.Change) {
							if(!value.type.IsCastableTo(typeof(uNodeRoot)) &&
								!value.type.IsCastableTo(target.type) &&
								!target.type.IsCastableTo(value.type) &&
								value.type != typeof(object)) {

								RegisterEditorError(value.type.PrettyName(true) + " can't be set to " + target.type.PrettyName(true));
							}
						} else if(ReflectionUtils.CanCreateInstance(target.type) && ReflectionUtils.CanCreateInstance(value.type)) {
							uNodeHelper.SetObject(
								ReflectionUtils.CreateInstance(target.type),
								ReflectionUtils.CreateInstance(value.type), setType);
						}
					}
				}
				catch(System.Exception ex) {
					RegisterEditorError(ex.Message);
				}
			}
		}

		public override string GenerateCode() {
			if(target.isAssigned && value.isAssigned) {
				if(!value.type.IsCastableTo(typeof(uNodeRoot))) {
					return CodeGenerator.GenerateFlowStatement(
						CodeGenerator.GenerateSetCode(target, value, setType, target.type, value.type),
						CodeGenerator.GetFinishCode(this, true, false, false, onFinished)
					);
				} else {
					return CodeGenerator.GenerateFlowStatement(
						CodeGenerator.GenerateSetCode(target, value, setType),
						CodeGenerator.GetFinishCode(this, true, false, false, onFinished)
					);
				}
			}
			return CodeGenerator.GetFinishCode(this, true, false, false, onFinished);
		}

		public override string GetNodeName() {
			switch(setType) {
				case SetType.Change:
					return "Set Value";
				case SetType.Add:
					return "Add Value";
				case SetType.Divide:
					return "Divide Value";
				case SetType.Subtract:
					return "Subtract Value";
				case SetType.Multiply:
					return "Multiply Value";
				case SetType.Modulo:
					return "Mod Value";
			}
			return base.GetNodeName();
		}

		public override string GetRichName() {
			var setCode = "=";
			switch(setType) {
				case SetType.Add:
					setCode = "+=";
					break;
				case SetType.Divide:
					setCode = "/=";
					break;
				case SetType.Subtract:
					setCode = "-=";
					break;
				case SetType.Multiply:
					setCode = "*=";
					break;
				case SetType.Modulo:
					setCode = "%=";
					break;
			}
			return $"{target.GetNicelyDisplayName(richName:true)} {setCode} {value.GetNicelyDisplayName(richName:true)}";
		}

		public override Type GetNodeIcon() {
			switch(setType) {
				case SetType.Change:
					return typeof(TypeIcons.ChangeIcon);
				case SetType.Add:
					return typeof(TypeIcons.AddIcon);
				case SetType.Divide:
					return typeof(TypeIcons.DivideIcon);
				case SetType.Subtract:
					return typeof(TypeIcons.SubtractIcon);
				case SetType.Multiply:
					return typeof(TypeIcons.MultiplyIcon);
				case SetType.Modulo:
					return typeof(TypeIcons.ModuloIcon);
			}
			return base.GetNodeIcon();
		}

		public override bool IsCoroutine() {
			return HasCoroutineInFlow(onFinished);
		}
	}
}