using System;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  [Serializable]
  public enum RagonAxis
  {
    XYZ,
    XY,
    YZ,
    XZ,
    X,
    Y,
    Z
  }

  [Serializable]
  public class RagonVector3 : RagonProperty
  {
    [SerializeField] private Vector3 _value;

    public Vector3 Value
    {
      get => _value;
      set
      {
        _value = value;
        Changed();
      }
    }

    private RagonAxis _axis;

    public RagonVector3(
      Vector3 initialValue,
      RagonAxis axis = RagonAxis.XYZ,
      bool invokeLocal = true,
      int priority = 0
    ) : base(priority, invokeLocal)
    {
      _value = initialValue;
      _axis = axis;

      switch (_axis)
      {
        case RagonAxis.XYZ:
          SetFixedSize(12);
          break;
        case RagonAxis.XY:
        case RagonAxis.XZ:
        case RagonAxis.YZ:
          SetFixedSize(8);
          break;
        case RagonAxis.X:
        case RagonAxis.Y:
        case RagonAxis.Z:
          SetFixedSize(4);
          break;
      }
    }

    public override void Serialize(RagonSerializer serializer)
    {
      switch (_axis)
      {
        case RagonAxis.XYZ:
          serializer.WriteFloat(_value.x);
          serializer.WriteFloat(_value.y);
          serializer.WriteFloat(_value.z);
          break;
        case RagonAxis.XY:
          serializer.WriteFloat(_value.x);
          serializer.WriteFloat(_value.y);
          break;
        case RagonAxis.XZ:
          serializer.WriteFloat(_value.x);
          serializer.WriteFloat(_value.z);
          break;
        case RagonAxis.YZ:
          serializer.WriteFloat(_value.y);
          serializer.WriteFloat(_value.z);
          break;
        case RagonAxis.X:
          serializer.WriteFloat(_value.x);
          break;
        case RagonAxis.Y:
          serializer.WriteFloat(_value.y);
          break;
        case RagonAxis.Z:
          serializer.WriteFloat(_value.z);
          break;
      }
    }

    public override void Deserialize(RagonSerializer serializer)
    {
      switch (_axis)
      {
        case RagonAxis.XYZ:
          _value.x = serializer.ReadFloat();
          _value.y = serializer.ReadFloat();
          _value.z = serializer.ReadFloat();
          break;
        case RagonAxis.XY:
          _value.x = serializer.ReadFloat();
          _value.y = serializer.ReadFloat();
          break;
        case RagonAxis.XZ:
          _value.x = serializer.ReadFloat();
          _value.z = serializer.ReadFloat();
          break;
        case RagonAxis.YZ:
          _value.y = serializer.ReadFloat();
          _value.z = serializer.ReadFloat();
          break;
        case RagonAxis.X:
          _value.x = serializer.ReadFloat();
          break;
        case RagonAxis.Y:
          _value.y = serializer.ReadFloat();
          break;
        case RagonAxis.Z:
          _value.z = serializer.ReadFloat();
          break;
      }

      if (!Entity.IsMine)
        OnChanged?.Invoke();
    }
  }
}