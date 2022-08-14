using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Prototyping
{
  [ExecuteInEditMode]
  public class RagonObject : MonoBehaviour
  {
    private delegate void OnEventDelegate(RagonPlayer player, RagonSerializer buffer);

    public static string GenerateIdentifier() => Guid.NewGuid().ToString().Replace("-", "");

    public bool AutoReplication => _replication;
    public bool IsAttached => _attached;
    public bool IsMine => _mine;
    public int Id => _entityId;
    public int Type => _entityType;
    public RagonPlayer Owner => _owner;

    [SerializeField] private int _entityType;
    [SerializeField, ReadOnly] private int _entityId;
    [SerializeField, ReadOnly] private bool _mine;
    [SerializeField, ReadOnly] private RagonPlayer _owner;
    [SerializeField, ReadOnly] private bool _attached;
    [SerializeField, ReadOnly] private bool _replication;

    protected RagonRoom Room;
    private RagonSerializer _serializer;
    private RagonEntity[] _behaviours;
    private List<RagonProperty> _properties;
    private bool _propertiesChanged;
    private byte[] _spawnPayload;
    private byte[] _destroyPayload;

    private Dictionary<int, OnEventDelegate> _events = new();

    internal void RetrieveProperties()
    {
      _properties = new List<RagonProperty>();
      _behaviours = GetComponents<RagonEntity>();
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
            _properties.Add(property);
          }
        }
      }
    }

    internal void WriteStateInfo(RagonSerializer serializer)
    {
      serializer.WriteUShort((ushort) _properties.Count);
      foreach (var property in _properties)
        serializer.WriteUShort((ushort) property.Size);
    }

    internal void TrackChangedProperty(RagonProperty property)
    {
      _propertiesChanged = true;
    }

    public void Attach(int entityType, RagonPlayer owner, int entityId, byte[] payloadData)
    {
      _entityType = entityType;
      _entityId = entityId;
      _owner = owner;
      _attached = true;
      _mine = RagonNetwork.Room.LocalPlayer.Id == owner.Id;
      _spawnPayload = payloadData;
      _replication = true;

      foreach (var state in _behaviours)
        state.Attach(this);

      var propertyIdGenerator = 0;
      foreach (var property in _properties)
      {
        property.Attach(this, propertyIdGenerator);
        propertyIdGenerator++;
      }
    }

    public void ChangeOwner(RagonPlayer newOwner)
    {
      _owner = newOwner;
      _mine = RagonNetwork.Room.LocalPlayer.Id == newOwner.Id;
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
      if (!_propertiesChanged) return;

      var maskChanges = 0L;
      
      serializer.Clear();
      serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_STATE);
      serializer.WriteUShort((ushort) _entityId);
      var offset = serializer.Lenght;
      serializer.WriteLong(maskChanges);

      foreach (var prop in _properties)
      {
        if (prop.IsDirty)
        {
          maskChanges |= (uint) (1 << prop.Id);
          prop.Serialize(serializer);
          prop.Clear();
        }
      }
      
      serializer.WriteLong(maskChanges, offset);
      
      _propertiesChanged = false;
      
      var sendData = serializer.ToArray();
      RagonNetwork.Connection.SendData(sendData);
    }

    internal void ProcessState(RagonSerializer data)
    {
      var maskChanges = data.ReadLong();
      for (int i = 0; i < _properties.Count; i++)
      {
        if (((maskChanges >> i) & 1) == 1)
        {
          _properties[i].Deserialize(data);
        }
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
      var evntId = RagonNetwork.EventManager.GetEventCode(evnt);
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
      _serializer.WriteUShort(evntId);
      _serializer.WriteByte((byte) replicationMode);
      _serializer.WriteByte((byte) target);
      _serializer.WriteUShort((ushort) Id);
      
      evnt.Serialize(_serializer);

      var sendData = _serializer.ToArray();
      RagonNetwork.Connection.SendData(sendData);
    }
    
    internal void ProcessEvent(RagonPlayer player, ushort eventCode, RagonSerializer data)
    {
      foreach (var behaviour in _behaviours)
        behaviour.ProcessEvent(player, eventCode, data);
    }
  }
}