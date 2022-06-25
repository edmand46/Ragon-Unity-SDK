using System;
using System.Collections.Generic;
using Example.Game;
using NetStack.Serialization;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Integration
{
  public class RagonEntity<TState>: RagonEntityExtended<TState, EmptyPayload, EmptyPayload> where TState: IRagonSerializable, new()
  {
  }
}