namespace Ragon.Client
{
  public interface IRagonLog
  {
    public void Warn(string message);
    public void Trace(string message);
    public void Info(string message);
    public void Error(string message);
  }
}