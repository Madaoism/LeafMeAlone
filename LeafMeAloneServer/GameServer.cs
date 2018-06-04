﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
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

        private int playerSpawnIndex = 0;

        private Stopwatch currentFrameTimer;

        private Random rnd;

        // Whether game is running on net or not
        private bool development;

        //Used to assign unique object ids. Increments with each object. Potentially subject to overflow issues.
        public int nextObjectId = 0;
        private MatchHandler matchHandler;

        public GameServer(bool networked)
        {
            if (instance != null)
            {
                Console.WriteLine(SINGLETON_VIOLATED);
            }

            instance = this;
            development = !networked;

            timer = new Stopwatch();
            currentFrameTimer = new Stopwatch();
            rnd = new Random();

            timer.Start();
            currentFrameTimer.Start();

            networkServer = new NetworkServer(networked);
            matchHandler = new MatchHandler(Match.DefaultMatch, networkServer, this);

            // Create the initial game map.
            CreateMap();

            // Create the leaves for the game.
            CreateRandomLeaves(Constants.NUM_LEAVES);
            //CreateLeaves(100, -10, 10, -10, 10);

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
                currentFrameTimer.Restart();

                //Check if a client wants to connect.
                networkServer.CheckForConnections();

                Console.WriteLine($"Timer at {currentFrameTimer.ElapsedMilliseconds} before receive");

                // Go ahead and try to receive new updates
                networkServer.Receive();

                Console.WriteLine($"Timer at {currentFrameTimer.ElapsedMilliseconds} before incoming packets");

                //Update the server players based on received packets.
                if (!matchHandler.MatchInitializing())
                {
                    HandleIncomingPackets();
                }

                Console.WriteLine($"Timer at {currentFrameTimer.ElapsedMilliseconds} before status updates");

                matchHandler.DoMatchStatusUpdates();

                //Clear list for next frame.
                networkServer.PlayerPackets.Clear();

                Console.WriteLine($"Timer at {currentFrameTimer.ElapsedMilliseconds} before object updates");


                UpdateObjects(timer.ElapsedMilliseconds / 1000.0f);


                Console.WriteLine($"Timer at {currentFrameTimer.ElapsedMilliseconds} before send updates");

                //Send object data to all clients.
                networkServer.SendWorldUpdateToAllClients();
                toDestroyQueue.Clear();

                timer.Restart();

                Console.WriteLine($"Timer at {currentFrameTimer.ElapsedMilliseconds} after send updates");


                if ((int)(TICK_TIME - currentFrameTimer.ElapsedMilliseconds) < 0)
                {
                    Console.WriteLine($"Warning: Server is falling behind. Took {currentFrameTimer.ElapsedMilliseconds} ms");
                }

                //Sleep for the rest of this tick.
                Thread.Sleep(Math.Max(0, (int)(TICK_TIME - currentFrameTimer.ElapsedMilliseconds)));
            }
        }

        private void HandleIncomingPackets()
        {
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
        }

        /// <summary>
        /// Handles what to do once a new client socket is connected
        /// </summary>
        internal void ConnectCallback()
        {
            if ((development && playerServerList.Count == 1) || (!development && playerServerList.Count == 4)) {
                matchHandler.StartMatch();
            }
        }

        /// <summary>
        /// Call Update() on all objects in the object dict.
        /// </summary>
        /// <param name="deltaTime"></param>
        public void UpdateObjects(float deltaTime)
        {
            //TestPhysics();

            List<GameObjectServer> toUpdateList = GetInteractableObjects();
            //This foreach loop hurts my soul. May consider making it normal for loop.
            foreach (GameObjectServer toUpdate in toUpdateList)
            {
                toUpdate.Update(deltaTime);
                
            }

            // Add the effects of the player tools.
            AddPlayerToolEffects();

        }

        public PlayerServer CreateNewPlayer()
        {
            //Assign id based on the next spot in the gameObjectDict.
            int id = gameObjectDict.Count();

            //Create two players, one to send as an active player to client. Other to keep track of on server.
            PlayerServer newPlayer = matchHandler.AddPlayer();
            newPlayer.Register();
            playerServerList.Add(newPlayer);

            //Create the active player with the same id as the newPlayer.
            PlayerServer newActivePlayer = new PlayerServer(newPlayer.Team)
            {
                ObjectType = ObjectType.ACTIVE_PLAYER,
                Id = newPlayer.Id
            };
            newActivePlayer.Transform.Position = newPlayer.Transform.Position;

            CreatePlayerPacket objPacket = (CreatePlayerPacket)ServerPacketFactory.NewCreatePacket(newPlayer);

            // Sending this new packet before the new client joins. 
            networkServer.SendAll(PacketUtil.Serialize(objPacket));

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

            float startX = (-newMap.Width / 2.0f) + Constants.TREE_RADIUS;
            float startY = -newMap.Height / 2.0f;
            float endX = newMap.Width / 2.0f;
            float endY = newMap.Height / 2.0f;

            // Spawn trees around the border of the map!
            // Start by iterating through the height of the map, centered on origin and increase by the radius of a tree.
            for (float y = startY; y < endY; y += (Constants.TREE_RADIUS))
            {

                // Iterate through the width of the map, centered on origin and increase by radius of a tree.
                for (float x = startX; x < endX; x += (Constants.TREE_RADIUS))
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
                    if (y <= startY || endY <= (y + (Constants.TREE_RADIUS)))
                    {

                        // Make a new tree.
                        TreeServer newTree = new TreeServer();

                        // Set the tree's initial position.
                        newTree.Transform.Position = new Vector3(x, Constants.FLOOR_HEIGHT, y);

                        // If this is the bottom row.
                        if (y <= startY)
                        {
                            // Increase the radius.
                            newTree.Radius = Constants.TREE_RADIUS * 2;
                        }

                        // Send the new object to client.
                        networkServer.SendNewObjectToAll(newTree);

                    }

                    // If this is the far left or right columns, create a tree.
                    else if (x <= startX || endX <= (x + (Constants.TREE_RADIUS)))
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

            float minX = matchHandler.GetMatch().NoMansLand.leftX;
            float maxX = matchHandler.GetMatch().NoMansLand.rightX;
            float minZ = matchHandler.GetMatch().NoMansLand.downZ + (2 * Constants.TREE_RADIUS);
            float maxZ = matchHandler.GetMatch().NoMansLand.upZ - (2 * Constants.TREE_RADIUS);

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

                //Only check if tools are active.
                if (player.ActiveToolMode != ToolMode.NONE)
                {
                    // Affect all objects within range of the player.
                    player.AffectObjectsInToolRange(GetInteractableObjects());
                }

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

        /// <summary>
        /// Get list of players and leaves.
        /// </summary>
        /// <returns></returns>
        public List<GameObjectServer> GetInteractableObjects()
        {
            List<GameObjectServer> gameObjects = GetGameObjectList();
            List<GameObjectServer> interactableGameObjects = new List<GameObjectServer>();

            for (int i = 0; i < gameObjects.Count; i++)
            {
                if (gameObjects[i] is LeafServer || gameObjects[i] is PlayerServer)
                {
                    interactableGameObjects.Add(gameObjects[i]);
                }
            }

            return interactableGameObjects;
        }
    }
}
