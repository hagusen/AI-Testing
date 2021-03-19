using UnityEngine;
using UnityEngine.AI;

namespace MaxyGames.Runtime {
	/// <summary>
	/// A Utility for generated code to be more simple as possible.
	/// </summary>
	public static class RuntimeUtility {
		public static T DebugValue<T>(object instance, T value, int objectUID, int nodeUID, int valueID, bool isSet = false) {
#if UNITY_EDITOR
			uNodeDEBUG.InvokeValueNode(instance, objectUID, nodeUID, valueID, value, isSet);
#endif
			return value;
		}

		public static void DebugFlow(object instance, int objectUID, int nodeUID, bool? state) {
#if UNITY_EDITOR
			uNodeDEBUG.InvokeEventNode(instance, objectUID, nodeUID, state);
#endif
		}

		public static Vector3 RandomNavSphere(Vector3 origin, float distance, int layermask = -1) {
			Vector3 randomDirection = Random.insideUnitSphere * distance;
			randomDirection += origin;
			NavMeshHit navHit;
			NavMesh.SamplePosition(randomDirection, out navHit, distance, layermask);
			return navHit.position;
		}

		public static Vector3 RandomNavSphere(Vector3 origin, float minDistance, float maxDistance, int layermask = -1) {
			float distance = Random.Range(minDistance, maxDistance);
			Vector3 randomDirection = Random.insideUnitSphere * distance;
			randomDirection += origin;
			NavMeshHit navHit;
			NavMesh.SamplePosition(randomDirection, out navHit, distance, layermask);
			return navHit.position;
		}

		public static RuntimeEvent GetRuntimeEvent(Component component) {
			if(component == null)
				throw new System.ArgumentNullException("component");
			RuntimeEvent runtime = component.GetComponent<RuntimeEvent>();
			if(runtime == null) {
				runtime = component.gameObject.AddComponent<RuntimeEvent>();
			}
			return runtime;
		}

		public static RuntimeEvent GetRuntimeEvent(GameObject gameObject) {
			if(gameObject == null)
				throw new System.ArgumentNullException("gameObject");
			RuntimeEvent runtime = gameObject.GetComponent<RuntimeEvent>();
			if(runtime == null) {
				runtime = gameObject.AddComponent<RuntimeEvent>();
			}
			return runtime;
		}

		public static void AddEvent(GameObject gameObject, string eventCode, System.Action action) {
			AddEvent(gameObject, eventCode, p => action());
		}

		public static void AddEvent(GameObject gameObject, string eventCode, System.Action<object> action) {
			switch(eventCode) {
				case "Awake":
					GetRuntimeEvent(gameObject).onAwake += () => action(null);
					break;
				case "Start":
					GetRuntimeEvent(gameObject).onStart += () => action(null);
					break;
				case "Update":
					GetRuntimeEvent(gameObject).onUpdate += () => action(null);
					break;
				case "FixedUpdate":
					GetRuntimeEvent(gameObject).onFixedUpdate += () => action(null);
					break;
				case "LateUpdate":
					GetRuntimeEvent(gameObject).onLateUpdate += () => action(null);
					break;
				case "OnAnimatorIK":
					GetRuntimeEvent(gameObject).onAnimatorIK += (p) => action(p);
					break;
				case "OnAnimatorMove":
					GetRuntimeEvent(gameObject).onAnimatorMove += () => action(null);
					break;
				case "OnApplicationFocus":
					GetRuntimeEvent(gameObject).onApplicationFocus += (p) => action(p);
					break;
				case "OnApplicationPause":
					GetRuntimeEvent(gameObject).onApplicationPause += (p) => action(p);
					break;
				case "OnApplicationQuit":
					GetRuntimeEvent(gameObject).onApplicationQuit += () => action(null);
					break;
				case "OnBecameInvisible":
					GetRuntimeEvent(gameObject).onBecameInvisible += () => action(null);
					break;
				case "OnBecameVisible":
					GetRuntimeEvent(gameObject).onBecameVisible += () => action(null);
					break;
				case "OnCollisionEnter":
					GetRuntimeEvent(gameObject).onCollisionEnter += (p) => action(p);
					break;
				case "OnCollisionEnter2D":
					GetRuntimeEvent(gameObject).onCollisionEnter2D += (p) => action(p);
					break;
				case "OnCollisionExit":
					GetRuntimeEvent(gameObject).onCollisionExit += (p) => action(p);
					break;
				case "OnCollisionExit2D":
					GetRuntimeEvent(gameObject).onCollisionExit2D += (p) => action(p);
					break;
				case "OnCollisionStay":
					GetRuntimeEvent(gameObject).onCollisionStay += (p) => action(p);
					break;
				case "OnCollisionStay2D":
					GetRuntimeEvent(gameObject).onCollisionStay2D += (p) => action(p);
					break;
				case "OnDestroy":
					GetRuntimeEvent(gameObject).onDestroy += () => action(null);
					break;
				case "OnDisable":
					GetRuntimeEvent(gameObject).onDisable += () => action(null);
					break;
				case "OnEnable":
					GetRuntimeEvent(gameObject).onEnable += () => action(null);
					break;
				case "OnGUI":
					GetRuntimeEvent(gameObject).onGUI += () => action(null);
					break;
				case "OnMouseDown":
					GetRuntimeEvent(gameObject).onMouseDown += () => action(null);
					break;
				case "OnMouseDrag":
					GetRuntimeEvent(gameObject).onMouseDrag += () => action(null);
					break;
				case "OnMouseEnter":
					GetRuntimeEvent(gameObject).onMouseEnter += () => action(null);
					break;
				case "OnMouseExit":
					GetRuntimeEvent(gameObject).onMouseExit += () => action(null);
					break;
				case "OnMouseOver":
					GetRuntimeEvent(gameObject).onMouseOver += () => action(null);
					break;
				case "OnMouseUp":
					GetRuntimeEvent(gameObject).onMouseUp += () => action(null);
					break;
				case "OnMouseUpAsButton":
					GetRuntimeEvent(gameObject).onMouseUpAsButton += () => action(null);
					break;
				case "OnPostRender":
					GetRuntimeEvent(gameObject).onPostRender += () => action(null);
					break;
				case "OnPreCull":
					GetRuntimeEvent(gameObject).onPreCull += () => action(null);
					break;
				case "OnPreRender":
					GetRuntimeEvent(gameObject).onPreRender += () => action(null);
					break;
				case "OnRenderObject":
					GetRuntimeEvent(gameObject).onRenderObject += () => action(null);
					break;
				case "OnTransformChildrenChanged":
					GetRuntimeEvent(gameObject).onTransformChildrenChanged += () => action(null);
					break;
				case "OnTransformParentChanged":
					GetRuntimeEvent(gameObject).onTransformParentChanged += () => action(null);
					break;
				case "OnTriggerEnter":
					GetRuntimeEvent(gameObject).onTriggerEnter += (p) => action(p);
					break;
				case "OnTriggerEnter2D":
					GetRuntimeEvent(gameObject).onTriggerEnter2D += (p) => action(p);
					break;
				case "OnTriggerExit":
					GetRuntimeEvent(gameObject).onTriggerExit += (p) => action(p);
					break;
				case "OnTriggerExit2D":
					GetRuntimeEvent(gameObject).onTriggerExit2D += (p) => action(p);
					break;
				case "OnTriggerStay":
					GetRuntimeEvent(gameObject).onTriggerStay += (p) => action(p);
					break;
				case "OnTriggerStay2D":
					GetRuntimeEvent(gameObject).onTriggerStay2D += (p) => action(p);
					break;
				case "OnWillRenderObject":
					GetRuntimeEvent(gameObject).onWillRenderObject += () => action(null);
					break;
				default:
					throw new System.ArgumentException("Invalid event code", "eventCode");
			}
		}
	}

	public class RuntimeEvent : MonoBehaviour {
		public event System.Action onAwake;
		public event System.Action onStart;
		public event System.Action onUpdate;
		public event System.Action onFixedUpdate;
		public event System.Action onLateUpdate;
		public event System.Action<int> onAnimatorIK;
		public event System.Action onAnimatorMove;
		public event System.Action<bool> onApplicationFocus;
		public event System.Action<bool> onApplicationPause;
		public event System.Action onApplicationQuit;
		public event System.Action onBecameInvisible;
		public event System.Action onBecameVisible;
		public event System.Action<Collision> onCollisionEnter;
		public event System.Action<Collision2D> onCollisionEnter2D;
		public event System.Action<Collision> onCollisionExit;
		public event System.Action<Collision2D> onCollisionExit2D;
		public event System.Action<Collision> onCollisionStay;
		public event System.Action<Collision2D> onCollisionStay2D;
		public event System.Action onDestroy;
		public event System.Action onDisable;
		public event System.Action onEnable;
		public event System.Action onGUI;
		public event System.Action onMouseDown;
		public event System.Action onMouseDrag;
		public event System.Action onMouseEnter;
		public event System.Action onMouseExit;
		public event System.Action onMouseOver;
		public event System.Action onMouseUp;
		public event System.Action onMouseUpAsButton;
		public event System.Action onPostRender;
		public event System.Action onPreCull;
		public event System.Action onPreRender;
		public event System.Action onRenderObject;
		public event System.Action onTransformChildrenChanged;
		public event System.Action onTransformParentChanged;
		public event System.Action<Collider> onTriggerEnter;
		public event System.Action<Collider2D> onTriggerEnter2D;
		public event System.Action<Collider> onTriggerExit;
		public event System.Action<Collider2D> onTriggerExit2D;
		public event System.Action<Collider> onTriggerStay;
		public event System.Action<Collider2D> onTriggerStay2D;
		public event System.Action onWillRenderObject;

		private void Awake() {
			if(onAwake != null)
				onAwake();
		}

		private void Start() {
			if(onStart != null)
				onStart();
		}

		private void Update() {
			if(onUpdate != null)
				onUpdate();
		}
		private void FixedUpdate() {
			if(onFixedUpdate != null)
				onFixedUpdate();
		}

		private void LateUpdate() {
			if(onLateUpdate != null)
				onLateUpdate();
		}

		private void OnAnimatorIK(int layerIndex) {
			if(onAnimatorIK != null)
				onAnimatorIK(layerIndex);
		}

		private void OnAnimatorMove() {
			if(onAnimatorMove != null)
				onAnimatorMove();
		}

		private void OnApplicationFocus(bool focus) {
			if(onApplicationFocus != null)
				onApplicationFocus(focus);
		}

		private void OnApplicationPause(bool pause) {
			if(onApplicationPause != null)
				onApplicationPause(pause);
		}

		private void OnApplicationQuit() {
			if(onApplicationQuit != null)
				onApplicationQuit();
		}

		private void OnBecameInvisible() {
			if(onBecameInvisible != null)
				onBecameInvisible();
		}

		private void OnBecameVisible() {
			if(onBecameVisible != null)
				onBecameVisible();
		}

		private void OnCollisionEnter(Collision collision) {
			if(onCollisionEnter != null)
				onCollisionEnter(collision);
		}

		private void OnCollisionEnter2D(Collision2D collision) {
			if(onCollisionEnter2D != null)
				onCollisionEnter2D(collision);
		}

		private void OnCollisionExit(Collision collision) {
			if(onCollisionExit != null)
				onCollisionExit(collision);
		}

		private void OnCollisionExit2D(Collision2D collision) {
			if(onCollisionExit2D != null)
				onCollisionExit2D(collision);
		}

		private void OnCollisionStay(Collision collision) {
			if(onCollisionStay != null)
				onCollisionStay(collision);
		}

		private void OnCollisionStay2D(Collision2D collision) {
			if(onCollisionStay2D != null)
				onCollisionStay2D(collision);
		}

		private void OnDestroy() {
			if(onDestroy != null)
				onDestroy();
		}

		private void OnDisable() {
			if(onDisable != null)
				onDisable();
		}

		private void OnEnable() {
			if(onEnable != null)
				onEnable();
		}

		private void OnGUI() {
			if(onGUI != null)
				onGUI();
		}

		private void OnMouseDown() {
			if(onMouseDown != null)
				onMouseDown();
		}

		private void OnMouseDrag() {
			if(onMouseDrag != null)
				onMouseDrag();
		}

		private void OnMouseEnter() {
			if(onMouseEnter != null)
				onMouseEnter();
		}

		private void OnMouseExit() {
			if(onMouseExit != null)
				onMouseExit();
		}

		private void OnMouseOver() {
			if(onMouseOver != null)
				onMouseOver();
		}

		private void OnMouseUp() {
			if(onMouseUp != null)
				onMouseUp();
		}

		private void OnMouseUpAsButton() {
			if(onMouseUpAsButton != null)
				onMouseUpAsButton();
		}

		private void OnPostRender() {
			if(onPostRender != null)
				onPostRender();
		}

		private void OnPreCull() {
			if(onPreCull != null)
				onPreCull();
		}

		private void OnPreRender() {
			if(onPreRender != null)
				onPreRender();
		}

		private void OnRenderObject() {
			if(onRenderObject != null)
				onRenderObject();
		}

		private void OnTransformChildrenChanged() {
			if(onTransformChildrenChanged != null)
				onTransformChildrenChanged();
		}

		private void OnTransformParentChanged() {
			if(onTransformParentChanged != null)
				onTransformParentChanged();
		}

		private void OnTriggerEnter(Collider other) {
			if(onTriggerEnter != null)
				onTriggerEnter(other);
		}

		private void OnTriggerEnter2D(Collider2D collision) {
			if(onTriggerEnter2D != null)
				onTriggerEnter2D(collision);
		}

		private void OnTriggerExit(Collider other) {
			if(onTriggerExit != null)
				onTriggerExit(other);
		}

		private void OnTriggerExit2D(Collider2D collision) {
			if(onTriggerExit2D != null)
				onTriggerExit2D(collision);
		}

		private void OnTriggerStay(Collider other) {
			if(onTriggerStay != null)
				onTriggerStay(other);
		}

		private void OnTriggerStay2D(Collider2D collision) {
			if(onTriggerStay2D != null)
				onTriggerStay2D(collision);
		}

		private void OnWillRenderObject() {
			if(onWillRenderObject != null)
				onWillRenderObject();
		}
	}
}
