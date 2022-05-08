using System;
using Ragon.Client;
using Ragon.Client.Integration;
using UnityEngine;

namespace Example.Game
{
  public class CharacterEntity : RagonEntity<CharacterState>
  {
    public override void OnSpawn()
    {
    }

    public override void OnDespawn()
    {
    }

    public override void OnStateUpdated(CharacterState prev, CharacterState current)
    {
    }

    private void Update()
    {
      if (IsOwner)
      {
        if (Input.GetKey(KeyCode.A))
          transform.position = new Vector3(transform.position.x - 1f * Time.deltaTime, transform.position.y, transform.position.z);
        
        if (Input.GetKey(KeyCode.D))
          transform.position = new Vector3(transform.position.x + 1f * Time.deltaTime, transform.position.y, transform.position.z) ; 
        
        State.Position = transform.position;
        
        RagonNetwork.Room.SendEntityState(EntityId, State);
      }
      else
      {
        transform.position = State.Position;
      }
    }
  }
}