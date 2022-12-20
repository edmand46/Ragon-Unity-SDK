using System;
using UnityEngine;
using Ragon.Common;
using ENet;
using Event = ENet.Event;
using EventType = ENet.EventType;

namespace Ragon.Client
{
  public enum DeliveryType
  {
    Reliable,
    Unreliable,
  }

  public class RagonENetConnection: IRagonConnection
  {
    private static bool _libraryLoaded = false;
    
    public Action<byte[]> OnDataReceived;
    public Action OnConnected;
    public Action OnDisconnected;
    public RagonConnectionStatus Status { get; private set; }
    private Host _host;
    private Peer _peer;
    private Event _netEvent;
    
    private double _upstreamBandwidth = 0d;
    private double _downstreamBandwidth = 0d;
    private ulong _upstreamData = 0;
    private ulong _downstreamData = 0;
    private double _time = 0d;
    private double _deltaTime = 0d;
    private double _elapsedTime = 0d;
    private double _lastTime = 0d;
    private const double _interval = 1.0d;
    private System.Diagnostics.Stopwatch _timer;

    public void Send(byte[] data, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      var packet = new Packet();
      if (deliveryType == DeliveryType.Reliable)
      {
        packet.Create(data, PacketFlags.Reliable);
        _peer.Send(0, ref packet);
      }
      else
      {
        packet.Create(data, PacketFlags.UnreliableFragmented);
        _peer.Send(1, ref packet);
      }
    }
    
    public void Prepare()
    {
      if (!_libraryLoaded)
      {
        Library.Initialize();
        _libraryLoaded = true;
      }
    }

    public void Disconnect()
    {
      if (_peer.IsSet)
        _peer.DisconnectNow(0);
    }

    public void Connect(string server, ushort port, uint protocol)
    {
      _host = new Host();
      _host.Create();
      _timer = System.Diagnostics.Stopwatch.StartNew();
      
      Address address = new Address();
      address.SetHost(server);
      address.Port = port;
      
      _peer = _host.Connect(address, 2, protocol);
      _peer.Timeout(32, 5000, 5000);
    }
    public void Update()
    {
      if (_host == null) return;

      bool polled = false;
      while (!polled)
      {
        if (_host.CheckEvents(out _netEvent) <= 0)
        {
          if (_host.Service(0, out _netEvent) <= 0)
            break;

          polled = true;
        }

        switch (_netEvent.Type)
        {
          case EventType.None:
            break;
          case EventType.Connect:
            Status = RagonConnectionStatus.CONNECTED;
            OnConnected?.Invoke();
            break;
          case EventType.Disconnect:
            Status = RagonConnectionStatus.DISCONNECTED;
            OnDisconnected?.Invoke();
            break;
          case EventType.Timeout:
            Status = RagonConnectionStatus.DISCONNECTED;
            OnDisconnected?.Invoke();
            break;
          case EventType.Receive:
            var data = new byte[_netEvent.Packet.Length];
            
            _netEvent.Packet.CopyTo(data);
            _netEvent.Packet.Dispose();
            
            OnDataReceived?.Invoke(data);
            break;
        }
      }
      
      // ComputeBandwidth();
    }

    private void ComputeBandwidth()
    {
      _time += _deltaTime;

      if (_time >= _interval)
      {
        var bytesSent = _peer.IsSet ? _peer.BytesSent : 0;
        var bytesReceived = _peer.IsSet ? _peer.BytesReceived : 0;
        
        if (_upstreamData > 0)
        {
          _upstreamData = bytesSent - _upstreamData;
          _upstreamBandwidth = (_upstreamData / _time) * 8 / (1000 * 1000);
        }

        if (_downstreamData > 0)
        {
          _downstreamData = bytesReceived - _downstreamData;
          _downstreamBandwidth = (_downstreamData / _time) * 8 / (1000 * 1000);
        }

        _upstreamData = bytesSent;
        _downstreamData = bytesReceived;

        _time -= _interval;
      }
      
      _elapsedTime = _timer.ElapsedMilliseconds;
      _deltaTime = (_elapsedTime - _lastTime) / 1000d;
      _lastTime = _elapsedTime;
    }
    
    public void Dispose()
    {
      
      if (_host.IsSet)
      {
        _host?.Flush();
        _host?.Dispose();
      }
      
      if (_libraryLoaded)
        Library.Deinitialize();
    }
  }
}