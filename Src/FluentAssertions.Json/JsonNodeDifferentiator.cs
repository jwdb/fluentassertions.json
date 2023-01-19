using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions.Execution;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FluentAssertions.Json
{
    internal class JsonNodeDifferentiator
    {
        private readonly bool ignoreExtraProperties;
        private readonly Func<IJsonAssertionOptions<object>, IJsonAssertionOptions<object>> config;

        public JsonNodeDifferentiator(bool ignoreExtraProperties,
            Func<IJsonAssertionOptions<object>, IJsonAssertionOptions<object>> config)
        {
            this.ignoreExtraProperties = ignoreExtraProperties;
            this.config = config;
        }

        public Difference FindFirstDifference(JsonNode actual, JsonNode expected)
        {
            var path = new JPath();

            if (actual == expected)
            {
                return null;
            }

            if (actual == null)
            {
                return new Difference(DifferenceKind.ActualIsNull, path);
            }

            if (expected == null)
            {
                return new Difference(DifferenceKind.ExpectedIsNull, path);
            }

            return FindFirstDifference(actual, expected, path);
        }

        private Difference FindFirstDifference(JsonNode actual, JsonNode expected, JPath path)
        {
            return actual switch
            {
                JsonArray actualArray => FindJsonArrayDifference(actualArray, expected, path),
                JsonObject actualObject => FindJObjectDifference(actualObject, expected, path),
                JsonValue actualValue => FindValueDifference(actualValue, expected, path),
                null => null,
                _ => throw new NotSupportedException(),
            };
        }

        private Difference FindJsonArrayDifference(JsonArray actualArray, JsonNode expected, JPath path)
        {
            if (expected is not JsonArray expectedArray)
            {
                return new Difference(DifferenceKind.OtherType, path, Describe(expected), Describe(actualArray));
            }

            if (ignoreExtraProperties)
            {
                return CompareExpectedItems(actualArray, expectedArray, path);
            }
            else
            {
                return CompareItems(actualArray, expectedArray, path);
            }
        }

        private Difference CompareExpectedItems(JsonArray actual, JsonArray expected, JPath path)
        {
            JsonNode[] actualChildren = actual.ToArray();
            JsonNode[] expectedChildren = expected.ToArray();

            int matchingIndex = 0;
            for (int expectedIndex = 0; expectedIndex < expectedChildren.Length; expectedIndex++)
            {
                var expectedChild = expectedChildren[expectedIndex];
                bool match = false;
                for (int actualIndex = matchingIndex; actualIndex < actualChildren.Length; actualIndex++)
                {
                    var difference = FindFirstDifference(actualChildren[actualIndex], expectedChild);

                    if (difference == null)
                    {
                        match = true;
                        matchingIndex = actualIndex + 1;
                        break;
                    }
                }

                if (!match)
                {
                    if (matchingIndex >= actualChildren.Length)
                    {
                        if (actualChildren.Any(actualChild => FindFirstDifference(actualChild, expectedChild) == null))
                        {
                            return new Difference(DifferenceKind.WrongOrder, path.AddIndex(expectedIndex));
                        }

                        return new Difference(DifferenceKind.ActualMissesElement, path.AddIndex(expectedIndex));
                    }

                    return FindFirstDifference(actualChildren[matchingIndex], expectedChild,
                        path.AddIndex(expectedIndex));
                }
            }

            return null;
        }

        private Difference CompareItems(JsonArray actual, JsonArray expected, JPath path)
        {
            JsonNode[] actualChildren = actual.ToArray();
            JsonNode[] expectedChildren = expected.ToArray();

            if (actualChildren.Length != expectedChildren.Length)
            {
                return new Difference(DifferenceKind.DifferentLength, path, actualChildren.Length, expectedChildren.Length);
            }

            for (int i = 0; i < actualChildren.Length; i++)
            {
                Difference firstDifference = FindFirstDifference(actualChildren[i], expectedChildren[i], path.AddIndex(i));

                if (firstDifference != null)
                {
                    return firstDifference;
                }
            }

            return null;
        }

        private Difference FindJObjectDifference(JsonObject actual, JsonNode expected, JPath path)
        {
            if (expected is not JsonObject expectedObject)
            {
                return new Difference(DifferenceKind.OtherType, path, Describe(actual), Describe(expected));
            }

            return CompareProperties(actual, expectedObject, path);
        }

        private Difference CompareProperties(JsonObject actual, JsonObject expected, JPath path)
        {
            var actualDictionary = actual.ToDictionary(p => p.Key, p => p.Value);
            var expectedDictionary = expected.ToDictionary(p => p.Key, p => p.Value);

            foreach (KeyValuePair<string, JsonNode> expectedPair in expectedDictionary)
            {
                if (!actualDictionary.ContainsKey(expectedPair.Key))
                {
                    return new Difference(DifferenceKind.ActualMissesProperty, path.AddProperty(expectedPair.Key));
                }
            }

            foreach (KeyValuePair<string, JsonNode> actualPair in actualDictionary)
            {
                if (!ignoreExtraProperties && !expectedDictionary.ContainsKey(actualPair.Key))
                {
                    return new Difference(DifferenceKind.ExpectedMissesProperty, path.AddProperty(actualPair.Key));
                }
            }

            foreach (KeyValuePair<string, JsonNode> expectedPair in expectedDictionary)
            {
                JsonNode actualValue = actualDictionary[expectedPair.Key];

                Difference firstDifference = FindFirstDifference(actualValue, expectedPair.Value,
                    path.AddProperty(expectedPair.Key));

                if (firstDifference != null)
                {
                    return firstDifference;
                }
            }

            return null;
        }

        private Difference FindValueDifference(JsonValue actualValue, JsonNode expected, JPath path)
        {
            if (expected is not JsonValue expectedValue)
            {
                return new Difference(DifferenceKind.OtherType, path, actualValue, expected);
            }

            return CompareValues(actualValue, expectedValue, path);
        }

        private Difference CompareValues(JsonValue actual, JsonValue expected, JPath path)
        {
            actual.TryGetValue<JsonElement>(out var actualElement);
            expected.TryGetValue<JsonElement>(out var expectedElement);

            if (actualElement.ValueKind != expectedElement.ValueKind)
            {
                return new Difference(DifferenceKind.OtherType, path, Describe(actual), Describe(expected));
            }

            bool hasMismatches;
            using (var scope = new AssertionScope())
            {
                if (actual.TryGetValue<JsonElement>(out var actualDecimal) | expected.TryGetValue<JsonElement>(out var expectedDecimal))
                {
                    object actualObject = actual.As<object>();
                    object expectedObject = actual.As<object>();
                    if (actualDecimal.ValueKind != JsonValueKind.Undefined)
                    {
                        actualObject = actualDecimal.ValueKind switch
                        {
                            JsonValueKind.False => false,
                            JsonValueKind.True => true,
                            JsonValueKind.Number => actualDecimal.GetDouble(),
                            _ => actualDecimal.ToString()
                        };
                    }
                    if (expectedDecimal.ValueKind != JsonValueKind.Undefined)
                    {
                        expectedObject = expectedDecimal.ValueKind switch
                        {
                            JsonValueKind.False => false,
                            JsonValueKind.True => true,
                            JsonValueKind.Number => expectedDecimal.GetDouble(),
                            _ => expectedDecimal.ToString()
                        };
                    }
                    actualObject.Should().BeEquivalentTo(expectedObject, options =>
                        (JsonAssertionOptions<object>)config.Invoke(new JsonAssertionOptions<object>(options)));
                }
                else
                {
                    actual.ToString().Should().BeEquivalentTo(expected.ToString());
                }

                hasMismatches = scope.Discard().Length > 0;
            }

            if (hasMismatches)
            {
                return new Difference(DifferenceKind.OtherValue, path);
            }

            return null;
        }
        private static string Describe(JsonNode node)
        {
            if ((node as JsonValue)?.TryGetValue<JsonElement>(out var element) ?? false)
            {
                return element.ValueKind switch
                {
                    JsonValueKind.Object => "an object",
                    JsonValueKind.Array => "an array",
                    JsonValueKind.String => "a string",
                    JsonValueKind.Null => "type null",
                    JsonValueKind.Undefined => "type undefined",
                    JsonValueKind.Number => "an integer",
                    JsonValueKind.False => "a false value",
                    JsonValueKind.True => "a true value",
                    _ => throw new ArgumentOutOfRangeException(nameof(element.ValueKind), element.ValueKind, null),
                };
            }

            return node.ToJsonString();
        }
    }
}
