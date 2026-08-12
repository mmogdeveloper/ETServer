# ETServer

ETServer 是从 [ET release8.1](https://github.com/egametang/ET/tree/release8.1) 分离出的 .NET 服务端项目，并将机器人压测工具拆分为独立进程。

当前开发期输出仍位于共享的 `Bin/` 目录；发布时通过脚本生成相互独立的 Server 和 Robot 目录。

## 当前边界

- `App.dll`：服务端入口。
- `Robot.App.dll`：独立机器人压测入口。
- 服务端不再提供 `CreateRobot` 控制台命令，也不再持有 `RobotManagerComponent`。
- Robot 代码位于 `DotNet/Robot/{App,Model,Hotfix}`。
- 仍保留 `Unity/Assets/Scripts` 中 ET 的共享、生成及部分客户端模拟代码；本仓库不是“删除全部 Unity 路径”的最小化服务端包。
- Robot 目前仍复用基础 `Model.dll` 和 `Hotfix.dll`，程序集最小化尚未完成。

## 目录结构

```text
ETServer/
├── Config/                         # 启动、Excel、Recast、NLog 配置
├── DotNet/
│   ├── App/                        # Server 入口
│   ├── Core/                       # ET 核心框架
│   ├── Hotfix/                     # Server/共享热更逻辑
│   ├── Loader/                     # 代码与配置加载
│   ├── Model/                      # Server/共享模型
│   └── Robot/
│       ├── App/                    # Robot 入口
│       ├── Model/                  # Robot Model 构建项目
│       └── Hotfix/                 # Robot Hotfix 构建项目
├── Scripts/                        # 构建、发布、集成测试入口
├── Tests/Integration/              # 本地真实进程集成测试
├── Unity/Assets/Scripts/
│   ├── Model/Robot/                # Robot Model 源码
│   └── Hotfix/Robot/               # Robot Hotfix 源码
└── Publish/                        # 生成目录，Git 忽略
```

## 环境要求

- .NET 8 SDK
- MongoDB，默认地址 `mongodb://127.0.0.1:27017`
- `mongosh`
- Bash 与 `lsof`（执行仓库提供的 macOS/Linux 脚本时）

验证 MongoDB：

```bash
mongosh mongodb://127.0.0.1:27017 \
  --eval 'db.adminCommand({ ping: 1 })'
```

首次构建或依赖变化后执行（Server 与 Robot 是两棵独立依赖图）：

```bash
dotnet restore ET.sln
dotnet restore DotNet/Robot/App/DotNet.Robot.App.csproj
```

## 构建

构建服务端：

```bash
dotnet build DotNet/App/DotNet.App.csproj --no-restore
```

构建机器人：

```bash
./Scripts/build-robot.sh
```

Release：

```bash
./Scripts/build-robot.sh Release
```

Robot 构建脚本会先强制重建 `Robot.Hotfix.dll`，再重建 `Robot.App.dll`，避免入口更新但仍加载旧 Hotfix。

## 本地运行

配置加载依赖 `Bin` 作为当前工作目录，因此从该目录启动。

启动服务端：

```bash
cd Bin
dotnet App.dll \
  --AppType=Server \
  --StartConfig=StartConfig/Localhost \
  --Process=1 \
  --Develop=1 \
  --Console=0 \
  --LogLevel=3
```

服务端就绪后，在另一个终端启动机器人：

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

机器人每处理 10 个输出一次 Info 进度，最终输出：

```text
robot batch complete: prefix=Robot total=10 ready=10 failed=0
```

`ready` 只在 Fiber 创建、登录、进图、场景切换及 AI 初始化全部成功后递增。

## 独立发布

```bash
./Scripts/publish.sh
```

输出：

```text
Publish/Server/
├── Bin/App.dll
└── Config/

Publish/Robot/
├── Bin/Robot.App.dll
└── Config/
```

脚本断言 Server 包不包含 Robot 入口/Hotfix，Robot 包不包含 Server 的 `App.dll`。

当前脚本组装的是框架依赖型目录，尚未验证特定 RID（如 `linux-x64`）的跨平台发布。

## 最小正式发布清单

正式发布使用一条门禁命令：

```bash
./Scripts/release.sh
```

执行前必须完成：

1. 将 `Config/Json/s/StartConfig/Release` 中的回环 IP、本地无认证 MongoDB 地址替换为生产配置。
2. 将 `VERSION` 改为不含 `-` 的稳定版本号。
3. 提交全部发布变更，确保 Git 工作区干净。
4. 确保 NuGet 源可访问，以审计 Server 和 Robot 两棵依赖图。

该命令会强制执行生产配置检查、依赖还原、已知漏洞审计、干净工作区检查、稳定版本检查、无旧 DLL 的 Release 重建，并生成：

- `Publish/Server` 与 `Publish/Robot` 独立目录；
- 关闭 Debug 的生产 NLog 配置；
- 每个包的 `RELEASE.json`（版本、Git commit、SDK、构建时间）；
- 每个包的 `SHA256SUMS` 完整性清单。

本地演练时可以显式放行尚未替换的生产配置、脏工作区和预发布版本，但不得把放行结果作为正式发布证据：

```bash
ALLOW_LOOPBACK_RELEASE=1 \
ALLOW_DIRTY_RELEASE=1 \
ALLOW_PRERELEASE=1 \
./Scripts/release.sh
```

不建议设置 `SKIP_VULNERABILITY_CHECK=1`。

## 自动化集成测试

确保 MongoDB 已启动且 ET 本地端口未被占用，然后执行：

```bash
./Scripts/test-integration.sh
```

该入口会：

1. 重建 Server 与 Robot（包括 Robot Hotfix）。
2. 生成独立发布目录。
3. 启动本地 Server。
4. 启动 10 个 Robot。
5. 验证唯一批次汇总为 `ready=10 failed=0`。
6. 向两个进程发送 SIGTERM，并验证退出码为 0。
7. 验证测试使用的 TCP/UDP 端口全部释放。

最近一次本地验证结果：

```text
Robot integration passed: ready=10 failed=0 robot_exit=0 server_exit=0
```

## 日志

- 根服务端/从 `Bin` 启动的进程使用 `Config/NLog/NLog.config`。
- 从 `Share/Tool` 作为工作目录启动工具时，可能使用 `Share/Config/NLog/NLog.config`。
- 压测统计必须使用低频 Info 汇总，不要以 Debug 日志条数作为机器人成功数。
- 100 个持续移动机器人会产生大量 Debug 协议日志；压测或生产环境建议使用 `--LogLevel=3`，并按部署要求关闭 NLog 的 `ServerDebug` 规则。
- 发布脚本自动使用 `Config/NLog/NLog.Release.config`：不写 Debug，Info/Warn/Error 分文件，并按日期或 100 MiB 归档。

## 已验证范围

- 50 个 Robot：`ready=50 failed=0`。
- 100 个 Robot：`ready=100 failed=0`。
- 单 Robot 失败后批次继续执行。
- EnterMap 失败会向上返回并计入 `failed`。
- 100 Robot 负载下 SIGTERM 正常退出。
- 100 Robot 持续运行 30 分钟，无持续 RSS 增长和新增运行异常。
- MongoDB `ET1` 插入、读取、更新、删除成功；测试数据已清理。
- Release 全量构建：0 Warning、0 Error。
- Server 与 Robot 两棵 NuGet 依赖图：无已知漏洞。
- 升级 MongoDB.Driver 后的 10 Robot 发布目录集成测试：`ready=10 failed=0`。

## 尚未完成

- Robot 对基础 `DotNet.Model`、`DotNet.Hotfix` 的程序集依赖最小化。
- Linux RID 发布与部署验证。
- 超过 100 Robot、超过 30 分钟的容量与稳定性测试。
- 将 EnterMap 故障注入固化为永久自动化测试。
- 当前工作区修改尚未提交。

## 上游与许可证

上游项目：[egametang/ET release8.1](https://github.com/egametang/ET/tree/release8.1)

许可证遵循上游 ET 项目及本仓库所含第三方依赖的许可证。
