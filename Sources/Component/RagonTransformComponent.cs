/*
 * Copyright 2023 Eduard Kargin <kargin.eduard@gmail.com>
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

using System.Collections.Generic;
using Fusumity.Attributes.Specific;
using Ragon.Client.Compressor;
using UnityEngine;

namespace Ragon.Client.Unity
{
  [RequireComponent(typeof(RagonLink))]
  public class RagonTransformComponent : RagonBehaviour
  {
    [SerializeField] private Transform target;

    [SerializeField] private bool positionReplication;

    [SerializeField, ShowIf("positionReplication")]
    private RagonAxis positionAxis = RagonAxis.XYZ;

    [SerializeField, RemoveFoldout, ShowIf("positionReplication"),
     ShowIf("positionAxis", RagonAxis.XYZ, RagonAxis.X, RagonAxis.XY, RagonAxis.XZ)]
    private RagonFloatPropertyInfo positionX;

    [SerializeField, RemoveFoldout, ShowIf("positionReplication"),
     ShowIf("positionAxis", RagonAxis.XYZ, RagonAxis.Y, RagonAxis.YZ, RagonAxis.XY)]
    private RagonFloatPropertyInfo positionY;

    [SerializeField, RemoveFoldout, ShowIf("positionReplication"),
     ShowIf("positionAxis", RagonAxis.XYZ, RagonAxis.Z, RagonAxis.YZ, RagonAxis.YZ, RagonAxis.XZ)]
    private RagonFloatPropertyInfo positionZ;

    [SerializeField] private bool rotationReplication;

    [SerializeField, ShowIf("rotationReplication")]
    private RagonAxis rotationAxis = RagonAxis.XYZ;

    [SerializeField, RemoveFoldout, ShowIf("rotationReplication"),
     ShowIf("rotationAxis", RagonAxis.XYZ, RagonAxis.X, RagonAxis.XY, RagonAxis.XZ)]
    private RagonFloatPropertyInfo rotationX;

    [SerializeField, RemoveFoldout, ShowIf("rotationReplication"),
     ShowIf("rotationAxis", RagonAxis.XYZ, RagonAxis.Y, RagonAxis.YZ, RagonAxis.XY)]
    private RagonFloatPropertyInfo rotationY;

    [SerializeField, RemoveFoldout, ShowIf("rotationReplication"),
     ShowIf("rotationAxis", RagonAxis.XYZ, RagonAxis.Z, RagonAxis.YZ, RagonAxis.YZ, RagonAxis.XZ)]
    private RagonFloatPropertyInfo rotationZ;

    [SerializeField] private bool scaleReplication;

    [SerializeField, ShowIf("scaleReplication")]
    private RagonAxis scaleAxis = RagonAxis.XYZ;

    [SerializeField, RemoveFoldout, ShowIf("scaleReplication"),
     ShowIf("scaleAxis", RagonAxis.XYZ, RagonAxis.X, RagonAxis.XY, RagonAxis.XZ)]
    private RagonFloatPropertyInfo scaleX;

    [SerializeField, RemoveFoldout, ShowIf("scaleReplication"),
     ShowIf("scaleAxis", RagonAxis.XYZ, RagonAxis.Y, RagonAxis.YZ, RagonAxis.XY)]
    private RagonFloatPropertyInfo scaleY;

    [SerializeField, RemoveFoldout, ShowIf("scaleReplication"),
     ShowIf("scaleAxis", RagonAxis.XYZ, RagonAxis.Z, RagonAxis.YZ, RagonAxis.YZ, RagonAxis.XZ)]
    private RagonFloatPropertyInfo scaleZ;

    private RagonVector3 _rotation;
    private RagonVector3 _position;
    private RagonVector3 _scale;

    private LimitedQueue<Quaternion> _rotationBuffer;
    private LimitedQueue<Vector3> _positionBuffer;
    private LimitedQueue<Vector3> _scaleBuffer;

    [SerializeField, ReadOnly] private int bits;

    public void SetTarget(Transform t) => target = t;

    public override bool OnDiscovery(List<RagonProperty> properties)
    {
      if (rotationReplication)
      {
        var compressorX = new FloatCompressor(rotationX.Min, rotationX.Max, rotationX.Precision);
        var compressorY = new FloatCompressor(rotationY.Min, rotationY.Max, rotationY.Precision);
        var compressorZ = new FloatCompressor(rotationZ.Min, rotationZ.Max, rotationZ.Precision);

        _rotationBuffer = new LimitedQueue<Quaternion>(3);
        _rotation = new RagonVector3(rotationAxis, compressorX, compressorY, compressorZ, false);
        _rotation.Changed += () => _rotationBuffer.Enqueue(Quaternion.Euler(_rotation.Value));

        if (target)
          _rotation.Value = target.localRotation.eulerAngles;

        properties.Add(_rotation);
      }

      if (positionReplication)
      {
        var compressorX = new FloatCompressor(positionX.Min, positionX.Max, positionX.Precision);
        var compressorY = new FloatCompressor(positionY.Min, positionY.Max, positionY.Precision);
        var compressorZ = new FloatCompressor(positionZ.Min, positionZ.Max, positionZ.Precision);

        _positionBuffer = new LimitedQueue<Vector3>(3);
        _position = new RagonVector3(positionAxis, compressorX, compressorY, compressorZ, false);
        _position.Changed += () => _positionBuffer.Enqueue(_position.Value);
        
        if (target)
          _position.Value = target.position;

        properties.Add(_position);
      }

      if (scaleReplication)
      {
        var compressorX = new FloatCompressor(scaleX.Min, scaleX.Max, scaleX.Precision);
        var compressorY = new FloatCompressor(scaleY.Min, scaleY.Max, scaleY.Precision);
        var compressorZ = new FloatCompressor(scaleZ.Min, scaleZ.Max, scaleZ.Precision);

        _scaleBuffer = new LimitedQueue<Vector3>(3);
        _scale = new RagonVector3(scaleAxis, compressorX, compressorY, compressorZ, false);
        _scale.Changed += () => _scaleBuffer.Enqueue(_scale.Value);
        
        if (target)
          _scale.Value = target.localScale;

        properties.Add(_scale);
      }

      return true;
    }

    public override void OnUpdateEntity()
    {
      if (positionReplication)
      {
        var positionEqual = IsEqual(_position.Value, target.position, 0.1f);
        if (!positionEqual)
          _position.Value = target.position;
      }

      if (rotationReplication)
      {
        var rotationEqual = IsEqual(_rotation.Value, target.localRotation.eulerAngles, 0.1f);
        if (!rotationEqual)
          _rotation.Value = target.localRotation.eulerAngles;
      }

      if (scaleReplication)
      {
        var positionEqual = IsEqual(_scale.Value, target.localScale, 0.1f);
        if (!positionEqual)
          _scale.Value = target.localScale;
      }
    }

    public override void OnUpdateProxy()
    {
      if (positionReplication)
        target.position = Vector3.Lerp(target.position, _position.Value, Time.deltaTime * 10);

      if (rotationReplication)
        target.localRotation =
          Quaternion.Lerp(target.localRotation, Quaternion.Euler(_rotation.Value), Time.deltaTime * 15);

      if (scaleReplication)
        target.localScale = Vector3.Lerp(target.localScale, _scale.Value, Time.deltaTime * 15);
    }

    public bool IsEqual(Vector3 v1, Vector3 v2, float precision)
    {
      bool equal = true;

      if (Mathf.Abs(v1.x - v2.x) > precision) equal = false;
      if (Mathf.Abs(v1.y - v2.y) > precision) equal = false;
      if (Mathf.Abs(v1.z - v2.z) > precision) equal = false;

      return equal;
    }

    private void OnValidate()
    {
      bits = 0;

      if (positionReplication)
      {
        if (positionAxis is RagonAxis.XYZ or RagonAxis.X or RagonAxis.XY or RagonAxis.XZ)
          ValidateProperty(positionX);

        if (positionAxis is RagonAxis.XYZ or RagonAxis.Y or RagonAxis.XY or RagonAxis.YZ)
          ValidateProperty(positionY);

        if (positionAxis is RagonAxis.XYZ or RagonAxis.Z or RagonAxis.XZ or RagonAxis.YZ)
          ValidateProperty(positionZ);
      }

      if (rotationReplication)
      {
        if (rotationAxis is RagonAxis.XYZ or RagonAxis.X or RagonAxis.XY or RagonAxis.XZ)
          ValidateProperty(rotationX);

        if (rotationAxis is RagonAxis.XYZ or RagonAxis.Y or RagonAxis.XY or RagonAxis.YZ)
          ValidateProperty(rotationY);

        if (rotationAxis is RagonAxis.XYZ or RagonAxis.Z or RagonAxis.XZ or RagonAxis.YZ)
          ValidateProperty(rotationZ);
      }

      if (scaleReplication)
      {
        if (scaleAxis is RagonAxis.XYZ or RagonAxis.X or RagonAxis.XY or RagonAxis.XZ)
          ValidateProperty(scaleX);

        if (scaleAxis is RagonAxis.XYZ or RagonAxis.Y or RagonAxis.XY or RagonAxis.YZ)
          ValidateProperty(scaleY);

        if (scaleAxis is RagonAxis.XYZ or RagonAxis.Z or RagonAxis.XZ or RagonAxis.YZ)
          ValidateProperty(scaleZ);
      }
    }

    private void ValidateProperty(RagonFloatPropertyInfo info)
    {
      if (info.Min > info.Max)
        info.Max = info.Min + 1.0f;

      if (info.Max < info.Min)
        info.Min = info.Max - 1.0f;

      var floatCompressor = new FloatCompressor(info.Min, info.Max, info.Precision);
      info.Bits = floatCompressor.RequiredBits;

      bits += info.Bits;
    }
  }
}