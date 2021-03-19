using System;
using UnityEngine;

namespace MaxyGames.uNode {
	/// <summary>
	/// EventNode for StateMachine
	/// </summary>
	public class StateEventNode : BaseEventNode {
		public enum EventType {
			OnEnter,
			OnExit,
			Update,
			FixedUpdate,
			LateUpdate,
			OnAnimatorIK,
			OnAnimatorMove,
			OnApplicationFocus,
			OnApplicationPause,
			OnApplicationQuit,
			OnBecameInvisible,
			OnBecameVisible,
			OnCollisionEnter,
			OnCollisionEnter2D,
			OnCollisionExit,
			OnCollisionExit2D,
			OnCollisionStay,
			OnCollisionStay2D,
			OnDestroy,
			OnDisable,
			OnEnable,
			OnGUI,
			OnMouseDown,
			OnMouseDrag,
			OnMouseEnter,
			OnMouseExit,
			OnMouseOver,
			OnMouseUp,
			OnMouseUpAsButton,
			OnPostRender,
			OnPreCull,
			OnPreRender,
			OnRenderObject,
			OnTransformChildrenChanged,
			OnTransformParentChanged,
			OnTriggerEnter,
			OnTriggerEnter2D,
			OnTriggerExit,
			OnTriggerExit2D,
			OnTriggerStay,
			OnTriggerStay2D,
			OnWillRenderObject,
		}
		public EventType eventType;
		[Hide]
		public MemberData storeParameter = new MemberData();

		[System.NonSerialized]
		private StateNode stateNode;

		public override void Initialize() {
			stateNode = uNodeHelper.GetComponentInParent<StateNode>(this);
			if(stateNode == null) {
				Debug.LogError("Parent StateNode not found.", this);
				return;
			}
			switch(eventType) {
				case EventType.OnEnter:
					stateNode.onEnter += Activate;
					break;
				case EventType.OnExit:
					stateNode.onExit += Activate;
					break;
				case EventType.Update:
					runtimeUNode.onUpdate += Activate;
					break;
				case EventType.FixedUpdate:
					runtimeUNode.onFixedUpdate += Activate;
					break;
				case EventType.LateUpdate:
					runtimeUNode.onLateUpdate += Activate;
					break;
				case EventType.OnAnimatorIK:
					runtimeUNode.onAnimatorIK += delegate (int obj) {
						if(storeParameter.isAssigned) {
							storeParameter.Set(obj);
						}
						Activate();
					};
					break;
				case EventType.OnAnimatorMove:
					runtimeUNode.onAnimatorMove += Activate;
					break;
				case EventType.OnApplicationFocus:
					runtimeUNode.onApplicationFocus += delegate (bool obj) {
						if(storeParameter.isAssigned) {
							storeParameter.Set(obj);
						}
						Activate();
					};
					break;
				case EventType.OnApplicationPause:
					runtimeUNode.onApplicationPause += delegate (bool obj) {
						if(storeParameter.isAssigned) {
							storeParameter.Set(obj);
						}
						Activate();
					};
					break;
				case EventType.OnApplicationQuit:
					runtimeUNode.onApplicationQuit += Activate;
					break;
				case EventType.OnBecameInvisible:
					runtimeUNode.onBecameInvisible += Activate;
					break;
				case EventType.OnBecameVisible:
					runtimeUNode.onBecameVisible += Activate;
					break;
				case EventType.OnCollisionEnter:
					runtimeUNode.onCollisionEnter += delegate (Collision col) {
						if(storeParameter.isAssigned) {
							storeParameter.Set(col);
						}
						Activate();
					};
					break;
				case EventType.OnCollisionEnter2D:
					runtimeUNode.onCollisionEnter2D += delegate (Collision2D col) {
						if(storeParameter.isAssigned) {
							storeParameter.Set(col);
						}
						Activate();
					};
					break;
				case EventType.OnCollisionExit:
					runtimeUNode.onCollisionExit += delegate (Collision col) {
						if(storeParameter.isAssigned) {
							storeParameter.Set(col);
						}
						Activate();
					};
					break;
				case EventType.OnCollisionExit2D:
					runtimeUNode.onCollisionExit2D += delegate (Collision2D col) {
						if(storeParameter.isAssigned) {
							storeParameter.Set(col);
						}
						Activate();
					};
					break;
				case EventType.OnCollisionStay:
					runtimeUNode.onCollisionStay += delegate (Collision col) {
						if(storeParameter.isAssigned) {
							storeParameter.Set(col);
						}
						Activate();
					};
					break;
				case EventType.OnCollisionStay2D:
					runtimeUNode.onCollisionStay2D += delegate (Collision2D col) {
						if(storeParameter.isAssigned) {
							storeParameter.Set(col);
						}
						Activate();
					};
					break;
				case EventType.OnDestroy:
					runtimeUNode.onDestroy += Activate;
					break;
				case EventType.OnDisable:
					runtimeUNode.onDisable += Activate;
					break;
				case EventType.OnEnable:
					runtimeUNode.onEnable += Activate;
					break;
				case EventType.OnGUI:
					runtimeUNode.onGUI += Activate;
					break;
				case EventType.OnMouseDown:
					runtimeUNode.onMouseDown += Activate;
					break;
				case EventType.OnMouseDrag:
					runtimeUNode.onMouseDrag += Activate;
					break;
				case EventType.OnMouseEnter:
					runtimeUNode.onMouseEnter += Activate;
					break;
				case EventType.OnMouseExit:
					runtimeUNode.onMouseExit += Activate;
					break;
				case EventType.OnMouseOver:
					runtimeUNode.onMouseOver += Activate;
					break;
				case EventType.OnMouseUp:
					runtimeUNode.onMouseUp += Activate;
					break;
				case EventType.OnMouseUpAsButton:
					runtimeUNode.onMouseUpAsButton += Activate;
					break;
				case EventType.OnPostRender:
					runtimeUNode.onPostRender += Activate;
					break;
				case EventType.OnPreCull:
					runtimeUNode.onPreCull += Activate;
					break;
				case EventType.OnPreRender:
					runtimeUNode.onPreRender += Activate;
					break;
				case EventType.OnRenderObject:
					runtimeUNode.onRenderObject += Activate;
					break;
				case EventType.OnTransformChildrenChanged:
					runtimeUNode.onTransformChildrenChanged += Activate;
					break;
				case EventType.OnTransformParentChanged:
					runtimeUNode.onTransformParentChanged += Activate;
					break;
				case EventType.OnTriggerEnter:
					runtimeUNode.onTriggerEnter += delegate (Collider col) {
						if(storeParameter.isAssigned) {
							storeParameter.Set(col);
						}
						Activate();
					};
					break;
				case EventType.OnTriggerEnter2D:
					runtimeUNode.onTriggerEnter2D += delegate (Collider2D col) {
						if(storeParameter.isAssigned) {
							storeParameter.Set(col);
						}
						Activate();
					};
					break;
				case EventType.OnTriggerExit:
					runtimeUNode.onTriggerExit += delegate (Collider col) {
						if(storeParameter.isAssigned) {
							storeParameter.Set(col);
						}
						Activate();
					};
					break;
				case EventType.OnTriggerExit2D:
					runtimeUNode.onTriggerExit2D += delegate (Collider2D col) {
						if(storeParameter.isAssigned) {
							storeParameter.Set(col);
						}
						Activate();
					};
					break;
				case EventType.OnTriggerStay:
					runtimeUNode.onTriggerStay += delegate (Collider col) {
						if(storeParameter.isAssigned) {
							storeParameter.Set(col);
						}
						Activate();
					};
					break;
				case EventType.OnTriggerStay2D:
					runtimeUNode.onTriggerStay2D += delegate (Collider2D col) {
						if(storeParameter.isAssigned) {
							storeParameter.Set(col);
						}
						Activate();
					};
					break;
				case EventType.OnWillRenderObject:
					runtimeUNode.onWillRenderObject += Activate;
					break;
			}
		}

		public void Activate() {
			if(stateNode.currentState == StateType.Running) {
				if(uNodeUtility.isInEditor && uNodeUtility.useDebug) {
					uNodeUtility.InvokeNode(owner, owner.GetInstanceID(), this.GetInstanceID(), true);
				}
				var nodes = GetFlows();
				int nodeCount = nodes.Count;
				if(nodeCount > 0) {
					for(int x = 0; x < nodeCount; x++) {
						if(nodes[x] != null) {
							nodes[x].InvokeFlow();
						}
					}
				}
			}
		}

		public override void RegisterPort() {
			if(CodeGenerator.isGenerating) {
				CodeGenerator.RegisterAsStateNode(this);
				foreach(var n in GetFlows()) {
					var node = n?.GetTargetNode();
					if(node != null) {
						CodeGenerator.RegisterAsStateNode(node);
					}
				}
			}
		}

		public override Type GetNodeIcon() {
			switch(eventType) {
				case EventType.OnMouseDown:
				case EventType.OnMouseDrag:
				case EventType.OnMouseEnter:
				case EventType.OnMouseExit:
				case EventType.OnMouseOver:
				case EventType.OnMouseUp:
				case EventType.OnMouseUpAsButton:
					return typeof(TypeIcons.MouseIcon);
				case EventType.OnCollisionEnter:
				case EventType.OnCollisionExit:
				case EventType.OnCollisionStay:
				case EventType.OnTriggerEnter:
				case EventType.OnTriggerExit:
				case EventType.OnTriggerStay:
					return typeof(BoxCollider);
				case EventType.OnCollisionEnter2D:
				case EventType.OnCollisionExit2D:
				case EventType.OnCollisionStay2D:
				case EventType.OnTriggerEnter2D:
				case EventType.OnTriggerExit2D:
				case EventType.OnTriggerStay2D:
					return typeof(BoxCollider2D);
			}
			return typeof(TypeIcons.EventIcon);
		}
	}
}