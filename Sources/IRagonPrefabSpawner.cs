using UnityEngine;

namespace Ragon.Client.Unity
{
  public interface IRagonPrefabSpawner
  {
    public GameObject InstantiateEntityGameObject(RagonEntity entity, GameObject prefab);
  }
}