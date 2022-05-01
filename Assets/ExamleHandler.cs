using System;
using NetStack.Serialization;
using Ragon.Client;
using UnityEngine;

namespace Example.Game
{
  public class ExampleHandler : MonoBehaviour, IRagonHandler
  {
    public void Start()
    {
      RagonNetwork.SetHandler(this);
      RagonNetwork.ConnectToServer("'127.0.0.1", 5000);
    }

    public void OnConnected()
    {
      Debug.Log("Connected");
      
      RagonNetwork.AuthorizeWithData(Array.Empty<byte>());
    }

    public void OnDisconnected()
    {
      Debug.Log("Disconnected");
    }
    
    public void OnAuthorized(BitBuffer payload)
    {
      RagonNetwork.FindRoomAndJoin("Example Map", 1, 2);
      
      Debug.Log("Authorized");
    }

    public void OnReady()
    {
      Debug.Log("Joined to room with id " + RagonNetwork.Room.Id);
      
      RagonNetwork.Room.
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
  }
}