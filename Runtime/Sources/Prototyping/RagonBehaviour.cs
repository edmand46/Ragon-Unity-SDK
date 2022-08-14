using System;
using System.Collections.Generic;
using System.Reflection;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Prototyping
{
  [RequireComponent(typeof(RagonEntity))]
  public class RagonBehaviour : MonoBehaviour
  {
    private delegate void OnEventDelegate(RagonPlayer player, RagonSerializer buffer);
    
    public RagonPlayer Owner => _entity.Owner;
    public RagonEntity Entity => _entity;
    public bool IsMine => _mine;

    private bool _mine;
    private RagonEntity _entity;
    private Dictionary<int, OnEventDelegate> _events = new();
    
    internal void Attach(RagonEntity ragonEntity)
    {
      _entity = ragonEntity;
      _mine = ragonEntity.IsMine;
      
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
      _entity.ReplicateEvent(evnt, target, replicationMode);
    }

    public virtual void OnCreatedEntity()
    {
    }

    public virtual void OnDestroyedEntity()
    {
    }
  }
}