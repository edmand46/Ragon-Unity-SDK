using Ragon.Common;

namespace Ragon.Client
{
  public interface IRagonConnection
  {
    public uint Ping { get; }
    public double UpstreamBandwidth { get; }
    public double DownstreamBandwidth { get; }

    public void Send(RagonSerializer serializer, DeliveryType deliveryType = DeliveryType.Unreliable);
    public void Send(byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable);
  }
}