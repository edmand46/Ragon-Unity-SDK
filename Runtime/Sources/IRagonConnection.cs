using System;
using Ragon.Common;

namespace Ragon.Client
{
  public interface IRagonConnection: IDisposable
  {
    public uint Ping { get; }
    public RagonConnectionState ConnectionState { get; }
    public double UpstreamBandwidth { get; }
    public double DownstreamBandwidth { get; }

    public void Send(RagonSerializer serializer, DeliveryType deliveryType = DeliveryType.Unreliable);
    public void Send(byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable);
    public void Connect(string address, ushort port, uint protocol);
    public void Disconnect();
    public void Update();
  }
}