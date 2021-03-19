using System;

namespace MaxyGames.uNode {
	/// <summary>
	/// Used to make flow output for node.
	/// </summary>
	[System.Serializable]
	public sealed class FlowOutput : IFlowPin {
		public string name;
		public MemberData flow = new MemberData();

		public FlowOutput() {

		}

		public FlowOutput(string name) {
			this.name = name;
		}

		public void OnExecute() {
			throw new NotImplementedException();
		}

		public bool isAssigned {
			get {
				return flow.isAssigned;
			}
		}

		public override string ToString() {
			return name;
		}
	}

	/// <summary>
	/// Used to make flow input for node.
	/// </summary>
	[System.Serializable]
	public sealed class FlowInput : IFlowPin, IFlowGenerate {
		/// <summary>
		/// The name of flow input.
		/// </summary>
		public readonly string name;
		/// <summary>
		/// The action to execute.
		/// </summary>
		public Action onExecute;
		/// <summary>
		/// Assign this to implement code generation.
		/// </summary>
		public Func<string> codeGeneration;

		public FlowInput(string name) {
			this.name = name;
		}

		public FlowInput(string name, Action onExecute) {
			this.name = name;
			this.onExecute = onExecute;
		}

		public FlowInput(string name, Action onExecute, Func<string> codeGeneration) {
			this.name = name;
			this.onExecute = onExecute;
			this.codeGeneration = codeGeneration;
		}

		public FlowInput(Action onExecute) {
			this.onExecute = onExecute;
		}

		public FlowInput(Action onExecute, Func<string> codeGeneration) {
			this.onExecute = onExecute;
			this.codeGeneration = codeGeneration;
		}

		/// <summary>
		/// Called on pin being executed.
		/// </summary>
		public void OnExecute() {
			if(onExecute == null) {
				throw new Exception("Pin is not initialized/implemented.");
			}
			onExecute();
		}

		/// <summary>
		/// Called on generating c# code.
		/// </summary>
		/// <returns></returns>
		public string GenerateCode() {
			if(codeGeneration == null) {
				throw new Exception("Pin code generation callback is not initialized/implemented.");
			}
			return codeGeneration();
		}

		public override string ToString() {
			return base.ToString();
		}
	}
}