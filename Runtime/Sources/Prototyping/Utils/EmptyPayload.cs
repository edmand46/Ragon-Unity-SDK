using NetStack.Serialization;
namespace Ragon.Client.Prototyping
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