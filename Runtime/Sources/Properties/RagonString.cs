using System;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  [Serializable]
  public class RagonString : RagonProperty
  {
    public string Value
    {
      get => _value;
      set
      {
        _value = value;
        Changed();
      }
    }

    [SerializeField] private string _value;

    public RagonString(
      string initialValue,
      bool invokeLocal = true,
      int priority = 0
    ) : base(priority, invokeLocal)
    {
      _value = initialValue;
    }

    public override void Serialize(RagonSerializer serializer)
    {
      serializer.WriteString(_value);
    }

    public override void Deserialize(RagonSerializer serializer)
    {
      _value = serializer.ReadString();

      if (!Entity.IsMine)
        OnChanged?.Invoke();
    }
  }
}