using System;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.uNode {
	[NodeMenu("★General", "State", IsCoroutine = true, order = 1, HideOnFlow = true)]
	public class StateNode : Node, ISuperNode {
		[SerializeField, Hide]
		private GameObject transitionEventObject;

		public GameObject TransitionEventObject {
			get {
				return transitionEventObject;
			}
			set {
				this.transitionEventObject = value;
			}
		}

		public TransitionEvent[] GetTransitions() {
			if(transitionEventObject != null) {
				return transitionEventObject.GetComponents<TransitionEvent>();
			}
			return new TransitionEvent[0];
		}

		[System.NonSerialized]
		private TransitionEvent[] _TransitionEvents;
		public TransitionEvent[] TransitionEvents {
			get {
				if(_TransitionEvents == null) {
					_TransitionEvents = new TransitionEvent[0];
				}
				if(_TransitionEvents.Length == 0 && transitionEventObject != null) {
					_TransitionEvents = transitionEventObject.GetComponents<TransitionEvent>();
				}
				return _TransitionEvents;
			}
		}

		public IList<NodeComponent> nestedFlowNodes {
			get {
				List<NodeComponent> nodes = new List<NodeComponent>();
				foreach(Transform t in transform) {
					var comp = t.GetComponent<StateEventNode>();
					if(comp) {
						nodes.Add(comp);
					}
				}
				return nodes;
			}
		}

		public System.Action onEnter;
		public System.Action onExit;

		public override void OnExecute() {
			if(onEnter != null) {
				onEnter();
			}
			if(transitionEventObject != null && jumpState == null) {
				for(int i = 0; i < TransitionEvents.Length; i++) {
					TransitionEvent transition = TransitionEvents[i];
					if(transition) {
						transition.OnEnter();
					}
				}
			}
			owner.StartCoroutine(OnUpdate(), this);
		}

		private System.Collections.IEnumerator OnUpdate() {
			while(state == StateType.Running) {
				if(transitionEventObject != null && jumpState == null) {
					for(int i = 0; i < TransitionEvents.Length; i++) {
						TransitionEvent transition = TransitionEvents[i];
						if(transition) {
							transition.OnUpdate();
							if(state != StateType.Running) {
								yield break;
							}
						}
					}
				}
				yield return null;
			}
		}

		public override void Finish() {
			if(onExit != null) {
				onExit();
			}
			if(transitionEventObject != null) {
				for(int i = 0; i < TransitionEvents.Length; i++) {
					TransitionEvent transition = TransitionEvents[i];
					if(transition) {
						transition.OnExit();
					}
				}
			}
			if(state == StateType.Running) {
				state = StateType.Success;
			}
			base.Finish();
		}

		public override bool IsSelfCoroutine() {
			return true;
		}

		public override void RegisterPort() {
			base.RegisterPort();
			if(CodeGenerator.isGenerating) {
				//Register this node as state node.
				CodeGenerator.RegisterAsStateNode(this);
			}
		}

		public override string GenerateCode() {
			string result = null;
			string onEnter = null;
			string onUpdate = null;
			string onExit = null;
			var transitions = GetTransitions();
			if(transitions.Length > 0) {
				for(int i = 0; i < transitions.Length; i++) {
					TransitionEvent transition = transitions[i];
					if(transition) {
						onEnter += transition.GenerateOnEnterCode().Add("\n", !string.IsNullOrEmpty(onEnter));
						onUpdate += transition.GenerateOnUpdateCode().AddLineInFirst();
						onExit += transition.GenerateOnExitCode().AddLineInFirst();
					}
				}
			}
			foreach(Transform t in transform) {
				var comp = t.GetComponent<StateEventNode>();
				if(comp) {
					if(comp.eventType == StateEventNode.EventType.OnEnter) {
						onEnter += CodeGenerator.GenerateNode(comp).AddLineInFirst();
					} else if(comp.eventType == StateEventNode.EventType.OnExit) {
						onExit += CodeGenerator.GenerateNode(comp).AddLineInFirst();
					} else {
						CodeGenerator.generatorData.AddEventCoroutineData(comp, CodeGenerator.GenerateNode(comp));
					}
				}
			}
			result = onEnter;
			result += CodeGenerator.GenerateCondition("while", CodeGenerator.CompareNodeState(this, null), onUpdate.AddLineInEnd() + CodeGenerator.GetYieldReturn(null).AddLineInFirst()).AddLineInFirst();
			CodeGenerator.SetStopAction(this, onExit);
			return result;
		}

		public override string GetNodeName() {
			return gameObject.name;
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.StateIcon);
		}

		public bool AcceptCoroutine() {
			return false;
		}
	}
}