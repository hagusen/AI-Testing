using UnityEngine;
using System;
using MaxyGames.uNode;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Yield", "Timer", IsCoroutine = true, HideOnFlow =true)]
	public class NodeTimer : Node {
		[System.NonSerialized]
		public FlowInput start = new FlowInput("Start");
		[System.NonSerialized]
		public FlowInput reset = new FlowInput("Reset");

		[Hide, FieldConnection(), Filter(typeof(float))]
		public MemberData waitTime = new MemberData(1f);

		[Hide, FieldConnection("On Start", true)]
		public MemberData onStart = new MemberData();
		[Hide, FieldConnection("On Update", true)]
		public MemberData onUpdate = new MemberData();
		[Hide, FieldConnection("On Finished", true)]
		public MemberData onFinished = new MemberData();

		private bool timerOn;
		private float time;

		public override void RegisterPort() {
			start.onExecute = () => {
				if(!timerOn) {
					timerOn = true;
					time = 0;
					if(onStart.isAssigned) {
						onStart.InvokeFlow();
					}
				}
			};
			reset.onExecute = () => {
				timerOn = false;
				time = 0;
			};
			if(runtimeUNode != null) {
				runtimeUNode.onUpdate += DoUpdate;
			}
			if(CodeGenerator.isGenerating) {
				CodeGenerator.RegisterFlowNode(this);
				start.codeGeneration += () => {
					return CodeGenerator.GenerateIfStatement(
						"timerOn".ToVariableName(this).NotOperation(),
						CodeGenerator.GenerateFlowStatement(
							"timerOn".ToVariableName(this).Set(true.ToCode()),
							"time".ToVariableName(this).Set(0.ToCode()),
							onStart.ToFlowCode(this, CodeGenerator.allowYieldStatement)
						)
					);
				};
				reset.codeGeneration += () => {
					return CodeGenerator.GenerateFlowStatement(
						CodeGenerator.GenerateSetCode("timerOn".ToVariableName(this), false.ParseValue()),
						CodeGenerator.GenerateSetCode("time".ToVariableName(this), 0.ParseValue())
					);
				};
				CodeGenerator.RegisterNodeSetup(InitCodeGeneration, this);
			}
		}

		void DoUpdate() {
			if(timerOn) {
				time += Time.deltaTime;
				if(time >= waitTime.GetValue<float>()) {
					time = 0;
					timerOn = false;
					if(onFinished.isAssigned) {
						onFinished.InvokeFlow();
					}
				} else if(onUpdate.isAssigned) {
					onUpdate.InvokeFlow();
				}
			}
		}

		void InitCodeGeneration() {
			CodeGenerator.RegisterPrivateVariable("timerOn".ToVariableName(this), typeof(bool), false);
			CodeGenerator.RegisterPrivateVariable("time".ToVariableName(this), typeof(float), 0);
			var updateContents = CodeGenerator.GenerateCondition(
					"if",
					"timerOn".ToVariableName(this),
					CodeGenerator.GenerateFlowStatement(
						"time".ToVariableName(this).Set(typeof(Time).Access("deltaTime"), SetType.Add),
						CodeGenerator.GenerateIfStatement(
							"time".ToVariableName(this).Compare(waitTime.ParseValue(), ComparisonType.GreaterThanOrEqual),
							CodeGenerator.GenerateFlowStatement(
								"time".ToVariableName(this).Set(0.ToCode()),
								"timerOn".ToVariableName(this).Set(false.ToCode()),
								onFinished.ToFlowCode(this, false)
							), onUpdate.ToFlowCode(this, false)
						)
					)
				);
			if(CodeGenerator.includeGraphInformation) {
				//Wrap the update contents with information of this node.
				updateContents = CodeGenerator.WrapWithInformation(updateContents, this);
			}
			CodeGenerator.InsertMethodCode("Update", typeof(void), updateContents);
		}

		public override bool IsFlowNode() {
			return false;
		}

		public override void CheckError() {
			base.CheckError();
			uNodeUtility.CheckError(waitTime, this, "waitTime");
		}

		public override Type ReturnType() {
			return typeof(TypeIcons.ClockIcon);
		}
	}
}
