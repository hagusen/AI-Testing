using UnityEngine;
using System;
using Object = UnityEngine.Object;

namespace MaxyGames.Events {
	[BlockMenu("★General", "ChangeType")]
	public class ChangeType : Action {
		public MemberData target;
		[Filter(OnlyGetType = true)]
		public MemberData type;
		[Filter(SetMember = true)]
		[ObjectType("type", isElementType = true)]
		public MemberData storeResult;

		public override string Name {
			get {
				return string.Format("Convert: <b>{2}</b> to <b>{1}</b> store to <b>{0}</b>",
					uNode.uNodeUtility.GetNicelyDisplayName(storeResult),
					uNode.uNodeUtility.GetNicelyDisplayName(type, true, false),
					uNode.uNodeUtility.GetNicelyDisplayName(target));
			}
		}

		protected override void OnExecute() {
			if(target.isAssigned && type.isAssigned && storeResult.isAssigned) {
				object t = target.Get();
				Type Type = type.Get() as System.Type;
				if(object.ReferenceEquals(t, null) && (!Type.IsValueType || Nullable.GetUnderlyingType(Type) != null) || 
					!object.ReferenceEquals(t, null) && Type.IsAssignableFrom(t.GetType())) {
					storeResult.Set(t);
					return;
				}
				Type = Nullable.GetUnderlyingType(Type) ?? Type;
				storeResult.Set(System.Convert.ChangeType(t, Type));
			}
		}

		public override string GenerateCode(Object obj) {
			if(target.isAssigned && type.isAssigned && storeResult.isAssigned) {
				Type Type = type.Get() as System.Type;
				if(Type.IsAssignableFrom(target.type)) {
					if(Type.IsValueType) {
						return CodeGenerator.GenerateSetCode(storeResult, "(" + CodeGenerator.ParseType(Type) + ")" + CodeGenerator.ParseValue((object)target));
					}
					return CodeGenerator.GenerateSetCode(storeResult, CodeGenerator.ParseValue((object)target) + " as " + CodeGenerator.ParseType(Type));
				} else {
					if(Type.IsValueType) {
						return CodeGenerator.GenerateSetCode(storeResult, "(" + CodeGenerator.ParseType(Type) + ")" + CodeGenerator.GenerateInvokeCode(typeof(Convert), "ChangeType", CodeGenerator.ParseValue((object)target), CodeGenerator.ParseValue(Type)).Replace(";", ""));
					}
					return CodeGenerator.GenerateSetCode(storeResult, CodeGenerator.GenerateInvokeCode(typeof(Convert), "ChangeType", CodeGenerator.ParseValue((object)target), CodeGenerator.ParseValue(Type)).Replace(";", "") + " as " + CodeGenerator.ParseType(Type));
				}
			}
			return null;
		}

		public override void CheckError(Object owner) {
			base.CheckError(owner);
			uNode.uNodeUtility.CheckError(target, owner, Name + " - target");
			uNode.uNodeUtility.CheckError(type, owner, Name + " - type");
		}
	}
}