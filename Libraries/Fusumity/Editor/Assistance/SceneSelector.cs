using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fusumity.Editor.Assistance
{
	[Overlay(typeof(EditorWindow), "Scene-Selector", "Scene Selector")]
	internal class SceneSelector : IMGUIOverlay
	{
		private static string[] _scenePaths;
		private static string[] _sceneNames;

		private void Initialize()
		{
			if (_scenePaths == null || _scenePaths.Length != EditorBuildSettings.scenes.Length)
			{
				var scenePaths = new List<string>();
				var sceneNames = new List<string>();

				foreach (var scene in EditorBuildSettings.scenes)
				{
					if (scene.path == null || scene.path.StartsWith("Assets") == false)
						continue;

					var scenePath = Application.dataPath + scene.path.Substring(6);

					scenePaths.Add(scenePath);
					sceneNames.Add(Path.GetFileNameWithoutExtension(scenePath));
				}

				_scenePaths = scenePaths.ToArray();
				_sceneNames = sceneNames.ToArray();
			}
		}

		public override void OnGUI()
		{
			Initialize();

			var activeScene = SceneManager.GetActiveScene();
			var sceneName = activeScene.name;
			var sceneIndex = -1;

			for (var i = 0; i < _sceneNames.Length; ++i)
			{
				if (sceneName == _sceneNames[i])
				{
					sceneIndex = i;
					break;
				}
			}

			var isGuiEnabled = GUI.enabled;
			GUI.enabled = !Application.isPlaying;

			var newSceneIndex = EditorGUILayout.Popup(sceneIndex, _sceneNames, GUILayout.Width(200.0f));
			if (newSceneIndex != sceneIndex)
			{
				if (activeScene.isDirty)
				{
					var dialogResult = EditorUtility.DisplayDialogComplex("Scene Have Been Modified",
						"Do you want to save the changes you made in the scene:"
						+ $"\n{activeScene.path}"
						+ "\nYour changes will be lost if you don't save them.",
						"Save", "Cancel", "Don't Save");

					switch (dialogResult)
					{
						case 0: // Save
							EditorSceneManager.SaveScene(activeScene);
							break;
						case 1: // Cancel
							return;
						case 2: // Don't Save
							break;
					}
				}

				EditorSceneManager.OpenScene(_scenePaths[newSceneIndex], OpenSceneMode.Single);
			}

			GUI.enabled = isGuiEnabled;
		}
	}
}