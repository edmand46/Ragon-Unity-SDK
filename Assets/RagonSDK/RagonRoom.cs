using System;
using NetStack.Serialization;
using Ragon.Common.Protocol;
using Ragon.Core;

namespace Ragon.Client
{
  public class RagonRoom
  {
    private RagonConnection _connection;
    private BitBuffer _buffer = new BitBuffer(8192);
    
    public int RoomOwner { get; private set; }
    public int MyId { get; private set; }
    public string Id { get; private set; }
    
    public RagonRoom(RagonConnection connection, int roomOwner, int myMyId)
    {
      _connection = connection;
      
      RoomOwner = roomOwner;
      MyId = myMyId;
      Id = "";
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
  }
}