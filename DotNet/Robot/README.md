# Robot Worker

Robot Worker 是独立进程的模拟客户端与压测工具。服务端通过 `App.dll` 启动，机器人通过 `Robot.App.dll` 启动。

## 代码结构

Robot 遵循 ET 的源码布局：业务源码统一位于 `Unity/Assets/Scripts`，
`DotNet/Robot` 只保留 App 入口和 .NET 构建项目。

```text
DotNet/Robot/
├── App/       # RobotProgram 与 Robot.App.csproj
├── Model/     # Robot.Model.csproj
└── Hotfix/    # Robot.Hotfix.csproj

Unity/Assets/Scripts/
├── Model/Robot/    # RobotManagerComponent 等模型
└── Hotfix/Robot/   # 登录、进图、AI、批次创建等逻辑
```

Robot 仍会复用基础 `Model.dll` 和 `Hotfix.dll`；这里的“独立”指进程、入口、机器人源码和发布目录已经分离，并不表示所有共享程序集都已最小化。

## 构建

从仓库根目录运行：

```bash
./Scripts/build-robot.sh
```

Release：

```bash
./Scripts/build-robot.sh Release
```

脚本使用 `--no-incremental`，按顺序强制构建：

1. `DotNet.Robot.Hotfix.csproj`
2. `DotNet.Robot.App.csproj`

并检查 `Bin/Robot.App.dll`、`Bin/Robot.Model.dll`、`Bin/Robot.Hotfix.dll` 均存在。

脚本使用 `--no-restore`；首次构建或依赖发生变化时，先执行：

```bash
dotnet restore DotNet/Robot/Hotfix/DotNet.Robot.Hotfix.csproj
```

## 运行

先启动 Server，再从 `Bin` 运行：

```bash
cd Bin
dotnet Robot.App.dll \
  --AppType=RobotWorker \
  --StartConfig=StartConfig/Localhost \
  --RobotCount=10 \
  --RobotInterval=2000 \
  --RobotAccountPrefix=Robot \
  --Console=0 \
  --LogLevel=3
```

参数：

| 参数 | 默认值 | 说明 |
|---|---:|---|
| `RobotCount` | `1` | 本批机器人数量，必须大于 0 |
| `RobotInterval` | `2000` | 相邻机器人创建间隔（毫秒），不能小于 0 |
| `RobotAccountPrefix` | `Robot` | 账号前缀，实际账号为 `<prefix>_<index>` |

## 成功与失败统计

每 10 个机器人输出一次 Info，最后始终输出汇总：

```text
robot batch progress: prefix=Robot processed=10 total=10 ready=10 failed=0
robot batch complete: prefix=Robot total=10 ready=10 failed=0
```

只有以下步骤全部成功才计入 `ready`：

```text
Robot Fiber 创建
→ 登录 Realm/Gate
→ 请求进入地图
→ 等待场景切换成功
→ 创建 AIComponent
→ FiberManager.Create 返回
```

单个机器人失败时记录 Error、增加 `failed`，并继续创建后续机器人。

Debug 日志在高负载下可能不完整，不能用 Realm Debug 条数或逐机器人 Debug 条数统计成功数量。

## 独立发布

```bash
./Scripts/publish.sh
```

Robot 包位于：

```text
Publish/Robot/
├── Bin/Robot.App.dll
├── Bin/Robot.Hotfix.dll
├── Bin/Robot.Model.dll
└── Config/
```

该目录不包含服务端入口 `App.dll`。

## 自动化集成测试

前置条件：

- MongoDB 监听 `127.0.0.1:27017`。
- ET Localhost 配置所需端口未被占用。
- 本机提供 `mongosh`、`lsof` 和 Bash。

运行：

```bash
./Scripts/test-integration.sh
```

测试会重新构建并发布，然后验证 10 个机器人完整就绪、Server/Robot SIGTERM 退出码为 0、TCP/UDP 端口全部释放。

## 已知边界

- 尚未将 EnterMap 故障注入固化为永久自动化用例；异常传播已经通过临时注入实测。
- 尚未验证特定 Linux RID 发布。
- Robot 仍依赖完整基础 Model/Hotfix 程序集。
