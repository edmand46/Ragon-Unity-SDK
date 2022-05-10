using System;
using System.Collections.Generic;
using NetStack.Serialization;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Integration
{
  public class RagonEntity<T>: MonoBehaviour, IRagonEventListener, IRagonStateListener, IRagonEntity where T: IRagonSerializable, new()
  {
    private delegate void SubscribeDelegate(BitBuffer buffer);

    [Header("Ragon Properties")]
    
    [SerializeField] protected int EntityType;
    [SerializeField] protected int EntityId;
    [SerializeField] protected uint OwnerId;
    [SerializeField] protected RagonRoom Room;
    
    [SerializeField] protected bool Attached;
    [SerializeField] protected bool IsOwner;
    
    protected T State;
    private T prevState;
    
    private Dictionary<int, SubscribeDelegate> _events = new();
    
    public void Attach(int entityType, uint ownerId, int entityId)
    {
      EntityType = entityType;
      EntityId = entityId;
      OwnerId = ownerId;
      Attached = true;
      IsOwner = RagonNetwork.Room.MyId == OwnerId;
      
      RagonManager.Instance.AddStateListener(EntityId, this);
      RagonManager.Instance.AddEntityEventListener(EntityId, this);
      
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

    public void Detach()
    {
      RagonManager.Instance.RemoveStateListener(EntityId);
      RagonManager.Instance.RemoveEntityEventListener(EntityId);
      
      OnDespawn();
      
      Destroy(gameObject);
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

    public void SendEvent<Event>(ushort eventCode, Event evnt) where Event : IRagonSerializable, new()
    {
      RagonNetwork.Room.SendEntityEvent(eventCode, EntityId, evnt);
    }

    public void ReplicateState()
    {
      RagonNetwork.Room.SendEntityState(EntityId, State);
    }
  }
}