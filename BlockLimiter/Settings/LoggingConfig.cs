using NLog;
using NLog.Config;
using NLog.Targets;
using System;

namespace BlockLimiter.Settings
{
    public static class LoggingConfig
    {
        public static void Set()
        {
            try
            {
                var config = LogManager.Configuration;
                if (config == null)
                {
                    LogManager.Configuration = new LoggingConfiguration();
                    config = LogManager.Configuration;
                }

                var rules = config.LoggingRules;
                if (rules == null)
                {
                    config.LoggingRules = new List<LoggingRule>();
                    rules = config.LoggingRules;
                }

                for (int i = rules.Count - 1; i >= 0; i--)
                {
                    var rule = rules[i];

                    if (rule.LoggerNamePattern != "BlockLimiter") continue;
                    rules.RemoveAt(i);
                }

                var blockLimiterConfig = BlockLimiterConfig.Instance;
                if (blockLimiterConfig == null || string.IsNullOrEmpty(blockLimiterConfig.LogFileName))
                {
                    config.Reload();
                    return;
                }

                var logTarget = new FileTarget
                {
                    FileName = blockLimiterConfig.LogFileName,
                    Layout = "${longdate} ${uppercase:${level}} ${message}",
                    // Add other necessary properties for FileTarget
                };

                config.AddTarget("file", logTarget);
                config.LoggingRules.Add(new LoggingRule("BlockLimiter", LogLevel.Debug, logTarget));

                LogManager.Configuration = config;
                LogManager.ReconfigExistingLoggers();
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                Console.WriteLine($"Error setting up logging configuration: {ex.Message}");
            }
        }
    }
}