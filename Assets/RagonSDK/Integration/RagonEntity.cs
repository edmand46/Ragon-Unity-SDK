using System;
using System.Collections.Generic;
using NetStack.Serialization;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Integration
{
  public class RagonEntity<T>: MonoBehaviour, IRagonEventHandler, IRagonStateHandler where T: IRagonSerializable, new()
  {
    private delegate void SubscribeDelegate(BitBuffer buffer);

    [Header("Ragon Properties")]
    
    [SerializeField] protected int EntityType;
    [SerializeField] protected int EntityId;
    [SerializeField] protected uint OwnerId;
    [SerializeField] protected RagonRoom Room;
    [SerializeField] protected bool Attached; 
    
    protected T State;
    
    private T prevState;
    private Dictionary<int, SubscribeDelegate> _events = new();
    
    public void Attach(int entityType, uint ownerId, int entityId, RagonManager manager)
    {
      EntityType = entityType;
      EntityId = entityId;
      OwnerId = ownerId;
      Attached = true;
      
      manager.AddStateListener(EntityId, this);
      manager.AddEntityEventListener(EntityId, this);
      
      State = new T();
      
      OnSpawn();
    }

    public void ProcessState(BitBuffer data)
    {
      State.Deserialize(data);   
      
      OnStateUpdated(prevState, State);
    }

    public void ProcessEvent(ushort eventCode, BitBuffer data)
    {
      if (_events.ContainsKey(eventCode))
        _events[eventCode]?.Invoke(data);
    }

    public void Detach(RagonManager manager)
    {
      
      manager.RemoveStateListener(EntityId);
      manager.RemoveEntityEventListener(EntityId);
      
     OnDespawn(); 
    }

    public virtual void OnSpawn()
    {
      
    }

    public virtual void OnDespawn()
    {
      
    }
    
    public virtual void OnStateUpdated(T prev, T current)
    {
      
    }

    public void Subscribe<Event>(ushort eventCode, Event evnt) where Event: IRagonSerializable, new()
    {
      
    }

    public void SendEvent<Event>(ushort eventCode, Event evnt) where Event : IRagonSerializable, new()
    {
      RagonNetwork.Room.SendEntityEvent(eventCode, EntityId, evnt);
    } 
  }
}