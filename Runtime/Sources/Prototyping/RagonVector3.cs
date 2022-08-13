using System;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Prototyping
{
  [Serializable]
  public class RagonVector3: RagonProperty
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
    
    public RagonVector3(Vector3 initialValue) : base(12)
    {
      _value = initialValue;
    }
    
    public override void Serialize(RagonSerializer serializer)
    {
      serializer.WriteFloat(_value.x);
      serializer.WriteFloat(_value.y);
      serializer.WriteFloat(_value.z);
    }

    public override void Deserialize(RagonSerializer serializer)
    {
      if (!IsAttached) return;
      
      _value.x = serializer.ReadFloat();
      _value.y = serializer.ReadFloat();
      _value.z = serializer.ReadFloat();
      
      OnChanged?.Invoke();
    }
  }
}