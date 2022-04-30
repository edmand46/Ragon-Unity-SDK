using System;
using ENet;
using Ragon.Core;
using UnityEngine;
using Event = ENet.Event;
using EventType = ENet.EventType;

namespace RagonSDK
{
  public class RagonConnection
  {
    private Host _host;
    private Peer _peer;
    private Event _netEvent;

    public Action<byte[]> OnData;
    public Action OnConnected;
    public Action OnDisconnected;

    public void SendData(byte[] data)
    {
      var packet = new Packet();
      packet.Create(data, PacketFlags.Reliable);

      _peer.Send(0, ref packet);
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
            Debug.Log("Event connected");
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
            OnData?.Invoke(data);
            break;
        }
      }
    }

    public void Send(ushort code, IPacket payload)
    {
      // _bitBuffer.Clear();
      // payload.Serialize(_bitBuffer);
      // _bitBuffer.ToArray(_sendBuffer);
      //
      // var data = new byte[_bitBuffer.Length + 2];
      // Array.Copy(_sendBuffer, 0, data, 2, _bitBuffer.Length);
      // ProtocolHeader.WriteOperation(code, data);
      //
      // var packet = default(Packet);
      // packet.Create(data, data.Length, PacketFlags.Instant);
      // _peer.Send(0, ref packet);
    }

    public void Send(ushort code, byte[] payload)
    {
      // _bitBuffer.Clear();
      // // payload.Serialize(_bitBuffer);
      // _bitBuffer.ToArray(_sendBuffer);
      //
      // var data = new byte[_bitBuffer.Length + 2];
      // Array.Copy(_sendBuffer, 0, data, 2, _bitBuffer.Length);
      // ProtocolHeader.WriteOperation(code, data);
      //
      // var packet = default(Packet);
      // packet.Create(data, data.Length, PacketFlags.Instant);
      // _peer.Send(0, ref packet);
    }

    public void Dispose()
    {
      if (_peer.IsSet)
        _peer.DisconnectNow(0);

      _host?.Flush();
      _host?.Dispose();

      Library.Deinitialize();
    }
  }
}