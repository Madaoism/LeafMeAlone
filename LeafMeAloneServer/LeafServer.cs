﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared;

namespace Server
{
    class LeafServer : GameObjectServer, ILeaf
    {
        public bool Burning { get; set; }
        public float TimeBurning { get; set; }

        public LeafServer(Transform startTransform) : base(startTransform)
        {

        }
    }
}
