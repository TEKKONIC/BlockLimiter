using NLog;
using Nexus.API;
using Sandbox.Engine.Multiplayer;
using Sandbox.ModAPI;
using BlockLimiter.Settings;
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
        public static NexusAPI Api { get; } = new NexusAPI(BlockLimiterNexusModId);
        public static bool RunningNexus { get; private set; }

        public static void Init()
        {
            if (!BlockLimiterConfig.Instance.IsNexusSupportEnabled)
                return;

            var server = NexusAPI.GetThisServer(); // Get the server object
            _thisServerId = server.ServerId; // Access the ServerId property
            Log.Info("BlockLimiter -> Nexus integration has been initialized with serverID " + _thisServerId);

            if (!NexusAPI.IsRunningNexus())
                return;

            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(BlockLimiterNexusModId, ReceivePacket);
            Log.Error("Running Nexus!");

            RunningNexus = true;
            var thisServer = NexusAPI.GetAllServers().FirstOrDefault(x => x.ServerId == _thisServerId);
        }

        public NexusSupport()
        {
            _nexusAPI = new NexusAPI(BlockLimiterNexusModId);
        }

        public void SyncBlockLimits()
        {
            var limits = BlockLimiterConfig.Instance.BlockLimits;
        }

        private static void ReceivePacket(ushort handlerId, byte[] data, ulong senderId, bool fromServer)
        {
            var messageType = ""; // Determine the message type from data
            if (messageType == "BlockLimitsSync")
            {
                var limits = Deserialize<List<LimitItem>>(data); // Properly deserialize byte[] to List<LimitItem>
                if (limits != null)
                {
                    BlockLimiterConfig.Instance.BlockLimits = limits;
                    BlockLimiter.ResetLimits();
                }
            }
            else if (messageType == "PlayerLimitUsageSync")
            {
                var playerData = Deserialize<dynamic>(data); // Properly deserialize byte[] to dynamic
                if (playerData != null)
                {
                    string playerId = playerData.PlayerId;
                    int currentLimitUsage = playerData.CurrentLimitUsage;
                    var instance = new NexusSupport();
                    instance.UpdatePlayerLimitUsage(playerId, currentLimitUsage); // Use an instance reference
                }
            }
        }

        public void SyncPlayerLimitUsage(string playerId, int currentLimitUsage)
        {
            var data = new { PlayerId = playerId, CurrentLimitUsage = currentLimitUsage };
            _nexusAPI.SendCrossServerMessage("PlayerLimitUsageSync", data); // Correct the usage of CrossServerMessage
        }

        private void UpdatePlayerLimitUsage(string playerId, int currentLimitUsage)
        {
            SyncPlayerLimitUsage(playerId, currentLimitUsage);
        }

        private static T Deserialize<T>(byte[] data)
        {
            // Implement the logic to deserialize byte[] to the specified type T
            // This is just a placeholder example
            return default(T);
        }
    }
}
