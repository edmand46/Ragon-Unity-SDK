using Ragon.Common;

namespace Ragon.Client.Prototyping
{
  public class RagonEntity<TState>: RagonEntityExtended<TState, EmptyPayload, EmptyPayload> where TState: IRagonSerializable, new()
  {
  }
}