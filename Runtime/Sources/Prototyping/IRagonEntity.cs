using Ragon.Common;

namespace Ragon.Client.Prototyping
{
  public interface IRagonEntity
  {
    public int Id { get; }
    public bool IsMine { get; }
    public bool IsAttached { get; }
    public RagonPlayer Owner { get; }
    public void ReplicateEvent<TEvent>(TEvent evnt, RagonTarget target, RagonReplicationMode replicationMode) where TEvent : IRagonEvent, new();
  }
}