#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Ragon.Editor
{
  public class RagonTools : EditorWindow
  {
    [MenuItem("Services/Ragon/Tools")]
    private static void ShowWindow()
    {
      var window = GetWindow<RagonTools>();
      window.titleContent = new GUIContent("Ragon Tools");
      window.Show();
    }

    private int _players = 0;
    private bool _rebuildEveryTime = false;

    private void OnGUI()
    {
      GUILayout.Space(10);

      EditorGUI.indentLevel = 1;

      var padding = new RectOffset(10, 10, 0, 0);
      var intStyle = new GUIStyle(EditorStyles.numberField);
      var boolStyle = new GUIStyle(EditorStyles.toggle);
      var buttonStyle = new GUIStyle() {padding = padding};

      _players = EditorGUILayout.IntField("Players", _players, intStyle);
      _rebuildEveryTime = EditorGUILayout.Toggle("Build", _rebuildEveryTime, boolStyle);

      GUILayout.BeginVertical(buttonStyle);

      if (GUILayout.Button("Play Editor And Clients"))
      {
        if (_rebuildEveryTime)
        {
#if UNITY_EDITOR_OSX
          Builder.BuildClientMacOnly();
#else
      Builder.BuildClientWinOnly();
#endif
        }

        Runner.RunEditorAndClients(_players);
      }

      if (GUILayout.Button("Play Only Clients"))
      {
        if (_rebuildEveryTime)
        {
#if UNITY_EDITOR_OSX
          Builder.BuildClientMacOnly();
#else
          Builder.BuildClientWinOnly();
#endif
        }

        Runner.RunClients(_players);
      }

      if (GUILayout.Button("Build"))
      {
#if UNITY_EDITOR_OSX
        Builder.BuildClientMacOnly();
#else
          Builder.BuildClientWinOnly();
#endif
      }

      GUILayout.EndVertical();
    }
  }
}
#endif