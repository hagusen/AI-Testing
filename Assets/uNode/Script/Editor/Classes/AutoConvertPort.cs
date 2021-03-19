namespace MaxyGames.uNode.Editors {
	/// <summary>
	/// Base class for auto convert pin.
	/// </summary>
	public abstract class AutoConvertPort {
		public FilterAttribute filter;

		public Node rightNode;
		public Node leftNode;

		public System.Type leftType;
		public System.Type rightType;

		public virtual int order { get { return 0; } }

		public abstract bool IsValid();
		public abstract Node CreateNode();
	}
}