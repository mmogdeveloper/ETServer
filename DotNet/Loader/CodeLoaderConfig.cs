using System.Reflection;

namespace ET
{
    public sealed class CodeLoaderConfig: Singleton<CodeLoaderConfig>, ISingletonAwake<Assembly[], string[]>
    {
        public Assembly[] ModelAssemblies { get; private set; }

        public string[] HotfixAssemblyNames { get; private set; }

        public void Awake(Assembly[] modelAssemblies, string[] hotfixAssemblyNames)
        {
            this.ModelAssemblies = modelAssemblies;
            this.HotfixAssemblyNames = hotfixAssemblyNames;
        }
    }
}
