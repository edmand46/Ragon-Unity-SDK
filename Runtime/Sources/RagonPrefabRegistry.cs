using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Ragon.Client
{
  [Serializable]
  public struct EntityPrefab
  {
    [HideInInspector] public ushort EntityType;
    public GameObject Prefab;
  }

  [ExecuteInEditMode]
  [CreateAssetMenu(fileName = "RagonPrefabRegistry")]
  public class RagonPrefabRegistry : ScriptableObject
  {
    [SerializeField] private List<EntityPrefab> _prefabs = new List<EntityPrefab>();
    [SerializeField] private bool _scan = false;

    public IReadOnlyDictionary<ushort, GameObject> Prefabs => _prefabsMap;
    private Dictionary<ushort, GameObject> _prefabsMap = new Dictionary<ushort, GameObject>();

    public void Cache()
    {
      _prefabsMap.Clear();
      foreach (var entityPrefab in _prefabs)
      {
        _prefabsMap.Add(entityPrefab.EntityType, entityPrefab.Prefab);
      }
    }
    
#if UNITY_EDITOR
    private void OnValidate()
    {
      _prefabs.Clear();
      var guids = AssetDatabase.FindAssets("t:Prefab");
      ushort sequencer = 0;

      foreach (var guid in guids)
      {
        var path = AssetDatabase.GUIDToAssetPath(guid);
        var toCheck = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (var obj in toCheck)
        {
          var go = obj as GameObject;
          if (go == null)
          {
            continue;
          }

          var comp = go.GetComponent<RagonEntity>();
          if (comp != null)
          {
            sequencer++;
            
            _prefabs.Add(new EntityPrefab() {Prefab = go, EntityType = sequencer});
            comp.SetType(sequencer);
            
            Undo.RecordObject(comp, "_sceneId");
          }
        }
      }
    }
#endif
  }
}