using System;
using System.Collections.Generic;
using System.Linq;
using Ragon.Client;
using Ragon.Client.Integration;
using UnityEngine;

namespace Example.Game
{
  [Serializable]
  public enum GamePrefab: ushort
  {
    CHARACTER,
  }

  [Serializable]
  public struct PrefabData
  {
    public GamePrefab PrefabId;
    public GameObject Prefab;
  }
  public class GameManager: MonoBehaviour
  {
    [SerializeField] private List<PrefabData> _prefabs = new List<PrefabData>();
    
    private void Start()
    {
      RagonManager.Instance.PrefabCallback((type) =>
      {
        return _prefabs.First(p => (ushort) p.PrefabId == type).Prefab;
      });  
      
      RagonNetwork.ConnectToServer("127.0.0.1", 5000);
    }

    private void Update()
    {
      if (Input.GetKeyDown(KeyCode.Space))
      {
        RagonNetwork.Room.CreateEntity((ushort) GamePrefab.CHARACTER, new TestEvent() { TestData = "Item0"});
      }
    }
  }
}