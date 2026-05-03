using DragonBot.Core;
using DragonBot.Modules;

namespace DragonBot.CoreModules
{
    internal sealed class ModuleMain : IModuleMain
    {
        public static Dictionary<Type, Action<object>> Initilizers { get; } = new()
        {
            { typeof(ICommand), static (module) => ((ICommand)module).RegisterCommands() }
        };

        public static void InitAssembly()
        {
            //TODO: Fix
            ModuleRegistrar.Initializers = ModuleRegistrar.Initializers.Merge(Initilizers);
        }
    }
}
