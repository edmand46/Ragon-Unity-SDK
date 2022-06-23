using System;
using System.Text;
using NetStack.Serialization;
using Ragon.Common;
using UnityEngine;

namespace Ragon.Client
{
  [DefaultExecutionOrder(-1500)]
  public class RagonNetwork : MonoBehaviour
  {
    private static RagonNetwork _instance;
    
    private RagonState _state;
    private RagonConnection _connection;
    private IRagonRoom _room;
    private IRoomInternal _roomInternal;
    private IRagonNetworkListener _eventListener;
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

    public static void SetListeners(IRagonNetworkListener events, IRagonEntityManager manager)
    {
      _instance._eventListener = events;
      _instance._entityManager = manager;
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
      var sendData = new[] { (byte)  RagonOperation.LEAVE_ROOM };
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
      _eventListener.OnDisconnected();
      _state = RagonState.DISCONNECTED;
    }

    private void OnConnected()
    {
      _eventListener.OnConnected();
      _state = RagonState.CONNECTED;
    }

    private void OnData(byte[] bytes)
    {
      if (_eventListener == null || _entityManager == null)
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
          _eventListener.OnAuthorized(playerId, playerName);
          break;
        }
        case RagonOperation.JOIN_SUCCESS:
        {
          var roomId = _serializer.ReadString();
          var localId = _serializer.ReadString();
          var ownerId = _serializer.ReadString();
          var min = _serializer.ReadUShort();
          var max = _serializer.ReadUShort();
          
          var room = new RagonRoom(_eventListener, _entityManager, _connection, roomId, ownerId, localId, min, max);
          
          _room = room;
          _roomInternal = room;
          break;
        }
        case RagonOperation.JOIN_FAILED:
        {
          _eventListener.OnFailed();
          break;
        }
        case RagonOperation.LEAVE_ROOM:
        {
          _eventListener.OnPlayerLeft(_room.LocalPlayer);
          _roomInternal.RemovePlayer(_room.LocalPlayer.Id);
          _room = null;
          break;
        }
        case RagonOperation.OWNERSHIP_CHANGED:
        {
          var newOwnerId = _serializer.ReadString();
          _roomInternal.OnOwnershipChanged(newOwnerId);
          var player = _room.PlayersMap[newOwnerId];
          _eventListener.OnOwnerShipChanged(player);
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
          _eventListener.OnPlayerJoined(player);
          break;
        }
        case RagonOperation.PLAYER_LEAVED:
        {
          var playerId = _serializer.ReadString();
          var player = _room.PlayersMap[playerId];
          
          _roomInternal.RemovePlayer(playerId);
          _eventListener.OnPlayerLeft(player);

          _buffer.Clear();
          var entities = _serializer.ReadUShort();
          for (var i = 0; i < entities; i++)
          {
            var entityId = _serializer.ReadInt();
            _entityManager.OnEntityDestroyed(entityId, _buffer);
          }
          break;
        }
        case RagonOperation.LOAD_SCENE:
        {
          var sceneName = _serializer.ReadString();
          _eventListener.OnLevel(sceneName);
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
          
          if (_serializer.Size > 0)
          {
            var entityPayload = _serializer.ReadData(_serializer.Size);
            _buffer.FromSpan(ref entityPayload, entityPayload.Length);
          }
          
          if (_room.Connections.TryGetValue(ownerId, out var owner))
            _entityManager.OnEntityCreated(entityId, entityType, stateAuthority, eventAuthority, owner, _buffer);
          else
            Debug.LogWarning($"Owner {ownerId} not found in players");
          break;
        }
        case RagonOperation.DESTROY_ENTITY:
        {
          _buffer.Clear();

          var entityId = _serializer.ReadInt();
          if (_serializer.Size > 0)
          {
            var entityPayload = _serializer.ReadData(_serializer.Size);
            _buffer.FromSpan(ref entityPayload, entityPayload.Length);
          }
          _entityManager.OnEntityDestroyed(entityId, _buffer);
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
          var eventCode = _serializer.ReadUShort();
          if (_serializer.Size > 0)
          {
            var payloadData = _serializer.ReadData(_serializer.Size);
            _buffer.FromSpan(ref payloadData, payloadData.Length);
          }
          _eventListener.OnEvent(eventCode, _buffer);
          break;
        }
        case RagonOperation.REPLICATE_ENTITY_EVENT:
        {
          _buffer.Clear();
          
          var eventCode = _serializer.ReadUShort();
          var entityId = _serializer.ReadInt();
          
          if (_serializer.Size > 0)
          {
            var eventPayload = _serializer.ReadData(_serializer.Size);
            _buffer.FromSpan(ref eventPayload, eventPayload.Length);
          }
          _entityManager.OnEntityEvent(entityId, eventCode, _buffer);
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
          
          var entitiesCount = _serializer.ReadInt();
          Debug.Log("Entities: " + entitiesCount);
          for (var i = 0; i < entitiesCount; i++)
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
            
            _buffer.Clear();
            if (payloadLenght > 0)
            {
              _buffer.FromSpan(ref payloadData, payloadData.Length);
            }

            _entityManager.OnEntityCreated(entityId, entityType, stateAuthority, eventAuthority, player, _buffer);

            if (stateLenght > 0)
            {
              _buffer.Clear();
              _buffer.FromSpan(ref stateData, stateData.Length);

              _entityManager.OnEntityState(entityId, _buffer);
            }
          }
          
          _eventListener.OnJoined();
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