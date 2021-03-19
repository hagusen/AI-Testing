using UnityEngine;

namespace MaxyGames.uNode {
	/// <summary>
	/// Base class of all event caller class.
	/// </summary>
	public abstract class EventCaller : MonoBehaviour {
		[System.NonSerialized]
		public IGraphWithUnityEvent owner;
		[System.NonSerialized]
		public System.Action<object> action;

		void Awake() {
			hideFlags = HideFlags.HideInInspector;
		}

		void OnValidate() {
			if(!Application.isPlaying) {
				DestroyImmediate(this);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnDestroyCaller : EventCaller {
		void OnDestroy() {
			try {
				if(owner as Object != null && owner.onDestroy != null) {
					owner.onDestroy();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class FixedUpdateCaller : EventCaller {
		void FixedUpdate() {
			try {
				if(owner as Object != null && owner.onFixedUpdate != null) {
					owner.onFixedUpdate();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class LateUpdateCaller : EventCaller {
		void LateUpdate() {
			try {
				if(owner as Object != null && owner.onLateUpdate != null) {
					owner.onLateUpdate();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnAnimatorIKCaller : EventCaller {
		void OnAnimatorIK(int layerIndex) {
			try {
				if(owner as Object != null && owner.onAnimatorIK != null) {
					owner.onAnimatorIK(layerIndex);
				}
				if(action != null) {
					action(layerIndex);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnAnimatorMoveCaller : EventCaller {
		void OnAnimatorMove() {
			try {
				if(owner as Object != null && owner.onAnimatorMove != null) {
					owner.onAnimatorMove();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnApplicationFocusCaller : EventCaller {
		void OnApplicationFocus(bool focus) {
			try {
				if(owner as Object != null && owner.onApplicationFocus != null) {
					owner.onApplicationFocus(focus);
				}
				if(action != null) {
					action(focus);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnApplicationPauseCaller : EventCaller {
		void OnApplicationPause(bool pauseStatus) {
			try {
				if(owner as Object != null && owner.onApplicationPause != null) {
					owner.onApplicationPause(pauseStatus);
				}
				if(action != null) {
					action(pauseStatus);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnApplicationQuitCaller : EventCaller {
		void OnApplicationQuit() {
			try {
				if(owner as Object != null && owner.onApplicationQuit != null) {
					owner.onApplicationQuit();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnBecameInvisibleCaller : EventCaller {
		void OnBecameInvisible() {
			try {
				if(owner as Object != null && owner.onBecameInvisible != null) {
					owner.onBecameInvisible();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnBecameVisibleCaller : EventCaller {
		void OnBecameVisible() {
			try {
				if(owner as Object != null && owner.onBecameVisible != null) {
					owner.onBecameVisible();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnCollisionEnter2DCaller : EventCaller {
		void OnCollisionEnter2D(Collision2D col) {
			try {
				if(owner as Object != null && owner.onCollisionEnter2D != null) {
					owner.onCollisionEnter2D(col);
				}
				if(action != null) {
					action(col);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnCollisionEnterCaller : EventCaller {
		void OnCollisionEnter(Collision col) {
			try {
				if(owner as Object != null && owner.onCollisionEnter != null) {
					owner.onCollisionEnter(col);
				}
				if(action != null) {
					action(col);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnCollisionExit2DCaller : EventCaller {
		void OnCollisionExit2D(Collision2D col) {
			try {
				if(owner as Object != null && owner.onCollisionExit2D != null) {
					owner.onCollisionExit2D(col);
				}
				if(action != null) {
					action(col);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnCollisionExitCaller : EventCaller {
		void OnCollisionExit(Collision col) {
			try {
				if(owner as Object != null && owner.onCollisionExit != null) {
					owner.onCollisionExit(col);
				}
				if(action != null) {
					action(col);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnCollisionStay2DCaller : EventCaller {
		void OnCollisionStay2D(Collision2D col) {
			try {
				if(owner as Object != null && owner.onCollisionStay2D != null) {
					owner.onCollisionStay2D(col);
				}
				if(action != null) {
					action(col);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnCollisionStayCaller : EventCaller {
		void OnCollisionStay(Collision col) {
			try {
				if(owner as Object != null && owner.onCollisionStay != null) {
					owner.onCollisionStay(col);
				}
				if(action != null) {
					action(col);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnGUICaller : EventCaller {
		void OnGUI() {
			try {
				if(owner as Object != null && owner.onGUI != null) {
					owner.onGUI();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnMouseEnterCaller : EventCaller {
		void OnMouseEnter() {
			try {
				if(owner as Object != null && owner.onMouseEnter != null) {
					owner.onMouseEnter();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnMouseDownCaller : EventCaller {
		void OnMouseDown() {
			try {
				if(owner as Object != null && owner.onMouseDown != null) {
					owner.onMouseDown();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnMouseDragCaller : EventCaller {
		void OnMouseDrag() {
			try {
				if(owner as Object != null && owner.onMouseDrag != null) {
					owner.onMouseDrag();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnMouseExitCaller : EventCaller {
		void OnMouseExit() {
			try {
				if(owner as Object != null && owner.onMouseExit != null) {
					owner.onMouseExit();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnMouseOverCaller : EventCaller {
		void OnMouseOver() {
			try {
				if(owner as Object != null && owner.onMouseOver != null) {
					owner.onMouseOver();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnMouseUpAsButtonCaller : EventCaller {
		void OnMouseUpAsButton() {
			try {
				if(owner as Object != null && owner.onMouseUpAsButton != null) {
					owner.onMouseUpAsButton();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnMouseUpCaller : EventCaller {
		void OnMouseUp() {
			try {
				if(owner as Object != null && owner.onMouseUp != null) {
					owner.onMouseUp();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnPostRenderCaller : EventCaller {
		void OnPostRender() {
			try {
				if(owner as Object != null && owner.onPostRender != null) {
					owner.onPostRender();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnPreCullCaller : EventCaller {
		void OnPreCull() {
			try {
				if(owner as Object != null && owner.onPreCull != null) {
					owner.onPreCull();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnPreRenderCaller : EventCaller {
		void OnPreRender() {
			try {
				if(owner as Object != null && owner.onPreRender != null) {
					owner.onPreRender();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnRenderObjectCaller : EventCaller {
		void OnRenderObject() {
			try {
				if(owner as Object != null && owner.onRenderObject != null) {
					owner.onRenderObject();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnTransformChildrenChangedCaller : EventCaller {
		void OnTransformChildrenChanged() {
			try {
				if(owner as Object != null && owner.onTransformChildrenChanged != null) {
					owner.onTransformChildrenChanged();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnTransformParentChangedCaller : EventCaller {
		void OnTransformParentChanged() {
			try {
				if(owner as Object != null && owner.onTransformParentChanged != null) {
					owner.onTransformParentChanged();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnTriggerEnter2DCaller : EventCaller {
		void OnTriggerEnter2D(Collider2D col) {
			try {
				if(owner as Object != null && owner.onTriggerEnter2D != null) {
					owner.onTriggerEnter2D(col);
				}
				if(action != null) {
					action(col);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnTriggerEnterCaller : EventCaller {
		void OnTriggerEnter(Collider col) {
			try {
				if(owner as Object != null && owner.onTriggerEnter != null) {
					owner.onTriggerEnter(col);
				}
				if(action != null) {
					action(col);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnTriggerExit2DCaller : EventCaller {
		void OnTriggerExit2D(Collider2D col) {
			try {
				if(owner as Object != null && owner.onTriggerExit2D != null) {
					owner.onTriggerExit2D(col);
				}
				if(action != null) {
					action(col);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnTriggerExitCaller : EventCaller {
		void OnTriggerExit(Collider col) {
			try {
				if(owner as Object != null && owner.onTriggerExit != null) {
					owner.onTriggerExit(col);
				}
				if(action != null) {
					action(col);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnTriggerStay2DCaller : EventCaller {
		void OnTriggerStay2D(Collider2D col) {
			try {
				if(owner as Object != null && owner.onTriggerStay2D != null) {
					owner.onTriggerStay2D(col);
				}
				if(action != null) {
					action(col);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnTriggerStayCaller : EventCaller {
		void OnTriggerStay(Collider col) {
			try {
				if(owner as Object != null && owner.onTriggerStay != null) {
					owner.onTriggerStay(col);
				}
				if(action != null) {
					action(col);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class OnWillRenderObjectCaller : EventCaller {
		void OnWillRenderObject() {
			try {
				if(owner as Object != null && owner.onWillRenderObject != null) {
					owner.onWillRenderObject();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}

	[AddComponentMenu("")]
	public class UpdateCaller : EventCaller {
		void Update() {
			try {
				if(owner as Object != null && owner.onUpdate != null) {
					owner.onUpdate();
				}
				if(action != null) {
					action(null);
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, owner as Object);
			}
		}
	}
}