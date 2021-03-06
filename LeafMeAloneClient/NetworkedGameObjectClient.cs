﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;
using Shared.Packet;
using SlimDX;

namespace Client
{
    /// <summary>
    /// Client-side GameObject.
    /// </summary>
    public abstract class NetworkedGameObjectClient : GraphicGameObject
    {
        public bool Moving = false;

        /// <summary>
        /// Constructs a new local GameObject with a model at the specified path and a specified position.
        /// </summary>
        /// <param name="createPacket">Initial packet for this object.</param>
        /// <param name="modelPath">Path to this gameobject's model.</param>
        public NetworkedGameObjectClient(CreateObjectPacket createPacket, string modelPath) : base(modelPath)
        {
            Id = createPacket.ObjData.IdData.ObjectId;
            Transform.Position.X = createPacket.ObjData.PositionX;
            Transform.Position.Y = createPacket.ObjData.PositionY;
            Transform.Position.Z = createPacket.ObjData.PositionZ;

            Transform.Rotation.Y = createPacket.ObjData.Rotation;
        }

        /// <summary>
        /// Updates this object from a network packet.
        /// </summary>
        /// <param name="packet">Packet to update from.</param>
        public virtual void UpdateFromPacket(BasePacket packet)
        {
            ObjectPacket objPacket = packet as ObjectPacket;

            // Set the initial positions of the object.
            float epsilon = 0.001f;
            Moving = !( Math.Abs(Transform.Position.X - objPacket.PositionX) < epsilon
                       &&  Math.Abs(Transform.Position.Z - objPacket.PositionZ) < epsilon );

            Transform.Position.X = objPacket.PositionX;
            Transform.Position.Z = objPacket.PositionZ;

            Transform.Rotation.Y = objPacket.Rotation;

            // Set the initial burning status.
            Burning = objPacket.Burning;
            Health = objPacket.Health;
        }
    }
}
