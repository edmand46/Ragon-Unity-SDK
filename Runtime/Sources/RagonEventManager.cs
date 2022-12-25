using System.Collections.Generic;
using Ragon.Common;

namespace Ragon.Client
{
  public class RagonEventManager
  {
    public int Count => _listeners.Count;
    private List<IRagonListener> _listeners = new();

    public void Add(IRagonListener listener)
    {
      _listeners.Add(listener);
    }

    public void Remove(IRagonListener listener)
    {
      _listeners.Remove(listener);
    }
    
    public void OnAuthorized(string playerId, string playerName)
    {
      foreach (var listener in _listeners)
        listener.OnAuthorized(playerId, playerName);
    }

    public void OnLeaved()
    {
      foreach (var listener in _listeners)
        listener.OnLeaved();
    }

    public void OnFailed(string message)
    {
      foreach (var listener in _listeners)
        listener.OnFailed(message);
    }

    public void OnOwnershipChanged(RagonPlayer player)
    {
      foreach (var listener in _listeners)
        listener.OnOwnershipChanged(player);
    }

    public void OnPlayerLeft(RagonPlayer player)
    {
      foreach (var listener in _listeners)
        listener.OnPlayerLeft(player);
    }

    public void OnPlayerJoined(RagonPlayer player)
    {
      foreach (var listener in _listeners)
        listener.OnPlayerJoined(player);
    }

    public void OnLevel(string sceneName)
    {
      foreach (var listener in _listeners)
        listener.OnLevel(sceneName);
    }

    public void OnJoined()
    {
      foreach (var listener in _listeners)
        listener.OnJoined();
    }

    public void OnConnected()
    {
      foreach (var listener in _listeners)
        listener.OnConnected();
    }

    public void OnDisconnected()
    {
      foreach (var listener in _listeners)
        listener.OnDisconnected();
    }
  }
}