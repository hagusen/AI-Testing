﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.uNode {
	/// <summary>
	/// Class for running uNode in runtime.
	/// </summary>
	[AddComponentMenu("uNode/uNode Runtime")]
	[GraphSystem("uNode Runtime", order = 100, allowCreateInScene = true, supportAttribute = false, supportGeneric = false, supportModifier = false, allowAutoCompile=true, allowCompileToScript=false, generationKind = GenerationKind.Compatibility, inherithFrom=typeof(RuntimeBehaviour))]
	public class uNodeRuntime : uNodeBase, IStateGraph, IRuntimeGraph, IGraphWithUnityEvent, IIndependentGraph {
		#region Fields
		[SerializeField]
		protected string @namespace;
		[HideInInspector]
		public List<string> usingNamespaces = new List<string>() { "UnityEngine", "System.Collections.Generic" };

		[HideInInspector, SerializeField]
		protected EventNode[] methods = new EventNode[0];
		[NonSerialized]
		private bool initializeOnAwake = false;
		[NonSerialized]
		internal bool manualHandlingEvent = false;
		#endregion

		#region CacheData
		internal Dictionary<string, EventNode> customMethod = new Dictionary<string, EventNode>();
		
		[System.NonSerialized]
		protected bool hasInitialize = false;
		/// <summary>
		/// The source of the graph that's spawning this graph (Runtime).
		/// </summary>
		/// <value></value>
		public uNodeRoot originalGraph { get; set; }
		public RuntimeBehaviour runtimeBehaviour { get; private set; }
		#endregion

		#region Intialize Function
		/// <summary>
		/// Initialize the graph, this also will call Awake function
		/// </summary>
		public void Initialize() {
			if(hasInitialize)
				return;
			if(Application.isPlaying) {
				hasInitialize = true;
				var type = GeneratedTypeName.ToType(false);
				if(type != null) {
					runtimeBehaviour = gameObject.AddComponent(type) as RuntimeBehaviour;
					for(int i = 0; i < Variables.Count; i++) {
						SetVariable(Variables[i].Name, variable[i].value);
					}
					var references = graphData.unityObjects;
					for (int i = 0; i < references.Count;i++) {
						SetVariable(references[i].name, references[i].value);
					}
					runtimeBehaviour.OnAwake();
					return;
				}
				if(RootObject) {
					var nodes = RootObject.GetComponentsInChildren<NodeComponent>();
					foreach(NodeComponent node in nodes) {
						if(node != null) {
							if(node is BaseEventNode) {
								(node as BaseEventNode).Initialize();
							}
							node.RegisterPort();
						}
					}
				}
				InitializeFunction();
				var graph = this as IGraphWithUnityEvent;
				try {
					if(graph.onAwake != null) {
						graph.onAwake();
					}
				}
				catch(System.Exception ex) {
					if(ex is uNodeException) {
						throw;
					}
					throw uNodeDebug.LogException(ex, this);
				}
			}
		}

		void InitializeFunction() {
			if(!uNodeUtility.isPlaying)
				return;
			uNodeHelper.InitializeRuntimeFunction(this);
			uNodeHelper.InitializeRuntimeFunctionCallers(this, gameObject);
		}
		#endregion

		#region UnityEvents
		Action IGraphWithUnityEvent.onAwake { get; set; }
		Action IGraphWithUnityEvent.onStart { get; set; }
		Action IGraphWithUnityEvent.onUpdate { get; set; }
		Action IGraphWithUnityEvent.onFixedUpdate { get; set; }
		Action IGraphWithUnityEvent.onLateUpdate { get; set; }
		Action<int> IGraphWithUnityEvent.onAnimatorIK { get; set; }
		Action IGraphWithUnityEvent.onAnimatorMove { get; set; }
		Action<bool> IGraphWithUnityEvent.onApplicationFocus { get; set; }
		Action<bool> IGraphWithUnityEvent.onApplicationPause { get; set; }
		Action IGraphWithUnityEvent.onApplicationQuit { get; set; }
		Action IGraphWithUnityEvent.onBecameInvisible { get; set; }
		Action IGraphWithUnityEvent.onBecameVisible { get; set; }
		Action<Collision> IGraphWithUnityEvent.onCollisionEnter { get; set; }
		Action<Collision2D> IGraphWithUnityEvent.onCollisionEnter2D { get; set; }
		Action<Collision> IGraphWithUnityEvent.onCollisionExit { get; set; }
		Action<Collision2D> IGraphWithUnityEvent.onCollisionExit2D { get; set; }
		Action<Collision> IGraphWithUnityEvent.onCollisionStay { get; set; }
		Action<Collision2D> IGraphWithUnityEvent.onCollisionStay2D { get; set; }
		Action IGraphWithUnityEvent.onDestroy { get; set; }
		Action IGraphWithUnityEvent.onDisable { get; set; }
		Action IGraphWithUnityEvent.onEnable { get; set; }
		Action IGraphWithUnityEvent.onGUI { get; set; }
		Action IGraphWithUnityEvent.onMouseDown { get; set; }
		Action IGraphWithUnityEvent.onMouseDrag { get; set; }
		Action IGraphWithUnityEvent.onMouseEnter { get; set; }
		Action IGraphWithUnityEvent.onMouseExit { get; set; }
		Action IGraphWithUnityEvent.onMouseOver { get; set; }
		Action IGraphWithUnityEvent.onMouseUp { get; set; }
		Action IGraphWithUnityEvent.onMouseUpAsButton { get; set; }
		Action IGraphWithUnityEvent.onPostRender { get; set; }
		Action IGraphWithUnityEvent.onPreCull { get; set; }
		Action IGraphWithUnityEvent.onPreRender { get; set; }
		Action IGraphWithUnityEvent.onRenderObject { get; set; }
		Action IGraphWithUnityEvent.onTransformChildrenChanged { get; set; }
		Action IGraphWithUnityEvent.onTransformParentChanged { get; set; }
		Action<Collider> IGraphWithUnityEvent.onTriggerEnter { get; set; }
		Action<Collider2D> IGraphWithUnityEvent.onTriggerEnter2D { get; set; }
		Action<Collider> IGraphWithUnityEvent.onTriggerExit { get; set; }
		Action<Collider2D> IGraphWithUnityEvent.onTriggerExit2D { get; set; }
		Action<Collider> IGraphWithUnityEvent.onTriggerStay { get; set; }
		Action<Collider2D> IGraphWithUnityEvent.onTriggerStay2D { get; set; }
		Action IGraphWithUnityEvent.onWillRenderObject { get; set; }
		List<EventCaller> IGraphWithUnityEvent.functionCallers { get; set; }
		HashSet<Type> IGraphWithUnityEvent.availableFunctionCallers { get; set; }

		void Awake() {
			try {
				initializeOnAwake = originalGraph == null && nodes != null && nodes.Length > 0;
				if(initializeOnAwake) {
					Initialize();
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, this);
			}
		}

		void Start() {
			try {
				if(!initializeOnAwake) {
					Initialize();
					if(enabled)
						DoOnEnable();
				}
				if(manualHandlingEvent)
					return;
				var graph = this as IGraphWithUnityEvent;
				if(graph.onStart != null) {
					graph.onStart();
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, this);
			}
		}

		void DoOnEnable() {
			try {
				var graph = this as IGraphWithUnityEvent;
				if(graph.onEnable != null) {
					graph.onEnable();
				}
				if(graph.functionCallers?.Count > 0) {
					foreach(EventCaller f in graph.functionCallers) {
						if(f) {
							f.enabled = true;
						}
					}
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, this);
			}
		}

		void OnEnable() {
			if(initializeOnAwake) {
				DoOnEnable();
				initializeOnAwake = false;
			}
		}

		void OnDisable() {
			try {
				if(enabled) {
					for(int i = 0; i < nodes.Length; i++) {
						Node node = nodes[i];
						if(node != null) {
							node.Stop();
						}
					}
				}
				var graph = this as IGraphWithUnityEvent;
				if(graph.onDisable != null) {
					graph.onDisable();
				}
				if(graph.functionCallers?.Count > 0) {
					foreach(EventCaller f in graph.functionCallers) {
						if(f) {
							f.enabled = false;
						}
					}
				}
				initializeOnAwake = true;
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, this);
			}
		}

		void OnDestroy() {
			try {
				var graph = this as IGraphWithUnityEvent;
				if(graph.onDestroy != null) {
					graph.onDestroy();
				}
				if(graph.functionCallers?.Count > 0) {
					foreach(EventCaller f in graph.functionCallers) {
						if(f) {
							Destroy(f);
						}
					}
				}
			}
			catch(System.Exception ex) {
				if(ex is uNodeException) {
					throw;
				}
				throw uNodeDebug.LogException(ex, this);
			}
		}
		#endregion

		#region Functions
		public IList<EventNode> eventNodes {
			get {
				return methods;
			}
			set {
				if(value is EventNode[]) {
					methods = value as EventNode[];
				} else {
					methods = (value as List<EventNode>).ToArray();
				}
			}
		}

		public new List<VariableData> Variables {
			get {
				return base.Variables;
			}
			set {
				variable = value;
			}
		}

		string IIndependentGraph.Namespace => !string.IsNullOrEmpty(@namespace) ? @namespace : RuntimeType.RuntimeNamespace;
		List<string> IIndependentGraph.UsingNamespaces { get => usingNamespaces; set => usingNamespaces = value; }
		bool IStateGraph.canCreateGraph => true;

		public override Type GetInheritType() {
			return typeof(MonoBehaviour);
		}

		/// <summary>
		/// Execute function and custom event by name
		/// This method will not throw an exception when no function or event are found
		/// </summary>
		/// <param name="Name">The function name to Activate</param>
		public void ExecuteFunction(string Name) {
			if(!hasInitialize) {
				Initialize();
			}
			if (runtimeBehaviour != null) {
				runtimeBehaviour.InvokeFunction(Name, new object[0]);
				return;
			}
			EventNode method = GetCustomEvent(Name);
			if(method != null) {
				method.Activate();
				var func = GetFunction(Name);
				if(func != null) {
					func.Invoke();
				}
			} else {
				var func = GetFunction(Name);
				if(func != null) {
					func.Invoke();
				} else {
					//throw new System.Exception("No function with name: " + Name);
				}
			}
		}

		public object InvokeFunction(string Name, object[] values) {
			if (runtimeBehaviour != null) {
				return runtimeBehaviour.InvokeFunction(Name, values);
			}
			if(values == null) {
				return InvokeFunction(Name, null, null);
			}
			System.Type[] parameters = new System.Type[values.Length];
			bool valid = true;
			for (int i = 0; i < values.Length;i++) {
				var val = values[i];
				if(val == null) {
					valid = false;
					continue;
					// throw new NullReferenceException("Invoking function: " + Name + " with null parameter at index: " + i + "Use InvokeFunction with its type paramter to use null parameter");
				}
				parameters[i] = val.GetType();
			}
			if(valid) {
				var func = GetFunction(Name, parameters);
				if(func != null) {
					return func.Invoke(values);
				}
			}
			int parameterLength = parameters.Length;
			for(int i = 0; i < functions.Length; i++) {
				uNodeFunction function = functions[i];
				if(function.Name == Name && 
					function.parameters.Length == parameterLength && 
					function.genericParameters.Length == 0) {
					bool isValid = true;
					if(parameterLength != 0) {
						for(int x = 0; x < parameters.Length; x++) {
							if(function.parameters[x].Type != null &&
								(parameters[x] != null && function.parameters[x].Type != parameters[x])) {
								isValid = false;
								break;
							}
						}
					}
					if(isValid) {
						return function.Invoke(values);
					}
				}
			}
			throw new System.Exception("No valid function with name: " + Name);
		}

		public object InvokeFunction(string Name, System.Type[] parameters, object[] values) {
			if(!hasInitialize) {
				Initialize();
			}
			if (runtimeBehaviour != null) {
				return runtimeBehaviour.InvokeFunction(Name, parameters, values);
			}
			var func = GetFunction(Name, parameters);
			if(func != null) {
				return func.Invoke(values);
			} else  {
				throw new System.Exception("No function with name: " + Name);
			}
		}
		
		protected EventNode GetCustomEvent(string Name) {
			if(!hasInitialize) {
				Initialize();
			}
			if(customMethod.ContainsKey(Name)) {
				return customMethod[Name];
			}
			//Debug.LogError("No method with name: " + Name, this);
			return null;
		}

		public void SetVariable(string Name, object value) {
			if (runtimeBehaviour != null) {
				runtimeBehaviour.SetVariable(Name, value);
				return;
			}
			var data = GetVariableData(Name);
			if(data == null) {
				throw new Exception($"Variable: {Name} not found in object: {this}");
			}
			data.Set(value);
		}

		public void SetVariable(string Name, object value, string @operator) {
			if(runtimeBehaviour != null) {
				runtimeBehaviour.SetVariable(Name, value);
				return;
			}
			var data = GetVariableData(Name);
			if(data == null) {
				throw new Exception($"Variable: {Name} not found in object: {this}");
			}
			switch(@operator) {
				case "+":
				case "-":
				case "/":
				case "*":
				case "%":
					var val = data.Get();
					value = uNodeHelper.ArithmeticOperator(val, value, @operator, data.Type, value?.GetType());
					break;
			}
			data.Set(value);
		}

		public object GetVariable(string Name) {
			if (runtimeBehaviour != null) {
				return runtimeBehaviour.GetVariable(Name);
			}
			var data = GetVariableData(Name);
			if(data == null) {
				throw new Exception($"Variable: {Name} not found in object: {this}");
			}
			return data.Get();
		}

		public T GetVariable<T>(string Name) {
			var result = GetVariable(Name);
			if(result != null) {
				return (T)result;
			}
			return default(T);
		}

		public void SetProperty(string Name, object value) {
			if (runtimeBehaviour != null) {
				runtimeBehaviour.SetProperty(Name, value);
				return;
			}
			var data = GetPropertyData(Name);
			if(data == null) {
				throw new Exception($"Property: {Name} not found in object: {this}");
			}
			data.Set(value);
		}

		public void SetProperty(string Name, object value, string @operator) {
			if(runtimeBehaviour != null) {
				runtimeBehaviour.SetProperty(Name, value);
				return;
			}
			var data = GetPropertyData(Name);
			if(data == null) {
				throw new Exception($"Property: {Name} not found in object: {this}");
			}
			switch(@operator) {
				case "+":
				case "-":
				case "/":
				case "*":
				case "%":
					var val = data.Get();
					value = uNodeHelper.ArithmeticOperator(val, value, @operator, data.ReturnType(), value?.GetType());
					break;
			}
			data.Set(value);
		}

		public object GetProperty(string Name) {
			if (runtimeBehaviour != null) {
				return runtimeBehaviour.GetProperty(Name);
			}
			var data = GetPropertyData(Name);
			if(data == null) {
				throw new Exception($"Property: {Name} not found in object: {this}");
			}
			return data.Get();
		}

		public T GetProperty<T>(string Name) {
			var result = GetProperty(Name);
			if(result != null) {
				return (T)result;
			}
			return default(T);
		}

		/// <summary>
		/// Get variable by name
		/// </summary>
		/// <param name="Name">The variable name to get</param>
		/// <returns></returns>
		public override VariableData GetVariableData(string Name) {
			if(!uNodeUtility.isPlaying) {
				return base.GetVariableData(Name);
			}
			for(int i = 0; i < variable.Count; i++) {
				if(variable[i].Name.Equals(Name)) {
					return variable[i];
				}
			}
			throw new System.NullReferenceException("Variable: " + Name + " Not Found");
		}
		#endregion

		#region Editor
		public override void Refresh() {
			base.Refresh();
			if(RootObject != null) {
				methods = RootObject.GetComponentsInChildren<EventNode>(true);
				var events = RootObject.GetComponentsInChildren<BaseEventNode>(true);
				var TRevents = RootObject.GetComponentsInChildren<TransitionEvent>(true);
				foreach(var transition in TRevents) {
					if(transition.owner != this) {
						transition.owner = this;
					}
				}
				foreach(var m in events) {
					if(m.owner != this) {
						m.owner = this;
					}
					if(m.gameObject.name != m.Name) {
						m.gameObject.name = m.Name;
					}
				}
			}
		}
		#endregion
	}
}