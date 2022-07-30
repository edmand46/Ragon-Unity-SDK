namespace Ragon.Client
{
  public interface IRoomInternal
  {
    public void AddPlayer(uint peerId, string playerId, string playerName);
    public void RemovePlayer(string id);
    public void OnOwnershipChanged(string id);
    public void Cleanup();
  }
}