/*
 * Copyright 2023 Eduard Kargin <kargin.eduard@gmail.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
 
using System.Collections.Generic;
using UnityEngine;

namespace Ragon.Client.Unity
{
  [DefaultExecutionOrder(-1500), DisallowMultipleComponent]
  public class RagonNetwork : MonoBehaviour, IRagonEntityListener
  {
    private static RagonNetwork _instance;
    private Dictionary<RagonEntity, RagonLink> _links;
    private RagonClient _networkClient;
   
    [SerializeField] private RagonConfiguration _configuration;
    [SerializeField] private RagonPrefabRegistry _registry;
    
    public static RagonConfiguration Configuration => _instance._configuration;
    public static RagonStatus Status => _instance._networkClient.Status;
    public static RagonEventCache Event => _instance._networkClient.Event;
    public static RagonRoom Room => _instance._networkClient.Room;
    public static RagonSession Session => _instance._networkClient.Session;
    public static RagonClient Network => _instance._networkClient;
    
    private void Awake()
    {
      DontDestroyOnLoad(gameObject);

      _instance = this;
      
      RagonLog.Set(new RagonUnityLogger());
      
      _links = new Dictionary<RagonEntity, RagonLink>();
      _registry.Cache();

      var defaultSceneCollector = new RagonSceneCollector();
      switch (_configuration.Type)
      {
        case RagonConnectionType.UDP:
        {
          var enetConnection = new RagonENetConnection();
          _networkClient = new RagonClient(enetConnection, this, defaultSceneCollector, _configuration.Rate);
          break;
        }
        case RagonConnectionType.WebSocket:
        {
          var webSocketConnection = new RagonWebSocketConnection();
          _networkClient = new RagonClient(webSocketConnection, this, defaultSceneCollector, _configuration.Rate);
          break;
        }
        default:
        {
          var enetConnection = new RagonENetConnection();
          _networkClient = new RagonClient(enetConnection, this, defaultSceneCollector, _configuration.Rate);
          break;
        }
      }
    }

    private void Update()
    {
      _networkClient.Update(Time.deltaTime);
    }

    private void OnDestroy()
    {
      _networkClient.Dispose();
    }
    
    public void OnEntityCreated(RagonEntity entity)
    {
      if (!_registry.Prefabs.TryGetValue(entity.Type, out var prefab))
      {
        RagonLog.Trace($"Entity Id: {entity.Id} Type: {entity.Type} not found in Prefab Registry");
        return;
      }

      var go = Instantiate(prefab);
      var link = go.GetComponent<RagonLink>();
      var properties = link.Discovery();

      foreach (var property in properties)
        entity.State.AddProperty(property);

      entity.Attached += link.OnAttached;
      entity.Detached += link.OnDetached;
      entity.OwnershipChanged += link.OnOwnershipChanged;
      
      _instance._links.Add(entity, link);
    }
    

    public static void Connect()
    {
      if (_instance._networkClient == null)
      {
        Debug.LogError("Network client is null!");
        return;
      }

      var configuration = _instance._configuration;
      _instance._networkClient.Connect(configuration.Address, configuration.Port, configuration.Protocol);
    }

    public static void Disconnect()
    {
      _instance._networkClient.Disconnect();
    }

    public static GameObject Create(GameObject prefab, IRagonPayload payload = null)
    {
      if (prefab.TryGetComponent<RagonLink>(out var prefabLink))
      {
        var go = Instantiate(prefab);
        var link = go.GetComponent<RagonLink>();
        var properties = link.Discovery();

        var entity = new RagonEntity(prefabLink.Type, prefabLink.StaticID);
        foreach (var property in properties)
          entity.State.AddProperty(property);

        entity.Attached += link.OnAttached;
        entity.Detached += link.OnDetached;
        entity.OwnershipChanged += link.OnOwnershipChanged;
        
        _instance._networkClient.Room.CreateEntity(entity, payload);
        _instance._links.Add(entity, link);
        
        return go;
      }
      
      return null;
    }

    public static RagonLink FindByEntityId(ushort entityId)
    {
      var entity = _instance._networkClient.Entity.FindById(entityId);
      return _instance._links[entity];
    }

    public static void Destroy(GameObject go, RagonPayload? payload = null)
    {
      if (go.TryGetComponent<RagonLink>(out var link))
      {
        var entity = link.Entity;
        _instance._networkClient.Room.DestroyEntity(entity);
        _instance._links.Remove(entity);
      }
    }
    
    public static void AddListener(IRagonListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonAuthorizationListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonConnectionListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonFailedListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonJoinListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonLeftListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonLevelListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonOwnershipChangedListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonPlayerJoinListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonPlayerLeftListener listener) => _instance._networkClient.AddListener(listener);

    public static void RemoveListener(IRagonListener listener) =>_instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonAuthorizationListener listener) =>_instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonConnectionListener listener) =>_instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonFailedListener listener) =>_instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonJoinListener listener) =>_instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonLeftListener listener) =>_instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonLevelListener listener) =>_instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonOwnershipChangedListener listener) =>_instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonPlayerJoinListener listener) =>_instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonPlayerLeftListener listener) =>_instance._networkClient.RemoveListener(listener);
    
  }
}