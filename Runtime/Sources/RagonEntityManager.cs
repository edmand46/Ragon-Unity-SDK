using System;
using System.Collections.Generic;
using Ragon.Common;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Ragon.Client
{
  [DefaultExecutionOrder(-10000)]
  public class RagonEntityManager : MonoBehaviour
  {
    [Range(1.0f, 60.0f, order = 0)] public float replicationRate = 1.0f;

    public static RagonEntityManager Instance { get; private set; }

    private Dictionary<int, RagonEntity> _entitiesDict = new Dictionary<int, RagonEntity>();
    private Dictionary<int, RagonEntity> _entitiesStatic = new Dictionary<int, RagonEntity>();

    private List<RagonEntity> _entitiesList = new List<RagonEntity>();
    private List<RagonEntity> _entitiesOwned = new List<RagonEntity>();

    private RagonPrefabRegistry _registry;
    private RagonSerializer _serializer = new RagonSerializer();
    private RagonRoom _room;

    private float _replicationTimer = 0.0f;
    private float _replicationRate = 0.0f;

    private void Awake()
    {
      Instance = this;

      _registry = Resources.Load<RagonPrefabRegistry>("RagonPrefabRegistry");
      Assert.IsNotNull(_registry, "Can't load prefab registry, please create RagonPrefabRegistry in Resources folder");

      _registry.Cache();

      _replicationRate = (1000.0f / replicationRate) / 1000.0f;
    }

    public void CollectSceneEntities()
    {
      _entitiesStatic.Clear();

      var gameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
      var objs = new List<RagonEntity>();

      foreach (var go in gameObjects)
      {
        var entities = go.GetComponentsInChildren<RagonEntity>();
        objs.AddRange(entities);
      }

      Debug.Log("Found scene entities: " + objs.Count);
      foreach (var entity in objs)
      {
        var sceneId = entity.SceneId;
        _entitiesStatic.Add(sceneId, entity);
      }
    }

    public void WriteSceneEntities(RagonSerializer serializer)
    {
      serializer.WriteUShort((ushort) _entitiesStatic.Count);
      foreach (var (sceneId, ragonObject) in _entitiesStatic)
      {
        serializer.WriteUShort(ragonObject.Type);
        serializer.WriteByte((byte) ragonObject.Authority);
        serializer.WriteUShort((ushort) sceneId);

        ragonObject.RetrieveProperties();
        ragonObject.WriteStateInfo(serializer);

        Debug.Log($"[Scene Entity] Name; {ragonObject.name} Authority: {ragonObject.Authority} SceneId: {sceneId}");
      }
    }

    public void OnRoomCreated(RagonRoom room)
    {
      _room = room;
    }

    public void OnRoomDestroyed()
    {
      _room = null;
    }

    public void Cleanup()
    {
      foreach (var ent in _entitiesList)
        ent.Detach(Array.Empty<byte>());

      _entitiesDict.Clear();
      _entitiesList.Clear();
      _entitiesOwned.Clear();
      _entitiesStatic.Clear();
    }

    public void FixedUpdate()
    {
      if (_room == null) return;

      _replicationTimer += Time.fixedDeltaTime;
      if (_replicationTimer > _replicationRate)
      {
        var changedEntities = 0;

        _serializer.Clear();
        _serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_STATE);

        var offset = _serializer.Lenght;
        _serializer.AddOffset(2);

        foreach (var ent in _entitiesOwned)
        {
          if (
            ent.IsAttached &&
            ent.AutoReplication && 
            ent.PropertiesChanged)
          {
            ent.ReplicateState(_serializer);
            changedEntities++;
          }
        }

        if (changedEntities > 0)
        {
          _serializer.WriteUShort((ushort) changedEntities, offset);
          _room.Connection.Send(_serializer);
        }

        _replicationTimer = 0.0f;
      }
    }

    public void OnEntityStaticCreated(ushort entityId, ushort staticId, ushort entityType, RagonPlayer creator, RagonSerializer serializer)
    {
      Debug.Log($"OnCreate scene entity: {entityId} {staticId} {entityType}");
      if (_entitiesStatic.Remove(staticId, out var ragonEntity))
      {
        var payload = Array.Empty<byte>();
        if (serializer.Size > 0)
        {
          var size = serializer.ReadUShort();
          var entityPayload = serializer.ReadData(size);
          payload = entityPayload.ToArray();
        }

        ragonEntity.RetrieveProperties();
        ragonEntity.Attach(_room, entityType, creator, entityId, payload);
        ragonEntity.ProcessState(serializer);

        _entitiesDict.Add(entityId, ragonEntity);
        _entitiesList.Add(ragonEntity);

        if (creator.IsMe)
          _entitiesOwned.Add(ragonEntity);
      }
    }

    public void OnEntityCreated(ushort entityId, ushort entityType, RagonPlayer creator, RagonSerializer serializer)
    {
      var payload = Array.Empty<byte>();
      if (serializer.Size > 0)
      {
        var size = serializer.ReadUShort();
        var entityPayload = serializer.ReadData(size);
        payload = entityPayload.ToArray();
      }

      var prefab = _registry.Prefabs[entityType];
      var go = Instantiate(prefab);

      var component = go.GetComponent<RagonEntity>();
      component.RetrieveProperties();
      component.Attach(_room, entityType, creator, entityId, payload);
      component.ProcessState(serializer);

      _entitiesDict.Add(entityId, component);
      _entitiesList.Add(component);

      if (creator.IsMe)
        _entitiesOwned.Add(component);
    }

    public void OnEntityDestroyed(int entityId, RagonSerializer serializer)
    {
      if (_entitiesDict.Remove(entityId, out var ragonEntity))
      {
        var payload = Array.Empty<byte>();
        if (serializer.Size > 0)
        {
          var size = serializer.ReadUShort();
          var entityPayload = serializer.ReadData(size);
          payload = entityPayload.ToArray();
        }

        _entitiesList.Remove(ragonEntity);

        if (_entitiesOwned.Contains(ragonEntity))
          _entitiesOwned.Remove(ragonEntity);

        ragonEntity.Detach(payload);
      }
    }

    public void OnEntityEvent(RagonPlayer player, int entityId, ushort evntCode, RagonSerializer payload)
    {
      if (_entitiesDict.TryGetValue(entityId, out var ent))
        ent.ProcessEvent(player, evntCode, payload);
      else
        Debug.LogWarning("[Event] Entity not found");
    }

    public void OnEntityState(int entityId, RagonSerializer payload)
    {
      if (_entitiesDict.TryGetValue(entityId, out var ent))
        ent.ProcessState(payload);
      else
        Debug.LogWarning("[State] Entity not found");
    }

    public void OnOwnerShipChanged(RagonPlayer player, int entityId)
    {
      if (_entitiesDict.TryGetValue(entityId, out var entity))
        entity.ChangeOwner(player);
      else
        Debug.LogWarning("[OwnerShip] Entity not found");
    }
  }
}