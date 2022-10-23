using System;
using NativeWebSocket;
using Ragon.Common;
using Debug = UnityEngine.Debug;

namespace Ragon.Client
{
  public class RagonWebSocketConnection: IRagonConnection, IDisposable
  {
    public Action<byte[]> OnData;
    public Action OnConnected;
    public Action OnDisconnected;
    
    public uint Ping { get; }
    public double UpstreamBandwidth { get; }
    public double DownstreamBandwidth { get; }
    public RagonConnectionState ConnectionState;
    
    private WebSocket _webSocket;
    
    public RagonWebSocketConnection()
    {
      
    }
    
    public void Send(RagonSerializer serializer, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      var sendData = serializer.ToArray();
      _webSocket.Send(sendData);
    }

    public void Send(byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      _webSocket.Send(rawData);
    }

    public async void Connect(string server, ushort port, uint protocol)
    {
      _webSocket = new WebSocket(server);
      _webSocket.OnOpen += () => OnConnected?.Invoke();
      _webSocket.OnClose += (code) => OnDisconnected?.Invoke();
      _webSocket.OnError += (err) => Debug.LogError(err);
      _webSocket.OnMessage += data => OnData?.Invoke(data);
      
      await _webSocket.Connect();
    }

    public void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
      _webSocket.DispatchMessageQueue();
#endif
    }

    public async void Dispose()
    {
      await _webSocket.Close();
    }
  }
}