using System;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  [Serializable]
  public class RagonInt : RagonProperty
  {
    public int Value
    {
      get => _value;
      set
      {
        _value = value;
        Changed();
      }
    }

    [SerializeField] private int _value;

    public RagonInt(
      int initialValue,
      bool invokeLocal = true,
      int priority = 0
    ) : base(priority, invokeLocal)
    {
      _value = initialValue;
      SetFixedSize(4);
    }

    public override void Serialize(RagonSerializer serializer)
    {
      serializer.WriteInt(_value);
    }

    public override void Deserialize(RagonSerializer serializer)
    {
      _value = serializer.ReadInt();

      if (!Entity.IsMine)
        OnChanged?.Invoke();
    }
  }
}