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

using UnityEngine.SceneManagement;

namespace Ragon.Client.Unity
{
  using System.Collections.Generic;
  using UnityEngine;

  namespace Ragon.Client.Unity
  {
    [DefaultExecutionOrder(-1500), DisallowMultipleComponent]
    public class RagonInstance : MonoBehaviour, IRagonEntityListener
    {
      private Dictionary<RagonEntity, RagonLink> _links;
      private RagonClient _networkClient;

      [SerializeField] private RagonConfiguration _configuration;
      [SerializeField] private RagonPrefabRegistry _registry;

      public RagonConfiguration Configuration => _configuration;
      public RagonStatus Status => _networkClient.Status;
      public RagonEventCache Event => _networkClient.Event;
      public RagonRoom Room => _networkClient.Room;
      public RagonSession Session => _networkClient.Session;
      public RagonClient Network => _networkClient;

      private void Awake()
      {
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

        SceneManager.MoveGameObjectToScene(go, gameObject.scene);

        var link = go.GetComponent<RagonLink>();
        var properties = link.Discovery();

        foreach (var property in properties)
          entity.State.AddProperty(property);

        entity.Attached += link.OnAttached;
        entity.Detached += link.OnDetached;
        entity.OwnershipChanged += link.OnOwnershipChanged;

        _links.Add(entity, link);
      }

      public void AddListener(IRagonListener listener)
      {
        _networkClient.AddListener(listener);
      }

      public void RemoveListener(IRagonListener listener)
      {
        _networkClient.RemoveListener(listener);
      }

      public void Connect()
      {
        if (_networkClient == null)
        {
          Debug.LogError("Network client is null!");
          return;
        }

        _networkClient.Connect(_configuration.Address, _configuration.Port, _configuration.Protocol);
      }

      public void Disconnect()
      {
        _networkClient.Disconnect();
      }

      public GameObject Create(GameObject prefab, IRagonPayload payload = null)
      {
        if (prefab.TryGetComponent<RagonLink>(out var prefabLink))
        {
          var go = Instantiate(prefab);

          SceneManager.MoveGameObjectToScene(go, gameObject.scene);

          var link = go.GetComponent<RagonLink>();
          var properties = link.Discovery();

          var entity = new RagonEntity(prefabLink.Type, prefabLink.StaticID);
          foreach (var property in properties)
            entity.State.AddProperty(property);

          entity.Attached += link.OnAttached;
          entity.Detached += link.OnDetached;
          entity.OwnershipChanged += link.OnOwnershipChanged;

          _networkClient.Room.CreateEntity(entity, payload);
          _links.Add(entity, link);

          return go;
        }

        return null;
      }

      public RagonLink FindByEntityId(ushort entityId)
      {
        var entity = _networkClient.Entity.FindById(entityId);
        return _links[entity];
      }

      public void Destroy(GameObject go, RagonPayload? payload = null)
      {
        if (go.TryGetComponent<RagonLink>(out var link))
        {
          var entity = link.Entity;
          _networkClient.Room.DestroyEntity(entity);
          _links.Remove(entity);
        }
      }
    }
  }
}