using NetStack.Quantization;
using NetStack.Serialization;
using Ragon.Client;
using Ragon.Common;
using UnityEngine;

namespace Example.Game
{
  
  public class CharacterState : IRagonSerializable
  {
    private static BoundedRange _compressor = new BoundedRange(100.0f, 100.0f, 0.01f)
    
    public Vector3 Position;
    public void Serialize(BitBuffer buffer)
    {
      buffer.AddUInt(_compressor.Quantize(Position.x));
      buffer.AddUInt(_compressor.Quantize(Position.y));
      buffer.AddUInt(_compressor.Quantize(Position.z));
    }

    public void Deserialize(BitBuffer buffer)
    {
      Position = new Vector3();

      Position.x = _compressor.Dequantize(buffer.ReadUInt());
      Position.y = _compressor.Dequantize(buffer.ReadUInt());
      Position.z = _compressor.Dequantize(buffer.ReadUInt());
    }
  }
}