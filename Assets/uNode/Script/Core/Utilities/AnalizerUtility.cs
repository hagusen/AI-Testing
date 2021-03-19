using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Object = UnityEngine.Object;

namespace MaxyGames.uNode {
    public class AnalizerUtility {
		private static BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;
		/// <summary>
		/// Perform field reflection in obj.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="validation"></param>
		/// <param name="doAction"></param>
		public static bool AnalizeObject(object obj, Func<object, bool> validation, Action<object> doAction = null) {
			if(object.ReferenceEquals(obj, null) || validation == null)
				return false;
			if(!(obj is UnityEngine.Object) && validation(obj)) {
				if(doAction != null)
					doAction(obj);
				return true;
			}
			if(obj is MemberData) {
				MemberData mData = obj as MemberData;
				if(mData != null && mData.instance != null && !(mData.instance is UnityEngine.Object)) {
					bool flag = AnalizeObject(mData.instance, validation, doAction);
					if(flag) {
						mData.instance = mData.instance;
					}
					return flag;
				}
				return false;
			}
			bool changed = false;
			if(obj is EventData) {
				EventData member = obj as EventData;
				if(member != null && member.blocks.Count > 0) {
					foreach(EventActionData action in member.blocks) {
						if(validation(action)) {
							if(doAction != null)
								doAction(action);
							changed = true;
							continue;
						}
						changed = AnalizeObject(action.block, validation, doAction) || changed;
					}
				}
				return changed;
			} else if(obj is IList) {
				IList list = obj as IList;
				for(int i = 0; i < list.Count; i++) {
					object element = list[i];
					if(element == null)
						continue;
					if(element is UnityEngine.Object) {
						if(validation(element)) {
							if(doAction != null)
								doAction(element);
							changed = true;
						}
						continue;
					}
					changed = AnalizeObject(element, validation, doAction) || changed;
				}
				return changed;
			}
			FieldInfo[] fieldInfo = ReflectionUtils.GetFields(obj, flags);
			foreach(FieldInfo field in fieldInfo) {
				Type fieldType = field.FieldType;
				if(!fieldType.IsClass)
					continue;
				object value = field.GetValueOptimized(obj);
				if(object.ReferenceEquals(value, null))
					continue;
				if(value is UnityEngine.Object) {
					if(validation(value)) {
						if(doAction != null)
							doAction(value);
						changed = true;
					}
					continue;
				}
				changed = AnalizeObject(value, validation, doAction) || changed;
			}
			return changed;
		}

		/// <summary>
		/// Perform field reflection in obj.
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="valueAction"></param>
		/// <param name="onAction"></param>
		public static void AnalizeObject(object obj, Func<object, bool> valueAction, Action<object, FieldInfo, Type, object> onAction) {
			if(object.ReferenceEquals(obj, null) || valueAction == null)
				return;
			if(!(obj is UnityEngine.Object) && valueAction(obj)) {
				return;
			}
			if(obj is MemberData) {
				MemberData mData = obj as MemberData;
				if(mData != null && mData.instance is MemberData) {
					AnalizeObject(mData.instance, valueAction, onAction);
				}
				return;
			}
			if(obj is EventData) {
				EventData member = obj as EventData;
				if(member != null && member.blocks.Count > 0) {
					foreach(EventActionData action in member.blocks) {
						if(valueAction(action)) {
							continue;
						}
						AnalizeObject(action.block, valueAction, onAction);
					}
				}
				return;
			} else if(obj is IList) {
				IList list = obj as IList;
				for(int i = 0; i < list.Count; i++) {
					object element = list[i];
					if(element == null || valueAction(element) || element is UnityEngine.Object)
						continue;
					AnalizeObject(element, valueAction, onAction);
				}
				return;
			}
			FieldInfo[] fieldInfo = ReflectionUtils.GetFields(obj, flags);
			foreach(FieldInfo field in fieldInfo) {
				Type fieldType = field.FieldType;
				if(!fieldType.IsClass)
					continue;
				object value = field.GetValueOptimized(obj);
				if(object.ReferenceEquals(value, null))
					continue;
				if(valueAction(value)) {
					if(onAction != null) {
						onAction(obj, field, fieldType, value);
					}
					continue;
				}
				if(value is UnityEngine.Object) {
					continue;
				}
				AnalizeObject(value, valueAction, onAction);
				if(onAction != null) {
					onAction(obj, field, fieldType, value);
				}
			}
		}

		/// <summary>
		/// Retarget node owner.
		/// </summary>
		/// <param name="fromOwner"></param>
		/// <param name="toOwner"></param>
		/// <param name="nodes"></param>
		public static void RetargetNodeOwner(uNodeRoot fromOwner, uNodeRoot toOwner, IList<uNodeComponent> nodes) {
			Object[] from = new Object[] { fromOwner, fromOwner.transform, fromOwner.gameObject };
			Object[] to = new Object[] { toOwner, toOwner.transform, toOwner.gameObject };
			foreach(var behavior in nodes) {
				if(behavior is Node) {
					Node node = behavior as Node;
					node.owner = toOwner;
				} else if(behavior is RootObject) {
					RootObject root = behavior as RootObject;
					root.owner = toOwner;
				}
				AnalizeObject(behavior, (obj) => {
					if(obj is MemberData) {
						MemberData data = obj as MemberData;
						data.RefactorUnityObject(from, to);
						//return true;
					}
					return false;
				}, (instance, field, type, value) => {
					Object o = value as Object;
					if(o) {
						if(o == fromOwner) {
							field.SetValueOptimized(instance, toOwner);
						} else if(o == from[1]) {
							field.SetValueOptimized(instance, to[1]);
						} else if(o == from[2]) {
							field.SetValueOptimized(instance, to[2]);
						}
					}
				});
			}
		}

		/// <summary>
		/// Get retarget node Action
		/// </summary>
		/// <param name="graph"></param>
		/// <param name="nodes"></param>
		/// <returns></returns>
		public static Action<uNodeRoot> GetRetargetNodeOwnerAction(uNodeRoot graph, IList<MonoBehaviour> nodes) {
			Action<uNodeRoot> action = null;
			var fromTR = graph.transform;
			var fromGO = graph.gameObject;
			var from = new Object[] { graph, fromTR, fromGO };
			foreach(var behavior in nodes) {
				if(behavior is Node) {
					Node node = behavior as Node;
					action += (owner) => {
						node.owner = owner;
					};
				} else if(behavior is RootObject) {
					RootObject root = behavior as RootObject;
					action += (owner) => {
						root.owner = owner;
					};
				}
				AnalizeObject(behavior, (obj) => {
					if(obj is MemberData) {
						MemberData data = obj as MemberData;
						if(data.instanceReference != null) {
							for(int i = 0; i < data.instanceReference.Count; i++) {
								Object o = data.instanceReference[i];
								int index = i;
								if(o == graph) {
									action += (owner) => {
										data.instanceReference[index] = owner;
									};
								} else if(o == fromTR) {
									action += (owner) => {
										data.instanceReference[index] = owner.transform;
									};
								} else if(o == fromGO) {
									action += (owner) => {
										data.instanceReference[index] = owner.gameObject;
									};
								}
							}
						}
						if(data.targetReference != null) {
							for(int i = 0; i < data.targetReference.Count; i++) {
								Object o = data.targetReference[i];
								int index = i;
								if(o == graph) {
									action += (owner) => {
										data.targetReference[index] = owner;
									};
								} else if(o == fromTR) {
									action += (owner) => {
										data.targetReference[index] = owner.transform;
									};
								} else if(o == fromGO) {
									action += (owner) => {
										data.targetReference[index] = owner.gameObject;
									};
								}
							}
						}
						if(data.typeReference != null) {
							for(int i = 0; i < data.typeReference.Count; i++) {
								Object o = data.typeReference[i];
								int index = i;
								if(o == graph) {
									action += (owner) => {
										data.typeReference[index] = owner;
									};
								} else if(o == fromTR) {
									action += (owner) => {
										data.typeReference[index] = owner.transform;
									};
								} else if(o == fromGO) {
									action += (owner) => {
										data.typeReference[index] = owner.gameObject;
									};
								}
							}
						}
						if(data.odinTargetData.references != null) {
							for(int i = 0; i < data.odinTargetData.references.Count; i++) {
								Object o = data.odinTargetData.references[i];
								int index = i;
								if(o == graph) {
									action += (owner) => {
										data.odinTargetData.references[index] = owner;
									};
								} else if(o == fromTR) {
									action += (owner) => {
										data.odinTargetData.references[index] = owner.transform;
									};
								} else if(o == fromGO) {
									action += (owner) => {
										data.odinTargetData.references[index] = owner.gameObject;
									};
								}
							}
						}
						if(data.odinInstanceData.references != null) {
							for(int i = 0; i < data.odinInstanceData.references.Count; i++) {
								Object o = data.odinInstanceData.references[i];
								int index = i;
								if(o == graph) {
									action += (owner) => {
										data.odinInstanceData.references[index] = owner;
									};
								} else if(o == fromTR) {
									action += (owner) => {
										data.odinInstanceData.references[index] = owner.transform;
									};
								} else if(o == fromGO) {
									action += (owner) => {
										data.odinInstanceData.references[index] = owner.gameObject;
									};
								}
							}
						}
						//if(data.HasUnityReference(from)) {
						//	var act = data.GetActionForRefactorUnityObject(from);
						//	action += (owner) => {
						//		act(new Object[] { owner, owner.transform, owner.gameObject });
						//	};
						//}
						//return true;
					}
					return false;
				}, (instance, field, type, value) => {
					Object o = value as Object;
					if(o) {
						if(o == graph) {
							action += (owner) => {
								field.SetValueOptimized(instance, owner);
							};
						} else if(o == fromTR) {
							action += (owner) => {
								field.SetValueOptimized(instance, owner.transform);
							};
						} else if(o == fromGO) {
							action += (owner) => {
								field.SetValueOptimized(instance, owner.gameObject);
							};
						}
					}
				});
			}
			return action;
		}

		/// <summary>
		/// Retarget node owner.
		/// </summary>
		/// <param name="fromOwner"></param>
		/// <param name="toOwner"></param>
		/// <param name="nodes"></param>
		public static void RetargetNodeOwner(uNodeRoot fromOwner, uNodeRoot toOwner, IList<MonoBehaviour> nodes, Action<object> valueAction = null) {
			Object[] from = new Object[] { fromOwner, fromOwner.transform, fromOwner.gameObject };
			Object[] to = new Object[] { toOwner, toOwner.transform, toOwner.gameObject };
			foreach(var behavior in nodes) {
				if(behavior is Node) {
					Node node = behavior as Node;
					node.owner = toOwner;
				} else if(behavior is RootObject) {
					RootObject root = behavior as RootObject;
					root.owner = toOwner;
				}
				AnalizeObject(behavior, (obj) => {
					if(valueAction != null) {
						valueAction(obj);
					}
					if(obj is MemberData) {
						MemberData data = obj as MemberData;
						data.RefactorUnityObject(from, to);
						//return true;
					}
					return false;
				}, (instance, field, type, value) => {
					Object o = value as Object;
					if(o) {
						if(o == fromOwner) {
							field.SetValueOptimized(instance, toOwner);
						} else if(o == from[1]) {
							field.SetValueOptimized(instance, to[1]);
						} else if(o == from[2]) {
							field.SetValueOptimized(instance, to[2]);
						}
					}
				});
			}
		}
    }
}