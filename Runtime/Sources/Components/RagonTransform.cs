using UnityEngine;

namespace Ragon.Client
{
  [RequireComponent(typeof(RagonEntity))]
  public class RagonTransform : RagonBehaviour
  {
    [SerializeField] private Transform target;

    private readonly RagonVector3 _rotation = new(Vector3.zero);
    private readonly RagonVector3 _position = new(Vector3.zero);

    public override void OnEntityTick()
    {
      _rotation.Value = target.rotation.eulerAngles;
      _position.Value = target.position;
    }

    public override void OnProxyTick()
    {
      target.position = Vector3.Lerp(target.position, _position.Value, Time.deltaTime * 10);
      target.rotation = Quaternion.Lerp(target.rotation, Quaternion.Euler(_rotation.Value), Time.deltaTime * 15);
    }
  }
}