using System;
using System.Collections.Generic;
using System.Reflection;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client.Prototyping
{
  [ExecuteInEditMode]
  public class RagonObject : MonoBehaviour
  {
    private delegate void OnEventDelegate(RagonPlayer player, RagonSerializer buffer);

    public bool AutoReplication => _replication;
    public bool IsAttached => _attached;
    public bool IsMine => _mine;
    public int Id => _entityId;
    public int Type => _entityType;
    public RagonPlayer Owner => _owner;
    
    [SerializeField] private int _entityType;
    [SerializeField, ReadOnly] private int _entityId;
    [SerializeField, ReadOnly] private bool _mine;
    [SerializeField, ReadOnly] private RagonPlayer _owner;
    [SerializeField, ReadOnly] private bool _attached;
    [SerializeField, ReadOnly] private bool _replication;
    
    protected RagonRoom Room;

    private RagonEntity[] _states; 
    private byte[] _spawnPayload;
    private byte[] _destroyPayload;

    private Dictionary<int, OnEventDelegate> _events = new();
    
    public static string GenerateIdentifier()
    {
      return Guid.NewGuid().ToString().Replace("-", "");
    }

    /// <summary>
    /// The unique name attributed to this network prefab.
    /// </summary>
    [Header("Base Settings")]
    public string uniqueName = GenerateIdentifier();

    /// <summary>
    /// Used in the Unity Inspector to regenerate the unique identifier.
    /// </summary>
    [SerializeField]
    internal bool regenerate;
    
    private void OnValidate()
    {
      if (regenerate)
      {
        uniqueName = GenerateIdentifier();
        regenerate = false;
      }
    }

    public List<RagonPropertyInfo> Prepare()
    {
      var listInfos = new List<RagonPropertyInfo>();
      _states = GetComponents<RagonEntity>();
      foreach (var state in _states)
      {
        var properties = state.Prepare();
        foreach (var prop in properties)
        {
          listInfos.Add(new RagonPropertyInfo() { Size = 4 });
        }
      }

      return listInfos;
    }
    
    public void Attach(int entityType, RagonPlayer owner, int entityId, byte[] payloadData)
    {
      _entityType = entityType;
      _entityId = entityId;
      _owner = owner;
      _attached = true;
      _mine = RagonNetwork.Room.LocalPlayer.Id == owner.Id;
      _spawnPayload = payloadData;
      _replication = true;

      foreach (var state in _states)
        state.Attach(this);
    }

    public void ChangeOwner(RagonPlayer newOwner)
    {
      _owner = newOwner;
      _mine = RagonNetwork.Room.LocalPlayer.Id == newOwner.Id;
    }

    public void Detach(byte[] payload)
    {
      _destroyPayload = payload;
      
      foreach (var state in _states)
        state.Detach();
      
      Destroy(gameObject);
    }

    internal void ProcessState(RagonSerializer data)
    {
      
    }

    internal void ProcessEvent(RagonPlayer player, ushort eventCode, RagonSerializer data)
    {
      if (_events.ContainsKey(eventCode))
        _events[eventCode]?.Invoke(player, data);
    }

    internal T GetPayload<T>(byte[] data) where T : IRagonPayload, new()
    {
      if (data == null) return new T();
      if (data.Length == 0) return new T();

      var serializer = new RagonSerializer();
      serializer.FromArray(data);

      var payload = new T();
      payload.Deserialize(serializer);

      return payload;
    }

    public T GetSpawnPayload<T>() where T : IRagonPayload, new()
    {
      return GetPayload<T>(_spawnPayload);
    }

    public T GetDestroyPayload<T>() where T : IRagonPayload, new()
    {
      return GetPayload<T>(_destroyPayload);
    }
  }
}