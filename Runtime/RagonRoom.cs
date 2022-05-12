using System;
using NetStack.Serialization;
using Ragon.Common;

namespace Ragon.Client
{
  public class RagonRoom
  {
    private RagonConnection _connection;
    private BitBuffer _buffer = new BitBuffer(8192);

    public int RoomOwner { get; private set; }
    public int MyId { get; private set; }
    public string Id { get; private set; }
    public int MinPlayers { get; private set; }
    public int MaxPlayers { get; private set; }

    public RagonRoom(RagonConnection connection, string id, int roomOwner, int myMyId, int min, int max)
    {
      _connection = connection;

      RoomOwner = roomOwner;
      MyId = myMyId;
      Id = id;
      MinPlayers = min;
      MaxPlayers = max;
    }

    public void CreateEntity(ushort entityType, IRagonSerializable spawnPayload)
    {
      _buffer.Clear();
      spawnPayload.Serialize(_buffer);

      var sendData = new byte[_buffer.Length + 4];
      var data = sendData.AsSpan();
      var operationData = data.Slice(0, 2);
      var entityTypeData = data.Slice(2, 2);
      
      RagonHeader.WriteUShort((ushort) RagonOperation.CREATE_ENTITY, ref operationData);
      RagonHeader.WriteUShort(entityType, ref entityTypeData);

      if (_buffer.Length > 0)
      {
        Span<byte> payloadData = data.Slice(4, _buffer.Length);
        _buffer.ToSpan(ref payloadData);
      }

      _connection.SendData(sendData);
    }

    public void DestroyEntity(int entityId, IRagonSerializable destroyPayload)
    {
      _buffer.Clear();
      destroyPayload.Serialize(_buffer);

      var sendData = new byte[_buffer.Length + 6];
      var data = sendData.AsSpan();
      var operationData = data.Slice(0, 2);
      var entityData = data.Slice(2, 4);

      RagonHeader.WriteUShort((ushort) RagonOperation.DESTROY_ENTITY, ref operationData);
      RagonHeader.WriteInt(entityId, ref entityData);

      if (_buffer.Length > 0)
      {
        Span<byte> payloadData = data.Slice(6, _buffer.Length);
        _buffer.ToSpan(ref payloadData);
      }

      _connection.SendData(sendData);
    }

    public void SendEntityEvent(ushort evntCode, int entityId)
    {
      var sendData = new byte[8];

      var data = sendData.AsSpan();
      var operationData = data.Slice(0, 2);
      var eventCodeData = data.Slice(2, 2);
      var entityData = data.Slice(4, 4);

      RagonHeader.WriteUShort((ushort) RagonOperation.REPLICATE_ENTITY_EVENT, ref operationData);
      RagonHeader.WriteUShort(evntCode, ref eventCodeData);
      RagonHeader.WriteInt(entityId, ref entityData);

      _connection.SendData(sendData);
    }

    public void SendEntityEvent(ushort evntCode, int entityId, IRagonSerializable payload)
    {
      _buffer.Clear();
      payload.Serialize(_buffer);

      var sendData = new byte[_buffer.Length + 8];
      var data = sendData.AsSpan();
      var operationData = data.Slice(0, 2);
      var eventCodeData = data.Slice(2, 2);
      var entityData = data.Slice(4, 4);
      var eventPayload = data.Slice(8, _buffer.Length);

      RagonHeader.WriteUShort((ushort) RagonOperation.REPLICATE_ENTITY_EVENT, ref operationData);
      RagonHeader.WriteUShort(evntCode, ref eventCodeData);
      RagonHeader.WriteInt(entityId, ref entityData);

      _buffer.ToSpan(ref eventPayload);

      _connection.SendData(sendData);
    }

    public void SendEvent(ushort evntCode, IRagonSerializable payload)
    {
      _buffer.Clear();
      payload.Serialize(_buffer);

      var sendData = new byte[_buffer.Length + 4];
      var data = sendData.AsSpan();
      var operationData = data.Slice(0, 2);
      var eventCodeData = data.Slice(2, 2);
      var eventData = data.Slice(4, _buffer.Length);

      RagonHeader.WriteUShort((ushort) RagonOperation.REPLICATE_EVENT, ref operationData);
      RagonHeader.WriteUShort(evntCode, ref eventCodeData);

      _buffer.ToSpan(ref eventData);

      _connection.SendData(sendData);
    }

    public void SendEvent(ushort evntCode)
    {
      var sendData = new byte[_buffer.Length + 4];
      var data = sendData.AsSpan();
      var operationData = data.Slice(0, 2);
      var eventCodeData = data.Slice(2, 2);

      RagonHeader.WriteUShort((ushort) RagonOperation.REPLICATE_EVENT, ref operationData);
      RagonHeader.WriteUShort(evntCode, ref eventCodeData);

      _connection.SendData(sendData);
    }

    public void SendEntityState(int entityId, IRagonSerializable payload)
    {
      _buffer.Clear();
      payload.Serialize(_buffer);

      var sendData = new byte[_buffer.Length + 6];
      var data = sendData.AsSpan();
      var operationData = data.Slice(0, 2);
      var entityIdData = data.Slice(2, 4);
      var entityData = data.Slice(6, _buffer.Length);

      RagonHeader.WriteUShort((ushort) RagonOperation.REPLICATE_ENTITY_STATE, ref operationData);
      RagonHeader.WriteInt(entityId, ref entityIdData);

      _buffer.ToSpan(ref entityData);

      _connection.SendData(sendData);
    }
    
    public bool OwnerIsMe() => RoomOwner == MyId;
  }
}