using NetStack.Serialization;
using Ragon.Client.Integration;
using Ragon.Common;

namespace Example.Game
{
  public class EmptyPayload: IRagonPayload
  {
    public void Serialize(BitBuffer buffer)
    {
      
    }

    public void Deserialize(BitBuffer buffer)
    {
     
    }
  }
}