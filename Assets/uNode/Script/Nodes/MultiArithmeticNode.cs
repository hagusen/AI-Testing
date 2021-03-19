using System.Collections.Generic;
using System.Linq;

namespace MaxyGames.uNode.Nodes {
	// [NodeMenu("Operator", "Arithmetic {+} {-} {/} {*} {%} {^}")]
	public class MultiArithmeticNode : ValueNode {
		public ArithmeticType operatorType = ArithmeticType.Add;

		[UnityEngine.HideInInspector]
		public List<MemberData> targets = new List<MemberData>() { new MemberData(0), new MemberData(0) };

		public override System.Type ReturnType() {
			if(targets.Count >= 2) {
				try {
					bool isDivide = operatorType == ArithmeticType.Divide;
					object obj = ReflectionUtils.CreateInstance(targets[0].type);
					if(isDivide) {
						//For fix zero divide error.
						obj = Operator.IncrementPrimitive(obj);
					}
					for(int i = 1; i < targets.Count; i++) {
						object obj2 = ReflectionUtils.CreateInstance(targets[i].type);
						if(isDivide) {
							//For fix zero divide error.
							obj2 = Operator.IncrementPrimitive(obj2);
						}
						obj = uNodeHelper.ArithmeticOperator(obj, obj2, operatorType);
					}
					if(!object.ReferenceEquals(obj, null)) {
						return obj.GetType();
					}
				}
				catch { }
			}
			return typeof(object);
		}

		protected override object Value() {
			if(targets.Count >= 2) {
				object obj = targets[0].Get();
				for(int i = 1; i < targets.Count; i++) {
					obj = uNodeHelper.ArithmeticOperator(obj, targets[i].Get(), operatorType);
				}
				if(!object.ReferenceEquals(obj, null)) {
					return obj;
				}
			}
			throw null;
		}

		public override string GenerateValueCode() {
			if(targets.Count >= 2) {
				string contents = targets[0].ToCode();
				for(int i = 1; i < targets.Count; i++) {
					contents = CodeGenerator.GenerateArithmetiCode(contents, targets[i].ToCode(), operatorType).Wrap();
				}
				return contents;
			}
			throw new System.Exception("Target is unassigned");
		}

		public override string GetNodeName() {
			return operatorType.ToString();
		}

		public override string GetRichName() {
			string separator = null;
			switch(operatorType) {
				case ArithmeticType.Add:
					separator = " + ";
					break;
				case ArithmeticType.Divide:
					separator = " + ";
					break;
				case ArithmeticType.Modulo:
					separator = " + ";
					break;
				case ArithmeticType.Multiply:
					separator = " + ";
					break;
				case ArithmeticType.Subtract:
					separator = " + ";
					break;
			}
			return string.Join(separator, from target in targets select target.GetNicelyDisplayName(richName: true));
		}

		public override System.Type GetNodeIcon() {
			switch(operatorType) {
				case ArithmeticType.Add:
					return typeof(TypeIcons.AddIcon2);
				case ArithmeticType.Divide:
					return typeof(TypeIcons.DivideIcon2);
				case ArithmeticType.Subtract:
					return typeof(TypeIcons.SubtractIcon2);
				case ArithmeticType.Multiply:
					return typeof(TypeIcons.MultiplyIcon2);
				case ArithmeticType.Modulo:
					return typeof(TypeIcons.ModuloIcon2);
			}
			return typeof(TypeIcons.CalculatorIcon);
		}

		public override void CheckError() {
			base.CheckError();
			bool flag = uNodeUtility.CheckError(targets, this, "targets");
			if(targets.Count < 2) {
				RegisterEditorError("The minimal value input must be 2.");
			} else if(!flag) {
				try {
					bool isDivide = operatorType == ArithmeticType.Divide;
					object obj = ReflectionUtils.CreateInstance(targets[0].type);
					if(isDivide) {
						//For fix zero divide error.
						obj = Operator.IncrementPrimitive(obj);
					}
					for(int i = 1; i < targets.Count; i++) {
						object obj2 = ReflectionUtils.CreateInstance(targets[i].type);
						if(isDivide) {
							//For fix zero divide error.
							obj2 = Operator.IncrementPrimitive(obj2);
						}
						obj = uNodeHelper.ArithmeticOperator(obj, obj2, operatorType);
					}
				}
				catch(System.Exception ex) {
					RegisterEditorError(ex.Message);
				}
			}
		}
	}
}