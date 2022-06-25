using System;
using DisruptorUnity3d;
using ENet;
using Ragon.Common;
using UnityEngine;
using Event = ENet.Event;
using EventType = ENet.EventType;

namespace Ragon.Client
{
  public enum DeliveryType
  {
    Reliable,
    Unreliable,
  } 
  
  public class RagonConnection
  {
    private Host _host;
    private Peer _peer;
    private Event _netEvent;

    public Action<byte[]> OnData;
    public RingBuffer<Event> SendBuffer;
    public RingBuffer<Event> ReceiveBuffer;
    public Action OnConnected;
    public Action OnDisconnected;

    public void SendData(byte[] data, DeliveryType deliveryType = DeliveryType.Unreliable)
    {
      var packet = new Packet();
      
      if (deliveryType == DeliveryType.Reliable)
      {
        packet.Create(data, PacketFlags.Reliable);
        _peer.Send(0, ref packet);
      }
      else
      {
        packet.Create(data, PacketFlags.None);
        _peer.Send(1, ref packet);
      }
    }

    public void Prepare()
    {
      Library.Initialize();
    }

    public void Connect(string server, ushort port)
    {
      _host = new Host();
      _host.Create();

      Address address = new Address();
      address.SetHost(server);
      address.Port = port;

      _peer = _host.Connect(address, 2, 0);
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
            OnConnected?.Invoke();
            break;
          case EventType.Disconnect:
            OnDisconnected?.Invoke();
            break;
          case EventType.Timeout:
            OnDisconnected?.Invoke();
            break;
          case EventType.Receive:
            var data = new byte[_netEvent.Packet.Length];
            _netEvent.Packet.CopyTo(data);
            _netEvent.Packet.Dispose();
            
            Console.WriteLine(data[0]);
            
            OnData?.Invoke(data);
            break;
        }
      }
    }

    public void Dispose()
    {
      if (_peer.IsSet)
      {
        _peer.DisconnectNow(0);
      }

      if (_host.IsSet)
      {
        _host?.Flush();
        _host?.Dispose();
      }

      Library.Deinitialize();
    }
  }
}