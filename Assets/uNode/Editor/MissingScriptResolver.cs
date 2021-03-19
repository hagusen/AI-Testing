using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace MaxyGames.uNode.Editors {
	internal class MissingScriptResolver : EditorWindow {
		public static MissingScriptResolver window;
		public static Dictionary<string, Dictionary<string, string>> missingMap;
		public static Dictionary<string, Dictionary<string, string>> resolverData;
		public static List<string> brokenAsset;
		public static List<SceneAsset> brokenScene;

		private Vector2 scrollPos;

		//[MenuItem("Tools/uNode/MissingScriptResolver")]
		public static void ShowWindow() {
			window = (MissingScriptResolver)EditorWindow.GetWindow(typeof(MissingScriptResolver), false);
			window.minSize = new Vector2(450, 250);
			window.autoRepaintOnSceneChange = true;
			window.wantsMouseMove = true;
			window.titleContent = new GUIContent("MissingScriptResolver");
			window.Show();
			window.Focus();
		}


		[MenuItem("Tools/uNode/Update/Fix Missing Script", false, 100000)]
		private static void DoSearchAndFixMissingScript() {
			if(EditorSettings.serializationMode != SerializationMode.ForceText) {
				EditorUtility.DisplayDialog("Unsupported Serialization Mode", "Please change the serialization mode to 'Force Text' in 'Edit > Project Settings > Editor' menu and then under 'Asset Serialization Mode' choose 'Force Text', and restart Unity or Reimport All Asset and try again.", "OK");
				return;
			}
			FindMissingScript();
			if(resolverData == null) {
				return;
			}
			DoFixMissingScript();
		}

		private static void FindMissingScript() {
			LoadOptions();
			if(resolverData == null) {
				return;
			}
			brokenScene = null;
			var prefabList = new List<GameObject>();
			AssetDatabase.Refresh();
			var brokenMap = new Dictionary<string, Dictionary<string, string>>();
			brokenAsset = new List<string>();
			{//Prefab
				var gameObjects = uNodeEditorUtility.FindPrefabs();
				foreach(GameObject gameObject in gameObjects) {
					prefabList.Add(gameObject);
				}
				List<GameObject> brokenPrefab = new List<GameObject>();
				foreach(var prefab in prefabList) {
					if(HasBrokenComponent(prefab)) {
						brokenPrefab.Add(prefab);
					}
				}
				foreach(var prefab in brokenPrefab) {
					var prefabPath = AssetDatabase.GetAssetPath(prefab);
					FilterBrokenAsset(prefabPath, brokenMap);
					brokenAsset.Add(prefabPath);
				}
			}
			{//Assets
				string[] paths = AssetDatabase.GetAllAssetPaths();
				for(int i = 0; i < paths.Length; i++) {
					string assetPath = paths[i];
					if(assetPath.EndsWith(".asset") && assetPath.StartsWith("Assets")) {
						var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
						if(asset == null) {
							FilterBrokenAsset(assetPath, brokenMap);
							brokenAsset.Add(assetPath);
						}
					}
				}
			}
			Debug.Log($"Prefab & Asset has {brokenMap.Count} missing script");
			brokenScene = uNodeEditorUtility.FindAssetsByType<SceneAsset>();
			foreach(var scene in brokenScene) {
				Debug.Log("Searching:" + scene.name, scene);
				var p = AssetDatabase.GetAssetPath(scene);
				if(string.IsNullOrEmpty(p))
					continue;
				var projectAbsPath = Path.GetDirectoryName(Application.dataPath);
				var prefabAbsPath = projectAbsPath + "/" + p;
				int count = 0;
				using(var streamReader = new StreamReader(prefabAbsPath)) {
					var text = streamReader.ReadToEnd();
					var lines = text.Split('\n');
					string scriptLine = "";
					foreach(var line in lines) {
						if(line.StartsWith("---")) {
							scriptLine = "";
						}
						if(line.StartsWith("MonoBehaviour:")) {
							scriptLine = line;
						}
						if(scriptLine.Length > 0) {
							if(line.Trim().StartsWith("m_Script:")) {
								var token = line.Split(',');
								try {
									string identifier = null;
									for(int x = 0; x < token.Length; x++) {
										var str = token[x];
										if(str.StartsWith("  m_Script: {fileID:")) {
											identifier = str.Replace("  m_Script: {fileID:", "").Trim();
										}
										if(str.StartsWith(" guid:")) {
											var assetGuid = str.Replace(" guid: ", "");
											if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(assetGuid))) {
												if(!brokenMap.ContainsKey(assetGuid)) {
													brokenMap.Add(assetGuid, new Dictionary<string, string>());
												}
												string s = "";
												if(resolverData != null && resolverData.ContainsKey(assetGuid) &&
													resolverData[assetGuid].ContainsKey(identifier)) {
													s = resolverData[assetGuid][identifier];
												}
												brokenMap[assetGuid].Add(identifier, s);
												count++;
											}
										}
									}
								}
								catch { }
							}
						}
					}
				}
				Debug.Log("	Found :" + count, scene);
			}
			missingMap = brokenMap;
		}

		static void FilterBrokenAsset(string assetPath, Dictionary<string, Dictionary<string, string>> brokenMap) {
			if(string.IsNullOrEmpty(assetPath))
				return;
			Debug.Log("Broken asset:" + assetPath);
			var projectAbsPath = Path.GetDirectoryName(Application.dataPath);
			var prefabAbsPath = projectAbsPath + "/" + assetPath;
			using(var streamReader = new StreamReader(prefabAbsPath)) {
				string scriptLine = "";
				var text = streamReader.ReadToEnd();
				var lines = text.Split('\n');
				foreach(var line in lines) {
					if(line.StartsWith("---")) {
						scriptLine = "";
					}
					if(line.StartsWith("  m_Script:")) {
						scriptLine = line;
					}
					if(scriptLine.Length > 0) {
						if(line.StartsWith("  m_Script:")) {
							var token = line.Split(',');
							try {
								string identifier = null;
								foreach(string str in token) {
									if(str.StartsWith("  m_Script: {fileID:")) {
										identifier = str.Replace("  m_Script: {fileID:", "").Trim();
									}
									if(str.StartsWith(" guid:")) {
										var assetGuid = str.Replace(" guid: ", "");
										var metadataPath = projectAbsPath + "/Library/metadata/" + assetGuid.Remove(2) + "/" + assetGuid + ".info";
										if(!string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(assetGuid).Trim()))
											continue;
										if(!File.Exists(metadataPath)) {
											if(!brokenMap.ContainsKey(assetGuid)) {
												brokenMap[assetGuid] = new Dictionary<string, string>();
											}
											if(!brokenMap[assetGuid].ContainsKey(identifier)) {
												string s = "";
												if(resolverData != null && resolverData.ContainsKey(assetGuid) &&
													resolverData[assetGuid].ContainsKey(identifier)) {
													s = resolverData[assetGuid][identifier];
												}
												brokenMap[assetGuid].Add(identifier, s);
											}
											continue;
										}
										var texts = File.ReadAllLines(metadataPath);
										string localIdentifier = null;
										foreach(string tex in texts) {
											string t = tex.Trim();
											if(t.StartsWith("localIdentifier:")) {
												localIdentifier = t.Split(':')[1];
											}
											if(t.StartsWith("scriptClassName:") && identifier.Equals(localIdentifier)) {
												if(!brokenMap.ContainsKey(assetGuid)) {
													brokenMap[assetGuid] = new Dictionary<string, string>();
												}
												if(!brokenMap[assetGuid].ContainsKey(localIdentifier)) {
													brokenMap[assetGuid].Add(localIdentifier, t.Split(':')[1]);
												}
												break;
											}
										}
									}
								}
							}
							catch { }
						}
					}
				}
			}
		}

		void OnGUI() {
			HandleKeyboard();
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			EditorGUILayout.BeginHorizontal();
			if(GUILayout.Button(new GUIContent("Find Missing Script"))) {
				FindMissingScript();
			}
			EditorGUILayout.EndHorizontal();
			if(missingMap == null) {
				EditorGUILayout.HelpBox("Click Search to find all missing script", MessageType.Info);
			} else {
				if(missingMap.Count == 0) {
					EditorGUILayout.HelpBox("No missing script found", MessageType.Info);
				} else {
					foreach(var v in missingMap) {
						foreach(var k in v.Value) {
							EditorGUILayout.BeginVertical("Box");
							EditorGUILayout.BeginHorizontal();
							EditorGUILayout.LabelField("Guid");
							EditorGUILayout.LabelField(v.Key);
							EditorGUILayout.EndHorizontal();
							EditorGUILayout.BeginHorizontal();
							EditorGUILayout.LabelField("identifier");
							EditorGUILayout.LabelField(k.Key);
							EditorGUILayout.EndHorizontal();
							uNodeGUIUtility.EditValueLayouted(new GUIContent("Type"), v.Value[k.Key], typeof(string), delegate (object o) {
								v.Value[k.Key] = o as string;
							}, new uNodeUtility.EditValueSettings() { attributes = new object[] { new FilterAttribute(typeof(Object)) { DisplayAbstractType = false, OnlyGetType = true } } });
							EditorGUILayout.EndVertical();
						}
					}
					EditorGUILayout.BeginHorizontal();
					if(GUILayout.Button("Save Setting")) {
						if(resolverData == null) {
							resolverData = new Dictionary<string, Dictionary<string, string>>();
						}
						foreach(var v in missingMap) {
							if(!resolverData.ContainsKey(v.Key)) {
								resolverData.Add(v.Key, JsonHelper.Deserialize<Dictionary<string, string>>(JsonHelper.Serialize(v.Value)));
							} else {
								resolverData.Remove(v.Key);
								resolverData.Add(v.Key, JsonHelper.Deserialize<Dictionary<string, string>>(JsonHelper.Serialize(v.Value)));
							}
						}
						SaveOptions();
					}
					if(GUILayout.Button("Fix All")) {
						DoFixMissingScript();
						return;
					}
					EditorGUILayout.EndHorizontal();
				}
			}
			EditorGUILayout.EndScrollView();
		}

		static void DoFixMissingScript() {
			if((brokenAsset == null || brokenAsset.Count == 0) &&
				(brokenScene == null || brokenScene.Count == 0)) {
				Debug.Log("No prefab / scene to fix");
				return;
			}
			if(brokenAsset != null) {
				int count = 0;
				foreach(var prefabPath in brokenAsset) {
					count++;
					EditorUtility.DisplayProgressBar("Loading", "Fixing prefabs & assets", brokenAsset.Count / count);
					var projectAbsPath = Path.GetDirectoryName(Application.dataPath);
					var prefabAbsPath = projectAbsPath + "/" + prefabPath;
					string result = null;
					using(var streamReader = new StreamReader(prefabAbsPath)) {
						string scriptLine = "";
						var text = streamReader.ReadToEnd();
						var lines = text.Split('\n');
						for(int i = 0; i < lines.Length; i++) {
							var line = lines[i];
							if(line.StartsWith("---")) {
								scriptLine = "";
							}
							if(line.StartsWith("  m_Script:")) {
								scriptLine = line;
							}
							if(scriptLine.Length > 0) {
								if(line.StartsWith("  m_Script:")) {
									var token = line.Split(',');
									try {
										int identifierIndex = 0;
										int guidIndex = 0;
										string identifier = null;
										string assetGuid = null;
										for(int x = 0; x < token.Length; x++) {
											var str = token[x];
											if(str.StartsWith("  m_Script: {fileID:")) {
												identifierIndex = x;
												identifier = str.Replace("  m_Script: {fileID:", "").Trim();
											}
											if(str.StartsWith(" guid:")) {
												guidIndex = x;
												assetGuid = str.Replace(" guid: ", "");
											}
										}
										if(identifier != null && assetGuid != null) {
											if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(assetGuid))) {
												foreach(var a in missingMap) {
													if(a.Key.Equals(assetGuid) &&
														a.Value.ContainsKey(identifier) &&
														TypeSerializer.Deserialize(a.Value[identifier], false) != null) {
														Type t = TypeSerializer.Deserialize(a.Value[identifier], false);
														if(t == null)
															continue;
														MonoScript m = null;
														foreach(var s in uNodeEditorUtility.MonoScripts) {
															if(s.GetClass() == t) {
																m = s;
																break;
															}
														}
														if(m == null)
															continue;
														string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m));
														if(!string.IsNullOrEmpty(guid)) {
															token[identifierIndex] = token[identifierIndex].Replace(identifier,
																((long)Unsupported.GetLocalIdentifierInFileForPersistentObject(m)).ToString());
															token[guidIndex] = token[guidIndex].Replace(assetGuid, guid);
															break;
														}
														break;
													}
												}
											}
											if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(assetGuid))) {
												foreach(var a in missingMap) {
													if(a.Key.Equals(assetGuid)) {
														if(a.Value.ContainsKey(identifier) &&
															TypeSerializer.Deserialize(a.Value[identifier], false) != null) {
														}
													}
												}
											}
											lines[i] = string.Join(",", token);
										}
									}
									catch { }
								}
							}
						}
						result = string.Join("\n", lines);
					}
					if(!string.IsNullOrEmpty(result)) {
						File.WriteAllText(prefabAbsPath, result);
						AssetDatabase.Refresh();
					}
				}
			}
			if(brokenScene != null) {
				int count = 0;
				foreach(var scene in brokenScene) {
					count++;
					EditorUtility.DisplayProgressBar("Loading", "Fixing scenes", brokenScene.Count / count);
					var p = AssetDatabase.GetAssetPath(scene);
					if(string.IsNullOrEmpty(p))
						continue;
					var projectAbsPath = Path.GetDirectoryName(Application.dataPath);
					var prefabAbsPath = projectAbsPath + "/" + p;
					string result = null;
					using(var streamReader = new StreamReader(prefabAbsPath)) {
						var text = streamReader.ReadToEnd();
						var lines = text.Split('\n');
						string scriptLine = "";
						for(int i = 0; i < lines.Length; i++) {
							var line = lines[i];
							if(line.StartsWith("---")) {
								scriptLine = "";
							}
							if(line.StartsWith("MonoBehaviour:")) {
								scriptLine = line;
							}
							if(scriptLine.Length > 0) {
								if(line.Trim().StartsWith("m_Script:")) {
									var token = line.Split(',');
									try {
										int identifierIndex = 0;
										int guidIndex = 0;
										string identifier = null;
										string assetGuid = null;
										for(int x = 0; x < token.Length; x++) {
											var str = token[x];
											if(str.StartsWith("  m_Script: {fileID:")) {
												identifierIndex = x;
												identifier = str.Replace("  m_Script: {fileID:", "").Trim();
											}
											if(str.StartsWith(" guid:")) {
												guidIndex = x;
												assetGuid = str.Replace(" guid: ", "");
											}
										}
										if(identifier != null && assetGuid != null) {
											if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(assetGuid))) {
												foreach(var a in missingMap) {
													if(a.Key.Equals(assetGuid) &&
														a.Value.ContainsKey(identifier) &&
														TypeSerializer.Deserialize(a.Value[identifier], false) != null) {
														Type t = TypeSerializer.Deserialize(a.Value[identifier], false);
														if(t == null)
															continue;
														MonoScript m = null;
														foreach(var s in uNodeEditorUtility.MonoScripts) {
															if(s.GetClass() == t) {
																m = s;
																break;
															}
														}
														if(m == null)
															continue;
														string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(m));
														if(!string.IsNullOrEmpty(guid)) {
															token[identifierIndex] = token[identifierIndex].Replace(identifier,
																((long)Unsupported.GetLocalIdentifierInFileForPersistentObject(m)).ToString());
															token[guidIndex] = token[guidIndex].Replace(assetGuid, guid);
															break;
														}
														break;
													}
												}
											}
											if(string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(assetGuid))) {
												foreach(var a in missingMap) {
													if(a.Key.Equals(assetGuid)) {
														if(a.Value.ContainsKey(identifier) &&
															TypeSerializer.Deserialize(a.Value[identifier], false) != null) {
														}
													}
												}
											}
											lines[i] = string.Join(",", token);
										}
									}
									catch { }
								}
							}
						}
						result = string.Join("\n", lines);
					}
					if(!string.IsNullOrEmpty(result)) {
						File.WriteAllText(prefabAbsPath, result);
						AssetDatabase.Refresh();
					}
				}
			}
			missingMap = null;
			EditorUtility.ClearProgressBar();
		}

		static bool HasBrokenComponent(GameObject gameObject) {
			var components = gameObject.GetComponentsInChildren<Component>(true);
			foreach(var component in components) {
				if(component == null)
					return true;
			}
			return false;
		}

		void HandleKeyboard() {
			Event current = Event.current;
			if(current.type == EventType.KeyDown) {
				if(current.keyCode == KeyCode.Escape) {
					Close();
					return;
				}
			}
			if(current.type == EventType.KeyDown) {
				Focus();
			}
		}

		static void LoadOptions() {
			char separator = Path.DirectorySeparatorChar;
			var resolver = uNodeEditorUtility.GetMonoScript(typeof(MissingScriptResolver));
			if(resolver != null) {
				var path = AssetDatabase.GetAssetPath(resolver);
				path = path.Replace(nameof(MissingScriptResolver) + ".cs", "MissingScriptData.json");
				if(!string.IsNullOrEmpty(path) && File.Exists(path)) {
					string json = File.ReadAllText(path);
					resolverData = JsonHelper.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
				} else {
					Debug.Log("Could't found the resolver script data on path: " + path);
				}
			} else {
				Debug.Log("Could't found Resolver script");
			}
			// string path = "uNode2Data" + separator + "MissingResolverData" + ".json";
			// if(File.Exists(path)) {
			// 	string json = File.ReadAllText(path);
			// 	resolverData = JsonHelper.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);
			// }
		}

		static void SaveOptions() {
			Directory.CreateDirectory("uNode2Data");
			char separator = Path.DirectorySeparatorChar;
			string path = "uNode2Data" + separator + "MissingResolverData" + ".json";
			using(FileStream stream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write)) {
				using(StreamWriter sw = new StreamWriter(stream)) {
					sw.Write(JsonHelper.Serialize(resolverData, true));
				}
			}
		}
	}
}