using System;
using System.Collections.Generic;
using System.Reflection;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Prototyping
{
  [RequireComponent(typeof(RagonObject))]
  public class RagonEntity : MonoBehaviour
  {
    private delegate void OnEventDelegate(RagonPlayer player, RagonSerializer buffer);

    public bool AutoReplication => _object.AutoReplication;
    public bool IsAttached => _object.IsAttached;
    public bool IsMine => _object.IsMine;
    public RagonPlayer Owner => _object.Owner;
    public RagonObject Object => _object;
    public int Id => _id;

    private int _id;
    private RagonObject _object;
    private List<RagonFloat> _state = new();
    private Dictionary<int, OnEventDelegate> _events = new();

    internal RagonPropertyInfo[] Prepare()
    {
      var fieldFlags = (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
      var fieldInfos = GetType().GetFields(fieldFlags);
      var baseProperty = typeof(RagonFloat);
      var infos = new List<RagonPropertyInfo>();
      
      foreach (var field in fieldInfos)
      {
        if (baseProperty.IsAssignableFrom(field.FieldType))
        {
          var property = (RagonFloat) field.GetValue(this);
          _state.Add(property);
          infos.Add(new RagonPropertyInfo()
          {
            Size = 4,
          });
        }
      }

      return infos.ToArray();
    }

    internal void Attach(RagonObject ragonObject)
    {
      _object = ragonObject;

      OnCreatedEntity();
    }

    internal void Detach()
    {
      OnDestroyedEntity();
    }

    internal void ProcessState(RagonSerializer data)
    {
      // State.Deserialize(data);
    }

    internal void ProcessEvent(RagonPlayer player, ushort eventCode, RagonSerializer data)
    {
      if (_events.ContainsKey(eventCode))
        _events[eventCode]?.Invoke(player, data);
    }

    public void OnEvent<TEvent>(Action<RagonPlayer, TEvent> callback) where TEvent : IRagonEvent, new()
    {
      var eventCode = RagonNetwork.EventManager.GetEventCode<TEvent>(new TEvent());
      if (_events.ContainsKey(eventCode))
      {
        Debug.LogWarning($"Event already {eventCode} subscribed");
        return;
      }

      var t = new TEvent();
      _events.Add(eventCode, (player, buffer) =>
      {
        t.Deserialize(buffer);
        callback.Invoke(player, t);
      });
    }

    public void ReplicateEvent<TEvent>(
      TEvent evnt,
      RagonTarget target = RagonTarget.ALL,
      RagonReplicationMode replicationMode = RagonReplicationMode.SERVER_ONLY)
      where TEvent : IRagonEvent, new()
    {
      // RagonNetwork.Room.ReplicateEntityEvent(, evnt, target, replicationMode);
    }

    public void ReplicateState()
    {
      long bitset = 0;
      var properties = _state.Count;

      var serializer = new RagonSerializer();
      serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_STATE);
      serializer.WriteLong(bitset);

      for (int i = 0; i < properties; i++)
      {
        var ragonProperty = _state[i];
        if (ragonProperty.IsDirty)
        {
          bitset <<= i;
          ragonProperty.Serialize(serializer);
        }
      }

      serializer.WriteLong(bitset, 2);
      var sendData = serializer.ToArray();

      RagonNetwork.Connection.SendData(sendData);
    }

    public virtual void OnCreatedEntity()
    {
    }

    public virtual void OnDestroyedEntity()
    {
    }
  }
}