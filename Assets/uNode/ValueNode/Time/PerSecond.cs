﻿using System;
using UnityEngine;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Math.Time", "Per Second", typeof(float))]
	public class PerSecond : ValueNode {
		[Hide, FieldConnection(), Filter(typeof(float), typeof(Vector2), typeof(Vector3), typeof(Vector4))]
		public MemberData input = new MemberData(1f);
		public bool unscaledTime;

		public override System.Type ReturnType() {
			if(input.isAssigned) {
				return input.type;
			}
			return typeof(float);
		}

		protected override object Value() {
			if(unscaledTime) {
				if(input.isAssigned) {
					Type t = input.type;
					return Operator.Multiply(input.Get(), Time.unscaledDeltaTime);
				}
				return 1 * Time.unscaledDeltaTime;
			} else {
				if(input.isAssigned) {
					Type t = input.type;
					return Operator.Multiply(input.Get(), Time.deltaTime);
				}
				return 1 * Time.deltaTime;
			}
		}

		public override string GenerateValueCode() {
			if(unscaledTime) {
				if(input.isAssigned) {
					return CodeGenerator.GenerateArithmetiCode(input.ParseValue(), typeof(Time).Access("unscaledDeltaTime"), ArithmeticType.Multiply);
				}
				return CodeGenerator.GenerateArithmetiCode(1.ParseValue(), typeof(Time).Access("unscaledDeltaTime"), ArithmeticType.Multiply);
			} else {
				if(input.isAssigned) {
					return CodeGenerator.GenerateArithmetiCode(input.ParseValue(), typeof(Time).Access("deltaTime"), ArithmeticType.Multiply);
				}
				return CodeGenerator.GenerateArithmetiCode(1.ParseValue(), typeof(Time).Access("deltaTime"), ArithmeticType.Multiply);
			}
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.ClockIcon);
		}

		public override string GetNodeName() {
			return "Per Second";
		}
	}
}