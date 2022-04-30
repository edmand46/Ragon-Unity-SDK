using NetStack.Serialization;
using Ragon.Common.Protocol;
using RagonSDK;
using UnityEngine;

namespace YohohoArena.Game
{
  public class ExampleHandler: MonoBehaviour, IRagonHandler
  {
    public  void Start()
    {
      RagonManager.Instance.SetHandler(this);
    }

    public void OnAuthorized(BitBuffer payload)
    {
      Debug.Log("Authorized");
      
      RagonManager.Instance.FindOrJoin();
    }

    public void OnJoined(BitBuffer payload)
    {
      Debug.Log("Joined");
      
      RagonManager.Instance.Send(RagonOperation.SCENE_IS_LOADED);
    }

    public void OnReady()
    {
      // RagonManager.Instance.CreateEntity();
    }
    
    public void OnEntityCreated(int entityId, int ownerId, BitBuffer payload)
    {
      Debug.Log("Entity created with id " + entityId);
    }

    public void OnEntityDestroyed(int entityId, BitBuffer payload)
    {
      Debug.Log("Entity destroyed with id " + entityId);
    }

    public void OnEntityState(int entityId, BitBuffer payload)
    {
      Debug.Log("Entity updated with id " + entityId);
    }

    public void OnEntityProperty(int entityId, int property, BitBuffer payload)
    {
      Debug.Log("Entity property updated");
    }

    public void OnEntityEvent(int entityId, int evntCode, BitBuffer payload)
    {
      Debug.Log($"Entity event {entityId} {evntCode}");
    }
    
    public void OnEvent(uint evntCode, BitBuffer payload)
    { 
      Debug.Log($"Event: {evntCode}");
    }

    public void OnLevel(string sceneName)
    {
      Debug.Log($"Scene: {sceneName}");
    }

    public void OnConnected()
    {
      Debug.Log("Connected");
    }

    public void OnDisconnected()
    {
      Debug.Log("Disconnected");
    }
  }
}