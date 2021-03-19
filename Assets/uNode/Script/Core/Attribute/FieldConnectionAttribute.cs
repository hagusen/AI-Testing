using System;
using UnityEngine;

namespace MaxyGames.uNode {
	/// <summary>
	/// Attribute for show field connection in node window.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class FieldConnectionAttribute : Attribute {
		/// <summary>
		/// The label of the connection
		/// </summary>
		public GUIContent label;
		/// <summary>
		/// True for flow and false for value connection.
		/// </summary>
		public bool isFlowConnection;
		public bool isFinishedFlow;
		public bool displayFlowInHierarchy = true;
		public bool showValue = true;
		/// <summary>
		/// Hide connection if node is flow node.
		/// </summary>
		public bool hideOnFlowNode;
		/// <summary>
		/// Hide connection if node is not flow node.
		/// </summary>
		public bool hideOnNotFlowNode;

		public FieldConnectionAttribute() {
			label = GUIContent.none;
		}

		public FieldConnectionAttribute(string label) {
			if(label == null) {
				this.label = GUIContent.none;
			} else {
				this.label = new GUIContent(label, string.Empty);
			}
		}

		public FieldConnectionAttribute(string label, bool isFlowConnection = false, bool showValue = false) {
			if(label == null) {
				this.label = GUIContent.none;
			} else {
				this.label = new GUIContent(label, string.Empty);
			}
			this.isFlowConnection = isFlowConnection;
			this.showValue = showValue;
		}

		public FieldConnectionAttribute(bool isFlowConnection) {
			label = GUIContent.none;
			this.isFlowConnection = isFlowConnection;
		}

		public FieldConnectionAttribute(bool isFlowConnection, bool showValue) {
			label = GUIContent.none;
			this.isFlowConnection = isFlowConnection;
			this.showValue = showValue;
		}
	}

	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	public class OutputAttribute : Attribute {
		/// <summary>
		/// The field label
		/// </summary>
		public GUIContent label = GUIContent.none;
		public Type type;
		public bool isInstance;

		public OutputAttribute(Type type = null) {
			this.type = type;
		}

		public OutputAttribute(string label, Type type = null) {
			this.label = new GUIContent(label, string.Empty);
			this.type = type;
		}
	}
}