﻿using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace MaxyGames.uNode.Editors {
	public class RefactorWindow : EditorWindow {
		private Vector2 scrollPos;

		void OnGUI() {
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			for (int i = 0; i < data.missingMembers.Count;i++) {
				var mMember = data.missingMembers[i];
				EditorGUILayout.LabelField("Type: " + mMember.type.PrettyName(true));
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(mMember.name);
				mMember.value = EditorGUILayout.TextField(mMember.value);
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.EndScrollView();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Start Refactor")) {
				bool flag = false;
				Func<object, bool> scriptValidation = (obj) => {
					MemberData member = obj as MemberData;
					if (member != null) {
						flag = FindMissingMember(member, true) || flag;
					}
					return false;
				};
				foreach(var target in data.missingTarget) {
					AnalizerUtility.AnalizeObject(target, scriptValidation);
					if(flag) {
						flag = false;
						uNodeGUIUtility.GUIChanged(target);
						if(EditorUtility.IsPersistent(target)) {
							uNodeEditorUtility.MarkDirty(target);
						}
					}
				}
				uNodeEditor.ClearGraphCache();
				uNodeEditor.window?.Refresh(true);
				Close();
			}
		}

		#region Window
		private static RefactorWindow window;

		[MenuItem("Tools/uNode/Advanced/Fix Project Graphs", false, 10002)]
		public static RefactorWindow ShowWindow() {
			window = GetWindow<RefactorWindow>(true);
			window.minSize = new Vector2(300, 250);
			window.titleContent = new GUIContent("Graph Refactor");
			window.Initialize();
			window.Show();
			return window;
		}

		public static RefactorWindow Refactor(UnityEngine.Object obj) {
			window = GetWindow<RefactorWindow>(true);
			window.minSize = new Vector2(300, 250);
			window.titleContent = new GUIContent("Graph Refactor");
			window.Initialize(obj);
			window.Show();
			return window;
		}
		#endregion

		private void Initialize(UnityEngine.Object target) {
			data = new RefactorData();
			GameObject gameObject = target as GameObject;
			if(target is Component) {
				gameObject = (target as Component).gameObject;
			}
			if(gameObject != null) {
				var scripts = gameObject.GetComponentsInChildren<MonoBehaviour>(true);
				Func<object, bool> scriptValidation = (obj) => {
					MemberData member = obj as MemberData;
					if (member != null) {
						FindMissingMember(member, false);
					}
					return false;
				};
				Array.ForEach(scripts, script => {
					data.missingTarget.Add(script);
					AnalizerUtility.AnalizeObject(script, scriptValidation);
				});
			}
		}

		private void Initialize() {
			data = new RefactorData();
			var graphPrefabs = uNodeEditorUtility.FindPrefabsOfType<uNodeRoot>();
			foreach (var prefab in graphPrefabs) {
				var gameObject = prefab;
				if (GraphUtility.HasTempGraphObject(prefab)) {
					gameObject = GraphUtility.GetTempGraphObject(prefab);
				}
				var scripts = gameObject.GetComponentsInChildren<MonoBehaviour>(true);
				Func<object, bool> scriptValidation = (obj) => {
					MemberData member = obj as MemberData;
					if (member != null) {
						FindMissingMember(member, false);
					}
					return false;
				};
				Array.ForEach(scripts, script => {
					data.missingTarget.Add(script);
					AnalizerUtility.AnalizeObject(script, scriptValidation);
				});
			}
		}

		RefactorData data = new RefactorData();

		#region Classes
		class RefactorData {
			public List<Object> missingTarget = new List<Object>();
			public List<MissingMember> missingMembers = new List<MissingMember>();

			public void RegisterMissingMember(Type type, string name) {
				if(!missingMembers.Any(m => m.type == type && m.name == name)) {
					missingMembers.Add(new MissingMember() {
						type = type,
						name = name,
						value = name,
					});
				}
			}

			public string DoRefactor(Type type, string name) {
				var member = missingMembers.FirstOrDefault(m => m.type == type && m.name == name);
				if(member != null) {
					return member.value;
				}
				return name;
			}
		}

		class MissingMember {
			public Type type;
			public string name;
			public string value;
		}
		#endregion

		#region Functions
		bool DoFindMissingMember(Type type, ref string path, MemberData member, bool doRefactor) {
			if(type == null) return false;
			string[] strArray = path.Split(new char[] { '.' });
			MemberInfo[] infoArray = new MemberInfo[strArray.Length - 1];
			for (int i = 0; i < strArray.Length; i++) {
				if (i == 0)
					continue;
				string mName = strArray[i];
				//Event
				EventInfo eventInfo = type.GetEvent(mName, MemberData.flags);
				if (eventInfo != null) {
					infoArray[i - 1] = eventInfo;
					if (i + 1 == strArray.Length)
						break;
					type = eventInfo.EventHandlerType;
					continue;
				}
				//Field
				FieldInfo field = type.GetField(mName, MemberData.flags);
				if (field != null) {
					infoArray[i - 1] = field;
					if (i + 1 == strArray.Length)
						break;
					type = field.FieldType;
					continue;
				}
				//Property
				PropertyInfo property = null;
				try {
					property = type.GetProperty(mName, MemberData.flags);
				} catch (AmbiguousMatchException) {
					property = type.GetProperty(mName, MemberData.flags | BindingFlags.DeclaredOnly);
				}
				if (property != null) {
					infoArray[i - 1] = property;
					if (i + 1 == strArray.Length)
						break;
					type = property.PropertyType;
					continue;
				}
				//Method
				Type[] paramsType = Type.EmptyTypes;
				Type[] genericType = Type.EmptyTypes;
				if (member != null) {
					if (member.SerializedItems?.Length == strArray.Length) {
						paramsType = member.ParameterTypes[i] ?? paramsType;
						genericType = member.GenericTypes[i] ?? genericType;
					}
				}
				MethodInfo method = null;
				if (genericType.Length > 0) {
					bool flag = false;
					bool flag2 = false;
					MethodInfo[] methods = type.GetMethods(MemberData.flags);
					MethodInfo backupMethod = null;
					for (int x = 0; x < methods.Length; x++) {
						method = methods[x] as MethodInfo;
						if (!method.Name.Equals(mName) || !method.IsGenericMethodDefinition) {
							continue;
						}
						if (method.GetGenericArguments().Length == genericType.Length && method.GetParameters().Length == paramsType.Length) {
							if (uNodeUtility.isPlaying) {
								method = method.MakeGenericMethod(genericType);
							} else {
								try {
									method = method.MakeGenericMethod(genericType);
								} catch (Exception ex) {
									Debug.LogException(ex);
								}
							}
							backupMethod = method;//for alternatife when method not found.
							ParameterInfo[] parameters = method.GetParameters();
							bool flag3 = false;
							for (int z = 0; z < parameters.Length; z++) {
								if (parameters[z].ParameterType != paramsType[z]) {
									flag3 = true;
									break;
								}
							}
							if (flag3)
								continue;
							break;
						}
					}
					if (backupMethod != null) {
						infoArray[i - 1] = backupMethod;
						if (i + 1 == strArray.Length) {
							flag2 = true;
							break;
						}
						type = backupMethod.ReturnType;
						flag = true;
					}
					if (flag2)
						break;
					if (flag)
						continue;
				}
				method = type.GetMethod(mName, MemberData.flags, null, paramsType, null);
				if (method != null) {
					if (method.IsGenericMethodDefinition && genericType.Length > 0) {
						if (uNodeUtility.isPlaying) {
							method = method.MakeGenericMethod(genericType);
						} else {
							try {
								method = method.MakeGenericMethod(genericType);
							} catch { continue; }
						}
					}
					infoArray[i - 1] = method;
					if (i + 1 == strArray.Length)
						break;
					type = method.ReturnType;
					continue;
				}
				if (path.EndsWith("ctor")) {
					//Constructor
					ConstructorInfo ctor = type.GetConstructor(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, null, paramsType, null);
					if (ctor == null && paramsType.Length == 0) {
						//
					}
					infoArray[i - 1] = ctor;
					if (i + 1 == strArray.Length)
						break;
					type = ReflectionUtils.GetMemberType(ctor.DeclaringType);
				} else {
					MemberInfo[] members = type.GetMember(mName, MemberData.flags);
					if (members != null && members.Length > 0) {
						infoArray[i - 1] = members[0];
						if (i + 1 == strArray.Length)
							break;
						type = ReflectionUtils.GetMemberType(members[0]);
					} else {
						data.RegisterMissingMember(type, mName);
						if(doRefactor) {
							strArray[i] = data.DoRefactor(type, mName);
							path = string.Join(".", strArray);
						}
						return true;
					}
				}
			}
			return false;
		}

		bool DoFindMissingMember(VariableData variable) {
			return false;
		}

		bool FindMissingMember(MemberData member, bool doRefactor) {
			bool flag = false;
			string refName = member.name;
			switch (member.targetType) {
				case MemberData.TargetType.uNodeVariable:
				case MemberData.TargetType.uNodeGroupVariable:
					if(!member.isDeepTarget)
						break;
					IVariableSystem IVS = member.startTarget as IVariableSystem;
					if(IVS != null) {
						flag = DoFindMissingMember(IVS.GetVariableData(member.startName).Type, ref refName, member, doRefactor);
						member.name = refName;
					} else if(member.startTarget is ILocalVariableSystem) {
						goto case MemberData.TargetType.uNodeLocalVariable;
					}
					break;
				case MemberData.TargetType.uNodeLocalVariable:
					if(!member.isDeepTarget)
						break;
					ILocalVariableSystem LVS = member.startTarget as ILocalVariableSystem;
					if(LVS == null)
						break;
					flag = DoFindMissingMember(LVS.GetLocalVariableData(member.startName).Type, ref refName, member, doRefactor);
					member.name = refName;
					break;
				case MemberData.TargetType.ValueNode:
					if(!member.isDeepTarget)
						break;
					Node VN = member.startTarget as Node;
					flag = DoFindMissingMember(VN.ReturnType(), ref refName, member, doRefactor);
					member.name = refName;
					break;
				case MemberData.TargetType.uNodeProperty:
					if(!member.isDeepTarget)
						break;
					flag = DoFindMissingMember((member.startTarget as IPropertySystem).GetPropertyData(member.startName).ReturnType(), ref refName, member, doRefactor);
					member.name = refName;
					break;
				case MemberData.TargetType.NodeField:
					if(member.startTarget != null) {
						var fieldInfo = member.startTarget.GetType().GetField(member.name, MemberData.flags);
						if(fieldInfo == null) {
							data.RegisterMissingMember(member.startTarget.GetType(), member.name);
							if(doRefactor) {
								member.name = data.DoRefactor(member.startTarget.GetType(), member.name);
							}
							flag = true;
						}
					}
					break;
				case MemberData.TargetType.NodeFieldElement:
					if(member.startTarget != null) {
						var fieldInfo = member.startTarget.GetType().GetField(member.startName.Split('#')[0], MemberData.flags);
						if(fieldInfo == null) {
							var arr = member.startName.Split('#');
							data.RegisterMissingMember(member.startTarget.GetType(), arr[0]);
							if(doRefactor) {
								member.name = string.Join("#", data.DoRefactor(member.startTarget.GetType(), arr[0]));
							}
							flag = true;
						}
					}
					break;
				case MemberData.TargetType.FlowInput:
					if(member.startTarget != null) {
						var fieldInfo = member.startTarget.GetType().GetField(member.name, MemberData.flags);
						if(fieldInfo == null) {
							data.RegisterMissingMember(member.startTarget.GetType(), member.name);
							if(doRefactor) {
								member.name = data.DoRefactor(member.startTarget.GetType(), member.name);
							}
							flag = true;
						}
					}
					break;
				case MemberData.TargetType.uNodeParameter:
					if(!member.isDeepTarget)
						return false;
					flag = DoFindMissingMember((member.startTarget as IParameterSystem).GetParameterData(member.startName).Type, ref refName, member, doRefactor);
					member.name = refName;
					break;
				case MemberData.TargetType.uNodeGenericParameter:
				case MemberData.TargetType.uNodeFunction:
				case MemberData.TargetType.Type:
				case MemberData.TargetType.None:
				case MemberData.TargetType.Null:
				case MemberData.TargetType.SelfTarget:
				case MemberData.TargetType.FlowNode:
				case MemberData.TargetType.Values:
					return false;
				default:
					flag = DoFindMissingMember(member.startType, ref refName, member, doRefactor);
					member.name = refName;
					break;
			}
			return flag;
		}
		#endregion
	}
}