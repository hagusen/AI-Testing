using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MaxyGames.uNode.Editors {
	public partial class ItemSelector : EditorWindow {
		public static ItemSelector window;
		private Data editorData = new Data();
		private List<SearchProgress> progresses;

		#region ShowWindows
		public static ItemSelector ShowWindow(
			UnityEngine.Object targetObject,
			FilterAttribute filter,
			Action<MemberData> selectCallback = null,
			bool onlyGetType = false,
			List<CustomItem> customItems = null) {
			if(window == null) {
				window = ScriptableObject.CreateInstance(typeof(ItemSelector)) as ItemSelector;
			}
			window.targetObject = targetObject;
			window.filter = filter;
			window.onlyGetType = onlyGetType;
			window.Init();
			window.selectCallback = selectCallback;
			if(customItems != null) {
				window.customItems = customItems;
			}
			return window;
		}

		public static ItemSelector ShowWindow(
			UnityEngine.Object targetObject,
			MemberData variable,
			FilterAttribute filter,
			Action<MemberData> selectCallback = null,
			List<CustomItem> customItems = null) {
			if(window == null) {
				window = ScriptableObject.CreateInstance(typeof(ItemSelector)) as ItemSelector;
			}
			window.targetObject = targetObject;
			window.reflectionValue = variable;
			window.filter = filter;
			window.selectCallback = selectCallback;
			window.Init();
			if(customItems != null) {
				window.customItems = customItems;
			}
			return window;
		}

		public static ItemSelector ShowAsNew(
			UnityEngine.Object targetObject,
			FilterAttribute filter,
			Action<MemberData> selectCallback = null,
			bool onlyGetType = false,
			List<CustomItem> customItems = null) {
			ItemSelector window = ScriptableObject.CreateInstance(typeof(ItemSelector)) as ItemSelector;
			window.targetObject = targetObject;
			window.filter = filter;
			window.onlyGetType = onlyGetType;
			window.Init();
			window.selectCallback = selectCallback;
			if(customItems != null) {
				window.customItems = customItems;
			}
			return window;
		}

		public static ItemSelector ShowCustomItem(List<CustomItem> customItems) {
			var win = ShowWindow(null, null, null, null, customItems);
			win.displayDefaultItem = false;
			return win;
		}
		#endregion

		#region UnityEvent
		private void OnDisable() {
			window = null;
			editorData.Dispose();
		}

		private void Update() {
			try {
				var progress = editorData.manager?.searchProgresses;
				if(progress != null && progress.Count > 0) {
					progresses = new List<SearchProgress>(progress);
				}
			}
			catch { }
			Repaint();
			//if(requiredRepaint) {
			//	Repaint();
			//	requiredRepaint = false;
			//}
		}

		private void OnInspectorUpdate() {
			if(requiredRepaint) {
				Repaint();
				requiredRepaint = false;
			}
		}

		static string GetPrettyTreeName(TreeViewItem tree) {
			if(tree is MemberTreeView) {
				var member = (tree as MemberTreeView).member;
				if(member is Type) {
					return (member as Type).PrettyName();
				}
				return member.Name;
			}
			return tree.displayName;
		}

		void DrawToolbar() {
			if(canSearch) {
				Rect rect = uNodeGUIUtility.GetRectCustomHeight(17);
				editorData.searchKind = (SearchKind)GUI.Toolbar(rect, (int)editorData.searchKind, new string[] { "Contains", "Equals", "Ends with", "Start with" }, EditorStyles.radioButton);
				if(!_hasFocus) {
					if(Event.current.type == EventType.Repaint) {
						editorData.searchField.SetFocus();
						_hasFocus = true;
					}
				}
				var search = editorData.searchField.OnToolbarGUI(editorData.searchString);
				if(search != editorData.searchString) {
					editorData.searchString = search;
				}
				if(displayDefaultItem && !filter.OnlyGetType) {
					editorData.searchFilter = (SearchFilter)GUI.Toolbar(uNodeGUIUtility.GetRectCustomHeight(17), (int)editorData.searchFilter, Enum.GetNames(typeof(SearchFilter)), EditorStyles.radioButton);
				}
			}
			if(editorData.manager.isDeep) {
				var items = editorData.manager.deepTrees;
				var lastItem = items.LastOrDefault();
				if(lastItem != null) {
					string fullPath = string.Join(" > ", items.Select(i => GetPrettyTreeName(i)));
					if(items.Count == 1 && lastItem is MemberTreeView) {
						var member = (lastItem as MemberTreeView).member;
						if(member is Type) {
							fullPath = (member as Type).PrettyName();
						} else {
							fullPath = member.DeclaringType.PrettyName() + "." + member.Name;
						}
					}
					EditorGUILayout.BeginHorizontal();
					if(GUILayout.Button(new GUIContent("<-" + lastItem.displayName, fullPath), EditorStyles.miniButton)) {
						items.RemoveAt(items.Count - 1);
						editorData.manager.searchString = string.Empty;
						editorData.manager.Reload();
						editorData.searchField.SetFocus();
						return;
					}
					if(editorData.manager.CanSelectTree(lastItem)) {
						if(GUILayout.Button(new GUIContent("[Select]"), EditorStyles.miniButton)) {
							editorData.manager.SelectDeepTrees();
							return;
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			}
		}

		void HandleEvent(Event evt) {
			if(!editorData.searchField.HasFocus()) {
				if(evt.isKey && evt.type == EventType.KeyUp) {
					if(evt.modifiers == EventModifiers.Control && evt.keyCode == KeyCode.A) {
						editorData.searchField.SetFocus();
						evt.Use();
					} else {
						var key = evt.keyCode.ToString();
						if(key.Length == 1) {
							if(evt.shift || evt.capsLock) {
								editorData.searchString += key;
							} else {
								editorData.searchString += key.ToLower();
							}
							evt.Use();
						} else if(evt.keyCode == KeyCode.Backspace) {
							var str = editorData.searchString;
							if(str.Length > 0) {
								editorData.searchString = str.RemoveLast();
								evt.Use();
							}
						} else if(evt.keyCode == KeyCode.Space) {
							editorData.searchString += " ";
							evt.Use();
						}
					}
				}
			}
		}

		bool hasSetupMember = false;
		void OnGUI() {
			if(!XmlDoc.hasLoadDoc && preferenceData.showDocumentation) {
				XmlDoc.LoadDocInBackground();
			}
			if(hasSetupMember) {
				HandleEvent(Event.current);
				DrawToolbar();
				if(!editorData.manager.isReloading) {
					Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
					editorData.manager.OnGUI(rect);
				} else {
					if(progresses != null && progresses.Count > 0) {
						for(int i=0; i<progresses.Count; i++) {
							if(progresses[i] == null || i >= 4) {
								break;
							}
							EditorGUI.ProgressBar(uNodeGUIUtility.GetRect(), progresses[i].progress, progresses[i].info);
						}
					} else {
						EditorGUI.ProgressBar(uNodeGUIUtility.GetRect(), editorData.setup.progress, "Searching Items");
					}
				}
			} else {
				EditorGUI.ProgressBar(uNodeGUIUtility.GetRect(), editorData.setup.progress, "Loading Types");
			}
		}
		#endregion

		#region Show Function
		static uNodePreference.PreferenceData _preferenceData;
		static uNodePreference.PreferenceData preferenceData {
			get {
				if(_preferenceData != uNodePreference.GetPreference()) {
					_preferenceData = uNodePreference.GetPreference();
				}
				return _preferenceData;
			}
		}
		#endregion
	}
}