/*
 * Copyright 2023-2024 Eduard Kargin <kargin.eduard@gmail.com>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
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