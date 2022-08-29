using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  public interface IRagonRoom
  {
    public string Id { get; }
    public RagonPlayer Owner { get; }
    public RagonPlayer LocalPlayer { get; }
    public int MinPlayers { get; }
    public int MaxPlayers { get; }

    public ReadOnlyCollection<RagonPlayer> Players { get; }
    public IReadOnlyDictionary<string, RagonPlayer> PlayersById { get; }
    public IReadOnlyDictionary<uint, RagonPlayer> ConnectionsById { get; }

    public void LoadScene(string map);
    public void SceneLoaded();
    
    public void CreateEntity(GameObject prefab, IRagonPayload spawnPayload);
    public void CreateEntity(GameObject prefab);

    public void DestroyEntity(GameObject gameObject);
    public void DestroyEntity(GameObject gameObject, IRagonPayload destroyPayload);
  }
}