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
    public static RagonRoom Room => _instance._room;
    public static RagonServerState State => _instance._state;
    
    public static void SetManager(IRagonManager manager) => _instance._manager = manager;
    public static void AuthorizeWithData(byte[] data) => _instance.Authorize(data);
    public static void FindRoomAndJoin(string map, int minPlayers, int maxPlayers) => _instance.FindOrJoin(map, minPlayers, maxPlayers);
    
    public static void ConnectToServer(string url, ushort port)
    {
      _instance._connection.Connect(url, port);
    }

    public static void Disconnect()
    {
      _instance.OnDisconnected();
      _instance._connection.Dispose();
    }

    private RagonServerState _state;
    private RagonRoom _room;
    private RagonConnection _connection;
    private IRagonManager _manager;
    private BitBuffer _buffer = new BitBuffer(8192);

    private void Awake()
    {
      _instance = this;

      _connection = new RagonConnection();
      _connection.OnData += OnData;
      _connection.OnConnected += OnConnected;
      _connection.OnDisconnected += OnDisconnected;
      _connection.Prepare();
    }

    private void OnDisconnected()
    {
      _manager.OnDisconnected();
      _state = RagonServerState.DISCONNECTED;
    }

    private void OnConnected()
    {
      _manager.OnConnected();
      _state = RagonServerState.CONNECTED;
    }

    private void Authorize(byte[] payloadRaw)
    {
      Span<byte> payload = payloadRaw.AsSpan();

      var sendData = new byte[payload.Length + 2];
      Span<byte> data = sendData.AsSpan();
      Span<byte> payloadData = data.Slice(2, payload.Length);

      RagonHeader.WriteUShort((ushort) RagonOperation.AUTHORIZE, ref data);

      payload.CopyTo(payloadData);

      _connection.SendData(sendData);
    }

    private void FindOrJoin(string map, int min, int max)
    {
      var data = Encoding.UTF8.GetBytes(map);
      var sendData = new byte[data.Length + 6];
      
      Span<byte> rawData = sendData.AsSpan();
      var operationData = rawData.Slice(0, 2);
      var minData = rawData.Slice(2, 2);
      var maxData = rawData.Slice(4, 2);
      var sceneData = rawData.Slice(6, data.Length);

      data.AsSpan().CopyTo(sceneData);

      RagonHeader.WriteUShort((ushort) RagonOperation.JOIN_ROOM, ref operationData);
      RagonHeader.WriteUShort((ushort) min, ref minData);
      RagonHeader.WriteUShort((ushort) max, ref maxData);

      _connection.SendData(sendData);
    }

    private void OnData(byte[] bytes)
    {
      if (_manager == null)
      {
        Debug.LogWarning("Handler is null");
        return;
      }

      ReadOnlySpan<byte> rawData = bytes.AsSpan();
      var operation = (RagonOperation) RagonHeader.ReadUShort(ref rawData);

      switch (operation)
      {
        case RagonOperation.AUTHORIZED_SUCCESS:
        {
          _manager.OnAuthorized(_buffer);
          break;
        }
        case RagonOperation.JOIN_ROOM:
        {
          var myIdData = rawData.Slice(2, 4);
          var roomOwnerData = rawData.Slice(6, 4);
          var minData = rawData.Slice(10, 4);
          var maxData = rawData.Slice(14, 4);
          var idData = rawData.Slice(18, rawData.Length - 18);

          var myId = RagonHeader.ReadInt(ref myIdData);
          var roomOwner = RagonHeader.ReadInt(ref roomOwnerData);
          var min = RagonHeader.ReadInt(ref minData);
          var max = RagonHeader.ReadInt(ref maxData);
          var id = Encoding.UTF8.GetString(idData);

          _room = new RagonRoom(_manager, _connection, id, roomOwner, myId, min, max);

          var sendData = new byte[2];
          
          Span<byte> operationData = sendData.AsSpan();
          RagonHeader.WriteUShort((ushort) RagonOperation.SCENE_IS_LOADED, ref operationData);

          _connection.SendData(sendData);
          break;
        }
        case RagonOperation.LEAVE_ROOM:
        {
          _room = null;
          break;
        }
        case RagonOperation.LOAD_SCENE:
        {
          var sceneData = rawData.Slice(2, rawData.Length - 2);
          var sceneName = Encoding.UTF8.GetString(sceneData);
          _manager.OnLevel(sceneName);
          break;
        }
        case RagonOperation.CREATE_ENTITY:
        {
          _buffer.Clear();

          var entityTypeData = rawData.Slice(2, 2);
          var entityIdData = rawData.Slice(4, 4);
          var ownerData = rawData.Slice(8, 4);

          if (rawData.Length - 12 > 0)
          {
            var entityPayload = rawData.Slice(12, rawData.Length - 12);
            _buffer.FromSpan(ref entityPayload, entityPayload.Length);
          }

          var entityType = RagonHeader.ReadUShort(ref entityTypeData);
          var entityId = RagonHeader.ReadInt(ref entityIdData);
          var ownerId = RagonHeader.ReadUShort(ref ownerData);

          _manager.OnEntityCreated(entityId, entityType, ownerId, _buffer);
          break;
        }
        case RagonOperation.DESTROY_ENTITY:
        {
          _buffer.Clear();
          var entityIdData = rawData.Slice(2, 4);
          var entityId = RagonHeader.ReadInt(ref entityIdData);

          if (rawData.Length - 10 > 0)
          {
            var entityPayload = rawData.Slice(6, rawData.Length - 6);
            _buffer.FromSpan(ref entityPayload, entityPayload.Length);
          }

          _manager.OnEntityDestroyed(entityId, _buffer);
          break;
        }
        case RagonOperation.REPLICATE_ENTITY_STATE:
        {
          var entityIdData = rawData.Slice(2, 4);
          var entityStateData = rawData.Slice(6, rawData.Length - 6);

          var entityId = RagonHeader.ReadInt(ref entityIdData);

          _buffer.Clear();
          _buffer.FromSpan(ref entityStateData, entityStateData.Length);

          _manager.OnEntityState(entityId, _buffer);
          break;
        }
        case RagonOperation.REPLICATE_EVENT:
        {
          _buffer.Clear();

          var entityCodeData = rawData.Slice(2, 2);
          var eventCode = RagonHeader.ReadUShort(ref entityCodeData);

          if (rawData.Length - 4 > 0)
          {
            var payloadData = rawData.Slice(4, rawData.Length - 4);
            _buffer.FromSpan(ref payloadData, payloadData.Length);
          }

          _manager.OnEvent(eventCode, _buffer);
          break;
        }
        case RagonOperation.REPLICATE_ENTITY_EVENT:
        {
          _buffer.Clear();

          var eventCodeData = rawData.Slice(2, 2);
          var entityIdData = rawData.Slice(4, 4);

          var eventCode = RagonHeader.ReadUShort(ref eventCodeData);
          var entityId = RagonHeader.ReadInt(ref entityIdData);

          if (rawData.Length - 8 > 0)
          {
            var eventPayload = rawData.Slice(8, rawData.Length - 8);
            _buffer.FromSpan(ref eventPayload, eventPayload.Length);
          }

          _manager.OnEntityEvent(entityId, eventCode, _buffer);
          break;
        }
        case RagonOperation.RESTORE_END:
        {
          var sendData = new byte[2];
          var data = sendData.AsSpan();
          RagonHeader.WriteUShort((ushort) RagonOperation.RESTORED, ref data);

          _connection.SendData(data.ToArray());

          _manager.OnReady();
          break;
        }
      }
    }

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