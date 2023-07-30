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
using Ragon.Protocol;
using TMPro;
using UnityEngine;

namespace Ragon.Client.Unity
{
  [DefaultExecutionOrder(-1500), DisallowMultipleComponent]
  public class RagonNetwork : MonoBehaviour, IRagonEntityListener
  {
    private static RagonNetwork _instance;
    private Dictionary<RagonEntity, RagonLink> _links;
    private IRagonPrefabSpawner _spawner;
    private RagonClient _networkClient;

    [SerializeField] private RagonConfiguration _configuration;
    [SerializeField] private RagonPrefabRegistry _registry;

    public static bool IsInitialized => _instance != null;
    public static RagonConfiguration Configuration => _instance._configuration;
    public static RagonStatus Status => _instance._networkClient.Status;
    public static RagonEventCache Event => _instance._networkClient.Event;
    public static RagonRoom Room => _instance._networkClient.Room;
    public static RagonSession Session => _instance._networkClient.Session;
    public static RagonClient Network => _instance._networkClient;
    
    private void Awake()
    {
      _instance = this;
      
      DontDestroyOnLoad(gameObject);
      
      var defaultLogger = new RagonUnityLogger();
      RagonLog.Set(defaultLogger);
  
      _links = new Dictionary<RagonEntity, RagonLink>();
      _registry.Cache();

      switch (_configuration.Type)
      {
        case RagonConnectionType.UDP:
        {
          var enetConnection = new RagonENetConnection();
          _networkClient = new RagonClient(enetConnection, _configuration.Rate);
          break;
        }
        case RagonConnectionType.WebSocket:
        {
          var webSocketConnection = new RagonWebSocketConnection();
          _networkClient = new RagonClient(webSocketConnection, _configuration.Rate);
          break;
        }
        default:
        {
          var enetConnection = new RagonENetConnection();
          _networkClient = new RagonClient(enetConnection, _configuration.Rate);
          break;
        }
      }
      
      Configure(new RagonSceneCollector(), new RagonPrefabSpawner());
    }

    public static void Configure(IRagonSceneCollector collector, IRagonPrefabSpawner spawner)
    {
      if (_instance._networkClient.Status != RagonStatus.DISCONNECTED)
      {
        RagonLog.Warn("You can't configure client when you connected to server");
        return;
      }

      if (collector != null)
        _instance._networkClient.Configure(collector);

      if (spawner != null)
        _instance._spawner = spawner;
      
      _instance._networkClient.Configure(_instance);
    }


    private void Update()
    {
      _networkClient.Update(Time.unscaledTime);
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

      var go = _spawner.InstantiateEntityGameObject(entity, prefab);
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
      var configuration = _instance._configuration;
      _instance._networkClient.Connect(configuration.Address, configuration.Port, configuration.Protocol);
    }

    public static void Disconnect()
    {
      _instance._networkClient.Disconnect();
    }

    public static GameObject Create(GameObject prefab, IRagonPayload spawnPayload = null)
    {
      if (prefab.TryGetComponent<RagonLink>(out var prefabLink))
      {
        var spawner = _instance._spawner;
        var entity = new RagonEntity(prefabLink.Type, prefabLink.StaticID);
        
        RagonPayload payload = null;
        if (spawnPayload != null)
        {
          var buffer = new RagonBuffer();
          spawnPayload.Serialize(buffer);
          
          payload = new RagonPayload(buffer.WriteOffset);
          payload.Read(buffer);
          
          entity.AttachPayload(payload);
        }

        var go = spawner.InstantiateEntityGameObject(entity, prefab);
        var link = go.GetComponent<RagonLink>();
        var properties = link.Discovery();
        
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

    public static bool TryGetLink(ushort entityId, out RagonLink link)
    {
      if (!_instance._networkClient.Entity.TryGetEntity(entityId, out var entity))
      {
        link = null;
        return false;
      }
      
      return _instance._links.TryGetValue(entity, out link);
    }

    public static void Transfer(GameObject go, RagonPlayer player)
    {
      if (!go.TryGetComponent<RagonLink>(out var link))
        return;

      var entity = link.Entity;
      _instance._networkClient.Room.TransferEntity(entity, player);
    }

    public static void Destroy(GameObject go, RagonPayload? payload = null)
    {
      if (!go.TryGetComponent<RagonLink>(out var link))
      {
        return;
      }

      var entity = link.Entity;
      
      _instance._networkClient.Room.DestroyEntity(entity);
      _instance._links.Remove(entity);
    }

    public static void AddListener(IRagonListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonAuthorizationListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonConnectionListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonFailedListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonJoinListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonLeftListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonSceneListener listener) => _instance._networkClient.AddListener(listener);
    
    public static void AddListener(IRagonSceneRequestListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonOwnershipChangedListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonPlayerJoinListener listener) => _instance._networkClient.AddListener(listener);

    public static void AddListener(IRagonPlayerLeftListener listener) => _instance._networkClient.AddListener(listener);

    public static void RemoveListener(IRagonListener listener) => _instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonAuthorizationListener listener) => _instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonConnectionListener listener) => _instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonFailedListener listener) => _instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonJoinListener listener) => _instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonLeftListener listener) => _instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonSceneListener listener) => _instance._networkClient.RemoveListener(listener);
    
    public static void RemoveListener(IRagonSceneRequestListener listener) => _instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonOwnershipChangedListener listener) => _instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonPlayerJoinListener listener) => _instance._networkClient.RemoveListener(listener);

    public static void RemoveListener(IRagonPlayerLeftListener listener) => _instance._networkClient.RemoveListener(listener);
  }
}