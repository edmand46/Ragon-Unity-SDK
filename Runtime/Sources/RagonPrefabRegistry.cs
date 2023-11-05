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

// Modifications copyright (C) 2023 Oleg Dzhuraev <godlikeaurora@gmail.com>

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ragon.Client.Unity
{
  [Serializable]
  public struct EntityPrefab
  {
    [HideInInspector] public ushort EntityType;
    public GameObject Prefab;
  }

  [CreateAssetMenu(fileName = "RagonPrefabRegistry")]
  public class RagonPrefabRegistry : ScriptableObject
  {
    [SerializeField] private List<EntityPrefab> _prefabs = new List<EntityPrefab>();

    public IReadOnlyDictionary<ushort, GameObject> Prefabs => _prefabsMap;
    private Dictionary<ushort, GameObject> _prefabsMap = new Dictionary<ushort, GameObject>();
    private Dictionary<string, ushort> _prefabTypes = new Dictionary<string, ushort>();

    public void Cache()
    {
      _prefabsMap.Clear();
      foreach (var entityPrefab in _prefabs)
        _prefabsMap.Add(entityPrefab.EntityType, entityPrefab.Prefab);
    }

#if UNITY_EDITOR
    public void Rescan()
    {
      var guids = AssetDatabase.FindAssets("t:Prefab");
      var links = new List<RagonLink>();
      ushort sequencer = 0;
      
      foreach (var guid in guids)
      {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        var toCheck = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var obj in toCheck)
        {
          var go = obj as GameObject;
          if (go == null)
            continue;

          var comp = go.GetComponent<RagonLink>();
          if (comp != null)
          {
            links.Add(comp);
            
            if (comp.Type > sequencer)
            {
              sequencer = comp.Type;
            }
          }
        }
      }
      
      _prefabs.Clear();
      
      foreach (var link in links)
      {
        if (!string.IsNullOrEmpty(link.PrefabId))
        {
          _prefabs.Add(new EntityPrefab() { Prefab = link.gameObject, EntityType = link.Type });
          continue;
        }

        RagonLog.Trace($"Found new prefab {link.gameObject.name}");
        
        var uuid = Guid.NewGuid().ToString();
        link.SetPrefabId(uuid.ToString());

        sequencer++;

        _prefabs.Add(new EntityPrefab() { Prefab = link.gameObject, EntityType = sequencer });
        _prefabTypes.Add(uuid, sequencer);

        link.Discovery();

        Undo.RecordObject(link, "prefabId");
        Undo.RecordObject(link, "_behaviours");
        Undo.RecordObject(link, "_properties");

        EditorUtility.SetDirty(link);
      }

      EditorUtility.SetDirty(this);
    }
#endif
  }
}