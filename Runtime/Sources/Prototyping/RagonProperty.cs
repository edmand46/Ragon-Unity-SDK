using System;
using Ragon.Common;

namespace Ragon.Client.Prototyping
{
  public class RagonProperty
  {
    public Action OnChanged;
    public bool IsDirty => _dirty;
    public int Id => _id;
    public int Size => _size;

    private RagonEntity _entity;
    private bool _dirty;
    private int _id;
    private int _size;

    public RagonProperty(int size)
    {
      _size = size;
    }

    public void MarkAsChanged()
    {
      _dirty = true;
      
      if (_entity)
        _entity.TrackChangedProperty(this);
    }

    public void Clear()
    {
      _dirty = false;
    }

    public void Attach(RagonEntity obj, int propertyId)
    {
      _entity = obj;
      _id = propertyId;
      
      MarkAsChanged();
    }

    public virtual void Serialize(RagonSerializer serializer)
    {
    }

    public virtual void Deserialize(RagonSerializer serializer)
    {
    }
  }
}