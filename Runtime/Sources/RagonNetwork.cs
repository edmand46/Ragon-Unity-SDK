using System;
using System.Collections.Generic;
using NetStack.Serialization;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  [DefaultExecutionOrder(-1500), DisallowMultipleComponent]
  public class RagonNetwork : MonoBehaviour
  {
    private static RagonNetwork _instance;

    private RagonState _state;
    private RagonConnection _connection;
    private IRagonRoom _room;
    private IRoomInternal _roomInternal;
    private List<IRagonNetworkListener> _listeners = new List<IRagonNetworkListener>();
    private IRagonEntityManager _entityManager;
    private BitBuffer _buffer = new BitBuffer(8192);
    private RagonSerializer _serializer = new RagonSerializer(8192);

    public static IRagonRoom Room => _instance._room;
    public static IRagonEntityManager Manager => _instance._entityManager;
    public static RagonState State => _instance._state;

    private void Awake()
    {
      _instance = this;

      _connection = new RagonConnection();
      _connection.OnData += OnData;
      _connection.OnConnected += OnConnected;
      _connection.OnDisconnected += OnDisconnected;
      _connection.Prepare();
    }

    #region PUBLIC_API

    public static void SetManager(IRagonEntityManager manager)
    {
      _instance._entityManager = manager;
    }
    public static void AddListener(IRagonNetworkListener listener)
    {
      _instance._listeners.Add(listener);
    }

    public static void AuthorizeWithKey(string key, string playerName, byte protocol, byte[] additionalData)
    {
      _instance.Authorize(key, playerName, protocol, additionalData);
    }

    public static void CreateOrJoin(string map, int minPlayers, int maxPlayers)
    {
      _instance.JoinOrCreateInternal(map, minPlayers, maxPlayers);
    }

    public static void Join(string roomId)
    {
      _instance.JoinInternal(roomId);
    }

    public static void Leave()
    {
      _instance.LeaveInternal();
    }

    public static void ConnectToServer(string url, ushort port)
    {
      _instance._connection.Connect(url, port);
    }

    public static void Disconnect()
    {
      _instance.OnDisconnected();
      _instance._connection.Dispose();
    }

    #endregion

    private void LeaveInternal()
    {
      var sendData = new[] {(byte) RagonOperation.LEAVE_ROOM};
      _connection.SendData(sendData, DeliveryType.Reliable);
    }

    private void JoinInternal(string roomId)
    {
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.JOIN_ROOM);
      _serializer.WriteString(roomId);

      var sendData = _serializer.ToArray();
      _connection.SendData(sendData, DeliveryType.Reliable);
    }

    private void JoinOrCreateInternal(string map, int min, int max)
    {
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.JOIN_OR_CREATE_ROOM);
      _serializer.WriteUShort((ushort) min);
      _serializer.WriteUShort((ushort) max);
      _serializer.WriteString(map);

      var sendData = _serializer.ToArray();
      _connection.SendData(sendData);
    }

    private void Authorize(string key, string playerName, byte protocol, byte[] additonalData)
    {
      ReadOnlySpan<byte> payload = additonalData.AsSpan();

      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.AUTHORIZE);
      _serializer.WriteString(key);
      _serializer.WriteString(playerName);
      _serializer.WriteByte(protocol);
      _serializer.WriteData(ref payload);

      var sendData = _serializer.ToArray();
      _connection.SendData(sendData);
    }

    #region CALLBACKS

    private void OnDisconnected()
    {
      foreach (var listener in _listeners)
       listener.OnDisconnected(); 
      _state = RagonState.DISCONNECTED;
    }

    private void OnConnected()
    {
      foreach (var listener in _listeners)
        listener.OnConnected(); 
      _state = RagonState.CONNECTED;
    }

    private void OnData(byte[] bytes)
    {
      if (_listeners.Count == 0 || _entityManager == null)
      {
        Debug.LogWarning("Listeners not defined!");
        return;
      }

      ReadOnlySpan<byte> rawData = bytes.AsSpan();

      _serializer.Clear();
      _serializer.FromSpan(ref rawData);

      var operation = _serializer.ReadOperation();
      switch (operation)
      {
        case RagonOperation.AUTHORIZED_SUCCESS:
        {
          var playerId = _serializer.ReadString();
          var playerName = _serializer.ReadString();
          
          foreach (var listener in _listeners)
            listener.OnAuthorized(playerId, playerName);
          break;
        }
        case RagonOperation.JOIN_SUCCESS:
        {
          var roomId = _serializer.ReadString();
          var localId = _serializer.ReadString();
          var ownerId = _serializer.ReadString();
          var min = _serializer.ReadUShort();
          var max = _serializer.ReadUShort();

          var room = new RagonRoom(_listeners, _entityManager, _connection, roomId, ownerId, localId, min, max);

          _room = room;
          _roomInternal = room;
          break;
        }
        case RagonOperation.JOIN_FAILED:
        {
          foreach (var listener in _listeners)
            listener.OnFailed();
          break;
        }
        case RagonOperation.LEAVE_ROOM:
        {
          foreach (var listener in _listeners)
            listener.OnPlayerLeft(_room.LocalPlayer);
          
          _roomInternal.RemovePlayer(_room.LocalPlayer.Id);
          _room = null;
          break;
        }
        case RagonOperation.OWNERSHIP_CHANGED:
        {
          var newOwnerId = _serializer.ReadString();
          _roomInternal.OnOwnershipChanged(newOwnerId);
          var player = _room.PlayersMap[newOwnerId];
          
          foreach (var listener in _listeners)
            listener.OnOwnerShipChanged(player);
          
          _entityManager.OnOwnerShipChanged(player);
          break;
        }
        case RagonOperation.PLAYER_JOINED:
        {
          var playerPeerId = (uint) _serializer.ReadUShort();
          var playerId = _serializer.ReadString();
          var playerName = _serializer.ReadString();
          _roomInternal.AddPlayer(playerPeerId, playerId, playerName);

          var player = _room.PlayersMap[playerId];
          foreach (var listener in _listeners)
            listener.OnPlayerJoined(player);
          break;
        }
        case RagonOperation.PLAYER_LEAVED:
        {
          var playerId = _serializer.ReadString();
          var player = _room.PlayersMap[playerId];

          _roomInternal.RemovePlayer(playerId);
          
          foreach (var listener in _listeners)
            listener.OnPlayerLeft(player);
          
          _buffer.Clear();
          var entities = _serializer.ReadUShort();
          var emptyPayload = Array.Empty<byte>();
          for (var i = 0; i < entities; i++)
          {
            var entityId = _serializer.ReadInt();
            _entityManager.OnEntityDestroyed(entityId, emptyPayload);
          }

          break;
        }
        case RagonOperation.LOAD_SCENE:
        {
          var sceneName = _serializer.ReadString();
          foreach (var listener in _listeners)
            listener.OnLevel(sceneName);
          break;
        }
        case RagonOperation.CREATE_STATIC_ENTITY:
        {
          _buffer.Clear();

          var entityType = _serializer.ReadUShort();
          var staticId = _serializer.ReadUShort();
          var stateAuthority = (RagonAuthority) _serializer.ReadByte();
          var eventAuthority = (RagonAuthority) _serializer.ReadByte();
          var entityId = _serializer.ReadInt();
          var ownerId = (uint) _serializer.ReadUShort();
          var payload = Array.Empty<byte>();

          if (_room.Connections.TryGetValue(ownerId, out var owner))
            _entityManager.OnEntityStaticCreated(entityId, staticId, entityType, stateAuthority, eventAuthority, owner, payload);
          else
            Debug.LogWarning($"Owner {ownerId} not found in players");
          break;
        }
        case RagonOperation.CREATE_ENTITY:
        {
          _buffer.Clear();

          var entityType = _serializer.ReadUShort();
          var stateAuthority = (RagonAuthority) _serializer.ReadByte();
          var eventAuthority = (RagonAuthority) _serializer.ReadByte();
          var entityId = _serializer.ReadInt();
          var ownerId = (uint) _serializer.ReadUShort();
          var payload = Array.Empty<byte>();
          if (_serializer.Size > 0)
          {
            var entityPayload = _serializer.ReadData(_serializer.Size);
            payload = entityPayload.ToArray();
          }
          if (_room.Connections.TryGetValue(ownerId, out var owner))
            _entityManager.OnEntityCreated(entityId, entityType, stateAuthority, eventAuthority, owner, payload);
          else
            Debug.LogWarning($"Owner {ownerId} not found in players");
          break;
        }
        case RagonOperation.DESTROY_ENTITY:
        {
          _buffer.Clear();

          var entityId = _serializer.ReadInt();
          var payload = Array.Empty<byte>();
          
          if (_serializer.Size > 0)
          {
            var entityPayload = _serializer.ReadData(_serializer.Size);
            payload = entityPayload.ToArray();
          }
          _entityManager.OnEntityDestroyed(entityId, payload);
          break;
        }
        case RagonOperation.REPLICATE_ENTITY_STATE:
        {
          _buffer.Clear();
          var entityId = _serializer.ReadInt();
          var entityStateData = _serializer.ReadData(_serializer.Size);

          _buffer.FromSpan(ref entityStateData, entityStateData.Length);
          _entityManager.OnEntityState(entityId, _buffer);
          break;
        }
        case RagonOperation.REPLICATE_EVENT:
        {
          _buffer.Clear();
          var peerId = _serializer.ReadUShort();
          var executionMode = (RagonEventMode) _serializer.ReadByte();
          var eventCode = _serializer.ReadUShort();
          if (_serializer.Size > 0)
          {
            var payloadData = _serializer.ReadData(_serializer.Size);
            _buffer.FromSpan(ref payloadData, payloadData.Length);
          }

          if (_room.Connections.TryGetValue(peerId, out var player)
              && executionMode == RagonEventMode.LOCAL_AND_SERVER
              && !player.IsMe)
          {
            foreach (var listener in _listeners)
              listener.OnEvent(player, eventCode, _buffer);
          }
          break;
        }
        case RagonOperation.REPLICATE_ENTITY_EVENT:
        {
          _buffer.Clear();

          var eventCode = _serializer.ReadUShort();
          var peerId = _serializer.ReadUShort();
          var executionMode = (RagonEventMode) _serializer.ReadByte();
          var entityId = _serializer.ReadInt();

          if (_serializer.Size > 0)
          {
            var eventPayload = _serializer.ReadData(_serializer.Size);
            _buffer.FromSpan(ref eventPayload, eventPayload.Length);
          }

          if (!_room.Connections.TryGetValue(peerId, out var player))
            break;

          if (executionMode == RagonEventMode.LOCAL_AND_SERVER && player.IsMe)
            break;

          _entityManager.OnEntityEvent(player, entityId, eventCode, _buffer);
          break;
        }
        case RagonOperation.SNAPSHOT:
        {
          var playersCount = _serializer.ReadInt();
          Debug.Log("Players: " + playersCount);
          for (var i = 0; i < playersCount; i++)
          {
            var playerId = _serializer.ReadString();
            var playerPeerId = (uint) _serializer.ReadUShort();
            var playerName = _serializer.ReadString();

            _roomInternal.AddPlayer(playerPeerId, playerId, playerName);
          }
          
          var dynamicEntities = _serializer.ReadInt();
          Debug.Log("Dynamic Entities: " + dynamicEntities);
          for (var i = 0; i < dynamicEntities; i++)
          {
            var entityId = _serializer.ReadInt();
            var stateAuthority = (RagonAuthority) _serializer.ReadByte();
            var eventAuthority = (RagonAuthority) _serializer.ReadByte();
            var entityType = _serializer.ReadUShort();
            var ownerPeerId = (uint) _serializer.ReadUShort();
            var payloadLenght = _serializer.ReadUShort();
            var payloadData = _serializer.ReadData(payloadLenght);
            var stateLenght = _serializer.ReadUShort();
            var stateData = _serializer.ReadData(stateLenght);
            var player = _room.Connections[ownerPeerId];

            _entityManager.OnEntityCreated(entityId, entityType, stateAuthority, eventAuthority, player, payloadData.ToArray());

            if (stateLenght > 0)
            {
              _buffer.Clear();
              _buffer.FromSpan(ref stateData, stateData.Length);

              _entityManager.OnEntityState(entityId, _buffer);
            }
          }
          
          _entityManager.OnJoined();
          
          var staticEntities = _serializer.ReadInt();
          Debug.Log("Static Entities: " + staticEntities);
          for (var i = 0; i < staticEntities; i++)
          {
            var entityId = _serializer.ReadInt();
            var staticId = _serializer.ReadUShort();
            var stateAuthority = (RagonAuthority) _serializer.ReadByte();
            var eventAuthority = (RagonAuthority) _serializer.ReadByte();
            var entityType = _serializer.ReadUShort();
            var ownerPeerId = (uint) _serializer.ReadUShort();
            var payloadLenght = _serializer.ReadUShort();
            var payloadData = _serializer.ReadData(payloadLenght);
            var stateLenght = _serializer.ReadUShort();
            var stateData = _serializer.ReadData(stateLenght);
            var player = _room.Connections[ownerPeerId];

            _entityManager.OnEntityStaticCreated(entityId, staticId, entityType, stateAuthority, eventAuthority, player, payloadData.ToArray());

            if (stateLenght > 0)
            {
              _buffer.Clear();
              _buffer.FromSpan(ref stateData, stateData.Length);
              _entityManager.OnEntityState(entityId, _buffer);
            }
          }

          foreach (var listener in _listeners)
            listener.OnJoined();
          
          Debug.Log("Snapshot received");
          break;
        }
      }
    }

    #endregion

    private void Update()
    {
      _connection.Update();
    }

    private void OnDestroy()
    {
      _connection.Dispose();
    }
  }
}