using System;
using System.Collections.Generic;
using NetStack.Serialization;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Integration
{
  public class RagonEntityExtended<TState, TSpawnPayload, TDestroyPayload>: MonoBehaviour, IRagonEventListener, IRagonStateListener, IRagonEntity 
    where TState: IRagonSerializable, new()
    where TSpawnPayload: IRagonPayload, new()
    where TDestroyPayload: IRagonPayload, new()
  {
    private delegate void SubscribeDelegate(BitBuffer buffer);

    [Header("Ragon Properties")]
    
    [SerializeField] protected int EntityType;
    [SerializeField] protected int EntityId;
    [SerializeField] protected RagonPlayer Owner;
    [SerializeField] protected RagonRoom Room;
    
    [SerializeField] protected bool Attached;
    [SerializeField] protected bool IsMine;
    
    protected TState State;
    
    private Dictionary<int, SubscribeDelegate> _events = new();
    
    public void Attach(int entityType, RagonPlayer owner, int entityId, BitBuffer payloadData)
    {
      EntityType = entityType;
      EntityId = entityId;
      Attached = true;
      Owner = owner;
      IsMine = RagonNetwork.Room.LocalPlayer.Id == owner.Id;

      RagonEntityManager manager = (RagonEntityManager) RagonNetwork.Manager;
      manager.AddStateListener(EntityId, this);
      manager.AddEntityEventListener(EntityId, this);
      
      State = new TState();
      
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
      RagonEntityManager manager = (RagonEntityManager) RagonNetwork.Manager;
      manager.RemoveStateListener(EntityId);
      manager.RemoveEntityEventListener(EntityId);

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

    public void ProcessEvent(ushort eventCode, BitBuffer data)
    {
      if (_events.ContainsKey(eventCode))
        _events[eventCode]?.Invoke(data);
    }

    public void Subscribe<Event>(ushort eventCode, Action<Event> callback) where Event: IRagonSerializable, new()
    {
      if (_events.ContainsKey(eventCode))
      {
        Debug.LogWarning($"Event already {eventCode} subscribed");
        return; 
      }

      var t = new Event();
      _events.Add(eventCode, (buffer) =>
      {
          t.Deserialize(buffer);
          callback.Invoke(t);
      });
    }

    public void ReplicateEvent<TEvent>(ushort eventCode, TEvent evnt, RagonExecutionMode executionMode = RagonExecutionMode.SERVER_ONLY) where TEvent : IRagonSerializable, new()
    {
      RagonNetwork.Room.SendEntityEvent(eventCode, EntityId, evnt, executionMode);
    }

    public void ReplicateState()
    {
      RagonNetwork.Room.SendEntityState(EntityId, State);
    }

    public virtual void OnCreatedEntity(IRagonPayload payload) 
    {
      
    }

    public virtual void OnDestroyedEntity(IRagonPayload payload)
    {
      
    }
    
    public virtual void OnStateUpdated()
    {
      
    }
  }
}