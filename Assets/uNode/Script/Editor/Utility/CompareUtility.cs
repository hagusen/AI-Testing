namespace MaxyGames.uNode.Editors {
	public static class CompareUtility {
		public static int Compare(int intA, int intB) {
			if (intA < intB) {
				return -1;
			}
			if (intB < intA) {
				return 1;
			}
			return 0;
		}

		public static int Compare(string strA, int intA, string strB, int intB) {
			if (intA == intB) {
				return string.Compare(strA, strB);
			}
			if (intA < intB) {
				return -1;
			}
			if (intB < intA) {
				return 1;
			}
			return string.Compare(strA, strB);
		}
	}
}