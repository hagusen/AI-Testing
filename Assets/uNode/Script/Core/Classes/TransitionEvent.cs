using UnityEngine;

namespace MaxyGames.uNode {
	/// <summary>
	/// Base class for all transition event.
	/// </summary>
	public abstract class TransitionEvent : uNodeComponent, INode<uNodeRoot> {
		public string Name;
		[Hide]
		public Node node;
		[Hide]
		public uNodeRoot owner;

		[HideInInspector, SerializeField]
		private Node targetNode;
		[Hide, SerializeField]
		private MemberData _target = new MemberData();

		[Hide]
		public Rect editorRect, editorPosition;

		public MemberData target {
			get {
				if(targetNode != null) {
					_target = MemberData.CreateConnection(targetNode, true);
					targetNode = null;
				}
				return _target;
			}
			set {
				targetNode = null;
				_target = value;
			}
		}

		public Node GetTargetNode() {
			if(targetNode != null) {
				target = MemberData.CreateConnection(targetNode, true);
				targetNode = null;
			}
			return target.GetTargetNode();
		}

		/// <summary>
		/// Called once after state Enter, generally used for Setup.
		/// </summary>
		public virtual void OnEnter() {

		}

		/// <summary>
		/// Called every frame when state is running.
		/// </summary>
		public virtual void OnUpdate() {

		}

		/// <summary>
		/// Called once after state exit, generally used for reset.
		/// </summary>
		public virtual void OnExit() {

		}

		/// <summary>
		/// Call to finish the transition.
		/// </summary>
		protected void Finish() {
			if(node.currentState == StateType.Running) {
				node.Finish();
				if(uNodeUtility.isInEditor) {
					uNodeUtility.InvokeTransition(node.owner, node.owner.GetInstanceID(), this.GetInstanceID());
				}
				if(target.isAssigned) {
					target.ActivateFlowNode();
				}
			}
		}

		public virtual void CodeGenerationInit() {

		}

		/// <summary>
		/// Used to generating OnEnter code.
		/// </summary>
		/// <returns></returns>
		public virtual string GenerateOnEnterCode() {
			return null;
		}

		/// <summary>
		/// Used to generating OnUpdate code
		/// </summary>
		/// <returns></returns>
		public virtual string GenerateOnUpdateCode() {
			return null;
		}

		/// <summary>
		/// Used to generating OnExit code
		/// </summary>
		/// <returns></returns>
		public virtual string GenerateOnExitCode() {
			return null;
		}

		public virtual System.Type GetIcon() {
			return null;
		}

		public uNodeRoot GetOwner() {
			return owner;
		}

		INodeRoot INode.GetNodeOwner() {
			return GetOwner();
		}
	}
}
