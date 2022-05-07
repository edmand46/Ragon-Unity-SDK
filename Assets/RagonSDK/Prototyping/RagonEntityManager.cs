using System;
using NetStack.Serialization;
using UnityEngine;

namespace Ragon.Client
{
  public class RagonEntityManager : MonoBehaviour, IRagonHandler
  {
    public static RagonEntityManager Instance { get; private set; }
    
    private void Awake()
    {
      Instance = this;
    }

    public void OnAuthorized(BitBuffer payload)
    {
      
    }

    public void OnReady()
    {
      
    }

    public void OnEntityCreated(int entityId, int ownerId, BitBuffer payload)
    {
     
    }

    public void OnEntityDestroyed(int entityId, BitBuffer payload)
    {
      ;
    }

    public void OnEntityState(int entityId, BitBuffer payload)
    {
      
    }

    public void OnEntityProperty(int entityId, int property, BitBuffer payload)
    {
      
    }

    public void OnEntityEvent(int entityId, int evntCode, BitBuffer payload)
    {
      
    }

    public void OnEvent(uint evntCode, BitBuffer payload)
    {
      
    }

    public void OnLevel(string sceneName)
    {
      
    }

    public void OnConnected()
    {
      
    }

    public void OnDisconnected()
    {
      
    }
  }
}