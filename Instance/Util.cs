namespace DragonBot.Instance
{
    public class Util
    {
        private readonly Bot bot;
        internal Util(Bot bot)
        {
            this.bot = bot;
        }
        public bool IsCommandRegistered(string commandName)
        {
            if (bot.BotConfig.RefreshCommands)
            {
                return false;
            }
            return bot.Client.GetGuild(bot.BotConfig.GuildID).GetApplicationCommandsAsync().Result
                .Any(cmd => cmd.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
