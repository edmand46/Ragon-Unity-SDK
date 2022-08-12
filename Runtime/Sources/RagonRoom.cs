using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using Ragon.Client.Prototyping;
using Ragon.Common;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Ragon.Client
{
  public class RagonRoom : IRoomInternal
  {
    private RagonConnection _connection;
    private List<IRagonNetworkListener> _listeners;
    private RagonEntityManager _entityManager;
    private RagonSerializer _serializer = new();
    private List<RagonPlayer> _players = new();
    private Dictionary<string, RagonPlayer> _playersMap = new();
    private Dictionary<uint, RagonPlayer> _connections = new();
    private string _ownerId;
    private string _localId;

    private Dictionary<int, GameObject> _unattached = new Dictionary<int, GameObject>();

    public RagonRoom(List<IRagonNetworkListener> listeners, RagonEntityManager manager, RagonConnection connection, string id, string ownerId,
      string localPlayerId,
      int min, int max)
    {
      _entityManager = manager;
      _listeners = listeners;
      _connection = connection;
      _ownerId = ownerId;
      _localId = localPlayerId;

      Id = id;
      MinPlayers = min;
      MaxPlayers = max;
    }

    public RagonPlayer Owner { get; private set; }
    public RagonPlayer LocalPlayer { get; private set; }

    public ReadOnlyCollection<RagonPlayer> Players => _players.AsReadOnly();
    public IReadOnlyDictionary<uint, RagonPlayer> Connections => _connections;
    public IReadOnlyDictionary<string, RagonPlayer> PlayersMap => _playersMap;

    public string Id { get; private set; }
    public int MinPlayers { get; private set; }
    public int MaxPlayers { get; private set; }

    public void Cleanup()
    {
      _players.Clear();
      _playersMap.Clear();
      _connections.Clear();
    }

    public void AddPlayer(uint peerId, string playerId, string playerName)
    {
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
      _connection.SendData(sendData);
    }

    public void SceneLoaded()
    {
      var sendData = new byte[] {(byte) RagonOperation.SCENE_IS_LOADED};
      _connection.SendData(sendData);
    }

    public void CreateStaticEntity(ushort entityType, ushort staticId, IRagonPayload spawnPayload, RagonAuthority state = RagonAuthority.OWNER_ONLY,
      RagonAuthority events = RagonAuthority.OWNER_ONLY)
    {
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.CREATE_STATIC_ENTITY);
      _serializer.WriteUShort(entityType);
      _serializer.WriteUShort(staticId);
      _serializer.WriteByte((byte) state);
      _serializer.WriteByte((byte) events);

      spawnPayload?.Serialize(_serializer);

      var sendData = _serializer.ToArray();
      _connection.SendData(sendData);
    }

    public void CreateEntity(GameObject prefab, IRagonPayload spawnPayload, RagonAuthority state = RagonAuthority.OWNER_ONLY, RagonAuthority events = RagonAuthority.OWNER_ONLY)
    {
      var ragonGroup = prefab.GetComponent<RagonObject>();
      if (!ragonGroup)
      {
        Debug.LogWarning("Ragon Object not found on GO");
        return;
      }

      var infos = ragonGroup.Prepare();
      var count = (ushort)infos.Count;
      
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.CREATE_ENTITY);
      _serializer.WriteUShort(25);
      _serializer.WriteUShort(count);
      foreach (var info in infos)
        _serializer.WriteUShort((ushort) info.Size);
      
      var sendData = _serializer.ToArray();
      _connection.SendData(sendData);
    }

    public void DestroyEntity(GameObject gameObject, IRagonPayload destroyPayload)
    {
      
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.DESTROY_ENTITY);
      // _serializer.WriteInt(entityId);

      destroyPayload?.Serialize(_serializer);

      var sendData = _serializer.ToArray();
      _connection.SendData(sendData);
    }

    public void ReplicateEvent(IRagonEvent evnt, RagonTarget target = RagonTarget.ALL, RagonReplicationMode replicationMode = RagonReplicationMode.SERVER_ONLY)
    {
      var evntCode = RagonNetwork.EventManager.GetEventCode(evnt);
      if (replicationMode == RagonReplicationMode.LOCAL_ONLY)
      {
        _serializer.Clear();
        foreach (var listener in _listeners)
          listener.OnEvent(RagonNetwork.Room.LocalPlayer, evntCode, _serializer);

        return;
      }

      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.REPLICATE_EVENT);
      _serializer.WriteUShort(evntCode);
      _serializer.WriteByte((byte) replicationMode);
      _serializer.WriteByte((byte) target);

      if (replicationMode == RagonReplicationMode.LOCAL_AND_SERVER)
      {
        _serializer.Clear();
        foreach (var listener in _listeners)
          listener.OnEvent(RagonNetwork.Room.LocalPlayer, evntCode, _serializer);
      }

      var sendData = _serializer.ToArray();
      _connection.SendData(sendData);
    }
  }
}