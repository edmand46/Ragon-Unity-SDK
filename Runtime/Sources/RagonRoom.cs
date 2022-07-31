using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  public class RagonRoom : IRagonRoom, IRoomInternal
  {
    private RagonConnection _connection;
    private List<IRagonNetworkListener> _listeners;
    private IRagonEntityManager _entityManager;
    private RagonSerializer _serializer = new();
    private List<RagonPlayer> _players = new();
    private Dictionary<string, RagonPlayer> _playersMap = new();
    private Dictionary<uint, RagonPlayer> _connections = new();
    private string _ownerId;
    private string _localId;

    public RagonRoom(List<IRagonNetworkListener> listeners, IRagonEntityManager manager, RagonConnection connection, string id, string ownerId,
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
      ;
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

    public void CreateStaticEntity(ushort entityType, ushort staticId, IRagonSerializable spawnPayload, RagonAuthority state = RagonAuthority.OWNER_ONLY,
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

    public void CreateEntity(ushort entityType, IRagonSerializable spawnPayload, RagonAuthority state = RagonAuthority.OWNER_ONLY,
      RagonAuthority events = RagonAuthority.OWNER_ONLY)
    {
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.CREATE_ENTITY);
      _serializer.WriteUShort(entityType);
      _serializer.WriteByte((byte) state);
      _serializer.WriteByte((byte) events);

      spawnPayload?.Serialize(_serializer);

      var sendData = _serializer.ToArray();
      _connection.SendData(sendData);
    }

    public void DestroyEntity(int entityId, IRagonSerializable destroyPayload)
    {
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.DESTROY_ENTITY);
      _serializer.WriteInt(entityId);
      
      destroyPayload?.Serialize(_serializer);

      var sendData = _serializer.ToArray();
      _connection.SendData(sendData);
    }

    public void ReplicateEntityEvent(ushort evntCode, int entityId, RagonEventMode eventMode = RagonEventMode.SERVER_ONLY)
    {
      if (eventMode == RagonEventMode.LOCAL_ONLY)
      {
        _serializer.Clear();
        _entityManager.OnEntityEvent(LocalPlayer, entityId, evntCode, _serializer);
        return;
      }

      if (eventMode == RagonEventMode.LOCAL_AND_SERVER)
      {
        _serializer.Clear();
        _entityManager.OnEntityEvent(LocalPlayer, entityId, evntCode, _serializer);
      }

      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
      _serializer.WriteUShort(evntCode);
      _serializer.WriteInt(entityId);

      var sendData = _serializer.ToArray();
      _connection.SendData(sendData);
    }

    public void ReplicateEvent(ushort evntCode, IRagonSerializable payload, RagonEventMode eventMode = RagonEventMode.SERVER_ONLY)
    {
      if (eventMode == RagonEventMode.LOCAL_ONLY)
      {
        _serializer.Clear();
        payload.Serialize(_serializer);
        foreach (var listener in _listeners)
          listener.OnEvent(RagonNetwork.Room.LocalPlayer, evntCode, _serializer);

        return;
      }
      
      if (eventMode == RagonEventMode.LOCAL_AND_SERVER)
      {
        _serializer.Clear();
        payload.Serialize(_serializer);
        foreach (var listener in _listeners)
          listener.OnEvent(RagonNetwork.Room.LocalPlayer, evntCode, _serializer);
      }
      
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.REPLICATE_EVENT);
      _serializer.WriteUShort(evntCode);
      _serializer.WriteByte((byte) eventMode);
      _serializer.WriteByte((byte) eventMode);

      payload.Serialize(_serializer);

      var sendData = _serializer.ToArray();
      _connection.SendData(sendData);
    }

    public void ReplicateEntityEvent(ushort evntCode, int entityId, IRagonSerializable payload, RagonEventMode eventMode = RagonEventMode.SERVER_ONLY)
    {
      if (eventMode == RagonEventMode.LOCAL_ONLY)
      {
        _serializer.Clear();
        payload.Serialize(_serializer);
        _entityManager.OnEntityEvent(LocalPlayer, entityId, evntCode, _serializer);
        return;
      }
      
      if (eventMode == RagonEventMode.LOCAL_AND_SERVER)
      {
        _serializer.Clear();
        payload.Serialize(_serializer);
        _entityManager.OnEntityEvent(LocalPlayer, entityId, evntCode, _serializer);
      }
      
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_EVENT);
      _serializer.WriteUShort(evntCode);
      _serializer.WriteByte((byte) eventMode);
      _serializer.WriteInt(entityId);
      payload.Serialize(_serializer);

      var sendData = _serializer.ToArray();
      _connection.SendData(sendData);
    }

    public void ReplicateEvent(ushort evntCode, RagonEventMode eventMode = RagonEventMode.SERVER_ONLY)
    {
      if (eventMode == RagonEventMode.LOCAL_ONLY)
      {
        _serializer.Clear();
        foreach (var listener in _listeners)
          listener.OnEvent(RagonNetwork.Room.LocalPlayer, evntCode, _serializer);

        return;
      }

      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.REPLICATE_EVENT);
      _serializer.WriteUShort(evntCode);
      _serializer.WriteByte((byte) eventMode);

      if (eventMode == RagonEventMode.LOCAL_AND_SERVER)
      {
        _serializer.Clear();
        foreach (var listener in _listeners)
          listener.OnEvent(RagonNetwork.Room.LocalPlayer, evntCode, _serializer);
      }

      var sendData = _serializer.ToArray();
      _connection.SendData(sendData);
    }

    public void ReplicateEntityState(int entityId, IRagonSerializable payload)
    {
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.REPLICATE_ENTITY_STATE);
      _serializer.WriteInt(entityId);
      
      payload.Serialize(_serializer);
      
      var sendData = _serializer.ToArray();
      _connection.SendData(sendData);
    }
  }
}