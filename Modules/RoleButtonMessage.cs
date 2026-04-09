using Discord;
using Discord.Net;
using Discord.WebSocket;
using DragonBot.Core;
using DragonBot.Instance;
using System.Text.Json;

namespace DragonBot.Modules
{
    [RegisterModule]
    internal sealed class RoleButtonMessage : ModuleBase, IModule<RoleButtonMessage>, ICommand
    {
        public static string Name { get; } = "Core_RoleButtonMessage";

        public static RoleButtonMessage Create(Bot bot)
        {
            return new RoleButtonMessage(bot);
        }
        private Dictionary<string, RoleButtonMessageConfig> MessageConfigs { get; } = [];
        private RoleButtonMessage(Bot bot) : base(bot)
        {
            bot.Client.SlashCommandExecuted += HandleCommands;
            bot.Client.InteractionCreated += OnInteract;
            MessageConfigs = StateManager.LoadState<Dictionary<string, RoleButtonMessageConfig>>(Name, [typeof(RoleButtonMessageConfig), typeof(ButtonData)])?.State ?? [];
        }
        public async void RegisterCommands()
        {
            if (!bot.Util.IsCommandRegistered("role-button-message"))
            {
                var guild = bot.Client.GetGuild(bot.BotConfig.GuildID);
                SlashCommandBuilder builder = new();

                builder.WithName("role-button-message")
                    .WithDescription("Creates a message with buttons that add/remove roles")
                    .WithDefaultMemberPermissions(GuildPermission.Administrator)
                    .WithContextTypes(InteractionContextType.Guild)
                    .AddOption(new SlashCommandOptionBuilder()
                        .WithName("create-message")
                        .WithDescription("Creates a new role button message")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("channel", ApplicationCommandOptionType.Channel, "The channel to create the message in", true)
                        .AddOption("title", ApplicationCommandOptionType.String, "The title of the role button message", true)
                        .AddOption("name", ApplicationCommandOptionType.String, "Optional Name to assign to the message", false)
                    ).AddOption(new SlashCommandOptionBuilder()
                        .WithName("remove-message")
                        .WithDescription("Removes a role button message")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("message-id", ApplicationCommandOptionType.String, "The ID or Name of the role button message", true)
                    ).AddOption(new SlashCommandOptionBuilder()
                        .WithName("add-button")
                        .WithDescription("Adds a button to an existing role button message")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("message-id", ApplicationCommandOptionType.String, "The ID or Name of the role button message", true)
                        .AddOption("role", ApplicationCommandOptionType.Role, "The role to assign/unassign", true)
                        .AddOption("label", ApplicationCommandOptionType.String, "The label for the button", true)
                        .AddOption("emote", ApplicationCommandOptionType.String, "The emote for the button", false)
                    ).AddOption(new SlashCommandOptionBuilder()
                        .WithName("remove-button")
                        .WithDescription("Removes a button from an existing role button message")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .AddOption("message-id", ApplicationCommandOptionType.String, "The ID or Name of the role button message", true)
                        .AddOption("role", ApplicationCommandOptionType.Role, "The role assigned to the button to remove", true)
                    ).AddOption(new SlashCommandOptionBuilder()
                        .WithName("list-messages")
                        .WithDescription("lists all role button messages")
                        .WithType(ApplicationCommandOptionType.SubCommand)
                    );

#pragma warning disable CS0618 // Type or member is obsolete
                try
                {
                    await guild.CreateApplicationCommandAsync(builder.Build());
                }
                catch (ApplicationCommandException ex)
                {
                    var json = JsonSerializer.Serialize(ex.Errors);
                    await Logger.Log($@"ApplicationCommandException thrown during command registration for command {builder.Name}
                    Errors reported: {json}", LogSeverity.Error);
                }
                catch (Exception ex)
                {
                    await Logger.Log($"Exeption {ex} thrown during command registration for command {builder.Name}", LogSeverity.Error);
                }
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }
        private async Task HandleCommands(SocketSlashCommand command)
        {
            //TODO: General cleanup
            SocketChannel? channel;
            SocketGuild Guild = bot.Client.GetGuild(bot.BotConfig.GuildID);
            var options = command.Data.Options.First().Options;
            var commandName = command.Data.Options.First().Name;
            var config = default(RoleButtonMessageConfig);
            if (options?.Count > 0)
            {
                MessageConfigs.TryGetValue(options.First().Value.ToString()!, out config);
            }
            // /role-button-message create channel:#bot-test title:TestTitle name:TestName
            // /role-button-message add-button message-id:TestName role:@Sailor label:Barotrauma emote:Barotrauma
            // /role-button-message add-button message-id:TestName role:@Engineer label:Factorio emote:Factorio
            if (commandName is "create-message")
            {
                channel = (SocketChannel?)options!.First(option => option.Name is "channel").Value;
                if (channel?.ChannelType is not ChannelType.Text)
                {
                    await command.RespondAsync($"The specified channel is {(channel is null ? "null" : "not a text channel")}.", ephemeral: true);
                    return;
                }
                ComponentBuilderV2 builder = new();
                var message = await Guild.GetTextChannel(channel.Id).SendMessageAsync(components: builder.WithTextDisplay("Use `/role-button-message add-button` to add buttons").Build(), flags: MessageFlags.ComponentsV2);
                string? configKey;
                if (options!.Count > 2 && options.First(option => option.Name is "name")?.Value is not null)
                {
                    configKey = options.First(option => option.Name is "name").Value.ToString();
                }
                else
                {
                    configKey = message.Id.ToString();
                }
                if (!MessageConfigs.TryAdd(configKey!, new RoleButtonMessageConfig(message.Id, channel.Id, options.First(option => option.Name is "title").Value.ToString() ?? string.Empty, [])))
                {
                    await command.RespondAsync($"A message with the name {configKey} already exists. Please choose a different name.", ephemeral: true);
                    return;
                }
                await StateManager.SaveState(ModuleState<Dictionary<string, RoleButtonMessageConfig>>.CreateState(MessageConfigs, Name, new Semvar("1.0.0")));
                await command.RespondAsync($"Message created successfully with Id {message.Id} and Name {configKey}.", ephemeral: true);
            }
            else if (commandName is "remove-message")
            {
                var messageID = options!.First(option => option.Name is "message-id").Value as string;
                if (!MessageConfigs.Remove(messageID!))
                {
                    await command.RespondAsync($"No message with Id {messageID} exists.", ephemeral: true);
                    return;
                }
                if (await Guild.GetTextChannel(config!.ChannelId).GetMessageAsync(config.MessageId) is IUserMessage userMessage)
                {
                    await userMessage.DeleteAsync();
                }
                await StateManager.SaveState(ModuleState<Dictionary<string, RoleButtonMessageConfig>>.CreateState(MessageConfigs, Name, new Semvar("1.0.0")));
                await command.RespondAsync($"Message with Id {messageID} removed successfully.", ephemeral: true);
            }
            else if (commandName is "add-button" or "remove-button")
            {
                if (config is null)
                {
                    await command.RespondAsync($"No message with Id {options!.First(option => option.Name is "message-id").Value} exists.", ephemeral: true);
                    return;
                }
                (string Label, ulong RoleId, string? Emote) = ((string?)options!.FirstOrDefault(option => option.Name is "label")?.Value ?? string.Empty, bot.RoleManager.NameToId(options!.First(option => option.Name is "role").Value.ToString() ?? string.Empty), (string?)options!.FirstOrDefault(option => option?.Name is "emote", null)?.Value);
                if (commandName is "add-button")
                {
                    if (!config.Buttons.TryAdd(RoleId, new ButtonData(Label, RoleId, Emote)))
                    {
                        await command.RespondAsync($"A button for the role {options!.First(option => option.Name is "role").Value} already exists.", ephemeral: true);
                        return;
                    }
                }
                else if (commandName is "remove-button")
                {
                    if (!config.Buttons.Remove(RoleId))
                    {
                        await command.RespondAsync($"No button for the role {options!.First(option => option.Name is "role").Value} exists.", ephemeral: true);
                        return;
                    }
                }
                await RefreshMessageComponents(Guild, config);
                await command.RespondAsync($"Button {(commandName is "add-button" ? "added" : "removed")} successfully.", ephemeral: true);
            }
            else if (commandName is "list-messages")
            {
                if (MessageConfigs.Count == 0)
                {
                    await command.RespondAsync("No role button messages have been created yet.", ephemeral: true);
                    return;
                }
                string response = "Role Button Messages:\n";
                foreach (var kvp in MessageConfigs)
                {
                    response += $"Name: {kvp.Key}, Message ID: {kvp.Value.MessageId}, Channel ID: {kvp.Value.ChannelId}, Title: {kvp.Value.Title}\n";
                }
                await command.RespondAsync(response, ephemeral: true);
            }
            async Task RefreshMessageComponents(SocketGuild Guild, RoleButtonMessageConfig config)
            {
                ComponentBuilderV2 builder = new();
                List<ButtonBuilder> actionRows = [];
                builder.WithTextDisplay(config!.Title);
                foreach (var button in config.Buttons.Values)
                {
                    var discordButton = new ButtonBuilder()
                        .WithLabel(button.Label)
                        .WithCustomId($"rolebutton-{button.RoleId}")
                        .WithStyle(ButtonStyle.Primary);
                    if (button.Emote is not null)
                    {
                        discordButton.WithEmote(Guild.Emotes.FirstOrDefault(e => e.Name.Equals(button.Emote)));
                    }
                    actionRows.Add(discordButton);
                }
                foreach (var chunk in actionRows.Chunk(5))
                {
                    builder.WithActionRow(chunk);
                }
                if (await Guild.GetTextChannel(config.ChannelId).GetMessageAsync(config.MessageId) is IUserMessage userMessage)
                {
                    await userMessage.ModifyAsync(msg => msg.Components = builder.Build());
                }
                await StateManager.SaveState(ModuleState<Dictionary<string, RoleButtonMessageConfig>>.CreateState(MessageConfigs, Name, new Semvar("1.0.0"))); ;
            }
        }
        private async Task OnInteract(SocketInteraction interaction)
        {
            if (interaction is SocketMessageComponent component)
            {
                var role = bot.RoleManager.Guild.GetRole(ulong.Parse(component.Data.CustomId.Split('-')[1]));
                if (bot.RoleManager.HasRole(component.User.Id, role))
                {
                    await RoleManager.RemoveRole((IGuildUser)component.User, role);
                }
                else
                {
                    await RoleManager.AddRole((IGuildUser)component.User, role);
                }
                await component.RespondAsync("Role updated!", ephemeral: true);
            }
        }
    }
    [Serializable]
    internal record RoleButtonMessageConfig(ulong MessageId, ulong ChannelId, string Title, Dictionary<ulong, ButtonData> Buttons);
    [Serializable]
    internal readonly record struct ButtonData(string Label, ulong RoleId, string? Emote);
}
