using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace ServerTest
{
    class ServerHandle
    {
        public static void WelcomeRecieved(int fromClient, Packet packet)
        {
            int clientIdCheck = packet.ReadInt();
            string username = packet.ReadString();

            Console.WriteLine($"{Server.clients[fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully");
            if(fromClient != clientIdCheck)
            {
                Console.WriteLine($"Player \"{username}\" (ID: {fromClient} has assumed the wrong client ID: {clientIdCheck})");
            }

            bool firstTime = !Server.loginInfo.TryGetValue(username, out int clientID);

            if (!firstTime)
            {
                Console.WriteLine($"Existing matching player data found for player: \"{username}\".");
                try
                {
                    Server.clients[fromClient].SendIntoGame(Server.saveData[Server.loginInfo[username]]);
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"Login failed- exception: {ex}");
                }
            }
            else
            {
                Console.WriteLine("No existing matching username; new player data created");
                int newID = Server.loginInfo.Keys.Count + 1;
                Server.loginInfo.Add(username, newID);
                Server.clients[fromClient].SendIntoGame(username);
            }
        }
        public static void messageReceived(int fromClient, Packet packet)
        {
            string msg = packet.ReadString();

            Console.WriteLine($"Message from client {fromClient}: {msg}");
        }
        public static void InputsReceived(int fromClient, Packet packet)
        {
            bool[] inputs = new bool[packet.ReadInt()];
            for(int i = 0; i<inputs.Length; i++)
            {
                inputs[i] = packet.ReadBool();
            }
            Quaternion rotation = packet.ReadQuaternion();

            Server.clients[fromClient].player.SetInput(inputs, rotation);
        }
    }
}
