using System;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  [Serializable]
  public class RagonFloat : RagonProperty
  {
    public float Value
    {
      get => _value;
      set
      {
        _value = value;
        Changed();
      }
    }

    [SerializeField] private float _value;

    public RagonFloat(
      float initialValue,
      bool invokeLocal = true,
      int priority = 0
    ) : base(priority, invokeLocal)
    {
      _value = initialValue;
      SetFixedSize(4);
    }

    public override void Serialize(RagonSerializer serializer)
    {
      serializer.WriteFloat(_value);
    }

    public override void Deserialize(RagonSerializer serializer)
    {
      _value = serializer.ReadFloat();

      if (!Entity.IsMine)
        OnChanged?.Invoke();
    }
  }
}