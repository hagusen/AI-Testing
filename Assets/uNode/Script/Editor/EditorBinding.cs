using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
//using UnityEngine.Events;
//using UnityEngine.SceneManagement;

namespace MaxyGames.uNode.Editors {
	public static class EditorBinding {
		public static Action<Type, Type> patchType;

		public static Func<UnityEngine.Object, UnityEngine.Object> getPrefabParent;

		public static Action<GameObject, UnityEngine.Object> savePrefabAsset;

		public static event EditorSceneManager.NewSceneCreatedCallback onNewSceneCreated;
		public static event EditorSceneManager.SceneClosingCallback onSceneClosing;
		//public static UnityAction<Scene, Scene> onSceneChanged;
		public static event EditorSceneManager.SceneSavingCallback onSceneSaving;
		public static event EditorSceneManager.SceneSavedCallback onSceneSaved;
		public static event EditorSceneManager.SceneOpeningCallback onSceneOpening;
		public static event EditorSceneManager.SceneOpenedCallback onSceneOpened;
		public static event Action onFinishCompiling;

		public static Type csharpParserType => "MaxyGames.uNode.Editors.CSharpParser".ToType(false);

		[InitializeOnLoadMethod]
		internal static void OnInitialize() {
			GraphUtility.Initialize();
			EditorSceneManager.newSceneCreated += onNewSceneCreated;
			EditorSceneManager.sceneClosing += onSceneClosing;
			EditorSceneManager.sceneSaving += onSceneSaving;
			EditorSceneManager.sceneSaved += onSceneSaved;
			EditorSceneManager.sceneOpening += onSceneOpening;
			EditorSceneManager.sceneOpened += onSceneOpened;
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		internal static void OnScriptReloaded() {
			uNodeEditor.OnFinishedCompiling();
			if(onFinishCompiling != null)
				onFinishCompiling();
		}
	}
}