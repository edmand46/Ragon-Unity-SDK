using UnityEngine;

namespace Ragon.Client
{
  public class RagonUnityLog: IRagonLog
  {
    public void Warn(string message)
    {
      Debug.LogWarning($"[Ragon] {message}" );
    }
    
    public void Trace(string message)
    {
      Debug.Log($"[Ragon] {message}");
    }
    
    public void Info(string message)
    {
      Debug.Log($"[Ragon] {message}" );
    }
    
    public void Error(string message)
    {
      Debug.LogError($"[Ragon] {message}");
    }
  }
}