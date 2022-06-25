using NetStack.Serialization;

namespace Ragon.Client.Integration
{
  public interface IRagonStateListener
  {
    public void ProcessState(BitBuffer buffer);
  }
}