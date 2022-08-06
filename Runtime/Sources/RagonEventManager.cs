using System;
using System.Collections.Generic;
using Ragon.Client.Prototyping;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Prototyping
{
  public class RagonEventManager
  {
    private Dictionary<Type, ushort> _eventsRegistryByType = new();
    private ushort _eventIdGenerator = 0;
    
    public ushort GetEventCode<TEvent>(TEvent _) where TEvent: IRagonEvent {
      var type = typeof(TEvent);
      var evntCode = _eventsRegistryByType[type];
      return evntCode;
    }
    
    public void Register<T>() where T: IRagonEvent, new()
    {
      var type = typeof(T);
      Debug.Log($"[Ragon] Registered Event: {type.Name} - {_eventIdGenerator}");
      _eventsRegistryByType.Add(type, _eventIdGenerator);
      _eventIdGenerator++;
    }
    
    public T Create<T>() where T: IRagonEvent, new()
    {
      return new T();
    }
  }
}