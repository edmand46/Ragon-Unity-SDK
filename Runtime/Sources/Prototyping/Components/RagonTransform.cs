using System;
using UnityEngine;

namespace Ragon.Client.Prototyping.Components
{
  public class RagonTransform: RagonBehaviour
  {
    [SerializeField] private RagonVector3 _position = new(Vector3.zero);
    
    private void Update()
    {
      if (Entity.IsMine)
        _position.Value = transform.position;
      else
        transform.position = Vector3.Lerp(transform.position, _position.Value, Time.deltaTime * 10);
    }
  }
}