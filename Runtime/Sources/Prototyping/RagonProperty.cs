using System;
using Ragon.Common;

namespace Ragon.Client.Prototyping
{
  public class RagonFloat
  {
    public Action OnChanged;
    public bool IsDirty => _dirty;
    
    public float Value
    {
      get => _value; 
      set
      {
        _value = value;
        _dirty = true;
      }
    }

    private float _value;
    private bool _dirty;

    public RagonFloat(float initialValue)
    {
      _value = initialValue;
      _dirty = true;
    }
    
    public void Serialize(RagonSerializer serializer)
    {
      serializer.WriteFloat(_value);
      _dirty = false;
    }

    public void Deserialize(RagonSerializer serializer)
    {
      _value = serializer.ReadFloat();
      _dirty = false;
      
      OnChanged?.Invoke();
    }
  }
}