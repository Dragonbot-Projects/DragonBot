using System.Text.Json;
using static DragonBot.Program;
using Discord.WebSocket;
using Discord;
using System.Text.Json.Serialization;
using DragonBot.Core;
using DragonBot.Modules;



#if DEBUG
using Microsoft.Extensions.Configuration;
#endif

namespace DragonBot.Instance
{
    public sealed class Bot
    {
        public BotConfig BotConfig { get; init; }
        public DiscordSocketClient Client { get; } = new();
        public MicroBus Bus { get; } = new();
        public RoleManager RoleManager { get; init; }
        public Util Util { get; init; }
        public Dictionary<string, ModuleBase> LoadedModules { get; }
        private Bot(string botName, string? token)
        {
            string DefaultToken = string.Empty;
#if DEBUG
            IConfigurationRoot config = new ConfigurationBuilder()
                .AddUserSecrets<Bot>()
                .Build();
            DefaultToken = config.GetSection("BotToken").Value!;
#endif
            if (File.Exists(Path.Combine(Settings!.InstanceConfigDir, $"{botName}.json")))
            {
                using StreamReader r = new(Path.Combine(Settings!.InstanceConfigDir, $"{botName}.json"));
                string json = r.ReadToEnd();
                BotConfig = JsonSerializer.Deserialize<BotConfig>(json)!;
            }
            else
            {
                BotConfig = new() { Token = token ?? DefaultToken};
                SaveConfig();
            }
            RoleManager = new(this);
            Util = new(this);
            LoadedModules = ModuleRegistrar.GetRequestedModules(this, BotConfig.EnabledModules);

        }
        internal static async Task<Bot> Create(string botName, string? token = null)
        {
            Bot bot = new(botName, token);
            bot.Client.Log += Logger.Log;
            await bot.Client.LoginAsync(TokenType.Bot, bot.BotConfig.Token);
            await bot.Client.StartAsync();
            bot.Client.Ready += bot.Ready;
            return bot;
        }
        public async Task Ready()
        {
            if (BotConfig.GuildId == 0)
            {
                BotConfig.GuildId = Client.Guilds.FirstOrDefault()?.Id ?? 0;
                SaveConfig();
            }
            ModuleRegistrar.InitializeModules(LoadedModules);
        }
        public void SaveConfig()
        {
            string path = Path.Combine(Settings!.InstanceConfigDir, $"{BotConfig.BotName}.json");
            using StreamWriter w = new(path);
            w.Write(JsonSerializer.Serialize(BotConfig));
        }
    }
    public record BotConfig([property: JsonPropertyName("LoggingEnabled")] bool Logging = true, [property: JsonPropertyName("DiscordToken")] string? Token = null)
    {
        [property: JsonPropertyName("BotName")]
        public string BotName { get => field ??= "DragonBot"; }
        [property: JsonPropertyName("EnabledModules")]
        public List<string> EnabledModules { get; } = ["Core:RoleButtonMessage"]; //FIXME: Remove hardcoding
        [property: JsonPropertyName("ModuleConfigs")]
        public Dictionary<string, object> ModuleConfigs { get; } = [];
        [property: JsonPropertyName("GuildID")]
        public ulong GuildId { get; internal set; }
    }
}
