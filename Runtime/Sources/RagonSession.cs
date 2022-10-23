using System;
using Ragon.Common;

namespace Ragon.Client
{
  public class RagonSession
  {
    private IRagonConnection _connection;
    private RagonSerializer _serializer = new RagonSerializer();
    
    public RagonSession(IRagonConnection connection)
    {
      _connection = connection;
    }

    public void CreateOrJoin(string map, int minPlayers, int maxPlayers)
    {
      var parameters = new RagonRoomParameters() {Map = map, Min = minPlayers, Max = maxPlayers};
      CreateOrJoin(parameters);
    }
    
    public void CreateOrJoin(RagonRoomParameters parameters)
    {
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.JOIN_OR_CREATE_ROOM);

      parameters.Serialize(_serializer);

      var sendData = _serializer.ToArray();
      _connection.Send(sendData);
    }

    public void Create(string map, int minPlayers, int maxPlayers)
    {
      Create(null, new RagonRoomParameters() {Map = map, Min = minPlayers, Max = maxPlayers});
    }

    public void Create(string roomId, string map, int minPlayers, int maxPlayers)
    {
      Create(roomId, new RagonRoomParameters() {Map = map, Min = minPlayers, Max = maxPlayers});
    }
    
    public  void Create(string roomId, RagonRoomParameters parameters)
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
      _connection.Send(sendData);
    }
    
    public  void Leave()
    {
      var sendData = new[] {(byte) RagonOperation.LEAVE_ROOM};
      _connection.Send(sendData, DeliveryType.Reliable);
    }

    public  void Join(string roomId)
    {
      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.JOIN_ROOM);
      _serializer.WriteString(roomId);

      var sendData = _serializer.ToArray();
      _connection.Send(sendData, DeliveryType.Reliable);
    }

    public  void AuthorizeWithKey(string key, string playerName, byte[] additonalData)
    {
      ReadOnlySpan<byte> payload = additonalData.AsSpan();

      _serializer.Clear();
      _serializer.WriteOperation(RagonOperation.AUTHORIZE);
      _serializer.WriteString(key);
      _serializer.WriteString(playerName);
      _serializer.WriteData(ref payload);

      var sendData = _serializer.ToArray();
      _connection.Send(sendData);
    }
    
  }
}