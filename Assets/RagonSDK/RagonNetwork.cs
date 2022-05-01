using System;
using System.Text;
using NetStack.Serialization;
using Ragon.Common.Protocol;
using Ragon.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ragon.Client
{
  [DefaultExecutionOrder(-9999)]
  public class RagonNetwork : MonoBehaviour
  {
    private static RagonNetwork _instance;
    public static RagonRoom Room => _instance._room;
    public static void SetHandler(IRagonHandler handler) => _instance._handler = handler;
    public static void ConnectToServer(string url, ushort port) => _instance._connection.Connect(url, port);
    public static void AuthorizeWithData(byte[] data) => _instance.Authorize(data);
    public static void FindRoomAndJoin(string map, int minPlayers, int maxPlayers) => _instance.FindOrJoin(map, minPlayers, maxPlayers);

    private RagonRoom _room;
    private RagonConnection _connection;
    private IRagonHandler _handler;
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
      _handler.OnDisconnected();
    }

    private void OnConnected()
    {
      _handler.OnConnected();
    }

    private void Authorize(byte[] payloadRaw)
    {
      Span<byte> payload = payloadRaw.AsSpan();
      Span<byte> data = stackalloc byte[payload.Length + 2];
      Span<byte> payloadData = data.Slice(2, payload.Length);

      RagonHeader.WriteUShort((ushort) RagonOperation.AUTHORIZE, ref data);

      payload.CopyTo(payloadData);

      _connection.SendData(data.ToArray());
    }

    private void FindOrJoin(string map, int min, int max)
    {
      var data = Encoding.UTF8.GetBytes(map);
      Span<byte> rawData = stackalloc byte[data.Length + 6];
      Span<byte> operationData = rawData.Slice(0, 2);
      Span<byte> minData = rawData.Slice(2, 2);
      Span<byte> maxData = rawData.Slice(4, 2);
      Span<byte> sceneData = rawData.Slice(6, data.Length);
      
      data.AsSpan().CopyTo(sceneData);

      RagonHeader.WriteUShort((ushort) RagonOperation.JOIN_ROOM, ref operationData);
      RagonHeader.WriteUShort((ushort) min, ref minData);
      RagonHeader.WriteUShort((ushort) max, ref maxData);

      _connection.SendData(rawData.ToArray());
    }

    private void OnData(byte[] bytes)
    {
      if (_handler == null)
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
          _handler.OnAuthorized(_buffer);
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
          
          _room = new RagonRoom(_connection, id, roomOwner, myId, min, max);

          Span<byte> operationData = stackalloc byte[2];
          RagonHeader.WriteUShort((ushort) RagonOperation.SCENE_IS_LOADED, ref operationData);
          
          _connection.SendData(operationData.ToArray());
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
          // var sceneData = rawData.Slice(2, rawData.Length - 2);
          var sceneName = Encoding.UTF8.GetString(sceneData);
          _handler.OnLevel(sceneName);
          break;
        }
        case RagonOperation.CREATE_ENTITY:
        {
          _buffer.Clear();
          
          var entityData = rawData.Slice(2, 4);
          var ownerData = rawData.Slice(6, 4);
          
          if (rawData.Length - 10 > 0)
          {
            var entityPayload = rawData.Slice(10, rawData.Length - 10);
            _buffer.FromSpan(ref entityPayload, entityPayload.Length);
          }
          
          var entityId = RagonHeader.ReadInt(ref entityData);
          var ownerId = RagonHeader.ReadInt(ref ownerData);

          _handler.OnEntityCreated(entityId, ownerId, _buffer);
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

          _handler.OnEntityDestroyed(entityId, _buffer);
          break;
        }
        case RagonOperation.REPLICATE_ENTITY_STATE:
        {
          var entityData = rawData.Slice(2, 4);
          var entityId = RagonHeader.ReadInt(ref entityData);
          var entityStateData = rawData.Slice(6, rawData.Length - 6);

          _buffer.Clear();
          _buffer.FromSpan(ref entityStateData, entityStateData.Length);

          _handler.OnEntityState(entityId, _buffer);
          break;
        }
        case RagonOperation.REPLICATE_ENTITY_PROPERTY:
        {
          var entityData = rawData.Slice(2, 4);
          var propertyData = rawData.Slice(6, 4);
          var entityId = RagonHeader.ReadInt(ref entityData);
          var property = RagonHeader.ReadInt(ref propertyData);

          _buffer.Clear();
          _handler.OnEntityProperty(entityId, property, _buffer);
          break;
        }
        case RagonOperation.REPLICATE_EVENT:
        {
          var eventData = rawData.Slice(2, 2);
          var eventPayload = rawData.Slice(2, rawData.Length - 2);
          var eventCode = RagonHeader.ReadUShort(ref eventData);

          _buffer.Clear();
          _buffer.FromSpan(ref eventPayload, eventPayload.Length);

          _handler.OnEvent(eventCode, _buffer);
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

          _handler.OnEntityEvent(entityId, eventCode, _buffer);
          break;
        }
        case RagonOperation.RESTORE_END:
        {
          Span<byte> data = stackalloc byte[2];
          RagonHeader.WriteUShort((ushort) RagonOperation.RESTORED, ref data);

          _connection.SendData(data.ToArray());
          _handler.OnReady();
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