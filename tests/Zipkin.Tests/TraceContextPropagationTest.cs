using System;

namespace Zipkin.Tests
{
	using System.Collections.Generic;
	using FluentAssertions;
	using NUnit.Framework;

	[TestFixture]
	public class TraceContextPropagationTest
	{
		[SetUp]
		public void Init()
		{
			TraceContextPropagation.Reset();
		}

		[Test]
		public void CurrentSpan_Should_be_null_when_not_initialized()
		{
			TraceContextPropagation.CurrentSpan.Should().BeNull();
		}

		[Test]
		public void IsWithinTrace_Should_be_false_when_not_initialized()
		{
			TraceContextPropagation.IsWithinTrace.Should().BeFalse();
		}

		[Test]
		public void PropagateTraceIdOnto_Should_not_do_anything_When_there_is_no_context()
		{
			// Arrange
			var dict = new Dictionary<string,object>();

			// Act
			TraceContextPropagation.PropagateTraceIdOnto(dict);

			// Assert
			dict.Should().HaveCount(0);
		}

		[Test]
		public void PropagateTraceIdOnto_Should_add_entries_to_dict_When_there_is_a_context()
		{
			// Arrange
			var dict = new Dictionary<string, object>();
			var span = new Span(Guid.Parse("00000000000000000000000000000123"), "a", 1000);
			TraceContextPropagation.PushSpan(span);

			// Act
			TraceContextPropagation.PropagateTraceIdOnto(dict);

			// Assert
			dict.Should().HaveCount(2);
			dict["_zipkin_traceid"].Should().Be(Guid.Parse("00000000000000000000000000000123"));
			dict["_zipkin_spanid"].Should().Be(1000L);
		}

		[Test]
		public void PropagateTraceIdOnto_Should_add_entries_to_dict_When_there_is_a_context2()
		{
			// Arrange
			var dict = new Dictionary<string, string>();
			var span = new Span(Guid.Parse("00000000000000000000000000000123"), "a", 1000);
			TraceContextPropagation.PushSpan(span);

			// Act
			TraceContextPropagation.PropagateTraceIdOnto(dict);

			// Assert
			dict.Should().HaveCount(2);
			dict["_zipkin_traceid"].Should().Be("00000000000000000000000000000123");
			dict["_zipkin_spanid"].Should().Be("1000");
		}

		[Test]
		public void IsWithinTrace_Should_be_true_When_there_is_a_context2()
		{
			// Arrange
			var span = new Span(Guid.Parse("00000000000000000000000000000123"), "a", 1000);
			TraceContextPropagation.PushSpan(span);

			// Act + Assert
			TraceContextPropagation.IsWithinTrace.Should().BeTrue();
		}

		[Test]
		public void CurrentSpan_Should_return_top_of_the_stack_When_there_is_a_context()
		{
			// Arrange
			var span1 = new Span(Guid.Parse("00000000000000000000000000000123"), "a", 1000);
			var span2 = new Span(Guid.Parse("00000000000000000000000000000321"), "a", 2000);
			TraceContextPropagation.PushSpan(span1);
			TraceContextPropagation.PushSpan(span2);

			// Act
			var currentSpan = TraceContextPropagation.CurrentSpan;

			// Assert
			currentSpan.Should().NotBeNull();
			currentSpan.Id.Should().Be(2000);
		}

		[Test]
		public void TryObtainTraceIdFrom_Should_ignore_empty_dictionary()
		{
			// Arrange
			var dict = new Dictionary<string, string>();

			// Act
			TraceInfo? traceInfo;
			TraceContextPropagation.TryObtainTraceIdFrom(dict, out traceInfo).Should().BeFalse();

			// Assert
			dict.Should().HaveCount(0);
		}

		[Test]
		public void TryObtainTraceIdFrom_Should_extract_ids_from_valid_dictionary()
		{
			// Arrange
			var dict = new Dictionary<string, string>();
			var span = new Span(Guid.Parse("00000000000000000000000000000123"), "a", 1000);
			TraceContextPropagation.PushSpan(span);
			TraceContextPropagation.PropagateTraceIdOnto(dict);

			// Act
			TraceInfo? traceInfo;
			TraceContextPropagation.TryObtainTraceIdFrom(dict, out traceInfo).Should().BeTrue();

			// Assert
			dict.Should().HaveCount(2);
			traceInfo.Value.span.TraceId.Should().Be(Guid.Parse("00000000000000000000000000000123"));
			traceInfo.Value.span.ParentId.Should().Be(1000L);
		}
	}
}