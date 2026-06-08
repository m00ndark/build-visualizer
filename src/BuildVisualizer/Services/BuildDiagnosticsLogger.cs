using BuildVisualizer.Models;
using Microsoft.Build.Framework;
using System;

namespace BuildVisualizer.Services
{
	public class BuildDiagnosticsLogger : ILogger
	{
		private IEventSource _eventSource;

		public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Quiet;

		public string Parameters { get; set; }

		public event Action<BuildDiagnostic> DiagnosticReceived;

		public void Initialize(IEventSource eventSource)
		{
			_eventSource = eventSource;
			_eventSource.ErrorRaised += OnErrorRaised;
			_eventSource.WarningRaised += OnWarningRaised;
			_eventSource.MessageRaised += OnMessageRaised;
		}

		public void Shutdown()
		{
			if (_eventSource != null)
			{
				_eventSource.ErrorRaised -= OnErrorRaised;
				_eventSource.WarningRaised -= OnWarningRaised;
				_eventSource.MessageRaised -= OnMessageRaised;
				_eventSource = null;
			}
		}

		private void OnErrorRaised(object sender, BuildErrorEventArgs e)
		{
			DiagnosticReceived?.Invoke(new BuildDiagnostic(
				DiagnosticSeverity.Error,
				e.Code,
				e.Message,
				e.File,
				e.LineNumber,
				e.ColumnNumber,
				e.EndLineNumber,
				e.EndColumnNumber,
				e.ProjectFile));
		}

		private void OnWarningRaised(object sender, BuildWarningEventArgs e)
		{
			DiagnosticReceived?.Invoke(new BuildDiagnostic(
				DiagnosticSeverity.Warning,
				e.Code,
				e.Message,
				e.File,
				e.LineNumber,
				e.ColumnNumber,
				e.EndLineNumber,
				e.EndColumnNumber,
				e.ProjectFile));
		}

		private void OnMessageRaised(object sender, BuildMessageEventArgs e)
		{
			if (e.Importance != MessageImportance.High)
				return;

			DiagnosticReceived?.Invoke(new BuildDiagnostic(
				DiagnosticSeverity.Message,
				e.Code,
				e.Message,
				e.File,
				e.LineNumber,
				e.ColumnNumber,
				e.EndLineNumber,
				e.EndColumnNumber,
				e.ProjectFile));
		}
	}
}
