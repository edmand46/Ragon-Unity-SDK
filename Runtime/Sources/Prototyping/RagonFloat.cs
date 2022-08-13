using System;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Prototyping
{
  [Serializable]
  public class RagonFloat: RagonProperty
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
    public RagonFloat(float initialValue) : base(4)
    {
      _value = initialValue;
    }
    
    public override void Serialize(RagonSerializer serializer)
    {
      serializer.WriteFloat(_value);
    }

    public override void Deserialize(RagonSerializer serializer)
    {
      if (!IsAttached) return;
      
      _value = serializer.ReadFloat();
      OnChanged?.Invoke();
    }
  }
}