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
    private Dictionary<int, OnEventDelegate> _events = new();
    
    internal void Attach(RagonObject ragonObject)
    {
      _object = ragonObject;

      OnCreatedEntity();
    }

    internal void Detach()
    {
      OnDestroyedEntity();
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
      
    }

    public virtual void OnCreatedEntity()
    {
    }

    public virtual void OnDestroyedEntity()
    {
    }
  }
}