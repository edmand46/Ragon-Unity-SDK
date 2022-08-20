using System;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Ragon.Client.Prototyping.Components
{
  public class RagonTransform : RagonBehaviour
  {
    [SerializeField] private Transform target;
    [SerializeField] private RagonVector3 position = new(Vector3.zero, RagonAxis.XZ);
    [SerializeField] private RagonVector3 rotation = new(Vector3.zero, RagonAxis.Y);
    
    private void Update()
    {
      if (IsMine)
      {
        position.Value = target.position;
        rotation.Value = target.rotation.eulerAngles;
      }
      else
      {
        target.position = Vector3.Lerp(target.position, position.Value, Time.deltaTime * 10);
        target.rotation = Quaternion.Euler(rotation.Value.x, rotation.Value.y, rotation.Value.z);
      }
    }
  }
}