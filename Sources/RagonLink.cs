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

using System.Collections.Generic;
using System.Reflection;
using Ragon.Protocol;
using UnityEngine;

namespace Ragon.Client.Unity
{
  public class RagonLink : MonoBehaviour
  {
    public RagonEntity Entity { get; private set; }

    public ushort StaticID => staticId;
    public ushort Type => type;

    [SerializeField] private RagonDiscovery discovery = RagonDiscovery.RootObject;
    [SerializeField] private RagonAuthority authority = RagonAuthority.All;
    [SerializeField] private bool autoDestroy = false;
    [SerializeField, ReadOnly] private ushort staticId;
    [SerializeField, ReadOnly] private ushort entityId;
    [SerializeField, ReadOnly] private ushort type;
    [SerializeField, ReadOnly] private bool hasAuthority;
    [SerializeField, ReadOnly] private bool attached;
    [SerializeField, ReadOnly] private RagonBehaviour[] _behaviours;

    private RagonProperty[] _properties;

    public void SetStatic(ushort sceneId)
    {
      staticId = sceneId;
    }

    public void SetType(ushort entityType)
    {
      type = entityType;
    }

    public RagonProperty[] Discovery()
    {
      switch (discovery)
      {
        case RagonDiscovery.RootObject:
          _behaviours = GetComponents<RagonBehaviour>();
          break;
        case RagonDiscovery.RootObjectWithNested:
          _behaviours = GetComponentsInChildren<RagonBehaviour>();
          break;
      }
      
      var propertyList = new List<RagonProperty>();
      foreach (var behaviour in _behaviours)
      {
        if (behaviour.OnDiscovery(propertyList))
          continue;

        var fieldFlags = (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var fieldInfos = behaviour.GetType().GetFields(fieldFlags);
        var baseProperty = typeof(RagonProperty);

        foreach (var field in fieldInfos)
        {
          if (baseProperty.IsAssignableFrom(field.FieldType))
          {
            var property = (RagonProperty)field.GetValue(behaviour);
            property.SetName(field.Name);
            propertyList.Add(property);
          }
        }
      }

      _properties = propertyList.ToArray();
      return _properties;
    }

    public void OnAttached(RagonEntity entity)
    {
      Entity = entity;

      entityId = entity.Id;
      type = entity.Type;
      hasAuthority = entity.HasAuthority;
      authority = entity.Authority;
      attached = true;

      foreach (var behaviour in _behaviours)
        behaviour.Attach(entity);
    }

    public void OnDetached()
    {
      attached = false;
      
      foreach (var behaviour in _behaviours)
        behaviour.Detach();

      if (autoDestroy)
        DestroyImmediate(gameObject);
    }

    public void OnOwnershipChanged(RagonPlayer prevOwner, RagonPlayer nextOwner)
    {
      hasAuthority = Entity.HasAuthority;
      
      foreach (var behaviour in _behaviours)
        behaviour.OnOwnershipChanged(nextOwner);
    }

    private void FixedUpdate()
    {
      if (!attached) return;
      if (hasAuthority)
      {
        foreach (var behaviour in _behaviours)
          behaviour.OnFixedUpdateEntity();

        return;
      }

      foreach (var behaviour in _behaviours)
        behaviour.OnFixedUpdateProxy();
    }

    private void LateUpdate()
    {
      if (!attached) return;
      if (hasAuthority)
      {
        foreach (var behaviour in _behaviours)
          behaviour.OnLateUpdateEntity();

        return;
      }

      foreach (var behaviour in _behaviours)
        behaviour.OnLateUpdateProxy();
    }

    private void Update()
    {
      if (!attached) return;
      if (hasAuthority)
      {
        foreach (var behaviour in _behaviours)
          behaviour.OnUpdateEntity();
      }
      else
      {
        foreach (var behaviour in _behaviours)
          behaviour.OnUpdateProxy();
      }
    }
  }
}