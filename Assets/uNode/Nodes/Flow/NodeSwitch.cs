using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("Flow", "Switch")]
	public class NodeSwitch : Node {
		[Hide]
		public MemberData onFinished = new MemberData();
		[Hide, FieldConnection(false), Filter(typeof(int), typeof(bool), typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(long), typeof(ulong), typeof(uint), typeof(string), typeof(System.Enum), InvalidTargetType = MemberData.TargetType.Null)]
		public MemberData target = new MemberData();
		[HideInInspector]
		public List<MemberData> values = new List<MemberData>();
		[HideInInspector, ObjectType("target")]
		public List<MemberData> targetNodes = new List<MemberData>();
		[HideInInspector]
		public MemberData defaultTarget = new MemberData();

		public override void OnExecute() {
			if(target == null || !target.isAssigned)
				return;
			object val = target.Get();
			if(object.ReferenceEquals(val, null))
				return;
			for(int i = 0; i < values.Count; i++) {
				MemberData member = values[i];
				if(member == null || !member.isAssigned)
					continue;
				object mVal = member.Get();
				if(mVal.Equals(val)) {
					Finish(targetNodes[i], onFinished);
					return;
				}
			}
			Finish(defaultTarget, onFinished);
		}

		public override string GenerateCode() {
			if(target.isAssigned) {
				string data = CodeGenerator.ParseValue((object)target);
				if(!string.IsNullOrEmpty(data)) {
					bool hasDefault = defaultTarget != null && defaultTarget.isAssigned;
					ArrayList list = new ArrayList();
					data = "switch(" + data + ") {";
					string datas = null;
					for(int i = 0; i < values.Count; i++) {
						if(targetNodes[i] == null || !targetNodes[i].isAssigned)
							continue;
						Node tn = targetNodes[i].GetTargetNode();
						if(tn != null) {
							string val = CodeGenerator.ParseValue((object)values[i]);
							if(!list.Contains(val)) {
								datas += "\ncase " + val + ": {";
								string str = CodeGenerator.GenerateFlowCode(targetNodes[i], this);
								if(!string.IsNullOrEmpty(str)) {
									datas += ("\n" + str).AddTabAfterNewLine(1);
								}
								datas += "\n}\nbreak;";
								list.Add(tn);
								list.Add(val);
							}
						}
					}
					if(hasDefault) {
						string str = CodeGenerator.GenerateFlowCode(defaultTarget, this);
						if(!string.IsNullOrEmpty(str)) {
							datas += "\ndefault: {";
							datas += ("\n" + str).AddTabAfterNewLine(1);
							datas += "\n}\nbreak;";
						}
					}
					if(string.IsNullOrEmpty(datas))
						return CodeGenerator.GetFinishCode(this, true, false, false, onFinished);
					data += datas.AddTabAfterNewLine() + "\n}";
					return data + CodeGenerator.GetFinishCode(this, true, false, false, onFinished);
				}
				throw new System.Exception("Can't Parse target");
			}
			throw new System.Exception("Target is unassigned");
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.BranchIcon);
		}

		public override bool IsCoroutine() {
			return HasCoroutineInFlow(targetNodes) || HasCoroutineInFlow(onFinished);
		}

		public override string GetRichName() {
			return $"{uNodeUtility.WrapTextWithKeywordColor("switch")}: {target.GetNicelyDisplayName(richName:true)}";
		}

		public override void CheckError() {
			base.CheckError();
			uNodeUtility.CheckError(target, this, "target");
		}
	}
}

#if UNITY_EDITOR
namespace MaxyGames.uNode.Editors.Commands {
	using System.Collections.Generic;
	using MaxyGames.uNode.Nodes;

	public class CustomInputSwitchItem : CustomInputPortItem {
		public override IList<ItemSelector.CustomItem> GetItems(Node source, MemberData data, System.Type type) {
			var items = new List<ItemSelector.CustomItem>();
			items.Add(new ItemSelector.CustomItem("Switch", () => {
				NodeEditorUtility.AddNewNode(graph.editorData, null, null, mousePositionOnCanvas, (NodeSwitch n) => {
					n.target = data;
					graph.Refresh();
				});
			}, "Flows") { icon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon)) });
			return items;
		}

		public override bool IsValidPort(Type type) {
			return type.IsPrimitive || type.IsEnum || type == typeof(string);
		}
	}
}
#endif