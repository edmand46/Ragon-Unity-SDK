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

using ENet;

namespace Ragon.Client.Unity
{
  public sealed class ENetReliableChannel : INetworkChannel
  {
    private Peer _peer;
    private byte _channelId;

    public ENetReliableChannel(Peer peer, int channelId)
    {
      _peer = peer;
      _channelId = (byte) channelId;
    }

    public void Send(byte[] data)
    {
      var packet = new Packet();
      packet.Create(data, data.Length, PacketFlags.Reliable);

      _peer.Send(_channelId, ref packet);
    }
  }
}