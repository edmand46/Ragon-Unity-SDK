using System;
using System.Collections.Generic;
using System.Text;
using NetStack.Serialization;
using Ragon.Common;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ragon.Client.Integration
{
  public class RagonEntityManager : MonoBehaviour, IRagonEntityManager
  {
    public static RagonEntityManager Instance { get; private set; }
    
    public void PrefabCallback(Func<ushort, GameObject> action) => _prefabCallback = action;

    private Dictionary<int, IRagonStateListener> _stateListeners = new Dictionary<int, IRagonStateListener>();
    private Dictionary<int, IRagonEventListener> _eventListeners = new Dictionary<int, IRagonEventListener>();
    private Dictionary<int, IRagonEntity> _entitiesDict = new Dictionary<int, IRagonEntity>();
    private List<IRagonEntity> _entitiesList = new List<IRagonEntity>();
    
    private Func<ushort, GameObject> _prefabCallback;

    private void Awake()
    {
      Instance = this;
    }

    public void AddStateListener(int entityId , IRagonStateListener listener) => _stateListeners.Add(entityId, listener);
    public void RemoveStateListener(int entityId) => _stateListeners.Remove(entityId);

    public void AddEntityEventListener(int entityId, IRagonEventListener listener) => _eventListeners.Add(entityId, listener);
    public void RemoveEntityEventListener(int entityId) => _eventListeners.Remove(entityId);
    
    public void OnEntityCreated(int entityId, ushort entityType, RagonAuthority state, RagonAuthority evnt, RagonPlayer creator, BitBuffer payload)
    {
      var prefab = _prefabCallback?.Invoke(entityType);
      var go = Instantiate(prefab);
      
      var component = go.GetComponent<IRagonEntity>();
      component.Attach(entityType, creator, entityId, payload);
      
      _entitiesDict.Add(entityId, component);
      _entitiesList.Add(component);
    }

    public void OnEntityDestroyed(int entityId, BitBuffer payload)
    {
      if (_entitiesDict.Remove(entityId, out var entity))
      {
        _entitiesList.Remove(entity);
        entity.Detach(payload);
      }
    }

    public void OnEntityState(int entityId, BitBuffer payload)
    {
      if (_stateListeners.ContainsKey(entityId)) 
        _stateListeners[entityId].ProcessState(payload);
    }
    
    public void OnEntityEvent(int entityId, ushort evntCode, BitBuffer payload)
    {
      if (_eventListeners.ContainsKey(entityId)) 
        _eventListeners[entityId].ProcessEvent(evntCode, payload);
    }
    
    public void OnOwnerShipChanged(RagonPlayer player)
    {
      foreach (var ent in _entitiesList)
        ent.ChangeOwner(player);
    }
  }
}