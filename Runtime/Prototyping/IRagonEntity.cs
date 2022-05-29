using NetStack.Serialization;

namespace Ragon.Client.Integration
{
  public interface IRagonEntity
  {
    public void Attach(int entityType, RagonPlayer player, int entityId, BitBuffer payload);
    public void ChangeOwner(RagonPlayer newOwner);
    public void Detach(BitBuffer payload);
  }
}