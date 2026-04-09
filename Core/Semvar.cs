namespace DragonBot.Core
{
    [Serializable]
    public readonly record struct Semvar
    {
        public Semvar(string semvar)
        {
            var parts = semvar.Split('.');
            if (parts.Length != 3)
            {
                throw new ArgumentException("Semvar must be in the format Major.Minor.Patch[-Meta]");
            }
            Major = int.Parse(parts[0]);
            Minor = int.Parse(parts[1]);
            if (parts[2].Contains('-'))
            {
                var tmp = parts[2].Split('-');
                Patch = int.Parse(tmp[0]);
                Meta = tmp[1];
            }
            else
            {
                Patch = int.Parse(parts[2]);
            }
        }
        int Major { get; }
        int Minor { get; }
        int Patch { get; }
        string? Meta { get; }
        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}{(Meta != null ? $"-{Meta}" : "")}";
        }
        public static bool operator >(Semvar a, Semvar b)
        {
            if (a.Major != b.Major)
            {
                return a.Major > b.Major;
            }
            if (a.Minor != b.Minor)
            {
                return a.Minor > b.Minor;
            }
            if (a.Patch != b.Patch)
            {
                return a.Patch > b.Patch;
            }
            if (a.Meta == null && b.Meta != null)
            {
                return true;
            }
            if (a.Meta != null && b.Meta == null)
            {
                return false;
            }
            if (a.Meta != null && b.Meta != null)
            {
                if (a.Meta is null && b.Meta is not null)
                {
                    return true;
                }
                return string.CompareOrdinal(a.Meta, b.Meta) > 0;
            }
            return false;
        }
        public static bool operator <(Semvar a, Semvar b)
        {
            if (a.Major != b.Major)
            {
                return a.Major < b.Major;
            }
            if (a.Minor != b.Minor)
            {
                return a.Minor < b.Minor;
            }
            if (a.Patch != b.Patch)
            {
                return a.Patch < b.Patch;
            }
            if (a.Meta == null && b.Meta != null)
            {
                return false;
            }
            if (a.Meta != null && b.Meta == null)
            {
                return true;
            }
            if (a.Meta != null && b.Meta != null)
            {
                if (a.Meta is not null && b.Meta is null)
                {
                    return true;
                }
                return string.CompareOrdinal(a.Meta, b.Meta) < 0;
            }
            return false;
        }
        public static bool operator >=(Semvar a, Semvar b)
        {
            return a > b || a == b;
        }
        public static bool operator <=(Semvar a, Semvar b)
        {
            return a < b || a == b;
        }
    }
}
