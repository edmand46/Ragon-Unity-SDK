/*
 * Copyright 2023 Eduard Kargin <kargin.eduard@gmail.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using Ragon.Protocol;
using UnityEngine;

namespace Ragon.Client.Unity
{
  public class RagonBehaviour : MonoBehaviour
  {
    private delegate void OnEventDelegate(RagonPlayer player, RagonBuffer serializer);

    public RagonEntity Entity => _entity;
    public RagonPlayer Owner => _entity.Owner;
    public bool HasAuthority => _entity.HasAuthority;

    private RagonEntity _entity;
    private Dictionary<int, OnEventDelegate> _events = new Dictionary<int, OnEventDelegate>();
    private Dictionary<int, Action<RagonPlayer, IRagonEvent>> _localEvents = new Dictionary<int, Action<RagonPlayer, IRagonEvent>>();
      
    internal void Attach(RagonEntity entity)
    {
      _entity = entity;
      
      OnAttachedEntity();
    }

    internal void Detach()
    {
      OnDetachedEntity();
    }

    public virtual bool OnDiscovery(List<RagonProperty> properties)
    {
      return false;
    } 

    public virtual void OnAttachedEntity()
    {
    }

    public virtual void OnDetachedEntity()
    {
    }

    public virtual void OnUpdateEntity()
    {
    }

    public virtual void OnUpdateProxy()
    {
    }
    
    public virtual void OnLateUpdateEntity()
    {
    }
    
    public virtual void OnLateUpdateProxy()
    {
    }

    public virtual void OnFixedUpdateEntity()
    {
    }
    
    public virtual void OnFixedUpdateProxy()
    {
    }

    public virtual void OnOwnershipChanged(RagonPlayer player)
    {
      
    }
  }
}