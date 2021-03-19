using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.uNode {
	/// <summary>
	/// Base class for all uNode component.
	/// </summary>
	public abstract class uNodeComponent : MonoBehaviour, INodeComponent { }

	/// <summary>
	/// Base class for all event node.
	/// </summary>
	public abstract class BaseEventNode : NodeComponent {
		/// <summary>
		/// The name of this node.
		/// </summary>
		public string Name;

		[HideInInspector, SerializeField]
		private List<Node> nodes = new List<Node>();
		[HideInInspector, SerializeField]
		public List<MemberData> targetNodes = new List<MemberData>();

		public abstract void Initialize();

		public List<MemberData> GetFlows() {
			if(nodes.Count > 0) {
				targetNodes.Clear();
				for(int i = 0; i < nodes.Count; i++) {
					targetNodes.Add(new MemberData(nodes[i], MemberData.TargetType.FlowNode));
				}
				nodes.Clear();
			}
			return targetNodes;
		}
	}

	public abstract class BaseRuntimeGraph : uNodeRoot, IClass, IIndependentGraph, ICustomIcon, IRuntimeInterfaceSystem {
		[SerializeField]
		protected Texture2D icon;
		[SerializeField]
		protected string @namespace;
		[HideInInspector, SerializeField]
		protected List<string> usingNamespaces = new List<string>() { "UnityEngine", "System.Collections.Generic" };
		[HideInInspector, SerializeField]
		protected List<VariableData> variables = new List<VariableData>();
		[HideInInspector, SerializeField]
		protected uNodeFunction[] functions = new uNodeFunction[0];
		[HideInInspector, SerializeField]
		protected uNodeProperty[] properties = new uNodeProperty[0];
		[HideInInspector, SerializeField]
		protected MemberData[] interfaces = new MemberData[0];

		public override string GraphName {
			get {
				if(string.IsNullOrEmpty(Name)) {
					return gameObject.name;
				}
				return Name;
			}
		}

		public override List<VariableData> Variables => variables;
		public override IList<uNodeProperty> Properties => properties;
		public override IList<uNodeFunction> Functions => functions;
		public override IList<uNodeConstuctor> Constuctors => new uNodeConstuctor[0];

		public IList<MemberData> Interfaces {
			get {
				return interfaces;
			}
			set {
				if(value is MemberData[]) {
					interfaces = value as MemberData[];
					return;
				}
				interfaces = value.ToArray();
			}
		}

		bool IClass.IsStruct => false;
		public virtual string uniqueIdentifier => GraphName;
		public string Namespace => !string.IsNullOrEmpty(@namespace) ? @namespace : RuntimeType.RuntimeNamespace;
		public List<string> UsingNamespaces { get => usingNamespaces; set => usingNamespaces = value; }

		public override void Refresh() {
			base.Refresh();
			if (RootObject == null)
				return;
			functions = GetFunctions();
			properties = GetProperties();
		}
		
		public Texture2D GetIcon() {
			return icon;
		}
	}

	public abstract class BaseRuntimeComponentGraph : BaseRuntimeGraph, IStateGraph, IClassComponent {
		[HideInInspector, SerializeField]
		protected EventNode[] events = new EventNode[0];

		public IList<EventNode> eventNodes => events;

		bool IStateGraph.canCreateGraph => true;

		public override Type GetInheritType() {
			return typeof(MonoBehaviour);
		}

		public override void Refresh() {
			base.Refresh();
			if (RootObject == null)
				return;
			events = RootObject.GetComponentsInChildren<EventNode>(true);
			var eventNodes = RootObject.GetComponentsInChildren<BaseEventNode>(true);
			var TRevents = RootObject.GetComponentsInChildren<TransitionEvent>(true);
			foreach(var transition in TRevents) {
				if(transition.owner != this) {
					transition.owner = this;
				}
			}
			foreach(var m in eventNodes) {
				if(m.owner != this) {
					m.owner = this;
				}
				if(m.gameObject.name != m.Name) {
					m.gameObject.name = m.Name;
				}
			}
		}
	}
	
    public abstract class BaseRuntimeAssetGraph : BaseRuntimeGraph, IClassAsset {
		public override Type GetInheritType() {
			return typeof(ScriptableObject);
		}
	}
}