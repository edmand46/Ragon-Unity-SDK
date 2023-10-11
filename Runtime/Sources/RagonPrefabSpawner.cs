using Ragon.Client;
using Ragon.Client.Unity;
using UnityEngine;

namespace Ragon
{
  public class RagonPrefabSpawner: IRagonPrefabSpawner
  {
    public GameObject InstantiateEntityGameObject(RagonEntity entity, GameObject prefab)
    {
      return Object.Instantiate(prefab);
    }
  }
}