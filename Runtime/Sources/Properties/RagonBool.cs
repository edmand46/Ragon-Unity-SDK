using System;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  [Serializable]
  public class RagonBool : RagonProperty
  {
    public bool Value
    {
      get => _value;
      set
      {
        _value = value;
        Changed();
      }
    }

    [SerializeField] private bool _value;

    public RagonBool(
      bool initialValue,
      bool invokeLocal = true,
      int priority = 0
    ) : base(priority, invokeLocal)
    {
      _value = initialValue;
      SetFixedSize(1);
    }

    public override void Serialize(RagonSerializer serializer)
    {
      serializer.WriteBool(_value);
    }

    public override void Deserialize(RagonSerializer serializer)
    {
      _value = serializer.ReadBool();

      if (!Entity.IsMine)
        OnChanged?.Invoke();
    }
  }
}