using System;

namespace ET.Client
{
    public static partial class EnterMapHelper
    {
        public static async ETTask EnterMapAsync(Scene root)
        {
            await root.GetComponent<ClientSenderComponent>().Call(C2G_EnterMap.Create());

            // 等待场景切换完成；任何失败必须向上传播，否则批次统计会将失败机器人误计为 ready。
            Wait_SceneChangeFinish result = await root.GetComponent<ObjectWait>().Wait<Wait_SceneChangeFinish>();
            if (result.Error != WaitTypeError.Success)
            {
                throw new System.Exception($"enter map failed: wait error={result.Error}");
            }

            EventSystem.Instance.Publish(root, new EnterMapFinish());
        }

        public static async ETTask Match(Fiber fiber)
        {
            try
            {
                G2C_Match g2CEnterMap = await fiber.Root.GetComponent<ClientSenderComponent>().Call(C2G_Match.Create()) as G2C_Match;
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }
    }
}
