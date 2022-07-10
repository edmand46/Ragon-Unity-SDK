using System;
using System.Collections.Generic;
using NetStack.Serialization;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Prototyping
{
  public class RagonEntityExtended<TState, TSpawnPayload, TDestroyPayload>: 
    MonoBehaviour,
    IRagonEntity,
    IRagonEntityInternal
    
    where TState: IRagonSerializable, new()
    where TSpawnPayload: IRagonPayload, new()
    where TDestroyPayload: IRagonPayload, new()
  {
    private delegate void SubscribeDelegate(RagonPlayer player, BitBuffer buffer);
    public bool AutoReplication => _replication;

    [Header("Ragon Properties")]
    
    [SerializeField] protected int EntityType;
    [SerializeField] protected int EntityId;
    [SerializeField] protected RagonPlayer Owner;
    [SerializeField] protected RagonRoom Room;
    
    [SerializeField] protected bool Attached;
    [SerializeField] protected bool IsMine;
    
    [Header("Other Properties")]
    
    protected TState State;
    
    private bool _replication;
    private Dictionary<int, SubscribeDelegate> _events = new();
    
    public int Id => EntityId;

    public void Attach(int entityType, RagonPlayer owner, int entityId, BitBuffer payloadData)
    {
      EntityType = entityType;
      EntityId = entityId;
      Attached = true;
      Owner = owner;
      IsMine = RagonNetwork.Room.LocalPlayer.Id == owner.Id;
      State = new TState();
      
      _replication = true;
      
      var payload = new TSpawnPayload();
      payload.Deserialize(payloadData);
      
      OnCreatedEntity(payload);
    }

    public void ChangeOwner(RagonPlayer newOwner)
    {
      Owner = newOwner;
      IsMine = RagonNetwork.Room.LocalPlayer.Id == newOwner.Id;
    }

    public void Detach(BitBuffer payloadData)
    {
      var payload = new TDestroyPayload();
      payload.Deserialize(payloadData);
      OnDestroyedEntity(payload);
      
      Destroy(gameObject);
    }
    
    public void ProcessState(BitBuffer data)
    {
      State.Deserialize(data);   
      
      OnStateUpdated();
    }

    public void ProcessEvent(RagonPlayer player, ushort eventCode, BitBuffer data)
    {
      if (_events.ContainsKey(eventCode))
        _events[eventCode]?.Invoke(player, data);
    }

    public void OnEvent<Event>(ushort eventCode, Action<Event> callback) where Event: IRagonSerializable, new()
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

    public void ReplicateEvent<TEvent>(ushort eventCode, TEvent evnt, RagonEventMode eventMode = RagonEventMode.SERVER_ONLY) where TEvent : IRagonSerializable, new()
    {
      RagonNetwork.Room.ReplicateEntityEvent(eventCode, EntityId, evnt, eventMode);
    }

    public void ReplicateState()
    {
      RagonNetwork.Room.ReplicateEntityState(EntityId, State);
    }

    #region VIRTUAL
    
    public virtual void OnCreatedEntity(IRagonPayload payload) 
    {
      
    }

    public virtual void OnDestroyedEntity(IRagonPayload payload)
    {
      
    }
    
    public virtual void OnStateUpdated()
    {
      
    }
    
    #endregion
  }
}