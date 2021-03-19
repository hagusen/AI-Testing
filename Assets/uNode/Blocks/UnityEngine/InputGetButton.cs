using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("UnityEngine", "Input.GetButton", hideOnBlock = true)]
	public class InputGetButton : AnyBlock {
		public enum GetButtonType {
			GetButton,
			GetButtonDown,
			GetButtonUp,
		}
		public GetButtonType getButtonType;
		[ObjectType(typeof(string))]
		public MemberData buttonName = new MemberData("");
		[Filter(typeof(bool), SetMember = true)]
		public MemberData storeValue;

		private bool condition;

		protected override void OnExecute() {
			if(buttonName.isAssigned && storeValue.isAssigned) {
				storeValue.Set(GetButton(buttonName.Get<string>()));
			}
		}

		protected override bool OnValidate() {
			if(buttonName.isAssigned) {
				condition = GetButton(buttonName.Get<string>());
				if(storeValue.isAssigned) {
					storeValue.Set(condition);
				}
				return condition;
			}
			return true;
		}

		public bool GetButton(string buttonName) {
			if(getButtonType == GetButtonType.GetButton) {
				return Input.GetButton(buttonName);
			} else if(getButtonType == GetButtonType.GetButtonDown) {
				return Input.GetButtonDown(buttonName);
			} else if(getButtonType == GetButtonType.GetButtonUp) {
				return Input.GetButtonUp(buttonName);
			}
			return false;
		}

		public override string GenerateCode(Object obj) {
			string data = null;
			if(getButtonType == GetButtonType.GetButton) {
				data = CodeGenerator.GetInvokeCode(typeof(Input), "GetButton", buttonName).ToString();
			} else if(getButtonType == GetButtonType.GetButtonDown) {
				data = CodeGenerator.GetInvokeCode(typeof(Input), "GetButtonDown", buttonName).ToString();
			} else if(getButtonType == GetButtonType.GetButtonUp) {
				data = CodeGenerator.GetInvokeCode(typeof(Input), "GetButtonUp", buttonName).ToString();
			}
			if(storeValue.isAssigned) {
				return CodeGenerator.ParseValue((object)storeValue) + " = " + data;
			}
			return data;
		}

		public override string GenerateConditionCode(Object obj) {
			return GenerateCode(obj).RemoveSemicolon();
		}

		public override string GetDescription() {
			if(getButtonType == GetButtonType.GetButton) {
				return "Returns true while the virtual button identified by buttonName is held down.";
			} else if(getButtonType == GetButtonType.GetButtonDown) {
				return "Returns true during the frame the user pressed down the virtual button identified by buttonName.";
			} else if(getButtonType == GetButtonType.GetButtonUp) {
				return "Returns true the first frame the user releases the virtual button identified by buttonName.";
			}
			return null;
		}

		public override void CheckError(Object owner) {
			base.CheckError(owner);
			uNode.uNodeUtility.CheckError(buttonName, owner, Name + " - buttonName");
			uNode.uNodeUtility.CheckError(storeValue, owner, Name + " - storeValue");
		}
	}
}