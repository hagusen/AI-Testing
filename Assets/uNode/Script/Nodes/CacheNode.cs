using UnityEngine;

namespace MaxyGames.uNode.Nodes {
	public class CacheNode : ValueNode {
		[Hide, FieldConnection("Value")]
		public MemberData target;
		[Hide, FieldConnection("", true, hideOnNotFlowNode = true)]
		public MemberData onFinished = new MemberData();

		private object cachedValue;
		private VariableData generatedVariable;

		protected override object Value() => cachedValue;

		public override void RegisterPort() {
			if(CodeGenerator.isGenerating) {
				generatedVariable = new VariableData("cachedValue", ReturnType()) {
					modifier = FieldModifier.PrivateModifier,
				};
				generatedVariable.Name = CodeGenerator.RegisterVariable(generatedVariable).name;
			}
		}

		public override void OnExecute() {
			cachedValue = target.Get();
			Finish(onFinished);
		}

		public override System.Type ReturnType() {
			if(target.isAssigned) {
				try {
					return target.type;
				}
				catch { }
			}
			return typeof(object);
		}

		public override string GenerateCode() {
			return CodeGenerator.GenerateFlowStatement(
				generatedVariable.Name.Set(target.ToCode()),
				CodeGenerator.GetFinishCode(this, true, false, false, onFinished)
			);
		}

		public override string GenerateValueCode() {
			return generatedVariable.Name;
		}

		public override string GetNodeName() => "Cached";

		public override string GetRichName() {
			return $"Cache Value: {target.GetNicelyDisplayName(richName:true)}";
		}

		public override bool IsFlowNode() => true;

		public override bool IsCoroutine() {
			return HasCoroutineInFlow(onFinished);
		}

		public override void CheckError() => uNodeUtility.CheckError(target, this, nameof(target));
	}
}