using KGySoft.Serialization.Binary;
using Nito.AsyncEx;

namespace DragonBot.Core
{
    public static class StateManager
    {
        public static async Task SaveState<T>(ModuleState<T> State)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "state", $"{State.ModuleName}.bin");
            if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "state")))
            {
                Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "state"));
            }
            if (File.Exists(path))
            {
                File.Move(path, path + ".bak", true);
                File.Delete(path);
            }
            await using FileStream fs = new(path, FileMode.OpenOrCreate, FileAccess.Write);
            BinarySerializer.SerializeToStream(fs, State, BinarySerializationOptions.CompactSerializationOfStructures);
        }
        public static ModuleState<T>? LoadState<T>(string moduleName, HashSet<Type> customTypes)
        {
            
            customTypes.Add(typeof(Semvar));
            var path = Path.Combine(AppContext.BaseDirectory, "state", $"{moduleName}.bin");
            if (!File.Exists(path))
            {
                return null;
            }
            using FileStream fs = new(path, FileMode.Open, FileAccess.Read);
            try
            {
                return BinarySerializer.DeserializeFromStream<ModuleState<T>>(fs, customTypes);
            }
            catch (Exception ex)
            {
                AsyncContext.Run(() => Logger.Log($"Failed to load state for module {moduleName}: {ex.Message}", Discord.LogSeverity.Warning));
                return null;
            }
        }
    }
    [Serializable]
    public record ModuleState<T>
    {
        public required T State { get; init; }
        public required string ModuleName { get; init; }
        public required Semvar Version { get; init; }
        public static ModuleState<T> CreateState(T state, string moduleName, Semvar version)
        {
            return new ModuleState<T>() { State = state, ModuleName = moduleName, Version = version };
        }
    }
}