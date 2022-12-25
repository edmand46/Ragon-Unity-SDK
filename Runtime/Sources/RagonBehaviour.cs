using System;
using System.Collections.Generic;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  public class RagonBehaviour : MonoBehaviour
  {
    private delegate void OnEventDelegate(RagonPlayer player, RagonSerializer serializer);

    public RagonPlayer Owner => _entity.Owner;
    public RagonEntity Entity => _entity;
    public bool IsMine => _mine;

    private bool _mine;
    private RagonEntity _entity;
    private Dictionary<int, OnEventDelegate> _events = new();
    private Dictionary<int, Action<RagonPlayer, IRagonEvent>> _localEvents = new();

    internal void Attach(RagonEntity ragonEntity)
    {
      _entity = ragonEntity;
      _mine = ragonEntity.IsMine;

      OnAttachedEntity();
    }

    internal void Detach()
    {
      OnDetachedEntity();
    }

    internal void ProcessEvent(RagonPlayer player, ushort eventCode, RagonSerializer data)
    {
      if (_events.ContainsKey(eventCode))
        _events[eventCode]?.Invoke(player, data);
    }

    public void OnEvent<TEvent>(Action<RagonPlayer, TEvent> callback) where TEvent : IRagonEvent, new()
    {
      var t = new TEvent();
      var eventCode = RagonNetwork.Event.GetEventCode(t);

      if (_events.ContainsKey(eventCode))
      {
        RagonNetwork.Log.Warn($"Event already {eventCode} subscribed");
        return;
      }

      _localEvents.Add(eventCode, (player, eventData) => { callback.Invoke(player, (TEvent) eventData); });

      _events.Add(eventCode, (player, serializer) =>
      {
        t.Deserialize(serializer);
        callback.Invoke(player, t);
      });
    }

    public void ReplicateEvent<TEvent>(
      TEvent evnt,
      RagonTarget target = RagonTarget.All,
      RagonReplicationMode replicationMode = RagonReplicationMode.Server)
      where TEvent : IRagonEvent, new()
    {
      if (target != RagonTarget.ExceptOwner)
      {
        if (replicationMode == RagonReplicationMode.Local)
        {
          var eventCode = RagonNetwork.Event.GetEventCode(evnt);
          _localEvents[eventCode].Invoke(RagonNetwork.Room.LocalPlayer, evnt);
          return;
        }

        if (replicationMode == RagonReplicationMode.LocalAndServer)
        {
          var eventCode = RagonNetwork.Event.GetEventCode(evnt);
          _localEvents[eventCode].Invoke(RagonNetwork.Room.LocalPlayer, evnt);
        }
      }

      _entity.ReplicateEvent(evnt, target, replicationMode);
    }

    public virtual void OnAttachedEntity()
    {
    }

    public virtual void OnDetachedEntity()
    {
    }

    public virtual void OnUpdateEntity()
    {
    }

    public virtual void OnUpdateProxy()
    {
    }
    
    public virtual void OnLateUpdateEntity()
    {
    }
    
    public virtual void OnLateUpdateProxy()
    {
    }

    public virtual void OnFixedUpdateEntity()
    {
    }
    
    public virtual void OnFixedUpdateProxy()
    {
    }

    public virtual void OnOwnershipChanged(RagonPlayer player)
    {
      
    }
  }
}