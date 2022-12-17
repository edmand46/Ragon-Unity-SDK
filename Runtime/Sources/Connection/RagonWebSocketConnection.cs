using System;
using NativeWebSocket;
using Ragon.Common;
using Debug = UnityEngine.Debug;

namespace Ragon.Client
{
  public class RagonWebSocketConnection: IRagonConnection, IDisposable
  {
    public RagonConnectionStatus Status { get; private set; }
    public Action<byte[]> OnData;
    public Action OnConnected;
    public Action OnDisconnected;
    
    private WebSocket _webSocket;
    
    public void Send(byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      _webSocket.Send(rawData);
    }

    public async void Connect(string server, ushort port, uint protocol)
    {
      _webSocket = new WebSocket(server);
      _webSocket.OnOpen += () =>
      {
        Status = RagonConnectionStatus.CONNECTED;
        OnConnected?.Invoke();
      };
      _webSocket.OnClose += (code) =>
      {
        Status = RagonConnectionStatus.DISCONNECTED;
        OnDisconnected?.Invoke();
      };
      
      _webSocket.OnError += (err) => Debug.LogError(err);
      _webSocket.OnMessage += data => OnData?.Invoke(data);
      
      await _webSocket.Connect();
    }

    public async void Disconnect()
    {
      await _webSocket.Close();
    }

    public void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
      if (Status == RagonConnectionStatus.CONNECTED)
      {
        _webSocket.DispatchMessageQueue();
      }
#endif
    }

    public async void Dispose()
    {
      if (Status == RagonConnectionStatus.CONNECTED)
      {
        await _webSocket.Close();
      }
    }
  }
}