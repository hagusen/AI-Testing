using UnityEngine;
using UnityEngine.Profiling;
using System;
using System.Collections;
using System.Collections.Generic;
using MaxyGames.uNode;

namespace MaxyGames {
	/// <summary>
	/// Provides useful function.
	/// </summary>
	public static class uNodeHelper {
		/// <summary>
		/// Get the actual runtime object, if the target is uNodeSpawner then get the RuntimeBehaviour
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public static object GetActualRuntimeValue(object value) {
			if(value is IRuntimeClassContainer) {
				return (value as IRuntimeClassContainer).RuntimeClass;
			}
			return value;
		}

		/// <summary>
		/// Get UNode Graph Component
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="uniqueIdentifier"></param>
		/// <returns></returns>
		public static uNodeRoot GetGraphComponent(GameObject gameObject, string uniqueIdentifier) {
			var graphs = gameObject.GetComponents<uNodeRoot>();
			foreach(var graph in graphs) {
				if(graph.GraphName == uniqueIdentifier) {
					return graph;
				}
			}
			return null;
		}

		#region GetGeneratedComponent
		/// <summary>
		/// Get Generated Class Component
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="gameObject"></param>
		/// <returns></returns>
		public static T GetGeneratedComponent<T>(this GameObject gameObject) {
			var uniqueIdentifier = typeof(T).Name;
			if(typeof(T).IsInterface) {
				uniqueIdentifier = "i:" + uniqueIdentifier;
			}
			object comp = GetGeneratedComponent(gameObject, uniqueIdentifier);
			if(comp != null) {
				if(comp is T) {
					return (T)comp;
				} else if(comp is IRuntimeClassContainer) {
					var result = (comp as IRuntimeClassContainer).RuntimeClass;
					if(result is T) {
						return (T)result;
					}
				}
			}
			return default;
		}

		/// <summary>
		/// Get Generated Class Component
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="component"></param>
		/// <returns></returns>
		public static T GetGeneratedComponent<T>(this Component component) {
			var uniqueIdentifier = typeof(T).Name;
			if(typeof(T).IsInterface) {
				uniqueIdentifier = "i:" + uniqueIdentifier;
			}
			object comp = GetGeneratedComponent(component, uniqueIdentifier);
			if(comp != null) {
				if(comp is T) {
					return (T)comp;
				} else if(comp is IRuntimeClassContainer) {
					var result = (comp as IRuntimeClassContainer).RuntimeClass;
					if(result is T) {
						return (T)result;
					}
				}
			}
			return default;
		}

		/// <summary>
		/// Get Generated Class Component
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static RuntimeComponent GetGeneratedComponent(this GameObject gameObject, RuntimeType type) {
			var comps = gameObject.GetComponents<IRuntimeComponent>();
			foreach(var c in comps) {
				if(type.IsInstanceOfType(c)) {
					return c as RuntimeComponent;
				}
			}
			return null;
		}

		/// <summary>
		/// Get Generated Class Component
		/// </summary>
		/// <param name="component"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static RuntimeComponent GetGeneratedComponent(this Component component, RuntimeType type) {
			var comps = component.GetComponents<IRuntimeComponent>();
			foreach(var c in comps) {
				if(type.IsInstanceOfType(c)) {
					return c as RuntimeComponent;
				}
			}
			return null;
		}


		/// <summary>
		/// Get Generated Class Component
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="uniqueID"></param>
		/// <returns></returns>
		public static RuntimeComponent GetGeneratedComponent(this GameObject gameObject, string uniqueID) {
			var comps = gameObject.GetComponents<IRuntimeComponent>();
			if(uniqueID.StartsWith("i:")) {
				uniqueID = uniqueID.Remove(0, 2);
				foreach(var c in comps) {
					if(c.uniqueIdentifier == uniqueID) {
						return c as RuntimeComponent;
					}
					var ifaces = c.GetInterfaces();
					foreach(var iface in ifaces) {
						if(iface.Name == uniqueID) {
							return c as RuntimeComponent;
						}
					}
				}
				return null;
			}
			foreach(var c in comps) {
				if(c.uniqueIdentifier == uniqueID) {
					return c as RuntimeComponent;
				}
			}
			return null;
		}

		/// <summary>
		/// Get Generated Class Component
		/// </summary>
		/// <param name="component"></param>
		/// <param name="uniqueID"></param>
		/// <returns></returns>
		public static RuntimeComponent GetGeneratedComponent(this Component component, string uniqueID) {
			var comps = component.GetComponents<IRuntimeComponent>();
			if(uniqueID.StartsWith("i:")) {
				uniqueID = uniqueID.Remove(0, 2);
				foreach(var c in comps) {
					if(c.uniqueIdentifier == uniqueID) {
						return c as RuntimeComponent;
					}
					var ifaces = c.GetInterfaces();
					foreach(var iface in ifaces) {
						if(iface.Name == uniqueID) {
							return c as RuntimeComponent;
						}
					}
				}
				return null;
			}
			foreach(var c in comps) {
				if(c.uniqueIdentifier == uniqueID) {
					return c as RuntimeComponent;
				}
			}
			return null;
		}
		#endregion

		#region GetGeneratedComponentInChildren
		/// <summary>
		/// Get Generated Class Component in children
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="gameObject"></param>
		/// <returns></returns>
		public static T GetGeneratedComponentInChildren<T>(this GameObject gameObject, bool includeInactive = false) {
			var uniqueIdentifier = typeof(T).Name;
			if(typeof(T).IsInterface) {
				uniqueIdentifier = "i:" + uniqueIdentifier;
			}
			object comp = GetGeneratedComponentInChildren(gameObject, uniqueIdentifier, includeInactive);
			if(comp != null) {
				if(comp is T) {
					return (T)comp;
				} else if(comp is IRuntimeClassContainer) {
					var result = (comp as IRuntimeClassContainer).RuntimeClass;
					if(result is T) {
						return (T)result;
					}
				}
			}
			return default;
		}

		/// <summary>
		/// Get Generated Class Component in children
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="gameObject"></param>
		/// <returns></returns>
		public static T GetGeneratedComponentInChildren<T>(this Component component, bool includeInactive = false) {
			var uniqueIdentifier = typeof(T).Name;
			if(typeof(T).IsInterface) {
				uniqueIdentifier = "i:" + uniqueIdentifier;
			}
			object comp = GetGeneratedComponentInChildren(component, uniqueIdentifier, includeInactive);
			if(comp != null) {
				if(comp is T) {
					return (T)comp;
				} else if(comp is IRuntimeClassContainer) {
					var result = (comp as IRuntimeClassContainer).RuntimeClass;
					if(result is T) {
						return (T)result;
					}
				}
			}
			return default;
		}

		/// <summary>
		/// Get Generated Class Component in children
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="type"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static RuntimeComponent GetGeneratedComponentInChildren(this GameObject gameObject, RuntimeType type, bool includeInactive = false) {
			var comps = gameObject.GetComponentsInChildren<IRuntimeComponent>(includeInactive);
			foreach(var c in comps) {
				if(type.IsInstanceOfType(c)) {
					return c as RuntimeComponent;
				}
			}
			return null;
		}


		/// <summary>
		/// Get Generated Class Component in children
		/// </summary>
		/// <param name="component"></param>
		/// <param name="type"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static RuntimeComponent GetGeneratedComponentInChildren(this Component component, RuntimeType type, bool includeInactive = false) {
			var comps = component.GetComponentsInChildren<IRuntimeComponent>(includeInactive);
			foreach(var c in comps) {
				if(type.IsInstanceOfType(c)) {
					return c as RuntimeComponent;
				}
			}
			return null;
		}

		/// <summary>
		/// Get Generated Class Component in children
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="uniqueID"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static RuntimeComponent GetGeneratedComponentInChildren(this GameObject gameObject, string uniqueID, bool includeInactive = false) {
			var comps = gameObject.GetComponentsInChildren<IRuntimeComponent>(includeInactive);
			if(uniqueID.StartsWith("i:")) {
				uniqueID = uniqueID.Remove(0, 2);
				foreach(var c in comps) {
					if(c.uniqueIdentifier == uniqueID) {
						return c as RuntimeComponent;
					}
					var ifaces = c.GetInterfaces();
					foreach(var iface in ifaces) {
						if(iface.Name == uniqueID) {
							return c as RuntimeComponent;
						}
					}
				}
				return null;
			}
			foreach(var c in comps) {
				if(c.uniqueIdentifier == uniqueID) {
					return c as RuntimeComponent;
				}
			}
			return null;
		}

		/// <summary>
		/// Get Generated Class Component in children
		/// </summary>
		/// <param name="component"></param>
		/// <param name="uniqueID"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static RuntimeComponent GetGeneratedComponentInChildren(this Component component, string uniqueID, bool includeInactive = false) {
			var comps = component.GetComponentsInChildren<IRuntimeComponent>(includeInactive);
			if(uniqueID.StartsWith("i:")) {
				uniqueID = uniqueID.Remove(0, 2);
				foreach(var c in comps) {
					if(c.uniqueIdentifier == uniqueID) {
						return c as RuntimeComponent;
					}
					var ifaces = c.GetInterfaces();
					foreach(var iface in ifaces) {
						if(iface.Name == uniqueID) {
							return c as RuntimeComponent;
						}
					}
				}
				return null;
			}
			foreach(var c in comps) {
				if(c.uniqueIdentifier == uniqueID) {
					return c as RuntimeComponent;
				}
			}
			return null;
		}
		#endregion

		#region GetGeneratedComponentInParent
		/// <summary>
		/// Get Generated Class Component in parent
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="gameObject"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static T GetGeneratedComponentInParent<T>(this GameObject gameObject, bool includeInactive = false) {
			var uniqueIdentifier = typeof(T).Name;
			if(typeof(T).IsInterface) {
				uniqueIdentifier = "i:" + uniqueIdentifier;
			}
			object comp = GetGeneratedComponentInParent(gameObject, uniqueIdentifier, includeInactive);
			if(comp != null) {
				if(comp is T) {
					return (T)comp;
				} else if(comp is IRuntimeClassContainer) {
					var result = (comp as IRuntimeClassContainer).RuntimeClass;
					if(result is T) {
						return (T)result;
					}
				}
			}
			return default;
		}

		/// <summary>
		/// Get Generated Class Component in parent
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="component"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static T GetGeneratedComponentInParent<T>(this Component component, bool includeInactive = false) {
			var uniqueIdentifier = typeof(T).Name;
			if(typeof(T).IsInterface) {
				uniqueIdentifier = "i:" + uniqueIdentifier;
			}
			object comp = GetGeneratedComponentInParent(component, uniqueIdentifier, includeInactive);
			if(comp != null) {
				if(comp is T) {
					return (T)comp;
				} else if(comp is IRuntimeClassContainer) {
					var result = (comp as IRuntimeClassContainer).RuntimeClass;
					if(result is T) {
						return (T)result;
					}
				}
			}
			return default;
		}

		/// <summary>
		/// Get Generated Class Component in parent
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="type"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static RuntimeComponent GetGeneratedComponentInParent(this GameObject gameObject, RuntimeType type, bool includeInactive = false) {
			var comps = gameObject.GetComponentsInParent<IRuntimeComponent>(includeInactive);
			foreach(var c in comps) {
				if(type.IsInstanceOfType(c)) {
					return c as RuntimeComponent;
				}
			}
			return null;
		}

		/// <summary>
		/// Get Generated Class Component in parent
		/// </summary>
		/// <param name="component"></param>
		/// <param name="type"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static RuntimeComponent GetGeneratedComponentInParent(this Component component, RuntimeType type, bool includeInactive = false) {
			var comps = component.GetComponentsInParent<IRuntimeComponent>(includeInactive);
			foreach(var c in comps) {
				if(type.IsInstanceOfType(c)) {
					return c as RuntimeComponent;
				}
			}
			return null;
		}

		/// <summary>
		/// Get Generated Class Component in parent
		/// </summary>
		/// <param name="gameObject"></param>
		/// <param name="uniqueID"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static RuntimeComponent GetGeneratedComponentInParent(this GameObject gameObject, string uniqueID, bool includeInactive = false) {
			var comps = gameObject.GetComponentsInParent<IRuntimeComponent>(includeInactive);
			if(uniqueID.StartsWith("i:")) {
				uniqueID = uniqueID.Remove(0, 2);
				foreach(var c in comps) {
					if(c.uniqueIdentifier == uniqueID) {
						return c as RuntimeComponent;
					}
					var ifaces = c.GetInterfaces();
					foreach(var iface in ifaces) {
						if(iface.Name == uniqueID) {
							return c as RuntimeComponent;
						}
					}
				}
				return null;
			}
			foreach(var c in comps) {
				if(c.uniqueIdentifier == uniqueID) {
					return c as RuntimeComponent;
				}
			}
			return null;
		}

		/// <summary>
		/// Get Generated Class Component in parent
		/// </summary>
		/// <param name="component"></param>
		/// <param name="uniqueID"></param>
		/// <param name="includeInactive"></param>
		/// <returns></returns>
		public static RuntimeComponent GetGeneratedComponentInParent(this Component component, string uniqueID, bool includeInactive = false) {
			var comps = component.GetComponentsInParent<IRuntimeComponent>(includeInactive);
			if(uniqueID.StartsWith("i:")) {
				uniqueID = uniqueID.Remove(0, 2);
				foreach(var c in comps) {
					if(c.uniqueIdentifier == uniqueID) {
						return c as RuntimeComponent;
					}
					var ifaces = c.GetInterfaces();
					foreach(var iface in ifaces) {
						if(iface.Name == uniqueID) {
							return c as RuntimeComponent;
						}
					}
				}
				return null;
			}
			foreach(var c in comps) {
				if(c.uniqueIdentifier == uniqueID) {
					return c as RuntimeComponent;
				}
			}
			return null;
		}
		#endregion

		/// <summary>
		/// GetComponentInParent including inactive object
		/// </summary>
		/// <param name="gameObject"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T GetComponentInParent<T>(GameObject gameObject) {
			if(gameObject == null) return default;
			return GetComponentInParent<T>(gameObject.transform);
		}

		/// <summary>
		/// GetComponentInParent including inactive object
		/// </summary>
		/// <param name="transform"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T GetComponentInParent<T>(Component component) {
			if(component == null) return default;
			Transform parent = component.transform;
			while(parent != null) {
				var comp = parent.GetComponent<T>();
				if(comp != null) {
					return comp;
				}
				parent = parent.parent;
			}
			return default;
		}

		#region Runtimes
		public static void AddFunctionCaller<T>(IGraphWithUnityEvent graph, GameObject gameObject) where T : EventCaller {
			T t = gameObject.AddComponent<T>();
			t.owner = graph;
			//t.hideFlags = HideFlags.HideInInspector;
			if(graph.functionCallers == null) {
				graph.functionCallers = new List<EventCaller>();
			}
			graph.functionCallers.Add(t);
			if(graph.availableFunctionCallers == null) {
				graph.availableFunctionCallers = new HashSet<Type>();
			}
			graph.availableFunctionCallers.Add(typeof(T));
		}

		public static void InitializeRuntimeFunction(IGraphWithUnityEvent graph) {
			var root = graph as uNodeRoot;
			string name = root.GraphName;
			var func = graph as IFunctionSystem;
			{
				uNodeFunction function = func.GetFunction("Awake");
				if(function != null) {
					graph.onAwake += delegate () {
						Profiler.BeginSample(name + "." + "Awake");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("Start");
				if(function != null) {
					graph.onStart += delegate () {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".Start");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("Update");
				if(function != null) {
					graph.onUpdate += delegate () {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".Update");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("FixedUpdate");
				if(function != null) {
					graph.onFixedUpdate += delegate () {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".FixedUpdate");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("LateUpdate");
				if(function != null) {
					graph.onLateUpdate += delegate () {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".LateUpdate");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnAnimatorIK", typeof(int));
				if(function != null) {
					graph.onAnimatorIK += delegate (int param1) {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".OnAnimatorIK");
						function.Invoke(new object[] { param1 }, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnAnimatorMove");
				if(function != null) {
					graph.onAnimatorMove += delegate () {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".OnAnimatorMove");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnApplicationFocus", typeof(bool));
				if(function != null) {
					graph.onApplicationFocus += delegate (bool param1) {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".Start");
						function.Invoke(new object[] { param1 }, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnApplicationPause", typeof(bool));
				if(function != null) {
					graph.onApplicationPause += delegate (bool param1) {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".OnApplicationPause");
						function.Invoke(new object[] { param1 }, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnApplicationQuit");
				if(function != null) {
					graph.onApplicationQuit += delegate () {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".OnApplicationQuit");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnBecameInvisible");
				if(function != null) {
					graph.onBecameInvisible += delegate () {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnBecameInvisible");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnBecameVisible");
				if(function != null) {
					graph.onBecameVisible += delegate () {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnBecameVisible");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnCollisionEnter", typeof(Collision));
				if(function != null) {
					graph.onCollisionEnter += delegate (Collision param1) {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnCollisionEnter");
						function.Invoke(new object[] { param1 }, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnCollisionEnter2D", typeof(Collision2D));
				if(function != null) {
					graph.onCollisionEnter2D += delegate (Collision2D param1) {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnCollisionEnter2D");
						function.Invoke(new object[] { param1 }, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnCollisionExit", typeof(Collision));
				if(function != null) {
					graph.onCollisionExit += delegate (Collision param1) {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnCollisionExit");
						function.Invoke(new object[] { param1 }, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnCollisionExit2D", typeof(Collision2D));
				if(function != null) {
					graph.onCollisionExit2D += delegate (Collision2D param1) {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnCollisionExit2D");
						function.Invoke(new object[] { param1 }, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnCollisionStay", typeof(Collision));
				if(function != null) {
					graph.onCollisionStay += delegate (Collision param1) {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnCollisionStay");
						function.Invoke(new object[] { param1 }, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnCollisionStay2D", typeof(Collision2D));
				if(function != null) {
					graph.onCollisionStay2D += delegate (Collision2D param1) {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnCollisionStay2D");
						function.Invoke(new object[] { param1 }, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnDestroy");
				if(function != null) {
					graph.onDestroy += delegate () {
						Profiler.BeginSample(name + ".OnDestroy");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnDisable");
				if(function != null) {
					graph.onDisable += delegate () {
						Profiler.BeginSample(name + ".OnDisable");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnEnable");
				if(function != null) {
					graph.onEnable += delegate () {
						Profiler.BeginSample(name + ".OnEnable");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnGUI");
				if(function != null) {
					graph.onGUI += delegate () {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".OnGUI");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnMouseDown");
				if(function != null) {
					graph.onMouseDown += delegate () {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnMouseDown");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnMouseDrag");
				if(function != null) {
					graph.onMouseDrag += delegate () {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnMouseDrag");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnMouseEnter");
				if(function != null) {
					graph.onMouseEnter += delegate () {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnMouseEnter");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnMouseExit");
				if(function != null) {
					graph.onMouseExit += delegate () {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnMouseExit");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnMouseOver");
				if(function != null) {
					graph.onMouseOver += delegate () {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnMouseOver");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnMouseUp");
				if(function != null) {
					graph.onMouseUp += delegate () {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnMouseUp");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnMouseUpAsButton");
				if(function != null) {
					graph.onMouseUpAsButton += delegate () {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnMouseUpAsButton");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnPostRender");
				if(function != null) {
					graph.onPostRender += delegate () {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".OnPostRender");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnPreCull");
				if(function != null) {
					graph.onPreCull += delegate () {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".OnPreCull");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnPreRender");
				if(function != null) {
					graph.onPreRender += delegate () {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".OnPreRender");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnRenderObject");
				if(function != null) {
					graph.onRenderObject += delegate () {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".OnRenderObject");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnTransformChildrenChanged");
				if(function != null) {
					graph.onTransformChildrenChanged += delegate () {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".OnTransformChildrenChanged");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnTransformParentChanged");
				if(function != null) {
					graph.onTransformParentChanged += delegate () {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".OnTransformParentChanged");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnTriggerEnter", typeof(Collider));
				if(function != null) {
					graph.onTriggerEnter += delegate (Collider param1) {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnTriggerEnter");
						function.Invoke(new object[] { param1 }, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnTriggerEnter2D", typeof(Collider2D));
				if(function != null) {
					graph.onTriggerEnter2D += delegate (Collider2D param1) {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnTriggerEnter2D");
						function.Invoke(new object[] { param1 }, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnTriggerExit", typeof(Collider));
				if(function != null) {
					graph.onTriggerExit += delegate (Collider param1) {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnTriggerExit");
						function.Invoke(new object[] { param1 }, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnTriggerExit2D", typeof(Collider2D));
				if(function != null) {
					graph.onTriggerExit2D += delegate (Collider2D param1) {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnTriggerExit2D");
						function.Invoke(new object[] { param1 }, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnTriggerStay", typeof(Collider));
				if(function != null) {
					graph.onTriggerStay += delegate (Collider param1) {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnTriggerStay");
						function.Invoke(new object[] { param1 }, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnTriggerStay2D", typeof(Collider2D));
				if(function != null) {
					graph.onTriggerStay2D += delegate (Collider2D param1) {
						if(!root.enabled)
							return;
						System.Type type = function.ReturnType();
						if(type != null && type != typeof(void) && (type.IsCastableTo(typeof(IEnumerable)) || type.IsCastableTo(typeof(IEnumerator)))) {
							function.owner.StartCoroutine(function.Invoke(null, null) as IEnumerator);
							return;
						}
						Profiler.BeginSample(name + ".OnTriggerStay2D");
						function.Invoke(new object[] { param1 }, null);
						Profiler.EndSample();
					};
				}
			}
			{
				uNodeFunction function = func.GetFunction("OnWillRenderObject");
				if(function != null) {
					graph.onWillRenderObject += delegate () {
						if(!root.enabled)
							return;
						Profiler.BeginSample(name + ".OnWillRenderObject");
						function.Invoke(null, null);
						Profiler.EndSample();
					};
				}
			}
			//Initialize Function Calers
			
		}

		public static void InitializeRuntimeFunctionCallers(IGraphWithUnityEvent graph, GameObject gameObject) {
			if(graph.onUpdate != null && !HasAvailableFunctionCaller<UpdateCaller>(graph)) {
				AddFunctionCaller<UpdateCaller>(graph, gameObject);
			}
			if(graph.onFixedUpdate != null && !HasAvailableFunctionCaller<FixedUpdateCaller>(graph)) {
				AddFunctionCaller<FixedUpdateCaller>(graph, gameObject);
			}
			if(graph.onLateUpdate != null && !HasAvailableFunctionCaller<LateUpdateCaller>(graph)) {
				AddFunctionCaller<LateUpdateCaller>(graph, gameObject);
			}
			if(graph.onAnimatorMove != null && !HasAvailableFunctionCaller<OnAnimatorMoveCaller>(graph)) {
				AddFunctionCaller<OnAnimatorMoveCaller>(graph, gameObject);
			}
			if(graph.onApplicationFocus != null && !HasAvailableFunctionCaller<OnApplicationFocusCaller>(graph)) {
				AddFunctionCaller<OnApplicationFocusCaller>(graph, gameObject);
			}
			if(graph.onApplicationPause != null && !HasAvailableFunctionCaller<OnApplicationPauseCaller>(graph)) {
				AddFunctionCaller<OnApplicationPauseCaller>(graph, gameObject);
			}
			if(graph.onApplicationQuit != null && !HasAvailableFunctionCaller<OnApplicationQuitCaller>(graph)) {
				AddFunctionCaller<OnApplicationQuitCaller>(graph, gameObject);
			}
			if(graph.onBecameInvisible != null && !HasAvailableFunctionCaller<OnBecameInvisibleCaller>(graph)) {
				AddFunctionCaller<OnBecameInvisibleCaller>(graph, gameObject);
			}
			if(graph.onBecameVisible != null && !HasAvailableFunctionCaller<OnBecameVisibleCaller>(graph)) {
				AddFunctionCaller<OnBecameVisibleCaller>(graph, gameObject);
			}
			if(graph.onCollisionEnter != null && !HasAvailableFunctionCaller<OnCollisionEnterCaller>(graph)) {
				AddFunctionCaller<OnCollisionEnterCaller>(graph, gameObject);
			}
			if(graph.onCollisionEnter2D != null && !HasAvailableFunctionCaller<OnCollisionEnter2DCaller>(graph)) {
				AddFunctionCaller<OnCollisionEnter2DCaller>(graph, gameObject);
			}
			if(graph.onCollisionExit != null && !HasAvailableFunctionCaller<OnCollisionExitCaller>(graph)) {
				AddFunctionCaller<OnCollisionExitCaller>(graph, gameObject);
			}
			if(graph.onCollisionExit2D != null && !HasAvailableFunctionCaller<OnCollisionExit2DCaller>(graph)) {
				AddFunctionCaller<OnCollisionExit2DCaller>(graph, gameObject);
			}
			if(graph.onCollisionStay != null && !HasAvailableFunctionCaller<OnCollisionStayCaller>(graph)) {
				AddFunctionCaller<OnCollisionStayCaller>(graph, gameObject);
			}
			if(graph.onCollisionStay2D != null && !HasAvailableFunctionCaller<OnCollisionStay2DCaller>(graph)) {
				AddFunctionCaller<OnCollisionStay2DCaller>(graph, gameObject);
			}
			if(graph.onGUI != null && !HasAvailableFunctionCaller<OnGUICaller>(graph)) {
				AddFunctionCaller<OnGUICaller>(graph, gameObject);
			}
			if(graph.onMouseDown != null && !HasAvailableFunctionCaller<OnMouseDownCaller>(graph)) {
				AddFunctionCaller<OnMouseDownCaller>(graph, gameObject);
			}
			if(graph.onMouseDrag != null && !HasAvailableFunctionCaller<OnMouseDragCaller>(graph)) {
				AddFunctionCaller<OnMouseDragCaller>(graph, gameObject);
			}
			if(graph.onMouseEnter != null && !HasAvailableFunctionCaller<OnMouseEnterCaller>(graph)) {
				AddFunctionCaller<OnMouseEnterCaller>(graph, gameObject);
			}
			if(graph.onMouseExit != null && !HasAvailableFunctionCaller<OnMouseExitCaller>(graph)) {
				AddFunctionCaller<OnMouseExitCaller>(graph, gameObject);
			}
			if(graph.onMouseOver != null && !HasAvailableFunctionCaller<OnMouseOverCaller>(graph)) {
				AddFunctionCaller<OnMouseOverCaller>(graph, gameObject);
			}
			if(graph.onMouseUp != null && !HasAvailableFunctionCaller<OnMouseUpCaller>(graph)) {
				AddFunctionCaller<OnMouseUpCaller>(graph, gameObject);
			}
			if(graph.onMouseUpAsButton != null && !HasAvailableFunctionCaller<OnMouseUpAsButtonCaller>(graph)) {
				AddFunctionCaller<OnMouseUpAsButtonCaller>(graph, gameObject);
			}
			if(graph.onPostRender != null && !HasAvailableFunctionCaller<OnPostRenderCaller>(graph)) {
				AddFunctionCaller<OnPostRenderCaller>(graph, gameObject);
			}
			if(graph.onPreCull != null && !HasAvailableFunctionCaller<OnPreCullCaller>(graph)) {
				AddFunctionCaller<OnPreCullCaller>(graph, gameObject);
			}
			if(graph.onPreRender != null && !HasAvailableFunctionCaller<OnPreRenderCaller>(graph)) {
				AddFunctionCaller<OnPreRenderCaller>(graph, gameObject);
			}
			if(graph.onRenderObject != null && !HasAvailableFunctionCaller<OnRenderObjectCaller>(graph)) {
				AddFunctionCaller<OnRenderObjectCaller>(graph, gameObject);
			}
			if(graph.onTransformChildrenChanged != null && !HasAvailableFunctionCaller<OnTransformChildrenChangedCaller>(graph)) {
				AddFunctionCaller<OnTransformChildrenChangedCaller>(graph, gameObject);
			}
			if(graph.onTransformParentChanged != null && !HasAvailableFunctionCaller<OnTransformParentChangedCaller>(graph)) {
				AddFunctionCaller<OnTransformParentChangedCaller>(graph, gameObject);
			}
			if(graph.onTriggerEnter != null && !HasAvailableFunctionCaller<OnTriggerEnterCaller>(graph)) {
				AddFunctionCaller<OnTriggerEnterCaller>(graph, gameObject);
			}
			if(graph.onTriggerEnter2D != null && !HasAvailableFunctionCaller<OnTriggerEnter2DCaller>(graph)) {
				AddFunctionCaller<OnTriggerEnter2DCaller>(graph, gameObject);
			}
			if(graph.onTriggerExit != null && !HasAvailableFunctionCaller<OnTriggerExitCaller>(graph)) {
				AddFunctionCaller<OnTriggerExitCaller>(graph, gameObject);
			}
			if(graph.onTriggerExit2D != null && !HasAvailableFunctionCaller<OnTriggerExit2DCaller>(graph)) {
				AddFunctionCaller<OnTriggerExit2DCaller>(graph, gameObject);
			}
			if(graph.onTriggerStay != null && !HasAvailableFunctionCaller<OnTriggerStayCaller>(graph)) {
				AddFunctionCaller<OnTriggerStayCaller>(graph, gameObject);
			}
			if(graph.onTriggerStay2D != null && !HasAvailableFunctionCaller<OnTriggerStay2DCaller>(graph)) {
				AddFunctionCaller<OnTriggerStay2DCaller>(graph, gameObject);
			}
			if(graph.onWillRenderObject != null && !HasAvailableFunctionCaller<OnWillRenderObjectCaller>(graph)) {
				AddFunctionCaller<OnWillRenderObjectCaller>(graph, gameObject);
			}
		}

		public static bool HasAvailableFunctionCaller<T>(IGraphWithUnityEvent graph) where T : EventCaller {
			if(graph.availableFunctionCallers != null) {
				return graph.availableFunctionCallers.Contains(typeof(T));
			}
			return false;
		}
		#endregion

		public static string ExportGraphToJSON(uNodeRoot graph) {
			if(graph.RootObject == null)
				return "";
			Transform[] children = graph.RootObject.GetComponentsInChildren<Transform>(true);
			var serializedData = Serializer.Serializer.Serialize(graph.gameObject, new Component[] { graph, graph.transform }, children);
			return Serializer.SerializedData.ToJson(serializedData);
		}

		public static uNodeRoot ImportGraphFromJson(string json, GameObject gameObject) {
			Serializer.Serializer.Deserialize(Serializer.SerializedData.FromJson(json), gameObject);
			var graphs = gameObject.GetComponents<uNodeRoot>();
			if(graphs.Length > 0) {
				return graphs[graphs.Length - 1];
			}
			return null;
		}

		public static void AddEvent<T>(GameObject gameObject, Action action) where T : EventCaller {
			if(action == null)
				throw new ArgumentNullException("action");
			AddEvent<T>(gameObject, (obj) => action());
		}

		public static void AddEvent<T>(GameObject gameObject, Action<object> action) where T : EventCaller {
			if(gameObject == null)
				throw new ArgumentNullException("gameObject");
			if(action == null)
				throw new ArgumentNullException("action");
			T t = gameObject.GetComponent<T>();
			if(!t) {
				t = gameObject.AddComponent<T>();
			}
#if UNODE_DEBUG_PLUS
			t.hideFlags = HideFlags.HideInInspector;
#endif
			t.action += action;
		}

		public static bool HasLayer(LayerMask mask, int layer) {
			return mask == (mask | (1 << layer));
		}

		public static void SetObject(ref object reference, object value, SetType setType) {
			switch(setType) {
				case SetType.Change:
					reference = value;
					break;
				case SetType.Add:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Add);
					break;
				case SetType.Subtract:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Subtract);
					break;
				case SetType.Divide:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Divide);
					break;
				case SetType.Multiply:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Multiply);
					break;
			}
		}

		public static object SetObject(object reference, object value, SetType setType) {
			switch(setType) {
				case SetType.Change:
					reference = value;
					break;
				case SetType.Add:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Add);
					break;
				case SetType.Subtract:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Subtract);
					break;
				case SetType.Divide:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Divide);
					break;
				case SetType.Multiply:
					reference = ArithmeticOperator(reference, value, ArithmeticType.Multiply);
					break;
			}
			return reference;
		}

		public static bool OperatorComparison(object a, object b, ComparisonType operatorType) {
			if(a != null && b != null) {
				if(a is Enum && b is Enum) {
					a = Operator.Convert(a, Enum.GetUnderlyingType(a.GetType()));
					b = Operator.Convert(b, Enum.GetUnderlyingType(b.GetType()));
				}
				switch(operatorType) {
					case ComparisonType.Equal:
						return Operator.Equal(a, b, a.GetType(), b.GetType());
					case ComparisonType.NotEqual:
						return Operator.NotEqual(a, b, a.GetType(), b.GetType());
					case ComparisonType.GreaterThan:
						return Operator.GreaterThan(a, b, a.GetType(), b.GetType());
					case ComparisonType.LessThan:
						return Operator.LessThan(a, b, a.GetType(), b.GetType());
					case ComparisonType.GreaterThanOrEqual:
						return Operator.GreaterThanOrEqual(a, b, a.GetType(), b.GetType());
					case ComparisonType.LessThanOrEqual:
						return Operator.LessThanOrEqual(a, b, a.GetType(), b.GetType());
					default:
						throw new System.InvalidCastException();
				}
			} else {
				switch(operatorType) {
					case ComparisonType.Equal:
						return Operator.Equal(a, b);
					case ComparisonType.NotEqual:
						return Operator.NotEqual(a, b);
					case ComparisonType.GreaterThan:
						return Operator.GreaterThan(a, b);
					case ComparisonType.LessThan:
						return Operator.LessThan(a, b);
					case ComparisonType.GreaterThanOrEqual:
						return Operator.GreaterThanOrEqual(a, b);
					case ComparisonType.LessThanOrEqual:
						return Operator.LessThanOrEqual(a, b);
					default:
						throw new System.InvalidCastException();
				}
			}
		}

		public static bool OperatorComparison(object a, object b, ComparisonType operatorType, Type aType, Type bType) {
			if(a is Enum && b is Enum) {
				a = Operator.Convert(a, Enum.GetUnderlyingType(a.GetType()));
				b = Operator.Convert(b, Enum.GetUnderlyingType(b.GetType()));
			}
			switch(operatorType) {
				case ComparisonType.Equal:
					return Operator.Equal(a, b, aType, bType);
				case ComparisonType.NotEqual:
					return Operator.NotEqual(a, b, aType, bType);
				case ComparisonType.GreaterThan:
					return Operator.GreaterThan(a, b, aType, bType);
				case ComparisonType.LessThan:
					return Operator.LessThan(a, b, aType, bType);
				case ComparisonType.GreaterThanOrEqual:
					return Operator.GreaterThanOrEqual(a, b, aType, bType);
				case ComparisonType.LessThanOrEqual:
					return Operator.LessThanOrEqual(a, b, aType, bType);
				default:
					throw new System.InvalidCastException();
			}
		}

		public static object ShiftOperator(object a, int b, ShiftType operatorType) {
			switch(operatorType) {
				case ShiftType.LeftShift:
					return Operators.LeftShift(a, b, a.GetType());
				case ShiftType.RightShift:
					return Operators.RightShift(a, b, a.GetType());
				default:
					throw new System.InvalidCastException();
			}
		}

		public static object BitwiseOperator(object a, object b, BitwiseType operatorType) {
			switch(operatorType) {
				case BitwiseType.And:
					return Operators.And(a, b);
				case BitwiseType.Or:
					return Operators.Or(a, b);
				case BitwiseType.ExclusiveOr:
					return Operators.ExclusiveOr(a, b);
				default:
					throw new System.InvalidCastException();
			}
		}

		public static object ArithmeticOperator(object a, object b, ArithmeticType operatorType) {
			switch(operatorType) {
				case ArithmeticType.Add:
					return Operator.Add(a, b);
				case ArithmeticType.Subtract:
					return Operator.Subtract(a, b);
				case ArithmeticType.Divide:
					return Operator.Divide(a, b);
				case ArithmeticType.Multiply:
					return Operator.Multiply(a, b);
				case ArithmeticType.Modulo:
					return Operator.Modulo(a, b);
				default:
					throw new System.InvalidCastException();
			}
		}

		public static object ArithmeticOperator(object a, object b, ArithmeticType operatorType, Type aType, Type bType) {
			if(aType == null) {
				aType = typeof(object);
			}
			if(bType == null) {
				bType = aType;
			}
			switch(operatorType) {
				case ArithmeticType.Add:
					return Operator.Add(a, b, aType, bType);
				case ArithmeticType.Subtract:
					return Operator.Subtract(a, b, aType, bType);
				case ArithmeticType.Divide:
					return Operator.Divide(a, b, aType, bType);
				case ArithmeticType.Multiply:
					return Operator.Multiply(a, b, aType, bType);
				case ArithmeticType.Modulo:
					return Operator.Modulo(a, b, aType, bType);
				default:
					throw new System.InvalidCastException();
			}
		}

		public static object ArithmeticOperator(object a, object b, string operatorCode, Type aType, Type bType) {
			if(aType == null) {
				aType = a?.GetType() ?? bType ?? b?.GetType();
			}
			if(bType == null) {
				bType = aType;
			}
			switch(operatorCode) {
				case "+":
					return Operator.Add(a, b, aType, bType);
				case "-":
					return Operator.Subtract(a, b, aType, bType);
				case "/":
					return Operator.Divide(a, b, aType, bType);
				case "*":
					return Operator.Multiply(a, b, aType, bType);
				case "%":
					return Operator.Modulo(a, b, aType, bType);
				default:
					throw new System.InvalidCastException();
			}
		}
	}
}