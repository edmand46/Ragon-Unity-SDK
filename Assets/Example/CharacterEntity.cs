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
        var direction = Vector3.zero;
        if (Input.GetKey(KeyCode.A))
        {
          direction += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
          direction += Vector3.right;
        }

        State.Position = transform.position += direction * Time.deltaTime;

        RagonNetwork.Room.SendEntityState(EntityId, State);
      }
      else
      {
        transform.position = State.Position;
      }
    }
  }
}