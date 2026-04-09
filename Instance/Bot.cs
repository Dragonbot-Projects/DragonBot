using System.Text.Json;
using static DragonBot.Program;
using Discord.WebSocket;
using Discord;
using System.Text.Json.Serialization;
using DragonBot.Core;
using DragonBot.Modules;
using Nito.AsyncEx;




#if DEBUG
using Microsoft.Extensions.Configuration;
#endif

namespace DragonBot.Instance
{
    public sealed class Bot
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };

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
            if (File.Exists(Path.Combine(Settings!.InstanceConfigsDirectory, $"{botName}.json")))
            {
                using StreamReader r = new(Path.Combine(Settings!.InstanceConfigsDirectory, $"{botName}.json"));
                string json = r.ReadToEnd();
                var test = JsonSerializer.Deserialize<BotConfig>(json, JsonOptions);
                try
                {
                    BotConfig = JsonSerializer.Deserialize<BotConfig>(json, JsonOptions)!;
                }
                catch (JsonException ex)
                {
                    if (Settings.InvalidConfigBehavior is InvalidConfigBehavior.Exit)
                    {
                        AsyncContext.Run(() => Logger.Log($"Invalid config for Bot {botName}.\n{ex}", LogSeverity.Critical));
                        Environment.Exit(-1);
                    }
                    else
                    {
                        AsyncContext.Run(() => Logger.Log($"Invalid config for Bot {botName}. Resetting config to default. \n{ex}", LogSeverity.Error));
                        BotConfig = new() { DiscordToken = token ?? DefaultToken, EnabledModules = [] };
                        AsyncContext.Run(() => SaveConfig());
                    }
                }
                catch (Exception ex)
                {
                    AsyncContext.Run(() => Logger.Log($"Unexpected error loading config for Bot {botName}.\n{ex}", LogSeverity.Error));
                    Environment.Exit(-1);
                }
            }
            else
            {
                BotConfig = new() { DiscordToken = token ?? DefaultToken, EnabledModules = [] };
                AsyncContext.Run(() => SaveConfig());
            }
            RoleManager = new(this);
            Util = new(this);
            LoadedModules = ModuleRegistrar.GetRequestedModules(this, BotConfig.EnabledModules!);

        }
        internal static async Task<Bot> Create(string botName, string? token = null)
        {
            Bot bot = new(botName, token);
            bot.Client.Log += Logger.Log;
            await bot.Client.LoginAsync(TokenType.Bot, bot.BotConfig.DiscordToken);
            await bot.Client.StartAsync();
            bot.Client.Ready += bot.Ready;
            return bot;
        }
        public async Task Ready()
        {
            if (BotConfig.GuildID == 0)
            {
                BotConfig.GuildID = Client.Guilds.FirstOrDefault()?.Id ?? 0;
                await SaveConfig();
            }
            ModuleRegistrar.InitializeModules(LoadedModules);
        }
        private async Task SaveConfig()
        {
            string path = Path.Combine(Settings!.InstanceConfigsDirectory, $"{BotConfig.BotName}.json");
            await using StreamWriter w = new(path);
            w.Write(JsonSerializer.Serialize(BotConfig));
        }
    }
    public record BotConfig(bool LoggingEnabled = true, bool RefreshCommands = false, string? DiscordToken = null)
    {
        public string BotName { get; init; } = "DragonBot";
        public ulong GuildID { get; internal set; }
        public required List<string> EnabledModules { get; init;  }
    }
}
