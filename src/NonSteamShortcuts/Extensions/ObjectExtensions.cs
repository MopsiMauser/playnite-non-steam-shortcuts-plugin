using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;

namespace NonSteamShortcuts.Extensions
{
    internal static class ObjectExtensions
    {
        private static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings()
        {
            Formatting = Formatting.None,
            ContractResolver = new DefaultContractResolver()
        };

        /// <summary>
        /// Perform a deep copy of the object, using Json as a serialisation method.
        /// </summary>
        /// <typeparam name="T">The type of object being copied.</typeparam>
        /// <param name="source">The object instance to copy.</param>
        /// <returns>The copied object.</returns>
        public static T GetClone<T>(this T source)
        {
            if (Object.ReferenceEquals(source, null))
            {
                return default(T);
            }

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source, jsonSerializerSettings));
        }
    }
}
