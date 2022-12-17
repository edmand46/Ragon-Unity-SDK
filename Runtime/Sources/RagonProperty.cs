using System;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  public class RagonProperty
  {
    public Action OnChanged;
    public RagonEntity Entity => _entity;
    public bool IsDirty => _dirty && _ticks >= _priority;
    public bool IsFixed => _fixed;
    public int Id => _id;
    public int Size => _size;

    private bool _fixed;
    private RagonEntity _entity;
    private bool _dirty;
    private int _id;
    private int _size;
    private int _ticks;
    private int _priority;

    public RagonProperty(int priority)
    {
      _size = 0;
      _fixed = false;
    }

    public void SetFixedSize(int size)
    {
      _size = size;
      _fixed = true;
    }

    public void MarkAsChanged()
    {
      if (_dirty) return;

      _dirty = true;

      if (_entity)
        _entity.TrackChangedProperty(this);
    }

    public void AddTick()
    {
      _ticks++;
    }
    
    public void Attach(RagonEntity obj, int propertyId)
    {
      _entity = obj;
      _id = propertyId;

      MarkAsChanged();
    }

    public void Write(RagonSerializer serializer)
    {
      if (_fixed)
      {
        Serialize(serializer);
        return;
      }

      var sizeOffset = serializer.Lenght;
      serializer.AddOffset(2); // ushort
      var propOffset = serializer.Lenght;

      Serialize(serializer);

      var propSize = (ushort) (serializer.Lenght - propOffset);
      serializer.WriteUShort(propSize, sizeOffset);

      _dirty = false;
      _ticks = 0;
    }

    public virtual void Serialize(RagonSerializer serializer)
    {
    }

    public virtual void Deserialize(RagonSerializer serializer)
    {
    }
  }
}