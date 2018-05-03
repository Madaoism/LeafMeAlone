﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;
using SlimDX;

namespace Server
{
    public class PlayerServer : GameObjectServer, IPlayer
    {

        public bool Dead { get; set; }
        public ToolType ToolEquipped { get; set; }

        // If the user is using the primary function of their tool.
        public bool UsingToolPrimary { get; set; }

        // If the user is using the secondary function of their tool.
        public bool UsingToolSecondary { get; set; }

        public PlayerServer() : base(ObjectType.PLAYER)
        { }

        public Transform GetTransform()
        {
            return Transform;
        }

        public void SetTransform(Transform value)
        {
            Transform = value;
        }

        public override void Update(float deltaTime)
        {

            if (UsingToolPrimary)
            {

                List<LeafServer> LeafList = GameServer.instance.LeafList;
                for (int i = 0; i < LeafList.Count; i++)
                {
                    if (LeafList[i].IsInPlayerToolRange(this))
                    {

                    }
                }
            }

            else if (UsingToolSecondary)
            {

            }

        }

        public void UpdateFromPacket(PlayerPacket packet)
        {
            Transform.Position += new Vector3(packet.MovementX, packet.MovementY, 0.0f) * GameServer.TICK_TIME_S;

            Transform.Rotation.Y = packet.Rotation;
        }

        public override void HitByTool(ToolType toolType)
        {
            // TODO
        }
    }
}
