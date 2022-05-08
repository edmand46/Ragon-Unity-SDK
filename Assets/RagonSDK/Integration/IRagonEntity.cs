namespace Ragon.Client.Integration
{
  public interface IRagonEntity
  {
    public void Attach(int entityType, uint ownerId, int entityId);
    public void Detach();
  }
}