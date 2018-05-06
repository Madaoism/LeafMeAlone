﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;
using SlimDX;

namespace Server
{
    /// <summary>
    /// Leaf class on the game server. Is a physics object.
    /// </summary>
    public class LeafServer : PhysicsObject
    {

        public const float LEAF_BURN_TIME = 3.0f;

        public LeafServer() : base(ObjectType.LEAF, LEAF_BURN_TIME)
        {

        }

        public LeafServer(Transform startTransform) : 
            base (ObjectType.LEAF, startTransform, LEAF_BURN_TIME)
        {

        }

        public LeafServer(Transform startTransform, float mass) : 
            base(ObjectType.LEAF, startTransform, mass)
        {


        }
    }
}