
using Ragon.Common;

namespace Ragon.Client
{
  public interface IRagonEntityManager
  {
    void CollectSceneData();
    void Cleanup();
    void OnOwnerShipChanged(RagonPlayer player);
    void OnEntityCreated(int entityId, ushort entityType, RagonAuthority state, RagonAuthority evnt, RagonPlayer creator, byte[] payload);
    void OnEntityStaticCreated(int entityId, ushort staticId, ushort entityType, RagonAuthority state, RagonAuthority evnt, RagonPlayer creator, byte[] payload);
    void OnEntityDestroyed(int entityId, byte[] payload);
    void OnEntityEvent(RagonPlayer player, int entityId, ushort evntCode, RagonSerializer payload);
    void OnEntityState(int entityId, RagonSerializer payload);
  }
}