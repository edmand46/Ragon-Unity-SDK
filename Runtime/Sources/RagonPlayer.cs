using System;

namespace Ragon.Client
{
  [Serializable]
  public class RagonPlayer
  {
    public string Id { get; private set; }
    public string Name { get; set; }
    public uint PeerId { get; set; }
    public bool IsRoomOwner { get; set; }
    public bool IsMe { get; set; }
    
    public RagonPlayer(uint peerId, string playerId, string name, bool isRoomOwner, bool isMe)
    {
      PeerId = peerId;
      IsRoomOwner = isRoomOwner;
      IsMe = isMe;
      Name = name;
      Id = playerId;
    } 
  }
}