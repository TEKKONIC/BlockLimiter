using System;
using System.Timers;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Session;
using VRage.Game.ModAPI;
using BlockLimiter.Settings;

namespace BlockLimiter
{
    public class RecountTimer
    {
        private readonly Timer _recountTimer;

        public RecountTimer()
        {
            _recountTimer = new Timer(Math.Max(BlockLimiterConfig.Instance.recountTimerInterval, 1) * 1000);
            _recountTimer.Elapsed += OnRecountTimerElapsed;
            _recountTimer.AutoReset = true;
            _recountTimer.Enabled = BlockLimiterConfig.Instance.IsRecountTimerEnabled;
        }


        private void OnRecountTimerElapsed(object sender, ElapsedEventArgs e)
        {
            BlockLimiter.ResetLimits();
        }

        public void Start()
        {
            _recountTimer.Start();
        }

        public void Stop()
        {
            _recountTimer.Stop();
        }
    }
}