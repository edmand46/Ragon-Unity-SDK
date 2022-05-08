using System;
using System.Collections.Generic;
using System.Text;
using NetStack.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ragon.Client.Integration
{
  public class RagonManager : MonoBehaviour, IRagonHandler
  {
    public static RagonManager Instance { get; private set; }

    private Dictionary<int, IRagonStateHandler> _stateHandlers = new Dictionary<int, IRagonStateHandler>();
    private Dictionary<int, IRagonEventHandler> _eventHandlers = new Dictionary<int, IRagonEventHandler>();
    private List<IRagonEventHandler> _globalHandlers = new List<IRagonEventHandler>();

    private void Awake()
    {
      Instance = this;
    }

    public void AddStateListener(int entityId , IRagonStateHandler handler) => _stateHandlers.Add(entityId, handler);
    public void RemoveStateListener(int entityId) => _stateHandlers.Remove(entityId);

    public void AddEntityEventListener(int entityId, IRagonEventHandler handler) => _eventHandlers.Add(entityId, handler);
    public void RemoveEntityEventListener(int entityId) => _eventHandlers.Remove(entityId);

    public void AddGlobalEventListener(IRagonEventHandler handler) => _globalHandlers.Add(handler);
    public void RemoveGlobalEventListener(IRagonEventHandler handler) => _globalHandlers.Remove(handler);
 
    public void OnConnected()
    {
      var apiKey = Encoding.UTF8.GetBytes("123");
      RagonNetwork.AuthorizeWithData(apiKey);
    }

    public void OnDisconnected()
    {
      
    }
    
    public void OnAuthorized(BitBuffer payload)
    {
      RagonNetwork.FindRoomAndJoin("Example Map", 1, 2);
    }

    public void OnReady()
    {
      
    }

    public void OnEntityCreated(int entityId, ushort entityType, ushort ownerId, BitBuffer payload)
    {
      
    }
    public void OnEntityDestroyed(int entityId, BitBuffer payload)
    {
      ;
    }

    public void OnEntityState(int entityId, BitBuffer payload)
    {
      
    }

    public void OnEntityProperty(int entityId, int property, BitBuffer payload)
    {
      
    }

    public void OnEntityEvent(int entityId, ushort evntCode, BitBuffer payload)
    {
      if (_eventHandlers.ContainsKey(entityId)) 
        _eventHandlers[entityId].ProcessEvent(evntCode, payload);
    }

    public void OnEvent(ushort evntCode, BitBuffer payload)
    {
      foreach (var handler in _globalHandlers)
        handler.ProcessEvent(evntCode, payload);
    }
    
    public void OnLevel(string sceneName)
    {
      SceneManager.LoadScene(sceneName);
    }
  }
}