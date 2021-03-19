using MaxyGames.uNode;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MaxyGames {
	/// <summary>
	/// Class for Generating C# code from uNode with useful function for generating C# code more easier.
	/// </summary>
	public static partial class CodeGenerator {
		#region Setup
		private static List<string> InitData = new List<string>();
		private static void Initialize() {
			if(graph != null) {
				uNodeUtility.TempManagement.DestroyTempObjets(); // Make sure the data are clean.
				graph.Refresh();//Ensure the data is up to date.
				if(stateGraph != null) {
					generatorData.eventNodes.AddRange(stateGraph.eventNodes);
				}
				if(!setting.forceGenerateAllNode) {
					if(stateGraph != null) {
						foreach(EventNode method in stateGraph.eventNodes) {
							foreach(var node in method.GetFlows()) {
								ConnectNode(node.GetTargetNode());
							}
						}
					}
					foreach(uNodeFunction function in graph.Functions) {
						if(function != null && function.startNode) {
							ConnectNode(function.startNode);
						}
					}
					foreach(var property in graph.Properties) {
						if(property != null && !property.AutoProperty) {
							if(property.getRoot != null && property.getRoot.startNode) {
								ConnectNode(property.getRoot.startNode);
							}
							if(property.setRoot != null && property.setRoot.startNode) {
								ConnectNode(property.setRoot.startNode);
							}
						}
					}
					foreach(uNodeConstuctor ctor in graph.Constuctors) {
						if(ctor != null && ctor.startNode) {
							ConnectNode(ctor.startNode);
						}
					}
					generatorData.allNode.AddRange(generatorData.connectedNode);
				} else {
					generatorData.allNode.AddRange(graph.nodes);
				}
				if(stateGraph != null) {
					foreach(EventNode method in stateGraph.eventNodes) {
						if(method == null)
							continue;
						//Register events.
						method.RegisterPort();
					}
				}
				foreach(var nodeComp in generatorData.allNode) {
					if(nodeComp == null)
						continue;
					//Register node pin for custom node.
					nodeComp.RegisterPort();
				}
				var flowMaps = new List<KeyValuePair<Node, bool>>();
				foreach(var nodeComp in generatorData.allNode) {
					if(nodeComp == null)
						continue;
					try {
						if (nodeComp is Node node) {
							//Skip if not flow node
							if (!HasFlowPort(node))
								continue;
							if(!uNodeUtility.IsInStateGraph(node) || setting.enableOptimization && IsCanBeGrouped(node)){
								flowMaps.Add(new KeyValuePair<Node, bool>(node, true));
								continue;
							}
							flowMaps.Add(new KeyValuePair<Node, bool>(node, false));
						}
					}
					catch(System.Exception ex) {
						uNodeDebug.LogException(ex, nodeComp);
						throw;
					}
				}
				foreach(var pair in flowMaps) {
					if(pair.Value) {
						generatorData.flowNode.Add(pair.Key);
						generatorData.groupedNode.Add(pair.Key);
					} else {
						generatorData.ungroupedNode.Add(pair.Key);
					}
				}
			}
		}

		private static bool IsCanBeGrouped(Node node) {
			if(IsStackOverflow(node))
				return false;
			var flows = GetFlowConnectedTo(node);
			if(flows.Count <= 1 || flows.All(n => !(n is Node nod) || !generatorData.ungroupedNode.Contains(n) && IsCanBeGrouped(nod))) {
				return true;
			}
			return false;
		}

		/// <summary>
		/// Is the node is stack overflowed?
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static bool IsStackOverflow(NodeComponent node) {
			if(!generatorData.stackOverflowMap.TryGetValue(node, out var result)) {
				result = uNodeUtility.IsStackOverflow(node);
				generatorData.stackOverflowMap[node] = result;
			}
			return result;
		}

		/// <summary>
		/// Reset the generator settings
		/// </summary>
		public static void ResetGenerator() {
			generatorData = new GData();
			graph = null;
			coNum = 0;
			InitData = new List<string>();
			actionID = 0;
			actionDataID = new Dictionary<Block, int>();
		}

		/// <summary>
		/// Generate new c#
		/// </summary>
		/// <param name="setting"></param>
		/// <returns></returns>
		public static GeneratedData Generate(GeneratorSetting setting) {
			//Wait other generation till finish before generate new script.
			if(isGenerating) {
				if(setting.isAsync) {
					//Wait until other generator is finished
					while(isGenerating) {
						ThreadingUtil.WaitOneFrame();
					}
				} else {
					//Update thread queue so the other generation will finish
					while(isGenerating) {
						uNodeThreadUtility.Update();
					}
				}
			}
			try {
				//Set max queue for async generation
				ThreadingUtil.SetMaxQueue(setting.maxQueue);
				isGenerating = true;
				float progress = 0;
				float classCount = setting.graphs.Count != 0 ? setting.graphs.Count / setting.graphs.Count : 0;
				StringBuilder classBuilder = new StringBuilder();
				GeneratedData generatedData = new GeneratedData(classBuilder, setting);
				foreach(var classes in setting.graphs) {
					ResetGenerator();
					graph = classes;
					graphSystem = ReflectionUtils.GetAttributeFrom<GraphSystemAttribute>(classes);
					classes.graphData.unityObjects.Clear();
					generatorData.setting = setting;
					generatorData.state.state = State.Classes;
					//class name.
					string className = classes.Name;
					ThreadingUtil.Do(() => {
						className = classes.GraphName;
						if(string.IsNullOrEmpty(classes.Name) && className == "_" + Mathf.Abs(classes.GetInstanceID())) {
							className = classes.gameObject.name;
						}
						className = uNodeUtility.AutoCorrectName(className);
						generatorData.typeName = className;
						generatedData.classNames[uNodeUtility.GetActualObject(classes)] = className;
						//update progress bar
						setting.updateProgress?.Invoke(progress, "initializing class:" + classes.GraphName);
						//Initialize code gen for classes
						Initialize();
						if(graphSystem.supportVariable) {
							foreach(VariableData var in classes.Variables) {
								List<AData> attribute = new List<AData>();
								if(var.attributes != null && var.attributes.Length > 0) {
									foreach(var a in var.attributes) {
										attribute.Add(TryParseAttributeData(a));
									}
								}
								generatorData.AddVariable(new VData(var, attribute) { modifier = var.modifier });
							}
						}
						if(graphSystem.supportProperty) {
							generatorData.state.state = State.Property;
							foreach(var var in classes.Properties) {
								List<AData> attribute = new List<AData>();
								if(var.attributes != null && var.attributes.Length > 0) {
									foreach(var a in var.attributes) {
										attribute.Add(TryParseAttributeData(a));
									}
								}
								generatorData.properties.Add(new PData(var, attribute) { modifier = var.modifier });
							}
						}
						if(graphSystem.supportConstructor) {
							generatorData.state.state = State.Constructor;
							foreach(var var in classes.Constuctors) {
								generatorData.constructors.Add(new CData(var) { modifier = var.modifier });
							}
						}
					});
					int nodeCount = generatorData.allNode.Count != 0 ? generatorData.allNode.Count / generatorData.allNode.Count : 1;
					int fieldCount = classes.Variables.Count != 0 ? classes.Variables.Count / classes.Variables.Count : 1;
					int propCount = classes.Properties.Count != 0 ? classes.Properties.Count / classes.Properties.Count : 1;
					int ctorCount = classes.Constuctors.Count != 0 ? classes.Constuctors.Count / classes.Constuctors.Count : 1;
					float childFill = ((nodeCount + fieldCount + propCount + ctorCount) / 4F / (classCount)) / 4;
					//Generate functions
					GenerateFunctions((prog, text) => {
						float p = progress + (prog * (childFill));
						if(setting.updateProgress != null)
							setting.updateProgress(p, text);
					});
					progress += childFill;
					//Generate variables
					string variables = GenerateVariables((prog, text) => {
						float p = progress + (prog * (childFill));
						if(setting.updateProgress != null)
							setting.updateProgress(p, text);
					});
					progress += childFill;
					//Generate properties
					string properties = GenerateProperties((prog, text) => {
						float p = progress + (prog * (childFill));
						if(setting.updateProgress != null)
							setting.updateProgress(p, text);
					});
					progress += childFill;
					//Generate constructors
					string constructors = GenerateConstructors((prog, text) => {
						float p = progress + (prog * (childFill));
						if(setting.updateProgress != null)
							setting.updateProgress(p, text);
					});
					progress += childFill;
					generatorData.state.state = State.Classes;
					string genericParameters = null;
					string whereClause = null;
					if(classes is IGenericParameterSystem && graphSystem.supportGeneric) {//Implementing generic parameters
						var gData = (classes as IGenericParameterSystem).GenericParameters.Select(i => new GPData(i.name, i.typeConstraint.Get<Type>())).ToList();
						if(gData != null && gData.Count > 0) {
							genericParameters += "<";
							for(int i = 0; i < gData.Count; i++) {
								if(i != 0)
									genericParameters += ", ";
								genericParameters += gData[i].name;
							}
							genericParameters += ">";
						}
						if(gData != null && gData.Count > 0) {
							for(int i = 0; i < gData.Count; i++) {
								if(!string.IsNullOrEmpty(gData[i].type) &&
									!"object".Equals(gData[i].type) &&
									!"System.Object".Equals(gData[i].type)) {
									whereClause += " where " + gData[i].name + " : " +
										ParseType(gData[i].type);
								}
							}
						}
					}
					string interfaceName = null;
					if(classes is IInterfaceSystem) {//Implementing interfaces
						List<Type> interfaces = (classes as IInterfaceSystem).Interfaces.Where(item => item != null && item.Get<Type>() != null).Select(item => item.Get<Type>()).ToList();
						if(interfaces != null && interfaces.Count > 0) {
							for(int i = 0; i < interfaces.Count; i++) {
								if(interfaces[i] == null)
									continue;
								if(!string.IsNullOrEmpty(interfaceName)) {
									interfaceName += ", ";
								}
								interfaceName += ParseType(interfaces[i]);
							}
						}
					}
					if(!string.IsNullOrEmpty(classBuilder.ToString())) {
						classBuilder.AppendLine();
						classBuilder.AppendLine();
					}
					if(!string.IsNullOrEmpty(classes.summary)) {
						classBuilder.AppendLine("/// <summary>".AddLineInEnd() +
							"/// " + classes.summary.Replace("\n", "\n" + "/// ").AddLineInEnd() +
							"/// </summary>");
					}
					if(classes is IAttributeSystem) {
						foreach(var attribute in (classes as IAttributeSystem).Attributes) {
							if(attribute == null)
								continue;
							AData aData = TryParseAttributeData(attribute);
							if(aData != null) {
								string a = TryParseAttribute(aData);
								if(!string.IsNullOrEmpty(a)) {
									classBuilder.Append(a.AddLineInEnd());
								}
							}
						}
					}
					string classModifier = "public ";
					if(classes is IClassModifier) {
						classModifier = (classes as IClassModifier).GetModifier().GenerateCode();
					}
					Type InheritedType;
					if(graphSystem.inherithFrom != null) {
						InheritedType = graphSystem.inherithFrom;
					} else {
						InheritedType = classes.GetInheritType();
					}
					string classKeyword = "class ";
					if(classes is IClass && (classes as IClass).IsStruct) {
						classKeyword = "struct ";
						InheritedType = null;
					}
					if(InheritedType == null) {
						classBuilder.Append(classModifier + classKeyword + className + genericParameters +
							(interfaceName != null ? (" " + interfaceName) : null) + whereClause + " {");
					} else {
						classBuilder.Append(classModifier + classKeyword + className + genericParameters + " : " +
							ParseType(InheritedType) +
							(interfaceName != null ? (", " + interfaceName) : null) + whereClause + " {");
					}
					string classData = variables.AddLineInFirst() + properties.AddFirst("\n\n", !string.IsNullOrEmpty(variables)) + constructors.AddFirst("\n\n",
						!string.IsNullOrEmpty(variables) || !string.IsNullOrEmpty(properties));
					if(generatorData.coroutineEvent.Count > 0) {
						generatorData.state.isStatic = false;
						generatorData.state.state = State.Function;
						foreach(var p in generatorData.coroutineEvent) {
							var pair = p;
							if(!string.IsNullOrEmpty(pair.Value.variableName) && pair.Key != null) {
								ThreadingUtil.Queue(() => {
									classData += "\nprivate" + $" {_eventCoroutineClass} " + pair.Value + ";";
									string onStopAction = pair.Value.onStop;
									string invokeCode = string.IsNullOrEmpty(pair.Value.customExecution) ?
										_coroutineEventCode.InvokeCode(null, generatorData.GetEventID(pair.Key)).RemoveLast() :
										pair.Value.customExecution;
									string genData = null;
									if(!string.IsNullOrEmpty(onStopAction)) {
										genData = pair.Value.variableName.
											Set(
												GenerateNewObjectCode(
													_eventCoroutineClass,
													classes.ToCode(),
													invokeCode,
													GenerateAnonymousMethod(null, null, onStopAction)
												)
											);
									} else {
										genData = pair.Value.variableName.
											Set(
												GenerateNewObjectCode(
													_eventCoroutineClass,
													classes.ToCode(),
													invokeCode
												)
											);
									}
									if(setting.debugScript && pair.Key as NodeComponent) {
										if(setting.debugPreprocessor)
											genData += "\n#if UNITY_EDITOR".AddLineInFirst();
										genData += DoGenerateInvokeCode(pair.Value + ".Debug", new string[] {
										ParseValue(uNodeUtility.GetObjectID(classes)), ParseValue(uNodeUtility.GetObjectID(pair.Key as NodeComponent))
									}).AddLineInFirst();
										if(setting.debugPreprocessor)
											genData += "\n#endif".AddLineInFirst();
									}
									InitData.Add(genData);
								});
							}
						}
						ThreadingUtil.WaitQueue();
						if(InitData.Count > 0) {//Insert init code into Awake functions.
							string code = "";
							foreach(string s in InitData) {
								code += "\n" + s;
							}
							var method = generatorData.AddMethod("Awake", ParseType(typeof(void)), new string[0]);
							method.code = code + method.code.AddLineInFirst();
						}
					}
					string functionData = null;
					generatorData.state.state = State.Function;
					foreach(MData d in generatorData.methodData) {
						var data = d;
						ThreadingUtil.Queue(() => {
							generatorData.state.isStatic = data.modifier != null && data.modifier.Static;
							functionData += data.GenerateCode().AddFirst("\n\n");
						});
					}
					ThreadingUtil.WaitQueue();
					classData += functionData;
					//generate Nested Type
					if(classes is INestedClassSystem) {
						var nestedType = (classes as INestedClassSystem).NestedClass;
						if(nestedType) {
							generatorData.state.state = State.Classes;
							ThreadingUtil.Do(() => {
								GameObject targetObj = nestedType.gameObject;
								setting.updateProgress?.Invoke(progress, "Generating NestedType...");
								var nestedData = Generate(new GeneratorSetting(targetObj, setting));
								classData += nestedData.FullTypeScript().AddLineInFirst().AddLineInFirst();
							});
						}
					}
					classBuilder.Append(classData.AddTabAfterNewLine(1, false));
					classBuilder.Append("\n}");
				}
				if(setting.graphs.Count == 0) {
					ResetGenerator();
					generatorData.setting = setting;
				}
				ThreadingUtil.Do(() => {
					//Generate interfaces
					if(setting.interfaces != null) {
						foreach(var t in setting.interfaces) {
							if(string.IsNullOrEmpty(t.name))
								continue;
							string value = null;
							value += t.modifiers.GenerateCode() + "interface " + t.name + " {";
							string contents = null;
							foreach(var p in t.properties) {
								if(string.IsNullOrEmpty(p.name))
									continue;
								string localVal = ParseType(p.returnType) + " " + p.name + " {";
								if(p.accessor == PropertyAccessorKind.ReadOnly) {
									localVal += "get;".AddLineInFirst().AddTabAfterNewLine();
								} else if(p.accessor == PropertyAccessorKind.WriteOnly) {
									localVal += "set;".AddLineInFirst().AddTabAfterNewLine();
								} else {
									localVal += ("get;".AddLineInFirst() + "set;".AddLineInFirst()).AddTabAfterNewLine();
								}
								localVal += "\n}";
								contents += localVal.AddLineInFirst().AddFirst("\n", contents != null);
							}
							foreach(var f in t.functions) {
								if(string.IsNullOrEmpty(f.name))
									continue;
								string param = null;
								foreach(var p in f.parameters) {
									if(!string.IsNullOrEmpty(param)) {
										param += ", ";
									}
									if(p.refKind != ParameterData.RefKind.None) {
										if(p.refKind == ParameterData.RefKind.Ref) {
											param += "ref ";
										} else if(p.refKind == ParameterData.RefKind.Out) {
											param += "out ";
										}
									}
									param += ParseType(p.type) + " " + p.name;
								}
								string gParam = null;
								foreach(var p in f.genericParameters) {
									if(!string.IsNullOrEmpty(gParam)) {
										gParam += ", ";
									}
									gParam += p.name;
								}
								if(!string.IsNullOrEmpty(gParam)) {
									gParam = "<" + gParam + ">";
								}
								contents += (ParseType(f.returnType) + " " + f.name + gParam + "(" + param + ");").AddLineInFirst().AddFirst("\n", contents != null);
							}
							value += contents.AddTabAfterNewLine(false) + "\n}";
							classBuilder.Append(value.AddLineInFirst().AddLineInFirst());
						}
					}
					//Generate enums
					if(setting.enums != null) {
						foreach(var t in setting.enums) {
							if(string.IsNullOrEmpty(t.name) || t.enumeratorList.Length == 0)
								continue;
							string value = null;
							value += t.modifiers.GenerateCode() + "enum " + t.name;
							if(t.inheritFrom.isAssigned && t.inheritFrom.Get<Type>() != typeof(int)) {
								value += " : " + ParseType(t.inheritFrom);
							}
							value += " {";
							string EL = null;
							foreach(var e in t.enumeratorList) {
								EL += "\n" + e.name + ",";
							}
							value += EL.AddTabAfterNewLine() + "\n}";
							classBuilder.Append(value.AddLineInFirst().AddLineInFirst());
						}
					}
				});
				ThreadingUtil.Do(() => {
					//Initialize the generated data for futher use
					generatedData.errors = generatorData.errors;
					generatedData.graphOwner = uNodeUtility.GetActualObject(setting.targetData?.gameObject ?? setting.graphs.Select(g => g.gameObject).FirstOrDefault());
					generatedData.graphs = setting.graphs.ToArray();
					for(int i = 0; i < generatedData.graphs.Length; i++) {
						generatedData.graphs[i] = uNodeUtility.GetActualObject(generatedData.graphs[i]);
					}
				});
				RegisterScriptHeader("#pragma warning disable");
				//Finish generating scripts
				setting.updateProgress?.Invoke(1, "finish");
				OnSuccessGeneratingGraph?.Invoke(generatedData, setting);
				//Ensure the generator data is clean.
				ResetGenerator();
				isGenerating = false;
				//Return the generated data
				return generatedData;
			} catch {
				isGenerating = false;
				throw;
			}
		}
		#endregion

		#region Private Functions
		private static void GenerateFunctions(Action<float, string> updateProgress = null) {
			//if((runtimeUNode == null || runtimeUNode.eventNodes.Count == 0) && uNodeObject.Functions.Count == 0)
			//	return;
			float progress = 0;
			float count = 0;
			if(generatorData.allNode.Count > 0) {
				count = generatorData.allNode.Count / generatorData.allNode.Count;
			}
			if(setting.generateTwice) {
				count /= 2;
			}
			generatorData.state.state = State.Function;

			#region Generate Nodes
			generatorData.state.isStatic = false;
			for(int i = 0; i < generatorData.allNode.Count; i++) {
				Node node = generatorData.allNode[i] as Node;
				if(node == null) continue;
				ThreadingUtil.Queue(() => {
					if(node != null) {
						Action action;
						if(generatorData.initActionForNodes.TryGetValue(node, out action)) {
							action();
						}
						//Skip if not flow node
						if(!node.IsFlowNode())
							return;
						if(node.rootObject != null) {
							var ro = node.rootObject;
							if(ro is uNodeFunction) {
								generatorData.state.isStatic = (ro as uNodeFunction).modifiers.Static;
							}
						}
						if(IsUngroupedNode(node)) {
							isInUngrouped = true;
						}
						GenerateNode(node);
						isInUngrouped = false;
						progress += count;
						if(updateProgress != null)
							updateProgress(progress / generatorData.allNode.Count, "generating node:" + node.gameObject.name);
					}
				});
			}
			ThreadingUtil.WaitQueue();
			generatorData.state.isStatic = false;
			if(setting.generateTwice) {
				generatorData.generatedData.Clear();
				generatorData.initializedUserObject.Clear();
				foreach(var mData in generatorData.methodData) {
					mData.ClearCode();
				}
				for(int i = 0; i < generatorData.allNode.Count; i++) {
					Node node = generatorData.allNode[i] as Node;
					if(node == null) continue;
					if(node != null) {
						ThreadingUtil.Queue(() => {
							Action action;
							if(generatorData.initActionForNodes.TryGetValue(node, out action)) {
								action();
							}
							if(!node.IsFlowNode())
								return;
							if(node.rootObject != null) {
								var ro = node.rootObject;
								if(ro is uNodeFunction) {
									generatorData.state.isStatic = (ro as uNodeFunction).modifiers.Static;
								}
							}
							if(IsUngroupedNode(node)) {
								isInUngrouped = true;
							}
							GenerateNode(node);
							isInUngrouped = false;
							progress += count;
							if(updateProgress != null)
								updateProgress(progress / generatorData.allNode.Count, "generating 2nd node:" + node.gameObject.name);
						});
					}
				}
				ThreadingUtil.WaitQueue();
			}
			#endregion

			#region Generate Functions
			for(int x = 0; x < graph.Functions.Count; x++) {
				uNodeFunction function = graph.Functions[x];
				if(function == null)
					return;
				ThreadingUtil.Queue(() => {
					generatorData.state.isStatic = function.modifiers.Static;
					List<AData> attribute = new List<AData>();
					if(function.attributes != null && function.attributes.Length > 0) {
						foreach(var a in function.attributes) {
							attribute.Add(TryParseAttributeData(a));
						}
					}
					MData mData = generatorData.GetMethodData(
						function.Name, 
						function.parameters.Select(i => ParseType(i.type)).ToList(),
						function.genericParameters.Length
					);
					if(mData == null) {
						mData = new MData(
							function.Name, 
							ParseType(function.returnType),
							function.parameters.Select(i => new MPData(i.name, ParseType(i.type), i.refKind)).ToList(),
							function.genericParameters.Select(i => new GPData(i.name, i.typeConstraint.Get<Type>())).ToList()
						);
						generatorData.methodData.Add(mData);
					}
					mData.modifier = function.modifiers;
					mData.attributes = attribute;
					mData.summary = function.summary;
					mData.owner = function;
					if(function.localVariable != null && function.localVariable.Count > 0) {
						string result = null;
						foreach(var vdata in function.localVariable) {
							if(IsInstanceVariable(vdata)) {
								continue;
							} else if(!vdata.resetOnEnter) {
								RegisterVariable(vdata).modifier.SetPrivate();
								continue;
							}
							if(vdata.type.isAssigned && vdata.type.targetType == MemberData.TargetType.Type && vdata.type.startType.IsValueType && vdata.value == null) {
								result += (ParseType(vdata.type) + " " + GetVariableName(vdata) + ";").AddFirst("\n", !string.IsNullOrEmpty(result));
								continue;
							}
							if(vdata.type.targetType == MemberData.TargetType.uNodeGenericParameter) {
								string vType = ParseType(vdata.type);
								if(vdata.variable != null) {
									result += (vType + " " + GetVariableName(vdata) + " = default(" + vType + ");").AddFirst("\n", !string.IsNullOrEmpty(result));
								} else {
									result += (vType + " " + GetVariableName(vdata) + ";").AddFirst("\n", !string.IsNullOrEmpty(result));
								}
								continue;
							}
							result += (ParseType(vdata.type) + " " + GetVariableName(vdata) +
									" = " + ParseValue(vdata.value) + ";").AddFirst("\n", !string.IsNullOrEmpty(result));
						}
						mData.code += result.AddLineInFirst();
					}
					if(function.startNode != null && (mData.modifier == null || !mData.modifier.Abstract)) {
						mData.code += GenerateNode(function.startNode).AddLineInFirst();
					}
				});
			}
			ThreadingUtil.WaitQueue();
			#endregion

			#region Generate Event Nodes
			generatorData.state.isStatic = false;
			for(int i = 0; i < generatorData.eventNodes.Count; i++) {
				EventNode m = generatorData.eventNodes[i];
				if(m == null)
					continue;
				try {
					List<string> parameterType = new List<string>();
					if(m.eventType == EventNode.EventType.OnCollisionEnter ||
						m.eventType == EventNode.EventType.OnCollisionExit ||
						m.eventType == EventNode.EventType.OnCollisionStay) {
						parameterType.Add(ParseType(typeof(Collision)));
					} else if(m.eventType == EventNode.EventType.OnTriggerEnter ||
						m.eventType == EventNode.EventType.OnTriggerExit ||
						m.eventType == EventNode.EventType.OnTriggerStay) {
						parameterType.Add(ParseType(typeof(Collider)));
					} else if(m.eventType == EventNode.EventType.OnCollisionEnter2D ||
						m.eventType == EventNode.EventType.OnCollisionExit2D ||
						m.eventType == EventNode.EventType.OnCollisionStay2D) {
						parameterType.Add(ParseType(typeof(Collision2D)));
					} else if(m.eventType == EventNode.EventType.OnTriggerEnter2D ||
						 m.eventType == EventNode.EventType.OnTriggerExit2D ||
						 m.eventType == EventNode.EventType.OnTriggerStay2D) {
						parameterType.Add(ParseType(typeof(Collider2D)));
					} else if(m.eventType == EventNode.EventType.OnApplicationPause ||
						m.eventType == EventNode.EventType.OnApplicationFocus) {
						parameterType.Add(ParseType(typeof(bool)));
					} else if(m.eventType == EventNode.EventType.OnAnimatorIK) {
						parameterType.Add(ParseType(typeof(int)));
					}
					if(m.targetObjects != null && m.targetObjects.Length > 0) {//Generate event code for multiple target objects.
						foreach(var e in m.targetObjects) {
							if(e == null || !e.isAssigned)
								continue;
							string contents = "";
							var flows = m.GetFlows();
							if(flows != null && flows.Count > 0) {
								foreach(var flow in flows) {
									if(flow == null || flow.GetTargetNode() == null)
										continue;
									ThreadingUtil.Queue(() => {
										try {
											contents += GetInvokeNodeCode(flow.GetTargetNode()).AddLineInFirst();
										}
										catch(System.Exception ex) {
											Debug.LogException(ex, flow.GetTargetNode());
										}
									});
								}
								ThreadingUtil.WaitQueue();
							}
							switch(m.eventType) {
								case EventNode.EventType.OnCollisionEnter: {
									string paramName = GenerateVariableName("col", m);
									if(m.storeValue.isAssigned) {
										contents = contents.Insert(0,
											GenerateSetCode(
												m.storeValue,
												WrapString(paramName),
												m.storeValue.type
											).AddLineInFirst());
									}
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnCollisionEnter".ParseValue(),
											GenerateAnonymousMethod(
												new Type[] { typeof(object) },
												new string[] { paramName },
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnCollisionExit: {
									string paramName = GenerateVariableName("col", m);
									if(m.storeValue.isAssigned) {
										contents = contents.Insert(0,
											GenerateSetCode(
												m.storeValue,
												WrapString(paramName),
												m.storeValue.type
											).AddLineInFirst());
									}
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnCollisionExit".ParseValue(),
											GenerateAnonymousMethod(
												new Type[] { typeof(object) },
												new string[] { paramName },
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnCollisionStay: {
									string paramName = GenerateVariableName("col", m);
									if(m.storeValue.isAssigned) {
										contents = contents.Insert(0,
											GenerateSetCode(
												m.storeValue,
												WrapString(paramName),
												m.storeValue.type
											).AddLineInFirst());
									}
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnCollisionStay".ParseValue(),
											GenerateAnonymousMethod(
												new Type[] { typeof(object) },
												new string[] { paramName },
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnCollisionEnter2D: {
									string paramName = GenerateVariableName("col", m);
									if(m.storeValue.isAssigned) {
										contents = contents.Insert(0,
											GenerateSetCode(
												m.storeValue,
												WrapString(paramName),
												m.storeValue.type
											).AddLineInFirst());
									}
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnCollisionEnter2D".ParseValue(),
											GenerateAnonymousMethod(
												new Type[] { typeof(object) },
												new string[] { paramName },
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnCollisionExit2D: {
									string paramName = GenerateVariableName("col", m);
									if(m.storeValue.isAssigned) {
										contents = contents.Insert(0,
											GenerateSetCode(
												m.storeValue,
												WrapString(paramName),
												m.storeValue.type
											).AddLineInFirst());
									}
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnCollisionExit2D".ParseValue(),
											GenerateAnonymousMethod(
												new Type[] { typeof(object) },
												new string[] { paramName },
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnCollisionStay2D: {
									string paramName = GenerateVariableName("col", m);
									if(m.storeValue.isAssigned) {
										contents = contents.Insert(0,
											GenerateSetCode(
												m.storeValue,
												WrapString(paramName),
												m.storeValue.type
											).AddLineInFirst());
									}
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnCollisionStay2D".ParseValue(),
											GenerateAnonymousMethod(
												new Type[] { typeof(object) },
												new string[] { paramName },
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnTriggerEnter: {
									string paramName = GenerateVariableName("other", m);
									if(m.storeValue.isAssigned) {
										contents = contents.Insert(0,
											GenerateSetCode(
												m.storeValue,
												WrapString(paramName),
												m.storeValue.type
											).AddLineInFirst());
									}
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnTriggerEnter".ParseValue(),
											GenerateAnonymousMethod(
												new Type[] { typeof(object) },
												new string[] { paramName },
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnTriggerExit: {
									string paramName = GenerateVariableName("other", m);
									if(m.storeValue.isAssigned) {
										contents = contents.Insert(0,
											GenerateSetCode(
												m.storeValue,
												WrapString(paramName),
												m.storeValue.type
											).AddLineInFirst());
									}
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnTriggerExit".ParseValue(),
											GenerateAnonymousMethod(
												new Type[] { typeof(object) },
												new string[] { paramName },
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnTriggerStay: {
									string paramName = GenerateVariableName("other", m);
									if(m.storeValue.isAssigned) {
										contents = contents.Insert(0,
											GenerateSetCode(
												m.storeValue,
												WrapString(paramName),
												m.storeValue.type
											).AddLineInFirst());
									}
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnTriggerStay".ParseValue(),
											GenerateAnonymousMethod(
												new Type[] { typeof(object) },
												new string[] { paramName },
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnTriggerEnter2D: {
									string paramName = GenerateVariableName("other", m);
									if(m.storeValue.isAssigned) {
										contents = contents.Insert(0,
											GenerateSetCode(
												m.storeValue,
												WrapString(paramName),
												m.storeValue.type
											).AddLineInFirst());
									}
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnTriggerEnter2D".ParseValue(),
											GenerateAnonymousMethod(
												new Type[] { typeof(object) },
												new string[] { paramName },
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnTriggerExit2D: {
									string paramName = GenerateVariableName("other", m);
									if(m.storeValue.isAssigned) {
										contents = contents.Insert(0,
											GenerateSetCode(
												m.storeValue,
												WrapString(paramName),
												m.storeValue.type
											).AddLineInFirst());
									}
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnTriggerExit2D".ParseValue(),
											GenerateAnonymousMethod(
												new Type[] { typeof(object) },
												new string[] { paramName },
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnTriggerStay2D: {
									string paramName = GenerateVariableName("other", m);
									if(m.storeValue.isAssigned) {
										contents = contents.Insert(0,
											GenerateSetCode(
												m.storeValue,
												WrapString(paramName),
												m.storeValue.type
											).AddLineInFirst());
									}
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnTriggerStay2D".ParseValue(),
											GenerateAnonymousMethod(
												new Type[] { typeof(object) },
												new string[] { paramName },
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnBecameInvisible: {
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnBecameInvisible".ParseValue(),
											GenerateAnonymousMethod(
												null,
												null,
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnBecameVisible: {
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnBecameVisible".ParseValue(),
											GenerateAnonymousMethod(
												null,
												null,
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnDestroy: {
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnDestroy".ParseValue(),
											GenerateAnonymousMethod(
												null,
												null,
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnMouseDown: {
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnMouseDown".ParseValue(),
											GenerateAnonymousMethod(
												null,
												null,
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnMouseDrag: {
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnMouseDrag".ParseValue(),
											GenerateAnonymousMethod(
												null,
												null,
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnMouseEnter: {
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnMouseEnter".ParseValue(),
											GenerateAnonymousMethod(
												null,
												null,
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnMouseExit: {
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnMouseExit".ParseValue(),
											GenerateAnonymousMethod(
												null,
												null,
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnMouseOver: {
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnMouseOver".ParseValue(),
											GenerateAnonymousMethod(
												null,
												null,
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnMouseUp: {
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnMouseUp".ParseValue(),
											GenerateAnonymousMethod(
												null,
												null,
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnMouseUpAsButton: {
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnMouseUpAsButton".ParseValue(),
											GenerateAnonymousMethod(
												null,
												null,
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnTransformChildrenChanged: {
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnTransformChildrenChanged".ParseValue(),
											GenerateAnonymousMethod(
												null,
												null,
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
								case EventNode.EventType.OnTransformParentChanged: {
									generatorData.InsertMethodCode(
										"Awake",
										ParseType(typeof(void)),
										GenerateInvokeCode(
											"MaxyGames.Runtime.RuntimeUtility".ToType(),
											"AddEvent",
											ParseValue(e),
											"OnTransformParentChanged".ParseValue(),
											GenerateAnonymousMethod(
												null,
												null,
												contents
											)).AddLineInFirst(),
										new string[0]);
									break;
								}
							}
						}
					} else {//Self target : Add functions.
						string fName = m.eventType == EventNode.EventType.Custom ? generatorData.GetMethodName(m) : m.eventType.ToString();
						MData mData = generatorData.GetMethodData(fName, parameterType);
						if(mData == null) {
							var func = graph.GetFunction(fName);
							Type funcType = typeof(void);
							if(func != null) {
								funcType = func.ReturnType();
							}
							mData = generatorData.AddMethod(fName, ParseType(funcType), parameterType);
							if(m.eventType == EventNode.EventType.Custom) {
								mData.modifier = new FunctionModifier() { Public = true };
							}
						}
						string initData = null;
						if(m.eventType == EventNode.EventType.OnCollisionEnter ||
							m.eventType == EventNode.EventType.OnCollisionExit ||
							m.eventType == EventNode.EventType.OnCollisionStay) {
							parameterType.Add(ParseType(typeof(Collision)));
							if(m.storeValue.isAssigned) {
								initData = GenerateSetCode(m.storeValue, mData.parameters[0].name);
							}
						} else if(m.eventType == EventNode.EventType.OnTriggerEnter ||
							m.eventType == EventNode.EventType.OnTriggerExit ||
							m.eventType == EventNode.EventType.OnTriggerStay) {
							parameterType.Add(ParseType(typeof(Collider)));
							if(m.storeValue.isAssigned) {
								initData = GenerateSetCode(m.storeValue, mData.parameters[0].name);
							}
						} else if(m.eventType == EventNode.EventType.OnCollisionEnter2D ||
							m.eventType == EventNode.EventType.OnCollisionExit2D ||
							m.eventType == EventNode.EventType.OnCollisionStay2D) {
							parameterType.Add(ParseType(typeof(Collision2D)));
							if(m.storeValue.isAssigned) {
								initData = GenerateSetCode(m.storeValue, mData.parameters[0].name);
							}
						} else if(m.eventType == EventNode.EventType.OnTriggerEnter2D ||
							 m.eventType == EventNode.EventType.OnTriggerExit2D ||
							 m.eventType == EventNode.EventType.OnTriggerStay2D) {
							parameterType.Add(ParseType(typeof(Collider2D)));
							if(m.storeValue.isAssigned) {
								initData = GenerateSetCode(m.storeValue, mData.parameters[0].name);
							}
						} else if(m.eventType == EventNode.EventType.OnApplicationPause ||
							m.eventType == EventNode.EventType.OnApplicationFocus) {
							parameterType.Add(ParseType(typeof(bool)));
							if(m.storeValue.isAssigned) {
								initData = GenerateSetCode(m.storeValue, mData.parameters[0].name);
							}
						} else if(m.eventType == EventNode.EventType.OnAnimatorIK) {
							parameterType.Add(ParseType(typeof(int)));
							if(m.storeValue.isAssigned) {
								initData = GenerateSetCode(m.storeValue, mData.parameters[0].name);
							}
						}
						if(!string.IsNullOrEmpty(initData)) {
							mData.AddCode(initData, -100);
						}
						var flows = m.GetFlows();
						if(flows != null && flows.Count > 0) {
							for(int x = 0; x < flows.Count; x++) {
								var flow = flows[x];
								if(flow == null || flow.GetTargetNode() == null)
									continue;
								ThreadingUtil.Queue(() => {
									try {
										mData.AddCode(RunEvent(flow), -100);
									}
									catch(System.Exception ex) {
										Debug.LogException(ex, flow.GetTargetNode());
									}
								});
							}
							ThreadingUtil.WaitQueue();
						}
					}
				}
				catch(System.Exception ex) {
					if(setting != null && setting.isAsync) {

					} else {
						if(!generatorData.hasError)
							Debug.LogError("Error generate code from event: " + m.Name + "\nError:" + ex.ToString(), m);
						generatorData.hasError = true;
						throw ex;
					}
				}
			}
			#endregion

			#region Generate Event Functions
			List<string> CoroutineEventFunc = new List<string>();
			if(generatorData.ungroupedNode.Count > 0) {
				//Creating ActivateEvent for non grouped node
				for(int i=0;i<generatorData.ungroupedNode.Count;i++) {
					Node node = generatorData.ungroupedNode.ElementAt(i) as Node;
					if(node == null || !node.IsFlowNode()) //skip on node is not flow node.
						continue;
					ThreadingUtil.Queue(() => {
						isInUngrouped = true;
						string generatedCode = GenerateNode(node);
						isInUngrouped = false;
						//if(string.IsNullOrEmpty(generatedCode)) continue;
						//if(!string.IsNullOrEmpty(str)) 
						{
							if(!string.IsNullOrEmpty(generatedCode) && !setting.fullComment) {
								generatedCode = "\n" + generatedCode;
							}
							string s = "case " + generatorData.GetEventID(node) + ": {" +
								generatedCode.AddTabAfterNewLine(1) + "\n}";
							if(generatorData.nonBreakNode.Contains(node as Node)) {
								CoroutineEventFunc.Add(s);
							} else {
								CoroutineEventFunc.Add(s + "\nbreak;");
							}
						}
					});
				}
				ThreadingUtil.WaitQueue();
			}
			if(generatorData.eventCoroutineData.Count > 0) {
				foreach(var pair in generatorData.eventCoroutineData) {
					string data = "case " + generatorData.GetEventID(pair.Key) + ": {" +
						(pair.Value.AddLineInFirst() + "\nbreak;").AddTabAfterNewLine(1) + "\n}";
					CoroutineEventFunc.Add(data);
				}
			}
			if(CoroutineEventFunc.Count > 0 || generatorData.coroutineEvent.Count > 0) {
				MData method = generatorData.AddMethod(_coroutineEventCode, ParseType(typeof(IEnumerable)), new MPData("uid", ParseType(typeof(int))));
				method.code += "\nswitch(uid) {";
				foreach(string str in CoroutineEventFunc) {
					method.code += ("\n" + str).AddTabAfterNewLine(1);
				}
				foreach(var pair in generatorData.coroutineEvent) {
					if(!string.IsNullOrEmpty(pair.Value.customExecution)) continue;
					string data = pair.Value.contents.AddFirst("\n");
					if (!string.IsNullOrEmpty(data)) {
						method.code += ("\ncase " + generatorData.GetEventID(pair.Key) + ": {" + data.AddTabAfterNewLine(1) + "\n}\nbreak;").AddTabAfterNewLine();
					}
				}
				method.code += "\n}\nyield break;";
			}
			if(generatorData.eventActions.Count > 0) {
				MData method = generatorData.AddMethod(_activateActionCode, ParseType(typeof(bool)), new MPData("ID", ParseType(typeof(int))));
				method.code += "\nswitch(ID) {";
				string str = null;
				foreach(KeyValuePair<Block, string> value in generatorData.eventActions) {
					if(actionDataID.ContainsKey(value.Key)) {
						string data = value.Value.AddFirst("\n");
						str += "\ncase " + actionDataID[value.Key] + ": {" + data.AddTabAfterNewLine(1) + "\n}\nbreak;";
					}
				}
				method.code += str.AddTabAfterNewLine(1).Add("\n") + "}\nreturn true;";
			}
			if(generatorData.customUIDMethods.Count > 0) {
				foreach(var pair in generatorData.customUIDMethods) {
					foreach(var pair2 in pair.Value) {
						MData method = generatorData.AddMethod(pair.Key, ParseType(pair2.Key), new MPData("name", ParseType(typeof(string))));
						method.code += "\nswitch(name) {";
						foreach(var pair3 in pair2.Value) {
							string data = pair3.Value.AddFirst("\n");
							method.code += ("\ncase \"" + pair3.Key + "\": {" + data.AddTabAfterNewLine(1) + "\n}\nbreak;").AddTabAfterNewLine();
						}
						method.code += "\n}";
						if(pair2.Key == typeof(IEnumerable) || pair2.Key == typeof(IEnumerator)) {
							method.code += "yield break;".AddLineInFirst();
						} else if(pair2.Key != typeof(void)) {
							method.code += ("return default(" + ParseType(pair2.Key) + ");").AddLineInFirst();
						}
					}
				}
			}
			if(generatorData.debugMemberMap.Count > 0) {
				var map = generatorData.debugMemberMap;
				if(map.Count > 0) {
					MData method = generatorData.AddMethod(_debugGetValueCode, "T", new MPData[] { new MPData("ID", ParseType(typeof(int))), new MPData("debugValue", "T") }, new GPData[] { new GPData("T") });
					method.code += "\nswitch(ID) {";
					string str = null;
					foreach(var value in map) {
						string data = value.Value.Value.AddFirst("\n");
						str += "\ncase " + value.Value.Key + ": {" + data.AddTabAfterNewLine(1) + "\n}";
					}
					method.code += str.AddTabAfterNewLine(1).Add("\n") + "}\nthrow null;";
				}
			}
			#endregion
		}

		private static string GenerateVariables(Action<float, string> updateProgress = null) {
			if(generatorData.GetVariables().Count == 0)
				return null;
			float progress = 0;
			float count = generatorData.GetVariables().Count / generatorData.GetVariables().Count;
			string result = null;
			generatorData.state.state = State.Classes;
			foreach(VData vdata in generatorData.GetVariables()) {
				if(!vdata.isInstance)
					continue;
				ThreadingUtil.Queue(() => {
					try {
						generatorData.state.isStatic = vdata.IsStatic;
						string str = vdata.GenerateCode().AddFirst("\n", !string.IsNullOrEmpty(result));
						if(includeGraphInformation && vdata.variableRef is VariableData) {
							str = WrapWithInformation(str, vdata.variableRef);
						}
						result += str;
						progress += count;
						if (updateProgress != null)
							updateProgress(progress / generatorData.GetVariables().Count, "generating variable");
					} catch (Exception ex) {
						if (setting != null && setting.isAsync) {
							generatorData.errors.Add(ex);
							//In case async return error commentaries.
							result = "/*Error from variable: " + vdata.name + " */";
							return;
						}
						Debug.LogError("Error on generating variable:" + vdata.name);
						throw;
					}
				});
			}
			ThreadingUtil.WaitQueue();
			return result;
		}

		private static string GenerateProperties(Action<float, string> updateProgress = null) {
			if(generatorData.properties.Count == 0)
				return null;
			float progress = 0;
			float count = generatorData.properties.Count / generatorData.properties.Count;
			string result = null;
			generatorData.state.state = State.Property;
			foreach(var prop in generatorData.properties) {
				if(prop == null || !prop.obj)
					continue;
				ThreadingUtil.Queue(() => {
					generatorData.state.isStatic = prop.modifier != null && prop.modifier.Static;
					string str = prop.GenerateCode().AddFirst("\n", result != null);
					if(includeGraphInformation && prop.obj != null) {
						str = WrapWithInformation(str, prop.obj);
					}
					result += str;
					progress += count;
					if(updateProgress != null)
						updateProgress(progress / generatorData.properties.Count, "generating property");
				});
			}
			ThreadingUtil.WaitQueue();
			return result;
		}

		private static string GenerateConstructors(Action<float, string> updateProgress = null) {
			if(generatorData.constructors.Count == 0)
				return null;
			float progress = 0;
			float count = generatorData.constructors.Count / generatorData.constructors.Count;
			string result = null;
			generatorData.state.isStatic = false;
			generatorData.state.state = State.Constructor;
			for(int i = 0; i < generatorData.constructors.Count; i++) {
				var ctor = generatorData.constructors[i];
				if(ctor == null || !ctor.obj)
					continue;
				ThreadingUtil.Queue(() => {
					string str = ctor.GenerateCode().AddFirst("\n\n", result != null);
					if(includeGraphInformation && ctor.obj != null) {
						str = WrapWithInformation(str, ctor.obj);
					}
					result += str;
					progress += count;
					if(updateProgress != null)
						updateProgress(progress / generatorData.constructors.Count, "generating constructor");
				});
			}
			ThreadingUtil.WaitQueue();
			return result;
		}
		#endregion

		#region GetCorrectName
		/// <summary>
		/// Function to get correct code for Get correct name in MemberReflection
		/// </summary>
		/// <param name="mData"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static string GetCorrectName(MemberData mData, MemberData[] parameters = null, ValueData initializer = null, Action<string, string> onEnterAndExit = null, bool autoConvert = false) {
			List<string> result = new List<string>();
			MemberInfo[] memberInfo = null;
			switch(mData.targetType) {
				case MemberData.TargetType.None:
				case MemberData.TargetType.Null:
				case MemberData.TargetType.Type:
				case MemberData.TargetType.uNodeGenericParameter:
					break;
				default:
					memberInfo = mData.GetMembers(false);
					break;
				case MemberData.TargetType.SelfTarget: {
					if(mData.instance == null) {
						throw new System.Exception("Variable with self target type can't have null value");
					}
					return ParseValue(mData.instance);
				}
				case MemberData.TargetType.Values: {
					if(initializer != null && initializer.value as ConstructorValueData != null) {
						ConstructorValueData ctor = initializer.value as ConstructorValueData;
						if(ctor.initializer != null && ctor.initializer.Length > 0) {
							return ParseValue(mData.Get(), ctor.initializer);
						}
					}
					return ParseValue(mData.Get());
				}
				case MemberData.TargetType.uNodeFunction: {
					string data = mData.startName;
					string[] gType;
					string[] pType = null;
					if(mData.SerializedItems.Length > 0) {
						MemberDataUtility.GetItemName(SerializerUtility.Deserialize<MemberData.ItemData>(
									mData.SerializedItems[0]),
									mData.targetReference,
									out gType,
									out pType);
						if(gType.Length > 0) {
							data += String.Format("<{0}>", String.Join(", ", gType));
						}
					}
					data += "(";
					if(pType != null && pType.Length > 0) {
						var func = mData.GetUnityObject() as uNodeFunction;
						for(int i = 0; i < pType.Length; i++) {
							if(i != 0) {
								data += ", ";
							}
							if(func != null && func.parameters.Length > i) {
								if(func.parameters[i].isByRef) {
									if(func.parameters[i].refKind == ParameterData.RefKind.Ref) {
										data += "ref ";
									} else if(func.parameters[i].refKind == ParameterData.RefKind.Out) {
										data += "out ";
									}
								}
							}
							MemberData p = parameters[i];
							if(debugScript && setting.debugValueNode) {
								setting.debugScript = false;
								data += ParseValue((object)p);
								setting.debugScript = true;
							} else {
								data += ParseValue((object)p);
							}
						}
					}
					data += ")";
					return data;
				}
			}
			if(memberInfo != null && memberInfo.Length > 0) {
				int accessIndex = 0;
				if(parameters != null && parameters.Length > 1 && (parameters[0] == null || !parameters[0].isAssigned)) {
					accessIndex = 1;
				}
				int i = 0;
				string enter = null;
				string exit = null;
				foreach(var member in memberInfo) {
					if(member == null)
						throw new Exception("Incorrect/Unassigned Target");
					string genericData = null;
					if(mData.Items == null || i >= mData.Items.Length)
						break;
					MemberData.ItemData iData = mData.Items[i];
					if(mData.Items.Length > i + 1) {
						iData = mData.Items[i + 1];
					}
					if(iData != null) {
						string[] paramsType = new string[0];
						string[] genericType = new string[0];
						MemberDataUtility.GetItemName(mData.Items[i + 1],
							mData.targetReference,
							out genericType,
							out paramsType);
						if(genericType.Length > 0) {
							if(mData.targetType != MemberData.TargetType.uNodeGenericParameter &&
								mData.targetType != MemberData.TargetType.Type) {
								genericData += string.Format("<{0}>", string.Join(", ", genericType));
							} else {
								genericData += string.Format("{0}", string.Join(", ", genericType));
							}
						}
					}
					bool isRuntime = member is IRuntimeMember;
					if(isRuntime) {
						if(member is RuntimeField) {
							result.Add(GenerateGetRuntimeVariable(member as RuntimeField) + genericData);
						} else if(member is RuntimeProperty) {
							result.Add(GenerateGetRuntimeProperty(member as RuntimeProperty) + genericData);
						} else if(member is RuntimeMethod method) {
							ParameterInfo[] paramInfo = method.GetParameters();
							MemberData[] datas = new MemberData[paramInfo.Length];
							for(int index = 0; index < paramInfo.Length; index++) {
								datas[index] = parameters[accessIndex];
								accessIndex++;
							}
							result.Add(GenerateInvokeRuntimeMethod(member as RuntimeMethod, datas, ref enter, ref exit, autoConvert) + genericData);
						} else {
							throw new InvalidOperationException();
						}
					} else if(member is MethodInfo) {
						MethodInfo method = member as MethodInfo;
						ParameterInfo[] paramInfo = method.GetParameters();
						if(paramInfo.Length > 0) {
							string data = null;
							List<string> dataList = new List<string>();
							for(int index = 0; index < paramInfo.Length; index++) {
								MemberData p = parameters[accessIndex];
								string pData = null;
								if(paramInfo[index].IsOut) {
									pData += "out ";
								} else if(paramInfo[index].ParameterType.IsByRef) {
									pData += "ref ";
								}
								if(pData != null) {
									bool correct = true;
									if (p.type != null && p.type.IsValueType) {
										MemberInfo[] MI = p.GetMembers();
										if (MI != null && MI.Length > 1 && ReflectionUtils.GetMemberType(MI[MI.Length - 2]).IsValueType) {
											string varName = GenerateVariableName("tempVar");
											var pVal = ParseValue((object)p);
											pData += varName + "." + pVal.Remove(pVal.IndexOf(ParseStartValue(p)), ParseStartValue(p).Length + 1).SplitMember().Last();
											if (pVal.LastIndexOf(".") >= 0) {
												pVal = pVal.Remove(pVal.LastIndexOf("."));
											}
											enter += ParseType(ReflectionUtils.GetMemberType(MI[MI.Length - 2])) + " " + varName + " = " + pVal + ";\n";
											exit += pVal + " = " + varName + ";";
											correct = false;
										}
									}
									if(correct) {
										if(debugScript && setting.debugValueNode) {
											setting.debugScript = false;
											pData += ParseValue((object)p);
											setting.debugScript = true;
										} else {
											pData += ParseValue((object)p);
										}
									}
								} else {
									pData += ParseValue((object)p);
								}
								dataList.Add(pData);
								accessIndex++;
							}
							for(int index = 0; index < dataList.Count; index++) {
								if(index != 0) {
									data += ", ";
								}
								data += dataList[index];
							}
							if(member.Name == "Item" || member.Name == "get_Item") {
								result.Add("[" + data + "]");
							} else if(member.Name.StartsWith("set_")) {
								if(member.Name.Equals("set_Item") && method.GetParameters().Length == 2) {
									result.Add("[" + dataList[0] + "] = " + dataList[1]);
								} else {
									result.Add(member.Name.Replace("set_", "") + " = " + data + genericData);
								}
							} else if(member.Name.StartsWith("op_")) {
								if(member.Name == "op_Addition") {
									result.Add(dataList[0] + "+" + dataList[1]);
								} else if(member.Name == "op_Subtraction") {
									result.Add(dataList[0] + "-" + dataList[1]);
								} else if(member.Name == "op_Division") {
									result.Add(dataList[0] + "/" + dataList[1]);
								} else if(member.Name == "op_Multiply") {
									result.Add(dataList[0] + "*" + dataList[1]);
								} else if(member.Name == "op_Modulus") {
									result.Add(dataList[0] + "%" + dataList[1]);
								} else if(member.Name == "op_Equality") {
									result.Add(dataList[0] + "==" + dataList[1]);
								} else if(member.Name == "op_Inequality") {
									result.Add(dataList[0] + "!=" + dataList[1]);
								} else if(member.Name == "op_LessThan") {
									result.Add(dataList[0] + "<" + dataList[1]);
								} else if(member.Name == "op_GreaterThan") {
									result.Add(dataList[0] + ">" + dataList[1]);
								} else if(member.Name == "op_LessThanOrEqual") {
									result.Add(dataList[0] + "<=" + dataList[1]);
								} else if(member.Name == "op_GreaterThanOrEqual") {
									result.Add(dataList[0] + ">=" + dataList[1]);
								} else if(member.Name == "op_BitwiseAnd") {
									result.Add(dataList[0] + "&" + dataList[1]);
								} else if(member.Name == "op_BitwiseOr") {
									result.Add(dataList[0] + "|" + dataList[1]);
								} else if(member.Name == "op_LeftShift") {
									result.Add(dataList[0] + "<<" + dataList[1]);
								} else if(member.Name == "op_RightShift") {
									result.Add(dataList[0] + ">>" + dataList[1]);
								} else if(member.Name == "op_ExclusiveOr") {
									result.Add(dataList[0] + "^" + dataList[1]);
								} else if(member.Name == "op_UnaryNegation") {
									result.Add(dataList[0] + "-" + dataList[1]);
								} else if(member.Name == "op_UnaryPlus") {
									result.Add(dataList[0] + "+" + dataList[1]);
								} else if(member.Name == "op_LogicalNot") {
									result.Add(dataList[0] + "!" + dataList[1]);
								} else if(member.Name == "op_OnesComplement") {
									result.Add(dataList[0] + "~" + dataList[1]);
								} else if(member.Name == "op_Increment") {
									result.Add(dataList[0] + "++" + dataList[1]);
								} else if(member.Name == "op_Decrement") {
									result.Add(dataList[0] + "--" + dataList[1]);
								} else {
									result.Add(member.Name + genericData + "(" + data + ")");
								}
							} else if(member.Name.StartsWith("Get") && method.GetParameters().Length == 1 && (i > 0 && ReflectionUtils.GetMemberType(memberInfo[i - 1]).IsArray || i == 0 && mData.startType.IsArray)) {
								if(result.Count > 0) {
									result[result.Count - 1] = result[result.Count - 1] + "[" + data + "]";
								} else {
									result.Add("[" + data + "]");
								}
							} else if(member.Name.StartsWith("Set") && method.GetParameters().Length == 2 && (i > 0 && ReflectionUtils.GetMemberType(memberInfo[i - 1]).IsArray || i == 0 && mData.startType.IsArray)) {
								result.Add(member.Name.Replace("Set", "[" + dataList[0] + "]") + " = " + dataList[1]);
							} else {
								result.Add(member.Name + genericData + "(" + data + ")");
							}
						} else if(member.Name.StartsWith("get_")) {
							result.Add(member.Name.Replace("get_", "") + genericData);
						} else {
							if(i == memberInfo.Length - 1 && parameters != null && accessIndex < parameters.Length) {
								string data = null;
								for(int x = accessIndex; x < parameters.Length; x++) {
									if(x != accessIndex) {
										data += ", ";
									}
									MemberData p = parameters[x];
									data += ParseValue((object)p);
								}
								result.Add(member.Name + genericData + "(" + data + ")");
							} else {
								result.Add(member.Name + genericData + "()");
							}
						}
					} else if(member is ConstructorInfo) {
						ConstructorInfo ctor = member as ConstructorInfo;
						ParameterInfo[] paramInfo = ctor.GetParameters();
						string ctorInit = ParseConstructorInitializer(initializer);
						if(paramInfo.Length > 0) {
							string data = null;
							List<string> dataList = new List<string>();
							for(int index = 0; index < paramInfo.Length; index++) {
								MemberData p = parameters[accessIndex];
								string pData = null;
								if(paramInfo[index].IsOut) {
									pData += "out ";
								} else if(paramInfo[index].ParameterType.IsByRef) {
									pData += "ref ";
								}
								if(debugScript && setting.debugValueNode) {
									setting.debugScript = false;
									pData += ParseValue((object)p);
									setting.debugScript = true;
								} else {
									pData += ParseValue((object)p);
								}
								dataList.Add(pData);
								accessIndex++;
							}
							for(int index = 0; index < dataList.Count; index++) {
								if(index != 0) {
									data += ", ";
								}
								data += dataList[index];
							}
							if(result.Count > 0) {
								result.Add("(new " + ParseType(ctor.DeclaringType) + "(" + data + ")" + ctorInit + ")");
							} else {
								result.Add("new " + ParseType(ctor.DeclaringType) + "(" + data + ")" + ctorInit);
							}
						} else {
							if(i == memberInfo.Length - 1 && parameters != null && accessIndex < parameters.Length) {
								string data = null;
								for(int x = accessIndex; x < parameters.Length; x++) {
									if(x != accessIndex) {
										data += ", ";
									}
									MemberData p = parameters[x];
									data += ParseValue((object)p);
								}
								if(result.Count > 0) {
									result.Add("(new " + ParseType(ctor.DeclaringType) + "(" + data + ")" + ctorInit + ")");
								} else {
									result.Add("new " + ParseType(ctor.DeclaringType) + "(" + data + ")" + ctorInit);
								}
							} else {
								if(result.Count > 0) {
									result.Add("(new " + ParseType(ctor.DeclaringType) + "()" + ctorInit + ")");
								} else {
									result.Add("new " + ParseType(ctor.DeclaringType) + "()" + ctorInit);
								}
							}
						}
					} else {
						result.Add(member.Name + genericData);
					}
					i++;
				}
				if(enter != null && onEnterAndExit != null)
					onEnterAndExit(enter, exit);
			} else if(mData.targetType == MemberData.TargetType.Constructor) {
				string ctorInit = ParseConstructorInitializer(initializer);
				result.Add("new " + ParseType(mData.startType) + "()" + ctorInit);
			}
			if(result.Count > 0) {
				string resultCode = string.Join(".", result.ToArray());
				if(result.Any(i => i.StartsWith("[") || i.StartsWith("("))) {
					resultCode = null;
					for(int i = 0; i < result.Count; i++) {
						resultCode += result[i];
						if(i + 1 != result.Count && !result[i + 1].StartsWith("[") && !result[i + 1].StartsWith("(")) {
							resultCode += ".";
						}
					}
				}
				string startData;
				if(IsContainOperatorCode(mData.name)) {
					throw new System.Exception("unsupported generating operator code in current context");
				}
				if(mData.targetType == MemberData.TargetType.Constructor) {
					return resultCode;
				}
				startData = ParseStartValue(mData);
				if(string.IsNullOrEmpty(startData)) {
					return resultCode;
				}
				return startData.Add(".", !resultCode.StartsWith("[") && !resultCode.StartsWith("(")) + resultCode;
			} else if(mData.isAssigned) {
				string str = mData.name;
				string[] names = mData.namePath;
				if(mData.isAssigned && mData.SerializedItems != null && mData.SerializedItems.Length > 0) {
					if(mData.SerializedItems.Length == names.Length) {
						str = null;
						if(mData.targetType == MemberData.TargetType.Constructor) {
							str += "new " + mData.type.PrettyName();
						}
						int accessIndex = 0;
						if(parameters != null && parameters.Length > 1 && (parameters[0] == null || !parameters[0].isAssigned)) {
							accessIndex = 1;
						}
						for(int i = 0; i < names.Length; i++) {
							if(i != 0 && (mData.targetType != MemberData.TargetType.Constructor)) {
								str += ".";
							}
							if(mData.targetType != MemberData.TargetType.uNodeGenericParameter &&
								mData.targetType != MemberData.TargetType.Type &&
								mData.targetType != MemberData.TargetType.Constructor) {
								str += names[i];
							}
							MemberData.ItemData iData = mData.Items[i];
							if(iData != null) {
								string[] paramsType = new string[0];
								string[] genericType = new string[0];
								MemberDataUtility.GetItemName(mData.Items[i],
									mData.targetReference,
									out genericType,
									out paramsType);
								if(genericType.Length > 0) {
									if(mData.targetType != MemberData.TargetType.uNodeGenericParameter &&
										mData.targetType != MemberData.TargetType.Type) {
										str += string.Format("<{0}>", string.Join(", ", genericType));
									} else {
										str += string.Format("{0}", string.Join(", ", genericType));
									}
								}
								if(paramsType.Length > 0 ||
									mData.targetType == MemberData.TargetType.uNodeFunction ||
									mData.targetType == MemberData.TargetType.uNodeConstructor ||
									mData.targetType == MemberData.TargetType.Constructor ||
									mData.targetType == MemberData.TargetType.Method && !mData.isDeepTarget) {
									List<string> dataList = new List<string>();
									var func = mData.GetUnityObject() as RootObject;
									for(int index = 0; index < paramsType.Length; index++) {
										MemberData p = parameters[accessIndex];
										string data = null;
										if(func != null && func.parameters.Length > index) {
											if(func.parameters[index].isByRef) {
												if(func.parameters[index].refKind == ParameterData.RefKind.Ref) {
													data += "ref ";
												} else if(func.parameters[index].refKind == ParameterData.RefKind.Out) {
													data += "out ";
												}
											}
										}
										if(debugScript && setting.debugValueNode) {
											setting.debugScript = false;
											dataList.Add(data + ParseValue((object)p));
											setting.debugScript = true;
										} else {
											dataList.Add(data + ParseValue((object)p));
										}
										accessIndex++;
									}
									str += string.Format("({0})", string.Join(", ", dataList.ToArray()));
								}
							}
						}
					}
				} else if(mData.isAssigned) {
					switch(mData.targetType) {
						case MemberData.TargetType.Constructor:
							return "new " + mData.type.PrettyName() + "()";
					}
				}
				string nextNames = str;
				var strs = nextNames.SplitMember();
				if(strs.Count > 0) {
					strs.RemoveAt(0);
				}
				nextNames = string.Join(".", strs.ToArray());
				if(nextNames.StartsWith(".")) {
					nextNames = nextNames.Remove(0, 1);
				}
				str = ParseStartValue(mData).Add(".", !string.IsNullOrEmpty(nextNames)) + nextNames;
				if(str.Contains("get_")) {
					str = str.Replace("get_", "");
				} else if(str.Contains("set_")) {
					str = str.Replace("set_", "");
				}
				//if(str.Contains("Item")) {
				//	str = str.Replace(".Item", "[]");
				//}
				return str;
			}
			return null;
		}
		#endregion

		#region Parse Type
		/// <summary>
		/// Function to get correct code for type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string ParseType(Type type) {
			if(type == null)
				return null;
			if(generatorData.typesMap.ContainsKey(type)) {
				return generatorData.typesMap[type];
			}
			if(type is RuntimeType) {
				var runtimeType = type as RuntimeType;
				if(!generatePureScript) {
					RegisterUsingNamespace(RuntimeType.CompanyNamespace);
					if(runtimeType is RuntimeGraphType graphType) {
						if (graphType.target is IClassComponent) {
							return ParseType(typeof(RuntimeComponent));
						} else if(graphType.target is IClassAsset) {
							return ParseType(typeof(BaseRuntimeAsset));
						}
					} else if(runtimeType is RuntimeGraphInterface) {
						return ParseType(typeof(IRuntimeClass));
					}
					throw new InvalidOperationException();
				} else if(setting.fullTypeName) {
					return runtimeType.FullName;
				}
				if (setting.nameSpace != type.Namespace) {
					RegisterUsingNamespace(type.Namespace);
				}
				return runtimeType.Name;
			}
			if(type.IsGenericType) {
				if(type.GetGenericTypeDefinition() == typeof(Nullable<>)) {
					string str = string.Format("{0}?", ParseType(Nullable.GetUnderlyingType(type)));
					generatorData.typesMap.Add(type, str);
					return str;
				} else {
					string typeName = type.GetGenericTypeDefinition().FullName.Replace("+", ".").Split('`')[0];
					if(!setting.fullTypeName && setting.usingNamespace.Contains(type.Namespace)) {
						string result = typeName.Remove(0, type.Namespace.Length + 1);
						string firstName = result.Split('.')[0];
						bool flag = false;
						generatorData.ValidateTypes(type.Namespace, setting.usingNamespace, t => {
							if(t.IsGenericType && t.GetGenericArguments().Length == type.GetGenericArguments().Length && t.Name.Equals(firstName)) {
								flag = true;
								return true;
							}
							return false;
						});
						if(!flag) {
							typeName = result;
						}
					}
					string str = string.Format("{0}<{1}>", typeName, string.Join(", ", type.GetGenericArguments().Select(a => ParseType(a)).ToArray()));
					generatorData.typesMap.Add(type, str);
					return str;
				}
			} else if(type.IsValueType || type == typeof(string) || type == typeof(object)) {
				if(type == typeof(string)) {
					return "string";
				} else if(type == typeof(bool)) {
					return "bool";
				} else if(type == typeof(float)) {
					return "float";
				} else if(type == typeof(int)) {
					return "int";
				} else if(type == typeof(short)) {
					return "short";
				} else if(type == typeof(long)) {
					return "long";
				} else if(type == typeof(double)) {
					return "double";
				} else if(type == typeof(decimal)) {
					return "decimal";
				} else if(type == typeof(byte)) {
					return "byte";
				} else if(type == typeof(uint)) {
					return "uint";
				} else if(type == typeof(ulong)) {
					return "ulong";
				} else if(type == typeof(ushort)) {
					return "ushort";
				} else if(type == typeof(char)) {
					return "char";
				} else if(type == typeof(sbyte)) {
					return "sbyte";
				} else if(type == typeof(void)) {
					return "void";
				} else if(type == typeof(object)) {
					return "object";
				}
			} else if(type.IsArray) {
				string str = ParseType(type.GetElementType()) + "[]";
				generatorData.typesMap.Add(type, str);
				return str;
			}
			if(string.IsNullOrEmpty(type.FullName)) {
				string str = type.Name;
				generatorData.typesMap.Add(type, str);
				return str;
			}
			if(setting.fullTypeName) {
				string str = type.FullName.Replace("+", ".");
				generatorData.typesMap.Add(type, str);
				return str;
			}
			if(setting.usingNamespace.Contains(type.Namespace)) {
				string result = type.FullName.Replace("+", ".").Remove(0, type.Namespace.Length + 1);
				string firstName = result.Split('.')[0];
				generatorData.ValidateTypes(type.Namespace, setting.usingNamespace, t => {
					if(t.Name.Equals(firstName)) {
						result = type.FullName.Replace("+", ".");
						return true;
					}
					return false;
				});
				generatorData.typesMap.Add(type, result);
				return result;
			}
			string r = type.FullName.Replace("+", ".");
			generatorData.typesMap.Add(type, r);
			return r;
		}

		/// <summary>
		/// Function to get correct code for type
		/// </summary>
		/// <param name="FullTypeName"></param>
		/// <returns></returns>
		public static string ParseType(string FullTypeName) {
			if(!string.IsNullOrEmpty(FullTypeName)) {
				Type type = TypeSerializer.Deserialize(FullTypeName, false);
				if(type != null) {
					return ParseType(type);
				}
			}
			return FullTypeName;
		}

		/// <summary>
		/// Function to get correct code for type
		/// </summary>
		/// <param name="FullTypeName"></param>
		/// <returns></returns>
		public static string ParseType(MemberData variable) {
			if(!object.ReferenceEquals(variable, null)) {
				if(variable.isAssigned) {
					if(variable.targetType == MemberData.TargetType.Type) {
						object o = variable.Get();
						if(o is Type) {
							return ParseType(o as Type);
						}
						if(variable.SerializedItems?.Length > 0) {
							string data = null;
							string[] gType;
							string[] pType;
							MemberDataUtility.GetItemName(SerializerUtility.Deserialize<MemberData.ItemData>(
										variable.SerializedItems[0]),
										variable.targetReference,
										out gType,
										out pType);
							if(gType.Length > 0) {
								data += String.Format("{0}", String.Join(", ", gType));
							}
							return data;
						}
					} else if(variable.targetType == MemberData.TargetType.Null) {
						return "null";
					} else if(variable.targetType == MemberData.TargetType.uNodeGenericParameter) {
						if(variable.SerializedItems.Length > 0) {
							string data = null;
							string[] gType;
							string[] pType;
							MemberDataUtility.GetItemName(SerializerUtility.Deserialize<MemberData.ItemData>(
										variable.SerializedItems[0]),
										variable.targetReference,
										out gType,
										out pType);
							if(gType.Length > 0) {
								data += String.Format("{0}", String.Join(", ", gType));
							}
							return data;
						}
						return variable.name;
					} else if(variable.targetType == MemberData.TargetType.uNodeParameter) {
						return ParseType(variable.type);
					} else if(variable.targetType == MemberData.TargetType.NodeField) {
						return ParseType(variable.type);
					} else if(variable.targetType == MemberData.TargetType.NodeFieldElement) {
						return ParseType(variable.type);
					} else if(variable.targetType == MemberData.TargetType.uNodeType) {
						return ParseType(variable.type);
					} else {
						throw new System.Exception("Unsupported target type for parse to type");
					}
				} else {
					throw new System.Exception("Unassigned variable");
				}
			}
			return null;
		}
		#endregion

		#region Parse Value
		/// <summary>
		/// Are the member can be generate.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static bool CanParseValue(MemberData member) {
			if(!object.ReferenceEquals(member, null)) {
				if(member.isAssigned) {
					if(member.isStatic) {
						return true;
					} else if(graph != null && member.GetInstance() is uNodeRoot && (member.GetInstance() as uNodeRoot) == graph) {
						return true;
					} else if(graph != null && member.GetInstance() is INode<uNodeRoot> && (member.GetInstance() as INode<uNodeRoot>).GetOwner() == graph) {
						return true;
					} else if(member.targetType == MemberData.TargetType.Constructor) {
						return true;
					} else if(member.targetType == MemberData.TargetType.Method) {
						return true;
					} else if(member.targetType == MemberData.TargetType.Field) {
						return true;
					} else if(member.targetType == MemberData.TargetType.Property) {
						return true;
					} else if(member.targetType == MemberData.TargetType.Type) {
						return true;
					} else if(member.targetType == MemberData.TargetType.Null) {
						return true;
					} else if(member.targetType == MemberData.TargetType.None) {
						return true;
					} else if(member.targetType == MemberData.TargetType.ValueNode) {
						return true;
					} else if(member.targetType == MemberData.TargetType.SelfTarget) {
						return true;
					} else if(member.targetType == MemberData.TargetType.uNodeParameter) {
						return true;
					} else if(member.targetType == MemberData.TargetType.uNodeGenericParameter) {
						return true;
					} else if(member.targetType == MemberData.TargetType.FlowNode) {
						return true;
					} else if(member.targetType == MemberData.TargetType.uNodeFunction) {
						return true;
					} else if(member.targetType == MemberData.TargetType.FlowInput) {
						return true;
					}
				}
			}
			return false;
		}

		private static string GenerateGetRuntimeInstance(object instance, RuntimeType runtimeType) {
			RegisterUsingNamespace("MaxyGames");
			if(generatePureScript) {
				if(instance == null) {
					return DoGenerateInvokeCode(
						nameof(Extensions.ToRuntimeInstance),
						new string[0], 
						new string[] { runtimeType.Name }
					).RemoveSemicolon();
				}
				return ParseValue(instance).Access(DoGenerateInvokeCode(
					nameof(Extensions.ToRuntimeInstance),
					new string[0], 
					new string[] { runtimeType.Name })
				).RemoveSemicolon();
			} else {
				//Type type = typeof(IRuntimeClass);
				//if(runtimeType is RuntimeGraphType graphType) {
				//	if(graphType.target is IClassComponent) {
				//		type = typeof(RuntimeComponent);
				//	} else if(graphType.target is IClassAsset) {
				//		type = typeof(BaseRuntimeAsset);
				//	}
				//} else if(runtimeType is RuntimeGraphInterface) {
				//	type = typeof(IRuntimeClass);
				//}
				if(instance == null) {
					return DoGenerateInvokeCode(
						nameof(Extensions.ToRuntimeInstance),
						new string[] { runtimeType.Name.AddFirst(_runtimeInterfaceKey, runtimeType.IsInterface).ParseValue() }
					).RemoveSemicolon();
				}
				return ParseValue(instance).Access(
					DoGenerateInvokeCode(
						nameof(Extensions.ToRuntimeInstance),
						new string[] { runtimeType.Name.AddFirst(_runtimeInterfaceKey, runtimeType.IsInterface).ParseValue() }
					)
				).RemoveSemicolon();
			}
		}

		private static string GenerateGetGeneratedComponent(object instance, RuntimeType runtimeType) {
			if(!runtimeType.IsSubclassOf(typeof(Component))) {
				return GenerateGetRuntimeInstance(instance, runtimeType);
			}
			if(generatePureScript) {
				if(instance == null) {
					return DoGenerateInvokeCode(
						nameof(uNodeHelper.GetGeneratedComponent),
						new string[0], 
						new string[] { runtimeType.Name }
					).RemoveSemicolon();
				}
				return ParseValue(instance).Access(
					DoGenerateInvokeCode(
						nameof(uNodeHelper.GetGeneratedComponent),
						new string[0], 
						new string[] { runtimeType.Name }
					)
				).RemoveSemicolon();
			} else {
				RegisterUsingNamespace("MaxyGames");
				if (instance == null) {
					return DoGenerateInvokeCode(
						nameof(uNodeHelper.GetGeneratedComponent),
						new string[] { runtimeType.Name.AddFirst(_runtimeInterfaceKey, runtimeType.IsInterface).ParseValue()
					}).RemoveSemicolon();
				}
				return ParseValue(instance).InvokeCode(
					nameof(uNodeHelper.GetGeneratedComponent),
					runtimeType.Name.AddFirst(_runtimeInterfaceKey, runtimeType.IsInterface).ParseValue()
				).RemoveSemicolon();
			}
		}

		private static string GenerateGetRuntimeVariable(RuntimeField field) {
			if(generatePureScript) {
				return field.Name;
			} else {
				return DoGenerateInvokeCode(nameof(RuntimeComponent.GetVariable), new string[] { field.Name.ParseValue() }, new Type[] { field.FieldType }).RemoveSemicolon();
			}
		}

		private static string GenerateGetRuntimeProperty(RuntimeProperty property) {
			if(generatePureScript) {
				return property.Name;
			} else {
				return DoGenerateInvokeCode(nameof(RuntimeComponent.GetProperty), new string[] { property.Name.ParseValue() }, new Type[] { property.PropertyType }).RemoveSemicolon();
			}
		}

		private static string GenerateInvokeRuntimeMethod(RuntimeMethod method, MemberData[] parameters, ref string enter, ref string exit, bool autoConvert = false) {
			var paramInfo = method.GetParameters();
			string data = string.Empty;
			if(paramInfo.Length > 0) {
				List<string> dataList = new List<string>();
				for(int index = 0; index < paramInfo.Length; index++) {
					MemberData p = parameters[index];
					string pData = null;
					if(paramInfo[index].IsOut) {
						pData += "out ";
					} else if(paramInfo[index].ParameterType.IsByRef) {
						pData += "ref ";
					}
					if(pData != null) {
						bool correct = true;
						if(p.type != null && p.type.IsValueType) {
							MemberInfo[] MI = p.GetMembers();
							if(MI != null && MI.Length > 1 && ReflectionUtils.GetMemberType(MI[MI.Length - 2]).IsValueType) {
								string varName = GenerateVariableName("tempVar");
								var pVal = ParseValue((object)p);
								pData += varName + "." + pVal.Remove(pVal.IndexOf(ParseStartValue(p)), ParseStartValue(p).Length + 1).SplitMember().Last();
								if(pVal.LastIndexOf(".") >= 0) {
									pVal = pVal.Remove(pVal.LastIndexOf("."));
								}
								enter += ParseType(ReflectionUtils.GetMemberType(MI[MI.Length - 2])) + " " + varName + " = " + pVal + ";\n";
								exit += pVal + " = " + varName + ";";
								correct = false;
							}
						}
						if(correct) {
							if(debugScript && setting.debugValueNode) {
								setting.debugScript = false;
								pData += ParseValue((object)p);
								setting.debugScript = true;
							} else {
								pData += ParseValue((object)p);
							}
						}
					} else {
						pData += ParseValue((object)p);
					}
					dataList.Add(pData);
				}
				for(int index = 0; index < dataList.Count; index++) {
					if(index != 0) {
						data += ", ";
					}
					data += dataList[index];
				}
			}
			if(generatePureScript) {
				return method.Name + "(" + data + ")";
			} else {
				RegisterUsingNamespace("MaxyGames");
				if(paramInfo.Length == 0) {
					var result = DoGenerateInvokeCode(
						nameof(RuntimeComponent.InvokeFunction),
						new string[] {
							method.Name.ParseValue(),
							"null"
						})
						.RemoveSemicolon();
					if(autoConvert) {
						result = result.ConvertCode(method.ReturnType, true);
					}
					return result;
				} else {
					string paramValues = GenerateMakeArray(typeof(object), data);
					var paramTypes = paramInfo.Select(p => p.ParameterType).ToArray();
					var result = DoGenerateInvokeCode(
						nameof(RuntimeComponent.InvokeFunction), 
						new string[] {
							method.Name.ParseValue(),
							paramTypes.ParseValue(),
							paramValues })
						.RemoveSemicolon();
					if(autoConvert) {
						result = result.ConvertCode(method.ReturnType, true);
					}
					return result;
				}
			}
		}

		/// <summary>
		/// Return full name of the member.
		/// Note: this function is still unfinished.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static string ParseNameofMember(MemberData member) {
			string name = ParseStartValue(member);
			var path = member.namePath;
			for(int i = 1; i < path.Length; i++) {
				name += "." + path[i];
			}
			return name;
		}

		/// <summary>
		/// Return start name of member.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static string ParseStartValue(MemberData member) {
			if(!object.ReferenceEquals(member, null)) {
				if(member.isAssigned) {
					if(member.isStatic) {
						var type = member.startType;
						if (type is RuntimeGraphType graphType && graphType.IsSingleton) {
							if (generatePureScript) {
								return typeof(uNodeSingleton).InvokeCode(nameof(uNodeSingleton.GetInstance), new Type[] { type }, null).RemoveSemicolon();
							} else {
								return typeof(uNodeSingleton).InvokeCode(nameof(uNodeSingleton.GetGraphInstance), graphType.FullName.ParseValue()).RemoveSemicolon();
							}
						}
						return ParseType(type);
					} else if(member.targetType == MemberData.TargetType.uNodeParameter) {
						return member.startName;
					} else if(member.targetType == MemberData.TargetType.uNodeGenericParameter) {
						return "typeof(" + member.startName + ")";
					} else if(member.targetType == MemberData.TargetType.uNodeVariable) {
						VariableData ESV = member.GetVariable();
						if(ESV != null) {
							return GetVariableName(ESV);
						}
					} else if(member.targetType == MemberData.TargetType.uNodeProperty) {
						uNodeRoot UNR = member.startTarget as uNodeRoot;
						if(UNR != null) {
							return UNR.GetPropertyData(member.startName).Name;
						}
					} else if(member.targetType == MemberData.TargetType.uNodeLocalVariable) {
						RootObject RO = member.startTarget as RootObject;
						if(RO != null) {
							if(isInUngrouped) {
								VariableData v = RO.GetLocalVariableData(member.startName);
								return AddVariable(v);
							}
							return GetVariableName(RO.GetLocalVariableData(member.startName));
						}
					} else if(member.targetType == MemberData.TargetType.NodeField) {
						member.GetMembers();
						if(member.startTarget != null && member.fieldInfo != null) {
							return GetVariableName(member.startTarget, member.fieldInfo);
						}
					} else if(member.targetType == MemberData.TargetType.NodeFieldElement) {
						member.GetMembers();
						if(member.startTarget != null && member.fieldInfo != null) {
							return GetVariableName(member.startTarget, member.fieldInfo, int.Parse(member.startName.Split('#')[1]));
						}
					} else if(member.targetType == MemberData.TargetType.ValueNode) {
						if(debugScript && setting.debugValueNode) {
							Type targetType = member.startType;
							if(!generatorData.debugMemberMap.ContainsKey(member)) {
								int randomNum = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
								while(generatorData.debugMemberMap.Any((x) => x.Value.Key == randomNum)) {
									randomNum = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
								}
								generatorData.debugMemberMap.Add(member, new KeyValuePair<int, string>(randomNum,
									GenerateDebugCode(member, "debugValue").AddLineInFirst() +
									("return debugValue;").AddLineInFirst()
								));
							}
							return _debugGetValueCode + "(" +
								generatorData.debugMemberMap[member].Key + ", " +
								GenerateNode(member.GetTargetNode(), true) + ")";
						}
						return GenerateNode(member.GetTargetNode(), true);
					} else if(member.GetInstance() is uNodeRoot root && root == graph) {
						if(member.startType is RuntimeType runtimeType) {
							var runtimeInstance = ReflectionUtils.GetActualTypeFromInstance(member.instance, true);
							if(runtimeType is RuntimeGraphType) {
								if (runtimeInstance != runtimeType) {
									return "this".Access(GenerateGetGeneratedComponent(null, runtimeType));
								}
							} else if(runtimeType is RuntimeGraphInterface) {
								if (runtimeType != runtimeInstance && !runtimeInstance.IsCastableTo(runtimeType)) {
									return "this".Access(GenerateGetGeneratedComponent(null, runtimeType));
									// if(runtimeInstance == typeof(GameObject) || runtimeInstance.IsCastableTo(typeof(Component))) {
									// } else {
									// 	throw new Exception($"Cannot convert type from: '{runtimeType.FullName}' to '{runtimeInstance.FullName}'");
									// }
								}
							} else {
								throw new Exception($"Unsupported RuntimeType: {runtimeType.FullName}");
							}
						}
						switch(member.targetType) {
							case MemberData.TargetType.Constructor:
							case MemberData.TargetType.Event:
							case MemberData.TargetType.Field:
							case MemberData.TargetType.Method:
							case MemberData.TargetType.Property:
								return "base";
							default:
								return "this";
						}
					} else if(member.IsTargetingVariable) {
						object instance = member.GetInstance();
						if(instance is IVariableSystem) {
							VariableData varData = (instance as IVariableSystem).GetVariableData(member.startName);
							if(varData != null) {
								return GetVariableName(varData);
							}
						} else if(instance is ILocalVariableSystem) {
							VariableData varData = (instance as ILocalVariableSystem).GetLocalVariableData(member.startName);
							if(varData != null) {
								return GetVariableName(varData);
							}
						}
					} else if(member.instance != null) {
						string result = ParseValue(member.instance);
						if (member.startType is RuntimeType runtimeType) {
							if(runtimeType is RuntimeGraphType) {
								var runtimeInstance = ReflectionUtils.GetActualTypeFromInstance(member.instance, true);
								if (runtimeType != runtimeInstance) {
									return result.Access(GenerateGetGeneratedComponent(null, runtimeType));
								}
							} else if(runtimeType is RuntimeGraphInterface) {
								var runtimeInstance = ReflectionUtils.GetActualTypeFromInstance(member.instance, true);
								if (runtimeType != runtimeInstance && !runtimeInstance.IsCastableTo(runtimeType)) {
									return result.Access(GenerateGetGeneratedComponent(null, runtimeType));
									// if(runtimeInstance == typeof(GameObject) || runtimeInstance.IsCastableTo(typeof(Component))) {
									// } else {
									// 	throw new Exception($"Cannot convert type from: '{runtimeType.FullName}' to '{runtimeInstance.FullName}'");
									// }
								}
							} else {
								throw new Exception($"Unsupported RuntimeType: {runtimeType.FullName}");
							}
						}
						if(result == "this") {
							switch(member.targetType) {
								case MemberData.TargetType.Constructor:
								case MemberData.TargetType.Event:
								case MemberData.TargetType.Field:
								case MemberData.TargetType.Method:
								case MemberData.TargetType.Property:
									return "base";
							}
						}
						return result;
					}
				}
			}
			return null;
		}

		/// <summary>
		/// Function to generate correct code for ValueGetter
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="storeValue"></param>
		/// <returns></returns>
		public static string TryParseValue(MultipurposeMember variable, MemberData storeValue = null, Action<string, string> onEnterAndExit = null, bool autoConvert = false) {
			string resultCode = GetCorrectName(variable.target, variable.parameters, variable.initializer, onEnterAndExit, autoConvert);
			if(!string.IsNullOrEmpty(resultCode)) {
				if(storeValue != null && storeValue.isAssigned && variable.target.type != typeof(void) && CanParseValue(variable.target)) {
					if(resultCode.Contains(" = ")) {
						if(IsContainOperatorCode(variable.target.name)) {
							return GetCorrectName(storeValue) + " = (" + resultCode + ")";
						}
						return GetCorrectName(storeValue) + " = (" + resultCode + ")";
					}
					if(IsContainOperatorCode(variable.target.name)) {
						return GetCorrectName(storeValue) + " = " + resultCode;
					}
					return GetCorrectName(storeValue) + " = " + resultCode;
				}
				if(IsContainOperatorCode(variable.target.name)) {
					throw new System.Exception("unsupported generating operator code in the current context");
				}
				if((variable.target.targetType == MemberData.TargetType.uNodeGenericParameter ||
					variable.target.targetType == MemberData.TargetType.Type) && !resultCode.StartsWith("typeof(")) {
					resultCode = "typeof(" + resultCode + ")";
				}
				return resultCode;
			} else if(storeValue != null && CanParseValue(storeValue)) {
				return GenerateSetCode(storeValue, variable.target).RemoveLast();
			} else {
				return ParseValue((object)variable.target);
			}
		}

		/// <summary>
		/// Function to generate correct code for MemberReflection
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		public static string ParseValue(MemberData member, MemberData[] parameters = null, bool setVariable = false, bool autoConvert = false) {
			if(!object.ReferenceEquals(member, null)) {
				if(member.isTargeted) {
					if(member.targetType == MemberData.TargetType.None || member.targetType == MemberData.TargetType.Type) {
						object o = member.Get();
						if(o is Type) {
							return "typeof(" + ParseType(o as Type) + ")";
						}
					} else if(member.targetType == MemberData.TargetType.Null) {
						return "null";
					} else if(member.targetType == MemberData.TargetType.uNodeGenericParameter) {
						return "typeof(" + member.name + ")";
					} else if(member.targetType == MemberData.TargetType.FlowNode || member.targetType == MemberData.TargetType.FlowInput) {
						throw new Exception("Flow target type need to generated from GenerateFlowCode()");
					} else if(member.targetType == MemberData.TargetType.Constructor) {
						return "new " + ParseType(member.type) + "()";
					} else if(member.targetType == MemberData.TargetType.Values) {
						return ParseValue(member.Get());
					} else if(member.targetType == MemberData.TargetType.uNodeFunction) {
						string data = member.startName;
						string[] gType;
						string[] pType;
						MemberDataUtility.GetItemName(SerializerUtility.Deserialize<MemberData.ItemData>(
									member.SerializedItems[0]),
									member.targetReference,
									out gType,
									out pType);
						if(gType.Length > 0) {
							data += String.Format("<{0}>", String.Join(", ", gType));
						}
						data += "()";
					}
					if(member.isStatic) {
						return GetCorrectName(member, parameters, autoConvert: autoConvert);
						//string result = CSharpGenerator.ParseType(variable.startTypeName);
						//string[] str = GetCorrectName(variable).Split(new char[] { '.' });
						//for(int i = 0; i < str.Length; i++) {
						//	if(i == 0)
						//		continue;
						//	result += "." + str[i];
						//}
						//return result;
					} else if(member.targetType == MemberData.TargetType.ValueNode) {
						if(setVariable) {
							var tNode = member.GetTargetNode() as MultipurposeNode;
							if(tNode != null) {
								return ParseValue(tNode.target.target, tNode.target.parameters, setVariable:setVariable);
							}
							if(debugScript && setting.debugValueNode) {
								setting.debugScript = false;
								var result = GetCorrectName(member, parameters, autoConvert:autoConvert);
								setting.debugScript = true;
								return result;
							}
						}
						return GetCorrectName(member, parameters, autoConvert: autoConvert);
					} else if(member.IsTargetingVariable) {
						if(!member.isDeepTarget) {
							return ParseStartValue(member);
						}
						VariableData variable = member.GetVariable();
						if (variable != null) {
							return GetCorrectName(member, parameters, autoConvert: autoConvert);
						}
						throw new Exception("Variable not found: " + member.startName);
					} else if(member.GetInstance() is UnityEngine.Object) {
						if(member.targetType == MemberData.TargetType.uNodeVariable ||
							member.targetType == MemberData.TargetType.uNodeProperty ||
							member.targetType == MemberData.TargetType.uNodeParameter ||
							member.targetType == MemberData.TargetType.uNodeLocalVariable ||
							member.targetType == MemberData.TargetType.uNodeGenericParameter ||
							member.targetType == MemberData.TargetType.uNodeGroupVariable ||
							member.targetType == MemberData.TargetType.Property ||
							member.targetType == MemberData.TargetType.uNodeFunction ||
							member.targetType == MemberData.TargetType.Field ||
							member.targetType == MemberData.TargetType.Constructor ||
							member.targetType == MemberData.TargetType.Method ||
							member.targetType == MemberData.TargetType.Event) {
							return GetCorrectName(member, parameters, autoConvert: autoConvert);
						}
						if(graph is uNodeRuntime ||
							graph is uNodeClass && typeof(UnityEngine.Object).IsAssignableFrom(graph.GetInheritType())) {
							UnityEngine.Object obj = member.GetInstance() as UnityEngine.Object;
							if(obj is Transform && graph.transform == obj) {
								return GetCorrectName(member, parameters, autoConvert: autoConvert);
							} else if(obj is GameObject && graph.gameObject == obj) {
								return GetCorrectName(member, parameters, autoConvert: autoConvert);
							}
						}
						if(graph == member.GetInstance() as UnityEngine.Object) {
							return ParseValue(member.GetInstance(), autoConvert: autoConvert);
						}
						if(setting.resolveUnityObject) {
							return GetCorrectName(member, parameters, autoConvert: autoConvert);
						}
					} else if(member.instance == null) {
						return "null";
					} else {
						return GetCorrectName(member, parameters, autoConvert: autoConvert);
					}
					throw new Exception("Unsupported target reference: " + member.GetInstance().GetType());
				} else {
					throw new Exception("The value is un-assigned");
				}
			} else {
				throw new ArgumentNullException(nameof(member));
			}
		}

		/// <summary>
		/// Function to generate code for any object
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="initializer"></param>
		/// <returns></returns>
		public static string ParseValue(object obj, ParameterValueData[] initializer = null, bool autoConvert = false, bool setVariable = false) {
			if(object.ReferenceEquals(obj, null))
				return "null";
			if(obj is Type) {
				return "typeof(" + ParseType(obj as Type) + ")";
			} else if(obj is MemberData) {
				return ParseValue(obj as MemberData, setVariable:setVariable, autoConvert:autoConvert);
			} else if(obj is MultipurposeMember) {
				//return TryParseValue(obj as MemberInvoke);
				string header = null;
				string footer = null;
				var rezult = TryParseValue(obj as MultipurposeMember, null, (x, y) => {
					if(!string.IsNullOrEmpty(x)) {
						header += x.AddLineInEnd();
					}
					if(!string.IsNullOrEmpty(y)) {
						footer += y.AddLineInFirst();
					}
				}, autoConvert);
				return header + rezult + footer;
			} else if(obj is UnityEngine.Object) {
				UnityEngine.Object o = obj as UnityEngine.Object;
				if(o == graph) {
					return "this";
				}
				if(o != null) {
					if(generatorData.state.isStatic || generatorData.state.state == State.Classes) {
						return "null";
					}
					Type inherithType = graph.GetInheritType();
					if(graph is uNodeRuntime || graph is uNodeClass && (inherithType.IsCastableTo(typeof(GameObject)) || inherithType.IsCastableTo(typeof(Component)))) {
						if(o is GameObject) {
							GameObject g = o as GameObject;
							if(g == graph.gameObject) {
								return "this.gameObject";
							}
						} else if(o is Transform) {
							Transform g = o as Transform;
							if(g == graph.transform) {
								return "this.transform";
							}
						} else if(o is Component) {
							Component c = o as Component;
							if(c.gameObject == graph.gameObject) {
								return "this.GetComponent<" + ParseType(c.GetType()) + ">()";
							}
						}
					}
					if(setting.resolveUnityObject) {
						if(!generatorData.unityVariableMap.ContainsKey(o)) {
							Type objType = o.GetType();
							if(o is uNodeAssetInstance asset) {
								if(generatePureScript) {
									objType = ReflectionUtils.GetRuntimeType(o);
								} else {
									objType = typeof(BaseRuntimeAsset);
								}
							} 
							//else if(o is uNodeSpawner comp) {

							//}
							string varName = AddVariable(
								new VariableData("objectVariable", objType) { modifier = new FieldModifier() { Public = true } });
							generatorData.unityVariableMap.Add(o, varName);
							graph.graphData.unityObjects.Add(new GraphData.ObjectData(){
								name = varName,
								value = o,
							});
						}
						return generatorData.unityVariableMap[o];
					}
				}
				return "null";
			} else if(obj is LayerMask) {
				return ParseValue(((LayerMask)obj).value);
			} else if(obj is ObjectValueData) {
				return ParseValue((obj as ObjectValueData).value);
			} else if(obj is ParameterValueData) {
				return ParseValue((obj as ParameterValueData).value);
			} else if(obj is ConstructorValueData) {
				var val = obj as ConstructorValueData;
				Type t = val.type;
				if(t != null) {
					string pVal = null;
					if(val.parameters != null) {
						for(int i = 0; i < val.parameters.Length; i++) {
							string p = ParseValue(val.parameters[i]);
							if(!string.IsNullOrEmpty(pVal)) {
								pVal += ", ";
							}
							pVal += p;
						}
					}
					string data = "new " + ParseType(t) + "(" + pVal + ")";
					if(val.initializer != null && val.initializer.Length > 0) {
						data += " { ";
						bool isFirst = true;
						foreach(var param in initializer) {
							if(!isFirst) {
								data += ", ";
							}
							data += param.name + " = " + ParseValue(param.value);
							isFirst = false;

						}
						data += " }";
					}
					return data;
				}
				return "null";
			} else if(obj is BaseValueData) {
				throw new System.Exception("Unsupported Value Data:" + obj.GetType());
			} else if(obj is VariableData) {
				return GetVariableName(obj as VariableData);
			} else if(obj is StringWrapper) {
				return (obj as StringWrapper).value;
			}
			Type type = obj.GetType();
			if(type.IsValueType || type == typeof(string)) {
				if(obj is string) {
					return "\"" + StringHelper.StringLiteral(obj.ToString()) + "\"";
				} else if(obj is float) {
					return obj.ToString().Replace(',', '.') + "F";
				} else if(obj is int) {
					return obj.ToString();
				} else if(obj is uint) {
					return obj.ToString() + "U";
				} else if(obj is short) {
					return "(" + ParseType(typeof(short)) + ")" + obj.ToString();
				} else if(obj is ushort) {
					return "(" + ParseType(typeof(ushort)) + ")" + obj.ToString();
				} else if(obj is long) {
					return obj.ToString() + "L";
				} else if(obj is ulong) {
					return obj.ToString() + "UL";
				} else if(obj is byte) {
					return "(" + ParseType(typeof(byte)) + ")" + obj.ToString();
				} else if(obj is sbyte) {
					return "(" + ParseType(typeof(sbyte)) + ")" + obj.ToString();
				} else if(obj is double) {
					return obj.ToString().Replace(',', '.') + "D";
				} else if(obj is decimal) {
					return obj.ToString().Replace(',', '.') + "M";
				} else if(obj is bool) {
					return obj.ToString().ToLower();
				} else if(obj is char) {
					return "'" + obj.ToString() + "'";
				} else if(obj is Enum) {
					return ParseType(obj.GetType()) + "." + obj.ToString();
				} else if(obj is Vector2) {
					var val = (Vector2)obj;
					if(initializer == null || initializer.Length == 0) {
						if(val == Vector2.zero) {
							return ParseType(typeof(Vector2)) + ".zero";
						}
						if(val == Vector2.up) {
							return ParseType(typeof(Vector2)) + ".up";
						}
						if(val == Vector2.down) {
							return ParseType(typeof(Vector2)) + ".down";
						}
						if(val == Vector2.left) {
							return ParseType(typeof(Vector2)) + ".left";
						}
						if(val == Vector2.right) {
							return ParseType(typeof(Vector2)) + ".right";
						}
						if(val == Vector2.one) {
							return ParseType(typeof(Vector2)) + ".one";
						}
						return "new " + ParseType(typeof(Vector2)) + "(" + val.x + "f, " + val.y + "f)";
					}
				} else if(obj is Vector3) {
					if(initializer == null || initializer.Length == 0) {
						var val = (Vector3)obj;
						if(val == Vector3.zero) {
							return ParseType(typeof(Vector3)) + ".zero";
						} else if(val == Vector3.up) {
							return ParseType(typeof(Vector3)) + ".up";
						} else if(val == Vector3.down) {
							return ParseType(typeof(Vector3)) + ".down";
						} else if(val == Vector3.left) {
							return ParseType(typeof(Vector3)) + ".left";
						} else if(val == Vector3.right) {
							return ParseType(typeof(Vector3)) + ".right";
						} else if(val == Vector3.one) {
							return ParseType(typeof(Vector3)) + ".one";
						} else if(val == Vector3.forward) {
							return ParseType(typeof(Vector3)) + ".forward";
						} else if(val == Vector3.back) {
							return ParseType(typeof(Vector3)) + ".back";
						}
						return "new " + ParseType(typeof(Vector3)) + "(" + val.x + "f, " + val.y + "f, " + val.z + "f)";
					}
				} else if(obj is Vector4) {
					if(initializer == null || initializer.Length == 0) {
						var val = (Vector4)obj;
						if(val == Vector4.zero) {
							return ParseType(typeof(Vector4)) + ".zero";
						} else if(val == Vector4.one) {
							return ParseType(typeof(Vector4)) + ".one";
						}
					}
				} else if(obj is Color) {
					if(initializer == null || initializer.Length == 0) {
						var val = (Color)obj;
						if(val == Color.white) {
							return ParseType(typeof(Color)) + ".white";
						} else if(val == Color.black) {
							return ParseType(typeof(Color)) + ".black";
						} else if(val == Color.blue) {
							return ParseType(typeof(Color)) + ".blue";
						} else if(val == Color.clear) {
							return ParseType(typeof(Color)) + ".clear";
						} else if(val == Color.cyan) {
							return ParseType(typeof(Color)) + ".cyan";
						} else if(val == Color.gray) {
							return ParseType(typeof(Color)) + ".gray";
						} else if(val == Color.green) {
							return ParseType(typeof(Color)) + ".green";
						} else if(val == Color.magenta) {
							return ParseType(typeof(Color)) + ".magenta";
						} else if(val == Color.red) {
							return ParseType(typeof(Color)) + ".red";
						} else if(val == Color.yellow) {
							return ParseType(typeof(Color)) + ".yellow";
						}
					}
				}
			} else if(type.IsGenericType) {
				string elementObject = "";
				if(obj is IDictionary) {
					IDictionary dic = obj as IDictionary;
					if(dic != null && dic.Count > 0) {
						elementObject = " { ";
						int index = 0;
						foreach(DictionaryEntry o in dic) {
							if(index != 0) {
								elementObject += ", ";
							}
							elementObject += "{ " + ParseValue(o.Key) + ", " + ParseValue(o.Value) + " }";
							index++;
						}
						elementObject += " }";
					}
				} else if(obj is ICollection) {
					ICollection col = obj as ICollection;
					if(col != null && col.Count > 0) {
						elementObject = " { ";
						int index = 0;
						foreach(object o in col) {
							if(index != 0) {
								elementObject += ", ";
							}
							if(o is DictionaryEntry) {
								elementObject += "{ " + ParseValue(((DictionaryEntry)o).Key) + ", " + ParseValue(((DictionaryEntry)o).Value) + " }";
							} else {
								elementObject += ParseValue(o);
							}
							index++;
						}
						if(initializer != null && initializer.Length > 0) {
							foreach(var param in initializer) {
								if(index != 0) {
									elementObject += ", ";
								}
								elementObject += ParseValue(param.value);
								index++;
							}
						}
						elementObject += " }";
					}
				} else {
					IEnumerable val = obj as IEnumerable;
					if(val != null) {
						elementObject = " { ";
						int index = 0;
						foreach(object o in val) {
							if(index != 0) {
								elementObject += ", ";
							}
							if(o is DictionaryEntry) {
								elementObject += "{ " + ParseValue(((DictionaryEntry)o).Key) + ", " + ParseValue(((DictionaryEntry)o).Value) + " }";
							} else {
								elementObject += ParseValue(o);
							}
							index++;
						}
						if(initializer != null && initializer.Length > 0) {
							foreach(var param in initializer) {
								if(index != 0) {
									elementObject += ", ";
								}
								elementObject += ParseValue(param.value);
								index++;
							}
						}
						elementObject += " }";
						if(index == 0) {
							return "new " + ParseType(type) + "()";
						}
					}
				}
				return "new " + ParseType(type) + "()" + elementObject;
			} else if(type.IsArray) {
				string elementObject = "[0]";
				Array array = obj as Array;
				if(array != null && array.Length > 0) {
					int index = 0;
					elementObject = "[" + //array.Length + 
						"] {";
					foreach(object o in array) {
						if(index != 0) {
							elementObject += ",";
						}
						elementObject += " " + ParseValue(o);
						index++;
					}
					if(initializer != null && initializer.Length > 0) {
						foreach(var param in initializer) {
							if(index != 0) {
								elementObject += ", ";
							}
							elementObject += ParseValue(param.value);
							index++;
						}
					}
					elementObject += " }";
				}
				return "new " + ParseType(type.GetElementType()) + elementObject;
			} else if(obj is IEnumerable) {
				string elementObject = "";
				IEnumerable val = obj as IEnumerable;
				if(val != null) {
					elementObject = " { ";
					int index = 0;
					foreach(object o in val) {
						if(index != 0) {
							elementObject += ", ";
						}
						elementObject += ParseValue(o);
						index++;
					}
					if(initializer != null && initializer.Length > 0) {
						foreach(var param in initializer) {
							if(index != 0) {
								elementObject += ", ";
							}
							elementObject += ParseValue(param.value);
							index++;
						}
					}
					elementObject += " }";
					if(index == 0) {
						return "new " + ParseType(type) + "()";
					}
				}
				return "new " + ParseType(type) + "()" + elementObject;
			}
			if(ReflectionUtils.IsNullOrDefault(obj, type)) {
				if(!type.IsValueType && obj == null) {
					return "null";
				} else {
					string data = "new " + ParseType(type) + "()";
					if(initializer != null && initializer.Length > 0) {
						data += " { ";
						bool isFirst = true;
						foreach(var param in initializer) {
							if(!isFirst) {
								data += ", ";
							}
							data += param.name + " = " + ParseValue(param.value);
							isFirst = false;

						}
						data += " }";
					}
					return data;
				}
			} else if(obj is AnimationCurve) {
				var val = (AnimationCurve)obj;
				string data = "new " + ParseType(type) + "(";
				if(val.keys.Length > 0) {
					for(int i = 0; i < val.keys.Length; i++) {
						var key = val.keys[i];
						if(i != 0) {
							data += ", ";
						}
						data += ParseValue(key);
					}
				}
				data = data + ")";
				if(initializer != null && initializer.Length > 0) {
					data += " { ";
					bool isFirst = true;
					foreach(var param in initializer) {
						if(!isFirst) {
							data += ", ";
						}
						data += param.name + " = " + ParseValue(param.value);
						isFirst = false;

					}
					data += " }";
				}
				return data;
			} else if(type.IsValueType) {
				object clone = ReflectionUtils.CreateInstance(type);
				Dictionary<string, object> objMap = new Dictionary<string, object>();
				FieldInfo[] fields = ReflectionUtils.GetFields(obj);
				foreach(FieldInfo field in fields) {
					object fieldObj = field.GetValueOptimized(obj);
					if(field.FieldType.IsValueType) {
						object cloneObj = field.GetValueOptimized(clone);
						if(fieldObj.Equals(cloneObj))
							continue;
					}
					objMap.Add(field.Name, fieldObj);
				}
				PropertyInfo[] properties = ReflectionUtils.GetProperties(obj);
				foreach(PropertyInfo property in properties) {
					if(property.GetIndexParameters().Any()) {
						continue;
					}
					if(property.CanRead && property.CanWrite) {
						object fieldObj = property.GetValueOptimized(obj);
						if(property.PropertyType.IsValueType) {
							object cloneObj = property.GetValueOptimized(clone);
							if(fieldObj.Equals(cloneObj))
								continue;
						}
						objMap.Add(property.Name, fieldObj);
					}
				}
				string data = "new " + ParseType(type) + "()";
				if(objMap.Count > 0) {
					data += " { ";
					bool isFirst = true;
					if(initializer != null && initializer.Length > 0) {
						foreach(var param in initializer) {
							if(!isFirst) {
								data += ", ";
							}
							data += param.name + " = " + ParseValue(param.value);
							if(objMap.ContainsKey(param.name)) {
								objMap.Remove(param.name);
							}
							isFirst = false;

						}
					}
					foreach(KeyValuePair<string, object> pair in objMap) {
						if(!isFirst) {
							data += ", ";
						}
						data += pair.Key + " = " + ParseValue(pair.Value);
						isFirst = false;
					}
					data += " }";
				}
				return data;
			} else {
				ConstructorInfo[] ctor = type.GetConstructors();
				foreach(ConstructorInfo info in ctor) {
					if(info.GetParameters().Length == 0) {
						object clone = ReflectionUtils.CreateInstance(type);
						Dictionary<string, object> objMap = new Dictionary<string, object>();
						FieldInfo[] fields = ReflectionUtils.GetFields(obj);
						foreach(FieldInfo field in fields) {
							object fieldObj = field.GetValueOptimized(obj);
							if(field.FieldType.IsValueType) {
								object cloneObj = field.GetValueOptimized(clone);
								if(fieldObj.Equals(cloneObj))
									continue;
							}
							objMap.Add(field.Name, fieldObj);
						}
						PropertyInfo[] properties = ReflectionUtils.GetProperties(obj);
						foreach(PropertyInfo property in properties) {
							if(property.CanRead && property.CanWrite) {
								object fieldObj = property.GetValueOptimized(obj);
								if(property.PropertyType.IsValueType) {
									object cloneObj = property.GetValueOptimized(clone);
									if(fieldObj.Equals(cloneObj))
										continue;
								}
								objMap.Add(property.Name, fieldObj);
							}
						}
						string data = "new " + ParseType(type) + "()";
						if(objMap.Count > 0) {
							data += " { ";
							bool isFirst = true;
							if(initializer != null && initializer.Length > 0) {
								foreach(var param in initializer) {
									if(!isFirst) {
										data += ", ";
									}
									data += param.name + " = " + ParseValue(param.value);
									if(objMap.ContainsKey(param.name)) {
										objMap.Remove(param.name);
									}
									isFirst = false;

								}
							}
							foreach(KeyValuePair<string, object> pair in objMap) {
								if(!isFirst) {
									data += ", ";
								}
								data += pair.Key + " = " + ParseValue(pair.Value);
								isFirst = false;
							}
							data += " }";
						}
						return data;
					}
				}
			}
			return obj.ToString();
		}

		/// <summary>
		/// Parse Constructor initializer.
		/// </summary>
		/// <param name="initializer"></param>
		/// <returns></returns>
		public static string ParseConstructorInitializer(ValueData initializer) {
			string ctorInit = null;
			if(initializer != null && initializer.value as ConstructorValueData != null) {
				ConstructorValueData ctor = initializer.value as ConstructorValueData;
				if(ctor.initializer != null && ctor.initializer.Length > 0) {
					ctorInit += " { ";
					bool isFirst = true;
					foreach(var param in ctor.initializer) {
						if(!isFirst) {
							ctorInit += ", ";
						}
						ctorInit += param.name + " = " + ParseValue(param.value);
						isFirst = false;

					}
					ctorInit += " }";
				}
			}
			return ctorInit;
		}

		/// <summary>
		/// Function for generate code for attribute data.
		/// </summary>
		/// <param name="attribute"></param>
		/// <returns></returns>
		public static string TryParseAttribute(AData attribute) {
			if(attribute == null)
				return null;
			string parameters = null;
			if(attribute.attributeParameters != null) {
				foreach(string str in attribute.attributeParameters) {
					if(string.IsNullOrEmpty(str))
						continue;
					if(!string.IsNullOrEmpty(parameters)) {
						parameters += ", ";
					}
					parameters += str;
				}
			}
			string namedParameters = null;
			if(attribute.namedParameters != null && attribute.namedParameters.Count > 0) {
				foreach(var pain in attribute.namedParameters) {
					if(string.IsNullOrEmpty(pain.Value))
						continue;
					if(!string.IsNullOrEmpty(namedParameters)) {
						namedParameters += ", ";
					}
					namedParameters += pain.Key + " = " + pain.Value;
				}
			}
			string result = null;
			if(string.IsNullOrEmpty(parameters)) {
				if(string.IsNullOrEmpty(namedParameters)) {
					result = "[" + ParseType(attribute.attributeType) + "]";
				} else {
					result = "[" + ParseType(attribute.attributeType) + "(" + namedParameters + ")]";
				}
			} else {
				result = "[" + ParseType(attribute.attributeType) + "(" + parameters + namedParameters.AddFirst(", ", !string.IsNullOrEmpty(parameters)) + ")]";
			}
			return result;
		}

		/// <summary>
		/// Function for Convert AttributeData to AData
		/// </summary>
		/// <param name="attribute"></param>
		/// <returns></returns>
		public static AData TryParseAttributeData(AttributeData attribute) {
			if(attribute != null && attribute.type != null) {
				AData data = new AData();
				if(attribute.value != null && attribute.value.type != null) {
					data.attributeType = attribute.value.type;
					if(attribute.value.Value != null && attribute.value.Value is ConstructorValueData) {
						ConstructorValueData ctor = attribute.value.Value as ConstructorValueData;
						Type t = ctor.type;
						if(t != null) {
							if(ctor.parameters != null) {
								data.attributeParameters = new string[ctor.parameters.Length];
								for(int i = 0; i < ctor.parameters.Length; i++) {
									data.attributeParameters[i] = ParseValue(ctor.parameters[i]);
								}
							}
							data.attributeType = t;
							if(ctor.initializer != null && ctor.initializer.Length > 0) {
								if(data.namedParameters == null) {
									data.namedParameters = new Dictionary<string, string>();
								}
								foreach(var param in ctor.initializer) {
									data.namedParameters.Add(param.name, ParseValue(param.value));
								}
							}
						}
					} else {
						data.attributeType = attribute.type.Get<Type>();
					}
				} else {
					data.attributeType = attribute.type.Get<Type>();
				}
				return data;
			}
			return null;
		}
		#endregion
		
		#region Variable Functions
		/// <summary>
		/// Get variable data of variable.
		/// </summary>
		/// <param name="variable"></param>
		/// <returns></returns>
		public static VData GetVariable(VariableData variable) {
			foreach(VData vdata in generatorData.GetVariables()) {
				if(vdata.variableRef == variable) {
					return vdata;
				}
			}
			throw new System.Exception("no variable data found");
		}
		
		#region AddVariable
		/// <summary>
		/// Register new using namespaces
		/// </summary>
		/// <param name="nameSpace"></param>
		/// <returns></returns>
		public static bool RegisterUsingNamespace(string nameSpace) {
			return setting.usingNamespace.Add(nameSpace);
		}

		/// <summary>
		/// Register new script header like define symbol, pragma symbol or script copyright
		/// </summary>
		/// <param name="contents"></param>
		/// <returns></returns>
		public static bool RegisterScriptHeader(string contents) {
			return setting.scriptHeaders.Add(contents);
		}

		public static string AddVariable(VData variableData) {
			foreach(VData vdata in generatorData.GetVariables()) {
				if(vdata == variableData) {
					vdata.isInstance = true;
					return vdata.name;
				}
			}
			generatorData.AddVariable(variableData);
			return variableData.name;
		}

		public static string AddVariable(object from, FieldInfo field, bool isInstance = false) {
			return AddVariable(from, field, field.FieldType, isInstance);
		}

		public static string AddVariable(object from, FieldInfo field, Type type, bool isInstance = false) {
			if(from == null) {
				throw new ArgumentNullException("from");
			}
			if(field == null) {
				throw new ArgumentNullException("field");
			}
			foreach(VData vdata in generatorData.GetVariables()) {
				if(vdata.variableRef as object[] != null && (vdata.variableRef as object[])[0] == from && (vdata.variableRef as object[])[1] as FieldInfo == field) {
					if(isInstance)
						vdata.isInstance = true;
					return vdata.name;
				}
			}
			if(type == null) {
				throw new ArgumentNullException("type");
			}
			string name = GetVariableName(from, field);
			generatorData.AddVariable(new VData(from, field, type, name) {
				isInstance = isInstance,
				modifier = new FieldModifier() {
					Private = true,
					Public = false
				}
			});
			return name;
		}

		public static string AddVariable(object from, FieldInfo field, int index, bool isInstance = false) {
			return AddVariable(from, field, index, field.FieldType, isInstance);
		}

		public static string AddVariable(object from, FieldInfo field, int index, Type type, bool isInstance = false) {
			if(from == null) {
				throw new ArgumentNullException("from");
			}
			if(field == null) {
				throw new ArgumentNullException("field");
			}
			foreach(VData vdata in generatorData.GetVariables()) {
				if(vdata.variableRef as object[] != null && (vdata.variableRef as object[])[0] == from &&
					(vdata.variableRef as object[])[1] as FieldInfo == field && (vdata.variableRef as object[])[2] is int &&
						(int)(vdata.variableRef as object[])[2] == index) {
					if(isInstance)
						vdata.isInstance = true;
					return vdata.name;
				}
			}
			if(type == null) {
				throw new ArgumentNullException("type");
			}
			string name = GetVariableName(from, field, index);
			generatorData.AddVariable(new VData(from, field, index, type, name) {
				isInstance = isInstance,
				modifier = new FieldModifier() {
					Private = true,
					Public = false
				}
			});
			return name;
		}

		public static string AddVariable(VariableData variable, bool isInstance = true, bool autoCorrection = true) {
			return RegisterVariable(variable, isInstance, autoCorrection).name;
		}

		public static string AddVariable(VariableData variable, string variableName, bool isInstance = true, bool autoCorrection = true) {
			string name = variableName;
			foreach(VData vdata in generatorData.GetVariables()) {
				if(vdata.variableRef == variable) {
					if(isInstance) {
						vdata.isInstance = true;
					}
					return vdata.name;
				}
			}
			name = autoCorrection ? GenerateVariableName(variableName) : variableName;
			generatorData.AddVariable(new VData(variable, false) {
				name = name,
				isInstance = isInstance,
				variableRef = variable,
				modifier = variable.modifier
			});
			return name;
		}

		public static VData RegisterVariable(VariableData variable, bool isInstance = true, bool autoCorrection = true) {
			foreach(VData vdata in generatorData.GetVariables()) {
				if(vdata.variableRef == variable) {
					if(isInstance)
						vdata.isInstance = true;
					return vdata;
				}
			}
			var result = new VData(variable, false) {
				name = autoCorrection ? GenerateVariableName(variable.Name) : variable.Name,
				isInstance = isInstance,
				modifier = variable.modifier,
				variableRef = variable
			};
			generatorData.AddVariable(result);
			return result;
		}

		public static void RegisterVariableAlias(string variableName, VariableData variable, object owner) {
			generatorData.AddVariableAlias(variableName, variable, owner);
		}

		public static VariableData GetVariableAlias(string variableName, object owner) {
			return generatorData.GetVariableAlias(variableName, owner);
		}

		public static void RegisterPrivateVariable(string name, Type type, object value, bool isInstance = true) {
			foreach(VData vdata in generatorData.GetVariables()) {
				if(vdata.name == name) {
					vdata.isInstance = isInstance;
					return;
				}
			}
			generatorData.AddVariable(
				new VData(
					new VariableData(name, type, value) {
						modifier = new FieldModifier() {
							Public = false,
							Private = true,
						}
					}
				) {
					name = name,
					isInstance = isInstance
				});
		}

		public static void RegisterNodeSetup(Action action, NodeComponent owner) {
			Action act;
			generatorData.initActionForNodes.TryGetValue(owner, out act);
			act += action;
			generatorData.initActionForNodes[owner] = act;
		}
		#endregion

		#region GenerateVariableDeclaration
		/// <summary>
		/// Generate variable declaration for variable.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="alwaysHaveValue"></param>
		/// <param name="ignoreModifier"></param>
		/// <returns></returns>
		public static string GenerateVariableDeclaration(VariableData variable, bool alwaysHaveValue = true, bool ignoreModifier = false) {
			string varName = GetVariableName(variable);
			if(!string.IsNullOrEmpty(varName)) {
				if(ignoreModifier) {
					return GenerateVariableDeclaration(varName, variable.type, variable.variable, true, null, alwaysHaveValue);
				}
				return GenerateVariableDeclaration(varName, variable.type, variable.variable, true, variable.modifier, alwaysHaveValue);
			}
			return null;
		}

		/// <summary>
		/// Generate variable declaration for variable.
		/// </summary>
		/// <param name="variable"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static string GenerateVariableDeclaration(VariableData variable, object value, bool parseValue = true) {
			string varName = GetVariableName(variable);
			if(!string.IsNullOrEmpty(varName)) {
				return GenerateVariableDeclaration(varName, variable.type, value, parseValue);
			}
			return null;
		}

		/// <summary>
		/// Generate variable declaration for variableName.
		/// </summary>
		/// <param name="variableName"></param>
		/// <param name="type"></param>
		/// <param name="value"></param>
		/// <param name="alwaysHaveValue"></param>
		/// <param name="modifier"></param>
		/// <returns></returns>
		public static string GenerateVariableDeclaration(string variableName,
			Type type,
			object value = null,
			bool parseValue = true,
			FieldModifier modifier = null,
			bool alwaysHaveValue = true) {
			string M = null;
			if(modifier != null) {
				M = modifier.GenerateCode();
			}
			if(!alwaysHaveValue) {
				if(object.ReferenceEquals(value, null) && type.IsValueType) {
					return M + ParseType(type) + " " + variableName + ";";
				}
				return M + ParseType(type) + " " + variableName + " = " + (parseValue ? ParseValue(value) : value != null ? value.ToString() : "null") + ";";
			}
			if(object.ReferenceEquals(value, null) && ReflectionUtils.CanCreateInstance(type)) {
				value = ReflectionUtils.CreateInstance(type);
			}
			return M + ParseType(type) + " " + variableName + " = " + (parseValue ? ParseValue(value) : value != null ? value.ToString() : "null") + ";";
		}

		/// <summary>
		/// Generate variable declaration for variableName.
		/// </summary>
		/// <param name="variableName"></param>
		/// <param name="type"></param>
		/// <param name="value"></param>
		/// <param name="alwaysHaveValue"></param>
		/// <param name="modifier"></param>
		/// <returns></returns>
		public static string GenerateVariableDeclaration(string variableName,
			MemberData type,
			object value = null,
			bool parseValue = true,
			FieldModifier modifier = null,
			bool alwaysHaveValue = true) {
			string M = null;
			if(modifier != null) {
				M = modifier.GenerateCode();
			}
			if(!alwaysHaveValue) {
				if(object.ReferenceEquals(value, null) && type.Get<Type>().IsValueType) {
					return M + ParseType(type) + " " + variableName + ";";
				}
				return M + ParseType(type) + " " + variableName + " = " + (parseValue ? ParseValue(value) : value != null ? value.ToString() : "null") + ";";
			}
			if(object.ReferenceEquals(value, null) && ReflectionUtils.CanCreateInstance(type.Get<Type>())) {
				value = ReflectionUtils.CreateInstance(type.Get<Type>());
			}
			return M + ParseType(type) + " " + variableName + " = " + (parseValue ? ParseValue(value) : value != null ? value.ToString() : "null") + ";";
		}
		#endregion

		#endregion

		#region InsertMethod
		public static void InsertMethodCode(string methodName, Type returnType, string code, int priority = 0) {
			var mData = generatorData.GetMethodData(methodName);
			if(mData == null) {
				mData = generatorData.AddMethod(methodName, ParseType(returnType), new string[0]);
			}
			mData.AddCode(code, priority);
		}

		public static void InsertMethodCode(string methodName, Type returnType, Type[] parameterTypes, string code, int priority = 0) {
			var mData = generatorData.GetMethodData(methodName, parameterTypes.Select((item) => ParseType(item)).ToArray());
			if(mData == null) {
				mData = generatorData.AddMethod(methodName, ParseType(returnType), parameterTypes.Select((item) => ParseType(item)).ToArray());
			}
			mData.AddCode(code, priority);
		}
		#endregion

		/// <summary>
		/// This will remove break statement on ungrouped node.
		/// </summary>
		/// <param name="node"></param>
		public static void RemoveBreakStatement(Node node) {
			if(!generatorData.nonBreakNode.Contains(node)) {
				generatorData.nonBreakNode.Add(node);
			}
		}
	}
}