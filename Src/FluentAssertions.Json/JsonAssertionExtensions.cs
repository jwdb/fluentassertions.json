using System.Diagnostics;
using JetBrains.Annotations;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FluentAssertions.Json
{
    /// <summary>
    ///     Contains extension methods for JToken assertions.
    /// </summary>
    [DebuggerNonUserCode]
    public static class JsonAssertionExtensions
    {
        /// <summary>
        ///     Returns an <see cref="JTokenAssertions"/> object that can be used to assert the current <see cref="JToken"/>.
        /// </summary>
        [Pure]
        public static JTokenAssertions Should(this JToken jToken)
        {
            return new JTokenAssertions(jToken);
        }

        /// <summary>
        ///     Returns an <see cref="JTokenAssertions"/> object that can be used to assert the current <see cref="JObject"/>.
        /// </summary>
        [Pure]
        public static JTokenAssertions Should(this JObject jObject)
        {
            return new JTokenAssertions(jObject);
        }

        /// <summary>
        ///     Returns an <see cref="JTokenAssertions"/> object that can be used to assert the current <see cref="JValue"/>.
        /// </summary>
        [Pure]
        public static JTokenAssertions Should(this JValue jValue)
        {
            return new JTokenAssertions(jValue);
        }

        /// <summary>
        ///     Returns an <see cref="JsonNodeAssertions"/> object that can be used to assert the current <see cref="JsonNode"/>.
        /// </summary>
        [Pure]
        public static JsonNodeAssertions Should(this JsonNode jsonNode)
        {
            return new JsonNodeAssertions(jsonNode);
        }

        /// <summary>
        ///     Returns an <see cref="JsonNodeAssertions"/> object that can be used to assert the current <see cref="JsonArray"/>.
        /// </summary>
        [Pure]
        public static JsonNodeAssertions Should(this JsonArray jsonArray)
        {
            return new JsonNodeAssertions(jsonArray);
        }

        /// <summary>
        ///     Returns an <see cref="JsonNodeAssertions"/> object that can be used to assert the current <see cref="JsonObject"/>.
        /// </summary>
        [Pure]
        public static JsonNodeAssertions Should(this JsonObject jsonObject)
        {
            return new JsonNodeAssertions(jsonObject);
        }
    }
}
