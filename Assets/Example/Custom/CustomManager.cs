using System;
using System.Text;
using NetStack.Serialization;
using Ragon.Client;
using Unity.Collections;
using UnityEngine;

namespace Example.Game
{
  public class ExampleHandler : MonoBehaviour, IRagonHandler
  {
    private int lastEntityId = -1;

    public void Start()
    {
      RagonNetwork.SetManager(this);
      RagonNetwork.ConnectToServer("127.0.0.1", 5000);
    }

    private void Update()
    {
      if (Input.GetKeyDown(KeyCode.Alpha1))
      {
        RagonNetwork.Room.CreateEntity(0, new SpawnPlayerPacket());
      }

      if (Input.GetKeyDown(KeyCode.Alpha2))
      {
        RagonNetwork.Room.DestroyEntity(lastEntityId, new DestroyPlayerPacket());
      }

      if (Input.GetKeyDown(KeyCode.Alpha3))
      {
        RagonNetwork.Room.SendEvent(500);
      }

      if (Input.GetKeyDown(KeyCode.Alpha4))
      {
        RagonNetwork.Room.SendEntityEvent(500, lastEntityId);
      }

      if (Input.GetKeyDown(KeyCode.Alpha5))
      {
        RagonNetwork.Room.SendEvent(123, new TestEvent()
        {
          TestData = "Allooo"
        });
      }
    }

    public void OnConnected()
    {
      Debug.Log("Connected");

      var apiKey = Encoding.UTF8.GetBytes("123");
      RagonNetwork.AuthorizeWithData(apiKey);
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
      Debug.LogError("Joined to room with id " + RagonNetwork.Room.Id);
    }

    public void OnEntityCreated(int entityId, ushort entityType, ushort ownerId, BitBuffer payload)
    {
      Debug.Log($"Entity created with id {entityId} and type {entityType}");

      lastEntityId = entityId;
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

    public void OnEntityEvent(int entityId, ushort evntCode, BitBuffer payload)
    {
      Debug.Log($"Entity event {entityId} {evntCode}");
    }

    public void OnEvent(ushort evntCode, BitBuffer payload)
    {
      Debug.Log($"Event: {evntCode} with payload {payload.Length}");
    }

    public void OnLevel(string sceneName)
    {
      Debug.Log($"Scene: {sceneName}");
    }
  }
}