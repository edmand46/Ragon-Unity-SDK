using System;
using System.Collections.Generic;
using Ragon.Client;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  public class RagonEventRegistry
  {
    private Dictionary<Type, ushort> _eventsRegistryByType = new();
    private HashSet<ushort> _codes = new HashSet<ushort>();
    private HashSet<Type> _types = new HashSet<Type>();
    private ushort _eventIdGenerator = 0;

    public ushort GetEventCode<TEvent>(TEvent _) where TEvent: IRagonEvent {
      var type = typeof(TEvent);
      var evntCode = _eventsRegistryByType[type];
      return evntCode;
    }
    
    public void Register<T>() where T: IRagonEvent, new()
    {
      var type = typeof(T);
      if (_types.Contains(type))
      {
        RagonNetwork.Log.Warn($"[Ragon] Event already registered: {type.Name}");
        return;
      }
      RagonNetwork.Log.Trace($"[Ragon] Registered Event: {type.Name} - {_eventIdGenerator}");
      _eventsRegistryByType.Add(type, _eventIdGenerator);
      _codes.Add(_eventIdGenerator);
      _types.Add(type);
      _eventIdGenerator++;
    }
    
    public void Register<T>(ushort evntCode) where T: IRagonEvent, new()
    {
      var type = typeof(T);
      if (_codes.Contains(evntCode) || _types.Contains(type))
      {
        Debug.LogWarning($"[Ragon] Event already registered: {type.Name} - {evntCode}");
        return;
      }
      
      RagonNetwork.Log.Trace($"[Ragon] Registered Event: {type.Name} - {evntCode}");
      
      _codes.Add(evntCode);
      _types.Add(type);
      _eventsRegistryByType.Add(type, evntCode);
    }
    
    public T Create<T>() where T: IRagonEvent, new()
    {
      return new T();
    }
  }
}