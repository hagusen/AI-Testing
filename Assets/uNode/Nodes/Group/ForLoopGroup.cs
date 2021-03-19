using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.uNode.Nodes {
	//[NodeMenu("Group", "ForLoop")]
	public class ForLoopGroup : GroupNode {
		[ObjectType(typeof(int))]
		public MemberData startIndex = new MemberData(0);
		public ComparisonType compareType = ComparisonType.LessThan;
		[ObjectType(typeof(int))]
		public MemberData compareNumber = new MemberData(10);
		public SetType iteratorSetType = SetType.Add;
		[ObjectType(typeof(int))]
		public MemberData iteratorSetValue = new MemberData(1);

		[System.NonSerialized]
		public VariableData Item = new VariableData("forItem", typeof(int));

		public override void OnExecute() {
			if(IsCoroutine()) {
				owner.StartCoroutine(OnUpdate(), this);
			} else {
				if(startIndex.isAssigned && compareNumber.isAssigned) {
					for(var index = startIndex.Get();
						uNodeHelper.OperatorComparison(index, compareNumber.Get(), compareType);
						uNodeHelper.SetObject(ref index, iteratorSetValue.Get(), iteratorSetType)) {
						Item.Set(index);
						JumpStatement js = nodeToExecute.ActivateAndFindJumpState();
						if(js != null) {
							if(js.jumpType == JumpStatementType.Continue) {
								continue;
							} else {
								if(js.jumpType == JumpStatementType.Return) {
									jumpState = js;
								}
								break;
							}
						}
					}
				} else {
					throw new System.Exception("The target array must be array list or generic list");
				}
				Finish();
			}
		}

		IEnumerator OnUpdate() {
			if(startIndex.isAssigned && compareNumber.isAssigned) {
				for(var index = startIndex.Get();
					uNodeHelper.OperatorComparison(index, compareNumber.Get(), compareType);
					uNodeHelper.SetObject(ref index, iteratorSetValue.Get(), iteratorSetType)) {
					Item.Set(index);
					JumpStatement js = nodeToExecute.ActivateAndFindJumpState();
					if(!nodeToExecute.IsFinished()) {
						yield return nodeToExecute.WaitUntilFinish();
					}
					if(js != null) {
						if(js.jumpType == JumpStatementType.Continue) {
							continue;
						} else {
							if(js.jumpType == JumpStatementType.Return) {
								jumpState = js;
							}
							break;
						}
					}
				}
			} else {
				throw new System.Exception("The target array must be array list or generic list");
			}
			Finish();
		}

		public override VariableData GetVariableData(string variableName) {
			return Item;
		}

		[System.NonSerialized]
		private List<VariableData> cachedVar;
		public override List<VariableData> Variables {
			get {
				if(cachedVar == null) {
					cachedVar = new List<VariableData>() { Item };
				}
				return cachedVar;
			}
		}

		public override string GenerateCode() {
			if(!startIndex.isAssigned || !compareNumber.isAssigned || !iteratorSetValue.isAssigned) return null;
			string indexName;
			string gData = null;
			if(CodeGenerator.NeedInstanceVariable(this)) {
				indexName = CodeGenerator.GenerateVariableName("for_index", this);
				CodeGenerator.AddVariable(Item);
				gData += CodeGenerator.GetVariableName(Item) + " = " + indexName + ";";
			} else {
				indexName = CodeGenerator.AddVariable(Item, false);
			}
			string data = CodeGenerator.GetCompareCode(indexName, compareNumber, compareType);
			string iterator = CodeGenerator.GenerateSetCode(indexName, iteratorSetValue, iteratorSetType);
			data = "for(int " + CodeGenerator.GenerateSetCode(indexName, (object)CodeGenerator.ParseValue((object)startIndex), SetType.Change) +
				data + ";" + iterator.Replace(";", "") + "){" + (gData +
				CodeGenerator.GenerateFlowCode(nodeToExecute, this)).AddLineInFirst().AddTabAfterNewLine(1).AddLineInEnd() + "}" +
				CodeGenerator.GetFinishCode(this, true, false, false).AddLineInFirst();
			return data;
		}
	}
}
