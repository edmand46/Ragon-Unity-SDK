using NetStack.Serialization;
using Ragon.Common;

namespace Ragon.Client
{
  public interface IRagonEntityManager
  {
    void OnEntityCreated(int entityId, ushort entityType, RagonAuthority state, RagonAuthority evnt, RagonPlayer creator, BitBuffer payload);
    void OnEntityDestroyed(int entityId, BitBuffer payload);
    void OnOwnerShipChanged(RagonPlayer player);
    void OnEntityState(int entityId, BitBuffer payload);
    void OnEntityEvent(int entityId, ushort evntCode, BitBuffer payload);
  }
}