using System;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FluentAssertions.Collections;
using FluentAssertions.Execution;
using FluentAssertions.Formatting;
using FluentAssertions.Json.Common;
using FluentAssertions.Primitives;

namespace FluentAssertions.Json
{
    /// <summary>
    ///     Contains a number of methods to assert that an <see cref="JsonNode" /> is in the expected state.
    /// </summary>
    [DebuggerNonUserCode]
    public class JsonNodeAssertions : ReferenceTypeAssertions<JsonNode, JsonNodeAssertions>
    {
        private GenericCollectionAssertions<JsonNode> EnumerableSubject { get; }

        static JsonNodeAssertions()
        {
            Formatter.AddFormatter(new JsonNodeFormatter());
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="JsonNodeAssertions" /> class.
        /// </summary>
        /// <param name="subject">The subject</param>
        public JsonNodeAssertions(JsonNode subject)
            : base(subject)
        {
            EnumerableSubject = subject switch
            {
                JsonArray actualArray => new GenericCollectionAssertions<JsonNode>(actualArray),
                JsonObject actualObject => new GenericCollectionAssertions<JsonNode>(actualObject.Select(c => c.Value).ToArray()),
                JsonValue actualValue => new GenericCollectionAssertions<JsonNode>(new[] { actualValue }),
                null => new GenericCollectionAssertions<JsonNode>(null),
                _ => throw new NotSupportedException(),
            };
        }

        /// <summary>
        ///     Returns the type of the subject the assertion applies on.
        /// </summary>
        protected override string Identifier => nameof(JsonNode);

        /// <summary>
        ///     Asserts that the current <see cref="JsonNode" /> is equivalent to the parsed <paramref name="expected" /> JSON
        /// </summary>
        /// <param name="expected">The string representation of the expected element</param>
        /// <param name="because">
        ///     A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        ///     is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        ///     Zero or more objects to format using the placeholders in <see paramref="because" />.
        /// </param>
        public AndConstraint<JsonNodeAssertions> BeEquivalentTo(string expected, string because = "",
            params object[] becauseArgs)
        {
            JsonNode parsedExpected;
            try
            {
                parsedExpected = JsonNode.Parse(expected);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Unable to parse expected JSON string:{Environment.NewLine}" +
                    $"{expected}{Environment.NewLine}" +
                    "Check inner exception for more details.",
                    nameof(expected), ex);
            }

            return BeEquivalentTo(parsedExpected, because, becauseArgs);
        }

        /// <summary>
        ///     Asserts that the current <see cref="JsonNode" /> is equivalent to the <paramref name="expected" /> element
        /// </summary>
        /// <param name="expected">The expected element</param>
        /// <param name="because">
        ///     A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        ///     is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        ///     Zero or more objects to format using the placeholders in <see paramref="because" />.
        /// </param>
        public AndConstraint<JsonNodeAssertions> BeEquivalentTo(JsonNode expected, string because = "",
            params object[] becauseArgs)
        {
            return BeEquivalentTo(expected, false, options => options, because, becauseArgs);
        }

        /// <summary>
        ///     Asserts that the current <see cref="JsonNode" /> is equivalent to the <paramref name="expected" /> element
        /// </summary>
        /// <param name="expected">The expected element</param>
        /// <param name="config">The options to consider while asserting values</param>
        /// <param name="because">
        ///     A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        ///     is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        ///     Zero or more objects to format using the placeholders in <see paramref="because" />.
        /// </param>
        public AndConstraint<JsonNodeAssertions> BeEquivalentTo(JsonNode expected,
            Func<IJsonAssertionOptions<object>, IJsonAssertionOptions<object>> config,
            string because = "",
            params object[] becauseArgs)
        {
            return BeEquivalentTo(expected, false, config, because, becauseArgs);
        }

        private AndConstraint<JsonNodeAssertions> BeEquivalentTo(JsonNode expected, bool ignoreExtraProperties,
            Func<IJsonAssertionOptions<object>, IJsonAssertionOptions<object>> config,
            string because = "",
            params object[] becauseArgs)
        {
            var differentiator = new JsonNodeDifferentiator(ignoreExtraProperties, config);
            Difference difference = differentiator.FindFirstDifference(Subject, expected);

            var expectation = ignoreExtraProperties ? "was expected to contain" : "was expected to be equivalent to";

            var message = $"JSON document {difference?.ToString().EscapePlaceholders()}.{Environment.NewLine}" +
                          $"Actual document{Environment.NewLine}" +
                          $"{Format(Subject, true).EscapePlaceholders()}{Environment.NewLine}" +
                          $"{expectation}{Environment.NewLine}" +
                          $"{Format(expected, true).EscapePlaceholders()}{Environment.NewLine}" +
                          "{reason}.";

            Execute.Assertion
                .ForCondition(difference == null)
                .BecauseOf(because, becauseArgs)
                .FailWith(message);

            return new AndConstraint<JsonNodeAssertions>(this);
        }

        /// <summary>
        ///     Asserts that the current <see cref="JsonNode" /> is not equivalent to the parsed <paramref name="unexpected" /> JSON
        /// </summary>
        /// <param name="unexpected">The string representation of the unexpected element</param>
        /// <param name="because">
        ///     A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        ///     is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        ///     Zero or more objects to format using the placeholders in <see paramref="because" />.
        /// </param>
        public AndConstraint<JsonNodeAssertions> NotBeEquivalentTo(string unexpected, string because = "",
            params object[] becauseArgs)
        {
            JsonNode parsedUnexpected;
            try
            {
                parsedUnexpected = JsonNode.Parse(unexpected);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Unable to parse unexpected JSON string:{Environment.NewLine}" +
                    $"{unexpected}{Environment.NewLine}" +
                    "Check inner exception for more details.",
                    nameof(unexpected), ex);
            }

            return NotBeEquivalentTo(parsedUnexpected, because, becauseArgs);
        }

        /// <summary>
        ///     Asserts that the current <see cref="JsonNode" /> is not equivalent to the <paramref name="unexpected" /> element
        /// </summary>
        /// <param name="unexpected">The unexpected element</param>
        /// <param name="because">
        ///     A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        ///     is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        ///     Zero or more objects to format using the placeholders in <see paramref="because" />.
        /// </param>
        public AndConstraint<JsonNodeAssertions> NotBeEquivalentTo(JsonNode unexpected, string because = "", params object[] becauseArgs)
        {
            var differentiator = new JsonNodeDifferentiator(false, options => options);
            Difference difference = differentiator.FindFirstDifference(Subject, unexpected);

            Execute.Assertion
                .ForCondition((Subject is null && unexpected is not null) ||
                              difference != null)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected JSON document not to be equivalent to {0}{reason}.", unexpected);

            return new AndConstraint<JsonNodeAssertions>(this);
        }

        /// <summary>
        ///     Asserts that the current <see cref="JsonNode" /> has the specified <paramref name="expected" /> value.
        /// </summary>
        /// <param name="expected">The expected value</param>
        public AndConstraint<JsonNodeAssertions> HaveValue(string expected)
        {
            return HaveValue(expected, string.Empty);
        }

        /// <summary>
        ///     Asserts that the current <see cref="JsonNode" /> has the specified <paramref name="expected" /> value.
        /// </summary>
        /// <param name="expected">The expected value</param>
        /// <param name="because">
        ///     A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        ///     is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        ///     Zero or more objects to format using the placeholders in <see paramref="because" />.
        /// </param>
        public AndConstraint<JsonNodeAssertions> HaveValue(string expected, string because, params object[] becauseArgs)
        {
            Execute.Assertion
                .ForCondition(Subject is not null)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected JSON token to have value {0}, but the element was <null>.", expected);

            Execute.Assertion
                .ForCondition(Subject?.ToJsonString() == expected)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected JSON property {0} to have value {1}{reason}, but found {2}.",
                    Subject?.GetPath(), expected, Subject?.ToJsonString());

            return new AndConstraint<JsonNodeAssertions>(this);
        }

        /// <summary>
        ///     Asserts that the current <see cref="JsonNode" /> does not have the specified <paramref name="unexpected" /> value.
        /// </summary>
        /// <param name="unexpected">The unexpected element</param>
        /// <param name="because">
        ///     A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        ///     is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        ///     Zero or more objects to format using the placeholders in <see paramref="because" />.
        /// </param>
        public AndConstraint<JsonNodeAssertions> NotHaveValue(string unexpected, string because = "", params object[] becauseArgs)
        {
            Execute.Assertion
                .ForCondition(Subject is not null)
                .BecauseOf(because, becauseArgs)
                .FailWith("Did not expect the JSON property to have value {0}, but the token was <null>.", unexpected);

            Execute.Assertion
                .ForCondition(Subject?.ToJsonString() != unexpected)
                .BecauseOf(because, becauseArgs)
                .FailWith("Did not expect JSON property {0} to have value {1}{reason}.",
                    Subject?.GetPath(), unexpected, Subject?.ToJsonString());

            return new AndConstraint<JsonNodeAssertions>(this);
        }

        /// <summary>
        ///     Asserts that the current <see cref="JsonNode" /> matches the specified <paramref name="regularExpression" /> regular expression pattern.
        /// </summary>
        /// <param name="regularExpression">The expected regular expression pattern</param>
        public AndConstraint<JsonNodeAssertions> MatchRegex(string regularExpression)
        {
            return MatchRegex(regularExpression, string.Empty);
        }

        /// <summary>
        ///     Asserts that the current <see cref="JsonNode" /> matches the specified <paramref name="regularExpression" /> regular expression pattern.
        /// </summary>
        /// <param name="regularExpression">The expected regular expression pattern</param>
        /// <param name="because">
        ///     A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        ///     is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        ///     Zero or more objects to format using the placeholders in <see paramref="because" />.
        /// </param>
        public AndConstraint<JsonNodeAssertions> MatchRegex(string regularExpression, string because, params object[] becauseArgs)
        {
            if (regularExpression == null)
            {
                throw new ArgumentNullException(nameof(regularExpression), "MatchRegex does not support <null> pattern");
            }

            Execute.Assertion
                .ForCondition(Regex.IsMatch(Subject.ToJsonString(), regularExpression))
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected {context:JSON property} {0} to match regex pattern {1}{reason}, but found {2}.",
                    Subject.GetPath(), regularExpression, Subject.ToString());

            return new AndConstraint<JsonNodeAssertions>(this);
        }

        /// <summary>
        ///     Asserts that the current <see cref="JsonNode" /> does not match the specified <paramref name="regularExpression" /> regular expression pattern.
        /// </summary>
        /// <param name="regularExpression">The expected regular expression pattern</param>
        /// <param name="because">
        ///     A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        ///     is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        ///     Zero or more objects to format using the placeholders in <see paramref="because" />.
        /// </param>
        public AndConstraint<JsonNodeAssertions> NotMatchRegex(string regularExpression, string because = "", params object[] becauseArgs)
        {
            if (regularExpression == null)
            {
                throw new ArgumentNullException(nameof(regularExpression), "MatchRegex does not support <null> pattern");
            }

            Execute.Assertion
                .ForCondition(!Regex.IsMatch(Subject.ToJsonString(), regularExpression))
                 .BecauseOf(because, becauseArgs)
                 .FailWith("Did not expect {context:JSON property} {0} to match regex pattern {1}{reason}.",
                      Subject.GetPath(), regularExpression, Subject.ToJsonString());

            return new AndConstraint<JsonNodeAssertions>(this);
        }

        /// <summary>
        ///     Asserts that the current <see cref="JsonNode" /> has a direct child element with the specified
        ///     <paramref name="expected" /> name.
        /// </summary>
        /// <param name="expected">The name of the expected child element</param>
        public AndWhichConstraint<JsonNodeAssertions, JsonNode> HaveElement(string expected)
        {
            return HaveElement(expected, string.Empty);
        }

        /// <summary>
        ///     Asserts that the current <see cref="JsonNode" /> has a direct child element with the specified
        ///     <paramref name="expected" /> name.
        /// </summary>
        /// <param name="expected">The name of the expected child element</param>
        /// <param name="because">
        ///     A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        ///     is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        ///     Zero or more objects to format using the placeholders in <see paramref="because" />.
        /// </param>
        public AndWhichConstraint<JsonNodeAssertions, JsonNode> HaveElement(string expected, string because,
            params object[] becauseArgs)
        {
            JsonNode jsonNode = Subject[expected];
            Execute.Assertion
                .ForCondition(jsonNode != null)
                .BecauseOf(because, becauseArgs)
                .FailWith("Expected JSON document {0} to have element \"" + expected.EscapePlaceholders() + "\"{reason}" +
                          ", but no such element was found.", Subject);

            return new AndWhichConstraint<JsonNodeAssertions, JsonNode>(this, jsonNode);
        }

        /// <summary>
        ///     Asserts that the current <see cref="JsonNode" /> does not have a direct child element with the specified
        ///     <paramref name="unexpected" /> name.
        /// </summary>
        /// <param name="unexpected">The name of the not expected child element</param>
        /// <param name="because">
        ///     A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        ///     is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        ///     Zero or more objects to format using the placeholders in <see paramref="because" />.
        /// </param>
        public AndWhichConstraint<JsonNodeAssertions, JsonNode> NotHaveElement(string unexpected, string because = "",
            params object[] becauseArgs)
        {
            JsonNode jsonNode = Subject[unexpected];
            Execute.Assertion
                .ForCondition(jsonNode == null)
                .BecauseOf(because, becauseArgs)
                .FailWith("Did not expect JSON document {0} to have element \"" + unexpected.EscapePlaceholders() + "\"{reason}.", Subject);

            return new AndWhichConstraint<JsonNodeAssertions, JsonNode>(this, jsonNode);
        }

        /// <summary>
        /// Expects the current <see cref="JsonNode" /> to contain only a single item.
        /// </summary>
        /// <param name="because">
        /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        /// Zero or more objects to format using the placeholders in <see cref="because" />.
        /// </param>
        public AndWhichConstraint<JsonNodeAssertions, JsonNode> ContainSingleItem(string because = "", params object[] becauseArgs)
        {
            string formattedDocument = Format(Subject).Replace("{", "{{").Replace("}", "}}");

            using (new AssertionScope("JSON document " + formattedDocument))
            {
                var constraint = EnumerableSubject.ContainSingle(because, becauseArgs);
                return new AndWhichConstraint<JsonNodeAssertions, JsonNode>(this, constraint.Which);
            }
        }

        /// <summary>
        /// Asserts that the number of items in the current <see cref="JsonNode" /> matches the supplied <paramref name="expected" /> amount.
        /// </summary>
        /// <param name="expected">The expected number of items in the current <see cref="JsonNode" />.</param>
        /// <param name="because">
        /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        /// Zero or more objects to format using the placeholders in <see cref="because" />.
        /// </param>
        public AndConstraint<JsonNodeAssertions> HaveCount(int expected, string because = "", params object[] becauseArgs)
        {
            string formattedDocument = Format(Subject).Replace("{", "{{").Replace("}", "}}");

            using (new AssertionScope("JSON document " + formattedDocument))
            {
                EnumerableSubject.HaveCount(expected, because, becauseArgs);
                return new AndConstraint<JsonNodeAssertions>(this);
            }
        }

        /// <summary>
        /// Recursively asserts that the current <see cref="JsonNode"/> contains at least the properties or elements of the specified <paramref name="subtree"/>.
        /// </summary>
        /// <param name="subtree">The subtree to search for</param>
        /// <param name="because">
        /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        /// Zero or more objects to format using the placeholders in <see cref="because" />.
        /// </param>
        /// <remarks>Use this method to match the current <see cref="JsonNode"/> against an arbitrary subtree,
        /// permitting it to contain any additional properties or elements. This way we can test multiple properties on a <see cref="JsonObject"/> at once,
        /// or test if a <see cref="JsonArray"/> contains any items that match a set of properties, assert that a JSON document has a given shape, etc. </remarks>
        /// <example>
        /// This example asserts the values of multiple properties of a child object within a JSON document.
        /// <code>
        /// var json = JsonNode.Parse("{ success: true, data: { id: 123, type: 'my-type', name: 'Noone' } }");
        /// json.Should().ContainSubtree(JsonNode.Parse("{ success: true, data: { type: 'my-type', name: 'Noone' } }"));
        /// </code>
        /// </example>
        /// <example>This example asserts that a <see cref="JsonArray"/> within a <see cref="JsonObject"/> has at least one element with at least the given properties</example>
        /// <code>
        /// var json = JToken.Parse("{ id: 1, items: [ { id: 2, type: 'my-type', name: 'Alpha' }, { id: 3, type: 'other-type', name: 'Bravo' } ] }");
        /// json.Should().ContainSubtree(JToken.Parse("{ items: [ { type: 'my-type', name: 'Alpha' } ] }"));
        /// </code>
        public AndConstraint<JsonNodeAssertions> ContainSubtree(string subtree, string because = "", params object[] becauseArgs)
        {
            JsonNode subtreeToken;
            try
            {
                subtreeToken = JsonNode.Parse(subtree);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(
                    $"Unable to parse expected JSON string:{Environment.NewLine}" +
                    $"{subtree}{Environment.NewLine}" +
                    "Check inner exception for more details.",
                    nameof(subtree), ex);
            }

            return ContainSubtree(subtreeToken, because, becauseArgs);
        }

        /// <summary>
        /// Recursively asserts that the current <see cref="JsonNode"/> contains at least the properties or elements of the specified <paramref name="subtree"/>.
        /// </summary>
        /// <param name="subtree">The subtree to search for</param>
        /// <param name="because">
        /// A formatted phrase as is supported by <see cref="string.Format(string,object[])" /> explaining why the assertion
        /// is needed. If the phrase does not start with the word <i>because</i>, it is prepended automatically.
        /// </param>
        /// <param name="becauseArgs">
        /// Zero or more objects to format using the placeholders in <see cref="because" />.
        /// </param>
        /// <remarks>Use this method to match the current <see cref="JsonNode"/> against an arbitrary subtree,
        /// permitting it to contain any additional properties or elements. This way we can test multiple properties on a <see cref="JsonObject"/> at once,
        /// or test if a <see cref="JsonArray"/> contains any items that match a set of properties, assert that a JSON document has a given shape, etc. </remarks>
        /// <example>
        /// This example asserts the values of multiple properties of a child object within a JSON document.
        /// <code>
        /// var json = JsonNode.Parse("{ success: true, data: { id: 123, type: 'my-type', name: 'Noone' } }");
        /// json.Should().ContainSubtree(JsonNode.Parse("{ success: true, data: { type: 'my-type', name: 'Noone' } }"));
        /// </code>
        /// </example>
        /// <example>This example asserts that a <see cref="JsonArray"/> within a <see cref="JsonObject"/> has at least one element with at least the given properties</example>
        /// <code>
        /// var json = JsonNode.Parse("{ id: 1, items: [ { id: 2, type: 'my-type', name: 'Alpha' }, { id: 3, type: 'other-type', name: 'Bravo' } ] }");
        /// json.Should().ContainSubtree(JsonNode.Parse("{ items: [ { type: 'my-type', name: 'Alpha' } ] }"));
        /// </code>
        public AndConstraint<JsonNodeAssertions> ContainSubtree(JsonNode subtree, string because = "", params object[] becauseArgs)
        {
            return BeEquivalentTo(subtree, true, options => options, because, becauseArgs);
        }

#pragma warning disable CA1822 // Making this method static is a breaking chan
        public string Format(JsonNode value, bool useLineBreaks = false)
        {
            // SMELL: Why is this method necessary at all?
            // SMELL: Why aren't we using the Formatter class directly?
            var output = new FormattedObjectGraph(maxLines: 100);

            new JsonNodeFormatter().Format(value, output, new FormattingContext
            {
                UseLineBreaks = useLineBreaks
            }, null);

            return output.ToString();
        }
#pragma warning restore CA1822 // Making this method static is a breaking chan
    }
}
