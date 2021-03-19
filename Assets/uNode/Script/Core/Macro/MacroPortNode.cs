using System;

namespace MaxyGames.uNode.Nodes {
	public class MacroPortNode : CustomNode, IMacroPort {
		[Hide]
		public string Name;
		[Hide]
		public PortKind kind;
		[Filter(OnlyGetType =true)]
		public MemberData type = new MemberData(typeof(object));
		[Hide]
		public MemberData target = MemberData.none;

		[Serializable]
		public class LinkedPort {
			public string guid;
			public uNodeMacro owner;

			private MacroPortNode _port;

			public MacroPortNode port {
				get {
					if(_port == null && owner != null) {
						_port = owner.GetPortByGuid(guid);
					}
					return _port;
				}
			}
		}
		[Hide]
		public LinkedPort linkedPort;

		public override object GetValue() {
			return target.Get();
		}

		public override void OnExecute() {
			if(target.isAssigned) {
				target.ActivateFlowNode();
			}
			Finish();
		}

		public override void RegisterPort() {
			if(parentComponent is IMacro) {
				(parentComponent as IMacro).InitMacroPort(this);
			}
		}

		public override bool IsFlowNode() {
			if(CodeGenerator.isGenerating) {
				return kind == PortKind.FlowInput || kind == PortKind.FlowOutput;
			}
			return kind == PortKind.FlowOutput;
		}

		public override bool CanGetValue() {
			if(CodeGenerator.isGenerating) {
				return kind == PortKind.ValueInput || kind == PortKind.ValueOutput;
			}
			return kind == PortKind.ValueInput;
		}

		public override string GenerateCode() {
			if(target.isAssigned) {
				return CodeGenerator.GetFinishCode(this, true, target);
			}
			return null;
		}

		public override string GenerateValueCode() {
			if(target.isAssigned) {
				return CodeGenerator.ParseValue((object)target);
			}
			throw new Exception("Target is not assigned");
		}

		public override string GetNodeName() {
			return GetName();
		}

		public string GetName() {
			return (!string.IsNullOrEmpty(Name) ? Name.Trim() : gameObject.name);
		}

		public override void CheckError() {
			base.CheckError();
			var pComp = parentComponent;
			if(!(pComp == null && owner is IMacroGraph || pComp is IMacro)) {
				RegisterEditorError("Invalid node context, this node are valid only for macro graph.");
			}
		}

		public override Type GetNodeIcon() {
			if(kind == PortKind.FlowInput || kind == PortKind.ValueInput) {
				return typeof(TypeIcons.InputIcon);
			}
			return typeof(TypeIcons.OutputIcon);
		}

		public override Type ReturnType() {
			if(kind == PortKind.ValueInput || kind == PortKind.ValueOutput) {
				if(type.isAssigned) {
					return type.startType;
				} else {
					return typeof(object);
				}
			}
			return base.ReturnType();
		}

		public override bool IsCoroutine() {
			if(kind == PortKind.FlowInput && target.isAssigned) {
				var node = target.GetTargetNode();
				if(node != null) {
					return node.IsCoroutine();
				}
			}
			return base.IsCoroutine();
		}
	}
}