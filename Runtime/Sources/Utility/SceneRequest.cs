using System;
using Ragon.Client;
using UnityEngine.SceneManagement;

namespace Ragon.Client.Utility
{
  public class SceneRequest : IRagonSceneRequestListener, IDisposable
  {
    private string _scene;
    private readonly RagonRoom _room;

    public SceneRequest(RagonRoom room)
    {
      _room = room;

      SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public void Dispose()
    {
      SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void OnRequestScene(RagonClient client, string sceneName)
    {
      _scene = sceneName;
      var activeScene = SceneManager.GetActiveScene();
      if (activeScene.name == _scene)
      {
        _room.SceneLoaded();
        return;
      }
      
      SceneManager.LoadSceneAsync(sceneName);
    }

    private void OnSceneLoaded(Scene sceneLoaded, LoadSceneMode mode)
    {
      if (sceneLoaded.name == _scene)
        _room.SceneLoaded();
    }
  }
}