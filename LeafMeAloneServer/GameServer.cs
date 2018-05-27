﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Shared;
using Shared.Packet;
using SlimDX;

namespace Server
{
    public class GameServer
    {
        public static GameServer instance;

        public List<GameObject> toDestroyQueue = new List<GameObject>();
        public List<PlayerServer> playerServerList = new List<PlayerServer>();
        public Dictionary<int, GameObjectServer> gameObjectDict =
            new Dictionary<int, GameObjectServer>();

        private NetworkServer networkServer;

        //Time in ms for each tick.
        public const long TICK_TIME = 33;

        //Time per second for each tick.
        public const float TICK_TIME_S = .033f;
        private const string SINGLETON_VIOLATED =
            "ERROR: Singleton pattern violated on GameServer.cs. There are multiple instances!";
        private Stopwatch timer;

        private List<Vector3> spawnPoints = new List<Vector3>();
        private int playerSpawnIndex = 0;

        private Stopwatch testTimer;

        private Random rnd;

        private Match activeMatch = Match.DefaultMatch;

        // Whether game is running on net or not
        private bool development;

        //Used to assign unique object ids. Increments with each object. Potentially subject to overflow issues.
        public int nextObjectId = 0;
        private bool matchOver = false;

        public GameServer(bool networked)
        {
            if (instance != null)
            {
                Console.WriteLine(SINGLETON_VIOLATED);
            }

            instance = this;
            development = !networked;

            timer = new Stopwatch();
            testTimer = new Stopwatch();
            rnd = new Random();

            timer.Start();
            testTimer.Start();

            spawnPoints.Add(new Vector3(-10, 0, -10));
            spawnPoints.Add(new Vector3(-10, 0, 10));
            spawnPoints.Add(new Vector3(10, 0, -10));
            spawnPoints.Add(new Vector3(10, 0, 10));

            networkServer = new NetworkServer(networked);

            // Create the initial game map.
            CreateMap();

            // Create the leaves for the game.
            CreateRandomLeaves(Constants.NUM_LEAVES);
            //CreateLeaves(100, -10, 10, -10, 10);

            if (development)
            {
                activeMatch.StartMatch(int.MaxValue);
            }
        }

        public static int Main(String[] args)
        {
            bool networked = false;

            if (args.Length > 0)
            {
                networked = true;
            }

            GameServer gameServer = new GameServer(networked);

            gameServer.networkServer.StartListening();

            gameServer.DoGameLoop();

            return 0;
        }

        /// <summary>
        /// Main game loop.
        /// </summary>
        public void DoGameLoop()
        {
            while (true)
            {

                //Check if a client wants to connect.
                networkServer.CheckForConnections();

                // Go ahead and try to receive new updates
                networkServer.Receive();

                //Update the server players based on received packets.
                //if (activeMatch.Started())
                //{
                    for (int i = 0; i < networkServer.PlayerPackets.Count(); i++)
                    {
                        RequestPacket packet = networkServer.PlayerPackets[i];
                        int playerId = packet.GetId();
                        gameObjectDict.TryGetValue(playerId, out GameObjectServer playerGameObject);

                        if (packet != null && playerGameObject != null)
                        {
                            PlayerServer player = (PlayerServer)playerGameObject;
                            player.UpdateFromPacket(networkServer.PlayerPackets[i]);
                        }
                    }

                //}

                //Clear list for next frame.
                networkServer.PlayerPackets.Clear();

                UpdateObjects(timer.ElapsedMilliseconds / 1000.0f);

                //Send object data to all clients.
                networkServer.SendWorldUpdateToAllClients();
                toDestroyQueue.Clear();

                if ((int)(TICK_TIME - timer.ElapsedMilliseconds) < 0)
                {
                    //     Console.WriteLine("Warning: Server is falling behind.");
                }

                timer.Restart();

                //Sleep for the rest of this tick.
                System.Threading.Thread.Sleep(Math.Max(0, (int)(TICK_TIME - timer.ElapsedMilliseconds)));
            }
        }

        /// <summary>
        /// Call Update() on all objects in the object dict.
        /// </summary>
        /// <param name="deltaTime"></param>
        public void UpdateObjects(float deltaTime)
        {
            //TestPhysics();

            List<GameObjectServer> toUpdateList = gameObjectDict.Values.ToList();
            //This foreach loop hurts my soul. May consider making it normal for loop.
            foreach (GameObjectServer toUpdate in toUpdateList)
            {
                toUpdate.Update(deltaTime);
            }

            // Add the effects of the player tools.
            AddPlayerToolEffects();

            if (!matchOver)
            {
                activeMatch.CountObjectsOnSides(GetLeafListAsObjects());
                Team winningTeam = activeMatch.GameOver();
                if (winningTeam != Team.NONE)
                {
                    DoMatchFinish(winningTeam);
                }
            }
        }

        /// <summary>
        /// Handles the match over code, sends the win/lose to each player, 
        /// sets the match as over, resets leaves
        /// </summary>
        /// <param name="winningTeam">The team which should win</param>
        private void DoMatchFinish(Team winningTeam)
        {
            BasePacket donePacket = new ThePacketToEndAllPackets(winningTeam);
            networkServer.SendAll(PacketUtil.Serialize(donePacket));
            matchOver = true;
            GetLeafListAsObjects().ForEach(l => l.Burning = true);
        }

        public PlayerServer CreateNewPlayer()
        {
            //Assign id based on the next spot in the gameObjectDict.
            int id = gameObjectDict.Count();

            //Create two players, one to send as an active player to client. Other to keep track of on server.
            PlayerServer newPlayer = new PlayerServer((Team)(playerSpawnIndex % 2) + 1);
            newPlayer.Register();

            //Create the active player with the same id as the newPlayer.
            PlayerServer newActivePlayer = new PlayerServer((Team)(playerSpawnIndex % 2) + 1);
            newActivePlayer.ObjectType = ObjectType.ACTIVE_PLAYER;
            newActivePlayer.Id = newPlayer.Id;

            playerServerList.Add(newPlayer);

            Vector3 nextSpawnPoint = spawnPoints[(playerSpawnIndex++ % spawnPoints.Count)];

            //Note currently assuming players get ids 0-3
            newActivePlayer.Transform.Position = nextSpawnPoint;
            newPlayer.Transform.Position = nextSpawnPoint;

            CreatePlayerPacket objPacket = ServerPacketFactory.NewCreatePacket(newPlayer);

            // Sending this new packet before the new client joins. 
            networkServer.SendAll(PacketUtil.Serialize(objPacket));

            if (!development && playerServerList.Count == Constants.NUM_PLAYERS)
            {
                activeMatch.StartMatch(Constants.MATCH_TIME);
            }

            return newActivePlayer;
        }

        /// <summary>
        /// Creates the initial game map.
        /// </summary>
        /// <returns>The new map</returns>
        public MapServer CreateMap()
        {

            Random rnd = new Random();

            // Create the map with a width and height.
            MapServer newMap = new MapServer(Constants.MAP_WIDTH, Constants.MAP_HEIGHT);

            // Spawn trees around the border of the map!
            // Start by iterating through the height of the map, centered on origin and increase by the radius of a tree.
            for (float y = -newMap.Height / 2.0f; y < newMap.Height / 2.0f; y += TreeServer.TREE_RADIUS)
            {

                // Iterate through the width of the map, centered on origin and increase by radius of a tree.
                for (float x = -newMap.Width / 2.0f; x < newMap.Width / 2.0f; x += TreeServer.TREE_RADIUS)
                {

                    float random = (float)rnd.NextDouble();

                    if (random < Constants.TREE_FREQUENCY)
                    {

                        // Make a new tree.
                        TreeServer newTree = new TreeServer();

                        // Set the tree's initial position.
                        newTree.Transform.Position = new Vector3(x, Constants.FLOOR_HEIGHT, y);

                        // Send the new object to client.
                        networkServer.SendNewObjectToAll(newTree);

                    }

                    // If this is a top or bottom row, create trees.
                    if (y <= -newMap.Height / 2.0f || (newMap.Height / 2.0f) <= y + TreeServer.TREE_RADIUS)
                    {

                        // Make a new tree.
                        TreeServer newTree = new TreeServer();

                        // Set the tree's initial position.
                        newTree.Transform.Position = new Vector3(x, Constants.FLOOR_HEIGHT, y);

                        // Send the new object to client.
                        networkServer.SendNewObjectToAll(newTree);

                    }

                    // If this is the far left or right columns, create a tree.
                    else if (x <= -newMap.Width / 2.0f || (newMap.Width / 2.0f) <= x + TreeServer.TREE_RADIUS)
                    {

                        // Make a new tree.
                        TreeServer newTree = new TreeServer();

                        // Set the tree's initial position.
                        newTree.Transform.Position = new Vector3(x, Constants.FLOOR_HEIGHT, y);

                        // Send the new object to client.
                        networkServer.SendNewObjectToAll(newTree);

                    }
                }
            }

            // Return the new map.
            return newMap;

        }

        /// <summary>
        /// Creates all leaves in the scene, placing them randomly.
        /// </summary>
        /// <param name="num">Number of leaves to create.</param>
        public void CreateRandomLeaves(int num)
        {

            // Itereate through number of leaves we want to create.
            for (int i = 0; i < num; i++)
            {

                CreateRandomLeaf();

            }
        }

        public void CreateRandomLeaf()
        {

            // Very slight random offset for leaves so that there's no z-fighting.
            double minY = Constants.FLOOR_HEIGHT;
            double maxY = Constants.FLOOR_HEIGHT + 0.2f;

            // Variables for determining leaf spawn locations.
            float HalfWidth = Constants.MAP_WIDTH / 2.0f;
            float HalfHeight = Constants.MAP_HEIGHT / 2.0f;

            float minX = activeMatch.NoMansLand.leftX;
            float maxX = activeMatch.NoMansLand.rightX;
            float minZ = activeMatch.NoMansLand.downZ;
            float maxZ = activeMatch.NoMansLand.upZ;

            // Get random doubles for position.
            double randX = rnd.NextDouble();
            double randY = rnd.NextDouble();
            double randZ = rnd.NextDouble();

            // Bind random doubles to our range.
            randX = (randX * (maxX - minX)) + minX;
            randY = (randY * (maxY - minY)) + minY;
            randZ = (randZ * (maxZ - minZ)) + minZ;

            // Get the new position
            Vector3 pos = new Vector3((float)randX, (float)randY, (float)randZ);
            float rotation = rnd.NextFloat() * 360.0f;

            // Create a new leaf
            LeafServer newLeaf = new LeafServer();

            // Set the leaf's initial position.
            newLeaf.Transform.Position = pos;

            // Set the leaf's initial rotation.
            newLeaf.Transform.Rotation.Y = rotation;

            // Add this leaf to the leaf list and object dictionary.
            newLeaf.Register();

            // Send this object to the other objects.
            networkServer.SendNewObjectToAll(newLeaf);
        }

        /// <summary>
        /// Add the tool effects of all the players.
        /// </summary>
        public void AddPlayerToolEffects()
        {

            // Iterate through all players.
            for (int i = 0; i < playerServerList.Count; i++)
            {

                // Get this player.
                PlayerServer player = playerServerList[i];

                // Affect all objects within range of the player.
                player.AffectObjectsInToolRange(gameObjectDict.Values.ToList());

            }
        }

        /// <summary>
        /// Destroys the given game object; removes it from the dictionary of 
        /// objects to send update packets for, and adds the destory packet to 
        /// the toDestroy Queue
        /// </summary>
        /// <param name="gameObj">The game object to destroy</param>
        public void Destroy(GameObject gameObj)
        {

            gameObjectDict.Remove(gameObj.Id);

            if (gameObj is PlayerServer player)
            {
                playerServerList.Remove(player);
            }

            toDestroyQueue.Add(gameObj);

            if (gameObj is LeafServer)
            {
                CreateRandomLeaf();
            }
        }

        /// <summary>
        /// Gets a list of game objects from the dictionary.
        /// </summary>
        /// <returns>A list of all objects from the object dictionary.</returns>
        public List<GameObjectServer> GetGameObjectList()
        {
            // Turn the game objects to a value list.
            return gameObjectDict.Values.ToList();
        }

        /// <summary>
        /// Gets a new random spawn point for the player.
        /// </summary>
        /// <returns>A vector 3 of the spawn point</returns>
        public Vector3 GetRandomSpawnPoint()
        {
            int index = new Random().Next(spawnPoints.Count);
            return spawnPoints.ElementAt(index);
        }
      
        public List<GameObject> GetLeafListAsObjects()
        {

            List<GameObjectServer> gameObjects = GetGameObjectList();
            List<GameObject> leaves = new List<GameObject>();

            for (int i = 0; i < gameObjects.Count; i++)
            {
                if (gameObjects[i] is LeafServer)
                {
                    leaves.Add(gameObjects[i]);
                }
            }

            return leaves;
        }
    }
}
