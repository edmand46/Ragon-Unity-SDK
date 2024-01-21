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
using Ragon.Client.Compressor;
using Ragon.Protocol;
using UnityEngine;

namespace Ragon.Client.Unity
{
  [Serializable]
  public class RagonInt : RagonProperty
  {
    [SerializeField] private int _value;
    
    public int Value
    {
      get => _value;
      set
      {
        _value = value;
        
        if (_value < _min)
          _value = _min;
        else if (_value > _max)
          _value = _max;
        
        MarkAsChanged();
      }
    }
    
    private int _min;
    private int _max;
    private IntCompressor _compressor;
    
    public RagonInt(
      int value,
      int min = -1000,
      int max = 1000,
      bool invokeLocal = false,
      int priority = 0
    ) : base(priority, invokeLocal)
    {
      _min = min;
      _max = max;
      _compressor = new IntCompressor(_min, _max);

      SetFixedSize(_compressor.RequiredBits);
    }

    public override void Serialize(RagonBuffer buffer)
    {
      uint compressedValue = _compressor.Compress(_value);
      buffer.Write(compressedValue, _compressor.RequiredBits);
    }

    public override void Deserialize(RagonBuffer buffer)
    {
      var compressedValue = buffer.Read(_compressor.RequiredBits);
      _value = _compressor.Decompress(compressedValue);
 
      InvokeChanged();
    }
  }
}