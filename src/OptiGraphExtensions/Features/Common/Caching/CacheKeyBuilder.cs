namespace OptiGraphExtensions.Features.Common.Caching
{
    public static class CacheKeyBuilder
    {
        private const string Separator = ":";

        public static string BuildEntityKey<T>(Guid id) where T : class
        {
            return $"{typeof(T).Name}{Separator}{id}";
        }

        public static string BuildEntityListKey<T>() where T : class
        {
            return $"{typeof(T).Name}{Separator}List";
        }

        public static string BuildEntityListKey<T>(params object[] parameters) where T : class
        {
            var paramString = string.Join(Separator, parameters);
            return $"{typeof(T).Name}{Separator}List{Separator}{paramString}";
        }

        public static string BuildEntityPattern<T>() where T : class
        {
            return $"^{typeof(T).Name}{Separator}.*";
        }

        public static string BuildExistsKey<T>(Guid id) where T : class
        {
            return $"{typeof(T).Name}{Separator}Exists{Separator}{id}";
        }
    }
}