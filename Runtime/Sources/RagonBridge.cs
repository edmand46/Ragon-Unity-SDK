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

using Ragon.Client;
using Ragon.Client.Unity;
using UnityEngine;

namespace Ragon.Client.Unity
{
  [DefaultExecutionOrder(-10_000)]
  public class RagonBridge : MonoBehaviour
  {
    public static RagonClient Client => _bridge._client;

    private RagonClient _client;
    private static RagonBridge _bridge;
    
    [SerializeField] private RagonConfiguration _configuration;

    private void Awake()
    {
      RagonLog.Set(new RagonUnityLogger());
      
      _bridge = this;
      
      DontDestroyOnLoad(gameObject);

      var defaultLogger = new RagonUnityLogger();
      RagonLog.Set(defaultLogger);

      switch (_configuration.Type)
      {
        case RagonConnectionType.UDP:
        {
          var enetConnection = new RagonENetConnection();
          _client = new RagonClient(enetConnection, _configuration.Rate);
          break;
        }
        case RagonConnectionType.WebSocket:
        {
          var webSocketConnection = new RagonWebSocketConnection();
          _client = new RagonClient(webSocketConnection, _configuration.Rate);
          break;
        }
        default:
        {
          var enetConnection = new RagonENetConnection();
          _client = new RagonClient(enetConnection, _configuration.Rate);
          break;
        }
      }
      
      _client.Configure(new EmptyEntityListener());
      _client.Configure(new EmptySceneCollector());
    }

    
    public static void Connect()
    {
      var configuration = _bridge._configuration;
      var client = _bridge._client;

      client.Connect(configuration.Address, configuration.Port, configuration.Protocol);
    }

    public static void Disconnect()
    {
      var client = _bridge._client;
      client.Disconnect();
    }
    
    private void Update()
    {
      _client?.Update(Time.deltaTime);
    }

    private void OnApplicationQuit()
    {
      _client?.Disconnect();
      _client?.Dispose();
    }
  }
}