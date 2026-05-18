namespace ET
{
    public static class StringHashHelper
    {
        public static long GetLongHashCode(this string value)
        {
            unchecked
            {
                ulong hash = 14695981039346656037;
                foreach (char c in value)
                {
                    hash ^= c;
                    hash *= 1099511628211;
                }
                return (long)hash;
            }
        }
    }
}
