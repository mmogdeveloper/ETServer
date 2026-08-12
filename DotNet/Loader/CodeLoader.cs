using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace ET
{
    public class CodeLoader: Singleton<CodeLoader>, ISingletonAwake
    {
        private AssemblyLoadContext assemblyLoadContext;

        private Assembly modelAssembly;

        public void Awake()
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly ass in assemblies)
            {
                if (ass.GetName().Name == "Model")
                {
                    this.modelAssembly = ass;
                    break;
                }
            }

            Assembly[] hotfixAssemblies = this.LoadHotfix();

            World.Instance.AddSingleton<CodeTypes, Assembly[]>(this.GetCodeAssemblies(hotfixAssemblies));

            IStaticMethod start = new StaticMethod(this.modelAssembly, "ET.Entry", "Start");
            start.Run();
        }

        private Assembly[] LoadHotfix()
        {
            assemblyLoadContext?.Unload();
            GC.Collect();
            assemblyLoadContext = new AssemblyLoadContext("Hotfix", true);

            List<string> assemblyNames = new() { "Hotfix" };
            if (CodeLoaderConfig.Instance != null)
            {
                assemblyNames.AddRange(CodeLoaderConfig.Instance.HotfixAssemblyNames);
            }

            Assembly[] assemblies = new Assembly[assemblyNames.Count];
            for (int i = 0; i < assemblyNames.Count; ++i)
            {
                string assemblyName = assemblyNames[i];
                string dllPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
                string pdbPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.pdb");
                using MemoryStream dllStream = new(File.ReadAllBytes(dllPath));
                using MemoryStream pdbStream = File.Exists(pdbPath) ? new MemoryStream(File.ReadAllBytes(pdbPath)) : null;
                assemblies[i] = pdbStream == null
                        ? assemblyLoadContext.LoadFromStream(dllStream)
                        : assemblyLoadContext.LoadFromStream(dllStream, pdbStream);
            }

            return assemblies;
        }

        private Assembly[] GetCodeAssemblies(Assembly[] hotfixAssemblies)
        {
            List<Assembly> assemblies = new()
            {
                typeof(World).Assembly,
                typeof(Init).Assembly,
                this.modelAssembly,
            };

            if (CodeLoaderConfig.Instance != null)
            {
                assemblies.AddRange(CodeLoaderConfig.Instance.ModelAssemblies);
            }

            assemblies.AddRange(hotfixAssemblies);
            return assemblies.Distinct().ToArray();
        }
        
        public void Reload()
        {
			Assembly[] hotfixAssemblies = this.LoadHotfix();
			
            CodeTypes codeTypes = World.Instance.AddSingleton<CodeTypes, Assembly[]>(this.GetCodeAssemblies(hotfixAssemblies));

            codeTypes.CreateCode();
            Log.Debug($"reload dll finish!");
        }
    }
}
