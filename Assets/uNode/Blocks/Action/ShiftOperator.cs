﻿using UnityEngine;

namespace MaxyGames.Events {
	[BlockMenu("★General", "ShiftOperator")]
	public class ShiftOperator : Action {
		public ShiftType operatorType = ShiftType.LeftShift;
		public MemberData targetA;
		[ObjectType(typeof(int))]
		public MemberData targetB = new MemberData(1);
		[Filter(SetMember = true)]
		public MemberData storeValue;

		protected override void OnExecute() {
			storeValue.Set(uNodeHelper.ShiftOperator(targetA.Get(), targetB.GetValue<int>(), operatorType));
		}

		public override string GenerateCode(Object obj) {
			if(targetA.isAssigned && targetB.isAssigned) {
				if(storeValue.isAssigned) {
					return CodeGenerator.GenerateSetCode(storeValue,
						CodeGenerator.GetOperatorCode(CodeGenerator.ParseValue((object)targetA),
						CodeGenerator.ParseValue((object)targetB), operatorType));
				}
			}
			throw new System.Exception("Target is unassigned");
		}
	}
}