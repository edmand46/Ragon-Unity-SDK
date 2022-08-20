using Ragon.Common;

namespace Ragon.Client.Prototyping
{
  public enum RagonReplicationMode: byte
  {
    LOCAL_ONLY,
    SERVER_ONLY,
    LOCAL_AND_SERVER,
  }
  
  public interface IRagonEvent: IRagonSerializable
  {
    
  }
}