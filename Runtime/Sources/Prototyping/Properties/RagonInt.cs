using System;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Prototyping
{ 
  [Serializable]
  public class RagonInt: RagonProperty
  {
    public int Value
    {
      get => _value; 
      set
      {
        _value = value;
        MarkAsChanged();
      }
    }
    
    [SerializeField] private int _value;
    
    public RagonInt(int initialValue) : base(4)
    {
      _value = initialValue;
    }
    
    public override void Serialize(RagonSerializer serializer)
    {
      serializer.WriteInt(_value);
    }

    public override void Deserialize(RagonSerializer serializer)
    {
      _value = serializer.ReadInt();
      OnChanged?.Invoke();
    }
  }
}