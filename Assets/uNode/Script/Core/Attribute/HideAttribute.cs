using System;

namespace MaxyGames {
	/// <summary>
	/// Make variable hide in inspector.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	public sealed class HideAttribute : UnityEngine.PropertyAttribute {
		/// <summary>
		/// The target field name
		/// </summary>
		public string targetField { get; set; }
		/// <summary>
		/// The value for hide
		/// </summary>
		public object hideValue { get; set; }
		public Type[] hideValueArray { get; set; }
		public bool hideOnSame { get; set; }
		/// <summary>
		/// Will auto set the target object to null when hide.
		/// </summary>
		public bool NullOnHide { get; set; }
		/// <summary>
		/// Are hide for element type
		/// </summary>
		public bool elementType { get; set; }

		public HideAttribute() {

		}

		public HideAttribute(string targetField, object hideValue, bool hideOnSame = true) {
			this.targetField = targetField;
			this.hideValue = hideValue;
			this.hideOnSame = hideOnSame;
			this.NullOnHide = true;
			this.elementType = false;
		}

		public HideAttribute(string targetField, params Type[] hideValueArray) {
			this.hideOnSame = true;
			this.targetField = targetField;
			this.hideValue = hideValueArray;
			this.hideValueArray = hideValueArray;
			this.NullOnHide = true;
			this.elementType = false;
		}
	}
}