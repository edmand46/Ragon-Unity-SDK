using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  public class RagonRoom: IRagonRoom
  {
    private RagonConnection _connection;
    private RagonSerializer _serializer = new();
    private List<RagonPlayer> _players = new();
    private Dictionary<string, RagonPlayer> _playersMap = new();
    private Dictionary<uint, RagonPlayer> _connections = new();
    private string _ownerId;
    private string _localId;

    private Dictionary<int, GameObject> _unattached = new Dictionary<int, GameObject>();

    public RagonRoom(
      RagonConnection connection,
      string id,
      string ownerId,
      string localPlayerId,
      int min,
      int max)
    {
      _connection = connection;
      _ownerId = ownerId;
      _localId = localPlayerId;

      Id = id;
      MinPlayers = min;
      MaxPlayers = max;
    }
    
    public string Id { get; private set; }
    public int MinPlayers { get; private set; }
    public int MaxPlayers { get; private set; }
    
    public RagonPlayer Owner { get; private set; }
    public RagonPlayer LocalPlayer { get; private set; }
    public RagonConnection Connection => _connection;
    
    public ReadOnlyCollection<RagonPlayer> Players => _players.AsReadOnly();
    public IReadOnlyDictionary<uint, RagonPlayer> ConnectionsById => _connections;
    public IReadOnlyDictionary<string, RagonPlayer> PlayersById => _playersMap;
    
    public void Cleanup()
    {
      _players.Clear();
      _playersMap.Clear();
      _connections.Clear();
    }

    public void AddPlayer(uint peerId, string playerId, string playerName)
    {
      if (_playersMap.ContainsKey(playerId))
        return;
      
      var isOwner = playerId == _ownerId;
      var isLocal = playerId == _localId;

      var player = new RagonPlayer(peerId, playerId, playerName, isOwner, isLocal);

      if (player.IsMe)
        LocalPlayer = player;

      if (player.IsRoomOwner)
        Owner = player;

      _players.Add(player);
      _playersMap.Add(player.Id, player);
      _connections.Add(player.PeerId, player);
    }

    public void RemovePlayer(string playerId)
    {
      _playersMap.Remove(playerId, out var player);
      _players.Remove(player);
      _connections.Remove(player.PeerId);
    }

    public void OnOwnershipChanged(string playerId)
    {
      foreach (var player in _players)
      {
        if (player.Id == playerId)
          Owner = player;
        player.IsRoomOwner = player.Id == playerId;
      }
    }

    public void LoadScene(string map)
    {
      if (!LocalPlayer.IsRoomOwner)
      {
        Debug.LogWarning("Only owner can change map");
        return;
      }

      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.LOAD_SCENE);
      _serializer.WriteString(map);

      var sendData = _serializer.ToArray();
      _connection.Send(sendData, DeliveryType.Reliable);
    }
    
    public void SceneLoaded()
    {
      var sendData = new byte[] {(byte) RagonOperation.SCENE_IS_LOADED};
      _connection.Send(sendData, DeliveryType.Reliable);
    }

    public void CreateEntity(GameObject prefab)
    {
      CreateEntity(prefab, null);
    }
    
    public void CreateEntity(GameObject prefab, IRagonPayload spawnPayload)
    {
      var ragonEntity = prefab.GetComponent<RagonEntity>();
      if (!ragonEntity)
      {
        Debug.LogWarning("Ragon Object not found on GO");
        return;
      }

      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.CREATE_ENTITY);
      _serializer.WriteUShort((ushort) ragonEntity.Type);

      ragonEntity.RetrieveProperties();
      ragonEntity.WriteStateInfo(_serializer);

      spawnPayload?.Serialize(_serializer);

      var sendData = _serializer.ToArray();
      _connection.Send(sendData, DeliveryType.Reliable);
    }

    public void DestroyEntity(GameObject gameObject)
    {
      DestroyEntity(gameObject, null);
    }
    
    public void DestroyEntity(GameObject gameObject, IRagonPayload destroyPayload)
    {
      var hasEntity = gameObject.TryGetComponent<RagonEntity>(out var ragonObject);
      if (!hasEntity)
      {
        Debug.LogError($"{gameObject.name} has not Ragon Entity component");
        return;
      }

      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.DESTROY_ENTITY);
      _serializer.WriteInt(ragonObject.Id);

      destroyPayload?.Serialize(_serializer);

      var sendData = _serializer.ToArray();
      _connection.Send(sendData, DeliveryType.Reliable);
    }
  }
}