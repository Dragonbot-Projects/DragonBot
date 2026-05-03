using Discord;
using DragonBot.Instance;
using DragonBot.Modules;
using KGySoft.CoreLibraries;
using Nito.AsyncEx;
using System.Reflection;


namespace DragonBot.Core
{
    internal static class AssemblyLoader
    {
        internal static HashSet<Assembly> BaseAssemblies { get; private set; } = [];
        internal static HashSet<string> FailedAssemblies { get; } = [];
        private static void Load(string assemblyName)
        {
            try
            {
                Assembly.LoadFile(assemblyName);
                AsyncContext.Run(() => Logger.Log($"Successfully loaded assembly {assemblyName}.", LogSeverity.Info));
            }
            catch (Exception ex)
            {
                AsyncContext.Run(() => Logger.Log($"Failed to load assembly {assemblyName}. Exception: {ex}", LogSeverity.Error));
            }
        }
        internal static void LoadAssemblies()
        {
            BaseAssemblies = [..AppDomain.CurrentDomain.GetAssemblies()];
            foreach (var assemblyFile in Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "modules"), "*.dll"))
            {
                Load(assemblyFile);
            }
        }
        internal static Assembly[] GetAssemblies()
        {
            return [.. AppDomain.CurrentDomain.GetAssemblies().Where(x => !BaseAssemblies.Contains(x))];
        }
        internal static void InitAssemblies()
        {
            foreach(Assembly assembly in AssemblyLoader.GetAssemblies())
            {
                assembly.GetTypes().Where(target => typeof(IModuleMain).IsAssignableFrom(target) && target.IsClass && !target.IsAbstract).ForEach(t =>
                {
                    try
                    {
                        var initMethod = t.GetMethod("InitAssembly", BindingFlags.Public | BindingFlags.Static);
                        if (initMethod == null)
                        {
                            AsyncContext.Run(() => Logger.Log($"No InitAssembly method found in assembly {assembly.FullName}. Skipping initialization.", LogSeverity.Error));
                            FailedAssemblies.Add(assembly.FullName!);
                            return;
                        }
                        initMethod?.Invoke(null, null);
                        AsyncContext.Run(() => Logger.Log($"Initialized assembly {assembly.FullName}.", LogSeverity.Info));
                    }
                    catch (Exception ex)
                    {
                        AsyncContext.Run(() => Logger.Log($"Failed to initialize assembly {assembly.FullName}. Exception: {ex}", LogSeverity.Error));
                    }
                });
            };
        }
    }
}
