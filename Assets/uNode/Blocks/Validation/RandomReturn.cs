using UnityEngine;
using MaxyGames.uNode;

namespace MaxyGames.Events {
	[BlockMenu("Other", "RandomReturn")]
	public class RandomReturn : Condition {
		[Tooltip("The list of bool for use to random chose then use it for validation if its true the validation will return true otherwise false")]
		public bool[] returnValue = new bool[2] { true, false };

		protected override bool OnValidate() {
			int num = 0;
			if(returnValue.Length > 1) {
				num = Random.Range(0, returnValue.Length-1);
			}
			return returnValue[num];
		}

		public override string ToolTip {
			get {
				string chance = "";
				int trueCount = 0;
				int falseCount = 0;
				if(returnValue.Length > 0) {
					foreach(bool boolVal in returnValue) {
						if(boolVal) {
							trueCount++;
						} else {
							falseCount++;
						}
					}
					int trueChance = trueCount * 100 / returnValue.Length;
					int falseChance = falseCount * 100 / returnValue.Length;
					chance += " -> (" + trueChance + "% True chance - " + falseChance + "% False chance)";
				}
				return base.ToolTip + chance;
			}
		}

		public override string GenerateConditionCode(Object obj) {
			if(returnValue.Length > 1) {
				VariableData variable = new VariableData(CodeGenerator.GenerateVariableName("_randomValue", this), typeof(bool[]));
				variable.Set(returnValue);
				CodeGenerator.AddVariable(variable, true, false);
				return variable.Name + "[" + CodeGenerator.GetInvokeCode(typeof(Random), "Range", 0, returnValue.Length - 1).Replace(";", "") + "]";
			}
			return base.GenerateConditionCode(obj);
		}
	}
}