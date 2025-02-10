using NLog;
using Nexus.API;
using Sandbox.Engine.Multiplayer;
using Sandbox.ModAPI;
using BlockLimiter.Settings;
using BlockLimiter.Utility;
using VRage.Game;
using VRageMath;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlockLimiter.Utility
{
    public class NexusSupport
    {
        private readonly NexusAPI _nexusAPI;
        private const ushort BlockLimiterNexusModId = 0x24fc;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static int _thisServerId = -1;
        public static NexusAPI Api { get; } = new NexusAPI();
        public static bool RunningNexus { get; private set; }


        #region Initalization
        public static void Init()
        {
            if (!BlockLimiterConfig.Instance.IsNexusSupportEnabled)
                return;
            if (Api.IsRunningNexus())
            {
                Log.Info("BlockLimiter -> Nexus is not running");
                return;
            }
            else
            {
                Log.Info("BlockLimiter -> Nexus is running");
            }
                

            var server = Api.GetThisServer(); // Get the server object from NexusAPI
            if (server != null)
            {
                _thisServerId = server.ServerID; // Get the server ID
                Log.Info("BlockLimiter -> Nexus integration has been initialized with serverID " + _thisServerId);
            }
            else
            {
                Log.Error("Failed to get the server object from NexusAPI");
                return;
            }

            if (Api.IsRunningNexus())
                return;

            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(BlockLimiterNexusModId, ReceivePacket);
            Log.Error("Running Nexus!");

            RunningNexus = true;
            var thisServer = Api.GetAllServers().FirstOrDefault(x => x.ServerID == _thisServerId);
            if (thisServer == null)
            {
                Log.Error("Failed to get this server from Nexus");
                return;
            }
        }

        public void StartNexusNetworking()
        {
            NexusAPI myNexusAPI = new NexusAPI();
            long MyModID = 62154; // Your custom nexusModMsgID

            MyAPIGateway.Utilities.RegisterMessageHandler(MyModID, MessageHandler);

            // Example custom byte array
            byte[] myModCustomData = new byte[0]; // Initialize the byte array

            // Example send custom mod msg
            // Send a custom message to a specific server
            myNexusAPI.SendModMsgToServer(myModCustomData, MyModID, 3);

            // Send a custom message to ALL ONLINE servers
            myNexusAPI.SendModMsgToAllServers(myModCustomData, MyModID);
        }

        #endregion

        public NexusSupport()
        {
            _nexusAPI = new NexusAPI();
        }

        public void SyncBlockLimits()
        {
            var limits = BlockLimiterConfig.Instance.BlockLimits;
            if (limits == null)
            {
                Log.Error("BlockLimiter -> BlockLimits is null");
                return;
            }
            else if (limits.Count == 0)
            {
                Log.Error("BlockLimiter -> BlockLimits is empty");
                return;
            }
            else
            {
                BlockLimiter.ResetLimits();
                Api.SendCrossServerMessage("Sigma Draconis BlockLimiter Syncing", limits);
                Log.Info("BlockLimiter -> Syncing block limits with Nexus");
            }
        
        }

        private static void ReceivePacket(ushort handlerId, byte[] data, ulong senderId, bool fromServer)
        {
            var messageType = ""; 
            if (messageType == "Sigma Draconis BlockLimiter --> Syncing")
            {
                var limits = Deserialize<List<LimitItem>>(data); 
                if (limits != null)
                {
                    BlockLimiterConfig.Instance.BlockLimits = limits;
                    BlockLimiter.ResetLimits();
                }
            }
            else if (messageType == "Sigma Draconis BlockLimiter --> Player Limit Usage Sync")
            {
                var playerData = Deserialize<dynamic>(data); 
                if (playerData != null)
                {
                    string playerId = playerData.PlayerId;
                    int currentLimitUsage = playerData.CurrentLimitUsage;
                    var instance = new NexusSupport();
                    instance.UpdatePlayerLimitUsage(playerId, currentLimitUsage); 
                }
            }
        }

        private static void MessageHandler(object obj)
        {
            // Implement the logic to handle the message

            byte[] data = (byte[])obj;
            Log.Info("Message received: " + BitConverter.ToString(data));
        }

        public void SyncPlayerLimitUsage(string playerId, int currentLimitUsage)
        {
            var data = new { PlayerId = playerId, CurrentLimitUsage = currentLimitUsage };
            var SyncPlayerLimitUsage = BlockLimiterConfig.Instance.SyncPlayerLimitUsage;
            if (SyncPlayerLimitUsage == null)
            {
                Log.Error("BlockLimiter -> SyncPlayerLimitUsage is null");
                return;
            }
            else if (SyncPlayerLimitUsage.Count == 0)
            {
                Log.Error("BlockLimiter -> SyncPlayerLimitUsage is empty");
                return;
            }
            else
            {
                BlockLimiter.ResetLimits();
            Api.SendCrossServerMessage("Sigma Draconis BlockLimiter --> Player Limit Usage Sync", data); 
        }

        private void UpdatePlayerLimitUsage(string playerId, int currentLimitUsage)
        {
            SyncPlayerLimitUsage(playerId, currentLimitUsage);
        }

        private static T Deserialize<T>(byte[] data)
        {
            // Implement the logic to deserialize byte[] to the specified type T
            
            return default(T);
        }
    }
}
