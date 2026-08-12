using System;
using System.Runtime.InteropServices;
using System.Threading;
using NLog;

namespace ET
{
    public sealed class AppRunner: IDisposable
    {
        private readonly Init init;
        private readonly PosixSignalRegistration sigtermRegistration;
        private int stopRequested;
        private int disposed;

        public AppRunner(Init init)
        {
            this.init = init;

            if (!OperatingSystem.IsWindows())
            {
                this.sigtermRegistration = PosixSignalRegistration.Create(PosixSignal.SIGTERM, context =>
                {
                    context.Cancel = true;
                    this.RequestStop();
                });
            }
        }

        public void Run()
        {
            Console.CancelKeyPress += this.OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += this.OnProcessExit;

            try
            {
                this.init.Start();

                while (Volatile.Read(ref this.stopRequested) == 0)
                {
                    Thread.Sleep(1);
                    try
                    {
                        this.init.Update();
                        this.init.LateUpdate();
                    }
                    catch (Exception e)
                    {
                        Log.Error(e);
                    }
                }
            }
            finally
            {
                this.Dispose();
            }
        }

        public void RequestStop()
        {
            Interlocked.Exchange(ref this.stopRequested, 1);
        }

        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
        {
            args.Cancel = true;
            this.RequestStop();
        }

        private void OnProcessExit(object sender, EventArgs args)
        {
            this.RequestStop();
            this.Dispose();
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) != 0)
            {
                return;
            }

            Console.CancelKeyPress -= this.OnCancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit -= this.OnProcessExit;
            this.sigtermRegistration?.Dispose();

            try
            {
                if (Logger.Instance != null)
                {
                    Log.Info("application stopping");
                }

                World.Instance?.Dispose();
            }
            finally
            {
                LogManager.Shutdown();
            }
        }
    }
}
