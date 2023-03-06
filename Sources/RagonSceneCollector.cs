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
using UnityEngine.SceneManagement;

namespace Ragon.Client.Unity
{
  public class RagonSceneCollector: IRagonSceneCollector
  {
    public RagonEntity[] Collect()
    {
      var gameObjects = SceneManager.GetActiveScene().GetRootGameObjects();
      var entities = new List<RagonEntity>();
      foreach (var go in gameObjects)
      {
        var links = go.GetComponentsInChildren<RagonLink>();
        foreach (var link in links)
        {
          var properties = link.Discovery();
          var entity = new RagonEntity(link.Type, link.StaticID);
          foreach (var property in properties)
            entity.State.AddProperty(property);
          
          entity.Attached += link.OnAttached;
          entity.Detached += link.OnDetached;
          entity.OwnershipChanged += link.OnOwnershipChanged;
          
          entities.Add(entity);
        }
      }
      return entities.ToArray();
    }
  }
}