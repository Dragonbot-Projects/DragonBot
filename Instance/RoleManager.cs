using Discord;
using Discord.WebSocket;

namespace DragonBot.Instance
{
    public sealed class RoleManager
    {
        private readonly Bot bot;
        public SocketGuild Guild { get => field ??= bot.Client.GetGuild(bot.BotConfig.GuildID); }
        internal RoleManager(Bot bot)
        {
            this.bot = bot;
        }
        public static async Task AddRole(IGuildUser User, IRole role)
        {
            await User.AddRoleAsync(role);
        }
        public static async Task RemoveRole(IGuildUser User, IRole role)
        {
            await User.RemoveRoleAsync(role);
        }
        public bool HasRole(ulong UserId, IRole role)
        {
            var user = Guild.GetUser(UserId);
            return user.Roles.Contains(role);
        }
        public string IdToName(ulong roleId)
        {
            var role = bot.Client.GetGuild(bot.BotConfig.GuildID).GetRole(roleId);
            return role.Name;
        }
        public ulong NameToId(string roleName)
        {
            var role = bot.Client.GetGuild(bot.BotConfig.GuildID).Roles.FirstOrDefault(r => r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));
            return role?.Id ?? 0;
        }
    }
}
