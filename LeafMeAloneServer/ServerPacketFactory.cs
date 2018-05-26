﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;
using Shared.Packet;
using SlimDX;

namespace Server
{
    /// <summary>
    /// Handles the generation of packets on the server
    /// </summary>
    public class ServerPacketFactory : PacketFactory
    {

        internal static BasePacket CreateUpdatePacket(GameObjectServer serverObject)
        {
                if (serverObject is PlayerServer player)
                {
                    return CreatePlayerPacket(player);
                } 
                return NewObjectPacket(serverObject);
        }

        internal static CreatePlayerPacket NewCreatePacket(PlayerServer player)
        {
            CreateObjectPacket createPacket = NewCreatePacket((GameObject) player);
            return new CreatePlayerPacket(createPacket, player.Team);
        }

        internal static PlayerPacket CreatePlayerPacket(PlayerServer player)
        {
            return new PlayerPacket(NewObjectPacket(player), player.ActiveToolMode, player.ToolEquipped,
                player.Dead);
        }
    }
}
