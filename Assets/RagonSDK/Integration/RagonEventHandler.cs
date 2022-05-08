using NetStack.Serialization;

namespace Ragon.Client.Integration
{
  public interface IRagonEventHandler
  {
    public void ProcessEvent(ushort eventCode, BitBuffer buffer);
  }
}