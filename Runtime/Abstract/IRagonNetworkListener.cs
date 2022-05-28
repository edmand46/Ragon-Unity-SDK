using NetStack.Serialization;
using Ragon.Common;

namespace Ragon.Client
{
  public interface IRagonNetworkListener
  {
    void OnAuthorized(BitBuffer payload);
    void OnJoined();
    void OnFailed();
    void OnLeaved();
    void OnConnected();
    void OnDisconnected();
    
    void OnPlayerJoined(RagonPlayer player);
    void OnPlayerLeft(RagonPlayer player);
    void OnOwnerShipChanged(RagonPlayer player);
    void OnEntityCreated(int entityId, ushort entityType, RagonAuthority state, RagonAuthority evnt, RagonPlayer creator, BitBuffer payload);
    void OnEntityDestroyed(int entityId, BitBuffer payload);
    void OnEntityState(int entityId, BitBuffer payload);
    void OnEntityEvent(int entityId, ushort evntCode, BitBuffer payload);
    void OnEvent(ushort evntCode, BitBuffer payload);
    void OnLevel(string sceneName);
  }
}