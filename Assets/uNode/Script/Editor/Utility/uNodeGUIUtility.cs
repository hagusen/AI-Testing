using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using UnityEditor.Experimental.GraphView;

namespace MaxyGames.uNode.Editors {
	/// <summary>
	/// Provides useful Utility for GUI
	/// </summary>
	public static class uNodeGUIUtility {
		/// <summary>
		/// An event to be called when GUI has been changed.
		/// Note: this will be executed in the next frame after gui changed
		/// </summary>
		public static event Action<UnityEngine.Object> onGUIChanged;

		public static void GUIChanged(UnityEngine.Object owner) {
			if(owner != null) {
				uNodeThreadUtility.ExecuteOnce(() => {
					onGUIChanged?.Invoke(owner);
				}, "UNODE_GUI_CHANGED_CALLBACK:" + owner.GetInstanceID());
			}
		}

		public static void EditRuntimeTypeValue(Rect position, GUIContent label, object value, RuntimeType type, Action<object> onChange, bool allowSceneObject) {
			if(label != GUIContent.none) {
				position = EditorGUI.PrefixLabel(position, label);
			}
			string name;
			if(value != null) {
				if(value as Component) {
					name = (value as Component).gameObject.name + $" ({type.Name})";
				} else if(value as ScriptableObject) {
					name = (value as ScriptableObject).name + $"({type.Name})";
				} else {
					name = value.ToString();
				}
			} else {
				name = $"None ({type.Name})";
			}
#if UNITY_2019_4_OR_NEWER
			position.width -= 20;
			if(EditorGUI.DropdownButton(position, new GUIContent(name, uNodeEditorUtility.GetTypeIcon(type)), FocusType.Keyboard, EditorStyles.objectField)) {
				if(value is UnityEngine.Object uobj) {
					if(Event.current.clickCount == 2) {
						Selection.activeObject = uobj;
					} else {
						if(uobj is Component comp) {
							EditorGUIUtility.PingObject(comp.gameObject);
						} else {
							EditorGUIUtility.PingObject(uobj);
						}
					}
				}
			}
			if(GUI.Button(new Rect(position.x + position.width, position.y, 20, position.height), GUIContent.none, Styles.objectField)) {
				var items = new List<ItemSelector.CustomItem>();
				items.Add(new ItemSelector.CustomItem("None", () => {
					if(onChange != null) {
						onChange(null);
					}
				}, "#"));
				items.AddRange(ItemSelector.MakeCustomItemsForInstancedType(type, (val) => {
					if(onChange != null) {
						onChange(val);
					}
				}, allowSceneObject));
				ItemSelector.ShowCustomItem(items).ChangePosition(position.ToScreenRect());
			}
#else
			if(EditorGUI.DropdownButton(position, new GUIContent(name, uNodeEditorUtility.GetTypeIcon(type)), FocusType.Keyboard, Styles.objectField)) {
				position.x += position.width - 20;
				position.width = 20;
				if(position.Contains(Event.current.mousePosition)) {
					var items = new List<ItemSelector.CustomItem>();
					items.Add(new ItemSelector.CustomItem("None", () => {
						if(onChange != null) {
							onChange(null);
						}
					}, "#"));
					items.AddRange(ItemSelector.MakeCustomItemsForInstancedType(type, (val) => {
						if(onChange != null) {
							onChange(val);
						}
					}, allowSceneObject));
					ItemSelector.ShowCustomItem(items).ChangePosition(position.ToScreenRect());
				} else if(value is UnityEngine.Object uobj) {
					if(Event.current.clickCount == 2) {
						Selection.activeObject = uobj;
					} else {
						if (uobj is Component comp) {
							EditorGUIUtility.PingObject(comp.gameObject);
						} else {
							EditorGUIUtility.PingObject(uobj);
						}
					}
				}
			}
#endif
			uNodeEditorUtility.GUIDropArea(position,
				() => {
					if(DragAndDrop.objectReferences.Length != 1) {
						return;
					}
					var dragObj = DragAndDrop.objectReferences[0];
					if(dragObj is GameObject) {
						foreach(var c in (dragObj as GameObject).GetComponents<MonoBehaviour>()) {
							if(ReflectionUtils.IsValidRuntimeInstance(c, type)) {
								dragObj = c;
								break;
							}
						}
					}
					if(!(dragObj is GameObject) && ReflectionUtils.IsValidRuntimeInstance(dragObj, type)) {
						if(onChange != null) {
							onChange(dragObj);
						}
					} else {
						uNodeEditorUtility.DisplayErrorMessage("Invalid dragged object.");
						DragAndDrop.objectReferences = new UnityEngine.Object[0];
					}
				},
				() => {
					GUI.DrawTexture(position, uNodeEditorUtility.MakeTexture(1, 1, new Color(0, 0.5f, 1, 0.5f)));
				});
		}

		/// <summary>
		/// True if type can be edited.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsSupportedType(Type type) {
			if(type == typeof(UnityEngine.Object) || type.IsSubclassOf(typeof(UnityEngine.Object))) {
				return true;
			}
			if(!type.IsInterface && !type.IsAbstract) {
				return type == typeof(string) || type == typeof(Enum) || type.IsSubclassOf(typeof(Enum)) || type == typeof(MemberData) || type == typeof(MultipurposeMember) || type == typeof(AnimationCurve) || type.IsValueType;
			}
			return false;
		}

		public static void EditVariableValue(VariableData variable, UnityEngine.Object target, bool drawDecorator = true) {
			if(drawDecorator) {
				FieldDecorator.DrawDecorators(variable.GetAttributes());
			}
			string varName = ObjectNames.NicifyVariableName(variable.Name);
			System.Type type = variable.Type;
			if(type != null) {
				if(type is RuntimeType) {
					EditRuntimeTypeValue(GetRect(), new GUIContent(varName), variable.variable, type as RuntimeType, (val) => {
						uNodeEditorUtility.RegisterUndo(target, "");
						variable.variable = val;
						variable.Serialize();
					}, uNodeEditorUtility.IsSceneObject(target));
				} else if(type.IsArray) {
					if(IsSupportedType(type.GetElementType()) || type.GetElementType().IsClass) {
						if(variable.variable != null && !variable.variable.GetType().IsCastableTo(type)) {
							variable.variable = null;
							variable.Serialize();
						}
						ShowField(new GUIContent(varName), variable.GetType().GetField("variable"), variable, target, new ObjectTypeAttribute(type), new uNodeUtility.EditValueSettings() {
							drawDecorator = false,
						});
					}
				} else if(type.IsGenericType) {
					if(variable.variable != null && !variable.variable.GetType().IsCastableTo(type)) {
						variable.variable = null;
						variable.Serialize();
					}
					ShowField(new GUIContent(varName), variable.GetType().GetField("variable"), variable, target, new ObjectTypeAttribute(type), new uNodeUtility.EditValueSettings() {
						drawDecorator = false,
					});
				} else if(IsSupportedType(type) || type.IsClass) {
					if(type == typeof(object) && !object.ReferenceEquals(variable.variable, null)) {
						type = variable.variable.GetType();
					}
					if(variable.variable != null && !variable.variable.GetType().IsCastableTo(type)) {
						variable.variable = null;
						variable.Serialize();
					}
					ShowField(new GUIContent(varName), variable.GetType().GetField("variable"), variable, new ObjectTypeAttribute(type), new uNodeUtility.EditValueSettings() {
						attributes = variable.GetAttributes(),
						unityObject = target,
						drawDecorator = false,
					});
				}
			} else {
				var position = EditorGUI.PrefixLabel(GetRect(), new GUIContent(varName));
				EditorGUI.HelpBox(position, "Type not found", MessageType.Error);
			}
		}

		#region VariableUtility
		private static VariableData selectedVar;

		public static ActionPopupWindow ShowRenameVariableWindow(Rect position, VariableData variable, List<VariableData> listVariable, UnityEngine.Object target, Action<VariableData, string> onRename = null, Action onApply = null) {
			var win = ActionPopupWindow.ShowWindow(position,
				new object[] { variable.Name, variable },
				delegate (ref object obj) {
					object[] o = obj as object[];
					o[0] = EditorGUILayout.TextField("Variable Name", o[0] as string);
				}, null, (ActionRef<object>)delegate (ref object obj) {
					if(GUILayout.Button("Apply") || Event.current.keyCode == KeyCode.Return) {
						object[] o = obj as object[];
						o[0] = uNodeUtility.AutoCorrectName(o[0] as string);
						string varName = o[0] as string;
						bool hasVariable = false;
						if(listVariable != null && listVariable.Count > 0) {
							foreach(VariableData V in listVariable) {
								if(V.Name == varName) {
									hasVariable = true;
									break;
								}
							}
						}
						if(!hasVariable) {
							VariableData V = o[1] as VariableData;
							Undo.SetCurrentGroupName("Rename Variable: " + V.Name);
							uNodeEditorUtility.RegisterFullHierarchyUndo(target is Component ? (target as Component).gameObject : target, "Rename Variable: " + V.Name);
							if(onRename != null) {
								onRename(variable, varName);
							}
							if(onRename == null) {
								RefactorUtility.RefactorVariable(V, varName, target);
							}
							V.Name = varName;
							if(onApply != null) {
								onApply();
							}
							GUIChanged(target);
						}
						ActionPopupWindow.CloseLast();
					}
				});
			win.headerName = "Rename Variable";
			return win;
		}

		public static void DrawVariable(VariableData variable,
			UnityEngine.Object target,
			bool autoInitializeDefaultType = false,
			List<VariableData> listVariable = null,
			bool isLocalVariable = true,
			Action<VariableData, string> onRename = null) {

			#region Variable Names
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel(new GUIContent("Name"));
			Rect vRect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label);
			if(EditorGUI.DropdownButton(vRect, new GUIContent(variable.Name), FocusType.Keyboard)) {
				ShowRenameVariableWindow(vRect, variable, listVariable, target, onRename);
			}
			EditorGUILayout.EndHorizontal();
			#endregion

			//variable.Name = EditorGUILayout.TextField(new GUIContent("Name", ""), variable.Name);
			Type type = variable.Type;
			if(type != null) {
				if(variable.type.targetType == MemberData.TargetType.uNodeGenericParameter) {
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel("Value");
					if(EditorGUILayout.DropdownButton(new GUIContent(variable.variable != null ? "default()" : "null", ""), FocusType.Keyboard, EditorStyles.miniButton)) {
						variable.variable = variable.variable == null ? new object() : null;
						GUIChanged(target);
					}
					EditorGUILayout.EndHorizontal();
				} else {
					bool showNull = false;
					if(type.IsCastableTo(typeof(UnityEngine.Object))) {
						if(target is uNodeRoot graph) {
							if(!graph.IsRuntimeGraph()) {
								showNull = true;
							}
						} else if(target is INode<uNodeRoot> node) {
							if(!node.GetOwner().IsRuntimeGraph()) {
								showNull = true;
							}
						}
					}
					if(showNull) {
						EditorGUILayout.BeginHorizontal();
						EditorGUILayout.PrefixLabel("Value");
						if(EditorGUILayout.DropdownButton(new GUIContent("null"), FocusType.Keyboard, EditorStyles.miniButton)) {

						}
						EditorGUILayout.EndHorizontal();
					} else if(IsSupportedType(type) && autoInitializeDefaultType) {
						if(variable.variable != null && !variable.variable.GetType().IsCastableTo(type)) {
							variable.variable = null;
							variable.Serialize();
						}
						ShowField(new GUIContent("Value"), variable.GetType().GetField("variable"), variable, target, new ObjectTypeAttribute(type));
					} else if(ReflectionUtils.CanCreateInstance(type) || !autoInitializeDefaultType && IsSupportedType(type)) {
						if(variable.variable == null && type.IsValueType) {
							variable.variable = ReflectionUtils.CreateInstance(type);
						}
						if(object.ReferenceEquals(variable.variable, null)) {
							EditorGUILayout.BeginHorizontal();
							EditorGUILayout.PrefixLabel("Value");
							if(EditorGUILayout.DropdownButton(new GUIContent("null", "Click to create new instance"), FocusType.Keyboard, EditorStyles.miniButton)) {
								variable.variable = ReflectionUtils.CreateInstance(type);
							}
							EditorGUILayout.EndHorizontal();
						} else {
							if(type == typeof(object) && !object.ReferenceEquals(variable.variable, null)) {
								type = variable.variable.GetType();
							}
							if(variable.variable != null && !variable.variable.GetType().IsCastableTo(type)) {
								variable.variable = null;
								variable.Serialize();
							}
							ShowField(new GUIContent("Value"), variable.GetType().GetField("variable"), variable, target, new ObjectTypeAttribute(type));
							if(type.IsValueType) {
								EditorGUILayout.BeginHorizontal();
								EditorGUILayout.PrefixLabel("Destroy Instance");
								if(EditorGUILayout.DropdownButton(new GUIContent("Destroy", "Make this variable to null"), FocusType.Keyboard, EditorStyles.miniButton)) {
									uNodeEditorUtility.RegisterUndo(target, "Destroy Instance of Variable: " + variable.Name);
									variable.variable = null;
									variable.Serialize();
									GUIChanged(target);
								}
								EditorGUILayout.EndHorizontal();
							}
						}
					}
				}
			}
			if(type != null) {//Type
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(new GUIContent("Type", variable.type.DisplayName(false, false)));
				Rect rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label);
				if(EditorGUI.DropdownButton(rect, new GUIContent(variable.type.DisplayName(false, false), uNodeEditorUtility.GetTypeIcon(type)), FocusType.Keyboard)) {
					FilterAttribute filter = new FilterAttribute();
					filter.OnlyGetType = true;
					selectedVar = variable;
					if(Event.current.button == 0) {
						TypeSelectorWindow.ShowAsNew(rect, filter, delegate (MemberData[] members) {
							uNodeEditorUtility.RegisterUndo(target, "Change Variable Type: " + selectedVar.type);
							selectedVar.type = members[0];
							selectedVar.variable = null;
							GUIChanged(target);
						}, variable.type).targetObject = target;
					} else {
						CommandWindow.CreateWindow(GUIToScreenRect(rect), (items) => {
							var member = CompletionEvaluator.CompletionsToMemberData(items);
							if(member != null) {
								uNodeEditorUtility.RegisterUndo(target, "Change Variable Type: " + selectedVar.type);
								selectedVar.type = member;
								selectedVar.variable = null;
								GUIChanged(target);
								return true;
							}
							return false;
						}, new CompletionEvaluator.CompletionSetting() {
							validCompletionKind = CompletionKind.Type | CompletionKind.Namespace | CompletionKind.Keyword,
						});
					}
				}
				EditorGUILayout.EndHorizontal();
			} else {
				EditorGUILayout.HelpBox("Missing type can't edited.", MessageType.Error);
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(new GUIContent("Object Type", variable.type.DisplayName(false, false)));
				Rect rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.label);
				if(EditorGUI.DropdownButton(rect, new GUIContent("Missing Type", variable.type.name), FocusType.Keyboard)) {
					FilterAttribute filter = new FilterAttribute();
					filter.OnlyGetType = true;
					selectedVar = variable;
					if(Event.current.button == 0) {
						TypeSelectorWindow.ShowAsNew(rect, filter, delegate (MemberData[] members) {
							uNodeEditorUtility.RegisterUndo(target, "Change Variable Type: " + selectedVar.type);
							selectedVar.type = members[0];
							selectedVar.variable = null;
							selectedVar.Serialize();
							GUIChanged(target);
						}, variable.type).targetObject = target;
					} else {
						CommandWindow.CreateWindow(GUIToScreenRect(rect), (items) => {
							var member = CompletionEvaluator.CompletionsToMemberData(items);
							if(member != null) {
								uNodeEditorUtility.RegisterUndo(target, "Change Variable Type: " + selectedVar.type);
								selectedVar.type = member;
								selectedVar.variable = null;
								selectedVar.Serialize();
								GUIChanged(target);
								return true;
							}
							return false;
						}, new CompletionEvaluator.CompletionSetting() {
							validCompletionKind = CompletionKind.Type | CompletionKind.Namespace | CompletionKind.Keyword,
						});
					}
				}
				EditorGUILayout.EndHorizontal();
			}
			if(!isLocalVariable) {
				ShowField(new GUIContent("Modifier"), variable.GetType().GetField(nameof(variable.modifier)), variable, target);
			}
			ShowField(new GUIContent("Summary"), variable.GetType().GetField(nameof(variable.summary)), variable, target);
			if(!isLocalVariable) {
				VariableEditorUtility.DrawAttribute(variable.attributes, target, (a) => {
					variable.attributes = a.ToArray();
					GUIChanged(target);
				}, variable.modifier.Event ? AttributeTargets.Event : AttributeTargets.Field);
			} else {
				ShowField(new GUIContent("Reset On Enter"), variable.GetType().GetField(nameof(variable.resetOnEnter)), variable, target);
			}
		}

		public static void AnalizeVariable(VariableData variable, uNodeRoot targetEvent) {
			targetEvent.Refresh();
			if(targetEvent.nodes.Length > 0) {
				int totalReferenceCount = 0;
				foreach(Node node in targetEvent.nodes) {
					if(node == null)
						continue;
					int referenceCount = 0;
					FindVariableReference(node, variable, targetEvent, delegate (MemberData obj) {
						referenceCount++;
						totalReferenceCount++;
					});
					if(referenceCount > 0)
						Debug.Log(variable.Name + " variable contains " + referenceCount + " reference in Node : " + node.gameObject.name, node);
				}
				Debug.Log("Total reference in " + variable.Name + " = " + totalReferenceCount + " reference.");
			}
		}

		public static void FindVariableReference(object obj, VariableData variable, uNodeRoot targetEvent, Action<MemberData> OnFound) {
			if(object.ReferenceEquals(obj, null) || variable == null || targetEvent == null)
				return;
			if(obj is MemberData) {
				MemberData member = obj as MemberData;
				if(member != null && member.GetInstance() as UnityEngine.Object == targetEvent && member.startName == variable.Name) {
					if(OnFound != null) {
						OnFound(member);
					}
				}
				return;
			}
			if(obj is EventData) {
				EventData member = obj as EventData;
				if(member != null && member.blocks.Count > 0) {
					foreach(EventActionData action in member.blocks) {
						FindVariableReference(action.block, variable, targetEvent, OnFound);
					}
				}
				return;
			}
			BindingFlags flags = BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance;
			FieldInfo[] fieldInfo = ReflectionUtils.GetFields(obj, flags);
			foreach(FieldInfo field in fieldInfo) {
				Type fieldType = field.FieldType;
				if(fieldType.IsAbstract || !fieldType.IsClass)
					continue;
				object value = field.GetValueOptimized(obj);
				if(object.ReferenceEquals(value, null))
					continue;
				if(fieldType == typeof(UnityEngine.Object) || fieldType.IsSubclassOf(typeof(UnityEngine.Object)) || value is UnityEngine.Object) {
					continue;
				}
				if(fieldType == typeof(MemberData)) {
					MemberData member = value as MemberData;
					if(member != null && member.instance as UnityEngine.Object == targetEvent && member.startName == variable.Name) {
						if(OnFound != null) {
							OnFound(member);
						}
					}
					continue;
				}
				if(fieldType == typeof(EventData)) {
					EventData member = value as EventData;
					if(member != null && member.blocks.Count > 0) {
						foreach(EventActionData action in member.blocks) {
							FindVariableReference(action.block, variable, targetEvent, OnFound);
						}
					}
					continue;
				}
				if(!fieldType.IsArray && !fieldType.IsGenericType) {
					FindVariableReference(value, variable, targetEvent, OnFound);
					continue;
				}
				if(fieldType.IsArray && fieldType.GetElementType().IsClass
					|| fieldType.IsGenericType && (fieldType.GetGenericArguments()[0].IsClass)) {
					IList list = value as IList;
					if(list == null)
						continue;
					for(int i = 0; i < list.Count; i++) {
						object element = list[i];
						if(element == null)
							continue;
						Type elementType = element.GetType();
						if(elementType.IsAbstract || !elementType.IsClass)
							break;
						if(elementType == typeof(UnityEngine.Object) || elementType.IsSubclassOf(typeof(UnityEngine.Object)) || element is UnityEngine.Object) {
							continue;
						}
						if(elementType == typeof(MemberData)) {
							MemberData member = element as MemberData;
							if(member != null && member.GetInstance() as UnityEngine.Object == targetEvent && member.startName == variable.Name) {
								if(OnFound != null) {
									OnFound(member);
								}
							}
							continue;
						}
						if(elementType == typeof(EventData)) {
							EventData member = element as EventData;
							if(member != null && member.blocks.Count > 0) {
								foreach(EventActionData action in member.blocks) {
									FindVariableReference(action.block, variable, targetEvent, OnFound);
								}
							}
							continue;
						}
						if(!elementType.IsArray && !elementType.IsGenericType) {
							FindVariableReference(element, variable, targetEvent, OnFound);
							continue;
						}
					}
				}
			}
		}

		public static void DrawVariablesInspector(List<VariableData> variables, UnityEngine.Object target, string header = "Variables", bool publicOnly = true) {
			bool hasVariableToShow = false;
			foreach(VariableData var in variables) {
				if(!publicOnly || var.showInInspector) {
					hasVariableToShow = true;
					break;
				}
			}
			GUI.changed = false;
			if(hasVariableToShow) {
				if(header != null) {
					EditorGUILayout.BeginVertical("Box");
					EditorGUILayout.LabelField(header, EditorStyles.boldLabel);
				}
				foreach(VariableData variable in variables) {
					if(!publicOnly || variable.showInInspector) {
						EditVariableValue(variable, target);
					}
				}
				if(header != null) {
					EditorGUILayout.EndVertical();
				}
			}
			if(GUI.changed) {
				uNodeEditorUtility.MarkDirty(target);
				GUIChanged(target);
			}
		}
		#endregion

		public static void DrawList<T>(GUIContent label, List<T> list, UnityEngine.Object targetObject, Action<List<T>> action, Action<List<T>> onAddClick = null, Func<T, int, GUIContent> funcGetElementLabel = null, bool enableMoveElement = true) {
			if(list == null) {
				list = new List<T>();
				if(action != null) {
					action(list);
				}
			}
			Rect rect = GetRect("PreButton");
			Rect addRect = new Rect(rect.x + rect.width - 20, rect.y, 20, rect.height);
			if(!addRect.Contains(Event.current.mousePosition) || onAddClick == null) {
				if(GUI.Button(rect, label, (GUIStyle)"PreButton")) {

				}
			} else {
				GUI.Box(rect, label, (GUIStyle)"PreButton");
			}
			if(onAddClick != null) {
				if(GUI.Button(addRect, new GUIContent(""), (GUIStyle)"OL Plus")) {
					onAddClick(list);
				}
			}
			for(int i = 0; i < list.Count; i++) {
				T element = list[i];
				Rect aRect = GetRect("minibutton", GUILayout.Height(15));
				if(aRect.Contains(Event.current.mousePosition) && Event.current.button == 0 && Event.current.clickCount == 2) {
					FieldsEditorWindow.ShowWindow(list[i], targetObject, delegate (object obj) {
						return list[(int)obj];
					}, i);
				}
				GUIContent elementLabel = null;
				if(funcGetElementLabel != null) {
					GUIContent content = funcGetElementLabel(element, i);
					if(content != null) {
						elementLabel = content;
					}
				}
				if(elementLabel == null) {
					elementLabel = new GUIContent("Element");
				}
				if(GUI.Button(aRect, elementLabel, "minibutton")) {
					if(Event.current.button == 1) {
						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent("Remove"), false, delegate (object obj) {
							uNodeEditorUtility.RegisterUndo(targetObject, "Remove List Element");
							if(action != null) {
								action(list);
							}
						}, element);
						menu.AddSeparator("");
						if(enableMoveElement) {
							menu.AddItem(new GUIContent("Move To Top"), false, delegate (object obj) {
								if((int)obj != 0) {
									uNodeEditorUtility.RegisterUndo(targetObject, "Move List To Top");
									uNodeEditorUtility.ListMoveToTop(list, (int)obj);
									if(action != null) {
										action(list);
									}
								}
							}, i);
							menu.AddItem(new GUIContent("Move Up"), false, delegate (object obj) {
								if((int)obj != 0) {
									uNodeEditorUtility.RegisterUndo(targetObject, "Move List Up");
									uNodeEditorUtility.ListMoveUp(list, (int)obj);
									if(action != null) {
										action(list);
									}
								}
							}, i);
							menu.AddItem(new GUIContent("Move Down"), false, delegate (object obj) {
								if((int)obj != list.Count - 1) {
									uNodeEditorUtility.RegisterUndo(targetObject, "Move List Down");
									uNodeEditorUtility.ListMoveDown(list, (int)obj);
									if(action != null) {
										action(list);
									}
								}
							}, i);
							menu.AddItem(new GUIContent("Move To Bottom"), false, delegate (object obj) {
								if((int)obj != list.Count - 1) {
									uNodeEditorUtility.RegisterUndo(targetObject, "Move List To Bottom Attribute");
									uNodeEditorUtility.ListMoveToBottom(list, (int)obj);
									if(action != null) {
										action(list);
									}
								}
							}, i);
						}
						menu.ShowAsContext();
					}
				}
			}
		}

		#region GUIUtility
		public static void DrawInterfaceData(InterfaceData data, UnityEngine.Object owner = null) {
			if(data == null) {
				throw new ArgumentNullException("data");
			}
			ShowField("name", data, owner);
			ShowField("summary", data, owner);
			ShowField("modifiers", data, owner);
			//VariableEditorUtility.DrawAttribute(data.attributes, owner, (att) => {
			//	data.attributes = att;
			//});
			//VariableEditorUtility.DrawGenericParameter(data.genericParameters, owner, (gpd) => {
			//	data.genericParameters = gpd;
			//});
			VariableEditorUtility.DrawInterfaceFunction(data.functions, owner, (func) => {
				data.functions = func.ToArray();
				GUIChanged(owner);
			});
			VariableEditorUtility.DrawInterfaceProperty(data.properties, owner, (prop) => {
				data.properties = prop.ToArray();
				GUIChanged(owner);
			});
		}
		public static void DrawEnumData(EnumData data, UnityEngine.Object owner = null) {
			if(data == null) {
				throw new ArgumentNullException("data");
			}
			ShowField("name", data, owner);
			ShowField("inheritFrom", data, owner);
			ShowField("modifiers", data, owner);
			ShowField("enumeratorList", data, owner);
		}

		public static void DrawConstructorInitializer(ValueData value, Type type, UnityEngine.Object unityObject = null, bool preferMemberData = false) {
			if(value == null || type == null)
				return;
			ConstructorValueData cVal = value.Value as ConstructorValueData;
			if(cVal == null || cVal.type != type) {
				cVal = new ConstructorValueData(type);
				value.Value = cVal;
			}
			DrawConstructorInitializer(cVal, unityObject, preferMemberData);
		}

		public static void DrawConstructorInitializer(ConstructorValueData cVal, UnityEngine.Object unityObject = null, bool preferMemberData = false) {
			Type type = cVal.type;
			if(type == null || type.IsPrimitive || type == typeof(decimal) || type == typeof(string) || type is RuntimeType)
				return;
			EditorGUILayout.BeginVertical("Box");
			EditorGUILayout.LabelField("Initializer", EditorStyles.boldLabel);
			if(cVal.initializer.Length > 0) {
				foreach(var val in cVal.initializer) {
					if(val == null)
						continue;
					var vType = val.type;
					string vName = ObjectNames.NicifyVariableName(val.name);
					if(vType != null) {
						if(vType.IsArray) {
							if(IsSupportedType(vType.GetElementType()) || vType.GetElementType().IsClass) {
								if(val.value != null && !val.value.GetType().IsCastableTo(vType)) {
									val.value = null;
								}
								ShowField(new GUIContent(vName), val.GetType().GetField("value"), val, unityObject, new ObjectTypeAttribute(vType));
							}
						} else if(vType.IsGenericType) {
							Type[] genericType = vType.GetGenericArguments();
							if(genericType.Length == 1 && IsSupportedType(genericType[0])) {
								if(val.value != null && !val.value.GetType().IsCastableTo(vType)) {
									val.value = null;
								}
								ShowField(new GUIContent(vName), val.GetType().GetField("value"), val, unityObject, new ObjectTypeAttribute(vType));
							}
						} else if(IsSupportedType(vType) || vType.IsClass) {
							if(!(val.value is MemberData) && !preferMemberData) {
								if(vType == typeof(object) && !object.ReferenceEquals(val.value, null)) {
									vType = val.value.GetType();
								}
								if(val.value != null && !val.value.GetType().IsCastableTo(vType)) {
									val.value = null;
								}
								ShowField(new GUIContent(vName), val.GetType().GetField("value"), val, unityObject, new ObjectTypeAttribute(vType));
							} else {
								if(val.value == null) {
									if(ReflectionUtils.CanCreateInstance(vType)) {
										val.value = new MemberData(ReflectionUtils.CreateInstance(vType));
									} else {
										val.value = MemberData.none;
									}
								}
								EditValueLayouted(new GUIContent(vName), val.value, typeof(MemberData), (o) => {
									uNodeEditorUtility.RegisterUndo(unityObject, "value");
									val.value = o;
									GUIChanged(unityObject);
								}, new uNodeUtility.EditValueSettings() {
									attributes = new object[] { new ObjectTypeAttribute(vType) }
								});
							}
						}
					} else {
						var position = EditorGUI.PrefixLabel(GetRect(), new GUIContent(vName));
						EditorGUI.HelpBox(position, "Type not found", MessageType.Error);
					}
				}
			} else {
				EditorGUILayout.LabelField("No initializer");
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button(new GUIContent("Refresh", ""), EditorStyles.miniButtonLeft)) {
				var init = cVal.initializer;
				if(cVal.type.IsArray || cVal.type.IsCastableTo(typeof(IList))) {
					for(int i = 0; i < init.Length; i++) {
						init[i].name = "Element" + i;
					}
				} else {
					var fields = cVal.type.GetMembers();
					for(int x = 0; x < fields.Length; x++) {
						var field = fields[x];
						var t = ReflectionUtils.GetMemberType(field);
						for(int y = 0; y < init.Length; y++) {
							if(field.Name == init[y].name) {
								if(t != init[y].type) {
									init[y] = new ParameterValueData() { name = field.Name, typeData = MemberData.CreateFromType(t) };
								}
								break;
							}
						}
					}
				}
				GUIChanged(unityObject);
			}
			if(GUILayout.Button(new GUIContent("Fields", ""), EditorStyles.miniButtonMid)) {
				var init = cVal.initializer;
				bool hasAddMenu = false;
				GenericMenu menu = new GenericMenu();
				if(cVal.type.IsArray || cVal.type.IsCastableTo(typeof(IList))) {
					menu.AddItem(new GUIContent("Add Field"), false, delegate (object obj) {
						uNodeEditorUtility.RegisterUndo(unityObject, "Add Field");
						var ctor = obj as ConstructorValueData;
						uNodeUtility.AddArray(ref ctor.initializer, new ParameterValueData() {
							name = "Element",
							typeData = MemberData.CreateFromType(cVal.type.ElementType())
						});
						for(int i = 0; i < ctor.initializer.Length; i++) {
							ctor.initializer[i].name = "Element" + i;
						}
						GUIChanged(unityObject);
					}, cVal);
					foreach(var v in init) {
						menu.AddItem(new GUIContent("Remove Field/" + v.name), false, delegate (object obj) {
							uNodeEditorUtility.RegisterUndo(unityObject, "Remove Field:" + v.name);
							var ctor = (obj as object[])[0] as ConstructorValueData;
							uNodeUtility.RemoveArray(ref ctor.initializer, (obj as object[])[1] as ParameterValueData);
							for(int i = 0; i < init.Length; i++) {
								init[i].name = "Element" + i;
							}
							GUIChanged(unityObject);
						}, new object[] { cVal, v });
					}
				} else {
					var fields = cVal.type.GetMembers();
					foreach(var vv in fields) {
						if(vv is FieldInfo || vv is PropertyInfo && (vv as PropertyInfo).CanWrite && (vv as PropertyInfo).GetIndexParameters().Length == 0) {
							bool valid = true;
							foreach(var v in init) {
								if(v.name == vv.Name) {
									valid = false;
									break;
								}
							}
							if(valid) {
								hasAddMenu = true;
								break;
							}
						}
					}
					if(hasAddMenu) {
						menu.AddItem(new GUIContent("Add All Fields"), false, delegate (object obj) {
							var ctor = obj as ConstructorValueData;
							foreach(var v in fields) {
								if(v is FieldInfo || v is PropertyInfo && (v as PropertyInfo).CanWrite && (v as PropertyInfo).GetIndexParameters().Length == 0) {
									var t = ReflectionUtils.GetMemberType(v);
									bool valid = true;
									foreach(var vv in ctor.initializer) {
										if(v.Name == vv.name) {
											valid = false;
											break;
										}
									}
									if(valid) {
										uNodeEditorUtility.RegisterUndo(unityObject, "");
										uNodeUtility.AddArray(ref ctor.initializer, new ParameterValueData() {
											name = v.Name,
											typeData = MemberData.CreateFromType(t)
										});
									}
								}
							}
							GUIChanged(unityObject);
						}, cVal);
					}
					foreach(var v in fields) {
						if(v is FieldInfo || v is PropertyInfo && (v as PropertyInfo).CanWrite && (v as PropertyInfo).GetIndexParameters().Length == 0) {
							var t = ReflectionUtils.GetMemberType(v);
							bool valid = true;
							foreach(var vv in init) {
								if(v.Name == vv.name) {
									valid = false;
									break;
								}
							}
							if(valid) {
								menu.AddItem(new GUIContent("Add Field/" + v.Name), false, delegate (object obj) {
									uNodeEditorUtility.RegisterUndo(unityObject, "Add Field:" + v.Name);
									var ctor = obj as ConstructorValueData;
									uNodeUtility.AddArray(ref ctor.initializer, new ParameterValueData() {
										name = v.Name,
										typeData = MemberData.CreateFromType(t)
									});
									GUIChanged(unityObject);
								}, cVal);
							}
						}
					}
					foreach(var v in init) {
						menu.AddItem(new GUIContent("Remove Field/" + v.name), false, delegate (object obj) {
							uNodeEditorUtility.RegisterUndo(unityObject, "Remove Field:" + v.name);
							var ctor = (obj as object[])[0] as ConstructorValueData;
							uNodeUtility.RemoveArray(ref ctor.initializer, (obj as object[])[1] as ParameterValueData);
							GUIChanged(unityObject);
						}, new object[] { cVal, v });
					}
				}
				menu.ShowAsContext();
			}
			if(GUILayout.Button(new GUIContent("Reset", ""), EditorStyles.miniButtonRight)) {
				uNodeEditorUtility.RegisterUndo(unityObject, "Reset Initializer");
				cVal.initializer = new ParameterValueData[0];
				GUIChanged(unityObject);
			}
			EditorGUILayout.EndHorizontal();
		}

		public static bool IsDoubleClick(Rect rect, int button = 0) {
			return Event.current.clickCount == 2 && rect.Contains(Event.current.mousePosition) && Event.current.button == button;
		}

		public static bool IsClicked(Rect rect, int button = 0) {
			return Event.current.clickCount >= 1 && rect.Contains(Event.current.mousePosition) && Event.current.button == button;
		}

		public static void ShowFields(object obj, UnityEngine.Object unityObject = null, System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
			uNodeUtility.EditValueSettings setting = null) {
			FieldInfo[] fieldInfo = ReflectionUtils.GetFields(obj, flags);
			Array.Sort(fieldInfo, (x, y) => {
				if(x.DeclaringType != y.DeclaringType) {
					return string.Compare(x.DeclaringType.IsSubclassOf(y.DeclaringType).ToString(), y.DeclaringType.IsSubclassOf(x.DeclaringType).ToString());
				}
				return string.Compare(x.MetadataToken.ToString(), y.MetadataToken.ToString());
			});
			ShowFields(fieldInfo, obj, unityObject);
		}

		public static void ShowFields(FieldInfo[] fields, object targetField, UnityEngine.Object unityObject = null,
			uNodeUtility.EditValueSettings setting = null) {
			foreach(FieldInfo field in fields) {
				if(IsHide(field, targetField)) {
					continue;
				}
				var control = FieldControl.FindControl(field.FieldType, false);
				if(control != null) {
					control.DrawLayouted(field.GetValueOptimized(targetField), new GUIContent(ObjectNames.NicifyVariableName(field.Name)), field.FieldType, (val) => {
						uNodeEditorUtility.RegisterUndo(unityObject, "");
						field.SetValueOptimized(targetField, val);
						GUIChanged(unityObject);
					}, new uNodeUtility.EditValueSettings() {
						attributes = field.GetCustomAttributes(true),
					});
					continue;
				}
				ShowField(field, targetField, unityObject, setting);
			}
		}

		public static FieldInfo GetField(object parent, string targetField) {
			return parent.GetType().GetField(targetField, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
		}

		public static Type GetFieldValueType(object parent, string targetField) {
			FieldInfo field = parent.GetType().GetField(targetField);
			object obj = field.GetValueOptimized(parent);
			if(obj is MemberData) {
				return (obj as MemberData).Get<Type>();
			} else if(obj is string) {
				return TypeSerializer.Deserialize(obj as string, false);
			} else if(obj is BaseValueData) {
				return (obj as BaseValueData).Get() as Type;
			} else if(obj is ValueData) {
				return (obj as ValueData).Get() as Type;
			}
			if(obj is Type) {
				return obj as Type;
			}
			return null;
		}

		public static bool IsHide(FieldInfo field, object targetField) {
			if(field.IsDefined(typeof(NonSerializedAttribute), true) || field.IsDefined(typeof(HideInInspector), true))
				return true;
			if(targetField == null || !field.IsDefined(typeof(HideAttribute), true))
				return false;
			foreach(HideAttribute hideA in ((HideAttribute[])field.GetCustomAttributes(typeof(HideAttribute), true))) {
				if(string.IsNullOrEmpty(hideA.targetField)) {
					if(hideA.NullOnHide && !object.ReferenceEquals(field.GetValueOptimized(targetField), null)) {
						field.SetValueOptimized(targetField, null);
					}
					return true;
				}
				object targetRef = ReflectionUtils.GetFieldValue(targetField, hideA.targetField);
				if(targetRef != null) {
					bool isHide = false;
					bool same = true;
					Type targetRefType = targetRef.GetType();
					if(targetRefType == typeof(MemberData)) {
						var fieldVal = targetRef as MemberData;
						if(fieldVal != null) {
							if(hideA.hideValue == null) {
								same = (!fieldVal.isAssigned || string.IsNullOrEmpty(fieldVal.targetTypeName));
								if(hideA.hideOnSame && same) {
									isHide = true;
								} else if(!hideA.hideOnSame && !same) {
									isHide = true;
								}
							} else if(hideA.hideValue != null && (!hideA.hideOnSame || fieldVal.isAssigned && !string.IsNullOrEmpty(fieldVal.targetTypeName))) {
								Type validType = fieldVal.type;
								if(validType != null) {
									if(hideA.elementType && (validType.IsArray || validType.IsGenericType)) {
										if(validType.IsArray) {
											validType = validType.GetElementType();
										} else {
											validType = validType.GetGenericArguments()[0];
										}
									}
								}
								if(hideA.hideValue is Type) {
									same = ((Type)hideA.hideValue) == validType || validType.IsCastableTo((Type)hideA.hideValue);
									if(hideA.hideOnSame && same) {
										isHide = true;
									} else if(!hideA.hideOnSame && !same) {
										isHide = true;
									}
								} else if(hideA.hideValue is Type[]) {
									Type[] hT = hideA.hideValue as Type[];
									for(int i = 0; i < hT.Length; i++) {
										same = hT[i] == validType || validType.IsCastableTo(hT[i]);
										if(hideA.hideOnSame && same) {
											isHide = true;
											break;
										} else if(!hideA.hideOnSame) {
											if(!same) {
												isHide = true;
												continue;
											} else {
												isHide = false;
												break;
											}
										}
									}
								}
							}
						}
					} else {
						same = targetRef.Equals(hideA.hideValue);
						if(hideA.hideOnSame && same) {
							isHide = true;
						} else if(!hideA.hideOnSame && !same) {
							isHide = true;
						}
					}
					if(isHide) {
						if(hideA.NullOnHide && !object.ReferenceEquals(field.GetValueOptimized(targetField), null)) {
							field.SetValueOptimized(targetField, null);
						}
						return true;
					}
				}
			}
			return false;
		}

		public static void DrawTypeDrawer(Rect position, Type type, ObjectTypeAttribute drawer, GUIContent label, Action<Type> onClick, FilterAttribute filter = null) {
			if(drawer == null && filter == null)
				return;
			if(filter == null) {
				filter = new FilterAttribute(drawer.type) { OnlyGetType = true, UnityReference = false };
			}
			GUIContent buttonLabel = new GUIContent(type.PrettyName());
			if(type == null) {
				buttonLabel.text = "Unassigned";
			}
			position = EditorGUI.PrefixLabel(position, label);
			if(EditorGUI.DropdownButton(position, buttonLabel, FocusType.Keyboard) && Event.current.button == 0) {
				GUI.changed = false;
				ItemSelector.ShowWindow(null, filter, delegate (MemberData member) {
					onClick(member.Get<Type>());
				}).ChangePosition(position.ToScreenRect());
			}
		}

		public static void DrawNullValue(Type instanceType = null, Action onClick = null) {
			Rect position = GetRect();
			DrawNullValue(position, instanceType, onClick);
		}

		public static void DrawNullValue(Rect position, Type instanceType = null, Action onClick = null) {
			if(instanceType != null && onClick != null)
				position.width -= 16;
			uNodeEditorGUI.Label(position, "null", (GUIStyle)"HelpBox");
			if(instanceType != null && onClick != null) {
				position.x += position.width;
				position.width = 16;
				if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.miniButton) && Event.current.button == 0) {
					if(onClick != null) {
						onClick();
					}
				}
			}
		}

		public static void DrawNullValue(GUIContent label, Type instanceType = null, Action<object> onCreateInstance = null) {
			Rect position = GetRect();
			DrawNullValue(position, label, instanceType, onCreateInstance);
		}

		public static void DrawNullValue(Rect position, GUIContent label, Type instanceType = null, Action<object> onCreateInstance = null) {
			position = EditorGUI.PrefixLabel(position, label);
			if(instanceType != null && onCreateInstance != null)
				position.width -= 16;
			uNodeEditorGUI.Label(position, "null", (GUIStyle)"HelpBox");
			if(instanceType != null && onCreateInstance != null) {
				position.x += position.width;
				position.width = 16;
				if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.miniButton) && Event.current.button == 0 &&
					ReflectionUtils.CanCreateInstance(instanceType)) {
					if(onCreateInstance != null) {
						onCreateInstance(ReflectionUtils.CreateInstance(instanceType));
					}
				}
			}
		}

		public static Rect GetRect(params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight, Styles.labelStyle, options);
		}

		public static Rect GetRect(float width, float height, params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(width, height, Styles.labelStyle, options);
		}

		public static Rect GetRect(GUIStyle style, params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight, style, options);
		}

		public static Rect GetRectCustomHeight(float height, params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, height, Styles.labelStyle, options);
		}

		public static Rect GetRectCustomHeight(float height, GUIStyle style, params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, height, style, options);
		}

		public static Rect GetRect(float heightMultiply, params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight * heightMultiply, Styles.labelStyle, options);
		}

		public static Rect GetRect(GUIStyle style, float heightMultiply, params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight * heightMultiply, style, options);
		}

		public static Rect GetRect(GUIContent label, GUIStyle style, params GUILayoutOption[] options) {
			return GUILayoutUtility.GetRect(label, style, options);
		}

		public static bool CanDrawOneLine(Type type) {
			if(type == typeof(int)) {
				return true;
			} else if(type == typeof(uint)) {
				return true;
			} else if(type == typeof(char)) {
				return true;
			} else if(type == typeof(byte)) {
				return true;
			} else if(type == typeof(sbyte)) {
				return true;
			} else if(type == typeof(float)) {
				return true;
			} else if(type == typeof(short)) {
				return true;
			} else if(type == typeof(ushort)) {
				return true;
			} else if(type == typeof(bool)) {
				return true;
			} else if(type == typeof(double)) {
				return true;
			} else if(type == typeof(decimal)) {
				return true;
			} else if(type == typeof(long)) {
				return true;
			} else if(type == typeof(ulong)) {
				return true;
			} else if(type == typeof(Color)) {
				return true;
			} else if(type == typeof(Color32)) {
				return true;
			} else if(type == typeof(Vector2)) {
				return true;
			} else if(type == typeof(Vector3)) {
				return true;
			} else if(type == typeof(Vector4)) {
				return true;
			} else if(type == typeof(Quaternion)) {
				return true;
			} else if(type.IsSubclassOf(typeof(Enum))) {
				return true;
			} else if(type == typeof(AnimationCurve)) {
				return true;
			} else if(type == typeof(string)) {
				return true;
			} else if(type == typeof(MemberData)) {
				return true;
			} else if(type.IsCastableTo(typeof(UnityEngine.Object))) {
				return true;
			} else if(type == typeof(Type)) {
				return true;
			}
			return false;
		}

		public static void ShowField(GUIContent label,
			FieldInfo field,
			object targetField,
			UnityEngine.Object unityObject,
			ObjectTypeAttribute objectType = null,
			uNodeUtility.EditValueSettings settings = null) {
			if(settings == null) {
				settings = new uNodeUtility.EditValueSettings();
			}
			settings.parentValue = targetField;
			settings.unityObject = unityObject;
			settings.attributes = field.GetCustomAttributes(true);
			ShowField(label, field, targetField, objectType, settings);
		}

		public static void ShowField(GUIContent label,
			FieldInfo field,
			object targetField,
			ObjectTypeAttribute objectType = null,
			uNodeUtility.EditValueSettings settings = null) {
			Type type = field.FieldType;
			object fieldValue = field.GetValueOptimized(targetField);
			if(label == null) {
				string ToolTip = "";
				string fieldName = ObjectNames.NicifyVariableName(field.Name);
				if(field.IsDefined(typeof(TooltipAttribute), true)) {
					ToolTip = ((TooltipAttribute)field.GetCustomAttributes(typeof(TooltipAttribute), true)[0]).tooltip;
				}
				label = new GUIContent(fieldName, ToolTip);
			}
			if(settings == null) {
				settings = new uNodeUtility.EditValueSettings();
			}
			EditorGUI.BeginChangeCheck();
			if(ReflectionUtils.TryCorrectingAttribute(targetField, ref settings.attributes)) {
				if(objectType != null && objectType.type != null && type == typeof(object)) {
					type = objectType.type;
				}
			} else {
				EditorGUI.EndChangeCheck();
				if(!type.IsValueType && !object.ReferenceEquals(fieldValue, null)) {
					field.SetValueOptimized(targetField, null);
					GUI.changed = false;
				}
				return;
			}
			if(type.IsGenericType ||
				type.IsArray ||
				IsSupportedType(type) ||
				type == typeof(Type) ||
				type == typeof(BaseValueData) ||
				type.IsSubclassOf(typeof(BaseValueData)) ||
				type == typeof(ValueData) ||
				type == typeof(object) ||
				FieldControl.FindControl(type, true) != null) {

				var oldValue = fieldValue;
				EditValueLayouted(label, oldValue, type, delegate (object val) {
					uNodeEditorUtility.RegisterUndo(settings.unityObject, field.Name);
					oldValue = val;
					field.SetValueOptimized(targetField, oldValue);
					GUIChanged(settings.unityObject);
				}, new uNodeUtility.EditValueSettings(settings) { parentValue = targetField });
				if(EditorGUI.EndChangeCheck()) {
					uNodeEditorUtility.RegisterUndo(settings.unityObject, field.Name);
					field.SetValueOptimized(targetField, oldValue);
					GUIChanged(settings.unityObject);
				}
			} else if(ReflectionUtils.CanCreateInstance(type)) {
				object obj = fieldValue;
				if(obj == null) {
					if(settings.nullable) {
						if(fieldValue != null)
							GUI.changed = true;
						obj = null;
					} else {
						obj = ReflectionUtils.CreateInstance(type);
						GUI.changed = true;
					}
				}
				if(obj != null) {
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(label);
					if(settings.nullable) {
						if(EditorGUILayout.DropdownButton(GUIContent.none, FocusType.Keyboard, EditorStyles.miniButton, GUILayout.Width(16)) && Event.current.button == 0) {
							obj = null;
							GUI.changed = true;
						}
					}
					EditorGUILayout.EndHorizontal();
					if(obj != null) {
						FieldInfo[] fieldInfo = ReflectionUtils.GetFieldsFromType(type);
						if(fieldInfo != null && fieldInfo.Length > 0) {
							Array.Sort(fieldInfo, (x, y) => {
								if(x.DeclaringType != y.DeclaringType) {
									return string.Compare(x.DeclaringType.IsSubclassOf(y.DeclaringType).ToString(), y.DeclaringType.IsSubclassOf(x.DeclaringType).ToString());
								}
								return string.Compare(x.MetadataToken.ToString(), y.MetadataToken.ToString());
							});
							EditorGUI.indentLevel++;
							ShowFields(fieldInfo, obj, settings.unityObject);
							EditorGUI.indentLevel--;
						}
					}
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.RegisterUndo(settings.unityObject, field.Name);
						field.SetValueOptimized(targetField, obj);
						GUIChanged(settings.unityObject);
					}
				} else {
					DrawNullValue(label, type, delegate (object o) {
						uNodeEditorUtility.RegisterUndo(settings.unityObject, "Create Field Instance");
						field.SetValueOptimized(targetField, obj);
						GUIChanged(settings.unityObject);
					});
					EditorGUI.EndChangeCheck();
				}
			} else {
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel(label);
				uNodeEditorGUI.Button(fieldValue == null ? "null" : type.PrettyName());
				EditorGUILayout.EndHorizontal();
				EditorGUI.EndChangeCheck();
			}
		}

		public static void ShowField(string fieldName,
			object parentField,
			UnityEngine.Object unityObject,
			BindingFlags flags,
			uNodeUtility.EditValueSettings setting = null) {
			if(object.ReferenceEquals(parentField, null))
				return;
			FieldInfo field = parentField.GetType().GetField(fieldName, flags);
			ShowField(null, field, parentField, unityObject, null, setting);
		}

		public static void ShowField(string fieldName, object parentField, UnityEngine.Object unityObject = null,
			uNodeUtility.EditValueSettings setting = null) {
			if(object.ReferenceEquals(parentField, null))
				return;
			FieldInfo field = parentField.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
			if(field == null) {
				throw new System.Exception("The field name : " + fieldName + " does't exists");
			}
			ShowField(null, field, parentField, unityObject, null, setting);
		}

		public static void ShowField(GUIContent label, string fieldName, object parentField, UnityEngine.Object unityObject = null) {
			if(object.ReferenceEquals(parentField, null))
				return;
			FieldInfo field = parentField.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
			if(field == null) {
				throw new System.Exception("The field name : " + fieldName + " does't exists");
			}
			ShowField(label, field, parentField, unityObject, null, null);
		}

		public static void ShowField(GUIContent label, string fieldName, object parentField, object[] attributes, UnityEngine.Object unityObject = null) {
			if(object.ReferenceEquals(parentField, null))
				return;
			FieldInfo field = parentField.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
			if(field == null) {
				throw new System.Exception("The field name : " + fieldName + " does't exists");
			}
			ShowField(label, field, parentField, null, new uNodeUtility.EditValueSettings() {
				parentValue = parentField,
				unityObject = unityObject,
				attributes = attributes,
			});
		}

		public static void ShowField(string fieldName, object parentField, BindingFlags flags, UnityEngine.Object unityObject = null,
			uNodeUtility.EditValueSettings setting = null) {
			if(object.ReferenceEquals(parentField, null))
				return;
			FieldInfo field = parentField.GetType().GetField(fieldName, flags);
			if(field == null) {
				throw new System.Exception("The field name : " + fieldName + " does't exists");
			}
			ShowField(null, field, parentField, unityObject, null, setting);
		}

		public static void ShowField(FieldInfo field, object targetField,
			UnityEngine.Object unityObject = null,
			uNodeUtility.EditValueSettings setting = null) {
			ShowField(null, field, targetField, unityObject, null, setting);
		}
		#endregion

		#region EditValue
		public static void EditUnityObject(UnityEngine.Object target) {
			Editor editor = Editor.CreateEditor(target);
			if(editor != null) {
				editor.OnInspectorGUI();
			}
		}

		public static void EditValue<T>(Rect position,
			GUIContent label,
			T fieldValue,
			Action<T> onChange,
			uNodeUtility.EditValueSettings settings = null) {
			EditValue(position, label, fieldValue, typeof(T), (obj) => onChange?.Invoke((T)obj), settings);
		}

		public static void EditValue(Rect position,
			GUIContent label,
			object fieldValue,
			Type type,
			Action<object> onChange,
			UnityEngine.Object unityObject,
			object[] fieldAttribute = null) {
			EditValue(position, label, fieldValue, type, onChange, new uNodeUtility.EditValueSettings() {
				attributes = fieldAttribute,
				unityObject = unityObject,
			});
		}

		public static void EditValue(Rect position,
			GUIContent label,
			object fieldValue,
			Type type,
			Action<object> onChange = null,
			uNodeUtility.EditValueSettings settings = null) {
			if(settings == null) {
				settings = new uNodeUtility.EditValueSettings();
			}
			var unityObject = settings.unityObject;
			var fieldAttribute = settings.attributes;
			var control = FieldControl.FindControl(type, false);
			if(control != null) {
				if(string.IsNullOrEmpty(label.tooltip)) {
					label.tooltip = settings.Tooltip;
				}
				control.Draw(position, label, fieldValue, type, (val) => {
					uNodeEditorUtility.RegisterUndo(unityObject, "");
					fieldValue = val;
					if(onChange != null) {
						onChange(fieldValue);
					}
					GUIChanged(unityObject);
				}, settings);
				return;
			}
			EditorGUI.BeginChangeCheck();
			if(type is RuntimeType) {
				EditRuntimeTypeValue(position, label, fieldValue, type as RuntimeType, (val) => {
					uNodeEditorUtility.RegisterUndo(unityObject, "");
					fieldValue = val;
					if(onChange != null) {
						onChange(fieldValue);
					}
					GUIChanged(unityObject);
				}, uNodeEditorUtility.IsSceneObject(unityObject));
			} else if(type.IsCastableTo(typeof(UnityEngine.Object))) {
				if(settings == null || settings.acceptUnityObject) {
					if(fieldValue != null && !(fieldValue is UnityEngine.Object)) {
						fieldValue = null;
					}
					UnityEngine.Object oldValue = fieldValue as UnityEngine.Object;
					oldValue = EditorGUI.ObjectField(position, label, oldValue, type, uNodeEditorUtility.IsSceneObject(unityObject));
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.RegisterUndo(unityObject, "");
						fieldValue = oldValue;
						if(onChange != null) {
							onChange(fieldValue);
						}
						GUIChanged(unityObject);
					}
				} else {
					position = EditorGUI.PrefixLabel(position, label);
					uNodeEditorGUI.Label(position, "null", EditorStyles.helpBox);
					EditorGUI.EndChangeCheck();
				}
			} else if(type == typeof(Type)) {
				var oldValue = fieldValue;
				Type t = oldValue as Type;
				if(oldValue is string) {
					t = TypeSerializer.Deserialize(oldValue as string, false);
				}
				if(EditorGUI.DropdownButton(position, new GUIContent(t != null ?
					t.PrettyName(true) : string.IsNullOrEmpty(oldValue as string) ?
					"null" : "Missing Type", t != null ? t.PrettyName(true) : null), FocusType.Keyboard) && Event.current.button == 0) {
					GUI.changed = false;
					if(Event.current.button == 0) {
						FilterAttribute filter = ReflectionUtils.GetAttribute<FilterAttribute>(fieldAttribute);
						if(filter == null)
							filter = new FilterAttribute();
						filter.OnlyGetType = true;
						filter.UnityReference = false;
						TypeSelectorWindow.ShowAsNew(position, filter, delegate (MemberData[] members) {
							uNodeEditorUtility.RegisterUndo(unityObject, "");
							oldValue = members[0].Get<Type>();
							if(onChange != null) {
								onChange(oldValue);
							}
							GUIChanged(unityObject);
						}, t);
					} else {
						CommandWindow.CreateWindow(GUIToScreenRect(position), (items) => {
							var member = CompletionEvaluator.CompletionsToMemberData(items);
							if(member != null) {
								uNodeEditorUtility.RegisterUndo(unityObject, "");
								oldValue = member.Get<Type>();
								if(onChange != null) {
									onChange(oldValue);
								}
								GUIChanged(unityObject);
								return true;
							}
							return false;
						}, new CompletionEvaluator.CompletionSetting() {
							validCompletionKind = CompletionKind.Type | CompletionKind.Namespace | CompletionKind.Keyword,
						});
					}
				}
				if(EditorGUI.EndChangeCheck()) {
					uNodeEditorUtility.RegisterUndo(unityObject, "");
					fieldValue = oldValue;
					if(onChange != null) {
						onChange(fieldValue);
					}
					GUIChanged(unityObject);
				}
			} else if(type == typeof(object)) {
				position = EditorGUI.PrefixLabel(position, label);
				position.width -= 20;
				if(fieldValue == null) {
					if(settings.acceptUnityObject) {
						EditValue(position, GUIContent.none, fieldValue, typeof(UnityEngine.Object), onChange, settings);
					} else {
						uNodeEditorGUI.Label(position, new GUIContent("null", type.PrettyName(true)), EditorStyles.helpBox);
					}
				} else if(fieldValue.GetType() == typeof(object)) {
					uNodeEditorGUI.Label(position, new GUIContent("new object()", type.PrettyName(true)), EditorStyles.helpBox);
				} else if(fieldValue.GetType().IsCastableTo(typeof(UnityEngine.Object))) {
					EditValue(position, GUIContent.none, fieldValue, typeof(UnityEngine.Object), onChange, settings);
				} else {
					EditValue(position, GUIContent.none, fieldValue, fieldValue.GetType(), onChange, settings);
				}
				position.x += position.width;
				position.width = 20;
				if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.popup)) {
					GUI.changed = false;
					if(Event.current.button == 0) {
						ItemSelector.ShowWindow(null, new FilterAttribute() { DisplayAbstractType = false, DisplayInterfaceType = false },
							delegate (MemberData member) {
								Type t = member.Get<Type>();
								if(ReflectionUtils.CanCreateInstance(t)) {
									uNodeEditorUtility.RegisterUndo(unityObject, "");
									fieldValue = ReflectionUtils.CreateInstance(t);
									if(onChange != null) {
										onChange(fieldValue);
									}
									GUIChanged(unityObject);
								} else if(settings.nullable) {
									uNodeEditorUtility.RegisterUndo(unityObject, "");
									fieldValue = null;
									if(onChange != null) {
										onChange(fieldValue);
									}
									GUIChanged(unityObject);
								}
							}, true).ChangePosition(position.ToScreenRect());
					} else if(Event.current.button == 1) {
						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent("Make Null"), false, () => {
							uNodeEditorUtility.RegisterUndo(unityObject, "");
							fieldValue = null;
							if(onChange != null) {
								onChange(fieldValue);
							}
							GUIChanged(unityObject);
						});
						var mPos = GUIToScreenRect(position);
						menu.AddItem(new GUIContent("Change Type"), false, () => {
							CommandWindow.CreateWindow(mPos, (items) => {
								var member = CompletionEvaluator.CompletionsToMemberData(items);
								if(member != null) {
									Type t = member.Get<Type>();
									if(ReflectionUtils.CanCreateInstance(t)) {
										uNodeEditorUtility.RegisterUndo(unityObject, "");
										fieldValue = ReflectionUtils.CreateInstance(t);
										if(onChange != null) {
											onChange(fieldValue);
										}
										GUIChanged(unityObject);
									} else if(settings.nullable) {
										uNodeEditorUtility.RegisterUndo(unityObject, "");
										fieldValue = null;
										if(onChange != null) {
											onChange(fieldValue);
										}
										GUIChanged(unityObject);
									}
									return true;
								}
								return false;
							}, new CompletionEvaluator.CompletionSetting() {
								validCompletionKind = CompletionKind.Type | CompletionKind.Namespace | CompletionKind.Keyword,
							});
						});
						menu.ShowAsContext();
					}
				}
				EditorGUI.EndChangeCheck();
			} else if(type.IsValueType && type != typeof(void) || type.IsClass && !type.IsAbstract ||
				 type.IsGenericType && !type.IsGenericTypeDefinition ||
				 type.IsArray) {
				if((type.IsValueType && type != typeof(void) || !settings.nullable && (type.IsClass && !type.IsAbstract ||
					type.IsGenericType && !type.IsGenericTypeDefinition || type.IsArray)) &&
					(fieldValue == null || !type.IsCastableTo(fieldValue.GetType()))) {

					fieldValue = ReflectionUtils.CreateInstance(type);
					if(onChange != null) {
						onChange(fieldValue);
					}
				}
				position = EditorGUI.PrefixLabel(position, label);
				if(EditorGUI.DropdownButton(position, new GUIContent(fieldValue != null ? type.PrettyName(true) : "null", type.PrettyName(true)), FocusType.Keyboard) && Event.current.button == 0) {
					//Make sure don't mark if value changed.
					GUI.changed = false;
					var w = ActionPopupWindow.ShowWindow(position,
						fieldValue,
						delegate (ref object obj) {
							EditValueLayouted(new GUIContent("Values", type.PrettyName(true)), obj, type, delegate (object val) {
								ActionPopupWindow.GetLast().variable = val;
								if(onChange != null)
									onChange(val);
							}, settings);
						}, null, delegate (ref object obj) {
							if(GUILayout.Button("Close")) {
								GUI.changed = false;
								ActionPopupWindow.CloseLast();
							}
						}, 300, 250);
					w.headerName = "Edit Values";
					if(type.IsValueType) {
						//Close Action when editing value type and performing redo or undo to avoid wrong value.
						w.onUndoOrRedo = delegate () {
							w.Close();
						};
					}
				}
				EditorGUI.EndChangeCheck();
			} else {
				position = EditorGUI.PrefixLabel(position, label);
				uNodeEditorGUI.Label(position, new GUIContent("unsupported type", type.PrettyName(true)), EditorStyles.helpBox);
				EditorGUI.EndChangeCheck();
			}
		}

		public static void EditValue(Rect position, GUIContent label, string fieldName, object parentField, UnityEngine.Object unityObject = null) {
			if(object.ReferenceEquals(parentField, null))
				return;
			FieldInfo field = parentField.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
			if(field == null) {
				throw new System.Exception("The field name : " + fieldName + " does't exists");
			}
			EditValue(position, label, field.GetValueOptimized(parentField), field.FieldType, (obj) => {
				field.SetValueOptimized(parentField, obj);
			}, unityObject, field.GetCustomAttributes(true));
		}

		public static void EditValue(Rect position, GUIContent label, string fieldName, int elementIndex, object parentField, UnityEngine.Object unityObject = null) {
			if(object.ReferenceEquals(parentField, null))
				return;
			FieldInfo field = parentField.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic);
			if(field == null) {
				throw new Exception($"Cannot find field with name:{fieldName} on type: {parentField.GetType().FullName}");
			}
			var arr = field.GetValueOptimized(parentField) as IList;
			EditValue(position, label, arr[elementIndex], field.FieldType.ElementType(), (obj) => {
				field.SetValueOptimized(parentField, obj);
			}, unityObject, field.GetCustomAttributes(true));
		}

		public static void EditValueLayouted(string fieldName, object owner, Action<object> onChange = null) {
			var field = GetField(owner, fieldName);
			if(field == null) {
				throw new Exception($"Cannot find field with name:{fieldName} on type: {owner.GetType().FullName}");
			}
			EditValueLayouted(field, owner, onChange);
		}

		public static void EditValueLayouted(FieldInfo field, object owner, Action<object> onChange = null) {
			EditValueLayouted(new GUIContent(ObjectNames.NicifyVariableName(field.Name)), field.GetValueOptimized(owner), field.FieldType, (val) => {
				onChange?.Invoke(val);
			}, new uNodeUtility.EditValueSettings() {
				attributes = field.GetCustomAttributes(true)
			});
		}

		public static void EditValueLayouted<T>(GUIContent label,
			T fieldValue,
			Action<T> onChange,
			uNodeUtility.EditValueSettings settings = null) {

			EditValueLayouted(
				label,
				fieldValue, typeof(T),
				(val) => {
					onChange((T)val);
				},
				settings
			);
		}

		public static void EditValueLayouted(GUIContent label,
			object fieldValue,
			Type type,
			Action<object> onChange = null,
			uNodeUtility.EditValueSettings settings = null) {
			if(settings == null) {
				settings = new uNodeUtility.EditValueSettings();
			}
			var control = FieldControl.FindControl(type, true);
			if(control != null) {
				control.DrawLayouted(fieldValue, label, type, (val) => {
					uNodeEditorUtility.RegisterUndo(settings.unityObject, "");
					fieldValue = val;
					if(onChange != null) {
						onChange(fieldValue);
					}
					GUIChanged(settings.unityObject);
				}, settings);
				return;
			}
			var fieldAttribute = settings.attributes;
			var unityObject = settings.unityObject;
			EditorGUI.BeginChangeCheck();
			if(type.IsValueType && type != typeof(void)) {
				var oldValue = fieldValue;
				if(oldValue == null) {
					oldValue = ReflectionUtils.CreateInstance(type);
					if(oldValue != null) {
						GUI.changed = true;
					}
				} else if(!type.IsCastableTo(oldValue.GetType())) {
					oldValue = ReflectionUtils.CreateInstance(type);
					GUI.changed = true;
				}
				FieldInfo[] fieldInfo = ReflectionUtils.GetFieldsFromType(type);
				if(fieldInfo != null && fieldInfo.Length > 0) {
					Array.Sort(fieldInfo, (x, y) => {
						if(x.DeclaringType != y.DeclaringType) {
							return string.Compare(x.DeclaringType.IsSubclassOf(y.DeclaringType).ToString(), y.DeclaringType.IsSubclassOf(x.DeclaringType).ToString());
						}
						return string.Compare(x.MetadataToken.ToString(), y.MetadataToken.ToString());
					});
					EditorGUI.LabelField(GetRect(), label);
					EditorGUI.indentLevel++;
					for(int i = 0; i < fieldInfo.Length; i++) {
						int index = i;
						object elementValue = fieldInfo[i].GetValueOptimized(oldValue);
						EditValueLayouted(new GUIContent(fieldInfo[index].Name), elementValue, fieldInfo[index].FieldType, o => {
							elementValue = o;
							fieldInfo[index].SetValueOptimized(oldValue, elementValue);
							GUIChanged(unityObject);
						}, new uNodeUtility.EditValueSettings(settings) {
							attributes = fieldInfo[index].GetCustomAttributes(true)
						});
					}
					EditorGUI.indentLevel--;
				}
				if(EditorGUI.EndChangeCheck()) {
					uNodeEditorUtility.RegisterUndo(unityObject, "");
					fieldValue = oldValue;
					if(onChange != null) {
						onChange(fieldValue);
					}
					GUIChanged(unityObject);
				}
			} else if(type is RuntimeType) {
				EditRuntimeTypeValue(GetRect(), label, fieldValue, type as RuntimeType, (val) => {
					uNodeEditorUtility.RegisterUndo(unityObject, "");
					fieldValue = val;
					if(onChange != null) {
						onChange(fieldValue);
					}
					GUIChanged(unityObject);
				}, uNodeEditorUtility.IsSceneObject(unityObject));
			} else if((type == typeof(UnityEngine.Object) || type.IsSubclassOf(typeof(UnityEngine.Object))) && (settings == null || settings.acceptUnityObject)) {
				if(fieldValue != null && !(fieldValue is UnityEngine.Object)) {
					fieldValue = null;
				}
				UnityEngine.Object oldValue = fieldValue as UnityEngine.Object;
				oldValue = EditorGUI.ObjectField(GetRect(), label, oldValue, type, uNodeEditorUtility.IsSceneObject(unityObject));
				if(EditorGUI.EndChangeCheck()) {
					uNodeEditorUtility.RegisterUndo(unityObject, "");
					fieldValue = oldValue;
					if(onChange != null) {
						onChange(fieldValue);
					}
					GUIChanged(unityObject);
				}
			} else if(type == typeof(Type)) {
				var oldValue = fieldValue;
				Type t = oldValue as Type;
				if(oldValue is string) {
					t = TypeSerializer.Deserialize(oldValue as string, false);
				}
				Rect rect = GetRect();
				rect = EditorGUI.PrefixLabel(rect, label);
				if(EditorGUI.DropdownButton(rect, new GUIContent(t != null ?
					t.PrettyName(true) : string.IsNullOrEmpty(oldValue as string) ?
					"null" : "Missing Type", t != null ? t.PrettyName(true) : null), FocusType.Keyboard) && Event.current.button == 0) {
					GUI.changed = false;
					if(Event.current.button == 0) {
						FilterAttribute filter = ReflectionUtils.GetAttribute<FilterAttribute>(fieldAttribute);
						if(filter == null)
							filter = new FilterAttribute();
						filter.OnlyGetType = true;
						filter.UnityReference = false;
						TypeSelectorWindow.ShowAsNew(rect, filter, delegate (MemberData[] members) {
							uNodeEditorUtility.RegisterUndo(unityObject, "");
							oldValue = members[0].Get<Type>();
							fieldValue = oldValue;
							if(onChange != null) {
								onChange(fieldValue);
							}
							GUIChanged(unityObject);
						}, t);
					} else {
						CommandWindow.CreateWindow(GUIToScreenRect(rect), (items) => {
							var member = CompletionEvaluator.CompletionsToMemberData(items);
							if(member != null) {
								uNodeEditorUtility.RegisterUndo(unityObject, "");
								oldValue = member.Get<Type>();
								fieldValue = oldValue;
								if(onChange != null) {
									onChange(fieldValue);
								}
								GUIChanged(unityObject);
								return true;
							}
							return false;
						}, new CompletionEvaluator.CompletionSetting() {
							validCompletionKind = CompletionKind.Type | CompletionKind.Namespace | CompletionKind.Keyword,
						});
					}
				}
				EditorGUI.EndChangeCheck();
			} else if(type == typeof(BaseValueData) || type.IsSubclassOf(typeof(BaseValueData))) {
				var Value = fieldValue as BaseValueData;
				if(Value == null) {
					Value = new ParameterValueData();
					GUI.changed = true;
				}
				if(Value != null) {
					if(Value is ConstructorValueData) {
						ConstructorValueData cVal = Value as ConstructorValueData;
						if(cVal.parameters != null && cVal.parameters.Length > 0) {
							for(int i = 0; i < cVal.parameters.Length; i++) {
								var o = cVal.parameters[i];
								var index = i;
								EditValueLayouted(new GUIContent(cVal.parameters[i].name, cVal.parameters[i].name), 
									o, 
									typeof(ParameterValueData), 
									delegate (object ob) {
										uNodeEditorUtility.RegisterUndo(unityObject, "");
										cVal.parameters[index] = ob as ParameterValueData;
										if(onChange != null) {
											onChange(cVal);
										}
										GUIChanged(unityObject);
									}, new uNodeUtility.EditValueSettings() { nullable = true, unityObject = unityObject });
							}
						}
						DrawConstructorInitializer(cVal, unityObject);
					} else if(Value is ObjectValueData) {
						ObjectValueData val = Value as ObjectValueData;
						EditValueLayouted(new GUIContent("Value"), val.value, typeof(object), delegate (object o) {
							uNodeEditorUtility.RegisterUndo(unityObject, "");
							val.value = o;
							if(onChange != null) {
								onChange(val);
							}
							GUIChanged(unityObject);
						}, new uNodeUtility.EditValueSettings() {
							unityObject = unityObject
						});
					} else {
						Rect position = GetRect();
						position = EditorGUI.PrefixLabel(position, label);
						if(EditorGUI.DropdownButton(position, new GUIContent("null"), FocusType.Keyboard)) {
							GenericMenu menu = new GenericMenu();
							menu.AddItem(new GUIContent("Instance Value"), false, delegate () {
								uNodeEditorUtility.RegisterUndo(unityObject, "");
								Value = new ObjectValueData();
								fieldValue = Value;
								if(onChange != null) {
									onChange(fieldValue);
								}
								GUIChanged(unityObject);
							});
							menu.AddItem(new GUIContent("Constructor Value"), false, delegate () {
								uNodeEditorUtility.RegisterUndo(unityObject, "");
								Value = new ConstructorValueData();
								fieldValue = Value;
								if(onChange != null) {
									onChange(fieldValue);
								}
								GUIChanged(unityObject);
							});
							menu.ShowAsContext();
							GUI.changed = false;
						}
					}
				}
				if(EditorGUI.EndChangeCheck()) {
					uNodeEditorUtility.RegisterUndo(unityObject, "");
					fieldValue = Value;
					if(onChange != null) {
						onChange(fieldValue);
					}
					GUIChanged(unityObject);
				}
			} else if(type.IsArray) {
				Type elementType = type.GetElementType();
				if(!IsSupportedType(elementType) && !elementType.IsClass) {
					EditorGUI.EndChangeCheck();
					return;
				}
				Array array = fieldValue as Array;
				if(array == null) {
					if(settings.nullable) {
						if(fieldValue != null)
							GUI.changed = true;
						array = null;
					} else {
						array = Array.CreateInstance(type.GetElementType(), 0);
						GUI.changed = true;
					}
				}
				if(array != null) {
					Rect position = GetRect();
					if(settings.nullable)
						position.width -= 16;
					int num = EditorGUI.IntField(position, label, array.Length);
					if(settings.nullable) {
						position.x += position.width;
						position.width = 16;
						if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.miniButton) && Event.current.button == 0) {
							array = null;
							GUI.changed = true;
						}
					}
					Array newArray = array;
					if(newArray != null) {
						if(num != array.Length) {
							newArray = uNodeEditorUtility.ResizeArray(array, type.GetElementType(), num);
							if(settings.parentValue != null && fieldAttribute != null) {
								ListCombineAttribute att = ReflectionUtils.GetAttribute<ListCombineAttribute>(fieldAttribute);
								if(att != null && att.otherFields != null && att.otherFields.Length > 0) {
									for(int i = 0; i < att.otherFields.Length; i++) {
										FieldInfo otherField = GetField(settings.parentValue, att.otherFields[i]);
										Type fieldType = otherField.FieldType;
										if(fieldType.IsArray) {
											uNodeEditorUtility.RegisterUndo(unityObject, "");
											Array a = otherField.GetValueOptimized(settings.parentValue) as Array;
											a = uNodeEditorUtility.ResizeArray(a, fieldType.GetElementType(), num);
											otherField.SetValueOptimized(settings.parentValue, a);
											GUIChanged(unityObject);
										} else if(fieldType.IsGenericType &&
											fieldType.GetGenericArguments().Length == 1 &&
											fieldType.IsCastableTo(typeof(IList))) {
											uNodeEditorUtility.RegisterUndo(unityObject, "");
											IList a = otherField.GetValueOptimized(settings.parentValue) as IList;
											uNodeEditorUtility.ResizeList(a, fieldType.GetElementType(), num);
											otherField.SetValueOptimized(settings.parentValue, a);
											GUIChanged(unityObject);
										}
									}
									GUI.changed = true;
								}
							}
						}
						if(newArray.Length > 0) {
							Event currentEvent = Event.current;
							EditorGUI.indentLevel++;
							List<FieldInfo> otherFields = null;
							if(settings.parentValue != null && fieldAttribute != null) {
								ListCombineAttribute att = ReflectionUtils.GetAttribute<ListCombineAttribute>(fieldAttribute);
								if(att != null && att.otherFields != null && att.otherFields.Length > 0) {
									for(int x = 0; x < att.otherFields.Length; x++) {
										FieldInfo otherField = GetField(settings.parentValue, att.otherFields[x]);
										Type fieldType = otherField.FieldType;
										if(fieldType.IsArray) {
											if(otherFields == null)
												otherFields = new List<FieldInfo>();
											otherFields.Add(otherField);
										} else if(fieldType.IsGenericType &&
											fieldType.GetGenericArguments().Length == 1 &&
											fieldType.IsCastableTo(typeof(IList))) {
											if(otherFields == null)
												otherFields = new List<FieldInfo>();
											otherFields.Add(otherField);
										}
									}
								}
							}
							for(int i = 0; i < newArray.Length; i++) {
								var elementToEdit = newArray.GetValue(i);
								if(IsSupportedType(elementType) || elementType.IsClass && !elementType.IsAbstract ||
									elementType.IsArray || elementType.IsGenericType) {
									int a = i;
									EditValueLayouted(new GUIContent("Element " + i), elementToEdit, elementType, delegate (object val) {
										uNodeEditorUtility.RegisterUndo(unityObject, "");
										elementToEdit = val;
										newArray.SetValue(elementToEdit, a);
										if(onChange != null)
											onChange(newArray);
										GUIChanged(unityObject);
									}, settings);
									if(otherFields != null && otherFields.Count > 0) {
										for(int z = 0; z < otherFields.Count; z++) {
											var v = otherFields[z].GetValueOptimized(settings.parentValue);
											if(v is Array) {
												Array arr = v as Array;
												Type fElementType = otherFields[z].FieldType.GetElementType();
												if(arr.Length < i + 1) {
													arr = uNodeEditorUtility.ResizeArray(arr, fElementType, newArray.Length);
													otherFields[z].SetValueOptimized(settings.parentValue, arr);
												}
												var fV = arr.GetValue(i);
												if(IsSupportedType(fElementType) || fElementType.IsClass && !fElementType.IsAbstract ||
													fElementType.IsArray || fElementType.IsGenericType) {
													EditValueLayouted(new GUIContent(" "), fV, elementType, delegate (object val) {
														uNodeEditorUtility.RegisterUndo(unityObject, "");
														fV = val;
														arr.SetValue(fV, a);
														otherFields[z].SetValueOptimized(settings.parentValue, arr);
														GUIChanged(unityObject);
													}, new uNodeUtility.EditValueSettings(settings) {
														attributes = otherFields[z].GetCustomAttributes(true)
													});
												}
											} else if(v is IList) {
												IList arr = v as IList;
												Type fElementType = otherFields[z].FieldType.GetGenericArguments()[0];
												if(arr.Count < i + 1) {
													uNodeEditorUtility.ResizeList(arr, fElementType, newArray.Length);
												}
												var fV = arr[i];
												if(IsSupportedType(fElementType) || fElementType.IsClass && !fElementType.IsAbstract ||
													fElementType.IsArray || fElementType.IsGenericType) {
													EditValueLayouted(new GUIContent(" "), fV, elementType, delegate (object val) {
														uNodeEditorUtility.RegisterUndo(unityObject, "");
														fV = val;
														arr[a] = fV;
														otherFields[z].SetValueOptimized(settings.parentValue, arr);
														GUIChanged(unityObject);
													}, new uNodeUtility.EditValueSettings(settings) {
														attributes = otherFields[z].GetCustomAttributes(true)
													});
												}
											}
										}
									}
								}
							}
							EditorGUI.indentLevel--;
						}
					}
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.RegisterUndo(unityObject, "");
						fieldValue = newArray;
						if(onChange != null) {
							onChange(fieldValue);
						}
						GUIChanged(unityObject);
					}
				} else {
					DrawNullValue(label, type, delegate (object o) {
						uNodeEditorUtility.RegisterUndo(unityObject, "Create Field Instance");
						if(onChange != null) {
							onChange(o);
						}
						GUIChanged(unityObject);
					});
					EditorGUI.EndChangeCheck();
				}
			} else if(type.IsGenericType && type.GetGenericArguments().Length == 1 && type.IsCastableTo(typeof(IList))) {
				Type elementType = type.GetGenericArguments()[0];
				if(!IsSupportedType(elementType) && !elementType.IsClass) {
					EditorGUI.EndChangeCheck();
					return;
				}
				IList array = fieldValue as IList;
				if(array == null) {
					if(settings.nullable) {
						if(fieldValue != null)
							GUI.changed = true;
						array = null;
					} else {
						array = ReflectionUtils.CreateInstance(type) as IList;
						GUI.changed = true;
					}
				}
				if(array != null) {
					Rect position = GetRect();
					if(settings.nullable)
						position.width -= 16;
					int num = EditorGUI.IntField(position, label, array.Count);
					if(settings.nullable) {
						position.x += position.width;
						position.width = 16;
						if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.miniButton) && Event.current.button == 0) {
							array = null;
							GUI.changed = true;
						}
					}
					if(array != null) {
						if(num != array.Count) {
							uNodeEditorUtility.ResizeList(array, elementType, num, unityObject);
							if(settings.parentValue != null && fieldAttribute != null) {
								ListCombineAttribute att = ReflectionUtils.GetAttribute<ListCombineAttribute>(fieldAttribute);
								if(att != null && att.otherFields != null && att.otherFields.Length > 0) {
									for(int i = 0; i < att.otherFields.Length; i++) {
										FieldInfo otherField = GetField(settings.parentValue, att.otherFields[i]);
										Type fieldType = otherField.FieldType;
										if(fieldType.IsArray) {
											uNodeEditorUtility.RegisterUndo(unityObject, "");
											Array a = otherField.GetValueOptimized(settings.parentValue) as Array;
											a = uNodeEditorUtility.ResizeArray(a, fieldType.GetElementType(), num);
											otherField.SetValueOptimized(settings.parentValue, a);
											GUIChanged(unityObject);
										} else if(fieldType.IsGenericType &&
											fieldType.GetGenericArguments().Length == 1 &&
											fieldType.IsCastableTo(typeof(IList))) {
											uNodeEditorUtility.RegisterUndo(unityObject, "");
											IList a = otherField.GetValueOptimized(settings.parentValue) as IList;
											uNodeEditorUtility.ResizeList(a, fieldType.GetElementType(), num);
											otherField.SetValueOptimized(settings.parentValue, a);
											GUIChanged(unityObject);
										}
									}
								}
							}
						}
						if(array.Count > 0) {
							EditorGUI.indentLevel++;
							List<FieldInfo> otherFields = null;
							if(settings.parentValue != null && fieldAttribute != null) {
								ListCombineAttribute att = ReflectionUtils.GetAttribute<ListCombineAttribute>(fieldAttribute);
								if(att != null && att.otherFields != null && att.otherFields.Length > 0) {
									for(int x = 0; x < att.otherFields.Length; x++) {
										FieldInfo otherField = GetField(settings.parentValue, att.otherFields[x]);
										Type fieldType = otherField.FieldType;
										if(fieldType.IsArray) {
											if(otherFields == null)
												otherFields = new List<FieldInfo>();
											otherFields.Add(otherField);
										} else if(fieldType.IsGenericType &&
											fieldType.GetGenericArguments().Length == 1 &&
											fieldType.IsCastableTo(typeof(IList))) {
											if(otherFields == null)
												otherFields = new List<FieldInfo>();
											otherFields.Add(otherField);
										}
									}
								}
							}
							for(int i = 0; i < array.Count; i++) {
								var elementToEdit = array[i];
								if(IsSupportedType(elementType) || elementType.IsClass && !elementType.IsAbstract ||
									elementType.IsArray || elementType.IsGenericType) {
									int a = i;
									EditValueLayouted(new GUIContent("Element " + i), elementToEdit, elementType, delegate (object val) {
										uNodeEditorUtility.RegisterUndo(unityObject, "");
										elementToEdit = val;
										array[a] = elementToEdit;
										if(onChange != null)
											onChange(array);
										GUIChanged(unityObject);
									}, settings);
									if(otherFields != null && otherFields.Count > 0) {
										for(int z = 0; z < otherFields.Count; z++) {
											var v = otherFields[z].GetValueOptimized(settings.parentValue);
											if(v is Array) {
												Array arr = v as Array;
												Type fElementType = otherFields[z].FieldType.GetElementType();
												if(arr.Length < i + 1) {
													arr = uNodeEditorUtility.ResizeArray(arr, fElementType, array.Count);
													otherFields[z].SetValueOptimized(settings.parentValue, arr);
												}
												var fV = arr.GetValue(i);
												if(IsSupportedType(fElementType) || fElementType.IsClass && !fElementType.IsAbstract ||
													fElementType.IsArray || fElementType.IsGenericType) {
													EditValueLayouted(new GUIContent(" "), fV, elementType, delegate (object val) {
														uNodeEditorUtility.RegisterUndo(unityObject, "");
														fV = val;
														arr.SetValue(fV, a);
														otherFields[z].SetValueOptimized(settings.parentValue, arr);
														GUIChanged(unityObject);
													}, new uNodeUtility.EditValueSettings(settings) {
														attributes = otherFields[z].GetCustomAttributes(true)
													});
												}
											} else if(v is IList) {
												IList arr = v as IList;
												Type fElementType = otherFields[z].FieldType.GetGenericArguments()[0];
												if(arr.Count < i + 1) {
													uNodeEditorUtility.ResizeList(arr, fElementType, array.Count);
												}
												var fV = arr[i];
												if(IsSupportedType(fElementType) || fElementType.IsClass && !fElementType.IsAbstract ||
													fElementType.IsArray || fElementType.IsGenericType) {
													EditValueLayouted(new GUIContent(" "), fV, elementType, delegate (object val) {
														uNodeEditorUtility.RegisterUndo(unityObject, "");
														fV = val;
														arr[a] = fV;
														otherFields[z].SetValueOptimized(settings.parentValue, arr);
														GUIChanged(unityObject);
													}, new uNodeUtility.EditValueSettings(settings) {
														attributes = otherFields[z].GetCustomAttributes(true)
													});
												}
											}
										}
									}
								}
							}
							EditorGUI.indentLevel--;
						}
					}
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.RegisterUndo(unityObject, "");
						fieldValue = array;
						if(onChange != null) {
							onChange(fieldValue);
						}
						GUIChanged(unityObject);
					}
				} else {
					DrawNullValue(label, type, delegate (object o) {
						uNodeEditorUtility.RegisterUndo(unityObject, "Create Field Instance");
						if(onChange != null) {
							onChange(o);
						}
						GUIChanged(unityObject);
					});
					EditorGUI.EndChangeCheck();
				}
			} else if(type.IsGenericType && type.IsCastableTo(typeof(IDictionary))) {
				Type keyType = type.GetGenericArguments()[0];
				Type valType = type.GetGenericArguments()[1];
				if(!IsSupportedType(keyType) && !keyType.IsClass || !IsSupportedType(valType) && !valType.IsClass || valType.IsAbstract) {
					EditorGUI.EndChangeCheck();
					return;
				}
				IDictionary map = fieldValue as IDictionary;
				if(map == null) {
					if(settings.nullable) {
						if(fieldValue != null)
							GUI.changed = true;
						map = null;
					} else {
						map = ReflectionUtils.CreateInstance(type) as IDictionary;
						GUI.changed = true;
					}
				}
				if(map != null) {
					Rect position = GetRect();
					if(settings.nullable)
						position.width -= 16;
					position = EditorGUI.PrefixLabel(position, label);
					if(EditorGUI.DropdownButton(position, new GUIContent("add new (" + keyType.PrettyName() + ", " + valType.PrettyName() + ")"), FocusType.Keyboard) && Event.current.button == 0) {
						GUI.changed = false;
						ActionPopupWindow.ShowWindow(position,
							new object[] { ReflectionUtils.CreateInstance(keyType), ReflectionUtils.CreateInstance(valType), map },
							delegate (ref object val) {
								object[] o = val as object[];
								EditValueLayouted(new GUIContent("Key"), o[0], keyType, delegate (object v) {
									o[0] = v;
								}, new uNodeUtility.EditValueSettings(settings) { nullable = false });
								EditValueLayouted(new GUIContent("Value"), o[1], valType, delegate (object v) {
									o[1] = v;
								}, settings);
								if(GUILayout.Button(new GUIContent("Add"))) {
									if(!map.Contains(o[0])) {
										uNodeEditorUtility.RegisterUndo(unityObject, "" + "Add Dictonary Value");
										(o[2] as IDictionary).Add(o[0], o[1]);
										fieldValue = o[2];
										if(onChange != null) {
											onChange(fieldValue);
										}
										ActionPopupWindow.CloseLast();
										GUIChanged(unityObject);
									}
								}
							}).headerName = "Add New Dictonary Value";
					}
					if(settings.nullable) {
						position.x += position.width;
						position.width = 16;
						if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.miniButton) && Event.current.button == 0) {
							map = null;
							GUI.changed = true;
						}
					}
					IDictionary newMap = map;
					if(newMap != null) {
						if(newMap.Count > 0) {
							List<object> keys = uNodeEditorUtility.GetKeys(newMap);
							List<object> values = uNodeEditorUtility.GetValues(newMap);
							if(keys.Count == values.Count) {
								EditorGUI.indentLevel++;
								for(int i = 0; i < keys.Count; i++) {
									Rect rect = GetRect();
									EditorGUI.LabelField(rect, new GUIContent("Element " + i));
									if(Event.current.button == 1 && rect.Contains(Event.current.mousePosition)) {
										GenericMenu menu = new GenericMenu();
										menu.AddItem(new GUIContent("Remove"), false, (obj) => {
											int index = (int)obj;
											newMap.Remove(keys[index]);
										}, i);
										menu.AddSeparator("");
										menu.AddItem(new GUIContent("Move To Top"), false, (obj) => {
											int index = (int)obj;
											if(index != 0) {
												uNodeEditorUtility.RegisterUndo(unityObject, "");
												uNodeEditorUtility.ListMoveToTop(keys, (int)obj);
												uNodeEditorUtility.ListMoveToTop(values, (int)obj);
												newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
												for(int x = 0; x < keys.Count; x++) {
													newMap.Add(keys[x], values[x]);
												}
												fieldValue = newMap;
												if(onChange != null) {
													onChange(fieldValue);
												}
												GUIChanged(unityObject);
											}
										}, i);
										menu.AddItem(new GUIContent("Move Up"), false, (obj) => {
											int index = (int)obj;
											if(index != 0) {
												uNodeEditorUtility.RegisterUndo(unityObject, "");
												uNodeEditorUtility.ListMoveUp(keys, (int)obj);
												uNodeEditorUtility.ListMoveUp(values, (int)obj);
												newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
												for(int x = 0; x < keys.Count; x++) {
													newMap.Add(keys[x], values[x]);
												}
												fieldValue = newMap;
												if(onChange != null) {
													onChange(fieldValue);
												}
												GUIChanged(unityObject);
											}
										}, i);
										menu.AddItem(new GUIContent("Move Down"), false, (obj) => {
											int index = (int)obj;
											if(index != keys.Count - 1) {
												uNodeEditorUtility.RegisterUndo(unityObject, "");
												uNodeEditorUtility.ListMoveDown(keys, (int)obj);
												uNodeEditorUtility.ListMoveDown(values, (int)obj);
												newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
												for(int x = 0; x < keys.Count; x++) {
													newMap.Add(keys[x], values[x]);
												}
												fieldValue = newMap;
												if(onChange != null) {
													onChange(fieldValue);
												}
												GUIChanged(unityObject);
											}
										}, i);
										menu.AddItem(new GUIContent("Move To Bottom"), false, (obj) => {
											int index = (int)obj;
											if(index != keys.Count - 1) {
												uNodeEditorUtility.RegisterUndo(unityObject, "");
												uNodeEditorUtility.ListMoveToBottom(keys, (int)obj);
												uNodeEditorUtility.ListMoveToBottom(values, (int)obj);
												newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
												for(int x = 0; x < keys.Count; x++) {
													newMap.Add(keys[x], values[x]);
												}
												fieldValue = newMap;
												if(onChange != null) {
													onChange(fieldValue);
												}
												GUIChanged(unityObject);
											}
										}, i);
										menu.ShowAsContext();
									}
									EditorGUI.indentLevel++;
									EditValueLayouted(new GUIContent("Key"), keys[i], keyType, delegate (object val) {
										if(!newMap.Contains(val)) {
											uNodeEditorUtility.RegisterUndo(unityObject, "");
											keys[i] = val;
											newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
											for(int x = 0; x < keys.Count; x++) {
												newMap.Add(keys[x], values[x]);
											}
											fieldValue = newMap;
											if(onChange != null) {
												onChange(fieldValue);
											}
											GUIChanged(unityObject);
										}
									}, new uNodeUtility.EditValueSettings(settings) { nullable = false });
									EditValueLayouted(new GUIContent("Value"), values[i], valType, delegate (object val) {
										uNodeEditorUtility.RegisterUndo(unityObject, "");
										values[i] = val;
										newMap = ReflectionUtils.CreateInstance(type) as IDictionary;
										for(int x = 0; x < values.Count; x++) {
											newMap.Add(keys[x], values[x]);
										}
										fieldValue = newMap;
										if(onChange != null) {
											onChange(fieldValue);
										}
										GUIChanged(unityObject);
									}, settings);
									EditorGUI.indentLevel--;
								}
								EditorGUI.indentLevel--;
							}
						}
					}
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.RegisterUndo(unityObject, "");
						fieldValue = newMap;
						if(onChange != null) {
							onChange(fieldValue);
						}
						GUIChanged(unityObject);
					}
				} else {
					DrawNullValue(label, type, delegate (object o) {
						uNodeEditorUtility.RegisterUndo(unityObject, "Create Field Instance");
						if(onChange != null) {
							onChange(o);
						}
						GUIChanged(unityObject);
					});
					EditorGUI.EndChangeCheck();
				}
			} else if(type.IsGenericType && type.GetGenericTypeDefinition().IsCastableTo(typeof(HashSet<>))) {
				Type keyType = type.GetGenericArguments()[0];
				if(!IsSupportedType(keyType) && !keyType.IsClass) {
					EditorGUI.EndChangeCheck();
					return;
				}
				IList map = ReflectionUtils.CreateInstance(typeof(List<>).MakeGenericType(keyType)) as IList;
				if(fieldValue == null) {
					if(settings.nullable) {
						if(fieldValue != null)
							GUI.changed = true;
						map = null;
					} else {
						GUI.changed = true;
					}
				} else {
					foreach(var val in fieldValue as IEnumerable) {
						map.Add(val);
					}
				}
				if(map != null) {
					Rect position = GetRect();
					if(settings.nullable)
						position.width -= 16;
					position = EditorGUI.PrefixLabel(position, label);
					if(EditorGUI.DropdownButton(position, new GUIContent("add new (" + keyType.PrettyName() + ")"), FocusType.Keyboard) && Event.current.button == 0) {
						GUI.changed = false;
						ActionPopupWindow.ShowWindow(position, new object[] { ReflectionUtils.CreateInstance(keyType), map, type },
							delegate (ref object val) {
								object[] o = val as object[];
								EditValueLayouted(new GUIContent("Value"), o[0], keyType, delegate (object v) {
									o[0] = v;
								}, new uNodeUtility.EditValueSettings(settings) { nullable = false });
								if(GUILayout.Button(new GUIContent("Add"))) {
									if(!map.Contains(o[0])) {
										uNodeEditorUtility.RegisterUndo(unityObject, "" + "Add Value");
										(o[1] as IList).Add(o[0]);
										fieldValue = ReflectionUtils.CreateInstance(o[2] as Type, o[1]);
										if(onChange != null) {
											onChange(fieldValue);
										}
										ActionPopupWindow.CloseLast();
										GUIChanged(unityObject);
									}
								}
							}).headerName = "Add New Collection Value";
					}
					if(settings.nullable) {
						position.x += position.width;
						position.width = 16;
						if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.miniButton) && Event.current.button == 0) {
							map = null;
							GUI.changed = true;
						}
					}

					var newMap = map;
					if(newMap != null) {
						if(newMap.Count > 0) {
							EditorGUI.indentLevel++;
							for(int i = 0; i < newMap.Count; i++) {
								Rect rect = GetRect();
								EditorGUI.PrefixLabel(rect, new GUIContent("Element " + i));
								EditorGUI.indentLevel++;
								EditValueLayouted(GUIContent.none, newMap[i], keyType, delegate (object val) {
									if(!newMap.Contains(val)) {
										uNodeEditorUtility.RegisterUndo(unityObject, "");
										newMap[i] = val;
										fieldValue = ReflectionUtils.CreateInstance(type, newMap);
										if(onChange != null) {
											onChange(fieldValue);
										}
										GUIChanged(unityObject);
									}
								}, new uNodeUtility.EditValueSettings(settings) { nullable = false });
								EditorGUI.indentLevel--;
								if(Event.current.button == 1 && rect.Contains(Event.current.mousePosition)) {
									GenericMenu menu = new GenericMenu();
									menu.AddItem(new GUIContent("Remove"), false, (obj) => {
										int index = (int)obj;
										newMap.Remove(newMap[index]);
									}, i);
									menu.AddSeparator("");
									menu.AddItem(new GUIContent("Move To Top"), false, (obj) => {
										int index = (int)obj;
										if(index != 0) {
											uNodeEditorUtility.RegisterUndo(unityObject, "");
											uNodeEditorUtility.ListMoveToTop(newMap, (int)obj);
											fieldValue = ReflectionUtils.CreateInstance(type, newMap);
											if(onChange != null) {
												onChange(fieldValue);
											}
											GUIChanged(unityObject);
										}
									}, i);
									menu.AddItem(new GUIContent("Move Up"), false, (obj) => {
										int index = (int)obj;
										if(index != 0) {
											uNodeEditorUtility.RegisterUndo(unityObject, "");
											uNodeEditorUtility.ListMoveUp(newMap, (int)obj);
											fieldValue = ReflectionUtils.CreateInstance(type, newMap);
											if(onChange != null) {
												onChange(fieldValue);
											}
											GUIChanged(unityObject);
										}
									}, i);
									menu.AddItem(new GUIContent("Move Down"), false, (obj) => {
										int index = (int)obj;
										if(index != newMap.Count - 1) {
											uNodeEditorUtility.RegisterUndo(unityObject, "");
											uNodeEditorUtility.ListMoveDown(newMap, (int)obj);
											fieldValue = ReflectionUtils.CreateInstance(type, newMap);
											if(onChange != null) {
												onChange(fieldValue);
											}
											GUIChanged(unityObject);
										}
									}, i);
									menu.AddItem(new GUIContent("Move To Bottom"), false, (obj) => {
										int index = (int)obj;
										if(index != newMap.Count - 1) {
											uNodeEditorUtility.RegisterUndo(unityObject, "");
											uNodeEditorUtility.ListMoveToBottom(newMap, (int)obj);
											fieldValue = ReflectionUtils.CreateInstance(type, newMap);
											if(onChange != null) {
												onChange(fieldValue);
											}
											GUIChanged(unityObject);
										}
									}, i);
									menu.ShowAsContext();
								}
							}
							EditorGUI.indentLevel--;
						}
					}
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.RegisterUndo(unityObject, "");
						fieldValue = ReflectionUtils.CreateInstance(type, newMap);
						if(onChange != null) {
							onChange(fieldValue);
						}
						GUIChanged(unityObject);
					}
				} else {
					DrawNullValue(label, type, delegate (object o) {
						uNodeEditorUtility.RegisterUndo(unityObject, "Create Field Instance");
						if(onChange != null) {
							onChange(o);
						}
						GUIChanged(unityObject);
					});
					EditorGUI.EndChangeCheck();
				}
			} else if(type == typeof(object)) {
				Rect position = GetRect();
				position = EditorGUI.PrefixLabel(position, label);
				if(fieldValue == null) {
					position.width -= 20;
					EditorGUI.LabelField(position, new GUIContent("null", type.PrettyName(true)), EditorStyles.helpBox);
					position.x += position.width;
					position.width = 20;
				} else if(fieldValue.GetType() == typeof(object)) {
					position.width -= 20;
					EditorGUI.LabelField(position, new GUIContent("new object()", type.PrettyName(true)), EditorStyles.helpBox);
					position.x += position.width;
					position.width = 20;
				} else if(fieldValue.GetType().IsCastableTo(typeof(UnityEngine.Object))) {
					position.width -= 20;
					EditValue(position, GUIContent.none, fieldValue, typeof(UnityEngine.Object), onChange, settings);
					position.x += position.width;
					position.width = 20;
				} else if(CanDrawOneLine(fieldValue.GetType())) {
					position.width -= 20;
					EditValue(position, GUIContent.none, fieldValue, fieldValue.GetType(), onChange, settings);
					position.x += position.width;
					position.width = 20;
				} else {
					EditorGUI.indentLevel++;
					EditValueLayouted(new GUIContent("Value"), fieldValue, fieldValue.GetType(), onChange, settings);
					EditorGUI.indentLevel--;
				}
				if(EditorGUI.DropdownButton(position, GUIContent.none, FocusType.Keyboard, EditorStyles.popup)) {
					GUI.changed = false;
					ItemSelector.ShowWindow(null, null, delegate (MemberData member) {
						Type t = member.Get<Type>();
						if(ReflectionUtils.CanCreateInstance(t)) {
							uNodeEditorUtility.RegisterUndo(unityObject, "");
							fieldValue = ReflectionUtils.CreateInstance(t);
							if(onChange != null) {
								onChange(fieldValue);
							}
							GUIChanged(unityObject);
						}
					}, true).ChangePosition(position.ToScreenRect());
				}
				EditorGUI.EndChangeCheck();
			} else if(type.IsClass && !type.IsAbstract) {
				object obj = fieldValue;
				if(obj == null && !settings.nullable) {
					obj = ReflectionUtils.CreateInstance(type);
					fieldValue = obj;
				}
				FieldInfo[] fieldInfo = ReflectionUtils.GetFieldsFromType(type);
				if(fieldInfo != null && fieldInfo.Length > 0) {
					Array.Sort(fieldInfo, (x, y) => {
						if(x.DeclaringType != y.DeclaringType) {
							return string.Compare(x.DeclaringType.IsSubclassOf(y.DeclaringType).ToString(), y.DeclaringType.IsSubclassOf(x.DeclaringType).ToString());
						}
						return string.Compare(x.MetadataToken.ToString(), y.MetadataToken.ToString());
					});
					if(label != GUIContent.none) {
						var pos = EditorGUI.PrefixLabel(GetRect(), label);
						if(uNodeEditorGUI.Button(pos, obj == null ? "null" : "new " + type.PrettyName() + "()", EditorStyles.miniButton)) {
							if(obj != null) {
								if(settings.nullable) {
									obj = null;
								}
							} else {
								obj = ReflectionUtils.CreateInstance(type);
							}
						}
						if(obj != null) {
							EditorGUI.indentLevel++;
							ShowFields(fieldInfo, obj, unityObject);
							EditorGUI.indentLevel--;
						}
					} else {
						if(obj != null) {
							ShowFields(fieldInfo, obj, unityObject);
						}
					}
				} else {
					if(label != GUIContent.none) {
						var pos = EditorGUI.PrefixLabel(GetRect(), label);
						if(uNodeEditorGUI.Button(pos, obj == null ? "null" : "new " + type.PrettyName() + "()", EditorStyles.miniButton)) {
							if(obj != null) {
								if(settings.nullable) {
									obj = null;
								}
							} else {
								obj = ReflectionUtils.CreateInstance(type);
							}
						}
					}
				}
				if(EditorGUI.EndChangeCheck()) {
					fieldValue = obj;
					if(onChange != null) {
						onChange(fieldValue);
					}
				}
			} else {
				EditorGUI.EndChangeCheck();
			}
		}
		#endregion

		/// <summary>
		/// Show a search bar GUI.
		/// </summary>
		/// <param name="searchString"></param>
		/// <param name="label"></param>
		/// <param name="controlName"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		public static string DrawSearchBar(string searchString, GUIContent label, string controlName = null, params GUILayoutOption[] options) {
			EditorGUILayout.BeginHorizontal(GUI.skin.FindStyle("Toolbar"));
			if(controlName != null) {
				GUI.SetNextControlName(controlName);
			}
			searchString = EditorGUILayout.TextField(label, searchString, GUI.skin.FindStyle("ToolbarSeachTextField"), options);
			if(GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton"))) {
				searchString = "";
				GUI.FocusControl(null);
			}
			EditorGUILayout.EndHorizontal();
			return searchString;
		}

		private static Rect lastClickedRect;
		public static bool DragGUIButton(Rect rect, GUIContent label, GUIStyle style, Action onDrag, Action onMouseOver = null) {
			if(rect.Contains(Event.current.mousePosition) && Event.current.button == 0) {
				if(onMouseOver != null) {
					onMouseOver();
				}
				if(Event.current.type == EventType.MouseDown) {
					lastClickedRect = rect;
				}
				if(Event.current.type == EventType.MouseUp) {
					lastClickedRect = Rect.zero;
				}
				if(lastClickedRect == rect && Event.current.type == EventType.MouseDrag) {
					lastClickedRect = Rect.zero;
					if(onDrag != null) {
						onDrag();
					}
				}
			}
			return GUI.Button(rect, label, style);
		}

		public static Rect GUIToScreenRect(Rect rect) {
			Vector2 vector = GUIUtility.GUIToScreenPoint(new Vector2(rect.x, rect.y));
			rect.x = vector.x;
			rect.y = vector.y;
			return rect;
		}

		public static Vector2 GUIToScreenPoint(Vector2 position) {
			return GUIUtility.GUIToScreenPoint(new Vector2(position.x, position.y));
		}

		#region Input/Output Description
		public static void DrawInputOutputDescription(Node node) {
			var graphEditor = uNodeEditor.window?.graphEditor as UIElementGraph;
			if(graphEditor != null && graphEditor.graphView != null) {
				UNodeView nodeView = null;
				if(graphEditor.graphView.nodeViewsPerNode.TryGetValue(node, out nodeView) && nodeView != null) {
					EditorGUILayout.BeginVertical("Box");
					EditorGUILayout.LabelField("Inputs", EditorStyles.centeredGreyMiniLabel);
					foreach(var port in nodeView.inputPorts) {
						if(port.orientation == Orientation.Vertical) {
							EditorGUILayout.LabelField(new GUIContent(port.GetPrettyName() + " : Flow", uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))), EditorStyles.boldLabel);
						} else {
							EditorGUILayout.LabelField(new GUIContent(port.GetPrettyName() + " : " + port.GetPortType().PrettyName(), uNodeEditorUtility.GetTypeIcon(port.GetPortType())), EditorStyles.boldLabel);
						}
						var tooltip = port.GetTooltip();
						if(!string.IsNullOrEmpty(tooltip)) {
							EditorGUI.indentLevel++;
							EditorGUILayout.LabelField(tooltip, EditorStyles.wordWrappedLabel);
							EditorGUI.indentLevel--;
						}
					}
					EditorGUILayout.EndVertical();
					EditorGUILayout.BeginVertical("Box");
					EditorGUILayout.LabelField("Outputs", EditorStyles.centeredGreyMiniLabel);
					foreach(var port in nodeView.outputPorts) {
						if(port.orientation == Orientation.Vertical) {
							EditorGUILayout.LabelField(new GUIContent(port.GetPrettyName() + " : Flow", uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))), EditorStyles.boldLabel);
						} else {
							EditorGUILayout.LabelField(new GUIContent(port.GetPrettyName() + " : " + port.GetPortType().PrettyName(), uNodeEditorUtility.GetTypeIcon(port.GetPortType())), EditorStyles.boldLabel);
						}
						var tooltip = port.GetTooltip();
						if(!string.IsNullOrEmpty(tooltip)) {
							EditorGUI.indentLevel++;
							EditorGUILayout.LabelField(tooltip, EditorStyles.wordWrappedLabel);
							EditorGUI.indentLevel--;
						}
					}
					EditorGUILayout.EndVertical();
					return;
				}
			}
			EditorGUILayout.BeginVertical("Box");
			EditorGUILayout.LabelField("Inputs", EditorStyles.centeredGreyMiniLabel);
			FieldInfo[] fields = EditorReflectionUtility.GetFields(node.GetType());
			Action drawOutput = null;
			if(fields.Length > 0) {
				try {
					bool isPrefab = uNodeEditorUtility.IsPrefab(node);
					if(isPrefab)
						EditorGUI.BeginChangeCheck();
					for(int i = 0; i < fields.Length; i++) {
						FieldInfo field = fields[i];
						if(field.IsDefined(typeof(FieldConnectionAttribute), true)) {
							var attributes = EditorReflectionUtility.GetAttributes(field);
							var FCA = ReflectionUtils.GetAttribute<FieldConnectionAttribute>(attributes);
							bool isFlow = node.IsFlowNode();
							if(FCA.hideOnFlowNode && isFlow) {
								continue;
							}
							if(FCA.hideOnNotFlowNode && !isFlow) {
								continue;
							}
							if(FCA.label == null) {
								FCA.label = new GUIContent(field.Name);
							}
							if(field.FieldType == typeof(MemberData)) {
								MemberData member = field.GetValueOptimized(node) as MemberData;
								if(member == null) {
									continue;
								}
								if(FCA.isFlowConnection) {
									drawOutput += () => {
										EditorGUILayout.LabelField(new GUIContent((string.IsNullOrEmpty(FCA.label.text) ? field.Name : FCA.label.text) + " : Flow", uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))), EditorStyles.boldLabel);
									};
								} else {
									Type type = ReflectionUtils.GetActualFieldType(field, node, ref attributes);
									EditorGUILayout.LabelField(new GUIContent((string.IsNullOrEmpty(FCA.label.text) ? field.Name : FCA.label.text) + " : " + type.PrettyName(), uNodeEditorUtility.GetTypeIcon(type)), EditorStyles.boldLabel);
								}
							} else if(FCA.isFlowConnection && field.FieldType == typeof(List<MemberData>)) {
								drawOutput += () => {
									EditorGUILayout.LabelField(new GUIContent((string.IsNullOrEmpty(FCA.label.text) ? field.Name : FCA.label.text) + " : Flow", uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))), EditorStyles.boldLabel);
								};
							}
						} else if(field.IsDefined(typeof(OutputAttribute), true)) {
							var attributes = EditorReflectionUtility.GetAttributes(field);
							Type FType = ReflectionUtils.GetActualFieldType(field, node, ref attributes);
							var FVA = ReflectionUtils.GetAttribute<OutputAttribute>(attributes);
							drawOutput += () => {
								EditorGUILayout.LabelField(
									new GUIContent(
										(string.IsNullOrEmpty(FVA.label.text) ? field.Name : FVA.label.text) + " : " + FType.PrettyName(),
										uNodeEditorUtility.GetTypeIcon(FType)),
									EditorStyles.boldLabel);
							};
						} else if(field.FieldType == typeof(FlowInput)) {
							string name = field.Name;
							FlowInput flowInput = field.GetValueOptimized(node) as FlowInput;
							if(flowInput != null && !string.IsNullOrEmpty(flowInput.name)) {
								name = flowInput.name;
							}
							EditorGUILayout.LabelField(
								new GUIContent(
									name + " : Flow",
									uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))),
								EditorStyles.boldLabel);
						}
						if(field.IsDefined(typeof(FieldDrawerAttribute), true)) {
							var attributes = EditorReflectionUtility.GetAttributes(field);
							var FD = ReflectionUtils.GetAttribute<FieldDrawerAttribute>(attributes);
							if(FD.label == null) {
								FD.label = new GUIContent(field.Name);
							}
							EditorGUILayout.LabelField(new GUIContent((string.IsNullOrEmpty(FD.label.text) ? field.Name : FD.label.text) + " : " + field.FieldType.PrettyName(), uNodeEditorUtility.GetTypeIcon(field.FieldType)), EditorStyles.boldLabel);
						}
					}
					if(isPrefab && EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.MarkDirty(node);
					}
				}
				catch(UnityEngine.ExitGUIException ex) {
					ex.ToString();
				}
				catch(System.Exception ex) {
					Debug.Log("error on draw field in node window", node);
					Debug.LogException(ex);
				}
			}
			EditorGUILayout.EndVertical();
			EditorGUILayout.BeginVertical("Box");
			EditorGUILayout.LabelField("Outputs", EditorStyles.centeredGreyMiniLabel);
			if(drawOutput != null) {
				drawOutput();
			}
			if(node.CanGetValue() || node.CanSetValue()) {
				Type t = node.ReturnType();
				if(t != null) {
					EditorGUILayout.LabelField(new GUIContent("Result : " + t.PrettyName(), uNodeEditorUtility.GetTypeIcon(t), t.PrettyName(true)), EditorStyles.boldLabel);
				}
			}
			EditorGUILayout.EndHorizontal();
		}

		public static void DrawInputDescription(MultipurposeMember member, Node node) {
			if(member != null && member.target != null) {
				if(member.target.targetType == MemberData.TargetType.Values) {
					EditorGUILayout.LabelField(new GUIContent("Value : " + member.target.type.PrettyName(), uNodeEditorUtility.GetTypeIcon(member.target.type)), EditorStyles.boldLabel);
				} else if(member.target.isTargeted && !member.target.isStatic && !member.target.IsTargetingUNode &&
					member.target.targetType != MemberData.TargetType.Type &&
					member.target.targetType != MemberData.TargetType.Null) {
					EditorGUILayout.LabelField(new GUIContent("Instance : " + member.target.startType.PrettyName(), uNodeEditorUtility.GetTypeIcon(member.target.startType)), EditorStyles.boldLabel);
				} else if(!uNodePreference.GetPreference().hideUNodeInstance && member.target.IsTargetingUNode && !member.target.IsTargetingNode) {
					EditorGUILayout.LabelField(new GUIContent("uNode : " + member.target.startType.PrettyName(), uNodeEditorUtility.GetTypeIcon(member.target.startType)), EditorStyles.boldLabel);
				}
				if(member.target.SerializedItems?.Length > 0) {
					MemberInfo[] members = null;
					if(uNodePreference.GetPreference().inEditorDocumentation) {
						members = member.target.GetMembers(false);
						if(members != null && members.Length > 0 && members.Length + 1 != member.target.SerializedItems.Length) {
							members = null;
						}
					}
					int totalParam = 0;
					int drawCount = 0;
					bool flag = false;
					uNodeFunction objRef = null;
					switch(member.target.targetType) {
						case MemberData.TargetType.uNodeFunction: {
							uNodeRoot root = member.target.GetInstance() as uNodeRoot;
							if(root != null) {
								var gTypes = member.target.GenericTypes[0];
								objRef = root.GetFunction(member.target.startName, gTypes != null ? gTypes.Length : 0, member.target.ParameterTypes[0]);
							}
							break;
						}
					}
					for(int i = 0; i < member.target.SerializedItems.Length; i++) {
						if(i != 0) {
							if(members != null && (member.target.isDeepTarget || !member.target.IsTargetingUNode)) {
								MemberInfo memberInfo = members[i - 1];
								if(memberInfo is MethodInfo || memberInfo is ConstructorInfo) {
									var method = memberInfo as MethodInfo;
									if(flag) {
										EditorGUILayout.Space();
									}
									var documentation = XmlDoc.XMLFromMember(memberInfo);
									//EditorGUILayout.LabelField(new GUIContent(method.Name, GetTypeIcon(method.ReturnType)), EditorStyles.boldLabel);
									EditorGUILayout.LabelField(
										new GUIContent(method != null ? 
											EditorReflectionUtility.GetOverloadingMethodNames(method, false) : 
											EditorReflectionUtility.GetConstructorPrettyName(memberInfo as ConstructorInfo)), 
										EditorStyles.boldLabel);
									EditorGUI.indentLevel++;
									if(documentation != null && documentation["summary"] != null) {
										EditorGUILayout.LabelField(documentation["summary"].InnerText.Trim(), EditorStyles.wordWrappedLabel);
									} else if(memberInfo is ISummary) {
										if(!string.IsNullOrEmpty((memberInfo as ISummary).GetSummary())) {
											EditorGUILayout.LabelField((memberInfo as ISummary).GetSummary(), EditorStyles.wordWrappedLabel);
										}
									}
									EditorGUI.indentLevel--;
									var parameters = method != null ? method.GetParameters() : (memberInfo as ConstructorInfo).GetParameters();
									if(parameters.Length > 0) {
										totalParam += parameters.Length;
										for(int x = 0; x < parameters.Length; x++) {
											System.Type PType = parameters[x].ParameterType;
											if(PType != null) {
												EditorGUILayout.LabelField(new GUIContent(parameters[x].Name + " : " + PType.PrettyName(), uNodeEditorUtility.GetTypeIcon(PType), PType.PrettyName(true)), EditorStyles.boldLabel);
												EditorGUI.indentLevel++;
												if(documentation != null && documentation["param"] != null) {
													XmlNode paramDoc = null;
													XmlNode doc = documentation["param"];
													while(doc.NextSibling != null) {
														if(doc.Attributes["name"] != null && doc.Attributes["name"].Value.Equals(parameters[x].Name)) {
															paramDoc = doc;
															break;
														}
														doc = doc.NextSibling;
													}
													if(paramDoc != null && !string.IsNullOrEmpty(paramDoc.InnerText)) {
														//Show documentation
														EditorGUILayout.LabelField(paramDoc.InnerText.Trim(), EditorStyles.wordWrappedLabel);
													}
												} else if(PType is ISummary) {
													if(!string.IsNullOrEmpty((PType as ISummary).GetSummary())) {
														EditorGUILayout.LabelField((PType as ISummary).GetSummary(), EditorStyles.wordWrappedLabel);
													}
												}
												EditorGUI.indentLevel--;
											}
										}
									}
									drawCount++;
									flag = true;
									continue;
								} else {
									if(flag) {
										EditorGUILayout.Space();
									}
									var documentation = XmlDoc.XMLFromMember(memberInfo);
									//EditorGUILayout.LabelField(new GUIContent(method.Name, GetTypeIcon(method.ReturnType)), EditorStyles.boldLabel);
									EditorGUILayout.LabelField(new GUIContent(memberInfo.Name), EditorStyles.boldLabel);
									EditorGUI.indentLevel++;
									if(documentation != null && documentation["summary"] != null) {
										EditorGUILayout.LabelField(documentation["summary"].InnerText.Trim(), EditorStyles.wordWrappedLabel);
									} else if(memberInfo is ISummary) {
										if(!string.IsNullOrEmpty((memberInfo as ISummary).GetSummary())) {
											EditorGUILayout.LabelField((memberInfo as ISummary).GetSummary(), EditorStyles.wordWrappedLabel);
										}
									}
									EditorGUI.indentLevel--;
									flag = true;
									continue;
								}
							}
						}
						System.Type[] paramsType = member.target.ParameterTypes[i];
						if(paramsType != null && paramsType.Length > 0) {
							if(drawCount > 0)
								EditorGUILayout.LabelField("Method " + (drawCount), EditorStyles.boldLabel);
							totalParam += paramsType.Length;
							for(int x = 0; x < paramsType.Length; x++) {
								System.Type PType = paramsType[x];
								if(PType != null) {
									GUIContent label;
									if(objRef != null && drawCount == 0) {
										label = new GUIContent(
											objRef.parameters[x].name + " : " + PType.PrettyName(),
											uNodeEditorUtility.GetTypeIcon(PType),
											PType.PrettyName(true));
									} else {
										label = new GUIContent(
											"P" + x + " : " + PType.PrettyName(),
											uNodeEditorUtility.GetTypeIcon(PType),
											PType.PrettyName(true));
									}
									EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
								}
							}
							drawCount++;
						}
					}
				}
			}
		}

		public static void DrawInitializer(MultipurposeMember member, UnityEngine.Object unityObject) {
			if(member.target.isAssigned) {
				if(member.target.targetType == MemberData.TargetType.Values || member.target.targetType == MemberData.TargetType.Constructor) {
					if(member.initializer == null) {
						member.initializer = new ValueData();
					}
					DrawConstructorInitializer(member.initializer, member.target.type, unityObject, true);
				}
			}
		}

		public static void DrawOutputDescription(MultipurposeNode node) {
			EditorGUILayout.BeginVertical("Box");
			EditorGUILayout.LabelField("Outputs", EditorStyles.centeredGreyMiniLabel);
			if(node.IsFlowNode()) {
				EditorGUILayout.LabelField(new GUIContent("Next : Flow", uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))), EditorStyles.boldLabel);
			}
			if(node.CanGetValue() || node.CanSetValue()) {
				Type t = node.ReturnType();
				if(t != null) {
					EditorGUILayout.LabelField(new GUIContent("Result : " + t.PrettyName(), uNodeEditorUtility.GetTypeIcon(t), t.PrettyName(true)), EditorStyles.boldLabel);
				}
			}
			EditorGUILayout.EndHorizontal();
		}
		#endregion

		#region SerializedPropertyUtility
		public static void ShowChildArrayProperty(int arrayIndex, SerializedProperty property, bool onlyVisible = true, int childCount = 999) {
			if(property.arraySize > arrayIndex) {
				SerializedProperty MyListRef = property.GetArrayElementAtIndex(arrayIndex);
				DisplayChildProperty(MyListRef, onlyVisible, childCount);
			}
		}

		public static void DisplayChildProperty(SerializedProperty property, bool onlyVisible = true, int childCount = 999) {
			SerializedProperty prop = property.Copy();
			for(int k = 0; k < childCount; k++) {
				if(k == 0) {
					if(onlyVisible) {
						prop.NextVisible(true);
					} else {
						prop.Next(true);
					}
					if(property.FindPropertyRelative(prop.name) == null) {
						break;
					}
				} else {
					if(onlyVisible) {
						prop.NextVisible(false);
					} else {
						prop.Next(false);
					}
					if(property.FindPropertyRelative(prop.name) == null) {
						break;
					}
				}
				EditorGUILayout.PropertyField(prop, true);
			}
		}
		#endregion

		#region Styles
		public static class Styles {
			public static readonly GUIStyle PRLabel = new GUIStyle("PR Label");
			public static readonly GUIStyle itemStatic;
			public static readonly GUIStyle itemNormal;
			public static readonly GUIStyle itemBackground;
			public static readonly GUIStyle itemBackground2;
			public static readonly GUIStyle itemBackgroundStyle = new GUIStyle("CN Box");
			public static readonly Texture2D errorIcon;
			public static readonly Texture2D warningIcon;
			public static readonly Texture2D messageIcon;

			public static readonly GUIStyle insertionStyle;

			public static readonly GUIStyle itemSelect = new GUIStyle("flow varPin in");
			public static readonly GUIStyle itemNext = new GUIStyle("AC RightArrow");

			private static GUIStyle _headerStyle;
			public static GUIStyle headerStyle {
				get {
					if(_headerStyle == null) {
						_headerStyle = new GUIStyle(EditorStyles.label);
						_headerStyle.overflow = ((GUIStyle)"RL Header").overflow;
						_headerStyle.border = ((GUIStyle)"RL Header").border;
						_headerStyle.padding.left += 5;
						_headerStyle.margin.top += 1;
						_headerStyle.margin.bottom += 2;
						_headerStyle.overflow.top += 1;
						_headerStyle.overflow.bottom += 2;
						_headerStyle.normal.background = ((GUIStyle)"RL Header").normal.background;
						_headerStyle.active.background = ((GUIStyle)"RL Header").active.background;
					}
					return _headerStyle;
				}
			}

			public static Texture2D favoriteIconOff {
				get {
					return EditorGUIUtility.FindTexture("d_Favorite");
				}
			}

			public static Texture2D favoriteIconOn {
				get {
					return EditorGUIUtility.FindTexture("Favorite Icon");
				}
			}


			public static GUIStyle backgroundStyle {
				get {
					return "RL Background";
				}
			}

			public static GUIStyle footerStyle {
				get {
					return "RL Footer";
				}
			}

			static Texture2D _selectedTexture;
			public static Texture2D selectedTexture {
				get {
					if(_selectedTexture == null) {
						_selectedTexture = uNodeEditorUtility.MakeTexture(1, 1, new Color(0.24f, 0.49f, 0.91f));
					}
					return _selectedTexture;
				}
			}

			public static Texture2D GetVisiblilityTexture(bool visible) {
				if(visible) {
					return EditorGUIUtility.isProSkin ?
						EditorGUIUtility.FindTexture("d_animationvisibilitytoggleon") : EditorGUIUtility.FindTexture("animationvisibilitytoggleon");
				} else {
					return EditorGUIUtility.isProSkin ?
						EditorGUIUtility.FindTexture("d_animationvisibilitytoggleoff") : EditorGUIUtility.FindTexture("animationvisibilitytoggleoff");
				}
			}

			static Styles() {
				itemNormal = EditorStyles.label;
				itemNormal.richText = true;
				itemStatic = new GUIStyle(EditorStyles.label);
				itemStatic.fontStyle = FontStyle.Bold;
				itemStatic.richText = true;
				itemBackground = new GUIStyle("CN EntryBackEven");
				itemBackground2 = new GUIStyle("CN EntryBackOdd");
				errorIcon = EditorGUIUtility.FindTexture("d_console.erroricon.sml");
				warningIcon = EditorGUIUtility.FindTexture("d_console.warnicon.sml");
				messageIcon = EditorGUIUtility.FindTexture("d_console.infoicon.sml");

				insertionStyle = (GUIStyle)"PR Insertion";
				insertionStyle.overflow = new RectOffset(4, 0, 0, 0);
			}

			static GUIStyle m_RichLabel;
			/// <summary>
			/// Rich label style with word wrapped.
			/// </summary>
			public static GUIStyle RichLabel {
				get {
					if(m_RichLabel == null) {
						m_RichLabel = new GUIStyle(GUI.skin.label);
						m_RichLabel.richText = true;
						m_RichLabel.wordWrap = true;
					}

					return m_RichLabel;
				}
			}

			static GUIStyle m_RichLabel2;
			/// <summary>
			/// Rich label style without word wrapped.
			/// </summary>
			public static GUIStyle RichLabel2 {
				get {
					if(m_RichLabel2 == null) {
						m_RichLabel2 = new GUIStyle(GUI.skin.label);
						m_RichLabel2.richText = true;
					}

					return m_RichLabel2;
				}
			}

			static GUIStyle m_CenterRichLabel;
			/// <summary>
			/// Rich label style without word wrapped.
			/// </summary>
			public static GUIStyle CenterRichLabel {
				get {
					if(m_CenterRichLabel == null) {
						m_CenterRichLabel = new GUIStyle(GUI.skin.label);
						m_CenterRichLabel.richText = true;
						m_CenterRichLabel.alignment = TextAnchor.MiddleCenter;
					}

					return m_CenterRichLabel;
				}
			}

			public static GUIStyle LeftMiniButton {
				get {
					return ButtonRichText;
				}
			}

			static GUIStyle m_MiniButtonRichText;
			public static GUIStyle ButtonRichText {
				get {
					if(m_MiniButtonRichText == null) {
						m_MiniButtonRichText = new GUIStyle(EditorStyles.miniButton);
						m_MiniButtonRichText.richText = true;
						//m_MiniButtonRichText.wordWrap = true;
						m_MiniButtonRichText.alignment = TextAnchor.MiddleLeft;
					}

					return m_MiniButtonRichText;
				}
			}

			static GUIStyle m_FoldoutBold;
			public static GUIStyle FoldoutBold {
				get {
					if(m_FoldoutBold == null) {
						m_FoldoutBold = new GUIStyle(EditorStyles.foldout);
						m_FoldoutBold.fontStyle = FontStyle.Bold;
					}

					return m_FoldoutBold;
				}
			}

			static GUIStyle _whiteBoldLabel;
			public static GUIStyle whiteBoldLabel {
				get {
					if(_whiteBoldLabel == null) {
						_whiteBoldLabel = new GUIStyle(EditorStyles.whiteLabel);
						_whiteBoldLabel.fontStyle = FontStyle.Bold;
						_whiteBoldLabel.normal.textColor = Color.white;
						_whiteBoldLabel.active.textColor = Color.white;
					}
					return _whiteBoldLabel;
				}
			}

			static GUIStyle _whiteLabel;
			public static GUIStyle whiteLabel {
				get {
					if(_whiteLabel == null) {
						_whiteLabel = new GUIStyle(EditorStyles.label);
						_whiteLabel.normal.textColor = Color.white;
						_whiteLabel.active.textColor = Color.white;
					}
					return _whiteLabel;
				}
			}

			static GUIStyle _popup;
			public static GUIStyle popupStyle {
				get {
					if(_popup == null) {
						_popup = new GUIStyle(EditorStyles.popup);
						_popup.richText = true;
					}
					return _popup;
				}
			}

			static GUIStyle _labelStyle;
			public static GUIStyle labelStyle {
				get {
					if(_labelStyle == null) {
						_labelStyle = new GUIStyle(EditorStyles.label);
						_labelStyle.margin.bottom = 0;
						_labelStyle.margin.top = 0;
					}
					return _labelStyle;
				}
			}

			public static GUIStyle objectField {
				get {
#if UNITY_2019_4_OR_NEWER
					return "ObjectFieldButton";
#else
					return EditorStyles.objectField;
#endif
				}
			}
		}
		#endregion
	}
}