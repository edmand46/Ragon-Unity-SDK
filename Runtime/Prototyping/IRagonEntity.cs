namespace Ragon.Client.Integration
{
  public interface IRagonEntity
  {
    public void Attach(int entityType, RagonPlayer player, int entityId);
    public void Detach();
  }
}