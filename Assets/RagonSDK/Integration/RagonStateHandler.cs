using NetStack.Serialization;

namespace Ragon.Client.Integration
{
  public interface IRagonStateHandler
  {
    public void ProcessState(BitBuffer buffer);
  }
}