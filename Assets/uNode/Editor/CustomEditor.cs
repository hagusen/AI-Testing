using UnityEngine;
using UnityEditor;
using System;
using System.Linq;
using System.Collections.Generic;
using MaxyGames.uNode.Nodes;

namespace MaxyGames.uNode.Editors {
	#region Property Drawer
	[CustomPropertyDrawer(typeof(MultipurposeMember))]
	class MultipurposeMemberDrawer : PropertyDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			if (fieldInfo.IsDefined(typeof(TooltipAttribute), true)) {
				label.tooltip = ((TooltipAttribute)fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), true)[0]).tooltip;
			}
			EditorGUI.BeginProperty(position, label, property);
			VariableEditorUtility.DrawMultipurposeMember(position, property, label);
			EditorGUI.EndProperty();
		}
	}

	[CustomPropertyDrawer(typeof(FieldModifier))]
	class FieldModifierDrawer : PropertyDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			if (fieldInfo.IsDefined(typeof(TooltipAttribute), true)) {
				label.tooltip = ((TooltipAttribute)fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), true)[0]).tooltip;
			}
			var value = PropertyDrawerUtility.GetActualObjectForSerializedProperty<FieldModifier>(property);
			EditorGUI.BeginProperty(position, label, property);
			uNodeGUIUtility.EditValue(position, label, value, null, new uNodeUtility.EditValueSettings() {
				unityObject = property.serializedObject.targetObject,
			});
			VariableEditorUtility.DrawMultipurposeMember(position, property, label);
			EditorGUI.EndProperty();
		}
	}

	[CustomPropertyDrawer(typeof(PropertyModifier))]
	class PropertyModifierDrawer : PropertyDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			if (fieldInfo.IsDefined(typeof(TooltipAttribute), true)) {
				label.tooltip = ((TooltipAttribute)fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), true)[0]).tooltip;
			}
			var value = PropertyDrawerUtility.GetActualObjectForSerializedProperty<PropertyModifier>(property);
			EditorGUI.BeginProperty(position, label, property);
			uNodeGUIUtility.EditValue(position, label, value, null, new uNodeUtility.EditValueSettings() {
				unityObject = property.serializedObject.targetObject,
			});
			VariableEditorUtility.DrawMultipurposeMember(position, property, label);
			EditorGUI.EndProperty();
		}
	}

	[CustomPropertyDrawer(typeof(FunctionModifier))]
	class FunctionModifierDrawer : PropertyDrawer {
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			if (fieldInfo.IsDefined(typeof(TooltipAttribute), true)) {
				label.tooltip = ((TooltipAttribute)fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), true)[0]).tooltip;
			}
			var value = PropertyDrawerUtility.GetActualObjectForSerializedProperty<FunctionModifier>(property);
			EditorGUI.BeginProperty(position, label, property);
			uNodeGUIUtility.EditValue(position, label, value, null, new uNodeUtility.EditValueSettings() {
				unityObject = property.serializedObject.targetObject,
			});
			VariableEditorUtility.DrawMultipurposeMember(position, property, label);
			EditorGUI.EndProperty();
		}
	}

	[CustomPropertyDrawer(typeof(MemberData))]
	class MemberDataDrawer : PropertyDrawer {
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			if (fieldInfo.IsDefined(typeof(ObjectTypeAttribute), true)) {
				var fieldAttributes = fieldInfo.GetCustomAttributes(true);
				object variable = PropertyDrawerUtility.GetParentObjectFromSerializedProperty<object>(property);
				if (variable != null && ReflectionUtils.TryCorrectingAttribute(variable, ref fieldAttributes)) {
					var OTA = ReflectionUtils.GetAttribute<ObjectTypeAttribute>(fieldAttributes);
					if (OTA != null && OTA.type != null) {
						return base.GetPropertyHeight(property, label);
					}
				}
				return 0;
			}
			return base.GetPropertyHeight(property, label);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			MemberData variable = PropertyDrawerUtility.GetActualObjectForSerializedProperty<MemberData>(property);
			FilterAttribute filter = null;
			if (fieldInfo.IsDefined(typeof(ObjectTypeAttribute), true)) {
				object pVariable = PropertyDrawerUtility.GetParentObjectFromSerializedProperty<object>(property);
				var fieldAttributes = fieldInfo.GetCustomAttributes(true);
				if (pVariable != null && ReflectionUtils.TryCorrectingAttribute(pVariable, ref fieldAttributes)) {
					filter = ReflectionUtils.GetAttribute<FilterAttribute>(fieldAttributes);
				} else
					return;
			} else if (fieldInfo.GetCustomAttributes(typeof(FilterAttribute), false).Length > 0) {
				filter = (FilterAttribute)fieldInfo.GetCustomAttributes(typeof(FilterAttribute), false)[0];
			}
			if (fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), false).Length > 0) {
				label.tooltip = ((TooltipAttribute)fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), false)[0]).tooltip;
			}
			EditorReflectionUtility.RenderVariable(position, variable, label, property.serializedObject.targetObject, filter);
		}
	}

	[CustomPropertyDrawer(typeof(HideAttribute))]
	class HideAttributeDrawer : PropertyDrawer {
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			object variable = PropertyDrawerUtility.GetParentObjectFromSerializedProperty<object>(property);
			if (variable != null) {
				if (uNodeGUIUtility.IsHide(fieldInfo, variable))
					return -EditorGUIUtility.standardVerticalSpacing;
			} else {
				if (fieldInfo.IsDefined(typeof(HideAttribute), true)) {
					HideAttribute[] hide = fieldInfo.GetCustomAttributes(typeof(HideAttribute), true) as HideAttribute[];
					foreach (HideAttribute ha in hide) {
						if (string.IsNullOrEmpty(ha.targetField)) {
							return -EditorGUIUtility.standardVerticalSpacing;
						}
					}
				}
			}
			System.Type type = fieldInfo.FieldType;
			if (fieldInfo.FieldType.IsArray) {
				type = fieldInfo.FieldType.GetElementType();
			} else if (fieldInfo.FieldType.IsGenericType) {
				System.Type[] gType = fieldInfo.FieldType.GetGenericArguments();
				if (gType.Length == 1) {
					type = gType[0];
				}
			}
			if (type == typeof(MemberData)) {
				if (fieldInfo.IsDefined(typeof(ObjectTypeAttribute), true)) {
					var fieldAttributes = fieldInfo.GetCustomAttributes(true);
					if (variable != null && ReflectionUtils.TryCorrectingAttribute(variable, ref fieldAttributes)) {
						var OTA = ReflectionUtils.GetAttribute<ObjectTypeAttribute>(fieldAttributes);
						if (OTA != null && OTA.type != null) {
							return EditorGUI.GetPropertyHeight(property, label, true);
						}
					}
					return -EditorGUIUtility.standardVerticalSpacing;
				}
			}
			return EditorGUI.GetPropertyHeight(property, label, true);
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			object variable = PropertyDrawerUtility.GetParentObjectFromSerializedProperty<object>(property);
			if (variable != null) {
				if (uNodeGUIUtility.IsHide(fieldInfo, variable))
					return;
			} else {
				if (fieldInfo.IsDefined(typeof(HideAttribute), true)) {
					HideAttribute[] hide = fieldInfo.GetCustomAttributes(typeof(HideAttribute), true) as HideAttribute[];
					foreach (HideAttribute ha in hide) {
						if (string.IsNullOrEmpty(ha.targetField)) {
							return;
						}
					}
				}
			}
			if (fieldInfo.IsDefined(typeof(TooltipAttribute), true)) {
				label.tooltip = ((TooltipAttribute)fieldInfo.GetCustomAttributes(typeof(TooltipAttribute), true)[0]).tooltip;
			}
			System.Type type = fieldInfo.FieldType;
			if (fieldInfo.FieldType.IsArray) {
				type = fieldInfo.FieldType.GetElementType();
			} else if (fieldInfo.FieldType.IsGenericType) {
				System.Type[] gType = fieldInfo.FieldType.GetGenericArguments();
				if (gType.Length == 1) {
					type = gType[0];
				}
			}
			if (type == typeof(MemberData)) {
				MemberData obj = PropertyDrawerUtility.GetActualObjectForSerializedProperty<MemberData>(property);
				FilterAttribute filter = null;
				if (fieldInfo.GetCustomAttributes(typeof(FilterAttribute), false).Length > 0) {
					filter = (FilterAttribute)fieldInfo.GetCustomAttributes(typeof(FilterAttribute), false)[0];
				} else if (fieldInfo.IsDefined(typeof(ObjectTypeAttribute), true)) {
					var fieldAttributes = fieldInfo.GetCustomAttributes(true);
					object pVariable = PropertyDrawerUtility.GetParentObjectFromSerializedProperty<object>(property);
					if (pVariable != null && ReflectionUtils.TryCorrectingAttribute(pVariable, ref fieldAttributes)) {
						filter = ReflectionUtils.GetAttribute<FilterAttribute>(fieldAttributes);
					} else
						return;
				}
				EditorReflectionUtility.RenderVariable(position, obj, label, property.serializedObject.targetObject, filter);
			} else {
				EditorGUI.PropertyField(position, property, label, true);
			}
		}
	}
	#endregion

	[CustomEditor(typeof(uNodeResourceDatabase), true)]
	class uNodeResourceDatabaseEditor : Editor {
		public override void OnInspectorGUI() {
			var comp = target as uNodeResourceDatabase;
			DrawDefaultInspector();
			if(GUILayout.Button(new GUIContent("Update Database", ""))) {
				var graphs = uNodeEditorUtility.FindComponentInPrefabs<uNodeRoot>();
				foreach(var root in graphs) {
					if(comp.graphDatabases.Any(g => g.graph == root)) {
						continue;
					}
					comp.graphDatabases.Add(new uNodeResourceDatabase.RuntimeGraphDatabase() {
						graph = root,
					});
					EditorUtility.SetDirty(comp);
				}
			}
		}
	}

	[CustomEditor(typeof(uNodeData), true)]
	class uNodeDataEditor : Editor {
		public override void OnInspectorGUI() {
			uNodeData comp = target as uNodeData;
			EditorGUI.BeginChangeCheck();
			base.OnInspectorGUI();
			EditorGUI.BeginDisabledGroup(uNodeEditorUtility.IsPrefab(comp));
			VariableEditorUtility.DrawNamespace("Using Namespaces", comp.generatorSettings.usingNamespace.ToList(), comp, (arr) => {
				comp.generatorSettings.usingNamespace = arr.ToArray();
				uNodeEditorUtility.MarkDirty(comp);
			});
			EditorGUI.EndDisabledGroup();
			if (EditorGUI.EndChangeCheck()) {
				uNodeGUIUtility.GUIChanged(comp);
			}
			if(comp.GetComponent<uNodeRoot>() == null) {
				if (GUILayout.Button(new GUIContent("Open uNode Editor", "Open uNode Editor to edit this uNode"), EditorStyles.toolbarButton)) {
					uNodeEditor.ChangeTarget(comp, true);
				}
			}
		}
	}

	[CustomEditor(typeof(NodeComponent), true)]
	class NodeComponentEditor : Editor {
		public override void OnInspectorGUI() {
			base.OnInspectorGUI();
			NodeComponent comp = target as NodeComponent;
			if (comp == null)
				return;
			EditorGUI.BeginChangeCheck();
			if (comp is MacroNode) {
				MacroNode node = comp as MacroNode;
				VariableEditorUtility.DrawVariable(node.variables, node,
					(v) => {
						node.variables = v;
					}, null);

				VariableEditorUtility.DrawCustomList(
					node.inputFlows,
					"Input Flows",
					(position, index, element) => {//Draw Element
						EditorGUI.LabelField(position, new GUIContent(element.GetName(), uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))));
					}, null, null);
				VariableEditorUtility.DrawCustomList(
					node.outputFlows,
					"Output Flows",
					(position, index, element) => {//Draw Element
						EditorGUI.LabelField(position, new GUIContent(element.GetName(), uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))));
					}, null, null);
				VariableEditorUtility.DrawCustomList(
					node.inputValues,
					"Input Values",
					(position, index, element) => {//Draw Element
						position.width -= EditorGUIUtility.labelWidth;
						EditorGUI.LabelField(position, new GUIContent(element.GetName(), uNodeEditorUtility.GetTypeIcon(element.ReturnType())));
						position.x += EditorGUIUtility.labelWidth;
						uNodeGUIUtility.EditValue(position, GUIContent.none, "type", element, element);
					}, null, null);
				VariableEditorUtility.DrawCustomList(
					node.outputValues,
					"Output Values",
					(position, index, element) => {//Draw Element
						position.width -= EditorGUIUtility.labelWidth;
						EditorGUI.LabelField(position, new GUIContent(element.GetName(), uNodeEditorUtility.GetTypeIcon(element.ReturnType())));
						position.x += EditorGUIUtility.labelWidth;
						uNodeGUIUtility.EditValue(position, GUIContent.none, "type", element, element);
					}, null, null);
			} else if (comp is LinkedMacroNode) {
				LinkedMacroNode node = comp as LinkedMacroNode;
				if (node.macroAsset != null) {
					VariableEditorUtility.DrawLinkedVariables(node, node.macroAsset, publicOnly: false);
					EditorGUI.BeginDisabledGroup(true);
					VariableEditorUtility.DrawCustomList(
						node.inputFlows,
						"Input Flows",
						(position, index, element) => {//Draw Element
							EditorGUI.LabelField(position, new GUIContent(element.GetName(), uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))));
						}, null, null);
					VariableEditorUtility.DrawCustomList(
						node.outputFlows,
						"Output Flows",
						(position, index, element) => {//Draw Element
							EditorGUI.LabelField(position, new GUIContent(element.GetName(), uNodeEditorUtility.GetTypeIcon(typeof(TypeIcons.FlowIcon))));
						}, null, null);
					VariableEditorUtility.DrawCustomList(
						node.inputValues,
						"Input Values",
						(position, index, element) => {//Draw Element
							position.width -= EditorGUIUtility.labelWidth;
							EditorGUI.LabelField(position, new GUIContent(element.GetName(), uNodeEditorUtility.GetTypeIcon(element.ReturnType())));
							position.x += EditorGUIUtility.labelWidth;
							uNodeGUIUtility.EditValue(position, GUIContent.none, "type", element, element);
						}, null, null);
					VariableEditorUtility.DrawCustomList(
						node.outputValues,
						"Output Values",
						(position, index, element) => {//Draw Element
							position.width -= EditorGUIUtility.labelWidth;
							EditorGUI.LabelField(position, new GUIContent(element.GetName(), uNodeEditorUtility.GetTypeIcon(element.ReturnType())));
							position.x += EditorGUIUtility.labelWidth;
							uNodeGUIUtility.EditValue(position, GUIContent.none, "type", element, element);
						}, null, null);
					EditorGUI.EndDisabledGroup();
				}
			}

			uNodeGUIUtility.ShowField("comment", comp, comp);
			if (comp.GetType().GetCustomAttributes(typeof(DescriptionAttribute), false).Length > 0) {
				DescriptionAttribute descriptionEvent = (DescriptionAttribute)comp.GetType().GetCustomAttributes(typeof(DescriptionAttribute), false)[0];
				if (descriptionEvent.description != null && descriptionEvent != null) {
					GUI.backgroundColor = Color.yellow;
					EditorGUILayout.HelpBox("Description: " + descriptionEvent.description, MessageType.None);
					GUI.backgroundColor = Color.white;
				}
			}
			if(EditorGUI.EndChangeCheck()) {
				uNodeEditorUtility.MarkDirty(comp);
				uNodeGUIUtility.GUIChanged(comp);
			}
		}
	}

	[CustomEditor(typeof(uNodeRoot), true)]
	class uNodeRootEditor : Editor {
		public override void OnInspectorGUI() {
			uNodeRoot root = target as uNodeRoot;
			EditorGUI.BeginDisabledGroup(uNodeEditorUtility.IsPrefab(root));
			EditorGUI.BeginChangeCheck();
			CustomInspector.DrawGraphInspector(root);
			if (EditorGUI.EndChangeCheck()) {
				uNodeEditor.GUIChanged();
			}
			EditorGUI.EndDisabledGroup();
			if (uNodeEditorUtility.IsPrefab(root)) {
				if(root is uNodeRuntime) {
					EditorGUILayout.HelpBox("Open Prefab to Edit Graph", MessageType.Info);
				} else {
					if(GUILayout.Button(new GUIContent("Open uNode Editor", "Open uNode Editor to edit this uNode"), EditorStyles.toolbarButton)) {
						uNodeEditor.ChangeTarget(target as uNodeRoot, true);
					}
					EditorGUILayout.HelpBox("Open uNode Editor to Edit values", MessageType.Info);
				}
			} else {
				if(GUILayout.Button(new GUIContent("Open uNode Editor", "Open uNode Editor to edit this uNode"), EditorStyles.toolbarButton)) {
					uNodeEditor.ChangeTarget(target as uNodeRoot, true);
				}
			}
			if (!Application.isPlaying && (root is uNodeRuntime || root is ISingletonGraph)) {
				var type = root.GeneratedTypeName.ToType(false);
				if (type != null) {
					EditorGUILayout.HelpBox("Run using Native C#", MessageType.Info);
				} else {
					EditorGUILayout.HelpBox("Run using Reflection", MessageType.Info);
				}
			} else if(Application.isPlaying && root is uNodeComponentSingleton singleton && (singleton.runtimeBehaviour != null || singleton.runtimeInstance != null)) {
				EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Runtime Component");
				if (singleton.runtimeBehaviour == null) {
					uNodeGUIUtility.DrawVariablesInspector(singleton.runtimeInstance.Variables, singleton.runtimeInstance, null);
				} else if(singleton.runtimeBehaviour != null) {
					Editor editor = Editor.CreateEditor(singleton.runtimeBehaviour);
					if(editor != null) {
						editor.OnInspectorGUI();
					} else {
						uNodeGUIUtility.ShowFields(singleton.runtimeBehaviour, singleton.runtimeBehaviour);
					}
				}
			}
			if(!Application.isPlaying && root is IIndependentGraph) {
				var system = GraphUtility.GetGraphSystem(root);
				if(system != null && system.allowAutoCompile && uNodeEditorUtility.IsPrefab(root)) {
					var actualGraph = root;
					if(GraphUtility.HasTempGraphObject(root.gameObject)) {
						actualGraph = GraphUtility.GetTempGraphObject(root);
					}
					uNodeGUIUtility.ShowField(new GUIContent("Compile to C#", "If true, the graph will be compiled to C# to run using native c# performance on build or in editor using ( Generate C# Scripts ) menu."), nameof(root.graphData.compileToScript), actualGraph.graphData, actualGraph);
				}
			}
		}
	}

	[CustomEditor(typeof(GraphAsset), true)]
	class GraphAssetEditor : Editor {
		public override void OnInspectorGUI() {
			GraphAsset asset = target as GraphAsset;
			DrawDefaultInspector();
			EditorGUILayout.HelpBox("The GraphAsset is not supported anymore", MessageType.Warning);
		}
	}

	[CustomEditor(typeof(uNodeInterface), true)]
	class GraphInterfaceEditor : Editor {
		public override void OnInspectorGUI() {
			uNodeInterface asset = target as uNodeInterface;
			DrawDefaultInspector();
			EditorGUI.BeginChangeCheck();
			VariableEditorUtility.DrawNamespace("Using Namespaces", asset.usingNamespaces, asset, (ns) => {
				asset.usingNamespaces = ns as List<string> ?? ns.ToList();
			});
			VariableEditorUtility.DrawInterfaceFunction(asset.functions, asset, (val) => {
				asset.functions = val.ToArray();
			});
			VariableEditorUtility.DrawInterfaceProperty(asset.properties, asset, (val) => {
				asset.properties = val.ToArray();
			});
			if(EditorGUI.EndChangeCheck()) {
				var runtimeType = ReflectionUtils.GetRuntimeType(asset) as RuntimeGraphInterface;
				if(runtimeType != null) {
					runtimeType.RebuildMembers();
				}
			}
		}
	}
	
	[CustomEditor(typeof(uNodeSpawner), true)]
	class uNodeSpawnerEditor : Editor {
		public override void OnInspectorGUI() {
			uNodeSpawner root = target as uNodeSpawner;
			serializedObject.UpdateIfRequiredOrScript();
			var position = uNodeGUIUtility.GetRect();
			var bPos = position;
			bPos.x += position.width - 20;
			bPos.width = 20;
			if (GUI.Button(bPos, "", EditorStyles.label)) {
				var items = ItemSelector.MakeCustomItemsForInstancedType(new System.Type[] { typeof(IGraphWithUnityEvent), typeof(IClassComponent) }, (val) => {
					serializedObject.FindProperty(nameof(root.target)).objectReferenceValue = val as uNodeRoot;
					serializedObject.ApplyModifiedProperties();
				}, uNodeEditorUtility.IsSceneObject(root));
				ItemSelector.ShowWindow(null, null, null, null, items).ChangePosition(bPos.ToScreenRect()).displayDefaultItem = false;
				Event.current.Use();
			}
			EditorGUI.PropertyField(position, serializedObject.FindProperty(nameof(root.target)), new GUIContent("Graph", "The target graph reference"));
			// EditorGUILayout.PropertyField(serializedObject.FindProperty(nameof(root.mainObject)));
			serializedObject.ApplyModifiedProperties();
			if (root.target != null) {
				if (!Application.isPlaying || root.runtimeBehaviour == null) {
					EditorGUI.BeginChangeCheck();
					VariableEditorUtility.DrawLinkedVariables(root, root.target, null);
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.MarkDirty(root);
					}
				} else if(root.runtimeBehaviour != null) {
					Editor editor = Editor.CreateEditor(root.runtimeBehaviour);
					if(editor != null) {
						EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Runtime Component");
						editor.OnInspectorGUI();
					} else {
						uNodeGUIUtility.ShowFields(root.runtimeBehaviour, root.runtimeBehaviour);
					}
				}
				if (root.target is IGraphWithUnityEvent || root.target is IClassComponent) {
					if (!Application.isPlaying) {
						var type = root.target.GeneratedTypeName.ToType(false);
						if (type != null) {
							EditorGUILayout.HelpBox("Run using Native C#", MessageType.Info);
						} else {
							EditorGUILayout.HelpBox("Run using Reflection", MessageType.Info);
						}
					}
				} else {
					EditorGUILayout.HelpBox("The target graph is not supported.", MessageType.Warning);
				}
				if (!Application.isPlaying || root.runtimeBehaviour == null) {
					EditorGUILayout.BeginHorizontal();
					if (GUILayout.Button(new GUIContent("Edit Target", ""), EditorStyles.toolbarButton)) {
						uNodeEditor.ChangeTarget(root.target, !(root.target is uNodeRuntime));
					}
					if (Application.isPlaying && root.runtimeInstance != null) {
						if (GUILayout.Button(new GUIContent("Debug Target", ""), EditorStyles.toolbarButton)) {
							uNodeEditor.ChangeTarget(root.runtimeInstance, false);
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			} else {
				EditorGUILayout.HelpBox("Please assign the target graph", MessageType.Error);
			}
		}
	}

	[CustomEditor(typeof(uNodeAssetInstance), true)]
	class uNodeAssetInstanceEditor : Editor {
		public override void OnInspectorGUI() {
			uNodeAssetInstance script = target as uNodeAssetInstance;
			serializedObject.UpdateIfRequiredOrScript();
			var position = uNodeGUIUtility.GetRect();
			var bPos = position;
			bPos.x += position.width - 20;
			bPos.width = 20;
			if (GUI.Button(bPos, "", EditorStyles.label)) {
				var items = ItemSelector.MakeCustomItemsForInstancedType(new System.Type[] { typeof(uNodeClassAsset) }, (val) => {
					script.target = val as uNodeClassAsset;
					uNodeEditorUtility.MarkDirty(script);
				}, uNodeEditorUtility.IsSceneObject(script));
				ItemSelector.ShowWindow(null, null, null, null, items).ChangePosition(bPos.ToScreenRect()).displayDefaultItem = false;
				Event.current.Use();
			}
			EditorGUI.PropertyField(position, serializedObject.FindProperty(nameof(script.target)), new GUIContent("Graph", "The target graph reference"));
			serializedObject.ApplyModifiedProperties();
			if (script.target != null) {
				if (!Application.isPlaying || script.runtimeAsset == null) {
					EditorGUI.BeginChangeCheck();
					VariableEditorUtility.DrawLinkedVariables(script, script.target, "");
					if(EditorGUI.EndChangeCheck()) {
						uNodeEditorUtility.MarkDirty(script);
					}
				} else if(script.runtimeAsset != null) {
					Editor editor = Editor.CreateEditor(script.runtimeAsset);
					if(editor != null) {
						EditorGUI.DropShadowLabel(uNodeGUIUtility.GetRect(), "Runtime Asset");
						editor.OnInspectorGUI();
					} else {
						uNodeGUIUtility.ShowFields(script.runtimeAsset, script.runtimeAsset);
					}
				}
				if (script.target is IClassAsset) {
					if (!Application.isPlaying) {
						var type = script.target.GeneratedTypeName.ToType(false);
						if (type != null) {
							EditorGUILayout.HelpBox("Run using Native C#", MessageType.Info);
						} else {
							EditorGUILayout.HelpBox("Run using Reflection", MessageType.Info);
						}
					}
				} else {
					EditorGUILayout.HelpBox("The target graph is not supported.", MessageType.Warning);
				}
				if (!Application.isPlaying || script.runtimeAsset == null) {
					EditorGUILayout.BeginHorizontal();
					if (GUILayout.Button(new GUIContent("Edit Target", ""), EditorStyles.toolbarButton)) {
						uNodeEditor.ChangeTarget(script.target, false);
					}
					if (Application.isPlaying && script.runtimeInstance != null) {
						if (GUILayout.Button(new GUIContent("Debug Target", ""), EditorStyles.toolbarButton)) {
							uNodeEditor.ChangeTarget(script.runtimeInstance, false);
						}
					}
					EditorGUILayout.EndHorizontal();
				}
			} else {
				EditorGUILayout.HelpBox("Please assign the target graph", MessageType.Error);
			}
		}
	}

	#region Nodes
	[CustomEditor(typeof(EventNode), true)]
	class EventNodeEditor : Editor {
		public override void OnInspectorGUI() {
			EventNode comp = target as EventNode;
			EditorGUI.BeginChangeCheck();
			uNodeGUIUtility.ShowFields(comp, comp);
			if (comp.GetType().GetCustomAttributes(typeof(DescriptionAttribute), false).Length == 0)
				return;
			DescriptionAttribute descriptionEvent = (DescriptionAttribute)comp.GetType().GetCustomAttributes(typeof(DescriptionAttribute), false)[0];
			if (descriptionEvent.description != null && descriptionEvent != null) {
				GUI.backgroundColor = Color.yellow;
				EditorGUILayout.HelpBox("Description: " + descriptionEvent.description, MessageType.None);
				GUI.backgroundColor = Color.white;
			}
			if (EditorGUI.EndChangeCheck()) {
				uNodeGUIUtility.GUIChanged(comp);
			}
		}
	}

	[CustomEditor(typeof(StateEventNode), true)]
	class StateEventNodeEditor : Editor {
		public override void OnInspectorGUI() {
			StateEventNode comp = target as StateEventNode;
			EditorGUI.BeginChangeCheck();
			DrawDefaultInspector();
			switch (comp.eventType) {
				case StateEventNode.EventType.OnAnimatorIK:
					uNodeGUIUtility.ShowField(
						new GUIContent("Store Parameter"),
						"storeParameter",
						comp,
						new object[] { new FilterAttribute(typeof(int)) { SetMember = true } },
						comp);
					break;
				case StateEventNode.EventType.OnApplicationFocus:
				case StateEventNode.EventType.OnApplicationPause:
					uNodeGUIUtility.ShowField(
						new GUIContent("Store Parameter"),
						"storeParameter",
						comp,
						new object[] { new FilterAttribute(typeof(bool)) { SetMember = true } },
						comp);
					break;
				case StateEventNode.EventType.OnCollisionEnter:
				case StateEventNode.EventType.OnCollisionExit:
				case StateEventNode.EventType.OnCollisionStay:
					uNodeGUIUtility.ShowField(
						new GUIContent("Store Collision"),
						"storeParameter",
						comp,
						new object[] { new FilterAttribute(typeof(Collision)) { SetMember = true } },
						comp);
					break;
				case StateEventNode.EventType.OnCollisionEnter2D:
				case StateEventNode.EventType.OnCollisionExit2D:
				case StateEventNode.EventType.OnCollisionStay2D:
					uNodeGUIUtility.ShowField(
						new GUIContent("Store Collision2D"),
						"storeParameter",
						comp,
						new object[] { new FilterAttribute(typeof(Collision2D)) { SetMember = true } },
						comp);
					break;
				case StateEventNode.EventType.OnTriggerEnter:
				case StateEventNode.EventType.OnTriggerExit:
				case StateEventNode.EventType.OnTriggerStay:
					uNodeGUIUtility.ShowField(
						new GUIContent("Store Collider"),
						"storeParameter",
						comp,
						new object[] { new FilterAttribute(typeof(Collider)) { SetMember = true } },
						comp);
					break;
				case StateEventNode.EventType.OnTriggerEnter2D:
				case StateEventNode.EventType.OnTriggerExit2D:
				case StateEventNode.EventType.OnTriggerStay2D:
					uNodeGUIUtility.ShowField(
						new GUIContent("Store Collider2D"),
						"storeParameter",
						comp,
						new object[] { new FilterAttribute(typeof(Collider2D)) { SetMember = true } },
						comp);
					break;
				default:
					if (comp.storeParameter.isAssigned) {
						comp.storeParameter = MemberData.none;
					}
					break;
			}
			if (EditorGUI.EndChangeCheck()) {
				uNodeGUIUtility.GUIChanged(comp);
			}
		}
	}

	[CustomEditor(typeof(NodeGroup), true)]
	class NodeGroupEditor : Editor {
		public override void OnInspectorGUI() {
			NodeGroup comp = target as NodeGroup;
			EditorGUI.BeginChangeCheck();
			base.OnInspectorGUI();
			if (GUILayout.Button(new GUIContent("Open Variable Editor", "Open Variable Editor to edit this variable"), EditorStyles.toolbarButton)) {
				VariableEditorWindow VEW = VariableEditorWindow.ShowWindow(comp, comp.variable);
				VEW.autoInitializeSupportedType = false;
			}
			uNodeGUIUtility.ShowField("comment", comp, comp);
			if (EditorGUI.EndChangeCheck()) {
				uNodeGUIUtility.GUIChanged(comp);
			}
		}
	}

	[CustomEditor(typeof(MultiArithmeticNode), true)]
	class MultiArithmeticNodeEditor : Editor {
		public override void OnInspectorGUI() {
			MultiArithmeticNode node = target as MultiArithmeticNode;
			DrawDefaultInspector();
			VariableEditorUtility.DrawMembers(new GUIContent("Targets"), node.targets, node, new FilterAttribute(typeof(object)),
				(obj) => {
					node.targets = obj;
				},
				() => {
					uNodeEditorUtility.RegisterUndo(node);
					var type = node.ReturnType();
					if(type != typeof(object) && ReflectionUtils.CanCreateInstance(type)) {
						node.targets.Add(new MemberData(ReflectionUtils.CreateInstance(type)));
					} else if(node.targets.Count > 0){
						node.targets.Add(new MemberData(node.targets[node.targets.Count - 1]));
					} else {
						node.targets.Add(new MemberData());
					}
				});
			if(GUILayout.Button(new GUIContent("Change Operator"))) {
				var customItems = new List<ItemSelector.CustomItem>();
				{//Primitives
					customItems.AddRange(GetCustomItemForPrimitives(node, typeof(int)));
					customItems.AddRange(GetCustomItemForPrimitives(node, typeof(float)));
				}
				var ns = NodeGraph.GetOpenedGraphUsingNamespaces();
				var preference = uNodePreference.GetPreference();
				var assemblies = EditorReflectionUtility.GetAssemblies();
				var includedAssemblies = uNodePreference.GetIncludedAssemblies();
				foreach(var assembly in assemblies) {
					if(!includedAssemblies.Contains(assembly.GetName().Name)) {
						continue;
					}
					var operators = EditorReflectionUtility.GetOperators(assembly, (op) => {
						return ns == null || ns.Contains(op.DeclaringType.Namespace);
					});
					if(operators.Count > 0) {
						foreach(var op in operators) {
							switch(op.Name) {
								case "op_Addition": {
									var parameters = op.GetParameters();
									customItems.Add(GetCustomItem(node, parameters[0].ParameterType, parameters[1].ParameterType, op.DeclaringType, op.ReturnType, ArithmeticType.Add));
									break;
								}
								case "op_Subtraction": {
									var parameters = op.GetParameters();
									customItems.Add(GetCustomItem(node, parameters[0].ParameterType, parameters[1].ParameterType, op.DeclaringType, op.ReturnType, ArithmeticType.Subtract));
									break;
								}
								case "op_Division": {
									var parameters = op.GetParameters();
									customItems.Add(GetCustomItem(node, parameters[0].ParameterType, parameters[1].ParameterType, op.DeclaringType, op.ReturnType, ArithmeticType.Divide));
									break;
								}
								case "op_Multiply": {
									var parameters = op.GetParameters();
									customItems.Add(GetCustomItem(node, parameters[0].ParameterType, parameters[1].ParameterType, op.DeclaringType, op.ReturnType, ArithmeticType.Multiply));
									break;
								}
								case "op_Modulus": {
									var parameters = op.GetParameters();
									customItems.Add(GetCustomItem(node, parameters[0].ParameterType, parameters[1].ParameterType, op.DeclaringType, op.ReturnType, ArithmeticType.Modulo));
									break;
								}
							}
						}
					}
				}
				customItems.Sort((x, y) => {
					if(x.category == y.category) {
						return string.Compare(x.name, y.name);
					}
					return string.Compare(x.category, y.category);
				});
				if(customItems.Count > 0) {
					ItemSelector.ShowWindow(null, null, null, false, customItems).
						ChangePosition(
							GUIUtility.GUIToScreenRect(GUILayoutUtility.GetLastRect())
						).displayDefaultItem = false;
				}
			}
		}

		private static List<ItemSelector.CustomItem> GetCustomItemForPrimitives(MultiArithmeticNode source, Type type) {
			var items = new List<ItemSelector.CustomItem>();
			items.Add(GetCustomItem(source, type, type, type, type, ArithmeticType.Add));
			items.Add(GetCustomItem(source, type, type, type, type, ArithmeticType.Divide));
			items.Add(GetCustomItem(source, type, type, type, type, ArithmeticType.Modulo));
			items.Add(GetCustomItem(source, type, type, type, type, ArithmeticType.Multiply));
			items.Add(GetCustomItem(source, type, type, type, type, ArithmeticType.Subtract));
			return items;
		}

		private static ItemSelector.CustomItem GetCustomItem(MultiArithmeticNode source, Type param1, Type param2, Type declaredType, Type returnType, ArithmeticType operatorType) {
			return new ItemSelector.CustomItem(string.Format(operatorType.ToString() + " ({0}, {1})", param1.PrettyName(), param2.PrettyName()), () => {
				uNodeEditorUtility.RegisterUndo(source);
				source.operatorType = operatorType;
				while(source.targets.Count > 2) {
					source.targets.RemoveAt(source.targets.Count - 1);
				}
				source.targets[0].CopyFrom(new MemberData(ReflectionUtils.CreateInstance(param1)));
				source.targets[1].CopyFrom(new MemberData(ReflectionUtils.CreateInstance(param2)));
				uNodeGUIUtility.GUIChanged(source);
			}, declaredType.PrettyName() + " : Operator") { icon = uNodeEditorUtility.GetTypeIcon(returnType) };
		}
	}

	[CustomEditor(typeof(MultiORNode), true)]
	class MultiORNodeEditor : Editor {
		public override void OnInspectorGUI() {
			Nodes.MultiORNode node = target as MultiORNode;
			DrawDefaultInspector();
			VariableEditorUtility.DrawMembers(new GUIContent("Targets"), node.targets, node, new FilterAttribute(typeof(bool)),
				(obj) => {
					node.targets = obj;
				},
				() => {
					uNodeEditorUtility.RegisterUndo(node);
					node.targets.Add(new MemberData(true));
				});
		}
	}

	[CustomEditor(typeof(MultiANDNode), true)]
	class MultiANDNodeEditor : Editor {
		public override void OnInspectorGUI() {
			MultiANDNode node = target as MultiANDNode;
			DrawDefaultInspector();
			VariableEditorUtility.DrawMembers(new GUIContent("Targets"), node.targets, node, new FilterAttribute(typeof(bool)),
				(obj) => {
					node.targets = obj;
				},
				() => {
					uNodeEditorUtility.RegisterUndo(node);
					node.targets.Add(new MemberData(true));
				});
		}
	}
	#endregion
}