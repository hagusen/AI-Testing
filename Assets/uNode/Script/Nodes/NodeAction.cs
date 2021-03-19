using System;

namespace MaxyGames.uNode.Nodes {
	[NodeMenu("★General", "Action", order = 10)]
	public class NodeAction : Node {
		[EventType(EventData.EventType.Action)]
		public EventData Action = new EventData();

		[Hide, FieldConnection("", true)]
		public MemberData onFinished = new MemberData();

		public override void OnExecute() {
			if(Action != null)
				Action.Execute(owner);
			Finish(onFinished);
		}

		public override string GenerateCode() {
			if(Action != null) {
				string code = Action.GenerateCode(this, EventData.EventType.Action);
				return code + CodeGenerator.GetFinishCode(this, true, false, false, onFinished).AddLineInFirst();
			}
			return CodeGenerator.GetFinishCode(this, true, false, false, onFinished);
		}

		public override void CheckError() {
			base.CheckError();
			Action.CheckError(this);
		}

		public override string GetNodeName() {
			return gameObject.name;
		}

		public override bool IsCoroutine() {
			return HasCoroutineInFlow(onFinished);
		}

		public override Type GetNodeIcon() {
			return typeof(TypeIcons.ActionIcon);
		}
	}
}
