using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ET
{
    public static class WaitTypeError
    {
        public const int Success = 0;
        public const int Destroy = 1;
        public const int Cancel = 2;
        public const int Timeout = 3;
    }
    
    public interface IWaitType
    {
        int Error
        {
            get;
            set;
        }
    }

    [EntitySystemOf(typeof(ObjectWait))]
    [FriendOf(typeof(ObjectWait))]
    public static partial class ObjectWaitSystem
    {
        [EntitySystem]
        private static void Awake(this ObjectWait self)
        {
            self.tcss.Clear();
        }
        
        [EntitySystem]
        private static void Destroy(this ObjectWait self)
        {
            List<object> callbacks = self.tcss.Values.SelectMany(static list => list).ToList();
            self.tcss.Clear();

            foreach (object callback in callbacks)
            {
                ((IDestroyRun)callback).SetResult();
            }
        }

        private interface IDestroyRun
        {
            void SetResult();
        }

        private class ResultCallback<K>: Object, IDestroyRun where K : struct, IWaitType
        {
            private ETTask<K> tcs;

            public ResultCallback()
            {
                this.tcs = ETTask<K>.Create(true);
            }

            public bool IsDisposed
            {
                get
                {
                    return this.tcs == null;
                }
            }

            public ETTask<K> Task => this.tcs;

            public void SetResult(K k)
            {
                ETTask<K> task = Interlocked.Exchange(ref this.tcs, null);
                task?.SetResult(k);
            }

            public void SetResult()
            {
                this.SetResult(new K() { Error = WaitTypeError.Destroy });
            }
        }
        
        public static async ETTask<T> Wait<T>(this ObjectWait self, ETCancellationToken cancellationToken = null) where T : struct, IWaitType
        {
            ResultCallback<T> tcs = new ResultCallback<T>();
            Type type = typeof (T);
            self.Add(type, tcs);

            void CancelAction()
            {
                self.Remove(type, tcs);
                tcs.SetResult(new T() { Error = WaitTypeError.Cancel });
            }

            T ret;
            try
            {
                cancellationToken?.Add(CancelAction);
                ret = await tcs.Task;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);    
            }
            return ret;
        }

        public static async ETTask<T> Wait<T>(this ObjectWait self, int timeout, ETCancellationToken cancellationToken = null) where T : struct, IWaitType
        {
            ResultCallback<T> tcs = new ResultCallback<T>();
            Type type = typeof(T);
            async ETTask WaitTimeout()
            {
                await self.Root().GetComponent<TimerComponent>().WaitAsync(timeout, cancellationToken);
                if (cancellationToken.IsCancel())
                {
                    return;
                }
                if (!self.Remove(type, tcs))
                {
                    return;
                }
                tcs.SetResult(new T() { Error = WaitTypeError.Timeout });
            }
            
            WaitTimeout().Coroutine();
            
            self.Add(type, tcs);
            
            void CancelAction()
            {
                self.Remove(type, tcs);
                tcs.SetResult(new T() { Error = WaitTypeError.Cancel });
            }
            
            T ret;
            try
            {
                cancellationToken?.Add(CancelAction);
                ret = await tcs.Task;
            }
            finally
            {
                cancellationToken?.Remove(CancelAction);    
            }
            return ret;
        }

        public static void Notify<T>(this ObjectWait self, T obj) where T : struct, IWaitType
        {
            Type type = typeof (T);
            if (!self.tcss.Remove(type, out List<object> callbacks) || callbacks.Count == 0)
            {
                return;
            }

            foreach (object callback in callbacks)
            {
                ((ResultCallback<T>)callback).SetResult(obj);
            }
        }


        private static void Add(this ObjectWait self, Type type, object obj)
        {
            if (self.tcss.TryGetValue(type, out var list))
            {
                list.Add(obj);
            }
            else
            {
                self.tcss.Add(type, new List<object> { obj });
            }
        }

        private static bool Remove(this ObjectWait self, Type type, object obj)
        {
            if (!self.tcss.TryGetValue(type, out List<object> callbacks) || !callbacks.Remove(obj))
            {
                return false;
            }

            if (callbacks.Count == 0)
            {
                self.tcss.Remove(type);
            }

            return true;
        }
    }

    [ComponentOf]
    public class ObjectWait: Entity, IAwake, IDestroy
    {
        public Dictionary<Type, List<object>> tcss = new();
    }
}
