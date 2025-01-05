using Nexus.API;
using BlockLimiter.Utility;
using BlockLimiter.Settings;
using System;
using System.Collections.Generic;

namespace BlockLimiter.Utility
{
    public class NexusSync
    {
        private readonly NexusAPI _nexusAPI;

        public NexusSync()
        {
            _nexusAPI = NexusAPI.Instance;
            _nexusAPI.OnMessageReceived += OnMessageReceived;
        }

        public void SyncBlockLimits()
        {
            var limits = BlockLimiterConfig.Instance.BlockLimits;
            _nexusAPI.SendMessageToAll("BlockLimiterSync", limits);
        }

        public void SyncPlayerLimitUsage(string playerId, int currentLimitUsage)
        {
            var data = new { PlayerId = playerId, CurrentLimitUsage = currentLimitUsage };
            _nexusAPI.SendMessageToAll("PlayerLimitUsageSync", data);
        }

        private void OnMessageReceived(string messageType, object data)
        {
            if (messageType == "BlockLimiterSync")
            {
                var limits = data as List<LimitItem>;
                if (limits != null)
                {
                    BlockLimiterConfig.Instance.BlockLimits = limits;
                    BlockLimiter.ResetLimits();
                }
            }
            else if (messageType == "PlayerLimitUsageSync")
            {
                var playerData = data as dynamic;
                if (playerData != null)
                {
                    string playerId = playerData.PlayerId;
                    int currentLimitUsage = playerData.CurrentLimitUsage;
                    UpdatePlayerLimitUsage(playerId, currentLimitUsage);
                }
            }
        }

        private void UpdatePlayerLimitUsage(string playerId, int currentLimitUsage)
        {
            // Implement the logic to update the player's current limit usage in your system
            // For example, update the player's limit usage in a shared database or in-memory store
            // Call this method whenever a player's limit usage changes
            var nexusSync = new NexusSync();
            nexusSync.SyncPlayerLimitUsage(playerId, currentLimitUsage);
        }
    }

    public class NexusAPI
    {
        
        private static NexusAPI _instance;
        public static NexusAPI Instance => _instance ??= new NexusAPI();

        public event Action<string, object> OnMessageReceived;

        public void SendMessageToAll(string messageType, object data)
        {
            // Implementation for sending message to all clients
        }

        protected virtual void RaiseMessageReceived(string messageType, object data)
        {
            OnMessageReceived?.Invoke(messageType, data);
        }
    }
}
