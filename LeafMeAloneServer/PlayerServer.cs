﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;
using SlimDX;

namespace Server
{
    public class PlayerServer : IPlayer
    {

        public int Id { get; internal set; }
        bool IPlayer.UsingTool { get; set; }
        private Transform transform;
        bool IPlayer.Dead { get; set; }
        PlayerPacket.ToolType IPlayer.ToolEquipped { get; set; }
        int INetworked.Id { get; set; }

        public Transform GetTransform()
        {
            return transform;
        }

        public void SetTransform(Transform value)
        {
            transform = value;
        }

        public void UpdateFromPacket(PlayerPacket packet)
        {
            transform.Position += new Vector3(packet.MovementX, packet.MovementY, 0.0f);

            transform.Direction.Y = packet.Rotation;
        }
    }
}
