namespace Ragon.Client
{
  public class RagonPlayer
  {
    private RagonRoom _ragonRoom;
    public string Id { get; private set; }
    public string Name { get; set; }
    public uint PeerId { get; set; }
    public bool IsOwner { get; set; }
    public bool IsLocal { get; set; }
    
    public RagonPlayer(uint peerId, string playerId, string name, bool isOwner, bool isLocal)
    {
      PeerId = peerId;
      IsOwner = isOwner;
      IsLocal = isLocal;
      Name = name;
      Id = playerId;
    } 
  }
}