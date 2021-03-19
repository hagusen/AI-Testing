using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using Object = UnityEngine.Object;

namespace MaxyGames.uNode.Editors {
	public static class NodeEditorUtility {
		#region Classes
		public class FieldNodeData {
			public FieldInfo field;
			public Attribute attribute;
			public object[] attributes;
		}
		#endregion

		#region GetFieldNodes
		private static Dictionary<Type, FieldNodeData[]> fieldNodesMap = new Dictionary<Type, FieldNodeData[]>();

		public static FieldNodeData[] GetFieldNodes(Type type) {
			FieldNodeData[] datas;
			if(fieldNodesMap.TryGetValue(type, out datas)) {
				return datas;
			}
			List<FieldNodeData> fieldNodes = new List<FieldNodeData>();
			FieldInfo[] fields = EditorReflectionUtility.GetFields(type);
			for(int i = 0; i < fields.Length; i++) {
				FieldInfo field = fields[i];
				if(field.IsDefinedAttribute(typeof(FieldConnectionAttribute))) {
					var attributes = EditorReflectionUtility.GetAttributes(field);
					var FCA = ReflectionUtils.GetAttribute<FieldConnectionAttribute>(attributes);
					fieldNodes.Add(new FieldNodeData() { field = field, attribute = FCA, attributes = attributes });
				} else if(field.IsDefinedAttribute(typeof(OutputAttribute))) {
					var attributes = EditorReflectionUtility.GetAttributes(field);
					var FVA = ReflectionUtils.GetAttribute<OutputAttribute>(attributes);
					if(FVA.type == null) {
						FVA.type = field.FieldType;
					}
					fieldNodes.Add(new FieldNodeData() { field = field, attribute = FVA, attributes = attributes });
				} else if(field.IsDefinedAttribute(typeof(FieldDrawerAttribute))) {
					var attributes = EditorReflectionUtility.GetAttributes(field);
					var FD = ReflectionUtils.GetAttribute<FieldDrawerAttribute>(attributes);
					if(FD.label == null) {
						FD.label = new GUIContent(field.Name);
					}
					fieldNodes.Add(new FieldNodeData() { field = field, attribute = FD, attributes = attributes });
				} else if(field.FieldType == typeof(FlowInput)) {
					fieldNodes.Add(new FieldNodeData() { field = field });
				}
			}
			datas = fieldNodes.ToArray();
			fieldNodesMap[type] = datas;
			return datas;
		}
		#endregion

		#region Reflect
		/// <summary>
		/// Perform reflect a component.
		/// </summary>
		/// <param name="components"></param>
		/// <param name="fromUNode"></param>
		/// <param name="targetUNode"></param>
		public static void PerformReflectComponent(List<uNodeComponent> components, uNodeRoot fromUNode, uNodeRoot targetUNode) {
			bool isYesToAll = false;
			List<VariableData> notHaveVariable = new List<VariableData>();
			float progress = 0;
			if(components.Count > 9) {
				EditorUtility.DisplayProgressBar("Loading", "", progress);
			}
			for(int i = 0; i < components.Count; i++) {
				uNodeComponent component = components[i];
				if(component == null)
					continue;
				if(isYesToAll) {
					ReflectComponent(component, fromUNode, targetUNode, notHaveVariable);
				} else {
					int option = EditorUtility.DisplayDialogComplex(
						"Has invalid reference",
						component.gameObject.name + " has some invalid reference.\nDo you want to correct it?",
						"Yes to All",
						"Yes",
						"No");
					if(option == 0) {
						isYesToAll = true;
						ReflectComponent(component, fromUNode, targetUNode, notHaveVariable);
					} else if(option == 1) {
						ReflectComponent(component, fromUNode, targetUNode, notHaveVariable);
					}
				}
				progress = (float)i / (float)components.Count;
				if(components.Count > 9) {
					EditorUtility.DisplayProgressBar("Loading", "", progress);
				}
			}
			if(components.Count > 9) {
				EditorUtility.ClearProgressBar();
			}
			if(notHaveVariable.Count > 0) {
				isYesToAll = false;
				List<VariableData> newVar = new List<VariableData>();
				for(int i = 0; i < notHaveVariable.Count; i++) {
					VariableData var = notHaveVariable[i];
					if(isYesToAll) {
						newVar.Add(new VariableData(var));
						continue;
					}
					int option = EditorUtility.DisplayDialogComplex(
						"Not have variable",
						var.Name + " variable has been targeted with some node and this uNode not have that variable.\nDo you want to correct it?",
						"Yes to All",
						"Yes",
						"No");
					if(option == 0) {
						isYesToAll = true;
						newVar.Add(new VariableData(var));
					} else if(option == 1) {
						newVar.Add(new VariableData(var));
					}
				}
				if(newVar.Count > 0) {
					targetUNode.Variables.AddRange(newVar);
				}
			}
		}

		private static bool IsNeedReflectComponent(IEnumerable<Object> references, uNodeRoot fromUNode, uNodeRoot targetUNode) {
			if(references == null) return false;
			foreach(UnityEngine.Object o in references) {
				if(!o)
					continue;
				if(o == fromUNode) {
					return true;
				} else if(o is GameObject) {
					if(o == fromUNode.gameObject) {
						return true;
					}
				} else if(o is Transform) {
					if(o == fromUNode.transform) {
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Used to validate that component need to be reflect.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="fromUNode"></param>
		/// <returns></returns>
		public static bool NeedReflectComponent(object obj, uNodeRoot fromUNode, uNodeRoot targetUNode) {
			if(obj == null || fromUNode == null)
				return false;
			if(obj is MemberData) {
				MemberData member = obj as MemberData;
				if(member.HasUnityReference(new Object[] { fromUNode, fromUNode.transform, fromUNode.gameObject })) {
					return true;
				}
				return false;
			}
			if(obj is EventData) {
				EventData member = obj as EventData;
				if(member != null && member.blocks.Count > 0) {
					foreach(EventActionData action in member.blocks) {
						if(NeedReflectComponent(action, fromUNode, targetUNode)) {
							return true;
						}
					}
				}
				return false;
			}
			FieldInfo[] fieldInfo = ReflectionUtils.GetFields(obj);
			foreach(FieldInfo field in fieldInfo) {
				Type fieldType = field.FieldType;
				if(!fieldType.IsClass)
					continue;
				object value = field.GetValueOptimized(obj);
				if(object.ReferenceEquals(value, null))
					continue;
				if(fieldType == typeof(UnityEngine.Object) || fieldType.IsSubclassOf(typeof(UnityEngine.Object)) || value is UnityEngine.Object) {
					if(value is uNodeRoot) {
						uNodeRoot member = value as uNodeRoot;
						if(member != null && member == fromUNode) {
							return true;
						}
					} else if(value is Transform) {
						Transform member = value as Transform;
						if(member != null && member == fromUNode.transform) {
							return true;
						}
					} else if(value is GameObject) {
						GameObject member = value as GameObject;
						if(member != null && member == fromUNode.gameObject) {
							return true;
						}
					} else if(value is Component) {
						Component member = value as Component;
						if(member != null) {
							Component comp = targetUNode.GetComponent(member.GetType());
							if(comp != null && fromUNode.GetComponent(member.GetType()) == member) {
								return true;
							}
						}
					}
					continue;
				}
				if(fieldType == typeof(MemberData)) {
					MemberData member = value as MemberData;
					if(member.HasUnityReference(new Object[] { fromUNode, fromUNode.transform, fromUNode.gameObject })) {
						return true;
					}
					continue;
				}
				if(fieldType == typeof(EventData)) {
					EventData member = value as EventData;
					if(member != null && member.blocks.Count > 0) {
						foreach(EventActionData action in member.blocks) {
							if(NeedReflectComponent(action, fromUNode, targetUNode)) {
								return true;
							}
						}
					}
					continue;
				}
				if(!fieldType.IsArray && !fieldType.IsGenericType) {
					if(NeedReflectComponent(value, fromUNode, targetUNode)) {
						return true;
					}
					continue;
				}
				if(fieldType.IsArray && fieldType.GetElementType().IsClass
					|| fieldType.IsGenericType && (fieldType.GetGenericArguments()[0].IsClass)) {
					IList list = value as IList;
					if(list == null)
						continue;
					for(int i = 0; i < list.Count; i++) {
						object element = list[i];
						if(object.ReferenceEquals(element, null))
							continue;
						Type elementType = element.GetType();
						if(elementType.IsAbstract || !elementType.IsClass)
							break;
						if(elementType == typeof(UnityEngine.Object) || elementType.IsSubclassOf(typeof(UnityEngine.Object)) || element is UnityEngine.Object) {
							if(element is uNodeRoot) {
								uNodeRoot member = element as uNodeRoot;
								if(member != null && member == fromUNode) {
									return true;
								}
							} else if(element is Transform) {
								Transform member = element as Transform;
								if(member != null && member == fromUNode.transform) {
									return true;
								}
							} else if(element is GameObject) {
								GameObject member = element as GameObject;
								if(member != null && member == fromUNode.gameObject) {
									return true;
								}
							} else if(element is Component) {
								Component member = element as Component;
								if(member != null) {
									Component comp = targetUNode.GetComponent(member.GetType());
									if(comp != null && fromUNode.GetComponent(member.GetType()) == member) {
										member = comp;
										list[i] = member;
									}
								}
							}
							continue;
						}
						if(elementType == typeof(MemberData)) {
							MemberData member = element as MemberData;
							if(member.HasUnityReference(new Object[] { fromUNode, fromUNode.transform, fromUNode.gameObject })) {
								return true;
							}
							continue;
						}
						if(elementType == typeof(EventData)) {
							EventData member = element as EventData;
							if(member != null && member.blocks.Count > 0) {
								foreach(EventActionData action in member.blocks) {
									if(NeedReflectComponent(action, fromUNode, targetUNode)) {
										return true;
									}
								}
							}
							continue;
						}
						if(!elementType.IsArray && !elementType.IsGenericType) {
							if(NeedReflectComponent(element, fromUNode, targetUNode)) {
								return true;
							}
							continue;
						}
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Used to copy Event Component to another Event.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="fromUNode"></param>
		/// <param name="targetUNode"></param>
		public static void ReflectComponent(object obj, uNodeRoot fromUNode, uNodeRoot targetUNode, List<VariableData> notHaveVariable = null) {
			if(object.ReferenceEquals(obj, null) || fromUNode == null || targetUNode == null)
				return;
			if(obj is MemberData) {
				MemberData member = obj as MemberData;
				member.RefactorUnityObject(
					new Object[] { fromUNode, fromUNode.gameObject, fromUNode.transform },
					new Object[] { targetUNode, targetUNode.gameObject, targetUNode.transform }
				);
				if(member.IsTargetingVariable) {
					if(member.startTarget == fromUNode as object) {
						if(notHaveVariable != null && !uNodeUtility.HasVariable(member.startName, targetUNode.Variables)) {
							for(int i = 0; i < fromUNode.Variables.Count; i++) {
								if(fromUNode.Variables[i].Name == member.startName) {
									if(!notHaveVariable.Contains(fromUNode.Variables[i])) {
										notHaveVariable.Add(fromUNode.Variables[i]);
									}
									break;
								}
							}
						}
					} else if(member.startTarget is INode<uNodeRoot> root && root.GetOwner() == fromUNode) {
						List<VariableData> variables = null;
						if(root is IVariableSystem) {
							variables = (root as IVariableSystem).Variables;
						} else if(root is ILocalVariableSystem) {
							variables = (root as ILocalVariableSystem).LocalVariables;
						}
						if(notHaveVariable != null && !uNodeUtility.HasVariable(member.startName, targetUNode.Variables)) {
							for(int i = 0; i < variables.Count; i++) {
								if(variables[i].Name == member.startName) {
									if(!notHaveVariable.Contains(variables[i])) {
										notHaveVariable.Add(variables[i]);
									}
									break;
								}
							}
						}
					}
				}
				if(member.instance is MemberData) {
					ReflectComponent(member.instance, fromUNode, targetUNode, notHaveVariable);
				}
				return;
			}
			if(obj is EventData) {
				EventData member = obj as EventData;
				if(member != null && member.blocks.Count > 0) {
					foreach(EventActionData action in member.blocks) {
						ReflectComponent(action.block, fromUNode, targetUNode, notHaveVariable);
					}
				}
				return;
			}
			FieldInfo[] fieldInfo = ReflectionUtils.GetFields(obj);
			foreach(FieldInfo field in fieldInfo) {
				Type fieldType = field.FieldType;
				if(!fieldType.IsClass)
					continue;
				object value = field.GetValueOptimized(obj);
				if(object.ReferenceEquals(value, null))
					continue;
				if(fieldType == typeof(UnityEngine.Object) || fieldType.IsSubclassOf(typeof(UnityEngine.Object)) || value is UnityEngine.Object) {
					if(value is uNodeRoot) {
						uNodeRoot member = value as uNodeRoot;
						if(member != null && member == fromUNode) {
							member = targetUNode;
							field.SetValueOptimized(obj, member);
						}
					} else if(value is Transform) {
						Transform member = value as Transform;
						if(member != null && member == fromUNode.transform) {
							member = fromUNode.transform;
							field.SetValueOptimized(obj, member);
						}
					} else if(value is GameObject) {
						GameObject member = value as GameObject;
						if(member != null && member == fromUNode.gameObject) {
							member = fromUNode.gameObject;
							field.SetValueOptimized(obj, member);
						}
					} else if(value is Component) {
						Component member = value as Component;
						if(member != null) {
							Component comp = targetUNode.GetComponent(member.GetType());
							if(comp != null && fromUNode.GetComponent(member.GetType()) == member) {
								member = comp;
								field.SetValueOptimized(obj, member);
							}
						}
					}
					continue;
				}
				if(fieldType == typeof(MemberData)) {
					ReflectComponent(value, fromUNode, targetUNode, notHaveVariable);
					continue;
				}
				if(fieldType == typeof(EventData)) {
					EventData member = value as EventData;
					if(member != null && member.blocks.Count > 0) {
						foreach(EventActionData action in member.blocks) {
							ReflectComponent(action.block, fromUNode, targetUNode, notHaveVariable);
						}
					}
					continue;
				}
				if(!fieldType.IsArray && !fieldType.IsGenericType) {
					ReflectComponent(value, fromUNode, targetUNode, notHaveVariable);
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
							if(element is uNodeRoot) {
								uNodeRoot member = element as uNodeRoot;
								if(member != null && member == fromUNode) {
									member = targetUNode;
								}
							} else if(element is Transform) {
								Transform member = element as Transform;
								if(member != null && member == fromUNode.transform) {
									member = targetUNode.transform;
									list[i] = member;
								}
							} else if(element is GameObject) {
								GameObject member = element as GameObject;
								if(member != null && member == fromUNode.gameObject) {
									member = targetUNode.gameObject;
									list[i] = member;
								}
							} else if(element is Component) {
								Component member = element as Component;
								if(member != null) {
									Component comp = targetUNode.GetComponent(member.GetType());
									if(comp != null && fromUNode.GetComponent(member.GetType()) == member) {
										member = comp;
										list[i] = member;
									}
								}
							}
							continue;
						}
						if(elementType == typeof(MemberData)) {
							MemberData member = element as MemberData;
							ReflectComponent(member, fromUNode, targetUNode, notHaveVariable);
							continue;
						}
						if(elementType == typeof(EventData)) {
							EventData member = element as EventData;
							if(member != null && member.blocks.Count > 0) {
								foreach(EventActionData action in member.blocks) {
									ReflectComponent(action.block, fromUNode, targetUNode, notHaveVariable);
								}
							}
							continue;
						}
						if(!elementType.IsArray && !elementType.IsGenericType) {
							ReflectComponent(element, fromUNode, targetUNode, notHaveVariable);
							continue;
						}
					}
				}
			}
		}
		#endregion

		#region Component
		/// <summary>
		/// Get the current root.
		/// </summary>
		/// <returns></returns>
		public static Component GetCurrentRoot(GraphEditorData editorData) {
			if(editorData.selectedGroup) {
				return editorData.selectedGroup;
			} else if(editorData.selectedRoot) {
				return editorData.selectedRoot;
			} else {
				return GetNodeRoot(editorData.graph).transform;
			}
		}

		/// <summary>
		/// Find all nodes in the current group.
		/// </summary>
		/// <returns></returns>
		public static List<Node> FindAllNodeInCurrentGroup(GraphEditorData editorData) {
			Transform tr = GetCurrentRoot(editorData).transform;
			if(tr) {
				List<Node> allNodes = new List<Node>();
				foreach(Transform child in tr) {
					Node n = child.GetComponent<Node>();
					if(n) {
						allNodes.Add(n);
					}
				}
				return allNodes;
			}
			return null;
		}

		/// <summary>
		/// Find all nodes
		/// </summary>
		/// <returns></returns>
		public static NodeComponent[] FindAllNode(GraphEditorData editorData) {
			return FindAllNode(editorData.graph);
		}

		/// <summary>
		/// Find all nodes
		/// </summary>
		/// <param name="targetRoot"></param>
		/// <returns></returns>
		public static NodeComponent[] FindAllNode(uNodeRoot targetRoot) {
			var root = GetNodeRoot(targetRoot);
			if(root) {
				return root.GetComponentsInChildren<NodeComponent>();
			}
			return null;
		}

		/// <summary>
		/// Used to copy component to another game object
		/// </summary>
		/// <param name="component">The component to copy</param>
		/// <param name="target">The target GameObject to add the component</param>
		/// <returns></returns>
		public static Component CopyComponent(Component component, GameObject target) {
			Type type = component.GetType();
			Component comp = target.AddComponent(type);
			FieldInfo[] fields = ReflectionUtils.GetFields(comp);
			foreach(var field in fields) {
				field.SetValue(target.GetComponent(type), field.GetValue(component));
			}
			return comp;
		}
		/// <summary>
		/// Find the parent node.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="from"></param>
		/// <param name="list"></param>
		/// <param name="UNR"></param>
		public static void FindParentNode<T>(Transform from, ref List<T> list, uNodeRoot UNR) where T : NodeComponent {
			if(list == null) {
				list = new List<T>();
			}
			if(from == null || from.parent == null || UNR != null && from.parent.gameObject == UNR.RootObject.transform)
				return;
			T node = from.parent.GetComponent<T>();
			if(node) {
				list.Add(node);
				FindParentNode(from.parent, ref list, UNR);
			}
		}

		/// <summary>
		/// Find the parent node.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="list"></param>
		/// <param name="UNR"></param>
		public static void FindParentNode(Transform from, ref List<ISuperNode> list, uNodeRoot UNR) {
			if(list == null) {
				list = new List<ISuperNode>();
			}
			if(from == null || from.parent == null || UNR != null && from.parent.gameObject == UNR.RootObject.transform)
				return;
			NodeComponent node = from.parent.GetComponent<NodeComponent>();
			if(node is ISuperNode) {
				list.Add(node as ISuperNode);
				FindParentNode(from.parent, ref list, UNR);
			}
		}

		/// <summary>
		/// Find the parent node.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="list"></param>
		/// <param name="UNR"></param>
		public static void FindParentNode(Transform from, ref List<IVariableSystem> list, uNodeRoot UNR) {
			if(list == null) {
				list = new List<IVariableSystem>();
			}
			if(from == null || from.parent == null || UNR != null && from.parent.gameObject == UNR.RootObject.transform)
				return;
			NodeComponent node = from.parent.GetComponent<NodeComponent>();
			if(node is IVariableSystem) {
				list.Add(node as IVariableSystem);
				FindParentNode(from.parent, ref list, UNR);
			}
		}

		/// <summary>
		/// Get the RootObject.
		/// </summary>
		/// <param name="target"></param>
		/// <returns></returns>
		public static GameObject GetNodeRoot(uNodeRoot target) {
			if(target == null)
				return null;
			if(target.RootObject != null)
				return target.RootObject;
			if(!Application.isPlaying) {
				if(uNodeEditorUtility.IsPrefab(target)) {
					uNodeRoot UNR = (uNodeRoot)PrefabUtility.InstantiatePrefab(target);
					if(UNR != null) {
						GameObject GO = new GameObject("Root");
						GO.transform.parent = UNR.transform;
						UNR.RootObject = GO;
						uNodeEditorUtility.SavePrefabAsset(UNR.transform.root.gameObject, target.transform.root);
						Object.DestroyImmediate(UNR.transform.root.gameObject);
						return UNR.RootObject;
					}
					return null;
				}
			}
			GameObject go = new GameObject("Root");
			go.transform.parent = target.transform;
			target.RootObject = go;
			return target.RootObject;
		}
		#endregion

		#region Add
		/// <summary>
		/// Add a new event node.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="Name"></param>
		/// <param name="methodType"></param>
		/// <param name="position"></param>
		public static void AddNewEvent(uNodeRoot root, string Name, EventNode.EventType methodType, Vector2 position) {
			GameObject go = new GameObject(Name);
			//go.hideFlags = HideFlags.HideInInspector;
			if(uNodeEditorUtility.IsPrefab(root.gameObject)) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			} else {
				go.transform.parent = GetNodeRoot(root).transform;
				go.AddComponent<EventNode>();
				go.GetComponent<EventNode>().owner = root;
				go.GetComponent<EventNode>().Name = Name;
				go.GetComponent<EventNode>().editorRect = new Rect(position.x, position.y, 170, 40);
				go.GetComponent<EventNode>().eventType = methodType;
				Undo.RegisterCreatedObjectUndo(go, "New Method : " + methodType.ToString());
			}
		}

		/// <summary>
		/// Add a new transition.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="Name"></param>
		/// <param name="eventType"></param>
		/// <param name="parent"></param>
		/// <param name="position"></param>
		public static void AddNewTransitionNode(uNodeRoot root, string Name, StateEventNode.EventType eventType, Transform parent, Vector2 position) {
			GameObject go = new GameObject(Name);
			//go.hideFlags = HideFlags.HideInInspector;
			if(uNodeEditorUtility.IsPrefab(parent.gameObject)) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			} else {
				go.transform.parent = parent;
				go.AddComponent<StateEventNode>();
				go.GetComponent<StateEventNode>().owner = root;
				go.GetComponent<StateEventNode>().Name = Name;
				go.GetComponent<StateEventNode>().editorRect = new Rect(position.x, position.y, 170, 40);
				go.GetComponent<StateEventNode>().eventType = eventType;
				Undo.RegisterCreatedObjectUndo(go, "New Event : " + eventType.ToString());
			}
		}

		/// <summary>
		/// Add a new function.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="Name"></param>
		/// <param name="returnType"></param>
		/// <param name="parameterName"></param>
		/// <param name="parameterType"></param>
		/// <param name="genericParameter"></param>
		public static void AddNewFunction(uNodeRoot root, string Name, Type returnType, string[] parameterName, Type[] parameterType, params string[] genericParameter) {
			if(parameterName.Length != parameterType.Length)
				throw new System.Exception("Parameter Name & Parameter Type length must same.");
			GameObject go = new GameObject(Name);
			uNodeFunction method = go.AddComponent<uNodeFunction>();
			method.Name = Name;
			method.returnType = new MemberData(returnType, MemberData.TargetType.Type);
			{//Init start node
				GameObject nodeObj = new GameObject("Entry");
				nodeObj.transform.parent = go.transform;
				var node = nodeObj.AddComponent<Nodes.NodeAction>();
				if(returnType != typeof(void) && returnType != typeof(IEnumerable) && returnType != typeof(IEnumerator)) {
					GameObject returnObj = new GameObject("Return");
					returnObj.transform.parent = go.transform;
					var returnNode = returnObj.AddComponent<NodeReturn>();
					node.onFinished = MemberData.CreateConnection(returnNode, true);
				}
				method.startNode = node;
			}
			if(parameterName.Length > 0) {
				for(int i = 0; i < parameterName.Length; i++) {
					ArrayUtility.Add(ref method.parameters, new ParameterData(parameterName[i], parameterType[i]));
				}
			}
			if(genericParameter != null && genericParameter.Length > 0) {
				for(int i = 0; i < genericParameter.Length; i++) {
					ArrayUtility.Add(ref method.genericParameters, new GenericParameterData(genericParameter[i]));
				}
			}
			if(uNodeEditorUtility.IsPrefab(root.gameObject)) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			} else {
				go.transform.parent = GetNodeRoot(root).transform;
				Undo.RegisterCreatedObjectUndo(go, "New Function : " + Name);
			}
		}

		/// <summary>
		/// Add a new function.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="Name"></param>
		/// <param name="returnType"></param>
		/// <param name="parameterName"></param>
		/// <param name="parameterType"></param>
		/// <param name="genericParameter"></param>
		/// <param name="action"></param>
		public static void AddNewFunction(uNodeRoot root, string Name, Type returnType, string[] parameterName, Type[] parameterType, string[] genericParameter, Action<uNodeFunction> action) {
			if(parameterName.Length != parameterType.Length)
				throw new Exception("Parameter Name & Parameter Type length must same.");
			GameObject go = new GameObject(Name);
			uNodeFunction method = go.AddComponent<uNodeFunction>();
			method.Name = Name;
			method.returnType = new MemberData(returnType, MemberData.TargetType.Type);
			{//Init start node
				GameObject nodeObj = new GameObject("Entry");
				nodeObj.transform.parent = go.transform;
				var node = nodeObj.AddComponent<Nodes.NodeAction>();
				if(returnType != typeof(void) && returnType != typeof(IEnumerable) && returnType != typeof(IEnumerator)) {
					GameObject returnObj = new GameObject("Return");
					returnObj.transform.parent = go.transform;
					var returnNode = returnObj.AddComponent<NodeReturn>();
					node.onFinished = MemberData.CreateConnection(returnNode, true);
				}
				method.startNode = node;
			}
			if(parameterName.Length > 0) {
				for(int i = 0; i < parameterName.Length; i++) {
					ArrayUtility.Add(ref method.parameters, new ParameterData(parameterName[i], parameterType[i]));
				}
			}
			if(genericParameter != null && genericParameter.Length > 0) {
				for(int i = 0; i < genericParameter.Length; i++) {
					ArrayUtility.Add(ref method.genericParameters, new GenericParameterData(genericParameter[i]));
				}
			}
			if(action != null) {
				action(method);
			}
			if(uNodeEditorUtility.IsPrefab(root.gameObject)) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			} else {
				go.transform.parent = GetNodeRoot(root).transform;
				Undo.RegisterCreatedObjectUndo(go, "New Function : " + Name);
			}
		}

		/// <summary>
		/// Add a new function.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="Name"></param>
		/// <param name="returnType"></param>
		/// <param name="action"></param>
		public static void AddNewFunction(uNodeRoot root, string Name, Type returnType, Action<uNodeFunction> action = null) {
			GameObject go = new GameObject(Name);
			//go.hideFlags = HideFlags.HideInInspector;
			uNodeFunction method = go.AddComponent<uNodeFunction>();
			method.Name = Name;
			method.returnType = new MemberData(returnType, MemberData.TargetType.Type);
			{//Init start node
				GameObject nodeObj = new GameObject("Entry");
				nodeObj.transform.parent = go.transform;
				var node = nodeObj.AddComponent<Nodes.NodeAction>();
				if(returnType != typeof(void) && returnType != typeof(IEnumerable) && returnType != typeof(IEnumerator)) {
					GameObject returnObj = new GameObject("Return");
					returnObj.transform.parent = go.transform;
					var returnNode = returnObj.AddComponent<NodeReturn>();
					node.onFinished = MemberData.CreateConnection(returnNode, true);
				}
				method.startNode = node;
			}
			if (action != null) {
				action(method);
			}
			if (uNodeEditorUtility.IsPrefab(root.gameObject)) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			} else {
				go.transform.parent = GetNodeRoot(root).transform;
				Undo.RegisterCreatedObjectUndo(go, "New Function : " + Name);
				if (GraphUtility.IsTempGraphObject(root.gameObject)) {
					uNodeThreadUtility.ExecuteOnce(() => {//Autosave
						GraphUtility.AutoSaveGraph(root.gameObject);
					}, "UNODE_ADD_VARIABLE_AUTOSAVE" + root.gameObject.GetInstanceID());
				}
			}
		}

		/// <summary>
		/// Add a new object.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="Name"></param>
		/// <param name="parent"></param>
		/// <param name="action"></param>
		public static void AddNewObject(uNodeRoot root, string Name, Transform parent, Action<GameObject> action) {
			GameObject go = new GameObject(Name);
			if(uNodeEditorUtility.IsPrefab(root.gameObject)) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			} else {
				go.transform.parent = parent == null ? GetNodeRoot(root).transform : parent;
				if(action != null) {
					action(go);
				}
				Undo.RegisterCreatedObjectUndo(go, "New Object : " + Name);
			}
			if(NodeGraph.openedGraph != null) {
				NodeGraph.openedGraph.Refresh();
			}
		}

		public static void AddNewObject<T>(uNodeRoot root, string Name, Transform parent, Action<T> action) where T : Component {
			GameObject go = new GameObject(Name);
			if(uNodeEditorUtility.IsPrefab(root.gameObject)) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			} else {
				go.transform.parent = parent == null ? GetNodeRoot(root).transform : parent;
				var comp = go.AddComponent<T>();
				if(action != null) {
					action(comp);
				}
				Undo.RegisterCreatedObjectUndo(go, "New Object : " + Name);
			}
			if(NodeGraph.openedGraph != null) {
				NodeGraph.openedGraph.Refresh();
			}
		}

		/// <summary>
		/// Add a new property.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="Name"></param>
		/// <param name="action"></param>
		public static void AddNewProperty(uNodeRoot root, string Name, Action<uNodeProperty> action = null) {
			GameObject go = new GameObject(Name);
			//go.hideFlags = HideFlags.HideInInspector;
			uNodeProperty property = go.AddComponent<uNodeProperty>();
			property.Name = Name;
			if(action != null) {
				action(property);
			}
			if(uNodeEditorUtility.IsPrefab(root.gameObject)) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			} else {
				go.transform.parent = GetNodeRoot(root).transform;
				Undo.RegisterCreatedObjectUndo(go, "New Property : " + Name);
				if(GraphUtility.IsTempGraphObject(root.gameObject)) {
					uNodeThreadUtility.ExecuteOnce(() => {//Autosave
						GraphUtility.AutoSaveGraph(root.gameObject);
					}, "UNODE_ADD_VARIABLE_AUTOSAVE" + root.gameObject.GetInstanceID());
				}
			}
			if(NodeGraph.openedGraph != null) {
				NodeGraph.openedGraph.Refresh();
			}
		}

		/// <summary>
		/// Add a new constructor.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="Name"></param>
		/// <param name="action"></param>
		public static void AddNewConstructor(uNodeRoot root, string Name, Action<uNodeConstuctor> action = null) {
			GameObject go = new GameObject(Name);
			//go.hideFlags = HideFlags.HideInInspector;
			uNodeConstuctor ctor = go.AddComponent<uNodeConstuctor>();
			ctor.Name = Name;
			{//Init start node
				GameObject nodeObj = new GameObject("Entry");
				nodeObj.transform.parent = go.transform;
				var node = nodeObj.AddComponent<Nodes.NodeAction>();
				ctor.startNode = node;
			}
			if(action != null) {
				action(ctor);
			}
			if(uNodeEditorUtility.IsPrefab(root.gameObject)) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			} else {
				go.transform.parent = GetNodeRoot(root).transform;
				Undo.RegisterCreatedObjectUndo(go, "New Constructor : " + Name);
			}
			if(NodeGraph.openedGraph != null) {
				NodeGraph.openedGraph.Refresh();
			}
		}
		#endregion

		#region Add Nodes
		/// <summary>
		/// Add a new node.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="editorData"></param>
		/// <param name="action"></param>
		public static void AddNewNode<T>(GraphEditorData editorData, Vector2 position, Action<T> action = null) where T : NodeComponent {
			AddNewNode(editorData, null, null, position, action);
		}

		/// <summary>
		/// Add a new node.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="type"></param>
		/// <param name="editorData"></param>
		/// <param name="position"></param>
		/// <param name="action"></param>
		public static void AddNewNode<T>(Type type, GraphEditorData editorData, Vector2 position, Action<T> action = null) where T : NodeComponent {
			AddNewNode(editorData, null, type, position, action);
		}

		/// <summary>
		/// Add a new node.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="editorData"></param>
		/// <param name="name"></param>
		/// <param name="action"></param>
		public static void AddNewNode<T>(GraphEditorData editorData, string name, Vector2 position, Action<T> action = null) where T : NodeComponent {
			AddNewNode(editorData, name, null, position, action);
		}

		/// <summary>
		/// Add a new node.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="editorData"></param>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <param name="position"></param>
		/// <param name="action"></param>
		public static void AddNewNode<T>(GraphEditorData editorData, string name, Type type, Vector2 position, Action<T> action = null) where T : NodeComponent {
			if(string.IsNullOrEmpty(name)) {
				name = "Node";
			}
			bool isPrefab = uNodeEditorUtility.IsPrefab(editorData.graph.gameObject);
			if(isPrefab) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			} else {
				Undo.RegisterFullObjectHierarchyUndo(editorData.graph.gameObject, "New Node : " + name);
			}
			GameObject go = new GameObject(name + " " + editorData.nodes.Count);
			T newNode;
			if(type != null) {
				if(type.IsCastableTo(typeof(NodeComponent))) {
					newNode = go.AddComponent(type) as T;
				} else {
					var nod = go.AddComponent<Nodes.HLNode>();
					nod.type = MemberData.CreateFromType(type);
					newNode = nod as T;
				}
			} else {
				newNode = go.AddComponent<T>();
			}
			if(isPrefab) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			} else {
				if(editorData.selectedGroup) {
					newNode.transform.parent = editorData.selectedGroup.transform;
				} else if(editorData.selectedRoot) {
					newNode.transform.parent = editorData.selectedRoot.transform;
				} else {
					newNode.transform.parent = GetNodeRoot(editorData.graph).transform;
				}
				newNode.owner = editorData.graph;
				newNode.gameObject.name = newNode.name;
				newNode.editorRect = new Rect(position.x, position.y, 170, 100);
				Undo.RegisterCreatedObjectUndo(go, "New Node : " + name);
				if(action != null) {
					action(newNode);
				}
			}
			editorData.Refresh();
		}
		#endregion

		#region Change Nodes
		/// <summary>
		/// Change the node.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="root"></param>
		/// <param name="oldNode"></param>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static T ChangeNode<T>(uNodeRoot root, Node oldNode, string name = null, Type type = null) where T : Node {
			if(oldNode == null)
				return null;
			if(string.IsNullOrEmpty(name)) {
				name = oldNode.gameObject.name;
			}
			bool isPrefab = uNodeEditorUtility.IsPrefab(root.gameObject);
			T newNode;
			Undo.RegisterFullObjectHierarchyUndo(root.gameObject, "Change Node : " + name);
			if(isPrefab) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			} else {
				if(type != null) {
					newNode = Undo.AddComponent(oldNode.gameObject, type) as T;
				} else {
					newNode = Undo.AddComponent<T>(oldNode.gameObject);
				}
				if(newNode as StateNode && oldNode as StateNode) {
					(newNode as StateNode).TransitionEventObject = (oldNode as StateNode).TransitionEventObject;
				}
				newNode.editorRect = oldNode.editorRect;
				Object.DestroyImmediate(oldNode, true);
			}
			newNode.gameObject.name = name;
			newNode.owner = root;
			Undo.RecordObject(newNode, "Change Node : " + name);
			if(NodeGraph.openedGraph != null) {
				NodeGraph.openedGraph.Refresh();
			}
			return newNode;
		}
		#endregion

		#region Remove Function
		/// <summary>
		/// Remove specific object
		/// </summary>
		/// <param name="editorData"></param>
		/// <param name="gameObjects"></param>
		public static void RemoveObject(GraphEditorData editorData, params GameObject[] gameObjects) {
			if(gameObjects == null || gameObjects.Length == 0)
				return;
			Undo.SetCurrentGroupName("Remove Object" + (gameObjects.Length == 1 ? ": " + gameObjects[0].name : "s"));
			Undo.RegisterFullObjectHierarchyUndo(editorData.graph.gameObject, "Object");
			bool isPrefab = uNodeEditorUtility.IsPrefab(editorData.graph.gameObject);
			if(isPrefab) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			}
			uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(editorData.owner) as GameObject);
			foreach(var gameObject in gameObjects) {
				Undo.DestroyObjectImmediate(gameObject);
			}
			editorData.Refresh();
		}

		/// <summary>
		/// Remove specific object
		/// </summary>
		/// <param name="editorData"></param>
		/// <param name="gameObjects"></param>
		public static void RemoveObject(GameObject owner, params GameObject[] gameObjects) {
			if(gameObjects == null || gameObjects.Length == 0)
				return;
			Undo.SetCurrentGroupName("Remove Object" + (gameObjects.Length == 1 ? ": " + gameObjects[0].name : "s"));
			Undo.RegisterFullObjectHierarchyUndo(owner, "Object");
			bool isPrefab = uNodeEditorUtility.IsPrefab(owner);
			if(isPrefab) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			}
			uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(owner) as GameObject);
			foreach(var gameObject in gameObjects) {
				Undo.DestroyObjectImmediate(gameObject);
			}
			uNodeEditor.window?.Refresh();
		}
		/// <summary>
		/// Remove specific transition.
		/// </summary>
		/// <param name="transition"></param>
		public static void RemoveTransition(TransitionEvent transition) {
			uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(transition) as GameObject);
			Undo.DestroyObjectImmediate(transition);
		}

		/// <summary>
		/// Remove specific node
		/// </summary>
		/// <param name="editorData"></param>
		/// <param name="node"></param>
		public static void RemoveNode(GraphEditorData editorData, NodeComponent node) {
			if(node == null)
				return;
			Undo.SetCurrentGroupName("Remove Node " + node.gameObject.name);
			Undo.RegisterFullObjectHierarchyUndo(editorData.graph.gameObject, "Node");
			if(uNodeEditorUtility.IsPrefab(editorData.graph.gameObject)) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			}
			uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(node.gameObject) as GameObject);
			Undo.DestroyObjectImmediate(node.gameObject);
			editorData.Refresh();
		}

		/// <summary>
		/// Remove specific node root (Function/Constructor/Indexer/Property)
		/// </summary>
		/// <param name="editorData"></param>
		/// <param name="nodeRoot"></param>
		public static void RemoveNodeRoot(GraphEditorData editorData, RootObject nodeRoot) {
			Undo.SetCurrentGroupName("Remove Root " + nodeRoot.Name);
			Undo.RegisterFullObjectHierarchyUndo(editorData.graph.gameObject, "Node");
			bool isPrefab = uNodeEditorUtility.IsPrefab(editorData.graph.gameObject);
			if(isPrefab) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			}
			uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(nodeRoot) as GameObject);
			Undo.DestroyObjectImmediate(nodeRoot.gameObject);
		}

		/// <summary>
		/// Remove specific component from uNode
		/// </summary>
		/// <param name="editorData"></param>
		/// <param name="comp"></param>
		public static void RemoveComponent(GraphEditorData editorData, Component comp) {
			if(comp == null)
				return;
			Undo.SetCurrentGroupName("Remove " + comp.transform.name);
			Undo.RegisterFullObjectHierarchyUndo(editorData.graph.gameObject, "Node");
			bool isPrefab = uNodeEditorUtility.IsPrefab(editorData.graph.gameObject);
			if(isPrefab) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			}
			uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(comp) as GameObject);
			Undo.DestroyObjectImmediate(comp.gameObject);
			editorData.Refresh();
		}

		/// <summary>
		/// Remove selected Node
		/// </summary>
		/// <param name="editorData"></param>
		public static void RemoveSelectedNode(GraphEditorData editorData) {
			Undo.SetCurrentGroupName("Remove Node");
			Undo.RegisterFullObjectHierarchyUndo(editorData.graph.gameObject, "Node");
			bool isPrefab = uNodeEditorUtility.IsPrefab(editorData.graph.gameObject);
			if(isPrefab) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			}
			uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(editorData.owner) as GameObject);
			foreach(NodeComponent comp in editorData.selectedNodes) {
				if(comp == null)
					continue;
				Undo.DestroyObjectImmediate(comp.gameObject);
			}
			editorData.selectedNodes.Clear();
			editorData.Refresh();
		}

		/// <summary>
		/// Remove all state machine node & method
		/// </summary>
		/// <param name="editorData"></param>
		public static void RemoveAllSMObject(GraphEditorData editorData) {
			if(editorData.graph.RootObject == null)
				return;
			List<Node> nodes = new List<Node>(editorData.graph.nodes);
			nodes.RemoveAll(i => i == null || !i.IsInRoot);
			Undo.SetCurrentGroupName("Remove Node");
			Undo.RegisterFullObjectHierarchyUndo(editorData.graph.gameObject, "Node");
			bool isPrefab = uNodeEditorUtility.IsPrefab(editorData.graph.gameObject);
			if(isPrefab) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			}
			uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(editorData.owner) as GameObject);
			for(int i = 0; i < nodes.Count; i++) {
				if(nodes[i] == null)
					continue;
				Undo.DestroyObjectImmediate(nodes[i]);
			}
			var eventNodes = editorData.graph.RootObject.GetComponentsInChildren<BaseEventNode>();
			if(eventNodes != null) {
				for(int i = 0; i < eventNodes.Length; i++) {
					if(eventNodes[i] != null) {
						Undo.DestroyObjectImmediate(eventNodes[i]);
					}
				}
			}
			editorData.selectedNodes.Clear();
			editorData.Refresh();
		}

		/// <summary>
		/// Remove all state machine node
		/// </summary>
		/// <param name="editorData"></param>
		public static void RemoveAllSMNodes(GraphEditorData editorData) {
			if(editorData.graph.RootObject == null)
				return;
			List<Node> nodes = new List<Node>(editorData.graph.nodes);
			nodes.RemoveAll(i => i == null || !i.IsInRoot);
			Undo.SetCurrentGroupName("Remove Node");
			Undo.RegisterFullObjectHierarchyUndo(editorData.graph.gameObject, "Node");
			bool isPrefab = uNodeEditorUtility.IsPrefab(editorData.graph.gameObject);
			if(isPrefab) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			}
			uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(editorData.owner) as GameObject);
			var eventNodes = editorData.graph.RootObject.GetComponentsInChildren<BaseEventNode>();
			if(eventNodes != null) {
				for(int i = 0; i < eventNodes.Length; i++) {
					if(eventNodes[i] != null) {
						uNodeEditorUtility.RegisterUndo(eventNodes[i], "Node");
					}
				}
			}
			for(int i = 0; i < nodes.Count; i++) {
				if(nodes[i] == null)
					continue;
				Undo.DestroyObjectImmediate(nodes[i]);
			}
			editorData.selectedNodes.Clear();
			editorData.Refresh();
		}

		/// <summary>
		/// Remove all node
		/// </summary>
		/// <param name="editorData"></param>
		public static void RemoveAllNodes(GraphEditorData editorData) {
			if(editorData.graph.RootObject == null)
				return;
			editorData.selectedNodes.Clear();
			Undo.SetCurrentGroupName("Remove All nodes");
			Undo.RegisterFullObjectHierarchyUndo(editorData.graph.gameObject, "Node");
			bool isPrefab = uNodeEditorUtility.IsPrefab(editorData.graph.gameObject);
			if(isPrefab) {
				throw new Exception("Editing graph prefab dirrectly is not supported.");
			}
			uNodeEditorUtility.UnlockPrefabInstance(EditorBinding.getPrefabParent(editorData.owner) as GameObject);
			Undo.DestroyObjectImmediate(editorData.graph.RootObject);
			editorData.Refresh();
		}
		#endregion

		#region Selections
		/// <summary>
		/// Select a node for the graph.
		/// </summary>
		/// <param name="editorData"></param>
		/// <param name="node"></param>
		/// <param name="clearSelectedNodes"></param>
		public static void SelectNode(uNodeEditor.GraphData editorData, NodeComponent node, bool clearSelectedNodes = true) {
			if(clearSelectedNodes)
				editorData.selectedData.selectedNodes.Clear();
			editorData.selectedData.selectedNodes.Add(node);
			editorData.selectedData.selected = editorData.selectedData.selectedNodes;
		}

		/// <summary>
		/// Select a node for the graph.
		/// </summary>
		/// <param name="editorData"></param>
		/// <param name="root"></param>
		public static void SelectRoot(uNodeEditor.GraphData editorData, RootObject root) {
			editorData.selectedData.GetPosition(root);
			editorData.selectedData.selected = root;
			editorData.selectedData.selectedRoot = root;
			editorData.selectedData.selectedGroup = null;
		}
		#endregion

		#region Drawer
		private static List<INodeDrawer> _nodeDrawers;
		/// <summary>
		/// Find command menu on create node.
		/// </summary>
		/// <returns></returns>
		public static List<INodeDrawer> FindNodeDrawers() {
			if(_nodeDrawers == null) {
				_nodeDrawers = EditorReflectionUtility.GetListOfType<INodeDrawer>();
				_nodeDrawers.Sort((x, y) => {
					return string.CompareOrdinal(x.order.ToString(), y.order.ToString());
				});
			}
			return _nodeDrawers;
		}

		private static Dictionary<Type, INodeDrawer> _nodeDrawerMap = new Dictionary<Type, INodeDrawer>();
		/// <summary>
		/// Find node drawer for specific node.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static INodeDrawer FindNodeDrawer(Node node) {
			return FindNodeDrawer(node.GetType());
		}

		/// <summary>
		/// Find node drawer for specific node.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static INodeDrawer FindNodeDrawer(Type type) {
			INodeDrawer drawer;
			if(!_nodeDrawerMap.TryGetValue(type, out drawer)) {
				var drawers = FindNodeDrawers();
				if(drawers.Count > 0) {
					for(int i = 0; i < drawers.Count; i++) {
						if(drawers[i].IsValid(type)) {
							drawer = drawers[i];
							break;
						}
					}
				}
				_nodeDrawerMap[type] = drawer;
			}
			return drawer;
		}
		#endregion

		#region Menu
		private static List<TransitionMenu> _transitionMenus;
		/// <summary>
		/// Find all transition menu.
		/// </summary>
		/// <returns></returns>
		public static List<TransitionMenu> FindTransitionMenu() {
			if(_transitionMenus == null) {
				_transitionMenus = new List<TransitionMenu>();
				foreach(Assembly assembly in EditorReflectionUtility.GetAssemblies()) {
					try {
						foreach(System.Type type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
							if(type.GetCustomAttributes(typeof(TransitionMenu), false).Length > 0) {
								TransitionMenu menuItem = (TransitionMenu)type.GetCustomAttributes(typeof(TransitionMenu), false)[0];
								menuItem.type = type;
								_transitionMenus.Add(menuItem);
							}
						}
					}
					catch { continue; }
				}

				_transitionMenus.Sort((x, y) => string.Compare(x.path, y.path));
			}
			return _transitionMenus;
		}

		private static List<NodeMenu> _nodeMenus;
		/// <summary>
		/// Find all node menu.
		/// </summary>
		/// <returns></returns>
		public static List<NodeMenu> FindNodeMenu() {
			if(_nodeMenus == null) {
				_nodeMenus = new List<NodeMenu>();
				foreach(Assembly assembly in EditorReflectionUtility.GetAssemblies()) {
					try {
						foreach(System.Type type in EditorReflectionUtility.GetAssemblyTypes(assembly)) {
							if(type.IsDefined(typeof(NodeMenu), false)) {
								NodeMenu menuItem = (NodeMenu)type.GetCustomAttributes(typeof(NodeMenu), false)[0];
								menuItem.type = type;
								if(string.IsNullOrEmpty(menuItem.tooltip) && type.IsDefined(typeof(DescriptionAttribute), false)) {
									var des = (DescriptionAttribute)type.GetCustomAttributes(typeof(DescriptionAttribute), false)[0];
									menuItem.tooltip = des.description;
								}
								_nodeMenus.Add(menuItem);
							}
						}
					}
					catch { continue; }
				}
				_nodeMenus.Sort((x, y) => CompareUtility.Compare(x.category, x.order, y.category, y.order));
				_nodeMenus.Sort((x, y) => {
					int result = string.Compare(x.category, y.category);
					if(result == 0) {
						return string.Compare(x.name, y.name);
					}
					return result;
				});
			}
			return _nodeMenus;
		}

		private static List<NodeMenuCommand> _customNodeCommands;
		public static List<NodeMenuCommand> FindNodeCommands() {
			if(_customNodeCommands == null) {
				_customNodeCommands = EditorReflectionUtility.GetListOfType<NodeMenuCommand>();
				_customNodeCommands.Sort((x, y) => {
					return CompareUtility.Compare(x.name, x.order, y.name, y.order);
				});
			}
			return _customNodeCommands;
		}

		private static List<CustomInputPortItem> _customInputPortItems;
		public static List<CustomInputPortItem> FindCustomInputPortItems() {
			if(_customInputPortItems == null) {
				_customInputPortItems = EditorReflectionUtility.GetListOfType<CustomInputPortItem>();
				_customInputPortItems.Sort((x, y) => {
					return CompareUtility.Compare(x.order, y.order);
				});
			}
			return _customInputPortItems;
		}

		private static List<GraphMenuCommand> _customGraphCommands;
		public static List<GraphMenuCommand> FindGraphCommands() {
			if(_customGraphCommands == null) {
				_customGraphCommands = EditorReflectionUtility.GetListOfType<GraphMenuCommand>();
				_customGraphCommands.Sort((x, y) => {
					return CompareUtility.Compare(x.name, x.order, y.name, y.order);
				});
			}
			return _customGraphCommands;
		}

		private static List<INodeItemCommand> _createNodeMenuCommands;
		/// <summary>
		/// Find command menu on create node.
		/// </summary>
		/// <returns></returns>
		public static List<INodeItemCommand> FindCreateNodeCommands() {
			if(_createNodeMenuCommands == null) {
				_createNodeMenuCommands = EditorReflectionUtility.GetListOfType<INodeItemCommand>();
				_createNodeMenuCommands.Sort((x, y) => {
					return CompareUtility.Compare(x.name, x.order, y.name, y.order);
				});
			}
			return _createNodeMenuCommands;
		}

		private static List<PortMenuCommand> _customPortCommands;
		/// <summary>
		/// Find command pin menu.
		/// </summary>
		/// <returns></returns>
		public static List<PortMenuCommand> FindPortCommands() {
			if(_customPortCommands == null) {
				_customPortCommands = EditorReflectionUtility.GetListOfType<PortMenuCommand>();
				_customPortCommands.Sort((x, y) => {
					return CompareUtility.Compare(x.name, x.order, y.name, y.order);
				});
			}
			return _customPortCommands;
		}

		private static List<AutoConvertPort> _convertPorts;
		/// <summary>
		/// Find command pin menu.
		/// </summary>
		/// <returns></returns>
		public static List<AutoConvertPort> FindAutoConvertPorts() {
			if(_convertPorts == null) {
				_convertPorts = EditorReflectionUtility.GetListOfType<AutoConvertPort>();
				_convertPorts.Sort((x, y) => {
					return string.CompareOrdinal(x.order.ToString(), y.order.ToString());
				});
			}
			return _convertPorts;
		}
		#endregion

		#region FitNodes
		public static void FitNodePositions(IEnumerable<NodeComponent> nodes) {
			FitNodePositions(nodes, Vector2.zero);
		}

		public static void FitNodePositions(IEnumerable<NodeComponent> nodes, Vector2 center, Vector2 spacing = default(Vector2)) {
			var list = new List<NodeComponent>(nodes);
			if(list.Count > 1) {
				if(spacing == Vector2.zero) {
					spacing = new Vector2(75, 30);
				}
				Vector2 offset = Vector2.zero;
				list.RemoveAll(n => (n is Node) && !(n as Node).IsFlowNode());
				list.Sort((x, y) => string.Compare(x.editorRect.x.ToString(), y.editorRect.x.ToString()));
				foreach(Node n in list) {
					n.editorRect.x = offset.x + spacing.x + center.x;
					n.editorRect.y = offset.y + spacing.y + center.y;
					offset.x += n.editorRect.width + spacing.x;
					//offset.y += n.editorRect.height + ySpacing;
				}
			} else if(list.Count == 0) {
				FitNodePositions(list[0], center, spacing);
			}
		}

		public static void FitNodePositionsWithGroup(NodeComponent node, Action<List<NodeComponent>, Rect> action = null) {
			List<NodeComponent> list;
			var rect = FitNodePositions(node, out list);
			if(action != null) {
				action(list, rect);
			}
			if(node is GroupNode && (node as GroupNode).nodeToExecute) {
				FitNodePositionsWithGroup((node as GroupNode).nodeToExecute, action);
			}
			if(list != null && list.Count > 0) {
				foreach(var comp in list) {
					if(comp is GroupNode && (comp as GroupNode).nodeToExecute) {
						FitNodePositionsWithGroup((comp as GroupNode).nodeToExecute, action);
					}
				}
			}
		}

		public static void FitNodePositionsWithGroup(NodeComponent node,
			Action<List<NodeComponent>, Rect> action = null,
			Vector2 center = default(Vector2),
			Vector2 spacing = default(Vector2),
			Vector2 offset = default(Vector2),
			HashSet<NodeComponent> oldNodes = null) {
			List<NodeComponent> list;
			var rect = FitNodePositions(node, out list, center, spacing, offset, oldNodes);
			if(action != null) {
				action(list, rect);
			}
			if(list != null && list.Count > 0) {
				foreach(var comp in list) {
					if(comp is GroupNode && (comp as GroupNode).nodeToExecute) {
						FitNodePositionsWithGroup((comp as GroupNode).nodeToExecute, action);
					}
				}
			}
		}

		public static Rect FitNodePositions(NodeComponent node) {
			return FitNodePositions(node, Vector2.zero);
		}

		public static Rect FitNodePositions(NodeComponent from, out List<NodeComponent> connections, Vector2 center = default(Vector2),
			Vector2 spacing = default(Vector2), Vector2 offset = default(Vector2),
			HashSet<NodeComponent> oldNodes = null) {
			if(spacing == Vector2.zero) {
				spacing = new Vector2(75, 30);
			}
			return FitNode(from, out connections, center, spacing, offset, oldNodes);
		}

		public static Rect FitNodePositions(NodeComponent from, Vector2 center,
			Vector2 spacing = default(Vector2), Vector2 offset = default(Vector2),
			HashSet<NodeComponent> oldNodes = null) {
			if(spacing == Vector2.zero) {
				spacing = new Vector2(75, 30);
			}
			return FitNode(from, center, spacing, offset, oldNodes);
		}

		private static Rect FitNode(NodeComponent from, Vector2 center,
			Vector2 spacing = default(Vector2), Vector2 offset = default(Vector2),
			HashSet<NodeComponent> oldNodes = null) {
			List<NodeComponent> connections;
			return FitNode(from, out connections, center, spacing, offset, oldNodes);
		}

		private static Rect FitNode(NodeComponent from, out List<NodeComponent> connections, Vector2 center,
		Vector2 spacing = default(Vector2), Vector2 offset = default(Vector2),
		HashSet<NodeComponent> oldNodes = null) {
			var data = oldNodes != null ? new HashSet<NodeComponent>(oldNodes) : new HashSet<NodeComponent>();
			var nodes = FindConnectedNode(from, false);
			List<NodeComponent> list = new List<NodeComponent>();
			Vector2 flowOffset = offset;
			Vector2 valueOffset = offset;
			Vector2 valueExOffset = offset;
			float additionWidth = 0;
			Action flowAction = null;
			foreach(var node in nodes) {
				if(data.Contains(node))
					continue;
				data.Add(node);
				var rectOffset = Rect.zero;
				if(node is Node nod && (nod.CanGetValue() || nod.CanSetValue())) {
					List<NodeComponent> list2;
					if(from is Node && !(from as Node).CanGetValue() && !(from as Node).CanSetValue()) {
						rectOffset = FitNode(node, out list2, center, spacing, new Vector2(valueOffset.x, valueOffset.y + spacing.y + from.editorRect.height), data);
						valueOffset.y += rectOffset.height + spacing.y;
						if(additionWidth < rectOffset.width) {
							additionWidth = rectOffset.width + spacing.x;
						}
					} else {
						rectOffset = FitNode(node, out list2, center, spacing, new Vector2(
							valueOffset.x - spacing.x - from.editorRect.width,
							(valueOffset.y + spacing.y) + from.editorRect.height), data);
						valueOffset.y += rectOffset.height + spacing.y;
					}
					if(list2 != null) {
						list.AddRange(list2);
					}
				} else {
					flowAction += () => {
						List<NodeComponent> list2;
						rectOffset = FitNode(node, out list2, center, spacing, new Vector2(flowOffset.x + spacing.x + additionWidth + from.editorRect.width, flowOffset.y), data);
						//flowOffset.x += rectOffset.width + spacing.x;
						flowOffset.y += rectOffset.height + spacing.y;
						if(list2 != null) {
							list.AddRange(list2);
						}
					};
				}
			}
			if(list.Count > 0) {
				NodeEditorUtility.MoveNodes(list, new Vector2(additionWidth, 0));
			}
			if(flowAction != null) {
				flowAction();
			}
			var rect = GetNodeRect(list);
			if(from is Node && ((from as Node).CanGetValue() || (from as Node).CanSetValue())) {
				//offset.y -= from.editorRect.height + spacing.y;
				//offset.y -= rect.height / 2;
				from.editorRect.x = offset.x + spacing.x + center.x;
				if(rect != Rect.zero) {
					from.editorRect.y = rect.y + (rect.height / 2) - (from.editorRect.height / 2);
				} else {
					from.editorRect.y = offset.y + spacing.y + center.y;
				}
			} else {
				offset.x += from.editorRect.width + additionWidth + spacing.x;
				//realOffset.y += referenceWidth / 2;
				from.editorRect.x = offset.x + spacing.x + center.x;
				from.editorRect.y = offset.y + spacing.y + center.y;
			}
			list.Add(from);
			connections = list;
			return GetNodeRect(list);
		}

		private static Vector2 FitPlace(Node node, Vector2 offset, Vector2 spacing, Vector2 center, List<Node> oldNode = null) {
			if(oldNode == null)
				oldNode = new List<Node>();
			Vector2 realOffset = offset;
			oldNode.Add(node);
			float referenceHeight = 0;
			float referenceWidth = 0;
			List<Node> nodes = new List<Node>();
			Func<object, bool> validation = delegate (object o) {
				if(o is MemberData) {
					MemberData member = o as MemberData;
					if(member.targetType == MemberData.TargetType.ValueNode ||
						member.targetType == MemberData.TargetType.NodeField ||
						member.targetType == MemberData.TargetType.NodeFieldElement) {
						if(member.isAssigned && member.GetInstance() as Node) {
							Node vNode = member.GetInstance() as Node;
							if(!oldNode.Contains(vNode) && !nodes.Contains(vNode)) {
								nodes.Add(vNode);
							}
						}
					}
				}
				return false;
			};
			AnalizerUtility.AnalizeObject(node, validation);
			for(int i = nodes.Count - 1; i >= 0; i--) {
				Node vNode = nodes[i];
				if(!vNode)
					continue;
				Vector2 retOffset = FitPlace(vNode,
					new Vector2(offset.x - spacing.x - node.editorRect.width, offset.y - spacing.y),
					spacing, center, oldNode);
				if(!vNode.IsFlowNode()) {
					//offset.y -= vNode.editorRect.height + spacing.y;
					offset.y -= retOffset.y;
				} else {
					offset.x += retOffset.x;
				}
				referenceHeight += retOffset.y;
				referenceWidth += retOffset.x;
			}
			if(!node.IsFlowNode()) {
				realOffset.y -= node.editorRect.height + spacing.y;
				realOffset.y -= referenceHeight / 2;
			} else {
				realOffset.x += node.editorRect.width + spacing.x;
				//realOffset.y += referenceWidth / 2;
			}
			node.editorRect.x = realOffset.x + spacing.x + center.x;
			node.editorRect.y = realOffset.y + spacing.y + center.y;
			return new Vector2(referenceWidth != 0 ? referenceWidth : node.editorRect.width + spacing.x,
				referenceHeight != 0 ? referenceHeight : node.editorRect.height + spacing.y);
		}
		#endregion

		#region Find Connection
		/// <summary>
		/// Find nodes which are connected to 'from' node to Get/Set value.
		/// </summary>
		/// <param name="from"></param>
		/// <returns></returns>
		public static HashSet<uNodeComponent> FindConnectedNodeToValueNode(NodeComponent from) {
			HashSet<uNodeComponent> result = new HashSet<uNodeComponent>();
			List<uNodeComponent> nodes = new List<uNodeComponent>();
			foreach(Transform t in from.transform.parent) {
				var comp = t.GetComponent<uNodeComponent>();
				if(comp != null) {
					if(comp is StateNode) {
						var state = comp as StateNode;
						var transitions = state.GetTransitions();
						foreach(var tr in transitions) {
							nodes.Add(tr);
						}
					}
					nodes.Add(comp);
				}
			}
			uNodeComponent currentNode = null;
			Func<object, bool> validation = delegate (object o) {
				if(o is MemberData) {
					MemberData member = o as MemberData;
					if(member.targetType == MemberData.TargetType.ValueNode) {
						Node n = member.GetTargetNode();
						if(n != null && n == from && !result.Contains(currentNode)) {
							result.Add(currentNode);
							return true;
						}
					}
				}
				return false;
			};
			foreach(var n in nodes) {
				currentNode = n;
				AnalizerUtility.AnalizeObject(n, validation);
			}
			return result;
		}

		/// <summary>
		/// Find nodes which are connected to 'from' using Flow connection.
		/// </summary>
		/// <param name="from"></param>
		/// <returns></returns>
		public static HashSet<uNodeComponent> FindConnectedNodeToFlowNode(NodeComponent from) {
			HashSet<uNodeComponent> result = new HashSet<uNodeComponent>();
			List<uNodeComponent> nodes = new List<uNodeComponent>();
			foreach(Transform t in from.transform.parent) {
				var comp = t.GetComponent<uNodeComponent>();
				if(comp != null) {
					if(comp is StateNode) {
						var state = comp as StateNode;
						var transitions = state.GetTransitions();
						foreach(var tr in transitions) {
							nodes.Add(tr);
						}
					}
					nodes.Add(comp);
				}
			}
			uNodeComponent currentNode = null;
			Func<object, bool> validation = delegate (object o) {
				if(o is MemberData) {
					MemberData member = o as MemberData;
					if(member.targetType == MemberData.TargetType.FlowInput ||
						member.targetType == MemberData.TargetType.FlowNode) {
						Node n = member.GetTargetNode();
						if(n != null && n == from && !result.Contains(currentNode)) {
							result.Add(currentNode);
							return true;
						}
					}
				}
				return false;
			};
			foreach(var n in nodes) {
				currentNode = n;
				AnalizerUtility.AnalizeObject(n, validation);
			}
			return result;
		}

		/// <summary>
		/// Find connected node.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="allConnections"></param>
		/// <returns></returns>
		public static HashSet<NodeComponent> FindConnectedNode(NodeComponent from, bool allConnections = true) {
			var hash = new HashSet<NodeComponent>();
			FindConnectedNode(from, hash, allConnections);
			return hash;
		}

		/// <summary>
		/// Find connected node.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="nodes"></param>
		/// <param name="allConnections"></param>
		/// <param name="includingNestedNodes"></param>
		public static void FindConnectedNode(NodeComponent from, HashSet<NodeComponent> nodes, bool allConnections = true, bool includingNestedNodes = false) {
			if(from is Node) {
				Node node = from as Node;
				if(node as StateNode) {
					StateNode eventNode = node as StateNode;
					TransitionEvent[] TE = eventNode.GetTransitions();
					foreach(TransitionEvent T in TE) {
						if(T.GetTargetNode() != null) {
							if(!nodes.Contains(T.GetTargetNode())) {
								nodes.Add(T.GetTargetNode());
								if(allConnections)
									FindConnectedNode(T.GetTargetNode(), nodes, allConnections, includingNestedNodes);
							}
						}
					}
				} else if(includingNestedNodes && from is ISuperNode) {
					ISuperNode superNode = from as ISuperNode;
					foreach(var n in superNode.nestedFlowNodes) {
						if(n == null)
							continue;
						nodes.Add(n);
						if(allConnections)
							FindConnectedNode(n, nodes, allConnections, includingNestedNodes);
					}
				}
				Func<object, bool> validation = delegate (object o) {
					if(o is MemberData) {
						MemberData member = o as MemberData;
						if(member.targetType == MemberData.TargetType.FlowNode ||
							member.targetType == MemberData.TargetType.ValueNode ||
							member.targetType == MemberData.TargetType.NodeField ||
							member.targetType == MemberData.TargetType.NodeFieldElement) {
							Node n = member.GetTargetNode();
							if(n != null) {
								if(!nodes.Contains(n)) {
									nodes.Add(n);
									if(allConnections)
										FindConnectedNode(n, nodes, allConnections, includingNestedNodes);
								}
								//return true;
							}
						}
					}
					return false;
				};
				/*if(node.CanHaveValueConnection() && node.valueNode != null) {
					foreach(var VN in node.valueNode) {
						if(VN == null || !VN.isAssigned) continue;
						var n = VN.GetTargetNode();
						if(!nodes.Contains(n)) {
							nodes.Add(n);
							uNodeUtils.ReflectObject(n, validation);
							if(allConnections)
								FindConnectedNode(n, nodes);
						}
					}
				}*/
				AnalizerUtility.AnalizeObject(node, validation);
			} else if(from is BaseEventNode) {
				BaseEventNode method = from as BaseEventNode;
				foreach(var flow in method.GetFlows()) {
					var n = flow.GetTargetNode();
					if(n == null)
						continue;
					if(!nodes.Contains(n)) {
						nodes.Add(n);
						if(allConnections)
							FindConnectedNode(n, nodes, allConnections, includingNestedNodes);
					}
				}
			}
		}

		/// <summary>
		/// Find all node which are connected to node.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="nodes"></param>
		/// <returns></returns>
		public static HashSet<NodeComponent> FindNodeConnectedToNode(NodeComponent node, IList<NodeComponent> nodes) {
			if(!node) {
				throw new ArgumentNullException("node");
			}
			if(nodes == null) {
				throw new ArgumentNullException("nodes");
			}
			Func<object, bool> validation = delegate (object o) {
				if(o is MemberData) {
					MemberData member = o as MemberData;
					if(member.targetType == MemberData.TargetType.FlowNode ||
						member.targetType == MemberData.TargetType.ValueNode ||
						member.targetType == MemberData.TargetType.NodeField ||
						member.targetType == MemberData.TargetType.NodeFieldElement) {
						Node n = member.GetTargetNode();
						if(n != null && n == node) {
							return true;
						}
					}
				}
				return false;
			};
			HashSet<NodeComponent> result = new HashSet<NodeComponent>();
			foreach(var n in nodes) {
				if(result.Contains(n))
					continue;
				AnalizerUtility.AnalizeObject(n, validation, (obj) => {
					result.Add(n);
				});
			}
			return result;
		}

		/// <summary>
		/// Find all MemberData that connected to node.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="nodes"></param>
		/// <returns></returns>
		public static List<MemberData> FindMemberConnectedToNode(NodeComponent node, IList<NodeComponent> nodes) {
			if(!node) {
				throw new ArgumentNullException("node");
			}
			if(nodes == null) {
				throw new ArgumentNullException("nodes");
			}
			List<MemberData> result = new List<MemberData>();
			Func<object, bool> validation = delegate (object o) {
				if(o is MemberData) {
					MemberData member = o as MemberData;
					if(member.targetType == MemberData.TargetType.FlowNode ||
						member.targetType == MemberData.TargetType.ValueNode ||
						member.targetType == MemberData.TargetType.NodeField ||
						member.targetType == MemberData.TargetType.NodeFieldElement) {
						Node n = member.GetTargetNode();
						if(n != null && n == node) {
							result.Add(member);
							//return true;
						}
					}
				}
				return false;
			};
			foreach(var n in nodes) {
				AnalizerUtility.AnalizeObject(n, validation);
			}
			return result;
		}

		/// <summary>
		/// Find all child nodes.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="from"></param>
		/// <returns></returns>
		public static List<T> FindChildNode<T>(Transform from) where T : NodeComponent {
			var list = new List<T>();
			if(from == null)
				return list;
			foreach(Transform child in from) {
				T node = child.GetComponent<T>();
				if(node) {
					list.Add(node);
				}
			}
			return list;
		}

		/// <summary>
		/// Find the root of the node, null if root is State Graphs.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="UNR"></param>
		/// <returns></returns>
		public static RootObject FindRootNode(Transform from, uNodeRoot UNR) {
			if(from == null || from.parent == null || UNR != null && from.parent.gameObject == UNR.RootObject.transform)
				return null;
			RootObject RO = from.parent.GetComponent<RootObject>();
			if(RO) {
				return RO;
			}
			return FindRootNode(from.parent, UNR);
		}
		#endregion

		#region MoveNodes
		/// <summary>
		/// Move the node to position
		/// </summary>
		/// <param name="position"></param>
		/// <param name="nodes"></param>
		public static void MoveNodes(Vector2 position, params NodeComponent[] nodes) {
			if(nodes.Length == 0)
				throw new ArgumentNullException();
			MoveNodes(nodes, position);
		}

		/// <summary>
		/// Move the node to position
		/// </summary>
		/// <param name="nodes"></param>
		/// <param name="position"></param>
		public static void MoveNodes(IList<NodeComponent> nodes, Vector2 position) {
			foreach(var node in nodes) {
				node.editorRect.x += position.x;
				node.editorRect.y += position.y;
			}
		}
		#endregion

		#region TeleportNodes
		/// <summary>
		/// Teleport the node to position
		/// </summary>
		/// <param name="position"></param>
		/// <param name="nodes"></param>
		public static void TeleportNodes(Vector2 position, params NodeComponent[] nodes) {
			TeleportNodes(nodes, position);
		}

		/// <summary>
		/// Teleport the node to position
		/// </summary>
		/// <param name="nodes"></param>
		/// <param name="position"></param>
		public static void TeleportNodes(IList<NodeComponent> nodes, Vector2 position) {
			Vector2 center = Vector2.zero;
			foreach(var node in nodes) {
				center.x += node.editorRect.x;
				center.y += node.editorRect.y;
			}
			center /= nodes.Count;
			foreach(var node in nodes) {
				node.editorRect.x = (node.editorRect.x - center.x) + position.x;
				node.editorRect.y = (node.editorRect.y - center.y) + position.y;
			}
		}
		#endregion

		#region GetNodeRect
		/// <summary>
		/// Get the node Rect
		/// </summary>
		/// <param name="node"></param>
		/// <param name="position"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		public static Rect GetNodeRect(Node node, Vector2 position, Vector2 size = new Vector2()) {
			return new Rect(node.editorRect.x + position.x, (node.editorRect.y + position.y) - 17, size.x, size.y);
		}

		/// <summary>
		/// Get the node Rect
		/// </summary>
		/// <param name="nodes"></param>
		/// <returns></returns>
		public static Rect GetNodeRect(params NodeComponent[] nodes) {
			return GetNodeRect(nodes.ToList());
		}

		/// <summary>
		/// Get the node Rect
		/// </summary>
		/// <param name="nodes"></param>
		/// <returns></returns>
		public static Rect GetNodeRect(IList<NodeComponent> nodes) {
			if(nodes == null || nodes.Count == 0)
				return Rect.zero;
			if(nodes.Count == 1) {
				return nodes[0].editorRect;
			}
			Rect rect = Rect.zero;
			foreach(var node in nodes) {
				if(rect == Rect.zero) {
					rect = node.editorRect;
					rect.width = rect.width + rect.x;
					rect.height = rect.height + rect.y;
					continue;
				}
				if(rect.width < node.editorRect.x + node.editorRect.width) {
					rect.width = node.editorRect.x + node.editorRect.width;
				}
				if(rect.height < node.editorRect.y + node.editorRect.height) {
					rect.height = node.editorRect.y + node.editorRect.height;
				}
				if(rect.x > node.editorRect.x) {
					rect.x = node.editorRect.x;
				}
				if(rect.y > node.editorRect.y) {
					rect.y = node.editorRect.y;
				}
			}
			rect.width -= rect.x;
			rect.height -= rect.y;
			return rect;
		}

		public static List<NodeComponent> GetNodeFromRect(Rect rect, IList<NodeComponent> nodes) {
			List<NodeComponent> list = new List<NodeComponent>();
			foreach(var n in nodes) {
				if(n != null && rect.Overlaps(n.editorRect)) {
					list.Add(n);
				}
			}
			return list;
		}
		#endregion

		#region Others
		public static Vector2 SnapTo(Vector2 vec, float snap) {
			return new Vector2(SnapTo(vec.x, snap), SnapTo(vec.y, snap));
		}

		public static float SnapTo(float a, float snap) {
			return Mathf.Round(a / snap) * snap;
		}
		#endregion
	}
}