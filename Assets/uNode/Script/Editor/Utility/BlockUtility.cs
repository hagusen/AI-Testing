using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using MaxyGames.Events;
using Object = UnityEngine.Object;

namespace MaxyGames.uNode.Editors {
	public static class BlockUtility {
		public static List<EventActionData> GetActionBlockFromNode(Node node) {
			if(node is NodeSetValue) {
				var source = node as NodeSetValue;
				var action = new SetValue();
				action.setType = source.setType;
				action.target = new MemberData(source.target);
				action.value = new MemberData(source.value);
				return new List<EventActionData>() { action };
			} else if(node is MultipurposeNode) {
				var source = node as MultipurposeNode;
				if(source.IsFlowNode()) {
					var action = new GetValue();
					action.target = new MultipurposeMember(source.target);
					return new List<EventActionData>() { action };
				}
			}
			return new List<EventActionData>();
		}

		public static List<EventActionData> GetConditionBlockFromNode(Node node) {
			if (node is NodeSetValue) {
				var source = node as NodeSetValue;
				var action = new SetValue();
				action.target = new MemberData(source.target);
				action.value = new MemberData(source.value);
				return new List<EventActionData>() { action };
			} else if (node is MultipurposeNode) {
				var source = node as MultipurposeNode;
				if (source.CanGetValue() && source.ReturnType() == typeof(bool)) {
					var action = new MethodValidation();
					action.target = new MultipurposeMember(source.target);
					return new List<EventActionData>() { action };
				}
			} else if(node is Nodes.ComparisonNode) {
				var source = node as Nodes.ComparisonNode;
				var action = new ObjectCompare();
				action.targetA = new MultipurposeMember(source.targetA);
				action.targetB = new MultipurposeMember(source.targetB);
				action.operatorType = source.operatorType;
				return new List<EventActionData>() { action };
			} else if(node is Nodes.MultiANDNode) {
				var source = node as Nodes.MultiANDNode;
				var result = new List<EventActionData>();
				foreach(var target in source.targets) {
					var action = new EqualityCompare();
					action.target = new MultipurposeMember(target);
					action.value = new MemberData(true);
					result.Add(action);
				}
				return result;
			} else if(node is Nodes.MultiORNode) {
				var source = node as Nodes.MultiORNode;
				var result = new List<EventActionData>();
				foreach(var target in source.targets) {
					if(result.Count > 0) {
						result.Add(EventActionData.OrEvent);
					}
					var action = new EqualityCompare();
					action.target = new MultipurposeMember(target);
					action.value = new MemberData(true);
					result.Add(action);
				}
				return result;
			} else if(node is Nodes.NotNode) {
				var source = node as Nodes.NotNode;
				var action = new EqualityCompare();
				action.target = new MultipurposeMember(source.target);
				MemberDataUtility.UpdateMultipurposeMember(action.target);
				action.value = new MemberData(false);
				return new List<EventActionData>() { action };
			}
			return new List<EventActionData>();
		}

		private static List<BlockMenuAttribute> _actionMenus;
		public static List<BlockMenuAttribute> GetActionMenus(bool acceptCoroutine = false) {
			if(_actionMenus != null) {
				return _actionMenus;
			}
			_actionMenus = new List<BlockMenuAttribute>();
			var menus = EventDataDrawer.FindAllMenu();
			for(int i = 0; i < menus.Count; i++) {
				if(!acceptCoroutine && menus[i].isCoroutine || menus[i].hideOnBlock)
					continue;
				if(menus[i].type.IsCastableTo(typeof(Events.Action)) || 
					menus[i].type.IsCastableTo(typeof(AnyBlock)) ||
					menus[i].type.IsCastableTo(typeof(IFlowNode)) ||
					menus[i].type.IsCastableTo(typeof(IStateNode)) ||
					 menus[i].type.IsCastableTo(typeof(ICoroutineNode)) ||
					 menus[i].type.IsCastableTo(typeof(IStateCoroutineNode))) {
					_actionMenus.Add(menus[i]);
				}
			}
			return _actionMenus;
		}

		private static List<BlockMenuAttribute> _conditionMenus;
		public static List<BlockMenuAttribute> GetConditionMenus() {
			if(_conditionMenus != null) {
				return _conditionMenus;
			}
			_conditionMenus = new List<BlockMenuAttribute>();
			var menus = EventDataDrawer.FindAllMenu();
			for(int i = 0; i < menus.Count; i++) {
				if(menus[i].hideOnBlock)
					continue;
				if(menus[i].type.IsCastableTo(typeof(Condition)) ||
					menus[i].type.IsCastableTo(typeof(IDataNode<bool>))) {
					_conditionMenus.Add(menus[i]);
				}
			}
			return _conditionMenus;
		}

		public static Condition onAddCompareValue(MemberData member) {
			MethodValidation cond = new MethodValidation();
			cond.target.target = member;
			MemberDataUtility.UpdateMultipurposeMember(cond.target);
			return cond;
		}

		public static Condition onAddEqualityComparer(MemberData member) {
			EqualityCompare cond = new EqualityCompare();
			cond.target = new MultipurposeMember() { target = member };
			MemberDataUtility.UpdateMultipurposeMember(cond.target);
			if (member.type != null && ReflectionUtils.CanCreateInstance(member.type)) {
				cond.value = new MemberData(ReflectionUtils.CreateInstance(member.type));
			} else {
				cond.value = new MemberData(null, MemberData.TargetType.Values);
			}
			return cond;
		}

		public static Condition onAddIsComparer(MemberData member) {
			IsCompare cond = new IsCompare();
			cond.target = new MultipurposeMember() { target = member };
			MemberDataUtility.UpdateMultipurposeMember(cond.target);
			return cond;
		}

		public static Events.Action onAddSetAction(MemberData member) {
			SetValue action = new SetValue();
			action.target = member;
			if (member.type != null && ReflectionUtils.CanCreateInstance(member.type)) {
				action.value = new MemberData(ReflectionUtils.CreateInstance(member.type));
			} else {
				action.value = new MemberData(null, MemberData.TargetType.Values);
			}
			return action;
		}

		public static Events.Action onAddGetAction(MemberData member) {
			GetValue action = new GetValue();
			action.target.target = member;
			MemberDataUtility.UpdateMultipurposeMember(action.target);
			return action;
		}

		public static Events.Action onAddCompareAction(MemberData member) {
			CompareOperator action = new CompareOperator();
			action.targetA = member;
			if (member.type != null && ReflectionUtils.CanCreateInstance(member.type)) {
				action.targetB = new MemberData(ReflectionUtils.CreateInstance(member.type));
			} else {
				action.targetB = new MemberData(null, MemberData.TargetType.Values);
			}
			return action;
		}

		public static void ShowAddActionMenu(Vector2 position, Action<Events.Action> action, UnityEngine.Object instance, bool acceptCoroutine = false) {
			List<ItemSelector.CustomItem> customItems = new List<ItemSelector.CustomItem>();
			var actions = GetActionMenus(acceptCoroutine);
			foreach(var a in actions) {
				var type = a.type;
				customItems.Add(new ItemSelector.CustomItem(a.name,
					delegate () {
						Events.Action act;
						if(type.IsSubclassOf(typeof(Events.Action))) {
							act = ReflectionUtils.CreateInstance(type) as Events.Action;
						} else {
							act = new HLAction() {
								type = MemberData.CreateFromType(type)
							};
						}
						action(act);
					}, a.category));
			}
			ItemSelector.SortCustomItems(customItems);
			FilterAttribute filter = new FilterAttribute() {
				InvalidTargetType = MemberData.TargetType.Values |
									MemberData.TargetType.Null |
									//MemberData.TargetType.Constructor |
									MemberData.TargetType.SelfTarget,
				MaxMethodParam = int.MaxValue,
				Instance = true,
				VoidType = true
			};
			ItemSelector w = ItemSelector.ShowWindow(instance, filter, (member) => {
				if(!(member.instance is UnityEngine.Object) && instance != null && !member.isStatic) {
					Type startType = member.startType;
					if(startType != null && member.instance == null) {
						if(instance.GetType().IsCastableTo(startType)) {
							member.instance = instance;
						} else if(member.IsTargetingUNode) {
							if(member.instance == null)
								member.instance = instance;
						} else if(instance is Component) {
							if(startType == typeof(GameObject)) {
								member.instance = (instance as Component).gameObject;
							} else if(startType.IsSubclassOf(typeof(Component))) {
								member.instance = (instance as Component).GetComponent(startType);
							}
						} else if(instance is GameObject) {
							if(startType == typeof(GameObject)) {
								member.instance = instance as GameObject;
							} else if(startType.IsSubclassOf(typeof(Component))) {
								member.instance = (instance as GameObject).GetComponent(startType);
							}
						}
						if(member.instance == null && ReflectionUtils.CanCreateInstance(startType)) {
							member.instance = ReflectionUtils.CreateInstance(startType);
						}
					}
					if(member.instance == null) {
						member.instance = MemberData.none;
					}
				}
				switch(member.targetType) {
					case MemberData.TargetType.uNodeFunction:
					case MemberData.TargetType.Constructor:
					case MemberData.TargetType.Method: {
						if(member.type != typeof(void)) {
							GenericMenu menu = new GenericMenu();
							menu.AddItem(new GUIContent("Invoke"), false, () => {
								action(onAddGetAction(member));
							});
							menu.AddItem(new GUIContent("Compare"), false, () => {
								action(onAddCompareAction(member));
							});
							menu.ShowAsContext();
						} else {
							action(onAddGetAction(member));
						}
						break;
					}
					case MemberData.TargetType.Field:
					case MemberData.TargetType.Property:
					case MemberData.TargetType.uNodeProperty:
					case MemberData.TargetType.uNodeVariable:
					case MemberData.TargetType.uNodeLocalVariable:
					case MemberData.TargetType.uNodeGroupVariable:
					case MemberData.TargetType.uNodeParameter: {
						GenericMenu menu = new GenericMenu();
						var members = member.GetMembers();
						if(members == null || members[members.Length - 1] == null || ReflectionUtils.CanGetMember(members[members.Length - 1])) {
							menu.AddItem(new GUIContent("Get"), false, () => {
								action(onAddGetAction(member));
							});
						}
						if(members == null || members[members.Length - 1] == null || ReflectionUtils.CanSetMember(members[members.Length - 1])) {
							menu.AddItem(new GUIContent("Set"), false, () => {
								action(onAddSetAction(member));
							});
						}
						menu.AddItem(new GUIContent("Compare"), false, () => {
							action(onAddCompareAction(member));
						});
						menu.ShowAsContext();
						break;
					}
					default:
						throw new Exception("Unsupported target kind:" + member.targetType);
				}
			}, false, customItems).ChangePosition(position);
			w.displayNoneOption = false;
			w.displayCustomVariable = false;
			w.customItems = customItems;
		}

        public static void ShowAddEventMenu(
			Vector2 position,
			Object instance,
			Action<Block> addConditionEvent = null) {
			List<ItemSelector.CustomItem> customItems = new List<ItemSelector.CustomItem>();
			var conditions = GetConditionMenus();
			foreach(var c in conditions) {
				var type = c.type;
				customItems.Add(new ItemSelector.CustomItem(c.name,
					delegate () {
						if (addConditionEvent != null) {
							Block act;
							if (type.IsSubclassOf(typeof(Block))) {
								act = ReflectionUtils.CreateInstance(type) as Block;
							} else if (type.IsCastableTo(typeof(IDataNode<bool>))) {
								act = new HLCondition() {
									type = MemberData.CreateFromType(type)
								};
							} else {
								throw new Exception("The type must inherith from Block or IDataNode<bool>");
							}
							addConditionEvent(act);
						}
					}, c.category));
			}
			ItemSelector.SortCustomItems(customItems);
			FilterAttribute filter = new FilterAttribute() {
				InvalidTargetType = MemberData.TargetType.Values | MemberData.TargetType.Null,
				MaxMethodParam = int.MaxValue,
				Instance = true,
				VoidType = false,
			};
			ItemSelector w = ItemSelector.ShowWindow(instance, filter, (member) => {
				if(instance != null && !member.isStatic) {
					Type startType = member.startType;
					if(startType != null && member.instance == null) {
						if(instance.GetType().IsCastableTo(startType)) {
							member.instance = instance;
						} else if(member.IsTargetingUNode) {
							if(member.instance == null)
								member.instance = instance;
						} else if(instance is Component) {
							if(startType == typeof(GameObject)) {
								member.instance = (instance as Component).gameObject;
							} else if(startType.IsSubclassOf(typeof(Component))) {
								member.instance = (instance as Component).GetComponent(startType);
							}
						} else if(instance is GameObject) {
							if(startType == typeof(GameObject)) {
								member.instance = instance as GameObject;
							} else if(startType.IsSubclassOf(typeof(Component))) {
								member.instance = (instance as GameObject).GetComponent(startType);
							}
						}
						if(member.instance == null && ReflectionUtils.CanCreateInstance(startType)) {
							member.instance = ReflectionUtils.CreateInstance(startType);
						}
					}
					if(member.instance == null) {
						member.instance = MemberData.none;
					}
				}
				Condition condition = null;
				System.Action addCondition = () => {
                    if(addConditionEvent != null) {
						addConditionEvent(condition);
					}
				};
				switch(member.targetType) {
					case MemberData.TargetType.Constructor:
					case MemberData.TargetType.Method: {
						GenericMenu menu = new GenericMenu();
						if(member.type == typeof(bool)) {
							menu.AddItem(new GUIContent("Compare Return Value"), false, () => {
								condition = onAddCompareValue(member);
								addCondition();
							});
						}
						menu.AddItem(new GUIContent("Equality Compare"), false, () => {
							condition = onAddEqualityComparer(member);
							addCondition();
						});
						menu.AddItem(new GUIContent("Is Compare"), false, () => {
							condition = onAddIsComparer(member);
							addCondition();
						});
						menu.ShowAsContext();
						break;
					}
					case MemberData.TargetType.Field:
					case MemberData.TargetType.Property:
					case MemberData.TargetType.uNodeVariable:
					case MemberData.TargetType.uNodeProperty:
					case MemberData.TargetType.uNodeLocalVariable:
					case MemberData.TargetType.uNodeGroupVariable: {
						GenericMenu menu = new GenericMenu();
						if(member.type == typeof(bool)) {
							menu.AddItem(new GUIContent("Compare Return Value"), false, () => {
								condition = onAddCompareValue(member);
								addCondition();
							});
						}
						menu.AddItem(new GUIContent("Equality Compare"), false, () => {
							condition = onAddEqualityComparer(member);
							addCondition();
						});
						menu.AddItem(new GUIContent("Is Compare"), false, () => {
							condition = onAddIsComparer(member);
							addCondition();
						});
						menu.ShowAsContext();
						break;
					}
					default:
						condition = onAddCompareValue(member);
						addCondition();
						break;
				}
			}, false, customItems).ChangePosition(position);
			w.displayNoneOption = false;
			w.displayCustomVariable = false;
			w.customItems = customItems;
		}
	}
}