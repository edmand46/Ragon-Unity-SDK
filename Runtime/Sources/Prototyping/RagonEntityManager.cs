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
  public class RagonEntityManager : MonoBehaviour
  {
    [Range(1.0f, 60.0f, order = 0)] public float ReplicationRate = 1.0f;

    public static RagonEntityManager Instance { get; private set; }

    public void PrefabCallback(Func<PrefabRequest, GameObject> action) => _prefabCallback = action;

    private Dictionary<int, RagonObject> _entitiesDict = new Dictionary<int, RagonObject>();
    private Dictionary<int, RagonObject> _entitiesStatic = new Dictionary<int, RagonObject>();
    
    private List<RagonObject> _entitiesList = new List<RagonObject>();
    private List<RagonObject> _entitiesOwned = new List<RagonObject>();

    private Func<PrefabRequest, GameObject> _prefabCallback;

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
      var entities = new List<RagonObject>();
      foreach (var go in gameObjects)
      {
        if (go.TryGetComponent<RagonObject>(out var ragonEntity))
        {
          entities.Add(ragonEntity);
        }
      }
      
      Debug.Log("Found static entities: " + entities.Count);
      
      ushort staticId = 0;
      foreach (var entityInternal in entities)
      {
        staticId += 1;
        _entitiesStatic.Add(staticId, entityInternal);
        
        if (RagonNetwork.Room.LocalPlayer.IsRoomOwner)
          RagonNetwork.Room.CreateStaticEntity(0, staticId, null);
      }
    }

    public void Cleanup()
    {
      foreach (var entity in _entitiesList)
        entity.Detach(Array.Empty<byte>());

      _entitiesDict.Clear();
      _entitiesList.Clear();
      _entitiesOwned.Clear();
      _entitiesStatic.Clear();
    }

    public void FixedUpdate()
    {
      _replicationTimer += Time.fixedTime;
      if (_replicationTimer > _replicationRate)
      {
        foreach (var entityInternal in _entitiesOwned)
        {
          // if (entityInternal.AutoReplication)
            // entityInternal.ReplicateState();
        }

        _replicationTimer = 0.0f;
      }
    }

    public void OnEntityStaticCreated(int entityId, ushort staticId, ushort entityType, RagonAuthority state, RagonAuthority evnt, RagonPlayer creator, byte[] payload)
    {
      if (_entitiesStatic.Remove(staticId, out var entity))
      {
        entity.Attach(entityType, creator, entityId, payload);

        _entitiesDict.Add(entityId, entity);
        _entitiesList.Add(entity);

        if (creator.IsMe)
          _entitiesOwned.Add(entity);
      }
    }

    public void OnEntityCreated(int entityId, ushort entityType, RagonAuthority state, RagonAuthority evnt, RagonPlayer creator, byte[] payload)
    {
      var prefabRequest = new PrefabRequest()
      {
        Type = entityType,
        IsOwned = creator.IsMe,
      };

      var prefab = _prefabCallback?.Invoke(prefabRequest);
      var go = Instantiate(prefab);

      var component = go.GetComponent<RagonObject>();
      component.Attach(entityType, creator, entityId, payload);

      _entitiesDict.Add(entityId, component);
      _entitiesList.Add(component);

      if (creator.IsMe)
        _entitiesOwned.Add(component);
    }

    public void OnEntityDestroyed(int entityId, byte[] payload)
    {
      if (_entitiesDict.Remove(entityId, out var entity))
      {
        _entitiesList.Remove(entity);

        if (_entitiesOwned.Contains(entity))
          _entitiesOwned.Remove(entity);

        entity.Detach(payload);
      }
    }

    public void OnEntityEvent(RagonPlayer player, int entityId, ushort evntCode, RagonSerializer payload)
    {
      if (_entitiesDict.ContainsKey(entityId))
      {
        _entitiesDict[entityId].ProcessEvent(player, evntCode, payload);
      }
    }

    public void OnEntityState(int entityId, RagonSerializer payload)
    {
      if (_entitiesDict.ContainsKey(entityId))
      {
        _entitiesDict[entityId].ProcessState(payload);
      }
    }

    public void OnOwnerShipChanged(RagonPlayer player)
    {
      foreach (var ent in _entitiesList)
      {
        ent.ChangeOwner(player);
      }
    }
  }
}