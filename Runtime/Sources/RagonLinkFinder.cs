using System.Collections.Generic;

namespace Ragon.Client.Unity
{
  public class RagonLinkFinder
  {
    private Dictionary<RagonEntity, RagonLink> _links = new Dictionary<RagonEntity, RagonLink>();

    internal bool Find(RagonEntity entity, out RagonLink link)
    {
      return _links.TryGetValue(entity, out link);
    }
    
    internal void Track(RagonEntity entity, RagonLink link)
    {
      _links.Add(entity, link);
    }
    
    internal void Untrack(RagonEntity entity)
    {
      _links.Remove(entity);
    }
  }
}