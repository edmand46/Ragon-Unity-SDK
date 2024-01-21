using Ragon.Client.Compressor;
using Ragon.Protocol;
using UnityEngine;

namespace Ragon.Client.Unity
{
  public class RagonQuaternion : RagonProperty
  {
    [SerializeField] private Quaternion _value;

    public Quaternion Value
    {
      get => _value;
      set
      {
        _value = value;

        MarkAsChanged();
      }
    }

    private readonly FloatCompressor _compressor;

    public RagonQuaternion(Quaternion value, bool invokeLocal = false, int priority = 0) : base(priority, invokeLocal)
    {
      _value = value;
      _compressor = new FloatCompressor(-1.0f, 1f, 0.01f);
      
      SetFixedSize(_compressor.RequiredBits * 4);
    }

    public override void Serialize(RagonBuffer buffer)
    {
      var compressedX = _compressor.Compress(_value.x);
      var compressedY = _compressor.Compress(_value.y);
      var compressedZ = _compressor.Compress(_value.z);
      var compressedW = _compressor.Compress(_value.w);

      buffer.Write(compressedX, _compressor.RequiredBits);
      buffer.Write(compressedY, _compressor.RequiredBits);
      buffer.Write(compressedZ, _compressor.RequiredBits);
      buffer.Write(compressedW, _compressor.RequiredBits);
    }

    public override void Deserialize(RagonBuffer buffer)
    {
      var compressedX = buffer.Read(_compressor.RequiredBits);
      var compressedY = buffer.Read(_compressor.RequiredBits);
      var compressedZ = buffer.Read(_compressor.RequiredBits);
      var compressedW = buffer.Read(_compressor.RequiredBits);

      var x = _compressor.Decompress(compressedX);
      var y = _compressor.Decompress(compressedY);
      var z = _compressor.Decompress(compressedZ);
      var w = _compressor.Decompress(compressedW);
      
      _value = new Quaternion(x, y, z, w);
      
      InvokeChanged();
    }
  }
}