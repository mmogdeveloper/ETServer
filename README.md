# ETServer

基于 [ET8.1](https://github.com/egametang/ET) 的纯服务端版本，移除了所有 Unity 客户端代码，仅保留 .NET Core 服务端部分。

---

## 项目结构

```
ETServer/
├── DotNet/                 # .NET Core 服务端解决方案
│   ├── App/                # 启动入口
│   ├── Core/               # 核心框架（ECS、Fiber、Actor、网络等）
│   ├── Hotfix/             # 热更新逻辑层
│   ├── Model/              # 数据模型层
│   ├── Loader/             # 热重载加载器
│   └── ThirdParty/         # 第三方库
├── Unity/Assets/Scripts/   # 服务端共享代码（Share 部分）
└── Config/                 # 服务器启动配置
```

## 主要特性

- **多线程多进程 Actor 架构** — 抽象出 Fiber（纤程）概念，轻松利用多核
- **Fiber 调度** — 支持主线程、线程池、独立线程三种调度模式
- **Fiber 间 Actor 消息** — 位置透明，跨进程/跨物理机发消息无需关心对象位置
- **运行时热重载** — 不停服更新逻辑代码，大幅提升运维效率
- **MemoryPack 序列化** — 零 GC 网络消息序列化
- **KCP / TCP / WebSocket** — 底层可动态切换，UDP 不通时自动切换 TCP/WebSocket，玩家不掉线
- **纯 C# Recast 寻路** — dotrecast，无任何 C++ 依赖
- **Roslyn 分析器** — 编译期强制 ET 代码规范

## 运行要求

- .NET 8 SDK 或更高版本
- 操作系统：Windows / Linux / macOS

## 快速启动

```bash
cd DotNet
dotnet run --project App
```

Linux 部署：

```bash
./Run.sh Config/StartConfig/your-config.txt
```

## 与原版 ET 的关系

本仓库是原版 ET 框架的服务端子集：

- 移除了 Unity 客户端工程
- 保留了 `Share`（共享逻辑）、`Model`（数据层）、`Hotfix`（热更层）、`Core`、`Loader` 等服务端工程
- ET.sln 仅包含服务端相关项目

原版 ET 框架（含客户端）：[https://github.com/egametang/ET](https://github.com/egametang/ET)

## Benchmark

100 万 Ping-Pong 平均耗时约 4 秒，平均每秒收发 20 万条消息。

## 许可证

遵循原版 ET 框架许可证。
