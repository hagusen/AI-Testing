using UnityEngine;

namespace MaxyGames.uNode.Editors {
	/// <summary>
	/// Base class for all custom port menu.
	/// </summary>
	public abstract class PortMenuCommand {
		/// <summary>
		/// The graph editor.
		/// </summary>
		public NodeGraph graph;
		/// <summary>
		/// The mouse position on canvas.
		/// </summary>
		public Vector2 mousePositionOnCanvas;
		/// <summary>
		/// The filter for the port.
		/// </summary>
		public FilterAttribute filter;

		[System.Flags]
		public enum PortKind {
			ValueInput = 1 << 0,
			ValueOutput = 1 << 1,
			FlowOutput = 1 << 2,
			FlowInput = 1 << 3,
		}

		/// <summary>
		/// The name of the menu.
		/// </summary>
		public abstract string name { get; }
		/// <summary>
		/// The order of the menu, default is 0.
		/// </summary>
		public virtual int order => 0;
		/// <summary>
		/// What is the valid port kind.
		/// </summary>
		/// <returns></returns>
		public abstract PortKind ValidPort();
		/// <summary>
		/// Callback for do something on menu is clicked.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="value"></param>
		/// <param name="mousePosition"></param>
		public abstract void OnClick(Node source, PortCommandData data, Vector2 mousePosition);
		/// <summary>
		/// Validate if port is valid for this command.
		/// </summary>
		/// <param name="source"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public virtual bool IsValidPort(Node source, PortCommandData data) {
			return true;
		}
	}

	public class PortCommandData {
		/// <summary>
		/// The name of the port.
		/// </summary>
		public string portName;
		/// <summary>
		/// The original port value.
		/// </summary>
		public MemberData member;
		/// <summary>
		/// Required for value port.
		/// </summary>
		public System.Type portType;
		/// <summary>
		/// Required for output value port.
		/// </summary>
		public System.Func<MemberData> getConnection;
		/// <summary>
		/// The attributes for the port.
		/// </summary>
		public object[] attributes;
	}
}