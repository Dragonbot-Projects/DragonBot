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
/*#if DEBUG
            if (Directory.Exists(Path.Combine(DefaultBaseDir, "instances")))
            {
                Directory.Delete(Path.Combine(DefaultBaseDir, "instances"), true);
            }
            if (Directory.Exists(Path.Combine(DefaultBaseDir, "logs")))
            {
                Directory.Delete(Path.Combine(DefaultBaseDir, "logs"), true);
            }
            if (File.Exists(Path.Combine(DefaultBaseDir, "settings.json")))
            {
                File.Delete(Path.Combine(DefaultBaseDir, "settings.json"));
            }
#endif*/
            if (File.Exists(Path.Combine(DefaultBaseDir, "settings.json")))
            {
                using StreamReader r = new(Path.Combine(DefaultBaseDir, "settings.json"));
                string json = r.ReadToEnd();
                Settings = JsonSerializer.Deserialize<GlobalSettings>(json) ?? new() { BaseDir = DefaultBaseDir };
            }
            else
            {
                Settings = new() { BaseDir = DefaultBaseDir };
                using StreamWriter w = new(Path.Combine(AppContext.BaseDirectory, "settings.json"));
                w.Write(JsonSerializer.Serialize(Settings));
            }
            Directory.CreateDirectory(Settings.InstanceConfigDir);
            //ModuleInitilaizer.Patch();
            RegisterModuleAttribute.RegisterModules();
            //TEMP HACK TO LOAD MODULES
            ModuleMain.InitAssembly();
        }
        private static async Task Run()
        {
            var configs = Directory.EnumerateFiles(Settings!.InstanceConfigDir);
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

        internal record GlobalSettings([property: JsonPropertyName("singleInstance")] bool SingleInstance = true)
        {
            [property: JsonPropertyName("baseDirectory")]
            internal required string BaseDir { get; init; } = AppContext.BaseDirectory;
            [property: JsonPropertyName("logDirectory")]
            internal string LogDir { get => field ??= Path.Combine(BaseDir, "logs"); init; }
            [property: JsonPropertyName("instanceConfigsDirectory")]
            internal string InstanceConfigDir { get => field ??= Path.Combine(BaseDir, "instances"); init; }
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
