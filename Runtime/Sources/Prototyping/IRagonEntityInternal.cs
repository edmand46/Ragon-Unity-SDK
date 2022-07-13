using NetStack.Serialization;

namespace Ragon.Client.Prototyping
{
  public interface IRagonEntityInternal
  {
    public bool IsAttached { get; }
    public bool AutoReplication { get; }
    public void Attach(int entityType, RagonPlayer player, int entityId, byte[] payload);
    public void Detach();
    public void ProcessEvent(RagonPlayer player, ushort eventCode, BitBuffer buffer);
    public void ProcessState(BitBuffer buffer);
    public void ChangeOwner(RagonPlayer newOwner);
    public void ReplicateState();
  }
}