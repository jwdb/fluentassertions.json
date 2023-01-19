using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions.Formatting;

namespace FluentAssertions.Json
{
    public class JsonNodeFormatter : IValueFormatter
    {
        /// <summary>
        /// Indicates whether the current <see cref="IValueFormatter"/> can handle the specified <paramref name="value"/>.
        /// </summary>
        /// <param name="value">The value for which to create a <see cref="string"/>.</param>
        /// <returns>
        /// <c>true</c> if the current <see cref="IValueFormatter"/> can handle the specified value; otherwise, <c>false</c>.
        /// </returns>
        public bool CanHandle(object value)
        {
            return value is JsonNode;
        }

        public void Format(object value, FormattedObjectGraph formattedGraph, FormattingContext context, FormatChild formatChild)
        {
            var jToken = value as JsonNode;

            if (context.UseLineBreaks)
            {
                var result = jToken?.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
                if (result is not null)
                {
                    formattedGraph.AddFragmentOnNewLine(result);
                }
                else
                {
                    formattedGraph.AddFragment("<null>");
                }
            }
            else
            {
                formattedGraph.AddFragment(jToken?.ToJsonString(new JsonSerializerOptions { WriteIndented = false }) ?? "<null>");
            }
        }
    }
}
