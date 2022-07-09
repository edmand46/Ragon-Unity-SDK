using NetStack.Serialization;
using Ragon.Common;

namespace Ragon.Client
{
  public interface IRagonNetworkListener
  {
    void OnAuthorized(string playerId, string playerName);
    void OnJoined();
    void OnFailed();
    void OnLeaved();
    void OnConnected();
    void OnDisconnected();
    
    void OnPlayerJoined(RagonPlayer player);
    void OnPlayerLeft(RagonPlayer player);
    void OnEvent(RagonPlayer player, ushort evntCode, BitBuffer payload);
    void OnOwnerShipChanged(RagonPlayer player);
    void OnLevel(string sceneName);
  }
}