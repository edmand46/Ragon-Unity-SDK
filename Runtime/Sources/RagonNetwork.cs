/*
 * Copyright 2023-2024 Eduard Kargin <kargin.eduard@gmail.com>
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

using Ragon.Client.Utility;
using Ragon.Protocol;
using UnityEngine;

namespace Ragon.Client.Unity
{
  [DefaultExecutionOrder(-1500), DisallowMultipleComponent]
  public class RagonNetwork : MonoBehaviour,
    IRagonEntityListener,
    IRagonSceneRequestListener
  {
    private static RagonNetwork _instance;
    private RagonLinkFinder _finder;
    private IRagonPrefabSpawner _spawner;
    private RagonClient _networkClient;
    private bool _sceneLoading = true;
    private SceneRequest _sceneRequest;

    [SerializeField] private RagonConfiguration _configuration;
    [SerializeField] private RagonPrefabRegistry _registry;

    public static bool AutoSceneLoading
    {
      get => _instance._sceneLoading;
      set
      {
        if (_instance._networkClient.Status != RagonStatus.DISCONNECTED) return;

        _instance._sceneLoading = value;
      }
    }

    public static RagonConfiguration Configuration => _instance._configuration;
    public static RagonClient Network => _instance._networkClient;
    public static RagonStatus Status => _instance._networkClient.Status;
    public static RagonEventCache Event => _instance._networkClient.Event;
    public static RagonSession Session => _instance._networkClient.Session;
    public static RagonRoom Room => _instance._networkClient.Room;

    private void Awake()
    {
      _instance = this;
      _sceneLoading = true;

      DontDestroyOnLoad(gameObject);

      var defaultLogger = new RagonUnityLogger();
      RagonLog.Set(defaultLogger);

      _finder = new RagonLinkFinder();
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

      var collector = new RagonSceneCollector(_finder);
      var spawner = new RagonPrefabSpawner();

      Configure(collector, spawner);
    }


    public void OnRequestScene(RagonClient client, string sceneName)
    {
      _sceneRequest?.Dispose();
      _sceneRequest = new SceneRequest(client.Room);
      _sceneRequest.OnRequestScene(client, sceneName);
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
    }

    public static void Connect()
    {
      var configuration = _instance._configuration;
      var client = _instance._networkClient;

      if (_instance._sceneLoading)
      {
        AddListener(_instance);
      }

      client.Connect(configuration.Address, configuration.Port, configuration.Protocol);
    }

    public static void Disconnect()
    {
      var client = _instance._networkClient;
      client?.Disconnect();
    }

    public static GameObject Create(GameObject prefab, IRagonPayload spawnPayload = null)
    {
      if (_instance._networkClient.Status != RagonStatus.ROOM)
      {
        RagonLog.Error("You should be in room for this create entities");
        return null;
      }

      if (prefab.TryGetComponent<RagonLink>(out var prefabLink))
      {
        var client = _instance._networkClient;
        var local = client.Room.Local;
        var spawner = _instance._spawner;
        var entity = new RagonEntity(prefabLink.Type, prefabLink.StaticID);
        var payload = RagonPayload.Empty;
        
        if (spawnPayload != null)
        {
          var buffer = new RagonBuffer();
          spawnPayload.Serialize(buffer);

          payload = new RagonPayload(buffer.WriteOffset);
          payload.Read(buffer);
        }
        
        entity.Prepare(client, 0, prefabLink.Type, true, local, payload);
        
        var go = spawner.InstantiateEntityGameObject(entity, prefab);
        var link = go.GetComponent<RagonLink>();
        var properties = link.Discovery();

        foreach (var property in properties)
          entity.State.AddProperty(property);

        entity.Attached += link.OnAttached;
        entity.Detached += link.OnDetached;
        entity.OwnershipChanged += link.OnOwnershipChanged;

        _instance._networkClient.Room.CreateEntity(entity, payload);
        _instance._finder.Track(entity, link);

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

      return _instance._finder.Find(entity, out link);
    }

    public static void Transfer(GameObject go, RagonPlayer player)
    {
      if (!go.TryGetComponent<RagonLink>(out var link))
      {
        RagonLog.Error($"{go.name} has not RagonLink component!");
        return;
      }

      if (!link.Entity.HasAuthority)
      {
        RagonLog.Error($"{go.name} have not authority!");
        return;
      }

      var entity = link.Entity;
      _instance._networkClient.Room.TransferEntity(entity, player);
    }

    public static void Destroy(GameObject go, RagonPayload? payload = null)
    {
      if (!go.TryGetComponent<RagonLink>(out var link))
      {
        RagonLog.Error($"{go.name} has not RagonLink component!");
        return;
      }

      var entity = link.Entity;

      _instance._networkClient.Room.DestroyEntity(entity);
      _instance._finder.Untrack(entity);
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

    public static void AddListener(IRagonDataListener listener) => _instance._networkClient.AddListener(listener);

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

    public static void RemoveListener(IRagonDataListener listener) => _instance._networkClient.RemoveListener(listener);
  }
}