using System;
using System.Collections.Generic;
using NetStack.Serialization;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  public class RagonEntity<T>: MonoBehaviour where T: IRagonSerializable, new()
  {

    private delegate void SubscribeDelegate(ReadOnlySpan<byte> data);

    [SerializeField] protected int EntityType;
    [SerializeField] protected int EntityId;
    [SerializeField] protected uint OwnerId;
    [SerializeField] protected RagonRoom Room;
    
    private T state;
    private T prevState;
    private Dictionary<int, SubscribeDelegate> _events = new();
    
    public void Attach()
    {
      state = new T();
    }

    public void ProcessState(BitBuffer data)
    {
      state.Deserialize(data);      
    }

    public void ProcessEvent(BitBuffer data)
    {
      
    }

    public void Detach()
    {
      
    }

    public virtual void OnSpawn()
    {
      
    }

    public virtual void OnDespawn()
    {
      
    }
    
    public virtual void StateUpdated(T prev, T current)
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