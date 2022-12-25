using System;
using System.Collections.Generic;
using Ragon.Common;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace Ragon.Client
{
  [DefaultExecutionOrder(-10000)]
  public class RagonEntityManager : MonoBehaviour
  {
    [SerializeField][Range(1.0f, 60.0f, order = 0)] private float _replicateRate = 20.0f;

    public static RagonEntityManager Instance { get; private set; }

    private Dictionary<int, RagonEntity> _entitiesDict = new Dictionary<int, RagonEntity>();
    private Dictionary<int, RagonEntity> _entitiesStatic = new Dictionary<int, RagonEntity>();

    private List<RagonEntity> _entitiesList = new List<RagonEntity>();
    private List<RagonEntity> _entitiesOwned = new List<RagonEntity>();

    private IRagonEntityCollector _entityCollector;
    private RagonPrefabRegistry _registry;
    private RagonSerializer _serializer = new RagonSerializer();
    private RagonRoom _room;

    private float _replicationTimer = 0.0f;
    private float _replicationRate = 0.0f;

    private void Awake()
    {
      Instance = this;

      _registry = Resources.Load<RagonPrefabRegistry>("RagonPrefabRegistry");
      _entityCollector = new RagonEntityCollector();

      Assert.IsNotNull(_registry, "Can't load prefab registry, please create RagonPrefabRegistry in Resources folder");

      _registry.Cache();
      _replicationRate = (1000.0f / _replicateRate) / 1000.0f;
    }

    public void AddCustomSceneCollector(IRagonEntityCollector collector) => _entityCollector = collector;
    public RagonEntity FindEntityById(ushort id) => _entitiesDict[id];

    internal void FindSceneEntities()
    {
      _entitiesStatic.Clear();

      var objs = _entityCollector.FindSceneEntities();
      RagonNetwork.Log.Trace("Found scene entities: " + objs.Length);

      foreach (var entity in objs)
      {
        var sceneId = entity.SceneId;
        _entitiesStatic.Add(sceneId, entity);
      }
    }

    internal void WriteSceneEntities(RagonSerializer serializer)
    {
      serializer.WriteUShort((ushort)_entitiesStatic.Count);
      foreach (var (sceneId, ragonObject) in _entitiesStatic)
      {
        serializer.WriteUShort(ragonObject.Type);
        serializer.WriteByte((byte)ragonObject.Authority);
        serializer.WriteUShort((ushort)sceneId);

        ragonObject.RetrieveProperties();
        ragonObject.WriteStateInfo(serializer);

        RagonNetwork.Log.Trace(
          $"[Scene Entity] Name; {ragonObject.name} Authority: {ragonObject.Authority} SceneId: {sceneId}");
      }
    }

    internal void OnRoomCreated(RagonRoom room)
    {
      _room = room;
    }

    internal void OnRoomDestroyed()
    {
      _room = null;
    }

    internal void Cleanup()
    {
      foreach (var ent in _entitiesList)
        ent.Detach(Array.Empty<byte>());

      _entitiesDict.Clear();
      _entitiesList.Clear();
      _entitiesOwned.Clear();
      _entitiesStatic.Clear();
    }

    internal void FixedUpdate()
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
          _serializer.WriteUShort((ushort)changedEntities, offset);

          var sendData = _serializer.ToArray();
          _room.Connection.Send(sendData, DeliveryType.Unreliable);
        }

        _replicationTimer = 0.0f;
      }
    }

    internal void OnEntityStaticCreated(ushort entityId, ushort staticId, ushort entityType, RagonPlayer creator,
      RagonSerializer serializer)
    {
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

    internal void OnEntityCreated(ushort entityId, ushort entityType, RagonPlayer creator, RagonSerializer serializer)
    {
      var payload = Array.Empty<byte>();

      if (serializer.Size > 0)
      {
        var size = serializer.ReadUShort();
        var entityPayload = serializer.ReadData(size);
        payload = entityPayload.ToArray();
      }

      if (!_registry.Prefabs.TryGetValue(entityType, out var prefab))
      {
        RagonNetwork.Log.Warn($"Entity Id: {entityId} Type: {entityType} not found in Prefab Registry");
        return;
      }

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

    internal void OnEntityDestroyed(int entityId, RagonSerializer serializer)
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

    internal void OnEntityEvent(RagonPlayer player, int entityId, ushort evntCode, RagonSerializer payload)
    {
      if (_entitiesDict.TryGetValue(entityId, out var ent))
        ent.ProcessEvent(player, evntCode, payload);
      else
        RagonNetwork.Log.Error($"[Event] Entity with Id {entityId} not found");
    }

    internal void OnEntityState(int entityId, RagonSerializer payload)
    {
      if (_entitiesDict.TryGetValue(entityId, out var ent))
        ent.ProcessState(payload);
      else
        RagonNetwork.Log.Error($"[State] Entity with Id {entityId} not found ");
    }

    internal void OnOwnershipChanged(RagonPlayer player, int entityId)
    {
      if (_entitiesDict.TryGetValue(entityId, out var entity))
      {
        entity.ChangeOwner(player);

        if (entity.IsMine)
          _entitiesOwned.Add(entity);
        else
          _entitiesOwned.Remove(entity);
      }
      else
      {
        RagonNetwork.Log.Error($"[OwnerShip] Entity with Id {entityId} not found");
      }
    }
  }
}