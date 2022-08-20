using System.Collections.Generic;
using System.Reflection;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  [DefaultExecutionOrder(-9000)]
  public class RagonEntity : MonoBehaviour
  {
    public bool AutoReplication => _replication;
    public bool PropertiesChanged => _propertiesChanged;
    public bool IsAttached => _attached;
    public bool IsMine => _mine;
    public int Id => _entityId;
    public int Type => _entityType;
    public RagonPlayer Owner => _owner;

    [SerializeField, ReadOnly] private int _entityType;
    [SerializeField, ReadOnly] private int _entityId;
    [SerializeField, ReadOnly] private bool _mine;
    [SerializeField, ReadOnly] private RagonPlayer _owner;
    [SerializeField, ReadOnly] private bool _attached;
    [SerializeField, ReadOnly] private bool _replication;
    [SerializeField, ReadOnly] private int _properties;
    [SerializeField, ReadOnly] private RagonBehaviour[] _behaviours;

    private RagonRoom _room;
    private RagonSerializer _serializer;
    private List<RagonProperty> _propertiesList;
    private bool _propertiesChanged;
    private byte[] _spawnPayload;
    private byte[] _destroyPayload;

    internal void RetrieveProperties()
    {
      _propertiesList = new List<RagonProperty>();
      _behaviours = GetComponents<RagonBehaviour>();
      _serializer = new RagonSerializer();

      foreach (var state in _behaviours)
      {
        var fieldFlags = (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var fieldInfos = state.GetType().GetFields(fieldFlags);
        var baseProperty = typeof(RagonProperty);

        foreach (var field in fieldInfos)
        {
          if (baseProperty.IsAssignableFrom(field.FieldType))
          {
            var property = (RagonProperty) field.GetValue(state);
            _propertiesList.Add(property);
            // Debug.Log($"RetrieveProperties: {gameObject.name} Prop: {property.Id} Size: {property.Size}");
          }
        }
      }

      _properties = _propertiesList.Count;
    }

    internal void WriteStateInfo(RagonSerializer serializer)
    {
      serializer.WriteUShort((ushort) _propertiesList.Count);
      foreach (var property in _propertiesList)
      {
        serializer.WriteBool(property.IsFixed);
        serializer.WriteUShort((ushort) property.Size);
        // Debug.Log($"WriteStateInfo: {gameObject.name} Prop: {property.Id} Size: {property.Size}");
      }
    }

    internal void SetType(ushort t) => _entityType = t;

    internal void TrackChangedProperty(RagonProperty property)
    {
      _propertiesChanged = true;
    }

    public void Attach(RagonRoom room, int entityType, RagonPlayer owner, int entityId, byte[] payloadData)
    {
      _entityType = entityType;
      _entityId = entityId;
      _owner = owner;
      _attached = true;
      _spawnPayload = payloadData;
      _replication = true;
      _room = room;
      _mine = _room.LocalPlayer.Id == owner.Id;

      foreach (var behaviour in _behaviours)
        behaviour.Attach(this);

      var propertyIdGenerator = 0;
      foreach (var property in _propertiesList)
      {
        property.Attach(this, propertyIdGenerator);
        propertyIdGenerator++;
      }
    }

    public void ChangeOwner(RagonPlayer newOwner)
    {
      _owner = newOwner;
      _mine = _room.LocalPlayer.Id == newOwner.Id;
    }

    public void Detach(byte[] payload)
    {
      _destroyPayload = payload;

      foreach (var state in _behaviours)
        state.Detach();

      Destroy(gameObject);
    }

    internal void ReplicateState(RagonSerializer serializer)
    {
      serializer.WriteUShort((ushort) _entityId);
      
      foreach (var prop in _propertiesList)
      {
        if (prop.IsDirty)
        {
          serializer.WriteBool(true);
          prop.Pack(serializer);
          prop.Clear();
        }
        else
        {
          serializer.WriteBool(false);
        }
      }
      
      _propertiesChanged = false;
    }

    internal void ProcessState(RagonSerializer data)
    {
      for (int i = 0; i < _propertiesList.Count; i++)
      {
        if (data.ReadBool())
          _propertiesList[i].Deserialize(data);
      }
    }

    internal T GetPayload<T>(byte[] data) where T : IRagonPayload, new()
    {
      if (data == null) return new T();
      if (data.Length == 0) return new T();

      var serializer = new RagonSerializer();
      serializer.FromArray(data);

      var payload = new T();
      payload.Deserialize(serializer);

      return payload;
    }

    public T GetSpawnPayload<T>() where T : IRagonPayload, new()
    {
      return GetPayload<T>(_spawnPayload);
    }

    public T GetDestroyPayload<T>() where T : IRagonPayload, new()
    {
      return GetPayload<T>(_destroyPayload);
    }

    public void ReplicateEvent<TEvent>(TEvent evnt, RagonTarget target, RagonReplicationMode replicationMode) where TEvent : IRagonEvent, new()
    {
      var evntId = RagonNetwork.Event.GetEventCode(evnt);
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
      _serializer.WriteUShort(evntId);
      _serializer.WriteByte((byte) replicationMode);
      _serializer.WriteByte((byte) target);
      _serializer.WriteUShort((ushort) Id);

      evnt.Serialize(_serializer);

      var sendData = _serializer.ToArray();
      _room.Connection.Send(sendData);
    }

    internal void ProcessEvent(RagonPlayer player, ushort eventCode, RagonSerializer data)
    {
      foreach (var behaviour in _behaviours)
        behaviour.ProcessEvent(player, eventCode, data);
    }

    private void Update()
    {
      if (!_attached) return;

      if (_mine)
      {
        foreach (var behaviour in _behaviours)
          behaviour.OnEntityTick();
      }
      else
      {
        foreach (var behaviour in _behaviours)
          behaviour.OnProxyTick();
      }
    }
  }
}