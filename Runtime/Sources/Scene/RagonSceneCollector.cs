using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Ragon.Client
{
  public class RagonSceneCollector: IRagonSceneCollector
  {
    public RagonEntity[] FindSceneEntities()
    {
      var gameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
      var objs = new List<RagonEntity>();

      foreach (var go in gameObjects)
      {
        var entities = go.GetComponentsInChildren<RagonEntity>();
        objs.AddRange(entities);
      }
      
      return objs.ToArray();
    }
  }
}