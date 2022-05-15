using System;
using System.Collections.Generic;
using System.Text;
using NetStack.Serialization;
using Ragon.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ragon.Client.Integration
{
  [DefaultExecutionOrder(-1000)]
  public class RagonManager : MonoBehaviour, IRagonManager
  {
    [Header("Connection")] 
    [SerializeField] private string _key = "defaultkey";
    
    [Header("Room")]
    [SerializeField] private string _map = "defaultmap";
    [SerializeField] private int _maxPlayers = 1; 
    [SerializeField] private int _minPlayers = 2; 
    
    public static RagonManager Instance { get; private set; }

    private Dictionary<int, IRagonStateListener> _stateListeners = new Dictionary<int, IRagonStateListener>();
    private Dictionary<int, IRagonEventListener> _eventListeners = new Dictionary<int, IRagonEventListener>();
    private List<IRagonEventListener> _globalListeners = new List<IRagonEventListener>();
    private Dictionary<int, IRagonEntity> _entities = new Dictionary<int, IRagonEntity>();
    private Func<ushort, GameObject> _prefabCallback;

    private void Awake()
    {
      Instance = this;
      
      RagonNetwork.SetManager(this);
    }

    public void AddStateListener(int entityId , IRagonStateListener listener) => _stateListeners.Add(entityId, listener);
    public void RemoveStateListener(int entityId) => _stateListeners.Remove(entityId);

    public void AddEntityEventListener(int entityId, IRagonEventListener listener) => _eventListeners.Add(entityId, listener);
    public void RemoveEntityEventListener(int entityId) => _eventListeners.Remove(entityId);

    public void AddGlobalEventListener(IRagonEventListener listener) => _globalListeners.Add(listener);
    public void RemoveGlobalEventListener(IRagonEventListener listener) => _globalListeners.Remove(listener);

    public void PrefabCallback(Func<ushort, GameObject> action) => _prefabCallback = action;
    
    public void OnConnected()
    {
      var apiKey = Encoding.UTF8.GetBytes(_key);
      RagonNetwork.AuthorizeWithData(apiKey);
    }

    public void OnDisconnected()
    {
      
    }
    
    public void OnAuthorized(BitBuffer payload)
    {
      RagonNetwork.FindRoomAndJoin(_map, _minPlayers, _maxPlayers);
    }

    public void OnReady()
    {
      Debug.Log($"Room {RagonNetwork.Room.Id}");
    }

    public void OnEntityCreated(int entityId, ushort entityType, ushort ownerId, BitBuffer payload)
    {
      var prefab = _prefabCallback?.Invoke(entityType);
      var go = Instantiate(prefab);
      var component = go.GetComponent<IRagonEntity>();
      
      component.Attach(entityType, ownerId, entityId);
      
      _entities.Add(entityId, component);
    }
    public void OnEntityDestroyed(int entityId, BitBuffer payload)
    {
      if (_entities.Remove(entityId, out var entity))
        entity.Detach();
    }

    public void OnEntityState(int entityId, BitBuffer payload)
    {
      if (_stateListeners.ContainsKey(entityId)) 
        _stateListeners[entityId].ProcessState(payload);
    }

    public void OnEntityProperty(int entityId, int property, BitBuffer payload)
    {
      
    }

    public void OnEntityEvent(int entityId, ushort evntCode, BitBuffer payload)
    {
      if (_eventListeners.ContainsKey(entityId)) 
        _eventListeners[entityId].ProcessEvent(evntCode, payload);
    }

    public void OnEvent(ushort evntCode, BitBuffer payload)
    {
      foreach (var handler in _globalListeners)
        handler.ProcessEvent(evntCode, payload);
    }
    
    public void OnLevel(string sceneName)
    {
      // SceneManager.LoadScene(sceneName);
    }
  }
}