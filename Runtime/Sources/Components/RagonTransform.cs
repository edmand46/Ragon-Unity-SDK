using UnityEngine;

namespace Ragon.Client
{
  [RequireComponent(typeof(RagonEntity))]
  public class RagonTransform : RagonBehaviour
  {
    [SerializeField] private Transform target;

    private readonly RagonVector3 _rotation = new(Vector3.zero);
    private readonly RagonVector3 _position = new(Vector3.zero);

    public override void OnUpdateEntity()
    {
      if (!IsEqual(_rotation.Value, target.rotation.eulerAngles, 0.0001f))
        _rotation.Value = target.rotation.eulerAngles;

      if (!IsEqual(_position.Value, target.position, 0.0001f))
        _position.Value = target.position;
    }

    public override void OnUpdateProxy()
    {
      target.position = Vector3.Lerp(target.position, _position.Value, Time.deltaTime * 10);
      target.rotation = Quaternion.Lerp(target.rotation, Quaternion.Euler(_rotation.Value), Time.deltaTime * 15);
    }

    public bool IsEqual(Vector3 v1, Vector3 v2, float precision)
    {
      bool equal = true;

      if (Mathf.Abs(v1.x - v2.x) > precision) equal = false;
      if (Mathf.Abs(v1.y - v2.y) > precision) equal = false;
      if (Mathf.Abs(v1.z - v2.z) > precision) equal = false;

      return equal;
    }
  }
}