using NetStack.Serialization;
using Ragon.Common;

namespace Ragon.Client
{
  public interface IRagonEntityManager
  {
    void OnJoined();
    void OnOwnerShipChanged(RagonPlayer player);
    void OnEntityCreated(int entityId, ushort entityType, RagonAuthority state, RagonAuthority evnt, RagonPlayer creator, byte[] payload);
    void OnEntityStaticCreated(int entityId, ushort staticId, ushort entityType, RagonAuthority state, RagonAuthority evnt, RagonPlayer creator, byte[] payload);
    void OnEntityDestroyed(int entityId);
    void OnEntityEvent(RagonPlayer player, int entityId, ushort evntCode, BitBuffer payload);
    void OnEntityState(int entityId, BitBuffer payload);
  }
}