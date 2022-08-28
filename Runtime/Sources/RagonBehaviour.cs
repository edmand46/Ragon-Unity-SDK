using System;
using System.Collections.Generic;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  [RequireComponent(typeof(RagonEntity))]
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
      var t = new TEvent();
      var eventCode = RagonNetwork.Event.GetEventCode(t);

      if (_events.ContainsKey(eventCode))
      {
        Debug.LogWarning($"Event already {eventCode} subscribed");
        return;
      }

      _localEvents.Add(eventCode, (RagonPlayer player, evnt) => { callback.Invoke(player, (TEvent) evnt); });

      _events.Add(eventCode, (player, serializer) =>
      {
        t.Deserialize(serializer);
        callback.Invoke(player, t);
      });
    }

    public void ReplicateEvent<TEvent>(
      TEvent evnt,
      RagonTarget target = RagonTarget.ALL,
      RagonReplicationMode replicationMode = RagonReplicationMode.SERVER_ONLY)
      where TEvent : IRagonEvent, new()
    {
      if (replicationMode == RagonReplicationMode.LOCAL_ONLY)
      {
        var eventCode = RagonNetwork.Event.GetEventCode(evnt);
        _localEvents[eventCode].Invoke(RagonNetwork.Room.LocalPlayer, evnt);
        return;
      }

      if (replicationMode == RagonReplicationMode.LOCAL_AND_SERVER)
      {
        var eventCode = RagonNetwork.Event.GetEventCode(evnt);
        _localEvents[eventCode].Invoke(RagonNetwork.Room.LocalPlayer, evnt);
      }

      _entity.ReplicateEvent(evnt, target, replicationMode);
    }

    public virtual void OnCreatedEntity()
    {
    }

    public virtual void OnDestroyedEntity()
    {
    }

    public virtual void OnEntityTick()
    {
    }

    public virtual void OnProxyTick()
    {
    }
  }
}