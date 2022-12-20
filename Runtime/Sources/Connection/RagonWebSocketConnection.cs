using System;
using NativeWebSocket;
using Debug = UnityEngine.Debug;

namespace Ragon.Client
{
  public class RagonWebSocketConnection : IRagonConnection, IDisposable
  {
    public Action<byte[]> OnDataReceived;
    public Action OnConnected;
    public Action OnDisconnected;
    public RagonConnectionState Status { get; private set; }

    private WebSocket _webSocket;

    public void Send(byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      _webSocket.Send(rawData);
    }

    public async void Disconnect()
    {
      await _webSocket.Close();
    }

    public void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
      if (Status == RagonConnectionState.CONNECTED)
      {
        _webSocket.DispatchMessageQueue();
      }
#endif
    }

    public async void Connect(string server, ushort port, uint protocol)
    {
      _webSocket = new WebSocket(server);
      _webSocket.OnOpen += OnOpen;
      _webSocket.OnClose += OnClose;
      _webSocket.OnError += OnError;
      _webSocket.OnMessage += OnData;
      
      await _webSocket.Connect();
    }

    public async void Dispose()
    {
      if (Status == RagonConnectionState.CONNECTED)
        await _webSocket.Close();
    }

    private void OnOpen()
    {
      try
      {
        Status = RagonConnectionState.CONNECTED;
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
        Status = RagonConnectionState.DISCONNECTED;
        OnDisconnected?.Invoke();
      }
      catch (Exception ex)
      {
        Debug.LogError(ex);
      }
    }

    private void OnData(byte[] data)
    {
      try
      {
        OnDataReceived.Invoke(data);
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
        Status = RagonConnectionState.DISCONNECTED;
        OnDisconnected?.Invoke();
      }
      catch (Exception ex)
      {
        Debug.LogError(ex);
      }
    }
  }
}