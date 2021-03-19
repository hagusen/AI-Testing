using UnityEngine;
using UnityEditor;
using MaxyGames.Events;
using MaxyGames.Runtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEditor.Callbacks;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using MaxyGames.OdinSerializer.Editor;
using Object = UnityEngine.Object;

namespace MaxyGames.uNode.Editors {
	/// <summary>
	/// Provides function to initialize useful function.
	/// </summary>
	[InitializeOnLoad]
	public class uNodeEditorInitializer {
		static Texture uNodeIcon;
		static Texture2D _backgroundIcon;
		static Texture2D backgroundIcon { 
			get {
				if(_backgroundIcon == null) {
					if(EditorGUIUtility.isProSkin) {
						_backgroundIcon = uNodeEditorUtility.MakeTexture(1, 1, new Color(0.2196079f, 0.2196079f, 0.2196079f));
					} else {
						_backgroundIcon = uNodeEditorUtility.MakeTexture(1, 1, new Color(0.7607844f, 0.7607844f, 0.7607844f));
					}
				}
				return _backgroundIcon;
			}
		}
		static List<int> markedObjects = new List<int>();
		static HashSet<string> assetGUIDs = new HashSet<string>();
		static Dictionary<string, UnityEngine.Object> markedAssets = new Dictionary<string, UnityEngine.Object>();

		static uNodeEditorInitializer() {
			EditorApplication.hierarchyWindowItemOnGUI += HierarchyItem;
			EditorApplication.projectWindowItemOnGUI += ProjectItem;
			Selection.selectionChanged += OnSelectionChanged;
			SceneView.duringSceneGui += OnSceneGUI;
			EditorApplication.update += Update;
			// Setup();

			#region Bind Init
			uNodeUtility.getActualObject = (obj) => {
				if(obj == null) return null;
				if(uNodeEditorUtility.IsPrefabInstance(obj)) {
					return PrefabUtility.GetCorrespondingObjectFromSource(obj);
				} else if(uNodeEditorUtility.IsPrefab(obj)) {
					return obj;
				} else {
					return uNodeEditorUtility.GetObjectSource(obj, null, obj.GetType()) ?? obj;
				}
			};
			EditorBinding.getPrefabParent += (obj) => {
				if(obj == null)
					return null;
				return PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
			};
			EditorApplication.playModeStateChanged += OnPlayModeChanged;
			//UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += EditorBinding.onSceneChanged;
			var typePatcher = "MaxyGames.uNode.Editors.TypePatcher".ToType(false);
			if(typePatcher != null) {
				var method = typePatcher.GetMethod("Patch", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
				EditorBinding.patchType = (System.Action<System.Type, System.Type>)System.Delegate.CreateDelegate(typeof(System.Action<System.Type, System.Type>), method);
			}
			if(EditorApplication.isPlayingOrWillChangePlaymode) {
#if UNODE_COMPILE_ON_PLAY
				uNodeThreadUtility.Queue(() => {
					GenerationUtility.CompileProjectGraphsAnonymous();
				});
#endif
			}
			#endregion
		}

		private static void OnPlayModeChanged(PlayModeStateChange state) {
			switch(state) {
				case PlayModeStateChange.ExitingEditMode:
					//case PlayModeStateChange.ExitingPlayMode:
					//Make sure we save all temporary graph on exit play mode or edit mode.
					GraphUtility.SaveAllGraph(false);
					if(uNodeEditor.window != null) {
						uNodeEditor.window.SaveEditorData();
					}
#if UNITY_2019_3_OR_NEWER
						//If play mode options is enable and domain reload is disable
						if(EditorSettings.enterPlayModeOptionsEnabled && EditorSettings.enterPlayModeOptions.HasFlags(EnterPlayModeOptions.DisableDomainReload)) {
							//then enable is playing and clean graph cache
							uNodeUtility.isPlaying = true;
							UGraphView.ClearCache();
#if UNODE_COMPILE_ON_PLAY
							//Do compile graphs project in temporary folder and load it when using auto compile on play
							GenerationUtility.CompileProjectGraphsAnonymous();
#endif
					}
#endif
					break;
				case PlayModeStateChange.EnteredEditMode:
					//If user is saving graph in play mode
					if(EditorPrefs.GetBool("unode_graph_saved_in_playmode", false)) {
						//Ensure the saved graph in play mode keep the changes.
						GraphUtility.DestroyTempGraph();
						EditorPrefs.SetBool("unode_graph_saved_in_playmode", false);
					}
#if UNITY_2019_3_OR_NEWER
					//If play mode options is enable and domain reload is disable
					if(EditorSettings.enterPlayModeOptionsEnabled && EditorSettings.enterPlayModeOptions.HasFlags(EnterPlayModeOptions.DisableDomainReload)) {
						//then clear graph cache
						UGraphView.ClearCache();
						//Set is playing to false
						uNodeUtility.isPlaying = true;
						return;
					}
#endif
					uNodeThreadUtility.ExecuteAfter(5, () => {
						UGraphView.ClearCache();
					});
					if(uNodeEditor.window != null) {
						EditorApplication.delayCall += () => uNodeEditor.window.Refresh(true);
					}
					//EditorBinding.restorePatch?.Invoke();
					break;
				case PlayModeStateChange.EnteredPlayMode:
					//This will prevent destroying temp graphs in play mode
					GraphUtility.PreventDestroyOnPlayMode();
					break;
				case PlayModeStateChange.ExitingPlayMode:
					//Update the assembly
					ReflectionUtils.UpdateAssemblies();
					//Clean compiled runtime assembly so the runtime type is cannot be loaded again
					ReflectionUtils.CleanRuntimeAssembly();
					break;
			}
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		static void Reload() {
			// Setup();
			uNodeThreadUtility.ExecuteAfter(1, OnSelectionChanged);
		}

		[InitializeOnLoadMethod]
		static void Setup() {
			uNodeIcon = uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.UNodeIcon));
			UpdateMarkedObject();
			uNodeUtility.hideRootObject = () => uNodePreference.GetPreference().hideChildObject;

			#region uNodeUtils Init
			uNodeUtility.isInEditor = true;
			uNodeUtility.enableLogger = true;
			if(uNodeUtility.guiChanged == null) {
				uNodeUtility.guiChanged += uNodeEditor.GUIChanged;
			}
			if(uNodeUtility.RegisterCompleteObjectUndo == null) {
				uNodeUtility.RegisterCompleteObjectUndo = uNodeEditorUtility.RegisterUndo;
			}
			if(uNodeUtility.richTextColor == null) {
				uNodeUtility.richTextColor = () => {
					if(uNodePreference.editorTheme != null) {
						return uNodePreference.editorTheme.textSettings;
					} else {
						return new EditorTextSetting();
					}
				};
			}
			if(uNodeUtility.getColorForType == null) {
				uNodeUtility.getColorForType = (t) => {
					return uNodePreference.GetColorForType(t);
				};
			}
			if(uNodeUtility.getObjectID == null) {
				uNodeUtility.getObjectID = delegate (UnityEngine.Object obj) {
					if(obj == null)
						return 0;
					if(uNodeEditorUtility.IsPrefab(obj)) {
						return (int)Unsupported.GetLocalIdentifierInFileForPersistentObject(obj);
					} else if(uNodeEditorUtility.IsPrefabInstance(obj)) {
						var o = PrefabUtility.GetCorrespondingObjectFromSource(obj);
						if(o == null)
							return obj.GetInstanceID();
						return (int)Unsupported.GetLocalIdentifierInFileForPersistentObject(o);
					}
					var result = uNodeEditorUtility.GetObjectSource(obj, null, obj.GetType());
					if (result == null || !EditorUtility.IsPersistent(result)) {
						return obj.GetInstanceID();
					}
					return (int)Unsupported.GetLocalIdentifierInFileForPersistentObject(result);
				};
			}
			if(uNodeUtility.debugObject == null) {
				uNodeUtility.debugObject = delegate () {
					if(uNodeEditor.window != null) {
						return uNodeEditor.window.debugObject;
					}
					return null;
				};
			}
			if(uNodeUtility.hasBreakpoint == null) {
				uNodeUtility.hasBreakpoint = delegate (int id) {
					return nodeDebugData.Contains(id);
				};
			}
			if(uNodeUtility.addBreakpoint == null) {
				uNodeUtility.addBreakpoint = delegate (int id) {
					if(!nodeDebugData.Contains(id)) {
						nodeDebugData.Add(id);
						SaveDebugData();
					}
				};
			}
			if(uNodeUtility.removeBreakpoint == null) {
				uNodeUtility.removeBreakpoint = delegate (int id) {
					if(nodeDebugData.Contains(id)) {
						nodeDebugData.Remove(id);
						SaveDebugData();
					}
				};
			}
			#endregion

			#region uNodeDEBUG Init
			if(uNodeDEBUG.InvokeEvent == null) {
				uNodeDEBUG.InvokeEvent = delegate (EventCoroutine coroutine, int objectUID, int nodeUID) {
					if(!uNodeUtility.useDebug || !Application.isPlaying)
						return;
					Dictionary<object, uNodeUtility.DebugData> debugMap = null;
					if(uNodeUtility.debugData.ContainsKey(objectUID)) {
						debugMap = uNodeUtility.debugData[objectUID];
					} else {
						debugMap = new Dictionary<object, uNodeUtility.DebugData>();
						uNodeUtility.debugData.Add(objectUID, debugMap);
					}
					object obj = coroutine.owner;
					uNodeUtility.DebugData data = null;
					if(debugMap.ContainsKey(obj)) {
						data = debugMap[obj];
					} else {
						data = new uNodeUtility.DebugData();
						debugMap.Add(obj, data);
					}
					uNodeUtility.DebugData.NodeDebug nodeDebug = null;
					if(data.nodeDebug.ContainsKey(nodeUID)) {
						nodeDebug = data.nodeDebug[nodeUID];
					} else {
						nodeDebug = new uNodeUtility.DebugData.NodeDebug();
						data.nodeDebug[nodeUID] = nodeDebug;
					}
					nodeDebug.customCondition = delegate () {
						if(string.IsNullOrEmpty(coroutine.state) || !coroutine.IsFinished) {
							return StateType.Running;
						} else {
							return coroutine.IsSuccess ? StateType.Success : StateType.Failure;
						}
					};
					nodeDebug.calledTime = Time.unscaledTime;
				};
			}
			if(uNodeDEBUG.InvokeEventNode == null) {
				uNodeDEBUG.InvokeEventNode = uNodeUtility.InvokeNode;
			}
			if(uNodeDEBUG.InvokeFlowNode == null) {
				uNodeDEBUG.InvokeFlowNode = uNodeUtility.InvokeFlowTransition;
			}
			if(uNodeDEBUG.InvokeNodeTransition == null) {
				uNodeDEBUG.InvokeNodeTransition = uNodeUtility.InvokeNodeTransition;
			}
			if(uNodeDEBUG.InvokeTransition == null) {
				uNodeDEBUG.InvokeTransition = uNodeUtility.InvokeTransition;
			}
			if(uNodeDEBUG.invokeValueNode == null) {
				uNodeDEBUG.invokeValueNode = (a, b, c, d) => {
					uNodeUtility.InvokeValueTransition(a, b, c, (int)(d as object[])[0], (d as object[])[1], (bool)(d as object[])[2]);
				};
			}
			#endregion

			#region EventDataDrawer Init
			if(EventDataDrawer.customMenu == null) {
				var menu = new List<CustomEventMenu>();
				menu.Add(new CustomEventMenu() {
					isSeparator = true,
				});
				menu.Add(new CustomEventMenu() {
					menuName = "EqualityComparer",
					isValidationMenu = true,
					filter = new FilterAttribute() { HideTypes = new List<System.Type>() { typeof(void) } },
					onClickItem = delegate (MemberData m) {
						EqualityCompare v = new EqualityCompare() { target = new MultipurposeMember() { target = m } };
						return new EventActionData(v, EventActionData.EventType.Event);
					},
				});
				//menu.Add(new CustomEventMenu() {
				//	menuName = "ConditionValidation",
				//	isValidationMenu = true,
				//	filter = new FilterAttribute(typeof(bool)) { MaxMethodParam = int.MaxValue },
				//	onClickItem = delegate (MemberData m) {
				//		MethodValidation v = new MethodValidation() { target = new MultipurposeMember() { target = m } };
				//		MemberDataUtility.UpdateMultipurposeMember(v.target);
				//		return new EventActionData(v, EventActionData.EventType.Event);
				//	},
				//});
				menu.Add(new CustomEventMenu() {
					menuName = "Compare Object",
					isValidationMenu = true,
					filter = new FilterAttribute() { HideTypes = new List<System.Type>() { typeof(void) } },
					onClickItem = delegate (MemberData m) {
						ObjectCompare v = new ObjectCompare();
						v.targetA = new MultipurposeMember() { target = m };
						MemberDataUtility.UpdateMultipurposeMember(v.targetA);
						return new EventActionData(v, EventActionData.EventType.Event);
					},
				});
				menu.Add(new CustomEventMenu() {
					isSeparator = true,
					isValidationMenu = true
				});
				menu.Add(new CustomEventMenu() {
					menuName = "Invoke or GetValue",
					filter = new FilterAttribute() { MaxMethodParam = int.MaxValue, VoidType = true },
					onClickItem = delegate (MemberData m) {
						GetValue v = new GetValue() { target = new MultipurposeMember() { target = m } };
						MemberDataUtility.UpdateMultipurposeMember(v.target);
						return new EventActionData(v, EventActionData.EventType.Event);
					},
				});
				menu.Add(new CustomEventMenu() {
					menuName = "SetValue",
					filter = new FilterAttribute() { SetMember = true },
					onClickItem = delegate (MemberData m) {
						SetValue v = new SetValue() { target = m };
						return new EventActionData(v, EventActionData.EventType.Event);
					},
				});
				EventDataDrawer.customMenu = menu;
			}
			if(EventDataDrawer.dragAndDropCapturer == null) {
				var drag = new List<DragAndDropCapturer>();
				drag.Add(new DragAndDropCapturer() {
					validation = (x) => {
						if(DragAndDrop.GetGenericData("uNode") != null || DragAndDrop.visualMode == DragAndDropVisualMode.None && DragAndDrop.objectReferences.Length == 1) {
							return true;
						}
						return false;
					},
					onDragPerformed = (ed, z) => {
						var MPos = uNodeGUIUtility.GUIToScreenPoint(Event.current.mousePosition);
						if(DragAndDrop.GetGenericData("uNode") != null) {
							var generic = DragAndDrop.GetGenericData("uNode");
							var UT = DragAndDrop.GetGenericData("uNode-Target") as UnityEngine.Object;
							if(generic is uNodeFunction) {
								var function = generic as uNodeFunction;
								EventData ED = ed;
								UnityEngine.Object UO = z;
								GetValue val = new GetValue();
								val.target.target = MemberData.CreateFromValue(function);
								MemberDataUtility.UpdateMultipurposeMember(val.target);
								if(UO)
									uNodeEditorUtility.RegisterUndo(UO, "Add new Event");
								ED.blocks.Add(new EventActionData(val));
							} else if(generic is uNodeProperty) {
								var property = generic as uNodeProperty;
								GenericMenu menu = new GenericMenu();
								if(property.CanGetValue()) {
									menu.AddItem(new GUIContent("Get"), false, (y) => {
										EventData ED = (y as object[])[0] as EventData;
										UnityEngine.Object UO = (y as object[])[1] as UnityEngine.Object;
										GetValue val = new GetValue();
										val.target.target = MemberData.CreateFromValue(property, UT as IPropertySystem);
										MemberDataUtility.UpdateMultipurposeMember(val.target);
										if(UO)
											uNodeEditorUtility.RegisterUndo(UO, "Add new Event");
										ED.blocks.Add(new EventActionData(val));
									}, new object[] { ed, z });
								}
								if(property.CanSetValue()) {
									menu.AddItem(new GUIContent("Set"), false, (y) => {
										EventData ED = (y as object[])[0] as EventData;
										UnityEngine.Object UO = (y as object[])[1] as UnityEngine.Object;
										SetValue val = new SetValue();
										var mData = MemberData.CreateFromValue(property, UT as IPropertySystem);
										val.target = mData;
										if(mData.type != null) {
											if(ReflectionUtils.CanCreateInstance(mData.type)) {
												val.value = new MemberData(ReflectionUtils.CreateInstance(mData.type));
											} else {
												val.value = MemberData.CreateFromValue(null, mData.type);
											}
										}
										if(UO)
											uNodeEditorUtility.RegisterUndo(UO, "Add new Event");
										ED.blocks.Add(new EventActionData(val));
									}, new object[] { ed, z });
								}
								menu.ShowAsContext();
							} else if(generic is VariableData) {
								var varData = generic as VariableData;
								GenericMenu menu = new GenericMenu();
								menu.AddItem(new GUIContent("Get"), false, (y) => {
									EventData ED = (y as object[])[0] as EventData;
									UnityEngine.Object UO = (y as object[])[1] as UnityEngine.Object;
									GetValue val = new GetValue();
									val.target.target = MemberData.CreateFromValue(varData, UT);
									MemberDataUtility.UpdateMultipurposeMember(val.target);
									if(UO)
										uNodeEditorUtility.RegisterUndo(UO, "Add new Event");
									ED.blocks.Add(new EventActionData(val));
								}, new object[] { ed, z });
								menu.AddItem(new GUIContent("Set"), false, (y) => {
									EventData ED = (y as object[])[0] as EventData;
									UnityEngine.Object UO = (y as object[])[1] as UnityEngine.Object;
									var mData = MemberData.CreateFromValue(varData, UT);
									SetValue val = new SetValue();
									val.target = mData;
									if(mData.type != null) {
										if(ReflectionUtils.CanCreateInstance(mData.type)) {
											val.value = new MemberData(ReflectionUtils.CreateInstance(mData.type));
										} else {
											val.value = MemberData.CreateFromValue(null, mData.type);
										}
									}
									if(UO)
										uNodeEditorUtility.RegisterUndo(UO, "Add new Event");
									ED.blocks.Add(new EventActionData(val));
								}, new object[] { ed, z });
								menu.ShowAsContext();
							}
						} else {
							GenericMenu menu = new GenericMenu();
							var unityObject = DragAndDrop.objectReferences[0];
							System.Action<Object, string> action = (dOBJ, startName) => {
								menu.AddItem(new GUIContent(startName + "Get"), false, (y) => {
									EventData ED = (y as object[])[0] as EventData;
									UnityEngine.Object UO = (y as object[])[1] as UnityEngine.Object;
									FilterAttribute filter = new FilterAttribute();
									filter.MaxMethodParam = int.MaxValue;
									filter.VoidType = true;
									filter.Public = true;
									filter.Instance = true;
									filter.Static = false;
									filter.DisplayDefaultStaticType = false;
									filter.InvalidTargetType = MemberData.TargetType.Null | MemberData.TargetType.Values;
									var customItems = ItemSelector.MakeCustomItems(dOBJ.GetType(), filter);
									if(customItems != null) {
										ItemSelector w = ItemSelector.ShowWindow(dOBJ, new MemberData(dOBJ, MemberData.TargetType.SelfTarget), filter, delegate (MemberData value) {
											value.instance = dOBJ;
											GetValue val = new GetValue();
											val.target.target = value;
											MemberDataUtility.UpdateMultipurposeMember(val.target);
											if(UO)
												uNodeEditorUtility.RegisterUndo(UO, "Add new Event");
											ED.blocks.Add(new EventActionData(val));
										}, customItems).ChangePosition(MPos);
										w.displayDefaultItem = false;
									}
								}, new object[] { ed, z });
								menu.AddItem(new GUIContent(startName + "Set"), false, (y) => {
									EventData ED = (y as object[])[0] as EventData;
									UnityEngine.Object UO = (y as object[])[1] as UnityEngine.Object;
									FilterAttribute filter = new FilterAttribute();
									filter.Public = true;
									filter.Instance = true;
									filter.Static = false;
									filter.SetMember = true;
									filter.DisplayDefaultStaticType = false;
									filter.InvalidTargetType = MemberData.TargetType.Null | MemberData.TargetType.Values;
									var customItems = ItemSelector.MakeCustomItems(dOBJ.GetType(), filter);
									if(customItems != null) {
										ItemSelector w = ItemSelector.ShowWindow(dOBJ, new MemberData(dOBJ, MemberData.TargetType.SelfTarget), filter, delegate (MemberData value) {
											value.instance = dOBJ;
											SetValue val = new SetValue();
											val.target = value;
											if(value.type != null) {
												if(ReflectionUtils.CanCreateInstance(value.type)) {
													val.value = new MemberData(ReflectionUtils.CreateInstance(value.type));
												} else {
													val.value = MemberData.CreateFromValue(null, value.type);
												}
											}
											if(UO)
												uNodeEditorUtility.RegisterUndo(UO, "Add new Event");
											ED.blocks.Add(new EventActionData(val));
										}, customItems).ChangePosition(MPos);
										w.displayDefaultItem = false;
									}
								}, new object[] { ed, z });
							};
							action(unityObject, "");
							menu.AddSeparator("");
							if(unityObject is GameObject) {
								Component[] components = (unityObject as GameObject).GetComponents<Component>();
								foreach(var c in components) {
									action(c, c.GetType().Name + "/");
								}
							} else if(unityObject is Component) {
								action((unityObject as Component).gameObject, "GameObject/");
								Component[] components = (unityObject as GameObject).GetComponents<Component>();
								foreach(var c in components) {
									action(c, c.GetType().Name + "/");
								}
							}
							menu.ShowAsContext();
						}
					}
				});
				EventDataDrawer.dragAndDropCapturer = drag;
			}
			#endregion

			#region Completions
			if(CompletionEvaluator.completionToNode == null) {
				CompletionEvaluator.completionToNode = (CompletionInfo completion, GraphEditorData editorData, Vector2 graphPosition) => {
					NodeComponent result = null;
					if(completion.isKeyword) {
						switch(completion.keywordKind) {
							case KeywordKind.As:
								NodeEditorUtility.AddNewNode<Nodes.ASNode>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.Break:
								NodeEditorUtility.AddNewNode<NodeJumpStatement>(editorData,
									graphPosition,
									(node) => {
										node.statementType = JumpStatementType.Break;
										result = node;
									});
								break;
							case KeywordKind.Continue:
								NodeEditorUtility.AddNewNode<NodeJumpStatement>(editorData,
									graphPosition,
									(node) => {
										node.statementType = JumpStatementType.Continue;
										result = node;
									});
								break;
							case KeywordKind.Default:
								NodeEditorUtility.AddNewNode<Nodes.DefaultNode>(editorData,
									graphPosition,
									(node) => {
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.type = member;
											}
										}
										result = node;
									});
								break;
							case KeywordKind.For:
								NodeEditorUtility.AddNewNode<Nodes.ForNumberLoop>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.Foreach:
								NodeEditorUtility.AddNewNode<Nodes.ForeachLoop>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.If:
								NodeEditorUtility.AddNewNode<Nodes.NodeIf>(editorData,
									graphPosition,
									(node) => {
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.condition = member;
											}
										}
										result = node;
									});
								break;
							case KeywordKind.Is:
								NodeEditorUtility.AddNewNode<Nodes.ISNode>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.Lock:
								NodeEditorUtility.AddNewNode<Nodes.NodeLock>(editorData,
									graphPosition,
									(node) => {
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.target = member;
											}
										}
										result = node;
									});
								break;
							case KeywordKind.Return:
								NodeEditorUtility.AddNewNode<NodeReturn>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.Switch:
								NodeEditorUtility.AddNewNode<Nodes.NodeSwitch>(editorData,
									graphPosition,
									(node) => {
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.target = member;
											}
										}
										result = node;
									});
								break;
							case KeywordKind.Throw:
								NodeEditorUtility.AddNewNode<Nodes.NodeThrow>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.Try:
								NodeEditorUtility.AddNewNode<Nodes.NodeTry>(editorData,
									graphPosition,
									(node) => {
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.Try = member;
											}
										}
										result = node;
									});
								break;
							case KeywordKind.Using:
								NodeEditorUtility.AddNewNode<Nodes.NodeUsing>(editorData,
									graphPosition,
									(node) => {
										result = node;
									});
								break;
							case KeywordKind.While:
								NodeEditorUtility.AddNewNode<Nodes.WhileLoop>(editorData,
									graphPosition,
									(node) => {
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.condition = member;
											}
										}
										result = node;
									});
								break;
						}
					} else if(completion.isSymbol) {
						switch(completion.name) {
							case "+":
							case "-":
							case "*":
							case "/":
							case "%":
								NodeEditorUtility.AddNewNode<Nodes.MultiArithmeticNode>(editorData,
									graphPosition,
									(node) => {
										if(completion.genericCompletions != null && completion.genericCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.genericCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.targets[0] = member;
											}
										}
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.targets[1] = member;
											}
										}
										if(completion.name == "+") {
											node.operatorType = ArithmeticType.Add;
										} else if(completion.name == "-") {
											node.operatorType = ArithmeticType.Subtract;
										} else if(completion.name == "*") {
											node.operatorType = ArithmeticType.Multiply;
										} else if(completion.name == "/") {
											node.operatorType = ArithmeticType.Divide;
										} else if(completion.name == "%") {
											node.operatorType = ArithmeticType.Modulo;
										}
										result = node;
									});
								break;
							case "==":
							case "!=":
							case ">":
							case ">=":
							case "<":
							case "<=":
								NodeEditorUtility.AddNewNode<Nodes.ComparisonNode>(editorData,
									graphPosition,
									(node) => {
										if(completion.genericCompletions != null && completion.genericCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.genericCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.targetA = member;
											}
										}
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.targetB = member;
											}
										}
										if(completion.name == "==") {
											node.operatorType = ComparisonType.Equal;
										} else if(completion.name == "!=") {
											node.operatorType = ComparisonType.NotEqual;
										} else if(completion.name == ">") {
											node.operatorType = ComparisonType.GreaterThan;
										} else if(completion.name == ">=") {
											node.operatorType = ComparisonType.GreaterThanOrEqual;
										} else if(completion.name == "<") {
											node.operatorType = ComparisonType.LessThan;
										} else if(completion.name == "<=") {
											node.operatorType = ComparisonType.LessThanOrEqual;
										}
										result = node;
									});
								break;
							case "++":
							case "--":
								NodeEditorUtility.AddNewNode<Nodes.IncrementDecrementNode>(editorData,
									graphPosition,
									(node) => {
										node.isDecrement = completion.name == "--";
										if(completion.genericCompletions != null && completion.genericCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.genericCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.target = member;
											}
											node.isPrefix = false;
										} else if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.target = member;
											}
											node.isPrefix = true;
										}
										result = node;
									});
								break;
							case "=":
							case "+=":
							case "-=":
							case "/=":
							case "*=":
							case "%=":
								NodeEditorUtility.AddNewNode<NodeSetValue>(editorData,
									graphPosition,
									(node) => {
										if(completion.genericCompletions != null && completion.genericCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.genericCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.target = member;
											}
										}
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.value = member;
											}
										}
										if(completion.name == "=") {
											node.setType = SetType.Change;
										} else if(completion.name == "+=") {
											node.setType = SetType.Add;
										} else if(completion.name == "-=") {
											node.setType = SetType.Subtract;
										} else if(completion.name == "/=") {
											node.setType = SetType.Divide;
										} else if(completion.name == "*=") {
											node.setType = SetType.Multiply;
										} else if(completion.name == "%=") {
											node.setType = SetType.Modulo;
										}
										result = node;
									});
								break;
							case "||":
								NodeEditorUtility.AddNewNode<Nodes.MultiORNode>(editorData,
									graphPosition,
									(node) => {
										if(completion.genericCompletions != null && completion.genericCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.genericCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.targets[0] = member;
											}
										}
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.targets[1] = member;
											}
										}
										result = node;
									});
								break;
							case "&&":
								NodeEditorUtility.AddNewNode<Nodes.MultiANDNode>(editorData,
									graphPosition,
									(node) => {
										if(completion.genericCompletions != null && completion.genericCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.genericCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.targets[0] = member;
											}
										}
										if(completion.parameterCompletions != null && completion.parameterCompletions.Count > 0) {
											var member = CompletionEvaluator.CompletionsToMemberData(
												completion.parameterCompletions,
												editorData,
												graphPosition);
											if(member != null) {
												node.targets[1] = member;
											}
										}
										result = node;
									});
								break;
							default:
								throw new System.Exception("Unsupported symbol:" + completion.name);
						}
					}
					return result;
				};
			}
			#endregion

			Update();
			EditorReflectionUtility.GetNamespaces();
		}

		static MethodInfo GetInspectors;
		static PropertyInfo GetTracker;
		static System.Action resetInspector;

		static void OnSelectionChanged() {
			if (GetInspectors == null) {
				var inspector = "UnityEditor.InspectorWindow".ToType(false);
				if (inspector != null) {
					GetInspectors = inspector.GetMethod("GetInspectors", MemberData.flags);
					GetTracker = inspector.GetProperty("tracker", MemberData.flags);
				}
			}
			if(GetInspectors != null) {
				if(resetInspector != null) {
					resetInspector();
					resetInspector = null;
				}
				IList inspectors = GetInspectors.Invoke(null, null) as IList;
				if (inspectors != null) {
					foreach (var obj in inspectors) {
						EditorWindow inspector = obj as EditorWindow;
						ActiveEditorTracker tracker = GetTracker.GetValue(inspector) as ActiveEditorTracker;
						if(tracker != null && tracker.activeEditors.Length == 0) continue;
						if(tracker.activeEditors[0].targets.Length != 1) continue;
						GameObject go = tracker.activeEditors[0].target as GameObject;
						if (go == null && tracker.activeEditors[0].GetType().FullName == "UnityEditor.PrefabImporterEditor")
							go = tracker.activeEditors[1].target as GameObject;
						if(go == null) continue;
						var graph = go.GetComponent<uNodeComponentSystem>();
						if(graph != null && !(graph is IRuntimeGraph)) {
							var vs = inspector.rootVisualElement.Q("unity-content-container");
							if(vs != null && vs.childCount >= 2) {
								int index = 0;
								foreach(var child in vs.ElementAt(0).Children()) {
									if(index >= tracker.activeEditors.Length) break;
									var comp = tracker.activeEditors[index].target as uNodeComponentSystem;
									index++;
									if(comp != null) continue;
									child.SetDisplay(DisplayStyle.None);
								}
								var inspectorAddComponent = vs.ElementAt(1);
								inspectorAddComponent.SetDisplay(DisplayStyle.None);
								resetInspector += () => inspectorAddComponent.SetDisplay(DisplayStyle.Flex);
							}
						}
					}
				}
			}
		}

		static int refreshTime;

		static void Update() {
			#region Startup
			if(WelcomeWindow.IsShowOnStartup && EditorApplication.timeSinceStartup < 30) {
				WelcomeWindow.ShowWindow();
			}
			#endregion

			if(EditorApplication.isCompiling) {
				uNodeEditor.OnCompiling();
			}
			if(uNodePreference.GetPreference().inEditorDocumentation) {
				XmlDoc.LoadDocInBackground();
				XmlDoc.UpdateProgress();
			}
			uNodeUtility.preferredDisplay = uNodePreference.GetPreference().displayKind;
			if(System.DateTime.Now.Second > refreshTime || refreshTime > 60 && refreshTime - 60 >= System.DateTime.Now.Second) {
				UpdateMarkedObject();
				refreshTime = System.DateTime.Now.Second + 4;
			}
			uNodeUtility.isPlaying = EditorApplication.isPlayingOrWillChangePlaymode;
			uNodeThreadUtility.Update();
			//uNodeUtils.frameCount++;
		}

		#region Project & Hierarchy
		static void UpdateMarkedObject() {
			if(uNodeIcon != null) {
				markedAssets.Clear();
				foreach(var guid in assetGUIDs) {
					var path = AssetDatabase.GUIDToAssetPath(guid);
					var asset = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
					if(asset is GameObject) {
						var go = asset as GameObject;
						var comp = go.GetComponent<uNodeRoot>();
						if(comp != null) {
							markedAssets.Add(guid, comp);
						}
					} else if(asset is ICustomIcon) {
						markedAssets.Add(guid, asset);
					}
				}
				GameObject[] objects = Object.FindObjectsOfType(typeof(GameObject)) as GameObject[];
				foreach(GameObject g in objects) {
					if(g.GetComponent<uNodeRoot>() != null || g.GetComponent<uNodeData>())
						markedObjects.Add(g.GetInstanceID());
				}
			}
		}

		static uNodeRoot draggedUNODE;
		static void HierarchyItem(int instanceID, Rect selectionRect) {
			//Show uNode Icon
			if (uNodeIcon != null) {
				Rect r = new Rect(selectionRect);
				r.x += r.width - 4;
				//r.x -= 5;
				r.width = 18;

				if (markedObjects.Contains(instanceID)) {
					GUI.Label(r, uNodeIcon);
				}
			}
			HandleDragAndDropEvents();
			//Drag & Drop
			if (Event.current.type == EventType.DragPerform) {
				if (DragAndDrop.objectReferences?.Length == 1) {
					var obj = DragAndDrop.objectReferences[0];
					if (obj is GameObject && uNodeEditorUtility.IsPrefab(obj)) {
						var comp = (obj as GameObject).GetComponent<uNodeRoot>();
						if(comp is uNodeRuntime) {
							//if(EditorUtility.DisplayDialog("", "Do you want to Instantiate the Prefab or Spawn the graph?", "Prefab", "Graph")) {
							//	comp = null;
							//	PrefabUtility.InstantiatePrefab(comp);
							//	Event.current.Use();
							//}
							return;
						}
						if (comp != null && (comp is IClassComponent || comp is IGraphWithUnityEvent)) {
							draggedUNODE = comp;
							DragAndDrop.AcceptDrag();
							Event.current.Use();
							EditorApplication.delayCall += () => {
								if(draggedUNODE != null) {
									var gameObject = new GameObject(draggedUNODE.gameObject.name);
									var spawner = gameObject.AddComponent<uNodeSpawner>();
									spawner.target = draggedUNODE;
									Selection.objects = new Object[] { gameObject };
									draggedUNODE = null;
								}
							};
						}
					}
				}
			}
			if(draggedUNODE != null) {
				if (selectionRect.Contains(Event.current.mousePosition)) {
					var gameObject = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
					if (gameObject != null) {
						var spawner = gameObject.AddComponent<uNodeSpawner>();
						spawner.target = draggedUNODE;
						Selection.objects = new Object[] { gameObject };
						draggedUNODE = null;
					}
				}
			}
		}

		private static void HandleDragAndDropEvents() {
			if (Event.current.type == EventType.DragUpdated) {
				if (DragAndDrop.objectReferences?.Length == 1) {
					var obj = DragAndDrop.objectReferences[0];
					if (obj is GameObject && uNodeEditorUtility.IsPrefab(obj)) {
						var comp = (obj as GameObject).GetComponent<uNodeRoot>();
						if (comp != null && !(comp is IIndependentGraph)) {
							Event.current.type = EventType.MouseDrag;
							DragAndDrop.PrepareStartDrag();
							DragAndDrop.objectReferences = new Object[0];
							DragAndDrop.StartDrag("Drag uNode");
							Event.current.Use();
						}
					}
				}
			}
		}

		private static void OnSceneGUI(SceneView obj) {
			HandleDragAndDropEvents();
		}

		private static void ProjectItem(string guid, Rect rect) {
			HandleDragAndDropEvents();
			if(uNodeIcon == null)
				return;
			if(!markedAssets.TryGetValue(guid, out var obj)) {
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
				if(asset is GameObject) {
					var go = asset as GameObject;
					obj = go.GetComponent<uNodeRoot>();
				} else if(asset is ICustomIcon) {
					obj = asset;
				}
				markedAssets[guid] = obj;
			}
			if(obj != null) {
				var isSmall = IsIconSmall(ref rect);
				if (obj is Component) {
					DrawCustomIcon(rect, backgroundIcon, isSmall);
				}
				if(obj is ICustomIcon customIcon) {
					if(customIcon.GetIcon() != null) {
						DrawCustomIcon(rect, customIcon.GetIcon(), isSmall);
					} else if(obj is uNodeInterface) {
						DrawCustomIcon(rect, uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.InterfaceIcon)), isSmall);
					} else {
						DrawCustomIcon(rect, uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.RuntimeTypeIcon)), isSmall);
					}
				} else if(obj is uNodeRuntime) {
					DrawCustomIcon(rect, uNodeIcon, isSmall);
				} else if(obj is IClass) {
					if((obj as IClass).IsStruct) {
						DrawCustomIcon(rect, uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.StructureIcon)), isSmall);
					} else {
						DrawCustomIcon(rect, uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.ClassIcon)), isSmall);
					}
				} else {
					DrawCustomIcon(rect, uNodeIcon, isSmall);
				}
			}
		}

		private static void DrawCustomIcon(Rect rect, Texture texture, bool isSmall) {
			const float LARGE_ICON_SIZE = 128f;
			if(rect.width > LARGE_ICON_SIZE) {
				// center the icon if it is zoomed
				var offset = (rect.width - LARGE_ICON_SIZE) / 2f;
				rect = new Rect(rect.x + offset, rect.y + offset, LARGE_ICON_SIZE, LARGE_ICON_SIZE);
			} else {
				if(isSmall && !IsTreeView(rect))
					rect = new Rect(rect.x + 3, rect.y, rect.width, rect.height);
			}
			GUI.DrawTexture(rect, texture);
		}
		private static bool IsTreeView(Rect rect) {
			return (rect.x - 16) % 14 == 0;
		}

		private static bool IsIconSmall(ref Rect rect) {
			var isSmall = rect.width > rect.height;

			if(isSmall)
				rect.width = rect.height;
			else
				rect.height = rect.width;

			return isSmall;
		}
		#endregion

		private static List<int> _nodeDebugData;
		private static List<int> nodeDebugData {
			get {
				if(_nodeDebugData == null) {
					_nodeDebugData = uNodeEditorUtility.LoadEditorData<List<int>>("BreakpointsMap");
					if(_nodeDebugData == null) {
						_nodeDebugData = new List<int>();
					}
				}
				return _nodeDebugData;
			}
		}

		private static void SaveDebugData() {
			uNodeEditorUtility.SaveEditorData(_nodeDebugData, "BreakpointsMap");
		}

		#region AOT Scans
		public static bool AOTScan(out List<Type> serializedTypes) {
			return AOTScan(out serializedTypes, true, true, true, true, null);
		}

		public static bool AOTScan(out List<Type> serializedTypes, bool scanBuildScenes = true, bool scanAllAssetBundles = true, bool scanPreloadedAssets = true, bool scanResources = true, List<string> resourcesToScan = null) {
			using(AOTSupportScanner aOTSupportScanner = new AOTSupportScanner()) {
				aOTSupportScanner.BeginScan();
				if(scanBuildScenes && !aOTSupportScanner.ScanBuildScenes(includeSceneDependencies: true, showProgressBar: true)) {
					Debug.Log("Project scan canceled while scanning scenes and their dependencies.");
					serializedTypes = null;
					return false;
				}
				if(scanResources && !aOTSupportScanner.ScanAllResources(includeResourceDependencies: true, showProgressBar: true, resourcesToScan)) {
					Debug.Log("Project scan canceled while scanning resources and their dependencies.");
					serializedTypes = null;
					return false;
				}
				if(scanAllAssetBundles && !aOTSupportScanner.ScanAllAssetBundles(showProgressBar: true)) {
					Debug.Log("Project scan canceled while scanning asset bundles and their dependencies.");
					serializedTypes = null;
					return false;
				}
				if(scanPreloadedAssets && !aOTSupportScanner.ScanPreloadedAssets(showProgressBar: true)) {
					Debug.Log("Project scan canceled while scanning preloaded assets and their dependencies.");
					serializedTypes = null;
					return false;
				}
				aOTSupportScanner.GetType().GetField("allowRegisteringScannedTypes", MemberData.flags).SetValueOptimized(aOTSupportScanner, true);
				ScanAOTOnGraphs();
				OnPreprocessBuild();
				serializedTypes = aOTSupportScanner.EndScan();
				for(int i=0;i< serializedTypes.Count;i++) {
					if(EditorReflectionUtility.IsInEditorAssembly(serializedTypes[i])) {
						serializedTypes.RemoveAt(i);
						i--;
					}
				}
			}
			return true;
		}

		private static void ScanAOTOnGraphs() {
			List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
			objects.AddRange(uNodeEditorUtility.FindPrefabsOfType<uNodeComponentSystem>());
			objects.AddRange(uNodeEditorUtility.FindAssetsByType<uNodeInterface>());
			HashSet<Type> serializedTypes = new HashSet<Type>();
			Action<object> analyzer = (param) => {
				AnalizerUtility.AnalizeObject(param, (fieldObj) => {
					if(fieldObj is MemberData member && member.isTargeted) {
						object mVal;
						if(member.targetType.IsTargetingValue()) {
							mVal = member.Get();
						} else {
							mVal = member.instance;
						}
						if(mVal != null && !(mVal is Object) && !(mVal is MemberData) && !serializedTypes.Contains(mVal.GetType())) {
							serializedTypes.Add(mVal.GetType());
							SerializerUtility.Serialize(mVal);
						}
					}
					return false;
				});
			};
			foreach(var obj in objects) {
				if(obj is GameObject) {
					var scripts = (obj as GameObject).GetComponentsInChildren<MonoBehaviour>(true);
					foreach(var script in scripts) {
						if(script is ISerializationCallbackReceiver serialization) {
							serialization.OnBeforeSerialize();
						}
						if(script is IVariableSystem VS && VS.Variables != null) {
							foreach(var var in VS.Variables) {
								var.Serialize();
							}
						}
						if(script is ILocalVariableSystem IVS && IVS.LocalVariables != null) {
							foreach(var var in IVS.LocalVariables) {
								var.Serialize();
							}
						}
						analyzer(script);
					}
				} else if(obj is ISerializationCallbackReceiver) {
					(obj as ISerializationCallbackReceiver).OnBeforeSerialize();
				} else {
					analyzer(obj);
				}
			}
			SerializerUtility.Serialize(new MemberData());
		}
		#endregion

		#region Build Processor
		private static bool isEditorOpen;
		private static bool hasRunPreBuild;

		public static void OnPreprocessBuild() {
			if(hasRunPreBuild)
				return;
			hasRunPreBuild = true;
			if(uNodePreference.preferenceData.generatorData.autoGenerateOnBuild) {
				GenerationUtility.GenerateCSharpScript();
				while(uNodeThreadUtility.IsNeedUpdate()) {
					uNodeThreadUtility.Update();
				}
				if(uNodeEditor.window != null) {
					uNodeEditor.window.Close();
					isEditorOpen = true;
				}
				GraphUtility.SaveAllGraph();
			}
		}

		public static void OnPostprocessBuild() {
			if(isEditorOpen) {
				uNodeThreadUtility.ExecuteAfter(5, () => {
					uNodeEditor.ShowWindow();
				});
				isEditorOpen = false;
			}
			hasRunPreBuild = false;
		}
		#endregion
	}

	internal static class uNodeEditorMenu {
		internal class MyCustomBuildProcessor : UnityEditor.Build.IPreprocessBuildWithReport, UnityEditor.Build.IPostprocessBuildWithReport {
			public int callbackOrder => 0;

			public void OnPreprocessBuild(BuildReport report) {
				uNodeEditorInitializer.OnPreprocessBuild();
			}

			public void OnPostprocessBuild(BuildReport report) {
				uNodeEditorInitializer.OnPostprocessBuild();
			}
		}

#if UNODE_DEBUG
		[MenuItem("Tools/uNode/Advanced/Scan AOT Type", false, 1000010)]
		public static void ScanAOTType() {
			uNodeEditorInitializer.AOTScan(out var types);
			Debug.Log(types.Count);
			foreach(var t in types) {
				Debug.Log(t);
			}
		}
#endif

		private static void MigrateSerialization(object data) {
			if(data == null)
				return;
			if(data is MemberData member) {
				if(!member.isStatic) {
					object value = member.instance;
					if(!(value is Object)) {
						MigrateSerialization(value);
					}
					member.instance = value;
				}
				if(member.targetType == MemberData.TargetType.Values) {
					member = new MemberData(member.Get());
				}
				return;
			}
			AnalizerUtility.AnalizeObject(data, (obj) => {
				if(obj is MemberData) {
					MigrateSerialization(obj);
					return true;
				} else if(obj is ISerializationCallbackReceiver serializationCallback) {
					if(serializationCallback is EventActionData EAD) {
						MigrateSerialization(EAD.block);
					}
					serializationCallback.OnBeforeSerialize();
					serializationCallback.OnAfterDeserialize();
					return true;
				}
				return false;
			}, (instance, field, type, value) => {
				field.SetValueOptimized(instance, value);
			});
		}

		[MenuItem("Tools/uNode/Update Graph Database", false, 2)]
		public static void UpdateDatabase() {
			var db = uNodeUtility.GetDatabase();
			if(db == null && EditorUtility.DisplayDialog("No graph database", "There's no graph database found in the project, do you want to create new?", "Ok", "Cancel")) {
				while(true) {
					var path = EditorUtility.SaveFolderPanel("Select resources folder to save database to", "Assets", "").Replace('/', Path.DirectorySeparatorChar);
					if(!string.IsNullOrEmpty(path)) {
						if(path.StartsWith(Directory.GetCurrentDirectory()) && path.ToLower().EndsWith("resources")) {
							db = ScriptableObject.CreateInstance<uNodeResourceDatabase>();
							path = path.Remove(0, Directory.GetCurrentDirectory().Length + 1) + Path.DirectorySeparatorChar + "uNodeDatabase.asset";
							AssetDatabase.CreateAsset(db, path);
						} else {
							uNodeEditorUtility.DisplayErrorMessage("Please select 'Resources' folder in project");
							continue;
						}
					}
					break;
				}
			}
			if(db != null) {
				var graphs = uNodeEditorUtility.FindComponentInPrefabs<uNodeRoot>();
				foreach(var root in graphs) {
					if(db.graphDatabases.Any(g => g.graph == root)) {
						continue;
					}
					db.graphDatabases.Add(new uNodeResourceDatabase.RuntimeGraphDatabase() {
						graph = root,
					});
					EditorUtility.SetDirty(db);
				}
			}
		}

		[MenuItem("Tools/uNode/Update/Migrate Project Serialization", false, 100001)]
		public static void MigrateSerializationData() {
			var prefabs = uNodeEditorUtility.FindPrefabsOfType<uNodeRoot>();
			foreach(var prefab in prefabs) {
				if(GraphUtility.HasTempGraphObject(prefab)) {
					var graph = GraphUtility.GetTempGraphObject(prefab);
					var scripts = graph.GetComponentsInChildren<MonoBehaviour>(true);
					foreach(var behavior in scripts) {
						MigrateSerialization(behavior);
						uNodeEditorUtility.MarkDirty(behavior);
					}
					GraphUtility.SaveGraph(graph);
				} else {
					var graph = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(prefab));
					var scripts = graph.GetComponentsInChildren<MonoBehaviour>(true);
					foreach(var behavior in scripts) {
						MigrateSerialization(behavior);
						uNodeEditorUtility.MarkDirty(behavior);
					}
					PrefabUtility.SaveAsPrefabAsset(graph, AssetDatabase.GetAssetPath(prefab));
					PrefabUtility.UnloadPrefabContents(graph);
				}
				uNodeEditorUtility.MarkDirty(prefab);
			}
			var assets = uNodeEditorUtility.FindAssetsByType<ScriptableObject>();
			foreach(var asset in assets) {
				MigrateSerialization(asset);
				uNodeEditorUtility.MarkDirty(asset);
			}
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

#if UNODE_COMPILE_ON_PLAY
		[MenuItem("Tools/uNode/Advanced/Compile On Play: Enabled", false, 10001)]
#else
		[MenuItem("Tools/uNode/Advanced/Compile On Play: Disabled", false, 10001)]
#endif
		private static void AdvancedCompileOnPlay() {
#if UNODE_COMPILE_ON_PLAY
			uNodeEditorUtility.RemoveDefineSymbols(new string[] { "UNODE_COMPILE_ON_PLAY" });
#else
			if(EditorBinding.csharpParserType != null) {
				uNodeEditorUtility.AddDefineSymbols(new string[] { "UNODE_COMPILE_ON_PLAY" });
			} else {
#if NET_STANDARD_2_0
				EditorUtility.DisplayDialog("Cannot enable Compile On Play", "Cannot enable compile graphs on play because unsupported compiling scripts in .NET Standard 2.0, change API compativility level to .NET 4.x to enable it or import CSharp Parser add-ons.", "Ok");
#else
				uNodeEditorUtility.AddDefineSymbols(new string[] { "UNODE_COMPILE_ON_PLAY" });
#endif
			}
#endif
		}

		[MenuItem("Assets/Create Asset Instance", false, 19)]
		public static void CreateAssetInstance() {
			if(Selection.activeObject is GameObject gameObject && gameObject.GetComponent<uNodeClassAsset>() != null) {
				var graph = gameObject.GetComponent<uNodeClassAsset>();
				var classAsset = ScriptableObject.CreateInstance<uNodeAssetInstance>();
				classAsset.target = graph;
				ProjectWindowUtil.CreateAsset(classAsset, $"New_{graph.DisplayName}.asset");
			} else {
				var items = ItemSelector.MakeCustomItemsForInstancedType(new System.Type[] { typeof(uNodeClassAsset) }, (val) => {
					var graph = val as uNodeClassAsset;
					var classAsset = ScriptableObject.CreateInstance<uNodeAssetInstance>();
					classAsset.target = graph;
					ProjectWindowUtil.CreateAsset(classAsset, $"New_{graph.DisplayName}.asset");
				}, false);
				var pos = EditorWindow.mouseOverWindow?.position ?? EditorWindow.focusedWindow?.position ?? Rect.zero;
				ItemSelector.ShowCustomItem(items).ChangePosition(pos);
			}
		}

		static List<string> GetObjDependencies(Object obj, HashSet<Object> scannedObjs) {
			List<string> result = new List<string>();
			if(!scannedObjs.Add(obj)) {
				return result;
			}
			result.AddRange(AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(obj), true));
			if(obj is MonoScript) {
				var monoScript = obj as MonoScript;
				var path = AssetDatabase.GetAssetPath(monoScript);
				if(path.EndsWith(".cs")) {
					var graphPath = path.RemoveLast(3).Add(".prefab");
					if(File.Exists(graphPath)) {
						var graphObj = AssetDatabase.LoadAssetAtPath<GameObject>(graphPath);
						if(graphObj != null && graphObj.GetComponent<uNodeComponentSystem>() != null) {
							result.AddRange(GetObjDependencies(graphObj, scannedObjs));
						}
					}
				}
				return result;
			}
			Func<object, bool> func = (val) => {
				if(val is MemberData) {
					var member = val as MemberData;
					var references = member.GetUnityReferences();
					foreach(var r in references) {
						var mainAsset = r;
						if(r is Component comp) {
							mainAsset = comp.gameObject;
						}
 						if(AssetDatabase.IsMainAsset(mainAsset) && scannedObjs.Add(mainAsset)) {
							result.Add(AssetDatabase.GetAssetPath(mainAsset));
							result.AddRange(GetObjDependencies(mainAsset, scannedObjs));
						}
					}
					if(member.isAssigned) {
						var type = member.startType;
						if(type != null && !type.IsRuntimeType()) {
							var monoScript = uNodeEditorUtility.GetMonoScript(type);
							if(monoScript != null) {
								result.AddRange(GetObjDependencies(monoScript, scannedObjs));
							} else {
								var loc = type.Assembly.Location;
								if(!string.IsNullOrEmpty(type.Assembly.Location)) {
									var fileName = Path.GetFileName(loc);
									if(scriptsMaps.TryGetValue(fileName, out var monoScripts)) {
										foreach(var script in monoScripts) {
											result.AddRange(GetObjDependencies(script, scannedObjs));
										}
									}
								}
							}
						}
					}
				} else if(val is Object) {
					var mainAsset = val as Object;
					if(mainAsset is Component comp) {
						mainAsset = comp.gameObject;
					}
					if(AssetDatabase.IsMainAsset(mainAsset) && scannedObjs.Add(mainAsset)) {
						result.Add(AssetDatabase.GetAssetPath(mainAsset));
						result.AddRange(GetObjDependencies(mainAsset, scannedObjs));
					}
				}
				return false;
			};
			if(obj is GameObject go) {
				var comps = go.GetComponentsInChildren<MonoBehaviour>(true);
				foreach(var c in comps) {
					if(scannedObjs.Add(c)) {
						AnalizerUtility.AnalizeObject(c, func);
						if(c is Nodes.LinkedMacroNode macro && macro.macroAsset != null) {
							result.Add(AssetDatabase.GetAssetPath(macro.macroAsset));
							result.AddRange(GetObjDependencies(macro.macroAsset.gameObject, scannedObjs));
						}
						if(c is IVariableSystem) {
							var varSystem = c as IVariableSystem;
							AnalizerUtility.AnalizeObject(varSystem.Variables, func);
						}
					}
				}
			} else {
				AnalizerUtility.AnalizeObject(obj, func);
			}
			return result;
		}

		static Dictionary<string, HashSet<MonoScript>> scriptsMaps;

		static void UpdateScriptMap() {
			scriptsMaps = new Dictionary<string, HashSet<MonoScript>>();
			var unodePath = uNodeEditorUtility.GetUNodePath();
			string[] assetPaths = AssetDatabase.GetAllAssetPaths();
			foreach(string assetPath in assetPaths) {
				if(assetPath.EndsWith(".cs") && !assetPath.StartsWith(unodePath + "/")) {
					var monoScript = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
					var type = monoScript.GetType();
					var assName = type.GetMethod("GetAssemblyName", MemberData.flags).InvokeOptimized(monoScript) as string;
					if(!scriptsMaps.TryGetValue(assName, out var monoScripts)) {
						monoScripts = new HashSet<MonoScript>();
						scriptsMaps[assName] = monoScripts;
					}
					monoScripts.Add(monoScript);
				}
			}
		}

		[MenuItem("Assets/Export uNode Graphs", false, 30)]
		public static void ExportSelectedGraphs() {
			EditorUtility.DisplayProgressBar("Finding Graphs Dependencies", "", 0);
			UpdateScriptMap();
			var guids = Selection.assetGUIDs;
			List<string> exportPaths = new List<string>();
			var hash = new HashSet<Object>();
			foreach(var guid in guids) {
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var obj = AssetDatabase.LoadAssetAtPath<Object>(path);
				if(AssetDatabase.IsValidFolder(path)) {//Skip if folder or unknow type.
					var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.StartsWith(path + "/"));
					foreach(var subPath in paths) {
						var subAsset = AssetDatabase.LoadAssetAtPath<Object>(subPath);
						exportPaths.Add(subPath);
						exportPaths.AddRange(GetObjDependencies(subAsset, hash));
					}
					continue;
				}
				exportPaths.Add(path);
				exportPaths.AddRange(GetObjDependencies(obj, hash));
			}
			var unodePath = uNodeEditorUtility.GetUNodePath();
			var projectDir = Directory.GetCurrentDirectory();
			for(int i=0;i< exportPaths.Count;i++) {
				var path = exportPaths[i];
				if(path.StartsWith(unodePath + "/") || path == unodePath || !path.StartsWith("Assets") && !path.StartsWith("ProjectSettings")) {
					exportPaths.RemoveAt(i);
					i--;
					continue;
				}
			}
			EditorUtility.ClearProgressBar();
			ExportGraphWindow.Show(exportPaths.Distinct().OrderBy(p => p).ToArray());
		}

		class ExportGraphWindow : EditorWindow {
			[Serializable]
			class ExportData {
				public string path;
				public bool enable;
			}
			ExportData[] exportPaths;
			Vector2 scroll;

			static ExportGraphWindow window;

			public static ExportGraphWindow Show(string[] exportedPath) {
				window = GetWindow<ExportGraphWindow>(true);
				window.exportPaths = exportedPath.Select(p => new ExportData() { path = p, enable = true }).ToArray();
				window.minSize = new Vector2(300, 250);
				window.titleContent = new GUIContent("Export Graphs");
				window.Show();
				return window;
			}

			private void OnGUI() {
				if(exportPaths.Length == 0) {
					EditorGUILayout.HelpBox("Nothing to export", MessageType.Info);
					return;
				}
				GUILayout.BeginVertical();
				scroll = EditorGUILayout.BeginScrollView(scroll);
				for(int i=0;i< exportPaths.Length;i++) {
					var data = exportPaths[i];
					var obj = AssetDatabase.LoadAssetAtPath<Object>(data.path);
					if(obj == null) continue;
					using(new GUILayout.HorizontalScope()) {
						data.enable = EditorGUILayout.Toggle(data.enable, GUILayout.Width(EditorGUIUtility.singleLineHeight));
						Texture icon = uNodeEditorUtility.GetTypeIcon(obj);
						if(obj is GameObject go) {
							var customIcon = go.GetComponent<ICustomIcon>();
							if(customIcon != null) {
								icon = uNodeEditorUtility.GetTypeIcon(customIcon);
							} else {
								var unode = go.GetComponent<uNodeComponentSystem>();
								if(unode != null) {
									icon = uNodeEditorUtility.GetTypeIcon(unode);
								}
							}
						}
						EditorGUILayout.LabelField(new GUIContent(icon), GUILayout.Width(EditorGUIUtility.singleLineHeight));
						EditorGUILayout.LabelField(new GUIContent(data.path));
					}
				}
				EditorGUILayout.EndScrollView();
				GUILayout.FlexibleSpace();
				if(GUILayout.Button("Export")) {
					var savePath = EditorUtility.SaveFilePanel("Export Graphs", "", "", "unitypackage");
					if(!string.IsNullOrEmpty(savePath)) {
						AssetDatabase.ExportPackage(exportPaths.Where(p => p.enable).Select(p => p.path).ToArray(), savePath);
						Close();
					}
				}
				GUILayout.EndVertical();
			}
		}

		// [MenuItem("Assets/Create Instance", false, 19)]
		// public static void CreateUNodeAssetInstance()
		// {
		// 	var graph = (Selection.activeObject as GameObject).GetComponent<uNodeClassAsset>();
		// 	var classAsset = ScriptableObject.CreateInstance<uNodeAssetInstance>();
		// 	classAsset.target = graph;
		// 	ProjectWindowUtil.CreateAsset(classAsset, $"New_{graph.DisplayName}.asset");
		// }

		// [MenuItem("Assets/Create Instance", true, 19)]
		// public static bool CanCreateUNodeAssetInstance()
		// {
		// 	var gameObject = Selection.activeObject as GameObject;
		// 	if(gameObject != null) {
		// 		var asset = gameObject.GetComponent<uNodeClassAsset>();
		// 		if(asset != null) {
		// 			return true;
		// 		}
		// 	}
		// 	return false;
		// }

		[MenuItem("Assets/Create/uNode/Class Component", false, -10000)]
		private static void CreateClassComponent() {
			GraphCreatorWindow.ShowWindow(typeof(uNodeClassComponent));
			//CreatePrefabWithComponent<uNodeClassComponent>("ClassComponent");
		}

		[MenuItem("Assets/Create/uNode/Class Asset", false, -10001)]
		private static void CreateClassAsset() {
			GraphCreatorWindow.ShowWindow(typeof(uNodeClassAsset));
			//CreatePrefabWithComponent<uNodeClassAsset>("ClassAsset");
		}

		[MenuItem("Assets/Create/uNode/C# Class", false, -900)]
		private static void CreateUNodeClass() {
			GraphCreatorWindow.ShowWindow(typeof(uNodeClass));
			//CreatePrefabWithComponent<uNodeClass>("Class");
		}

		[MenuItem("Assets/Create/uNode/C# Struct", false, -900)]
		private static void CreateUNodeStruct() {
			GraphCreatorWindow.ShowWindow(typeof(uNodeStruct));
			//CreatePrefabWithComponent<uNodeStruct>("Struct");
		}

		[MenuItem("Assets/Create/uNode/Macro", false, -80)]
		private static void CreateUNodeMacro() {
			//GraphCreatorWindow.ShowWindow(typeof(uNodeMacro));
			CreatePrefabWithComponent<uNodeMacro>("Macro");
		}

		//[MenuItem("Assets/Create/uNode/Runtime", false, -70)]
		//private static void CreateUNodeRuntime() {
		//	GraphCreatorWindow.ShowWindow(typeof(uNodeRuntime));
		//	CreatePrefabWithComponent<uNodeRuntime>("uNodeRuntime");
		//}

		[MenuItem("Assets/Create/uNode/Component Singleton", false, 101)]
		private static void CreateComponentSingleton() {
			GraphCreatorWindow.ShowWindow(typeof(uNodeComponentSingleton));
			//CreatePrefabWithComponent<uNodeComponentSingleton>("ComponentSingletonGraph");
		}

		[MenuItem("Assets/Create/uNode/Graph Interface", false, 102)]
		private static void CreateGraphInterface() {
			var classAsset = ScriptableObject.CreateInstance<uNodeInterface>();
			ProjectWindowUtil.CreateAsset(classAsset, $"New_Interface.asset");
		}

		// [MenuItem("Assets/Create/uNode/Asset Singleton", false, 100)]
		// private static void CreateAssetSingleton() {
		// 	CreatePrefabWithComponent<uNodeAssetSingleton>("AssetSingletonGraph");
		// }
		
		// [MenuItem("Assets/Create/uNode/Asset/GlobalVariable")]
		// private static void CreateMap() {
		// 	CustomAssetUtility.CreateAsset<GlobalVariable>();
		// }

		[MenuItem("Assets/Create/uNode/Editor/Graph Theme")]
		private static void CreateTheme() {
			CustomAssetUtility.CreateAsset<VerticalEditorTheme>((theme) => {
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				EditorUtility.FocusProjectWindow();
			});
		}

		private static void CreatePrefabWithComponent<T>(string name) where T : Component {
			GameObject go = new GameObject(name);
			go.AddComponent<T>();
			string path = CustomAssetUtility.GetCurrentPath() + "/New_" + go.name + ".prefab";
			int index = 0;
			while(File.Exists(path)) {
				index++;
				path = CustomAssetUtility.GetCurrentPath() + "/New_" + go.name + index + ".prefab";
			}
#if UNITY_2018_3_OR_NEWER
			GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
#else
			GameObject prefab = PrefabUtility.CreatePrefab(path, go);
#endif
			Object.DestroyImmediate(go);
			AssetDatabase.SaveAssets();
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = prefab;
		}
	}

	static class uNodeAssetHandler {
		[OnOpenAsset]
		public static bool OpenEditor(int instanceID, int line) {
			Object obj = EditorUtility.InstanceIDToObject(instanceID);
			if(obj is GameObject) {
				GameObject go = obj as GameObject;
				uNodeRoot root = go.GetComponent<uNodeRoot>();
				if(root != null) {
					uNodeEditor.ChangeTarget(root, true);
					return true; //comment this to allow editing prefab.
				} else {
					uNodeData data = go.GetComponent<uNodeData>();
					if(data != null) {
						uNodeEditor.ChangeTarget(data, true);
						return true; //comment this to allow editing prefab.
					}
				}
			}
			return false;
		}
	}

	class uNodeAssetModificationPreprocessor : UnityEditor.AssetModificationProcessor {
		static HashSet<string> removePaths = new HashSet<string>();
		static bool containsUNODE = false;
		private static AssetDeleteResult OnWillDeleteAsset(string path, RemoveAssetOptions options) {
			if (path.EndsWith(".prefab")) {
				var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
				if (obj != null && obj is GameObject) {
					if (GraphUtility.HasTempGraphObject(obj as GameObject)) {
						GraphUtility.DestroyTempGraphObject(obj as GameObject);
					}
				}
			} else if(path.StartsWith(uNodeEditorUtility.GetUNodePath() + "/") || path == uNodeEditorUtility.GetUNodePath()) {
				if(GraphUtility.GetTempManager() != null) {
					containsUNODE = true;
				}
			}
			removePaths.Add(path);
			uNodeThreadUtility.ExecuteOnce(() => {
				if(containsUNODE) {
					containsUNODE = false;
					//Close the uNode Editor window
					uNodeEditor.window?.Close();
					//Save all graphs and remove all root graphs.
					GraphUtility.SaveAllGraph();
					//Save all dirty assets
					AssetDatabase.SaveAssets();
				}
				uNodeThreadUtility.Queue(() => {
					EditorUtility.DisplayProgressBar("Deleting Files", "", 0);
					foreach(var p in removePaths) {
						if(Directory.Exists(p)) {
							ForceDeleteDirectory(p);
						} else if(File.Exists(p)) {
							if(AssetDatabase.IsMainAssetAtPathLoaded(p)) {
								var asset = AssetDatabase.LoadMainAssetAtPath(p);
								if(!(asset is GameObject || asset is Component)) {
									Resources.UnloadAsset(asset);
								}
							}
							new FileStream(p, FileMode.Open).Dispose();
							File.Delete(p);
						} else {
							continue;
						}
						var metaPath = p + ".meta";
						if(File.Exists(metaPath)) {
							File.Delete(metaPath);
						}
					}
					EditorUtility.ClearProgressBar();
					removePaths.Clear();
					uNodeThreadUtility.Queue(() => {
						AssetDatabase.Refresh();
					});
				});
			}, "[UNODE_DELETE]");
			return AssetDeleteResult.DidDelete;
		}

		static void ForceDeleteDirectory(string path) {
			var directory = new DirectoryInfo(path) { Attributes = FileAttributes.Normal };
			foreach(var info in directory.GetFileSystemInfos("*", SearchOption.AllDirectories)) {
				info.Attributes = FileAttributes.Normal;
				if(info is FileInfo fi) {
					var p = fi.FullName.Remove(0, Application.dataPath.Length + 1).Replace('\\', '/');
					if(!p.EndsWith(".meta") && AssetDatabase.IsMainAssetAtPathLoaded(p)) {
						Resources.UnloadAsset(AssetDatabase.LoadMainAssetAtPath(p));
					}
					fi.Create().Dispose();
					fi.Delete();
				}
			}
			directory.Delete(true);
		}

		private static AssetMoveResult OnWillMoveAsset(string sourcePath, string destinationPath) {
			if (sourcePath.EndsWith(".prefab")) {
				var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(sourcePath);
				if (obj != null) {
					if(obj is GameObject go && go.GetComponent<uNodeComponentSystem>() != null) {
						UGraphView.ClearCache();
						uNodeEditor.window?.Refresh();
						uNodeGUIUtility.GUIChanged(go);
					} else if(obj is uNodeInterface) {
						UGraphView.ClearCache();
						uNodeEditor.window?.Refresh();
						uNodeGUIUtility.GUIChanged(obj);
					}
				}
			}
			return AssetMoveResult.DidNotMove;
		}
	}
}