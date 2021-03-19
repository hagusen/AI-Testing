using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Group", "NodeGroup")]
	public class NodeGroup : GroupNode {
		[Tooltip("If true, will reset variable to the original value when this group is called.")]
		public bool resetOnExecute = true;
		[HideInInspector]
		public List<VariableData> variable = new List<VariableData>();
		[Hide, FieldConnection("", true)]
		public MemberData onFinished = new MemberData();

		public override void OnExecute() {
			InitializeVariable();
			if(IsCoroutine()) {
				owner.StartCoroutine(OnUpdate(), this);
			} else {
				JumpStatement js = nodeToExecute.ActivateAndFindJumpState();
				if(!nodeToExecute.IsFinished()) {
					throw new System.Exception($"Start node in group: {GetNodeName()} is not coroutine but it is not finished.");
				}
				if(js != null) {
					jumpState = js;
				}
				Finish(onFinished);
			}
		}

		IEnumerator OnUpdate() {
			JumpStatement js = nodeToExecute.ActivateAndFindJumpState();
			if(!nodeToExecute.IsFinished()) {
				yield return nodeToExecute.WaitUntilFinish();
			}
			if(js != null) {
				jumpState = js;
			}
			Finish(onFinished);
		}

		void InitializeVariable() {
			if(!resetOnExecute && _var != null)
				return;
			if(_var == null || _var.Count != variable.Count) {
				_var = new VariableData[variable.Count];
			}
			for(int i = 0; i < variable.Count; i++) {
				if(_var[i] == null) {
					_var[i] = new VariableData(variable[i]);
				} else {
					_var[i].CopyFrom(variable[i]);
				}
			}
		}

		public override List<VariableData> Variables {
			get {
				return variable;
			}
		}

		public override string GenerateCode() {
			if(nodeToExecute == null)
				return null;
			string gData = null;
			if(!CodeGenerator.NeedInstanceVariable(this) && resetOnExecute) {
				foreach(VariableData Item in variable) {
					CodeGenerator.AddVariable(Item, false);
					gData += CodeGenerator.GenerateVariableDeclaration(Item, false, true);
				}
			} else {
				foreach(VariableData Item in variable) {
					CodeGenerator.AddVariable(Item);
				}
			}
			return CodeGenerator.GenerateFlowStatement(
				gData,
				CodeGenerator.GenerateFlowCode(nodeToExecute, this),
				CodeGenerator.GetFinishCode(this, true, false, false, onFinished)
			);
		}

		public override string GetNodeName() {
			return gameObject.name;
		}
	}
}
