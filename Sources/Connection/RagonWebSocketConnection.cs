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

using System;
using NativeWebSocket;
using Ragon.Protocol;
using UnityEngine;

namespace Ragon.Client.Unity
{
    public class RagonWebSocketConnection : INetworkConnection
    {
        public INetworkChannel Reliable { get; private set; }
        public INetworkChannel Unreliable { get; private set; }
        public Action<byte[]> OnData { get; set; }
        public Action OnConnected { get; set; }
        public Action<RagonDisconnect> OnDisconnected { get; set; }
        public ulong BytesSent { get; }
        public ulong BytesReceived { get; }
        public int Ping { get; }

        private WebSocket _webSocket;

        public async void Disconnect()
        {
            await _webSocket.Close();
        }

        public void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR

            _webSocket.DispatchMessageQueue();
#endif
        }

        public void Prepare()
        {
            
        }

        public async void Connect(string server, ushort port, uint protocol)
        {
            _webSocket = new WebSocket(server);
            _webSocket.OnOpen += OnOpen;
            _webSocket.OnClose += OnClose;
            _webSocket.OnError += OnError;
            _webSocket.OnMessage += OnDataReceived;

            await _webSocket.Connect();
        }

        public async void Dispose()
        {
            await _webSocket.Close();
        }

        private void OnOpen()
        {
            try
            {
                Reliable = new RagonWebSocketReliableChannel(_webSocket, 0);
                Unreliable = new RagonWebSocketReliableChannel(_webSocket, 1);
                
                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        private void OnClose(WebSocketCloseCode code)
        {
            try
            {
                OnDisconnected?.Invoke(RagonDisconnect.SERVER);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        private void OnDataReceived(byte[] data)
        {
            try
            {
                OnData.Invoke(data);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        private void OnError(string message)
        {
            try
            {
                OnDisconnected?.Invoke(RagonDisconnect.TIMEOUT);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex);
            }
        }
    }
}