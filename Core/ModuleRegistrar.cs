using Discord;
using DragonBot.Instance;
using DragonBot.Modules;
using Nito.AsyncEx;
using System.Reflection;


namespace DragonBot.Core
{
    internal static class ModuleRegistrar
    {
        private static readonly Dictionary<string, Func<Bot, ModuleBase>> Modules = [];
        public static Dictionary<Type, Action<object>> Initializers = [];
        internal static async Task<RegistrationState> Register(string name, Func<Bot, ModuleBase> module)
        {
            if (Modules.ContainsKey(name))
            {
                return RegistrationState.AlreadyRegistered;
            }
            try
            {
                Type moduleClassType = module.GetMethodInfo().DeclaringType ?? throw new ModuleRegistrationExeption("Error getting declared type of module.", true);
                List<string> dependecies = await GetDependancies(name, moduleClassType);
                Modules.Add(name, module);
                await Logger.Log($"Sucessfully registered module {name}.", LogSeverity.Info);
                return RegistrationState.Success;
            }
            catch (Exception ex)
            {
                if (ex is ModuleRegistrationExeption exeption)
                {
                    if (exeption.Fatal)
                    {
                        await Logger.Log($"ModuleRegistrationExeption thrown in registration of module {name} with reason {ex.Message}. This is a fatal error and should never happen. Program will now exit.", LogSeverity.Critical);
                        Environment.Exit(-1);
                    }
                    await Logger.Log($"ModuleRegistrationExeption thrown in registration of module {name} with reason {ex.Message}.", LogSeverity.Error);
                }
                else if (name.StartsWith("Core:"))
                {
                    await Logger.Log($"Exeption {ex} thrown in registration for core module {name}. This is a fatal error and should never happen. Program will now exit.", LogSeverity.Critical);
                    Environment.Exit(-1);
                }
                else
                {
                    await Logger.Log($"Exeption {ex} thrown in registration for module {name}.", LogSeverity.Error);
                }
                return RegistrationState.ErrorThrown;
            }
            static async Task<List<string>> GetDependancies(string name, Type type)
            {
                try
                {
                    return (List<string>?)(type?.GetProperty("Dependecies")?.GetValue(null)) ?? [];
                }
                catch (TargetException)
                {
                    await Logger.Log($"No dependancies found for module {name}.", LogSeverity.Info);
                    return [];
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }
        internal static Dictionary<string, ModuleBase> GetRequestedModules(Bot bot, List<string> requestedModules)
        {
            return Modules
                .Where(x => requestedModules.Contains(x.Key))
                .ToDictionary(x => x.Key, x => x.Value.Invoke(bot));
        }
        internal static void InitializeModules(Dictionary<string, ModuleBase> LoadedModules)
        {
            foreach (var loadedModule in LoadedModules)
            {
                var moduleInstance = loadedModule.Value;
                if (moduleInstance is null) continue;

                var moduleType = moduleInstance.GetType();

                // Find initializers registered for this exact type or any base/interface type.
                var matchedInitializers = Initializers
                    .Where(kv => kv.Key.IsAssignableFrom(moduleType))
                    .Select(kv => kv.Value)
                    .ToList();

                if (matchedInitializers.Count == 0)
                {
                    continue;
                }

                foreach (var initializer in matchedInitializers)
                {
                    try
                    {
                        initializer?.Invoke(moduleInstance);
                    }
                    catch (Exception ex)
                    {
                        AsyncContext.Run(() => Logger.Log($"Exception initializing module {loadedModule.Key} ({moduleType.FullName}): {ex}", LogSeverity.Error));
                    }
                }
            }
        }
    }
    public enum RegistrationState
    {
        Success,
        ErrorThrown,
        AlreadyRegistered,
        MissingDependencies
    }
    [AttributeUsage(AttributeTargets.Class)]
    public class RegisterModuleAttribute : Attribute
    {
        public static void RegisterModules()
        {
            var targets = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(x => x.GetTypes())
                .Where(x => x.IsClass && x.IsSubclassOf(typeof(ModuleBase)) && x.GetCustomAttributes(typeof(RegisterModuleAttribute), false).Length != 0);

            foreach (var target in targets)
            {
                var name = target.GetProperty("Name")!.GetValue(null) as string;
                var createMethod = (Func<Bot, ModuleBase>)Delegate.CreateDelegate(typeof(Func<Bot, ModuleBase>), target.GetMethod("Create")!);
                if(name is null || createMethod is null)
                {
                    AsyncContext.Run(() => Logger.Log($"Invalid Module (Name:{name} createMethod:{createMethod}).", LogSeverity.Error));
                }
                else
                {
                    switch (AsyncContext.Run(() => ModuleRegistrar.Register(name, createMethod)))
                    {
                        case RegistrationState.Success:
                            break;
                        case RegistrationState.ErrorThrown:
                            break;
                        case RegistrationState.AlreadyRegistered:
                            AsyncContext.Run(() => Logger.Log($"Module {name} has already been registered. Did you forget to namespace your modules name. (ex: yourname_modulename)", LogSeverity.Warning));
                            break;
                        case RegistrationState.MissingDependencies:

                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
        }
    }
    [Serializable]
    internal class ModuleRegistrationExeption : Exception
    {
        public bool Fatal { get; }
        private ModuleRegistrationExeption()
        {
        }
        public ModuleRegistrationExeption(string? message) : base(message)
        {
        }
        public ModuleRegistrationExeption(string? message, bool fatal) : base(message)
        {
            Fatal = fatal;
        }
        public ModuleRegistrationExeption(string? message, Exception? innerException) : base(message, innerException)
        {
        }
        public ModuleRegistrationExeption(string? message, Exception? innerException, bool fatal) : base(message, innerException)
        {
            Fatal = fatal;
        }
    }
}
