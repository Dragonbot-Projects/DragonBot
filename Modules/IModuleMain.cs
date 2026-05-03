using System;
using System.Collections.Generic;
using System.Text;

namespace DragonBot.Modules
{
    public interface IModuleMain
    {
        public abstract static Dictionary<Type, Action<object>> Initilizers { get; }
        public abstract static void InitAssembly();
    }
}
