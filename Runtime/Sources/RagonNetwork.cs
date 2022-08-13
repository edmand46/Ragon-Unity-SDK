using System;
using System.Collections.Generic;
using Ragon.Client.Prototyping;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  [DefaultExecutionOrder(-1500), DisallowMultipleComponent]
  public class RagonNetwork : MonoBehaviour
  {
    private static RagonNetwork _instance;

    private RagonConnectionState _connectionState;
    private RagonConnection _connection;
    private RagonRoom _room;
    private RagonObjectManager _objectManager;
    private RagonSerializer _serializer = new RagonSerializer(8192);
    private RagonEventManager _eventManagerRegistry;
    private List<IRagonNetworkListener> _listeners = new List<IRagonNetworkListener>();
    
    public static RagonRoom Room => _instance._room;
    public static RagonObjectManager ObjectManager => _instance._objectManager;
    public static RagonEventManager EventManager => _instance._eventManagerRegistry;
    public static RagonConnection Connection => _instance._connection;
    public static RagonConnectionState ConnectionState => _instance._connectionState;

    private void Awake()
    {
      _instance = this;
      DontDestroyOnLoad(gameObject);

      _eventManagerRegistry = new RagonEventManager();
      
      _connection = new RagonConnection();
      _connection.OnData += OnData;
      _connection.OnConnected += OnConnected;
      _connection.OnDisconnected += OnDisconnected;
      _connection.Prepare();
    }

    #region PUBLIC_API

    public static void SetManager(RagonObjectManager manager)
    {
      _instance._objectManager = manager;
    }

    public static void AddListener(IRagonNetworkListener listener)
    {
      _instance._listeners.Add(listener);
    }

    public static void RemoveListener(IRagonNetworkListener listener)
    {
      _instance._listeners.Remove(listener);
    }

    public static void AuthorizeWithKey(string key, string playerName, byte protocol, byte[] additionalData)
    {
      _instance.Authorize(key, playerName, protocol, additionalData);
    }

    public static void CreateOrJoin(string map, int minPlayers, int maxPlayers)
    {
      var parameters = new RagonRoomParameters() {Map = map, Min = minPlayers, Max = maxPlayers};
      _instance.JoinOrCreateInternal(parameters);
    }

    public static void CreateOrJoin(RagonRoomParameters parameters)
    {
      _instance.JoinOrCreateInternal(parameters);
    }

    public static void Create(string map, int minPlayers, int maxPlayers)
    {
      var parameters = new RagonRoomParameters() {Map = map, Min = minPlayers, Max = maxPlayers};
      _instance.CreateInternal(null, parameters);
    }

    public static void Create(string roomId, string map, int minPlayers, int maxPlayers)
    {
      var parameters = new RagonRoomParameters() {Map = map, Min = minPlayers, Max = maxPlayers};
      _instance.CreateInternal(roomId, parameters);
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

    private void JoinOrCreateInternal(RagonRoomParameters parameters)
    {
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.JOIN_OR_CREATE_ROOM);

      parameters.Serialize(_serializer);

      var sendData = _serializer.ToArray();
      _connection.SendData(sendData);
    }

    private void CreateInternal(string roomId, RagonRoomParameters parameters)
    {
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.CREATE_ROOM);

      if (roomId != null)
      {
        _serializer.WriteBool(true);
        _serializer.WriteString(roomId);
      }
      else
      {
        _serializer.WriteBool(false);
      }

      parameters.Serialize(_serializer);

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
      _connectionState = RagonConnectionState.DISCONNECTED;
    }

    private void OnConnected()
    {
      foreach (var listener in _listeners)
        listener.OnConnected();
      _connectionState = RagonConnectionState.CONNECTED;
    }

    private void OnData(byte[] bytes)
    {
      if (_listeners.Count == 0 || _objectManager == null)
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

          var room = new RagonRoom(_listeners, _objectManager, _connection, roomId, ownerId, localId, min, max);

          _room = room;
          break;
        }
        case RagonOperation.JOIN_FAILED:
        {
          var message = _serializer.ReadString();
          foreach (var listener in _listeners)
            listener.OnFailed(message);
          break;
        }
        case RagonOperation.LEAVE_ROOM:
        {
          foreach (var listener in _listeners)
          {
            listener.OnPlayerLeft(_room.LocalPlayer);
            listener.OnLeaved();
          }

          foreach (var listener in _listeners)
            listener.OnPlayerLeft(_room.LocalPlayer);

          _room.RemovePlayer(_room.LocalPlayer.Id);

          _room.Cleanup();
          _objectManager.Cleanup();

          _room = null;
          break;
        }
        case RagonOperation.OWNERSHIP_CHANGED:
        {
          var newOwnerId = _serializer.ReadString();
          _room.OnOwnershipChanged(newOwnerId);
          var player = _room.PlayersMap[newOwnerId];

          foreach (var listener in _listeners)
            listener.OnOwnerShipChanged(player);

          _objectManager.OnOwnerShipChanged(player);
          break;
        }
        case RagonOperation.PLAYER_JOINED:
        {
          var playerPeerId = (uint) _serializer.ReadUShort();
          var playerId = _serializer.ReadString();
          var playerName = _serializer.ReadString();
          _room.AddPlayer(playerPeerId, playerId, playerName);

          var player = _room.PlayersMap[playerId];
          foreach (var listener in _listeners)
            listener.OnPlayerJoined(player);
          break;
        }
        case RagonOperation.PLAYER_LEAVED:
        {
          var playerId = _serializer.ReadString();
          var player = _room.PlayersMap[playerId];

          _room.RemovePlayer(playerId);

          foreach (var listener in _listeners)
            listener.OnPlayerLeft(player);

          var entities = _serializer.ReadUShort();
          var emptyPayload = Array.Empty<byte>();
          for (var i = 0; i < entities; i++)
          {
            var entityId = _serializer.ReadInt();
            _objectManager.OnEntityDestroyed(entityId, emptyPayload);
          }

          break;
        }
        case RagonOperation.LOAD_SCENE:
        {
          _objectManager.Cleanup();
          _room?.Cleanup();

          var sceneName = _serializer.ReadString();
          foreach (var listener in _listeners)
            listener.OnLevel(sceneName);
          break;
        }
        case RagonOperation.CREATE_STATIC_ENTITY:
        { 
          var entityType = _serializer.ReadUShort();
          var entityId = _serializer.ReadUShort();
          var staticId = _serializer.ReadUShort();
          var ownerId = (uint) _serializer.ReadUShort();
          var stateAuthority = RagonAuthority.OWNER_ONLY;
          var eventAuthority = RagonAuthority.ALL;
          var payload = _serializer.ReadData(_serializer.Size);

          if (_room.Connections.TryGetValue(ownerId, out var owner))
            _objectManager.OnEntityStaticCreated(entityId, staticId, entityType, stateAuthority, eventAuthority, owner, payload.ToArray());
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
          
          // if (_serializer.Size > 0)
          // {
          //   var entityPayload = _serializer.ReadData(_serializer.Size);
          //   payload = entityPayload.ToArray();
          // }

          if (_room.Connections.TryGetValue(ownerId, out var owner))
            _objectManager.OnEntityCreated(entityId, entityType, stateAuthority, eventAuthority, owner, payload);
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

          _objectManager.OnEntityDestroyed(entityId, payload);
          break;
        }
        case RagonOperation.REPLICATE_ENTITY_STATE:
        {
          var entitiesCount = _serializer.ReadUShort();
          for (var i = 0; i < entitiesCount; i++)
          {
            var entityId = _serializer.ReadUShort();
            _objectManager.OnEntityState(entityId, _serializer);
          }
          break;
        }
        case RagonOperation.REPLICATE_EVENT:
        {
          var peerId = _serializer.ReadUShort();
          var executionMode = (RagonReplicationMode) _serializer.ReadByte();
          var eventCode = _serializer.ReadUShort();
  
          if (_room.Connections.TryGetValue(peerId, out var player)
              && executionMode == RagonReplicationMode.LOCAL_AND_SERVER
              && !player.IsMe)
          {
            foreach (var listener in _listeners)
              listener.OnEvent(player, eventCode, _serializer);
          }

          break;
        }
        case RagonOperation.REPLICATE_ENTITY_EVENT:
        {
          var eventCode = _serializer.ReadUShort();
          var peerId = _serializer.ReadUShort();
          var executionMode = (RagonReplicationMode) _serializer.ReadByte();
          var entityId = _serializer.ReadInt();
          
          if (!_room.Connections.TryGetValue(peerId, out var player))
            break;

          if (executionMode == RagonReplicationMode.LOCAL_AND_SERVER && player.IsMe)
            break;

          _objectManager.OnEntityEvent(player, entityId, eventCode, _serializer);
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
            var player = _room.Connections[ownerPeerId];

            _objectManager.OnEntityCreated(entityId, entityType, stateAuthority, eventAuthority, player, payloadData.ToArray());
            // _entityManager.OnEntityState(entityId, _serializer);
          }

          _objectManager.CollectSceneData();

          var staticEntities = _serializer.ReadUShort();
          Debug.Log("Static Entities: " + staticEntities);
          for (var i = 0; i < staticEntities; i++)
          {
            var entityType = _serializer.ReadUShort();
            var entityId = _serializer.ReadUShort();
            var staticId = _serializer.ReadUShort();
            var ownerPeerId = (uint) _serializer.ReadUShort();
            var payloadLenght = _serializer.ReadUShort();
            var payloadData = _serializer.ReadData(payloadLenght);
            var stateAuthority = RagonAuthority.OWNER_ONLY;
            var eventAuthority = RagonAuthority.ALL;
            
            var player = _room.Connections[ownerPeerId];

            _objectManager.OnEntityStaticCreated(entityId, staticId, entityType, stateAuthority, eventAuthority, player, payloadData.ToArray());
            // _objectManager.OnEntityState(entityId, _serializer);
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