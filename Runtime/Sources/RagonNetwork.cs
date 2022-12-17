using System;
using Ragon.Client;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  [DefaultExecutionOrder(-1500), DisallowMultipleComponent]
  public class RagonNetwork : MonoBehaviour
  {
    private static RagonNetwork _instance;

    [SerializeField] private RagonSocketType _socketType;

    private RagonRoom _room;
    private IRagonConnection _connection;
    private RagonSession _session;
    private RagonEntityManager _entityManager;
    private RagonEventManager _eventManager;
    private RagonEventRegistry _eventRegistry;
    private RagonSerializer _writer;

    public static RagonSession Session => _instance._session;
    public static RagonEventRegistry Event => _instance._eventRegistry;
    public static RagonConnectionStatus Status => _instance._connection.Status;
    public static IRagonRoom Room => _instance._room;

    private void Awake()
    {
      _instance = this;
      DontDestroyOnLoad(gameObject);

      switch (_socketType)
      {
        case RagonSocketType.UDP:
        {
          var conn = new RagonENetConnection();
          conn.OnData += OnData;
          conn.OnConnected += OnConnected;
          conn.OnDisconnected += OnDisconnected;
          conn.Prepare();
          
          _connection = conn;
          break;
        }
        case RagonSocketType.WebSocket:
        {
          var conn = new RagonWebSocketConnection();
          conn.OnData += OnData;
          conn.OnConnected += OnConnected;
          conn.OnDisconnected += OnDisconnected;

          _connection = conn;
          break;
        }
      }
      
      _session = new RagonSession(_connection);
      _writer = new(2048);
      _eventRegistry = new RagonEventRegistry();
      _eventManager = new RagonEventManager();
    }

    private void OnConnected()
    {
      _eventManager.OnConnected();
    }

    private void OnDisconnected()
    {
      _eventManager.OnDisconnected();
    }

    private void FixedUpdate()
    {
      _connection.Update();
    }

    private void OnDestroy()
    {
      _connection.Disconnect();
      _connection.Dispose();
    }

    public static void AddListener(IRagonListener listener)
    {
      _instance._eventManager.Add(listener);
    }

    public static void RemoveListener(IRagonListener listener)
    {
      _instance._eventManager.Remove(listener);
    }

    public static void SetManager(RagonEntityManager manager)
    {
      _instance._entityManager = manager;
    }

    public static void Connect(string url, ushort port, string protocol = "1.0.0")
    {
      var encoded = RagonVersion.Parse(protocol);
      _instance._connection.Connect(url, port, encoded);
    }

    public static void Disconnect()
    {
      _instance._entityManager.Cleanup();
      _instance._connection.Disconnect();
      _instance.OnDisconnected();
    }

    private void OnData(byte[] bytes)
    {
      if (_entityManager == null)
      {
        Debug.LogWarning("Entity Manager not defined!");
        return;
      }

      if (_eventManager.Count == 0)
      {
        Debug.LogWarning("Event Listeners is 0!");
        return;
      }

      ReadOnlySpan<byte> rawData = bytes.AsSpan();

      _writer.Clear();
      _writer.FromSpan(ref rawData);

      var operation = _writer.ReadOperation();
      switch (operation)
      {
        case RagonOperation.AUTHORIZED_SUCCESS:
        {
          var playerId = _writer.ReadString();
          var playerName = _writer.ReadString();

          _eventManager.OnAuthorized(playerId, playerName);
          break;
        }
        case RagonOperation.JOIN_SUCCESS:
        {
          var roomId = _writer.ReadString();
          var playerId = _writer.ReadString();
          var ownerId = _writer.ReadString();
          var min = _writer.ReadUShort();
          var max = _writer.ReadUShort();
          var map = _writer.ReadString();

          var room = new RagonRoom(_connection, _entityManager, roomId, min, max);
          room.SetOwnerAndLocal(ownerId, playerId);

          _entityManager.OnRoomCreated(room);
          _room = room;
          _eventManager.OnLevel(map);
          break;
        }
        case RagonOperation.JOIN_FAILED:
        {
          var message = _writer.ReadString();
          _eventManager.OnFailed(message);
          break;
        }
        case RagonOperation.LEAVE_ROOM:
        {
          _eventManager.OnLeaved();
          _room.Cleanup();
          _entityManager.Cleanup();
          _entityManager.OnRoomDestroyed();
          _room = null;
          break;
        }
        case RagonOperation.OWNERSHIP_CHANGED:
        {
          var newOwnerId = _writer.ReadString();
          var player = _room.PlayersById[newOwnerId];

          _room.OnOwnershipChanged(newOwnerId);
          _eventManager.OnOwnerShipChanged(player);

          var entities = _writer.ReadUShort();
          for (var i = 0; i < entities; i++)
          {
            var entityId = _writer.ReadUShort();
            _entityManager.OnOwnerShipChanged(player, entityId);
          }

          break;
        }
        case RagonOperation.PLAYER_JOINED:
        {
          var playerPeerId = (uint)_writer.ReadUShort();
          var playerId = _writer.ReadString();
          var playerName = _writer.ReadString();
          _room.AddPlayer(playerPeerId, playerId, playerName);
          if (_room.PlayersById.TryGetValue(playerId, out var player))
          {
            _eventManager.OnPlayerJoined(player);
          }
          else
          {
            Debug.Log($"[Joined] {playerId}");
          }

          break;
        }
        case RagonOperation.PLAYER_LEAVED:
        {
          var playerId = _writer.ReadString();
          if (_room.PlayersById.TryGetValue(playerId, out var player))
          {
            _room.RemovePlayer(playerId);
            _eventManager.OnPlayerLeft(player);

            var entities = _writer.ReadUShort();
            var toDeleteIds = new ushort[entities];
            for (var i = 0; i < entities; i++)
            {
              var entityId = _writer.ReadUShort();
              toDeleteIds[i] = entityId;
            }

            foreach (var id in toDeleteIds)
              _entityManager.OnEntityDestroyed(id, _writer);
          }
          else
          {
            Debug.Log($"[Leaved] {playerId}");
          }

          break;
        }
        case RagonOperation.LOAD_SCENE:
        {
          _entityManager.Cleanup();
          _room?.Cleanup();

          var sceneName = _writer.ReadString();
          _eventManager.OnLevel(sceneName);
          break;
        }
        case RagonOperation.CREATE_ENTITY:
        {
          var entityType = _writer.ReadUShort();
          var entityId = _writer.ReadUShort();
          var ownerId = (uint)_writer.ReadUShort();

          if (_room.ConnectionsById.TryGetValue(ownerId, out var owner))
            _entityManager.OnEntityCreated(entityId, entityType, owner, _writer);
          else
            Debug.LogWarning($"Owner {ownerId} not found in players");
          break;
        }
        case RagonOperation.DESTROY_ENTITY:
        {
          var entityId = _writer.ReadInt();
          _entityManager.OnEntityDestroyed(entityId, _writer);
          break;
        }
        case RagonOperation.REPLICATE_ENTITY_STATE:
        {
          var entitiesCount = _writer.ReadUShort();
          for (var i = 0; i < entitiesCount; i++)
          {
            var entityId = _writer.ReadUShort();
            _entityManager.OnEntityState(entityId, _writer);
          }

          break;
        }
        case RagonOperation.REPLICATE_ENTITY_EVENT:
        {
          var eventCode = _writer.ReadUShort();
          var peerId = _writer.ReadUShort();
          var executionMode = (RagonReplicationMode)_writer.ReadByte();
          var entityId = _writer.ReadUShort();

          if (!_room.ConnectionsById.TryGetValue(peerId, out var player))
            break;

          if (executionMode == RagonReplicationMode.LocalAndServer && player.IsMe)
            break;

          _entityManager.OnEntityEvent(player, entityId, eventCode, _writer);
          break;
        }
        case RagonOperation.SNAPSHOT:
        {
          var playersCount = _writer.ReadUShort();
          Debug.Log("Players: " + playersCount);
          for (var i = 0; i < playersCount; i++)
          {
            var playerPeerId = (uint)_writer.ReadUShort();
            var playerId = _writer.ReadString();
            var playerName = _writer.ReadString();
            _room.AddPlayer(playerPeerId, playerId, playerName);
          }

          var dynamicEntities = _writer.ReadUShort();
          Debug.Log("Dynamic Entities: " + dynamicEntities);
          for (var i = 0; i < dynamicEntities; i++)
          {
            var entityType = _writer.ReadUShort();
            var entityId = _writer.ReadUShort();
            var ownerPeerId = (uint)_writer.ReadUShort();
            var player = _room.ConnectionsById[ownerPeerId];

            _entityManager.OnEntityCreated(entityId, entityType, player, _writer);
          }

          var staticEntities = _writer.ReadUShort();
          Debug.Log("Scene Entities: " + staticEntities);
          for (var i = 0; i < staticEntities; i++)
          {
            var entityType = _writer.ReadUShort();
            var entityId = _writer.ReadUShort();
            var staticId = _writer.ReadUShort();
            var ownerPeerId = _writer.ReadUShort();
            var player = _room.ConnectionsById[ownerPeerId];

            _entityManager.OnEntityStaticCreated(entityId, staticId, entityType, player, _writer);
          }

          _eventManager.OnJoined();

          break;
        }
      }
    }
  }
}