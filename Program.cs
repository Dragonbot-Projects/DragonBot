using DragonBot.Core;
using DragonBot.Instance;
using DragonBot.Modules;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DragonBot
{
    public static class Program
    {
        //Should never be null after Init();
        internal static GlobalSettings? Settings;
        public static async Task Main(string[] args)
        {
            if (Environment.GetCommandLineArgs().Contains("-init"))
            {
                Environment.Exit(0);
            }
            await Run();
            await Task.Delay(-1);
        }
        [ModuleInitializer]
        internal static void Init()
        {
            string DefaultBaseDir = AppContext.BaseDirectory;
            if (File.Exists(Path.Combine(DefaultBaseDir, "settings.json")))
            {
                using StreamReader r = new(Path.Combine(DefaultBaseDir, "settings.json"));
                string json = r.ReadToEnd();
                Settings = JsonSerializer.Deserialize<GlobalSettings>(json) ?? new() { BaseDirectory = DefaultBaseDir };
            }
            else
            {
                Settings = new() { BaseDirectory = DefaultBaseDir };
                using StreamWriter w = new(Path.Combine(AppContext.BaseDirectory, "settings.json"));
                w.Write(JsonSerializer.Serialize(Settings));
            }
            Directory.CreateDirectory(Settings.InstanceConfigsDirectory);
            AssemblyLoader.LoadAssemblies();
            AssemblyLoader.InitAssemblies();
            RegisterModuleAttribute.RegisterModules();
        }
        private static async Task Run()
        {
            var configs = Directory.EnumerateFiles(Settings!.InstanceConfigsDirectory).Where((config) => Path.GetExtension(config) == ".json") ;
            if (configs.Any())
            {
                foreach (var config in configs)
                {
                    await Bot.Create(Path.GetFileNameWithoutExtension(config));
                }
            }
            else
            {
                await Bot.Create("DragonBot");
            }
        }
        internal enum InvalidConfigBehavior
        {
            Reset,
            Skip,
            Exit
        }

        internal record GlobalSettings(bool SingleInstance = true, InvalidConfigBehavior InvalidConfigBehavior = InvalidConfigBehavior.Exit)
        {
            internal required string BaseDirectory { get; init; } = AppContext.BaseDirectory;
            internal string LogDirectory { get => field ??= Path.Combine(BaseDirectory, "logs"); init; }
            internal string InstanceConfigsDirectory { get => field ??= Path.Combine(BaseDirectory, "instances"); init; }
        }
        extension<TKey, TValue>(Dictionary<TKey, TValue> dest)
            where TKey : notnull
        {
            public Dictionary<TKey, TValue> Merge(Dictionary<TKey, TValue> source) =>
                dest.Concat(source)
                    .GroupBy(kvp => kvp.Key)
                    .ToDictionary(group => group.Key, group => group.Last().Value);
        }
    }
}
