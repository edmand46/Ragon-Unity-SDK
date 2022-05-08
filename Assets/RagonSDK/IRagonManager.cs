using NetStack.Serialization;

namespace Ragon.Client
{
  public interface IRagonHandler
  {
    void OnAuthorized(BitBuffer payload);
    void OnReady();
    void OnEntityCreated(int entityId, ushort entityType, ushort ownerId, BitBuffer payload);
    void OnEntityDestroyed(int entityId, BitBuffer payload);
    void OnEntityState(int entityId, BitBuffer payload);
    void OnEntityProperty(int entityId, int property, BitBuffer payload);
    void OnEntityEvent(int entityId, ushort evntCode, BitBuffer payload);
    void OnEvent(ushort evntCode, BitBuffer payload);
    void OnLevel(string sceneName);
    void OnConnected();
    void OnDisconnected();
  }
}