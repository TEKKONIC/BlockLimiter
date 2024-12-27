using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlockLimiter.Settings;
using Sandbox.Game.Entities.Blocks;
using Torch.Managers.PatchManager;
using VRage.Game;
using VRage.Network;
using BlockLimiter.Utility;
using NLog;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using Torch;
using Torch.API.Managers;
using Torch.Managers;
using Torch.Managers.ChatManager;
using VRageMath;

namespace BlockLimiter.Patch
{
    [PatchShim]
    public static class ProjectionPatch
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly MethodInfo NewBlueprintMethod = typeof(MyProjectorBase).GetMethod("OnNewBlueprintSuccess", BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo RemoveBlueprintMethod = typeof(MyProjectorBase).GetMethod("OnRemoveProjectionRequest", BindingFlags.NonPublic | BindingFlags.Instance);

        public static void Patch(PatchContext ctx)
        {
            try
            {
                ctx.GetPattern(typeof(MyProjectorBase).GetMethod("InitializeClipboard", BindingFlags.Instance | BindingFlags.NonPublic)).
                    Prefixes.Add(typeof(ProjectionPatch).GetMethod(nameof(PrefixInitializeClipboard), BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic));
            }
            catch (Exception e)
            {
                Log.Error(e.StackTrace, "Patching Failed");
            }
        }

        /// <summary>
        /// Checks projections before showing and remove any nonAllowed blocks.
        /// </summary>
        /// <param name="__instance"></param>
        /// <returns></returns>
        private static bool PrefixInitializeClipboard(MyProjectorBase __instance)
        {
            if (!BlockLimiterConfig.Instance.EnableLimits) return true;

            var remoteUserId = MyEventContext.Current.Sender.Value;
            var grid = __instance.CubeGrid;

            if (__instance.Clipboard.CopiedGrids == null || __instance.Clipboard.CopiedGrids.Count == 0) return true;

            var copiedGrid = __instance.Clipboard.CopiedGrids[0];
            
            if (copiedGrid == null || grid == null) return true;

            var player = MySession.Static.Players.TryGetPlayerBySteamId(remoteUserId);
            
            var projectedBlocks = copiedGrid.CubeBlocks;

            if (player == null || projectedBlocks == null || projectedBlocks.Count == 0)
            {
                return true;
            }

            if (Utilities.IsExcepted(player))
            {
                return true;
            }

            var playerId = player.Identity.IdentityId;
            if ((Grid.IsSizeViolation(copiedGrid) && BlockLimiterConfig.Instance.BlockType > BlockLimiterConfig.BlockingType.Warn)
                || BlockLimiterConfig.Instance.MaxBlockSizeProjections < 0 || (projectedBlocks.Count > BlockLimiterConfig.Instance.MaxBlockSizeProjections 
                                                                               && BlockLimiterConfig.Instance.MaxBlockSizeProjections > 0) 
                || (Grid.CountViolation(copiedGrid,playerId) && BlockLimiterConfig.Instance.BlockType > BlockLimiterConfig.BlockingType.Warn))
            {
                NetworkManager.RaiseEvent(__instance, RemoveBlueprintMethod, MyEventContext.Current.Sender);
                Utilities.ValidationFailed();
                var msg1 = Utilities.GetMessage(BlockLimiterConfig.Instance.DenyMessage, new List<string>{"Null"}, "SizeViolation", grid.CubeBlocks.Count);
                if (remoteUserId != 0 && MySession.Static.Players.IsPlayerOnline(player.Identity.IdentityId))
                    BlockLimiter.Instance.Torch.CurrentSession.Managers.GetManager<ChatManagerServer>()?
                        .SendMessageAsOther(BlockLimiterConfig.Instance.ServerName, msg1, Color.Red, remoteUserId);
                
                BlockLimiter.Instance.Log.Info($"Projection blocked from {player.DisplayName} due to size limit");
                
                return false;
            }

            var limits = new HashSet<LimitItem>();
            limits.UnionWith(BlockLimiterConfig.Instance.AllLimits.Where(x => x.RestrictProjection));

            if (limits.Count == 0)
            {
                return true;
            }

            int removalCount = 0;
            int count = 0;
            List<string> removedList = new List<string>();
            string limitName = string.Empty;

            var playerFaction = MySession.Static.Factions.GetPlayerFaction(playerId);

            foreach (var limit in limits)
            {
                var pBlocks = projectedBlocks.Where(block => limit.IsMatch(block)).ToList();
                var pCount = pBlocks.Count;
                var gCount = GridCache.GetGridCache(grid).CountBlocks(limit);
                var fCount = playerFaction?.CountBlocks(limit) ?? 0;

                foreach (var block in pBlocks)
                {
                    if (Math.Abs(pBlocks.Count + pCount - removalCount) <= limit.Limit &&
                        Math.Abs(gCount + pBlocks.Count - removalCount) <= limit.Limit &&
                        Math.Abs(fCount + pBlocks.Count - removalCount) <= limit.Limit)
                    {
                        break;
                    }

                    removalCount++;
                    count++;
                    projectedBlocks.Remove(block);
                    var blockDef = Utilities.GetDefinition(block).ToString().Substring(16);
                    if (removedList.Contains(blockDef)) continue;
                    removedList.Add(blockDef);
                    limitName = limit.Name;
                }
            }

            if (count == 0) return true;

            BlockLimiter.Instance.Log.Info($"Removed {count} blocks from projector set by {player.DisplayName}");

            NetworkManager.RaiseEvent(__instance, RemoveBlueprintMethod);

            ((IMyProjector)__instance).SetProjectedGrid(copiedGrid);

            Utilities.TrySendProjectionDenyMessage(removedList, limitName, remoteUserId, count);

            return false;
        }
    }
}