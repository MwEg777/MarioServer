using System;
using System.Drawing;
using System.Collections.Generic;
using System.Net.Sockets;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System.Net;
using System.Linq;
using CoreExtensions;
using Console = Colorful.Console;
using Newtonsoft.Json;
using static MythServer.Server;

namespace MythServer
{
    class Methods
    {

        public Methods()
        {

            instance = this;

        }

        public static Methods instance;

        public static List<Player> players = new List<Player>();

        public static Dictionary<string, Player> playerAddressesDict = new Dictionary<string, Player>();

        public static Dictionary<string, Player> tokenPlayerMap = new Dictionary<string, Player>();

        public static List<Room> rooms = new List<Room>();

        public static List<LobbyRoom> lobbyRooms = new List<LobbyRoom>();
        //public Player AddPlayer(PlayerHub hub)
        //{

        //    Player player = new Player
        //    {

        //        playerHub = hub

        //    };

        //    //player.udpIPEndPoint = (IPEndPoint)conn.Client.RemoteEndPoint;
        //    players.Add(player);
        //    return player;

        //}
        public bool RemovePlayer(Player player)
        {

            try
            { 

                return players.Remove(player);

            }
            catch(Exception ex)
            {

                Console.WriteLine("Couldn't remove player after he disconnected. " + ex, Color.Red);
                return false;

            }
        }
        public Player GetPlayerByID(string id)
        {

            return players.Find(p => p.id == id);

        }

        #region RoomsStuff

        #endregion

        #region PlayerRequests

        public void GET_DATA(Player player, Dictionary<string, object> payload)
        {

            try
            {

                string fbToken = payload["fbtoken"].ToString();
                player.token = fbToken;

                //TODO: Uncomment
                /*
                //Remove all player entries with that player value
                foreach (var item in tokenPlayerMap.Where(kvp => kvp.Value == player).ToList())
                    tokenPlayerMap.Remove(item.Key);

                tokenPlayerMap.Remove(fbToken);

                tokenPlayerMap.Add(fbToken, player);
                */

                AuthManager.FacebookAuth(payload["clientid"].ToString(), (fbResponse) => {

                    try
                    {

                        if (fbResponse.success)
                        {

                            player.id = fbResponse.id;

                            player.name = payload["name"].ToString();

                            //if (!string.IsNullOrEmpty(fbResponse.name) && !string.IsNullOrWhiteSpace(fbResponse.name))
                            //    player.name = fbResponse.name;

                            //TODO: Uncomment all of this
                            //TODO: Change this to id

                            /*
                            var filter = Builders<PlayerDB>.Filter.Eq("name", player.name);

                            if (!Server.db.PlayerExists(filter))
                                Server.db.InsertPlayer("Users", new PlayerDB
                                {

                                    name = player.name,
                                    id = player.id,
                                    deviceid = payload["clientid"].ToString(),
                                    friendids = new List<string>(),
                                    friendrequestids = new List<string>(),
                                    blockedids = new List<string>()

                                });


                            var update = Builders<PlayerDB>.Update.Set("name", player.name?? "Player");

                            Server.db.UpdatePlayer(filter, update);

                            */

                            //Removing duplicate instances of player
                            /*for (int i = 0; i < players.Count; i++)
                                if (players[i].id == player.id || players[i].address == player.address)
                                    if (!players[i].Equals(player))
                                        players.Remove(players[i]);*/

                            Server.SendMessageToPlayer(player, R_GOOD_TO_GO());

                        }
                        else
                        {

                            Console.WriteLine("Couldn't login user using facebook. ", Color.Red);
                            Server.SendMessageToPlayer(player, R_SERVER_MESSAGE("Error", "Couldn't login using facebook."));

                        }

                    }
                    catch(Exception ex)
                    {

                        Console.WriteLine("Couldn't get player data. " + ex, Color.Red);

                    }

                });

            }
            catch(Exception ex)
            {

                Console.WriteLine("Couldn't get player data. " + ex, Color.Red);

            }

        }

        

        public void IAM_ALIVE(Player player, Dictionary<string, object> payload)
        {

            //Console.WriteLine("Player sent that he's alive! Player ID is: " + player.id);
            //Server.SendMessageToPlayer(player, R_SERVER_MESSAGE("Got it", "I heard that you're alive"));

        }

        public void JOIN_GAME(Player player, Dictionary<string, object> payload)
        {

            Room firstAvailableRoom = rooms.Find(r => r.matchMaking);

            if (firstAvailableRoom != null) //Found an available room. Join it.
            {

                firstAvailableRoom.AddPlayerToRoom(player);

            }
            else //Create a new room and add him to it.
            {

                Room newRoom = new Room();
                rooms.Add(newRoom);
                newRoom.AddPlayerToRoom(player);

            }

        }

        public void LEAVE_GAME(Player player, Dictionary<string, object> payload)
        {

            if (player.playerRoom != null)
                player.playerRoom.RemovePlayerFromRoom(player);

        }

        public void UPDATE_MARIO_POS(Player player, Dictionary<string, object> payload)
        {

            player.playerRoom.UpdatePos(player, payload["msi"]);

        }

        public void UPDATE_MARIO_SIZE(Player player, Dictionary<string, object> payload)
        {

            player.playerRoom.UpdateSize(player, payload["newsize"]);

        }

        public void BOUNCE_BLOCK(Player player, Dictionary<string, object> payload)
        {

            player.playerRoom.BounceBlock(player, payload["parentname"]);

        }

        public void ACTIVATE_BLOCK(Player player, Dictionary<string, object> payload)
        {

            player.playerRoom.ActivateBlock(player, payload["parentname"], payload["triggerersize"]);

        }

        public void TAKE_POWERUP(Player player, Dictionary<string, object> payload)
        {

            player.playerRoom.TakePowerup(player, payload["powerupid"]);

        }

        public void STOMP_ENEMY(Player player, Dictionary<string, object> payload)
        {

            player.playerRoom.StompEnemy(player, payload["enemyname"], payload["enemypos"]);

        }

        #endregion

        #region ServerResponses

        public static string R_GOOD_TO_GO()
        {

            Dictionary<string, object> toConvert = new Dictionary<string, object>();

            toConvert.Add("type", "GOOD_TO_GO");

            return toConvert.ToJson();

        }

        public static string R_SERVER_MESSAGE(string title, string content)
        {

            Dictionary<string, object> toConvert = new Dictionary<string, object>();

            toConvert.Add("type", "SERVER_MESSAGE");
            toConvert.Add("title", title);
            toConvert.Add("content", content);

            return toConvert.ToJson();

        }

        public static string R_PLAYER_LEFT(Player leaver)
        {

            Dictionary<string, object> toConvert = new Dictionary<string, object>();

            toConvert.Add("type", "PLAYER_LEFT");
            toConvert.Add("name", leaver.name);
            toConvert.Add("id", leaver.id);

            return toConvert.ToJson();

        }

        public static string R_PLAYER_JOINED(Player joiner)
        {

            Dictionary<string, object> toConvert = new Dictionary<string, object>();

            toConvert.Add("type", "PLAYER_JOINED");
            toConvert.Add("name", joiner.name);
            toConvert.Add("id", joiner.id);

            return toConvert.ToJson();

        }

        public static string R_BEGIN_MATCH(Room room)
        {

            Dictionary<string, object> toConvert = new Dictionary<string, object>();

            toConvert.Add("type", "BEGIN_MATCH");
            toConvert.Add("roomid", room.roomID);

            return toConvert.ToJson();

        }

        public static string R_ROOM_PLAYERS(List<Friend> players)
        {

            Dictionary<string, object> toConvert = new Dictionary<string, object>();

            toConvert.Add("type", "ROOM_PLAYERS");
            toConvert.Add("players", players);

            return toConvert.ToJson();

        }

        public static string R_START_MATCH(Room room)
        {

            Dictionary<string, object> toConvert = new Dictionary<string, object>();

            toConvert.Add("type", "START_MATCH");
            toConvert.Add("roomid", room.roomID);

            return toConvert.ToJson();

        }

        public static string R_UPDATE_MARIO_POS(string playerID, object msi)
        {

            Dictionary<string, object> toConvert = new Dictionary<string, object>();

            toConvert.Add("type", "UPDATE_MARIO_POS");
            toConvert.Add("id", playerID);
            toConvert.Add("msi", msi);

            return toConvert.ToJson();

        }
        public static string R_UPDATE_MARIO_SIZE(string playerID, object newSize)
        {

            Dictionary<string, object> toConvert = new Dictionary<string, object>();

            toConvert.Add("type", "UPDATE_MARIO_SIZE");
            toConvert.Add("id", playerID);
            toConvert.Add("newsize", newSize);

            return toConvert.ToJson();

        }

        public static string R_BOUNCE_BLOCK(string playerID, object parentName)
        {

            Dictionary<string, object> toConvert = new Dictionary<string, object>();

            toConvert.Add("type", "BOUNCE_BLOCK");
            toConvert.Add("id", playerID);
            toConvert.Add("parentname", parentName);

            return toConvert.ToJson();

        }

        public static string R_ACTIVATE_BLOCK(string playerID, object parentName, string powerupID, object triggererSize)
        {

            Dictionary<string, object> toConvert = new Dictionary<string, object>();

            toConvert.Add("type", "ACTIVATE_BLOCK");
            toConvert.Add("id", playerID);
            toConvert.Add("parentname", parentName);
            toConvert.Add("powerupid", powerupID);
            toConvert.Add("triggerersize", triggererSize);

            return toConvert.ToJson();

        }

        public static string R_TAKE_POWERUP(string playerID, object powerupID)
        {

            Dictionary<string, object> toConvert = new Dictionary<string, object>();

            toConvert.Add("type", "TAKE_POWERUP");
            toConvert.Add("id", playerID);
            toConvert.Add("powerupid", powerupID);

            return toConvert.ToJson();

        }

        public static string R_STOMP_ENEMY(string playerID, object enemyName, object enemyPos)
        {

            Dictionary<string, object> toConvert = new Dictionary<string, object>();

            toConvert.Add("type", "STOMP_ENEMY");
            toConvert.Add("id", playerID);
            toConvert.Add("enemyname", enemyName);
            toConvert.Add("enemypos", enemyPos);

            return toConvert.ToJson();

        }

        #endregion

    }

    public class MongoCRUD
    {

        private IMongoDatabase db;

        public MongoCRUD(string database)
        {

            var client = new MongoClient();
            db = client.GetDatabase(database);

        }

        public PlayerDB GetPlayerDBByID<PlayerDB>(string table, string playerIDToGet)
        {

            var collection = db.GetCollection<PlayerDB>(table);
            var filter = Builders<PlayerDB>.Filter.Eq("id", playerIDToGet);
            try
            {
                return collection.Find(filter).First();
            }
            catch(InvalidOperationException)
            {
                return default;
            }
            
        }

        public void InsertPlayer<PlayerDB>(string table, PlayerDB playerToInsert)
        {

            var collection = db.GetCollection<PlayerDB>(table);
            collection.InsertOne(playerToInsert);

        }

        public void UpdatePlayer<PlayerDB>(FilterDefinition<PlayerDB> filter, UpdateDefinition<PlayerDB> update)
        {

            var collection = db.GetCollection<PlayerDB>("Users");
            collection.UpdateOne(filter, update);

        }

        public void UpdatePlayerGameRecords(string playerID, int kills ,bool won)
        {

            var collection = db.GetCollection<PlayerDB>("Users");
            var filter = Builders<PlayerDB>.Filter.Eq("id", playerID);
            var update = Builders<PlayerDB>.Update.Inc("games", 1).Inc("kills", kills).Inc(won? "wins" : "deaths", 1);
            collection.UpdateOne(filter, update);

        }

        public void RemovePlayer<PlayerDB>(FilterDefinition<PlayerDB> filter)
        {

            var collection = db.GetCollection<PlayerDB>("Users");
            collection.DeleteOne(filter);

        }

        public bool PlayerExists<PlayerDB>(FilterDefinition<PlayerDB> filter)
        {

            var collection = db.GetCollection<PlayerDB>("Users");
            return collection.Find(filter).Any();

        }
        
    }

    class PlayerDB
    {

        [BsonId]
        public string id { get; set; }

        public string name, deviceid;

        public int games, wins, kills, deaths;

        public List<string> friendids = new List<string>(), friendrequestids = new List<string>(),
            blockedids = new List<string>();

    }

    public class Player
    {

        public string id, token;
        public string name = "Player";
        public bool loaded = false, online = true, ready = false;
        public string address;
        public Queue<Dictionary<string, object>> clientRequestsQueue = new Queue<Dictionary<string, object>>();
        public string buffer;
        public Room playerRoom;
        public Team team;

        #region GameSpecific

        #endregion

    }

    public class Missile
    {

        public string id;
        public Player shooter, target;
        public bool active = true;

    }

    public class LobbyRoom
    {

        public string roomID;

        public Team[] lobbyRoomTeams;
        public List<Player> lobbyRoomPlayers = new List<Player>();
        public MatchType matchType;
        public bool matchMaking = false;

        public Player host;
        public Room gameRoom;

        public void BroadcastMessageToRoomPlayers(string messageToSend)
        {

            try
            {

                foreach (Player player in lobbyRoomPlayers)
                    Server.SendMessageToPlayer(player, messageToSend);

            }
            catch (Exception ex)
            {

                Console.WriteLine("Couldn't broadcast message to lobby room players over TCP. " + ex, Color.Red);

            }
        }

    }
    
    public class Room
    {

        public string roomID;
        public List<Player> roomPlayers = new List<Player>();
        public bool matchMaking = true, matchStarted = false, matchOngoing = false, matchBegan = false;
        public int maxRoomPlayers = 2;

        #region GameSpecific

        #endregion

        public Room()
        {

            roomID = Guid.NewGuid().ToString();

        }

        public void AddPlayerToRoom(Player player)
        {

            roomPlayers.Add(player);

            player.playerRoom = this;

            Console.WriteLine($"Added player {player.name} to room {roomID}.");

            BroadcastMessageToRoomPlayers(Methods.R_PLAYER_JOINED(player));
            if (roomPlayers.Count == maxRoomPlayers)
            {

                //Start match!
                Console.WriteLine($"Starting match!");
                BeginMatch();

            }
            else
                Console.WriteLine($"can't start match yet. Room players count: {roomPlayers.Count}");

        }

        public void BroadcastRoomPlayers()
        {

            List<Friend> roomPlayersAsFriendList = new List<Friend>();

            foreach (Player p in roomPlayers)
            {

                Friend f = new Friend
                {

                    name = p.name,
                    id = p.id

                };

                roomPlayersAsFriendList.Add(f);

            }

            BroadcastMessageToRoomPlayers(Methods.R_ROOM_PLAYERS(roomPlayersAsFriendList));

        }
        
        public void RemovePlayerFromRoom(Player playerToRemove)
        {

            Console.WriteLine($"Removing player {playerToRemove.name} from room..");

            bool playerRemoved = roomPlayers.Remove(playerToRemove);

            if (roomPlayers.Count == 0)
                Methods.rooms.Remove(this);
            else
                BroadcastMessageToRoomPlayers(Methods.R_PLAYER_LEFT(playerToRemove));

        }

        public void BeginMatch()
        {

            try
            {

                matchMaking = false;
                matchBegan = true;

                BroadcastRoomPlayers();

                BroadcastMessageToRoomPlayers(Methods.R_BEGIN_MATCH(this));

                Console.WriteLine("Match began in room " + roomID + " which has players: " + roomPlayers.Count, Color.DodgerBlue);

            }
            catch (Exception ex)
            {

                Console.WriteLine("Couldn't begin match. Exception: " + ex, Color.Red);

            }

        }

        public void PlayerLoaded(Player player)
        {

            player.loaded = true;

            bool matchCanStart = true;

            foreach(Player p in roomPlayers)
            {

                if (!p.loaded)
                {

                    matchCanStart = false;
                    break;

                }
            }

            if (matchCanStart)
            {

                StartMatch();

            }

        }

        void StartMatch()
        {

            matchStarted = true;

            BroadcastRoomPlayers();

            BroadcastMessageToRoomPlayers(Methods.R_START_MATCH(this));

        }

        public void BroadcastMessageToRoomPlayers(string messageToSend, List<Player> excludedPlayers = null)
        {

            try
            {

                List<Player> newList = new List<Player>(roomPlayers);

                foreach(Player player in newList)
                {

                    if (excludedPlayers != null)
                        if (excludedPlayers.Contains(player))
                            continue;

                    Server.SendMessageToPlayer(player, messageToSend);

                }

            }
            catch(Exception ex)
            {

                Console.WriteLine("Couldn't broadcast message to room players over TCP. " + ex, Color.Red);

            }
        }

        #region GameSpecificFunctions

        public void UpdatePos(Player player, object msi)
        {

            BroadcastMessageToRoomPlayers(Methods.R_UPDATE_MARIO_POS(player.id, msi));

        }

        public void UpdateSize(Player player, object newSize)
        {

            BroadcastMessageToRoomPlayers(Methods.R_UPDATE_MARIO_SIZE(player.id, newSize));

        }

        public void BounceBlock(Player player, object parentName)
        {

            BroadcastMessageToRoomPlayers(Methods.R_BOUNCE_BLOCK(player.id, parentName));

        }

        public void ActivateBlock(Player player, object parentName, object triggererSize)
        {

            string powerupID = Guid.NewGuid().ToString();
            BroadcastMessageToRoomPlayers(Methods.R_ACTIVATE_BLOCK(player.id, parentName, powerupID, triggererSize));

        }

        public void TakePowerup(Player player, object powerupID)
        {

            BroadcastMessageToRoomPlayers(Methods.R_TAKE_POWERUP(player.id, powerupID));

        }

        public void StompEnemy(Player player, object enemyName, object enemyPos)
        {

            BroadcastMessageToRoomPlayers(Methods.R_STOMP_ENEMY(player.id, enemyName, enemyPos));

        }

        #endregion

    }

    public class Friend
    {

        public string name, id;

    }

    public class Team
    {

        public List<Player> teamPlayers = new List<Player>();

    }

    public class MarioSyncInfo
    {

        public float marioSpeed, absSpeed;
        public bool isJumping, isSkidding, flipX;
        public object newPos;

    }

    public enum MatchType { Solo, Duo, Squad, BattleRoyale };

}