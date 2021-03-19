using System;

namespace MaxyGames {
	/// <summary>
	/// Used to show 2 or more list in one field.
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Field)]
	public class ListCombineAttribute : Attribute {
		public string[] otherFields { get; set; }

		public ListCombineAttribute(string otherField = null) {
			if(string.IsNullOrEmpty(otherField)) return;
			this.otherFields = new string[] { otherField };
		}
	}
}
