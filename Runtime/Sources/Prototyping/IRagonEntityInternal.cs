using Ragon.Common;

namespace Ragon.Client.Prototyping
{
  public interface IRagonEntityInternal
  {
    public bool IsAttached { get; }
    public bool AutoReplication { get; }
    public void Attach(int entityType, RagonPlayer player, int entityId, byte[] payload);
    public void Detach(byte[] payload);
    public void ProcessEvent(RagonPlayer player, ushort eventCode, RagonSerializer serializer);
    public void ProcessState(RagonSerializer serializer);
    public void ChangeOwner(RagonPlayer newOwner);
    public void ReplicateState();
  }
}