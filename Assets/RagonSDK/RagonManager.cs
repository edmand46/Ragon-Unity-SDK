using System;
using System.Linq;
using System.Text;
using NetStack.Serialization;
using Ragon.Common.Protocol;
using Ragon.Core;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using YohohoArena.Game;

namespace RagonSDK
{
  [DefaultExecutionOrder(-9999)]
  public class RagonManager : MonoBehaviour
  {
    public static RagonManager Instance { get; private set; }
    public static RagonConnection Connection => Instance._connection;
    public static int RoomOwner => Instance._roomOwner;
    public static int Id => Instance._id;

    private RagonConnection _connection;
    private int _roomOwner = -1;
    private int _id = -1;
    private IRagonHandler _handler;
    private BitBuffer _buffer = new BitBuffer(1024);
    private byte[] _bytes = new byte[1024];

    public void SetHandler(IRagonHandler handler)
    {
      _handler = handler;
    }

    public void Connect(string url, ushort port)
    {
      _connection.Connect(url, port);
    }

    private void Awake()
    {
      Instance = this;

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

      Authorize(Array.Empty<byte>());
    }

    public void Authorize(byte[] payloadRaw)
    {
      Span<byte> payload = payloadRaw.AsSpan();
      Span<byte> data = stackalloc byte[payload.Length + 2];
      Span<byte> payloadData = data.Slice(2, payload.Length);
      
      RagonHeader.WriteUShort((ushort) RagonOperation.AUTHORIZE, ref data);
      
      payload.CopyTo(payloadData);
      
      _connection.SendData(data.ToArray());
    }

    public void FindOrJoin()
    {
      var data = Encoding.UTF8.GetBytes(SceneManager.GetActiveScene().name);
      Span<byte> rawData = stackalloc byte[data.Length + 2];
      Span<byte> operationData = rawData.Slice(0, 2);
      Span<byte> sceneData = rawData.Slice(2, data.Length);

      data.AsSpan().CopyTo(sceneData);

      RagonHeader.WriteUShort((ushort) RagonOperation.JOIN_ROOM, ref operationData);

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
      // Debug.Log("Operation: " + operation);

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

          var myId = RagonHeader.ReadInt(ref myIdData);
          var roomOwner = RagonHeader.ReadInt(ref roomOwnerData);

          _id = myId;
          _roomOwner = roomOwner;
          _handler.OnJoined(_buffer);
          break;
        }
        case RagonOperation.LOAD_SCENE:
        {
          // var data = new byte[rawData.Length - 2];
          // Array.Copy(rawData, 2, data, 0, rawData.Length - 2);
          // var sceneName = Encoding.UTF8.GetString(data);
          // _handler.OnLevel(sceneName);
          break;
        }
        case RagonOperation.CREATE_ENTITY:
        {
          var entityData = rawData.Slice(2, 4);
          var ownerData = rawData.Slice(6, 4);
          var entityPayload = rawData.Slice(10, rawData.Length - 10);

          _buffer.Clear();
          _buffer.FromSpan(ref entityPayload, entityPayload.Length);

          var entityId = RagonHeader.ReadInt(ref entityData);
          var ownerId = RagonHeader.ReadInt(ref ownerData);

          Debug.Log("EntityId: " + entityId + " OwnerId " + ownerId);
          _handler.OnEntityCreated(entityId, ownerId, _buffer);
          break;
        }
        case RagonOperation.DESTROY_ENTITY:
        {
          var entityData = rawData.Slice(2, 4);
          var entityId = RagonHeader.ReadInt(ref entityData);
          _buffer.Clear();
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
          var eventCodeData = rawData.Slice(2, 2);
          var entityIdData = rawData.Slice(4, 4);
          var eventPayload = rawData.Slice(8, rawData.Length - 8);

          var eventCode = RagonHeader.ReadUShort(ref eventCodeData);
          var entityId = RagonHeader.ReadInt(ref entityIdData);

          _buffer.Clear();
          _buffer.FromSpan(ref eventPayload, eventPayload.Length);

          _handler.OnEntityEvent(entityId, eventCode, _buffer);
          break;
        }
        case RagonOperation.RESTORE_END:
        {
          Send(RagonOperation.RESTORED);
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

    public void CreateEntity(IPacket payload)
    {
      Span<byte> data = stackalloc byte[2];

      RagonHeader.WriteUShort((ushort) RagonOperation.CREATE_ENTITY, ref data);

      _connection.SendData(data.ToArray());
    }

    public void DestroyEntity(int entityId, IPacket payload)
    {
      Span<byte> data = stackalloc byte[6]; // 2 + 4
      Span<byte> operationData = data.Slice(0, 2);
      Span<byte> entityData = data.Slice(2, 4);

      RagonHeader.WriteUShort((ushort) RagonOperation.DESTROY_ENTITY, ref operationData);
      RagonHeader.WriteInt(entityId, ref entityData);

      _connection.SendData(data.ToArray());
    }

    public void SendEntityEvent(ushort evntCode, int entityId)
    {
      Span<byte> rawData = stackalloc byte[8];
      var operationData = rawData.Slice(0, 2);
      var eventCodeData = rawData.Slice(2, 4);
      var entityData = rawData.Slice(6, 4);

      RagonHeader.WriteUShort((ushort) RagonOperation.REPLICATE_ENTITY_EVENT, ref operationData);
      RagonHeader.WriteUShort(evntCode, ref eventCodeData);
      RagonHeader.WriteInt(evntCode, ref entityData);

      _connection.SendData(rawData.ToArray());
    }

    public void SendEntityEvent(ushort evntCode, int entityId, IPacket data)
    {
      _buffer.Clear();
      data.Serialize(_buffer);

      Span<byte> rawData = stackalloc byte[_buffer.Length + 8];
      var operationData = rawData.Slice(0, 2);
      var eventCodeData = rawData.Slice(2, 2);
      var entityData = rawData.Slice(4, 4);
      var eventPayload = rawData.Slice(8, _buffer.Length);

      RagonHeader.WriteUShort((ushort) RagonOperation.REPLICATE_ENTITY_EVENT, ref operationData);
      RagonHeader.WriteUShort(evntCode, ref eventCodeData);
      RagonHeader.WriteInt(entityId, ref entityData);

      _buffer.ToSpan(ref eventPayload);

      _connection.SendData(rawData.ToArray());
    }

    public void SendEvent(ushort evntCode, IPacket data)
    {
      _buffer.Clear();
      data.Serialize(_buffer);

      Span<byte> rawData = stackalloc byte[_buffer.Length + 4];
      var operationData = rawData.Slice(0, 2);
      var eventCodeData = rawData.Slice(2, 2);
      var eventData = rawData.Slice(4, _buffer.Length);

      RagonHeader.WriteUShort((ushort) RagonOperation.REPLICATE_EVENT, ref operationData);
      RagonHeader.WriteUShort(evntCode, ref eventCodeData);
      _buffer.ToSpan(ref eventData);

      _connection.SendData(rawData.ToArray());
    }

    public void SendEvent(ushort evntCode)
    {
      Span<byte> rawData = stackalloc byte[_buffer.Length + 4];
      var operationData = rawData.Slice(0, 2);
      var eventCodeData = rawData.Slice(2, 2);

      RagonHeader.WriteUShort((ushort) RagonOperation.REPLICATE_EVENT, ref operationData);
      RagonHeader.WriteUShort(evntCode, ref eventCodeData);

      _connection.SendData(rawData.ToArray());
    }

    public void SendEntityState(int entityId, IPacket data)
    {
      _buffer.Clear();
      data.Serialize(_buffer);

      Span<byte> rawData = stackalloc byte[_buffer.Length + 6];
      var operationData = rawData.Slice(0, 2);
      var entityIdData = rawData.Slice(2, 4);
      var entityData = rawData.Slice(6, _buffer.Length);

      RagonHeader.WriteUShort((ushort) RagonOperation.REPLICATE_ENTITY_STATE, ref operationData);
      RagonHeader.WriteInt(entityId, ref entityIdData);
      _buffer.ToSpan(ref entityData);

      _connection.SendData(rawData.ToArray());
    }

    public void Send(RagonOperation operation, IPacket data)
    {
      _buffer.Clear();
      data.Serialize(_buffer);

      Span<byte> rawData = stackalloc byte[_buffer.Length + 2];
      var operationData = rawData.Slice(0, 2);
      var packetData = rawData.Slice(2, _buffer.Length);

      _buffer.ToSpan(ref packetData);
      RagonHeader.WriteUShort((ushort) operation, ref operationData);

      _connection.SendData(rawData.ToArray());
    }

    public void Send(RagonOperation operation)
    {
      Span<byte> rawData = stackalloc byte[2];

      RagonHeader.WriteUShort((ushort) operation, ref rawData);

      _connection.SendData(rawData.ToArray());
    }
  }
}