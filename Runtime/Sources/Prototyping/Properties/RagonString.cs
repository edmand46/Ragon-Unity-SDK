using System;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Prototyping
{
  [Serializable]
  public class RagonString: RagonProperty
  {
    public string Value
    {
      get => _value; 
      set
      {
        _value = value;
        MarkAsChanged();
      }
    }
    
    [SerializeField] private string _value;
    
    public RagonString(string initialValue, int lenght = 256) : base(lenght)
    {
      _value = initialValue;
    }
    
    public override void Serialize(RagonSerializer serializer)
    {
      serializer.WriteString(_value, (ushort) Size);
    }

    public override void Deserialize(RagonSerializer serializer)
    {
      _value = serializer.ReadString((ushort) Size);
      OnChanged?.Invoke();
    }
  }
}