using System;

namespace ET.Client
{
    [Event(SceneType.Main)]
    public class EntryEvent3_InitRobotWorker: AEvent<Scene, EntryEvent3>
    {
        protected override async ETTask Run(Scene root, EntryEvent3 args)
        {
            if (Options.Instance.AppType != AppType.RobotWorker)
            {
                return;
            }

            if (Options.Instance.RobotCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(Options.RobotCount), "RobotCount must be greater than zero");
            }

            if (Options.Instance.RobotInterval < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(Options.RobotInterval), "RobotInterval cannot be negative");
            }

            RobotManagerComponent manager = root.AddComponent<RobotManagerComponent>();
            TimerComponent timer = root.GetComponent<TimerComponent>();
            int ready = 0;
            int failed = 0;
            for (int i = 0; i < Options.Instance.RobotCount; ++i)
            {
                string account = $"{Options.Instance.RobotAccountPrefix}_{i}";
                try
                {
                    await manager.NewRobot(account);
                    ++ready;
                }
                catch (Exception e)
                {
                    ++failed;
                    Log.Error($"robot start failed: account={account}\n{e}");
                }

                int processed = i + 1;
                if (processed % 10 == 0 || processed == Options.Instance.RobotCount)
                {
                    Log.Info($"robot batch progress: prefix={Options.Instance.RobotAccountPrefix} processed={processed} total={Options.Instance.RobotCount} ready={ready} failed={failed}");
                }

                if (Options.Instance.RobotInterval > 0 && i + 1 < Options.Instance.RobotCount)
                {
                    await timer.WaitAsync(Options.Instance.RobotInterval);
                }
            }

            Log.Info($"robot batch complete: prefix={Options.Instance.RobotAccountPrefix} total={Options.Instance.RobotCount} ready={ready} failed={failed}");
        }
    }
}
