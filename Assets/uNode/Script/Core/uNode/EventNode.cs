using System;
using UnityEngine;

namespace MaxyGames.uNode {
	/// <summary>
	/// A event node to call another node.
	/// </summary>
	public class EventNode : BaseEventNode {
		public enum EventType {
			Custom,
			Awake,
			Start,
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
		[Hide("eventType", EventType.Custom)]
		[Hide("eventType", EventType.Awake)]
		[Hide("eventType", EventType.Start)]
		[Hide("eventType", EventType.Update)]
		[Hide("eventType", EventType.FixedUpdate)]
		[Hide("eventType", EventType.LateUpdate)]
		[Hide("eventType", EventType.OnAnimatorIK)]
		[Hide("eventType", EventType.OnDisable)]
		[Hide("eventType", EventType.OnEnable)]
		[Hide("eventType", EventType.OnApplicationFocus)]
		[Hide("eventType", EventType.OnApplicationPause)]
		[Hide("eventType", EventType.OnApplicationQuit)]
		[Hide("eventType", EventType.OnGUI)]
		[Hide("eventType", EventType.OnPostRender)]
		[Hide("eventType", EventType.OnPreCull)]
		[Hide("eventType", EventType.OnPreRender)]
		[Hide("eventType", EventType.OnRenderObject)]
		[Hide("eventType", EventType.OnWillRenderObject)]
		[Filter(typeof(GameObject))]
		public MemberData[] targetObjects = new MemberData[0];
		[Hide("eventType", EventType.Awake)]
		[Hide("eventType", EventType.Custom)]
		[Hide("eventType", EventType.Start)]
		[Hide("eventType", EventType.Update)]
		[Hide("eventType", EventType.FixedUpdate)]
		[Hide("eventType", EventType.LateUpdate)]
		[Hide("eventType", EventType.OnAnimatorMove)]
		[Hide("eventType", EventType.OnApplicationQuit)]
		[Hide("eventType", EventType.OnBecameInvisible)]
		[Hide("eventType", EventType.OnBecameVisible)]
		[Hide("eventType", EventType.OnDestroy)]
		[Hide("eventType", EventType.OnDisable)]
		[Hide("eventType", EventType.OnEnable)]
		[Hide("eventType", EventType.OnGUI)]
		[Hide("eventType", EventType.OnMouseDown)]
		[Hide("eventType", EventType.OnMouseDrag)]
		[Hide("eventType", EventType.OnMouseEnter)]
		[Hide("eventType", EventType.OnMouseExit)]
		[Hide("eventType", EventType.OnMouseOver)]
		[Hide("eventType", EventType.OnMouseUp)]
		[Hide("eventType", EventType.OnMouseUpAsButton)]
		[Hide("eventType", EventType.OnPreCull)]
		[Hide("eventType", EventType.OnPreRender)]
		[Hide("eventType", EventType.OnRenderObject)]
		[Hide("eventType", EventType.OnTransformChildrenChanged)]
		[Hide("eventType", EventType.OnTransformParentChanged)]
		[Hide("eventType", EventType.OnWillRenderObject)]
		[Filter(SetMember =true)]
		public MemberData storeValue = new MemberData();

		public override void Initialize() {
			if(targetObjects.Length == 0 || eventType == EventType.Awake || eventType == EventType.Custom || eventType == EventType.Start || eventType == EventType.OnEnable || eventType == EventType.OnDisable) {
				switch(eventType) {
					case EventType.Start:
						runtimeUNode.onStart += Activate;
						break;
					case EventType.Custom:
						(runtimeUNode as uNodeRuntime).customMethod.Add(Name, this);
						break;
					case EventType.Awake:
						runtimeUNode.onAwake += Activate;
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
							if(storeValue.isAssigned) {
								storeValue.Set(obj);
							}
							Activate();
						};
						break;
					case EventType.OnAnimatorMove:
						runtimeUNode.onAnimatorMove += Activate;
						break;
					case EventType.OnApplicationFocus:
						runtimeUNode.onApplicationFocus += delegate (bool obj) {
							if(storeValue.isAssigned) {
								storeValue.Set(obj);
							}
							Activate();
						};
						break;
					case EventType.OnApplicationPause:
						runtimeUNode.onApplicationPause += delegate (bool obj) {
							if(storeValue.isAssigned) {
								storeValue.Set(obj);
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
							if(storeValue.isAssigned) {
								storeValue.Set(col);
							}
							Activate();
						};
						break;
					case EventType.OnCollisionEnter2D:
						runtimeUNode.onCollisionEnter2D += delegate (Collision2D col) {
							if(storeValue.isAssigned) {
								storeValue.Set(col);
							}
							Activate();
						};
						break;
					case EventType.OnCollisionExit:
						runtimeUNode.onCollisionExit += delegate (Collision col) {
							if(storeValue.isAssigned) {
								storeValue.Set(col);
							}
							Activate();
						};
						break;
					case EventType.OnCollisionExit2D:
						runtimeUNode.onCollisionExit2D += delegate (Collision2D col) {
							if(storeValue.isAssigned) {
								storeValue.Set(col);
							}
							Activate();
						};
						break;
					case EventType.OnCollisionStay:
						runtimeUNode.onCollisionStay += delegate (Collision col) {
							if(storeValue.isAssigned) {
								storeValue.Set(col);
							}
							Activate();
						};
						break;
					case EventType.OnCollisionStay2D:
						runtimeUNode.onCollisionStay2D += delegate (Collision2D col) {
							if(storeValue.isAssigned) {
								storeValue.Set(col);
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
							if(storeValue.isAssigned) {
								storeValue.Set(col);
							}
							Activate();
						};
						break;
					case EventType.OnTriggerEnter2D:
						runtimeUNode.onTriggerEnter2D += delegate (Collider2D col) {
							if(storeValue.isAssigned) {
								storeValue.Set(col);
							}
							Activate();
						};
						break;
					case EventType.OnTriggerExit:
						runtimeUNode.onTriggerExit += delegate (Collider col) {
							if(storeValue.isAssigned) {
								storeValue.Set(col);
							}
							Activate();
						};
						break;
					case EventType.OnTriggerExit2D:
						runtimeUNode.onTriggerExit2D += delegate (Collider2D col) {
							if(storeValue.isAssigned) {
								storeValue.Set(col);
							}
							Activate();
						};
						break;
					case EventType.OnTriggerStay:
						runtimeUNode.onTriggerStay += delegate (Collider col) {
							if(storeValue.isAssigned) {
								storeValue.Set(col);
							}
							Activate();
						};
						break;
					case EventType.OnTriggerStay2D:
						runtimeUNode.onTriggerStay2D += delegate (Collider2D col) {
							if(storeValue.isAssigned) {
								storeValue.Set(col);
							}
							Activate();
						};
						break;
					case EventType.OnWillRenderObject:
						runtimeUNode.onWillRenderObject += Activate;
						break;
				}
			} else {
				switch(eventType) {
					case EventType.Update:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<UpdateCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.FixedUpdate:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<FixedUpdateCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.LateUpdate:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<LateUpdateCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnAnimatorIK:
						runtimeUNode.onAnimatorIK += delegate (int obj) {
							Activate();
						};
						break;
					case EventType.OnAnimatorMove:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnAnimatorMoveCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnApplicationFocus:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnApplicationFocusCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									if(storeValue.isAssigned) {
										storeValue.Set(obj);
									}
									Activate();
								});
							}
						}
						break;
					case EventType.OnApplicationPause:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnApplicationPauseCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									if(storeValue.isAssigned) {
										storeValue.Set(obj);
									}
									Activate();
								});
							}
						}
						break;
					case EventType.OnApplicationQuit:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnApplicationQuitCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnBecameInvisible:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnBecameInvisibleCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnBecameVisible:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnBecameVisibleCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnCollisionEnter:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnCollisionEnterCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									if(storeValue.isAssigned) {
										storeValue.Set(obj);
									}
									Activate();
								});
							}
						}
						break;
					case EventType.OnCollisionEnter2D:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnCollisionEnter2DCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									if(storeValue.isAssigned) {
										storeValue.Set(obj);
									}
									Activate();
								});
							}
						}
						break;
					case EventType.OnCollisionExit:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnCollisionExitCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									if(storeValue.isAssigned) {
										storeValue.Set(obj);
									}
									Activate();
								});
							}
						}
						break;
					case EventType.OnCollisionExit2D:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnCollisionExit2DCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									if(storeValue.isAssigned) {
										storeValue.Set(obj);
									}
									Activate();
								});
							}
						}
						break;
					case EventType.OnCollisionStay:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnCollisionStayCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									if(storeValue.isAssigned) {
										storeValue.Set(obj);
									}
									Activate();
								});
							}
						}
						break;
					case EventType.OnCollisionStay2D:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnCollisionStay2DCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									if(storeValue.isAssigned) {
										storeValue.Set(obj);
									}
									Activate();
								});
							}
						}
						break;
					case EventType.OnDestroy:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnDestroyCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnGUI:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnGUICaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnMouseDown:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnMouseDownCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnMouseDrag:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnMouseDragCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnMouseEnter:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnMouseEnterCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnMouseExit:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnMouseExitCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnMouseOver:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnMouseOverCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnMouseUp:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnMouseUpCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnMouseUpAsButton:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnMouseUpAsButtonCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnPostRender:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnPostRenderCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnPreCull:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnPreCullCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnPreRender:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnPreRenderCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnRenderObject:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnRenderObjectCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnTransformChildrenChanged:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnTransformChildrenChangedCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnTransformParentChanged:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnTransformParentChangedCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
					case EventType.OnTriggerEnter:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnTriggerEnterCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									if(storeValue.isAssigned) {
										storeValue.Set(obj);
									}
									Activate();
								});
							}
						}
						break;
					case EventType.OnTriggerEnter2D:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnTriggerEnter2DCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									if(storeValue.isAssigned) {
										storeValue.Set(obj);
									}
									Activate();
								});
							}
						}
						break;
					case EventType.OnTriggerExit:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnTriggerExitCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									if(storeValue.isAssigned) {
										storeValue.Set(obj);
									}
									Activate();
								});
							}
						}
						break;
					case EventType.OnTriggerExit2D:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnTriggerExit2DCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									if(storeValue.isAssigned) {
										storeValue.Set(obj);
									}
									Activate();
								});
							}
						}
						break;
					case EventType.OnTriggerStay:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnTriggerStayCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									if(storeValue.isAssigned) {
										storeValue.Set(obj);
									}
									Activate();
								});
							}
						}
						break;
					case EventType.OnTriggerStay2D:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnTriggerStay2DCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									if(storeValue.isAssigned) {
										storeValue.Set(obj);
									}
									Activate();
								});
							}
						}
						break;
					case EventType.OnWillRenderObject:
						for(int i = 0; i < targetObjects.Length; i++) {
							if(targetObjects[i].isAssigned) {
								uNodeHelper.AddEvent<OnWillRenderObjectCaller>(targetObjects[i].Get<GameObject>(), (obj) => {
									Activate();
								});
							}
						}
						break;
				}
			}
		}

		public void Activate() {
			if(uNodeUtility.isInEditor && uNodeUtility.useDebug) {
				uNodeUtility.InvokeNode(owner, owner.GetInstanceID(), this.GetInstanceID(), true);
			}
			var nodes = GetFlows();
			int nodeCount = nodes.Count;
			if(nodeCount > 0) {
				for(int x = 0; x < nodeCount; x++) {
					if(nodes[x] != null && nodes[x].isAssigned) {
						nodes[x].InvokeFlow();
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
	}
}