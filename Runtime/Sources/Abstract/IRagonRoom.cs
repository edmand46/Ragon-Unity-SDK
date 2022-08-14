using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ragon.Client.Prototyping;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  public interface IRagonRoom
  {
    public string Id { get; }
    
    public int MinPlayers { get; }
    public int MaxPlayers { get; }
    
    public RagonPlayer Owner { get; }
    public RagonPlayer LocalPlayer { get; }
    public ReadOnlyCollection<RagonPlayer> Players { get; }
    public IReadOnlyDictionary<uint, RagonPlayer> Connections { get; }
    public IReadOnlyDictionary<string, RagonPlayer> PlayersMap { get; }
    
    public void LoadScene(string map);
    public void SceneLoaded();
    
    public void CreateEntity(GameObject prefab, IRagonPayload spawnPayload, RagonAuthority state = RagonAuthority.OWNER_ONLY, RagonAuthority events = RagonAuthority.ALL);
    public void CreateEntity(ushort type, IRagonPayload spawnPayload, RagonAuthority state = RagonAuthority.OWNER_ONLY, RagonAuthority events = RagonAuthority.ALL);
    public void CreateStaticEntity(ushort type, ushort staticId, IRagonPayload spawnPayload, RagonAuthority state = RagonAuthority.OWNER_ONLY, RagonAuthority events = RagonAuthority.ALL);
    public void DestroyEntity(int entityId, IRagonPayload destroyPayload);
    
    public void ReplicateEvent(IRagonEvent evnt, RagonTarget target = RagonTarget.ALL, RagonReplicationMode replicationMode = RagonReplicationMode.SERVER_ONLY);
  }
}