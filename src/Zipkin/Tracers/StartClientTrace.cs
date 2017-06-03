using System;

namespace Zipkin
{
	using Utils;

	/// <summary>
	/// Starts a trace. 
	/// 
	/// <para>The trace is subject to the sampling rate configured for the process or always sampled if isDebug is set to true.</para>
	/// 
	/// The Span is also annotated with <see cref="StandardAnnotationKeys.ClientSend"/> as per guidelines.
	/// </summary>
	public struct StartClientTrace : ITrace
	{
		private readonly long _start;
		private bool _skipDuration;

		public Span Span { get; private set; }

		/// <summary>
		/// Starts a client trace. 
		/// The <see cref="StandardAnnotationKeys.ClientSend"/> annotation is added by default.
		/// Give it a short lower-case description of the activity
		/// </summary>
		public StartClientTrace(string name, bool isDebug = false)
		{
			_skipDuration = false;

			var shouldSample = isDebug || ZipkinConfig.ShouldSample();

			if (shouldSample)
			{
				this._start = TickClock.Start();
				this.Span = new Span(Guid.NewGuid(), name, RandomHelper.NewId())
				{
					IsDebug = isDebug
				};
				this.TimeAnnotateWith(PredefinedTag.ClientSend);

				TraceContextPropagation.PushSpan(this.Span);
			}
			else
			{
				this._start = 0;
				this.Span = null;
			}
		}

		public void SkipDuration()
		{
			_skipDuration = true;
		}

		public void Dispose()
		{
			if (Span != null)
			{
				if (!_skipDuration)
					Span.DurationInMicroseconds = TickClock.GetDuration(_start);

				TraceContextPropagation.PopSpan();

				ZipkinConfig.Record(Span);

				Span = null;
			}
		}
	}
}