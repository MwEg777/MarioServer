using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Linq;
using Console = Colorful.Console;
using System.Text.RegularExpressions;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using WatsonWebsocket;

namespace MythServer
{
    public class Server
    {

        Methods methods = new Methods();
        static Dictionary<string, MethodInfo> reflectedMethods = new Dictionary<string, MethodInfo>();
        public static MongoCRUD db;
        public static WatsonWsServer wss;

        //public static WebSocketServer wssv;

        static bool _quitFlag = false;

        public Server(string ip, int port)
        {

            db = new MongoCRUD("Players");

            wss = new WatsonWsServer(ip, port, false);

            wss.ClientConnected += ClientConnected;
            wss.ClientDisconnected += ClientDisconnected;
            wss.MessageReceived += Player_MessageReceived;

            wss.Start();

            while (!_quitFlag)
            {
                Thread.Sleep(1);

            }

        }

        void ClientConnected(object sender, ClientConnectedEventArgs args)
        {

            Console.WriteLine("Client connected: " + args.IpPort);

            Thread p = new Thread( () => PlayerThread(sender, args.IpPort) );
            p.Start();

        }
        
        void ClientDisconnected(object sender, ClientDisconnectedEventArgs args)
        {

            Console.WriteLine("Client disconnected: " + args.IpPort);

            //Try to find the players in the players list
            Player player = Methods.players.Find(p => p.address == args.IpPort);

            //If found player, remove him from room then from server players
            if (player != null)
            {

                Console.WriteLine($"Removing player {player.name} from server.");

                if (player.playerRoom != null)
                    player.playerRoom.RemovePlayerFromRoom(player);

                Methods.players.Remove(player);

            }

        }

        void Player_MessageReceived(object sender, MessageReceivedEventArgs e)
        {

            Player player = Methods.playerAddressesDict[e.IpPort];

            string clientMessage = Encoding.UTF8.GetString(e.Data);

            //Console.WriteLine($"Got message from player {player.name}");

            try
            {

                string[] messages = clientMessage.Split(new string[] { "$eof$" }, StringSplitOptions.None);

                foreach (string message in messages)
                {

                    try
                    {

                        if (string.IsNullOrEmpty(message)) //Skip empty messages
                            continue;

                        Dictionary<string, object> clientMessageDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
                        if (clientMessageDict.ContainsKey("type"))
                            player.clientRequestsQueue.Enqueue(clientMessageDict);
                        else
                            Console.WriteLine("Message doesn't contain type. It's most likely not from a game player!", System.Drawing.Color.Red);

                    }
                    catch (Exception)
                    {

                        player.buffer += message.EndsWith("}") ? (message + "$eof$") : message;
                        //Console.WriteLine("Message parsing problem. Trying to complete it from previous buffer. \n Exception: " + exx, System.Drawing.Color.Red);

                        if (!string.IsNullOrEmpty(player.buffer))
                        {

                            string[] messagesBuffer = player.buffer.Split(new string[] { "$eof$" }, StringSplitOptions.None);

                            foreach (string msg in messagesBuffer)
                            {

                                try
                                {

                                    if (string.IsNullOrEmpty(msg)) //Skip empty messages
                                        continue;

                                    Dictionary<string, object> clientMessageDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(msg);
                                    if (clientMessageDict.ContainsKey("type"))
                                        player.clientRequestsQueue.Enqueue(clientMessageDict);
                                    else
                                        Console.WriteLine("Message doesn't contain type. It's most likely not from a game player!", System.Drawing.Color.Red);

                                }
                                catch (Exception)
                                {

                                    //Console.WriteLine("Couldn't parse one buffer message. \n Exception: " + exx2, System.Drawing.Color.Red);

                                }

                            }

                        }

                    }
                }

            }
            catch (Exception ex)
            {

                Console.WriteLine("Couldn't parse client response. " + "\n Exception is: " + ex.ToString() + ", Client response is: " + clientMessage, System.Drawing.Color.Red);

            }

        }

        public void PlayerThread(object obj, string ipPort)
        {

            //Remove old players with same address and port
            bool removedFromServerPlayers = Methods.players.Remove(Methods.players.Find(p => p.address == ipPort));
            bool removedFromDictPlayers = Methods.playerAddressesDict.Remove(ipPort);

            //Create a new player, assign the IP and Port combination to him
            Player player = new Player { address = ipPort };

            //Add him to the server's players list
            Methods.players.Add(player);
            Methods.playerAddressesDict.Add(ipPort, player);

            while (true)
            {

                if (!wss.IsClientConnected(ipPort))
                    break;

                try
                {

                    if (player.clientRequestsQueue.Count > 0)
                    {

                        Dictionary<string, object> request = player.clientRequestsQueue.Dequeue();

                            try
                            {

                            if (request != null)
                                ProcessClientMessage(player, request);
                            }
                            catch (Exception exo)
                            {

                                try
                                {

                                    Console.WriteLine("Problem processing a client message or dequeuing message. Exception: " + exo, System.Drawing.Color.Red);
                                    Console.WriteLine("Message is:");
                                Console.WriteLine("----------------------------------------");

                                foreach (KeyValuePair<string, object> kvp in request)
                                {

                                    Console.WriteLine($"Key: {kvp.Key}, Value: {kvp.Value}");

                                }

                                }
                                catch (Exception exo2)
                                {

                                    Console.WriteLine("Problem processing a client message, Probably message is corrupted from client side. \n Exception: " + exo2, System.Drawing.Color.Red);

                                }

                            }

                    }

                }
                catch (InvalidOperationException ex)
                {

                    Console.WriteLine("Modified exception in player requests. " + ex);
                
                }

            }

            Console.WriteLine("Client stopped listening. Exiting thread..");

        }

        public static async void SendMessageToPlayer(Player player, string message)
        {

            try
            { 

                await wss.SendAsync(player.address, message);
                //Console.WriteLine($"Message should be sent. Address: {player.address}, message: {message}");

            }
            catch (Exception ex)
            {

                Console.WriteLine("Couldn't send TCP message. \n Exception: " + ex, System.Drawing.Color.Red);

            }

        }

        public static void ProcessClientMessage(Player player, Dictionary<string, object> clientMessage)
        {

            string typeName = clientMessage["type"] as string;

            if (reflectedMethods.TryGetValue(typeName, out MethodInfo mthd))
                mthd.Invoke(Methods.instance, new object[] { player, clientMessage });
            else
            {
            
                Type thisType = Methods.instance.GetType();
                MethodInfo theMethod = thisType.GetMethod(typeName);
                reflectedMethods.Add(typeName, theMethod);
                theMethod.Invoke(Methods.instance, new object[] { player, clientMessage });

            }

        }

    }
}
