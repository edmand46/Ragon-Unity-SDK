using System;
using Ragon.Common;

namespace Ragon.Client.Prototyping
{
  public class RagonProperty
  {
    public Action OnChanged;
    public bool IsAttached => _attached;
    public int Id => _id;
    public int Size => _size;

    private RagonObject _entity;
    private bool _attached;
    private int _id;
    private int _size;

    public RagonProperty(int size)
    {
      _size = size;
    }
    
    public void Changed()
    {
      if (!_attached) return;
      _entity.TrackChangedProperty(this);
    }

    public void Attach(RagonObject entity, int propertyId)
    {
      _attached = true;
      _entity = entity;
      _id = propertyId;
      Changed();
    }

    public virtual void Serialize(RagonSerializer serializer)
    {
      
    }
    
    public virtual void Deserialize(RagonSerializer serializer)
    {
      
    }
  }
}