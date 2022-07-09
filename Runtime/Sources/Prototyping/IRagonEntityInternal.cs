using NetStack.Serialization;

namespace Ragon.Client.Prototyping
{
  public interface IRagonEntityInternal
  {
    public bool AutoReplication { get; }
    public void Create();
    public void Attach(int entityType, RagonPlayer player, int entityId, BitBuffer payload);
    public void Detach(BitBuffer payload);
    public void ProcessEvent(RagonPlayer player, ushort eventCode, BitBuffer buffer);
    public void ProcessState(BitBuffer buffer);
    public void ChangeOwner(RagonPlayer newOwner);
    public void ReplicateState();
  }
}