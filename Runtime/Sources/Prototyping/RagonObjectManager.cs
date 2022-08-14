using System;
using System.Collections.Generic;
using Ragon.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ragon.Client.Prototyping
{
  public struct PrefabRequest
  {
    public ushort Type;
    public bool IsOwned;
  }

  [DefaultExecutionOrder(-10000)]
  public class RagonObjectManager : MonoBehaviour
  {
    [Range(1.0f, 60.0f, order = 0)] public float ReplicationRate = 1.0f;

    public static RagonObjectManager Instance { get; private set; }

    public void PrefabCallback(Func<PrefabRequest, GameObject> action) => _prefabCallback = action;

    private Dictionary<int, RagonObject> _objectsDict = new Dictionary<int, RagonObject>();
    private Dictionary<int, RagonObject> _objectsStatic = new Dictionary<int, RagonObject>();
    
    private List<RagonObject> _objectsList = new List<RagonObject>();
    private List<RagonObject> _objectsOwned = new List<RagonObject>();

    private Func<PrefabRequest, GameObject> _prefabCallback;

    private RagonSerializer _serializer = new RagonSerializer();
    private float _replicationTimer = 0.0f;
    private float _replicationRate = 0.0f;

    private void Awake()
    {
      Instance = this;

      _replicationTimer = 1000.0f / ReplicationRate;
    }

    public void CollectSceneData()
    {
      var gameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
      var objs = new List<RagonObject>();
      foreach (var go in gameObjects)
      {
        if (go.TryGetComponent<RagonObject>(out var ragonObject))
        {
          objs.Add(ragonObject);
        }
      }
      
      Debug.Log("Found static entities: " + objs.Count);
      
      ushort staticId = 1;
      foreach (var staticObj in objs)
      {
        staticId += 1;
        _objectsStatic.Add(staticId, staticObj);
        
        if (RagonNetwork.Room.LocalPlayer.IsRoomOwner)
          RagonNetwork.Room.CreateStaticEntity(staticObj.gameObject, staticId, null);
      }
    }

    public void Cleanup()
    {
      foreach (var obj in _objectsList)
        obj.Detach(Array.Empty<byte>());

      _objectsDict.Clear();
      _objectsList.Clear();
      _objectsOwned.Clear();
      _objectsStatic.Clear();
    }

    public void FixedUpdate()
    {
      _replicationTimer += Time.fixedTime;
      if (_replicationTimer > _replicationRate)
      {
        foreach (var ownedObject in _objectsOwned)
        {
          if (ownedObject.AutoReplication)
            ownedObject.ReplicateState(_serializer);
        }

        _replicationTimer = 0.0f; 
      }
    }

    public void OnEntityStaticCreated(int objectId, ushort staticId, ushort entityType, RagonAuthority state, RagonAuthority evnt, RagonPlayer creator, byte[] payload)
    {
      if (_objectsStatic.Remove(staticId, out var ragonObject))
      {
        ragonObject.RetrieveProperties();
        ragonObject.Attach(entityType, creator, objectId, payload);

        _objectsDict.Add(objectId, ragonObject);
        _objectsList.Add(ragonObject);

        if (creator.IsMe)
          _objectsOwned.Add(ragonObject);
      }
    }

    public void OnEntityCreated(int objectId, ushort objectType, RagonAuthority state, RagonAuthority evnt, RagonPlayer creator, byte[] payload)
    {
      var prefabRequest = new PrefabRequest()
      {
        Type = objectType,
        IsOwned = creator.IsMe,
      };

      var prefab = _prefabCallback?.Invoke(prefabRequest);
      var go = Instantiate(prefab);

      var component = go.GetComponent<RagonObject>();
      component.RetrieveProperties();
      component.Attach(objectType, creator, objectId, payload);

      _objectsDict.Add(objectId, component);
      _objectsList.Add(component);

      if (creator.IsMe)
        _objectsOwned.Add(component);
    }

    public void OnEntityDestroyed(int objectId, byte[] payload)
    {
      if (_objectsDict.Remove(objectId, out var ragonObject))
      {
        _objectsList.Remove(ragonObject);

        if (_objectsOwned.Contains(ragonObject))
          _objectsOwned.Remove(ragonObject);

        ragonObject.Detach(payload);
      }
    }

    public void OnEntityEvent(RagonPlayer player, int objectId, ushort evntCode, RagonSerializer payload)
    {
      if (_objectsDict.ContainsKey(objectId))
      {
        _objectsDict[objectId].ProcessEvent(player, evntCode, payload);
      }
    }

    public void OnEntityState(int objectId, RagonSerializer payload)
    {
      if (_objectsDict.ContainsKey(objectId))
      {
        _objectsDict[objectId].ProcessState(payload);
      }
    }

    public void OnOwnerShipChanged(RagonPlayer player)
    {
      foreach (var obj in _objectsList)
      {
        obj.ChangeOwner(player);
      }
    }
  }
}