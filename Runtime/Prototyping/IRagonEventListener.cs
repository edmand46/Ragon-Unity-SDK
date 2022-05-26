using NetStack.Serialization;

namespace Ragon.Client.Integration
{
  public interface IRagonSceneEventListener
  {
    public void ProcessEvent(ushort eventCode, BitBuffer buffer);
  }
}