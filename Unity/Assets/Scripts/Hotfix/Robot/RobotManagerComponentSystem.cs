namespace ET.Client
{
    [EntitySystemOf(typeof(RobotManagerComponent))]
    [FriendOf(typeof(RobotManagerComponent))]
    public static partial class RobotManagerComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RobotManagerComponent self)
        {
        }

        [EntitySystem]
        private static void Destroy(this RobotManagerComponent self)
        {
            foreach (int fiberId in self.Robots)
            {
                FiberManager.Instance.Remove(fiberId).Coroutine();
            }
        }

        public static async ETTask NewRobot(this RobotManagerComponent self, string account)
        {
            int fiberId = await FiberManager.Instance.Create(SchedulerType.ThreadPool, self.Zone(), SceneType.Robot, account);
            self.Robots.Add(fiberId);
        }
    }
}
