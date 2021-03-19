using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace MaxyGames.uNode.Editors {
	public static class ConsoleActivityWatcher {
		[InitializeOnLoadMethod]
		static void Initialize() {
			//Delay some frame to avoid lag on editor startup.
			uNodeThreadUtility.ExecuteAfter(100, () => {
				consoleWindowType = "UnityEditor.ConsoleWindow".ToType();
				fieldActiveText = consoleWindowType.GetField("m_ActiveText", BindingFlags.Instance | BindingFlags.NonPublic);
				fieldInstanceID = consoleWindowType.GetField("m_ActiveInstanceID", BindingFlags.Instance | BindingFlags.NonPublic);
				EditorApplication.update += WatchConsoleActivity;
				// Debug.Log("Ready");
			});
		}

		private static Type consoleWindowType;
		private static FieldInfo fieldActiveText;
		private static FieldInfo fieldInstanceID;
		private static string lastText;
		private static int lastInstanceID;
		private static EditorWindow consoleWindow;

		private static float time;

		private static void WatchConsoleActivity() {
			// if (entryChanged == null) return;
			if (consoleWindow == null) {
				if (time < uNodeThreadUtility.time) {
					consoleWindow = Resources.FindObjectsOfTypeAll(consoleWindowType).FirstOrDefault() as EditorWindow;
					//Find console window every 1 second
					time = uNodeThreadUtility.time + 1;
				}
				if(consoleWindow == null) {
					return;
				}
			}
			string activeText;
			int activeInstanceID;
			if(EditorWindow.focusedWindow != consoleWindow) {
				activeText = lastText;
				activeInstanceID = lastInstanceID;
			} else if (consoleWindow != null) {
				activeText = (string)fieldActiveText.GetValue(consoleWindow);
				activeInstanceID = (int)fieldInstanceID.GetValue(consoleWindow);
			} else {
				activeText = null;
				activeInstanceID = 0;
			}
			try {
				if (activeText != lastText || activeInstanceID != lastInstanceID) {
					ConsoleActivityChanged(activeText, activeInstanceID);
				}
			} finally {
				lastText = activeText;
				lastInstanceID = activeInstanceID;
			}
		}

		private struct ActivityData {
			public int number;
			public string path;
			public uNodeEditor.EditorScriptInfo info;

			public ActivityData(int number, string path, uNodeEditor.EditorScriptInfo info) {
				this.number = number;
				this.path = path;
				this.info = info;
			}
		}

		private static void ConsoleActivityChanged(string text, int instanceID) {
			if(string.IsNullOrEmpty(text)) return;
			{
				var index = text.IndexOf(uNodeException.KEY_REFERENCE);
				if(index > 0) {
					string str = null;
					for (int i = index + uNodeException.KEY_REFERENCE.Length; i < text.Length;i++){
						if(str == null || char.IsNumber(text[i])) {
							str += text[i];
						} else {
							break;
						}
					}
					if(int.TryParse(str, out var id)) {
						var reference = EditorUtility.InstanceIDToObject(id);
						if(reference is INode<uNodeRoot>) {
							if(reference is NodeComponent) {
								uNodeEditor.HighlightNode(reference as NodeComponent);
							} else {
								uNodeEditor.ChangeTarget(reference as INode<uNodeRoot>);
							}
							return;
						}
					}
					return;
				}
			}
			List<ActivityData> datas = new List<ActivityData>();
			foreach(var info in uNodeEditor.SavedData.scriptInformations) {
				string path = info.path.Replace("\\", "/");
				int index = text.IndexOf("at " + path + ":");
				if(index < 0) {
					try {
						path = path.Remove(0, System.IO.Directory.GetCurrentDirectory().Length + 1);
						index = text.IndexOf("at " + path + ":");
					} catch {}
				}
				if(index >= 0) {
					datas.Add(new ActivityData(index, path, info));
				}
			}
			// datas.Sort((x, y) => CompareUtility.Compare(x.number, y.number));
			uNodeEditor.EditorScriptInfo scriptInfo = null;
			int line = 0;
			foreach(var data in datas.OrderByDescending(x => x.number)) {
				string num = "";
				for (int i = data.number + data.path.Length + 4; i < text.Length;i++) {
					var c = text[i];
					if(char.IsNumber(c)) {
						num += c;
					} else {
						break;
					}
				}
				if(int.TryParse(num, out line)) {
					line--;
					scriptInfo = data.info;
				}
			}
			if(scriptInfo != null && uNodeEditor.HighlightNode(scriptInfo, line)) {
				return;
			}
			var uObj = EditorUtility.InstanceIDToObject(instanceID);
			if(uObj is INode<uNodeRoot>) {
				if(uObj is NodeComponent) {
					uNodeEditor.HighlightNode(uObj as NodeComponent);
				} else {
					uNodeEditor.ChangeTarget(uObj as INode<uNodeRoot>);
				}
			}
		}
	}
}