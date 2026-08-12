using ET.Client;

namespace ET
{
    public static class RobotProgram
    {
        public static void Main()
        {
            Entry.Init();
            RobotModelMarker.Init();

            World.Instance.AddSingleton<CodeLoaderConfig, System.Reflection.Assembly[], string[]>(
                new[] { typeof(RobotModelMarker).Assembly },
                new[] { "Robot.Hotfix" });

            using AppRunner runner = new(new Init());
            runner.Run();
        }
    }
}
