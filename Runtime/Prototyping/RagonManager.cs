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
  public class RagonManager : MonoBehaviour, IRagonNetworkListener
  {
    [Header("Connection")] 
    [SerializeField] private string _key = "defaultkey";
    
    [Header("Room")]
    [SerializeField] private string _map = "defaultmap";
    [SerializeField] private int _maxPlayers = 2; 
    [SerializeField] private int _minPlayers = 1; 
    
    public static RagonManager Instance { get; private set; }
    
    public void PrefabCallback(Func<ushort, GameObject> action) => _prefabCallback = action;

    public event Action OnJoinSuccess;
    public event Action OnJoinFailed;
    
    private Dictionary<int, IRagonStateListener> _stateListeners = new Dictionary<int, IRagonStateListener>();
    private Dictionary<int, IRagonEventListener> _eventListeners = new Dictionary<int, IRagonEventListener>();
    private List<IRagonEventListener> _globalListeners = new List<IRagonEventListener>();
    private Dictionary<int, IRagonEntity> _entities = new Dictionary<int, IRagonEntity>();
    private Func<ushort, GameObject> _prefabCallback;

    private void Awake()
    {
      Instance = this;
      
      RagonNetwork.SetListener(this);
    }

    public void AddStateListener(int entityId , IRagonStateListener listener) => _stateListeners.Add(entityId, listener);
    public void RemoveStateListener(int entityId) => _stateListeners.Remove(entityId);

    public void AddEntityEventListener(int entityId, IRagonEventListener listener) => _eventListeners.Add(entityId, listener);
    public void RemoveEntityEventListener(int entityId) => _eventListeners.Remove(entityId);

    public void AddGlobalEventListener(IRagonEventListener listener) => _globalListeners.Add(listener);
    public void RemoveGlobalEventListener(IRagonEventListener listener) => _globalListeners.Remove(listener);
    
    public void OnJoined()
    {
      Debug.Log($"Room {RagonNetwork.Room.Id} {RagonNetwork.Room.LocalPlayer.Id}:{RagonNetwork.Room.LocalPlayer.PeerId}"); 
      OnJoinSuccess?.Invoke();
    }

    public void OnFailed()
    {
      OnJoinFailed?.Invoke();
    }

    public void OnLeaved()
    {
     
    }

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
      RagonNetwork.CreateOrJoin(_map, _minPlayers, _maxPlayers);
    }

    public void OnPlayerJoined(RagonPlayer player)
    {
      
    }

    public void OnPlayerLeft(RagonPlayer player)
    {
      
    }

    public void OnEntityCreated(int entityId, ushort entityType, RagonAuthority state, RagonAuthority evnt, RagonPlayer creator, BitBuffer payload)
    {
      Debug.Log($"Entity {entityId} Type:{entityType} Authority:{state}|{evnt} Owner:{creator.Name}");
      
      var prefab = _prefabCallback?.Invoke(entityType);
      var go = Instantiate(prefab);
      var component = go.GetComponent<IRagonEntity>();
      
      component.Attach(entityType, creator, entityId);
      
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
      Debug.Log($"{entityId} {evntCode} {payload.Length}");
      
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
      RagonNetwork.Room.SceneLoaded();
    }
  }
}