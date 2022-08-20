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
        OnChanged?.Invoke();
        MarkAsChanged();
      }
    }
    
    [SerializeField] private string _value;
    
    public RagonString(string initialValue)
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
      OnChanged?.Invoke();
    }
  }
}