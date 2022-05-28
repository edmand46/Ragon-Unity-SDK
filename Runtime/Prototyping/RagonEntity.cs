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
    [SerializeField] protected RagonPlayer Owner;
    [SerializeField] protected RagonRoom Room;
    
    [SerializeField] protected bool Attached;
    [SerializeField] protected bool IsMine;
    
    protected T State;
    
    private Dictionary<int, SubscribeDelegate> _events = new();
    
    public void Attach(int entityType, RagonPlayer owner, int entityId)
    {
      EntityType = entityType;
      EntityId = entityId;
      Owner = owner;
      Attached = true;
      IsMine = RagonNetwork.Room.LocalPlayer.Id == owner.Id;
      
      RagonManager.Instance.AddStateListener(EntityId, this);
      RagonManager.Instance.AddEntityEventListener(EntityId, this);
      
      State = new T();
      
      OnCreatedEntity();
    }
    
    public void Detach()
    {
      RagonManager.Instance.RemoveStateListener(EntityId);
      RagonManager.Instance.RemoveEntityEventListener(EntityId);
      
      OnDestroyedEntity();
      
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
    
    public virtual void OnCreatedEntity()
    {
      
    }

    public virtual void OnDestroyedEntity()
    {
      
    }
    
    public virtual void OnStateUpdated()
    {
      
    }
  }
}