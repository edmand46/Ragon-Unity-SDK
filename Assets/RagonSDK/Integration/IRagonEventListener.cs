using NetStack.Serialization;

namespace Ragon.Client.Integration
{
  public interface IRagonEventListener
  {
    public void ProcessEvent(ushort eventCode, BitBuffer buffer);
  }
}