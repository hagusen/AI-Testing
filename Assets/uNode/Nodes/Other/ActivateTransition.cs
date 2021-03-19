using UnityEngine;
using System.Collections.Generic;
using System;

namespace MaxyGames.uNode.Nodes {
	public class ActivateTransition : Node {
		public string transitionName = "Custom";

		public override void OnExecute() {
			StateNode stateNode = uNodeHelper.GetComponentInParent<StateNode>(this);
			var tr = stateNode.TransitionEvents;
			foreach(var t in tr) {
				if(t != null && t.Name.Equals(transitionName) && t is Transition.CustomTransition) {
					(t as Transition.CustomTransition).Execute();
					break;
				}
			}
			Finish();
		}

		public override string GetNodeName() {
			if(string.IsNullOrEmpty(transitionName)) {
				return "Assign this";
			}
			return "Activate: " + transitionName;
		}

		public override Type GetNodeIcon() {
			return null;
		}

		public override string GenerateCode() {
			return CodeGenerator.GetInvokeCode(
				"_ActivateTransition", 
				new object[] {
					CodeGenerator.ParseValue(transitionName + uNodeHelper.GetComponentInParent<StateNode>(this).GetInstanceID())
				});
		}
	}
}
