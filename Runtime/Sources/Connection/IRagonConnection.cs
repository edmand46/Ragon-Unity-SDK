using System;
using Ragon.Common;

namespace Ragon.Client
{
  public interface IRagonConnection: IDisposable
  {
    public RagonConnectionStatus Status { get; }
 
    public void Send(byte[] rawData, DeliveryType deliveryType = DeliveryType.Unreliable);
    public void Connect(string address, ushort port, uint protocol);
    public void Disconnect();
    public void Update();
  }
}