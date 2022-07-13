using System;
using System.Collections.Generic;
using NetStack.Serialization;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Prototyping
{
  public class RagonEntity<TState> :
    MonoBehaviour,
    IRagonEntity,
    IRagonEntityInternal
    where TState : IRagonSerializable, new()
  {
    private delegate void OnEventDelegate(RagonPlayer player, BitBuffer buffer);

    public bool AutoReplication => _replication;
    public bool IsAttached => _attached;
    public bool IsMine => _mine;
    public int Id => _entityId;
    public RagonPlayer Owner => _owner;

    [SerializeField] private int _entityType;
    [SerializeField] private int _entityId;
    [SerializeField] private bool _mine;
    [SerializeField] private RagonPlayer _owner;
    [SerializeField] private bool _attached;
    [SerializeField] private bool _replication;

    protected RagonRoom Room;
    protected TState State;

    private byte[] _spawnPayload;

    // private byte[] _destroyPayload;
    private Dictionary<int, OnEventDelegate> _events = new();

    public void Attach(int entityType, RagonPlayer owner, int entityId, byte[] payloadData)
    {
      _entityType = entityType;
      _entityId = entityId;
      _owner = owner;
      _attached = true;
      _mine = RagonNetwork.Room.LocalPlayer.Id == owner.Id;
      _spawnPayload = payloadData;
      _replication = true;
      
      State = new TState();

      OnCreatedEntity();
    }

    public void ChangeOwner(RagonPlayer newOwner)
    {
      _owner = newOwner;
      _mine = RagonNetwork.Room.LocalPlayer.Id == newOwner.Id;
    }

    public void Detach()
    {
      OnDestroyedEntity();
    }

    public void ProcessState(BitBuffer data)
    {
      State.Deserialize(data);
    }

    public void ProcessEvent(RagonPlayer player, ushort eventCode, BitBuffer data)
    {
      if (_events.ContainsKey(eventCode))
        _events[eventCode]?.Invoke(player, data);
    }

    public void OnEvent<Event>(ushort eventCode, Action<Event> callback) where Event : IRagonSerializable, new()
    {
      if (_events.ContainsKey(eventCode))
      {
        Debug.LogWarning($"Event already {eventCode} subscribed");
        return;
      }

      var t = new Event();
      _events.Add(eventCode, (player, buffer) =>
      {
        t.Deserialize(buffer);
        callback.Invoke(t);
      });
    }

    internal T GetPayload<T>(byte[] data) where T : IRagonPayload, new()
    {
      if (data == null) return new T();
      if (data.Length == 0) return new T();
      
      var buffer = new BitBuffer();
      buffer.FromArray(data, data.Length);

      var payload = new T();
      payload.Deserialize(buffer);

      return payload;
    }

    public int Type { get; private set; }

    public T GetSpawnPayload<T>() where T : IRagonPayload, new()
    {
      return GetPayload<T>(_spawnPayload);
    }

    // public T GetDestroyPayload<T>() where T: IRagonPayload, new()
    // {
    //   return GetPayload<T>(_destroyPayload);
    // }

    public void ReplicateEvent<TEvent>(ushort eventCode, TEvent evnt, RagonEventMode eventMode = RagonEventMode.SERVER_ONLY)
      where TEvent : IRagonSerializable, new()
    {
      RagonNetwork.Room.ReplicateEntityEvent(eventCode, _entityId, evnt, eventMode);
    }

    public void ReplicateState()
    {
      RagonNetwork.Room.ReplicateEntityState(_entityId, State);
    }

    #region VIRTUAL

    public virtual void OnCreatedEntity()
    {
    }

    public virtual void OnDestroyedEntity()
    {
    }

    #endregion
  }
}