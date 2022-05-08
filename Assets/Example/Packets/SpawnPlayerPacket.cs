using NetStack.Serialization;
using Ragon.Common;


namespace Example.Game
{
  public class SpawnPlayerPacket: IRagonSerializable
  {
    public string TestData;
    public void Serialize(BitBuffer buffer)
    {
      buffer.AddString(TestData);
    }
    public void Deserialize(BitBuffer buffer)
    {
      TestData = buffer.ReadString();
    }
  }
}