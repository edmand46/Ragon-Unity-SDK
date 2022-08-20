using System;
using Ragon.Client.Prototyping;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  public enum RagonConnectionState
  {
    DISCONNECTED,
    CONNECTED,
  }
  
  [DefaultExecutionOrder(-1500), DisallowMultipleComponent]
  public class RagonNetwork : MonoBehaviour
  {
    private static RagonNetwork _instance;
    private RagonRoom _room;
    private RagonConnection _connection;
    private RagonSession _session;
    private RagonEntityManager _entityManager;
    private RagonEventManager _eventManager;
    private RagonEventRegistry _eventRegistry;
    private RagonSerializer _serializer = new(8192);

    public static RagonSession Session => _instance._session;
    public static IRagonRoom Room => _instance._room;
    public static IRagonConnection Connection => _instance._connection;
    public static RagonEventRegistry Event => _instance._eventRegistry;
    public static RagonConnectionState State => _instance._connection.ConnectionState;

    private void Awake()
    {
      _instance = this;
      DontDestroyOnLoad(gameObject);
      
      _connection = new RagonConnection();
      _connection.OnData += OnData;
      _connection.OnConnected += OnConnected;
      _connection.OnDisconnected += OnDisconnected;
      _connection.Prepare();
      
      _session = new RagonSession(_connection);
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
      var strings = protocol.Split(".");
      if (strings.Length < 3)
      {
        Debug.LogWarning("Wrong protocol passed to connect method");
        return;
      }

      var parts = new uint[] {0, 0, 0};
      for (int i = 0; i < parts.Length; i++)
      {
        if (!uint.TryParse(strings[i], out var v))
        {
          Debug.LogWarning("Wrong version passed to connect method");
          return;
        }  
        
        parts[i] = v;
      }
      
      uint encoded = (parts[0] << 16) | (parts[1] << 8) | parts[2];
      _instance._connection.Connect(url, port, encoded);
    }

    public static void Disconnect()
    {
      _instance._connection.Dispose();
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
      
      _serializer.Clear();
      _serializer.FromSpan(ref rawData);

      var operation = _serializer.ReadOperation();
      switch (operation)
      {
        case RagonOperation.AUTHORIZED_SUCCESS:
        {
          var playerId = _serializer.ReadString();
          var playerName = _serializer.ReadString();

          _eventManager.OnAuthorized(playerId, playerName);
          break;
        }
        case RagonOperation.JOIN_SUCCESS:
        {
          var roomId = _serializer.ReadString();
          var localId = _serializer.ReadString();
          var ownerId = _serializer.ReadString();
          var min = _serializer.ReadUShort();
          var max = _serializer.ReadUShort();

          var room = new RagonRoom(_eventManager, _connection, roomId, ownerId, localId, min, max);
          _entityManager.OnRoomCreated(room);
          _room = room;
          break;
        }
        case RagonOperation.JOIN_FAILED:
        {
          var message = _serializer.ReadString();
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
          var newOwnerId = _serializer.ReadString();
          _room.OnOwnershipChanged(newOwnerId);
          var player = _room.PlayersById[newOwnerId];

          _eventManager.OnOwnerShipChanged(player);
          _entityManager.OnOwnerShipChanged(player);
          break;
        }
        case RagonOperation.PLAYER_JOINED:
        {
          var playerPeerId = (uint) _serializer.ReadUShort();
          var playerId = _serializer.ReadString();
          var playerName = _serializer.ReadString();
          _room.AddPlayer(playerPeerId, playerId, playerName);

          var player = _room.PlayersById[playerId];
          _eventManager.OnPlayerJoined(player);
          break;
        }
        case RagonOperation.PLAYER_LEAVED:
        {
          var playerId = _serializer.ReadString();
          var player = _room.PlayersById[playerId];
          
          _room.RemovePlayer(playerId);
          _eventManager.OnPlayerLeft(player);

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
          _entityManager.Cleanup();
          _room?.Cleanup();

          var sceneName = _serializer.ReadString();
          _eventManager.OnLevel(sceneName);
          break;
        }
        case RagonOperation.CREATE_SCENE_ENTITY:
        {
          var entityType = _serializer.ReadUShort();
          var entityId = _serializer.ReadUShort();
          var staticId = _serializer.ReadUShort();
          var ownerId = (uint) _serializer.ReadUShort();
          var stateAuthority = RagonAuthority.OWNER_ONLY;
          var eventAuthority = RagonAuthority.ALL;
          var payload = _serializer.ReadData(_serializer.Size);

          if (_room.ConnectionsById.TryGetValue(ownerId, out var owner))
            _entityManager.OnEntityStaticCreated(entityId, staticId, entityType, stateAuthority, eventAuthority, owner, payload.ToArray());
          else
            Debug.LogWarning($"Owner {ownerId} not found in players");
          break;
        }
        case RagonOperation.CREATE_ENTITY:
        {
          var entityType = _serializer.ReadUShort();
          var entityId = _serializer.ReadUShort();
          var ownerId = (uint) _serializer.ReadUShort();
          var stateAuthority = RagonAuthority.ALL;
          var eventAuthority = RagonAuthority.OWNER_ONLY;
          var payload = Array.Empty<byte>();

          if (_serializer.Size > 0)
          {
            var entityPayload = _serializer.ReadData(_serializer.Size);
            payload = entityPayload.ToArray();
          }

          if (_room.ConnectionsById.TryGetValue(ownerId, out var owner))
            _entityManager.OnEntityCreated(entityId, entityType, stateAuthority, eventAuthority, owner, payload);
          else
            Debug.LogWarning($"Owner {ownerId} not found in players");
          break;
        }
        case RagonOperation.DESTROY_ENTITY:
        {
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
          var entitiesCount = _serializer.ReadUShort();
          for (var i = 0; i < entitiesCount; i++)
          {
            var entityId = _serializer.ReadUShort();
            _entityManager.OnEntityState(entityId, _serializer);
          }

          break;
        }
        case RagonOperation.REPLICATE_EVENT:
        {
          var executorPeerId = _serializer.ReadUShort();
          var executionMode = (RagonReplicationMode) _serializer.ReadByte();
          var eventCode = _serializer.ReadUShort();

          if (_room.ConnectionsById.TryGetValue(executorPeerId, out var player)
              && executionMode == RagonReplicationMode.LOCAL_AND_SERVER
              && !player.IsMe)
          {
            _eventManager.OnEvent(player, eventCode, _serializer);
          }

          break;
        }
        case RagonOperation.REPLICATE_ENTITY_EVENT:
        {
          var eventCode = _serializer.ReadUShort();
          var peerId = _serializer.ReadUShort();
          var executionMode = (RagonReplicationMode) _serializer.ReadByte();
          var entityId = _serializer.ReadUShort();

          if (!_room.ConnectionsById.TryGetValue(peerId, out var player))
            break;

          if (executionMode == RagonReplicationMode.LOCAL_AND_SERVER && player.IsMe)
            break;

          _entityManager.OnEntityEvent(player, entityId, eventCode, _serializer);
          break;
        }
        case RagonOperation.SNAPSHOT:
        {
          var playersCount = _serializer.ReadUShort();
          Debug.Log("Players: " + playersCount);
          for (var i = 0; i < playersCount; i++)
          {
            var playerId = _serializer.ReadString();
            var playerPeerId = (uint) _serializer.ReadUShort();
            var playerName = _serializer.ReadString();

            _room.AddPlayer(playerPeerId, playerId, playerName);
          }
          
          _entityManager.CollectSceneData();
          
          var dynamicEntities = _serializer.ReadUShort();
          Debug.Log("Dynamic Entities: " + dynamicEntities);
          for (var i = 0; i < dynamicEntities; i++)
          {
            var entityType = _serializer.ReadUShort();
            var entityId = _serializer.ReadUShort();
            var ownerPeerId = (uint) _serializer.ReadUShort();
            var stateAuthority = RagonAuthority.ALL;
            var eventAuthority = RagonAuthority.OWNER_ONLY;
            var payloadLenght = _serializer.ReadUShort();
            var payloadData = _serializer.ReadData(payloadLenght);
            var player = _room.ConnectionsById[ownerPeerId];

            _entityManager.OnEntityCreated(entityId, entityType, stateAuthority, eventAuthority, player, payloadData.ToArray());
          }
          
          var staticEntities = _serializer.ReadUShort();
          Debug.Log("Scene Entities: " + staticEntities);
          for (var i = 0; i < staticEntities; i++)
          {
            var entityType = _serializer.ReadUShort();
            var entityId = _serializer.ReadUShort();
            var staticId = _serializer.ReadUShort();
            var ownerPeerId = _serializer.ReadUShort();
            var payloadLenght = _serializer.ReadUShort();
            var payloadData = _serializer.ReadData(payloadLenght);
            var stateAuthority = RagonAuthority.OWNER_ONLY;
            var eventAuthority = RagonAuthority.ALL;

            var player = _room.ConnectionsById[ownerPeerId];

            _entityManager.OnEntityStaticCreated(entityId, staticId, entityType, stateAuthority, eventAuthority, player, payloadData.ToArray());
          }

          _eventManager.OnJoined();

          Debug.Log("Snapshot received");
          break;
        }
      }
    }
  }
}