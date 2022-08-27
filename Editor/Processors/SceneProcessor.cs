#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Ragon.Client;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ragon.Editor
{
  public class SceneProcessor : UnityEditor.AssetModificationProcessor
  {
    public static string[] OnWillSaveAssets(string[] paths)
    {
      string sceneName = string.Empty;
      foreach (string path in paths)
      {
        if (path.Contains(".unity"))
        {
          sceneName = Path.GetFileNameWithoutExtension(path);
          break;
        }
      }

      if (sceneName.Length == 0)
        return paths;

      GenerateSceneIds();

      return paths;
    }

    public static void GenerateSceneIds()
    {
      var gameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
      var objs = new List<RagonEntity>();
      foreach (var go in gameObjects)
      {
        var entities = go.GetComponentsInChildren<RagonEntity>();
        objs.AddRange(entities);
      }

      Debug.Log("Found scene entities: " + objs.Count);

      ushort staticId = 1;
      foreach (var entity in objs)
      {
        staticId += 1;
        entity.SetSceneId(staticId);
        EditorUtility.SetDirty(entity);
      }
    }
  }
}
#endif