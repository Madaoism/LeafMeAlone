﻿using SlimDX;

namespace Shared
{
    /// <summary>
    /// Constants for the game.
    /// </summary>
    public static class Constants
    {
        public static Vector3 PlayerToToolOffset = new Vector3(1.8f, 3.85f, 3.0f);



        /// <summary>
        /// Shaders
        /// </summary>
        public const string DefaultShader = @"../../Shaders/defaultShader.fx";
        public const string ParticleShader = @"../../Shaders/particle.fx";

        /// <summary>
        /// Textures
        /// </summary>
        public const string FireTexture = @"../../Particles/fire_red.png";
        public const string WindTexture = @"../../Particles/Wind_Transparent2.png";


        /// <summary>
        /// Models
        /// </summary>
        public const string LeafModel = @"../../Models/05.13.18_Leaf.fbx";
        public const string PlayerModel = @"../../Models/05.03.18_Version2.fbx";
        public const string DefaultMapModel = @"../../Models/Terrain.fbx";
        public const string TreeModel = @"../../Models/TreeAttempt.fbx";

        /// <summary>
        /// Sounds/Audio files
        /// </summary>
        public const string Bgm = @"../../Sound/OptionalForestAmbient.wav";
        public const string FlameThrowerStart = @"../../Sound/collision.wav";
        public const string FlameThrowerLoop = @"../../Sound/Flamethrower.wav";
        public const string FlameThrowerEnd = @"../../Sound/collision.wav";

        public const string LeafBlowerStart = @"../../Sound/collision.wav";
        public const string LeafBlowerLoop = @"../../Sound/Leafblower.wav";
        public const string LeafBlowerEnd = @"../../Sound/collision.wav";

        public const string LeafIgniting = @"../../Sound/collision.wav";
        public const string LeafBurning = @"../../Sound/Fire Burning.wav";
        public const string LeafBurnup = @"../../Sound/collision.wav";
        public const string LeafPutoff = @"../../Sound/collision.wav";

        public const string PlayerFootstep = @"../../Sound/Footsteps.wav";

        // Height of the world floor.
        public const float FLOOR_HEIGHT = -10.0f;

        // Width/height of the map.
        public const float MAP_WIDTH = 150.0f;
        public const float MAP_HEIGHT = 150.0f;

        public const float NO_MANS_LAND_PERCENT = 0.1f;

        // Margin around the map. Leaves won't spawn in these margins.
        public const float BORDER_MARGIN = 5.0f;

        // Total number of leaves in the game.
        public const int NUM_LEAVES = 300;

        public const float PUSH_FACTOR = 3.0f;

        public const float BLOWER_DISTANCE_SCALER = 2.0f;

        public const float TREE_FREQUENCY = 0.01f;

        public const bool PIVOT_DEBUG = false;

        public const int DEATH_TIME = 3;
    }
}
