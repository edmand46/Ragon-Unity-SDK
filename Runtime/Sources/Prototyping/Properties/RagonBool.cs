using System;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Prototyping
{
  [Serializable]
  public class RagonBool: RagonProperty
  {
    public bool Value
    {
      get => _value; 
      set
      {
        _value = value;
        MarkAsChanged();
      }
    }
    
    [SerializeField] private bool _value;
    
    public RagonBool(bool initialValue) : base(1)
    {
      _value = initialValue;
    }
    
    public override void Serialize(RagonSerializer serializer)
    {
      serializer.WriteBool(_value);
    }

    public override void Deserialize(RagonSerializer serializer)
    {
      _value = serializer.ReadBool();
      OnChanged?.Invoke();
    }
  }
}