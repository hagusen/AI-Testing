using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Composites", "RandomSelector", IsCoroutine = true)]
	[Description("Execute node randomly, it will return success if any node return success " +
		"and if all of the node return failure it will return failure.")]
	public class RandomSelector : Node {
		[HideInInspector, FieldConnection(true)]
		public List<MemberData> targetNodes = new List<MemberData>() { new MemberData() };

		IEnumerator OnUpdate() {
			List<int> eventIndex = new List<int>();
			for(int i = 0; i < targetNodes.Count; ++i) {
				eventIndex.Add(i);
			}
			List<int> randomOrder = new List<int>();
			for(int i = targetNodes.Count; i > 0; --i) {
				int index = Random.Range(0, i);
				randomOrder.Add(eventIndex[index]);
				eventIndex.RemoveAt(index);
			}
			for(int i = 0; i < targetNodes.Count; i++) {
				var t = targetNodes[randomOrder[i]];
				if(!t.isAssigned)
					continue;
				Node n;
				WaitUntil w;
				if(!t.ActivateFlowNode(out n, out w)) {
					yield return w;
				}
				if(n == null) {
					throw new System.Exception("targetNode must be FlowNode");
				}
				JumpStatement js = n.GetJumpState();
				if(js != null) {
					jumpState = js;
					Finish();
					yield break;
				}
				if(n.currentState == StateType.Success) {
					state = StateType.Success;
					Finish();
					yield break;
				}
			}
			state = StateType.Failure;
			Finish();
		}

		public override void OnExecute() {
			owner.StartCoroutine(OnUpdate(), this);
		}

		public override bool IsSelfCoroutine() {
			return true;
		}

		public override void RegisterPort() {
			base.RegisterPort();
			if(CodeGenerator.isGenerating) {
				//Register this node as state node, because this is coroutine node with state.
				CodeGenerator.RegisterAsStateNode(this);
				for(int i = 0; i < targetNodes.Count; i++) {
					var node = targetNodes[i].GetTargetNode();
					if(node != null) {
						//Register each target node as state node, because this node need to compare the target state.
						CodeGenerator.RegisterAsStateNode(node);
					}
				}
			}
		}

		public override string GenerateCode() {
			string data = null;
			if(targetNodes.Count > 0) {
				int transitionCount = targetNodes.Count;
				string EI = CodeGenerator.GenerateVariableName("eventIndex", this);
				string RO = CodeGenerator.GenerateVariableName("randomOrder", this);
				string var = CodeGenerator.GenerateVariableName("index", this);
				string ListName = CodeGenerator.ParseType(typeof(List<int>));
				string RandomData = null;
				{
					RandomData += ListName + " " + EI + " = new " + ListName + "() { ";
					for(int i = 0; i < targetNodes.Count; i++) {
						if(i != 0) {
							RandomData += ", ";
						}
						RandomData += i;
					}
					RandomData += "};\n";
					RandomData += ListName + " " + RO + " = new " + ListName + "();\n";
					string contents = null;
					string index = CodeGenerator.GenerateVariableName("i", this);
					string ind = CodeGenerator.GenerateVariableName("indexs", this);
					contents += "int " + ind + " = " + CodeGenerator.GenerateInvokeCode(typeof(Random), "Range", CodeGenerator.ParseValue(0), index) + "\n";
					contents += RO + ".Add(" + EI + "[" + ind + "]);\n";
					contents += EI + ".RemoveAt(" + ind + ");";
					RandomData += "for(int " + index + " = " + (transitionCount - 1) + "; " + index + " >= 0; --" + index + ") {";
					RandomData += contents.AddLineInFirst().AddTabAfterNewLine(1).AddLineInEnd();
					RandomData += "}\n";
				}
				{
					string successReturnVal = CodeGenerator.GetFinishCode(this, true);
					for(int i = 0; i < targetNodes.Count; i++) {
						var t = targetNodes[i];
						if(i != 0) {
							data += " else ";
						}
						string s = null;
						s += CodeGenerator.GenerateFlowCode(t, this).AddLineInEnd();
						if(CodeGenerator.debugScript) {
							s += CodeGenerator.GenerateDebugCode(t).AddLineInEnd();
						}
						s += CodeGenerator.GenerateCondition("if", CodeGenerator.CompareNodeState(t.GetTargetNode(), true, false), successReturnVal);
						data += CodeGenerator.GenerateCondition("if", var + " == " + i, s);
					}
					if(!string.IsNullOrEmpty(data) && !string.IsNullOrEmpty(RandomData)) {
						string index = CodeGenerator.GenerateVariableName("loop", this);
						data = RandomData + CodeGenerator.GenerateForStatement("int " + index + " = 0", index + " < " + transitionCount, index + "++",
							("int " + var + " = " + RO + "[" + index + "];" + data.AddLineInFirst() + "yield return null;".AddLineInFirst())).AddLineInFirst();
						data += CodeGenerator.GetFinishCode(this, false, true, false);
					}
				}
			}
			return data;
		}

		public override System.Type GetNodeIcon() {
			return typeof(TypeIcons.BranchIcon);
		}
	}
}
