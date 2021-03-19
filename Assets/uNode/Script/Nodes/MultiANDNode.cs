using System.Collections.Generic;
using System.Linq;

namespace MaxyGames.uNode.Nodes {
    [NodeMenu("Data", "AND {&&}", typeof(bool))]
	public class MultiANDNode : ValueNode {
		[UnityEngine.HideInInspector, FieldConnection(false), Filter(typeof(bool))]
		public List<MemberData> targets = new List<MemberData>() { new MemberData(true), new MemberData(true) };

		public override System.Type ReturnType() {
			return typeof(bool);
		}

		protected override object Value() {
			if(targets.Count >= 2) {
				for(int i = 0; i < targets.Count; i++) {
					if(!targets[i].GetValue<bool>()) {
						return false;
					}
				}
				return true;
			}
			return false;
		}

		public override string GenerateValueCode() {
			if(targets.Count >= 2) {
				string contents = targets[0].ToCode();
				for(int i = 1; i < targets.Count; i++) {
					contents = CodeGenerator.GenerateAndCode(contents, targets[i].ToCode()).Wrap();
				}
				return contents;
			}
            throw new System.Exception("Target is unassigned");
		}

		public override string GetNodeName() {
			return "AND";
		}

		public override string GetRichName() {
			return string.Join(" && ", from target in targets select target.GetNicelyDisplayName(richName:true));
		}

		public override System.Type GetNodeIcon() {
			return typeof(TypeIcons.AndIcon2);
		}

		public override void CheckError() {
			base.CheckError();
			uNodeUtility.CheckError(targets, this, "targets");
			if(targets.Count < 2) {
				RegisterEditorError("The minimal value input must be 2.");
			}
		}
	}
}