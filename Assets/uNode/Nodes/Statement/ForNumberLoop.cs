using System.Collections;
using UnityEngine;

namespace MaxyGames.uNode.Nodes {
    [NodeMenu("Statement", "For")]
	[Description("The for number statement can run a node repeatedly until a condition evaluates to false.")]
	public class ForNumberLoop : Node {
		[Hide, FieldConnection("Body", true, displayFlowInHierarchy =false)]
		public MemberData body = new MemberData();

		[Filter(typeof(int), typeof(float), typeof(decimal), typeof(long), typeof(byte), typeof(sbyte),
			typeof(short), typeof(double), typeof(uint), typeof(ulong), typeof(ushort), OnlyGetType = true, UnityReference = false)]
		public MemberData indexType = new MemberData(typeof(int), MemberData.TargetType.Type);
		[Hide, FieldConnection("Start", false, true), ObjectType("indexType", isElementType = true)]
		public MemberData startIndex = new MemberData(0);
		public ComparisonType compareType = ComparisonType.LessThan;
		[Hide, FieldConnection("Count", false, true), ObjectType("indexType", isElementType = true)]
		public MemberData compareNumber = new MemberData(10);
		public SetType iteratorSetType = SetType.Add;
		[Hide, FieldConnection("Step", false, true), ObjectType("indexType", isElementType = true)]
		public MemberData iteratorSetValue = new MemberData(1);
		[Hide, Output("Index"), ObjectType("indexType", isElementType = true)]
		public object index = 0;

		[Hide, FieldConnection("Next", true)]
		public MemberData onFinished = new MemberData();

		public override void OnExecute() {
			if(!HasCoroutineInFlow(body)) {
				for(index = startIndex.Get(); uNodeHelper.OperatorComparison(index, compareNumber.Get(), compareType);
					uNodeHelper.SetObject(ref index, iteratorSetValue.Get(), iteratorSetType)) {
					if(!body.isAssigned) continue;
					Node n;
					WaitUntil w;
					if(!body.ActivateFlowNode(out n, out w)) {
						throw new System.Exception("body is not coroutine but body is not finished.");
					}
					if(n == null)//Skip on executing flow input pin.
						continue;
					JumpStatement js = n.GetJumpState();
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
				Finish(onFinished);
			} else {
				owner.StartCoroutine(OnUpdate(), this);
			}
		}

		IEnumerator OnUpdate() {
			for(index = startIndex.Get(); uNodeHelper.OperatorComparison(index, compareNumber.Get(), compareType);
				uNodeHelper.SetObject(ref index, iteratorSetValue.Get(), iteratorSetType)) {
				if(!body.isAssigned) continue;
				Node n;
				WaitUntil w;
				if(!body.ActivateFlowNode(out n, out w)) {
					yield return w;
				}
				if(n == null)//Skip on executing flow input pin.
					continue;
				JumpStatement js = n.GetJumpState();
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
			Finish(onFinished);
		}

		public override bool IsCoroutine() {
			return HasCoroutineInFlow(body, onFinished);
		}

		public override string GenerateCode() {
			if(!startIndex.isAssigned || !compareNumber.isAssigned || !iteratorSetValue.isAssigned) return null;
			var field = this.GetType().GetField("index");
			string indexName;
			string declaration = CodeGenerator.ParseType(indexType) + " ";
			if(CodeGenerator.NeedInstanceVariable(this, field, body)) {
				indexName = CodeGenerator.AddVariable(this, field, startIndex.type, true);
				declaration = null;
			} else {
				indexName = CodeGenerator.GetVariableName(this, field);
			}
			string data = CodeGenerator.GetCompareCode(indexName, compareNumber, compareType);
			string iterator = CodeGenerator.GenerateSetCode(indexName, iteratorSetValue, iteratorSetType);
			if(!string.IsNullOrEmpty(data) && !string.IsNullOrEmpty(iterator)) {
				data = CodeGenerator.GenerateForStatement(declaration +
					CodeGenerator.GenerateSetCode(indexName,
						(object)CodeGenerator.ParseValue(startIndex),
						SetType.Change).RemoveLast(), data, iterator.Replace(";", ""),
					CodeGenerator.GenerateFlowCode(body, this)) +
					CodeGenerator.GetFinishCode(this, true, false, false, onFinished).AddFirst("\n");
				return data;
			}
			return null;
		}

		public override string GetRichName() {
			if(!startIndex.isAssigned || !iteratorSetValue.isAssigned || !indexType.isAssigned || !compareNumber.isAssigned) {
				return base.GetRichName();
			}
			return $"{uNodeUtility.WrapTextWithKeywordColor("for")}({uNodeUtility.GetNicelyDisplayName(indexType, typeTargetWithTypeof:false)} i={startIndex.GetNicelyDisplayName(richName:true)}; {CodeGenerator.GenerateCompareCode("i", compareNumber.GetNicelyDisplayName(richName:true), compareType)}; {CodeGenerator.GenerateSetCode("i", iteratorSetValue.GetNicelyDisplayName(richName:true), iteratorSetType).RemoveSemicolon()})";
		}
	}
}
