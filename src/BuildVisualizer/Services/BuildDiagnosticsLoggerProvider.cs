using Microsoft.Build.Framework;
using Microsoft.VisualStudio.Shell.BuildLogging;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace BuildVisualizer.Services
{
	[Export(typeof(IVsBuildLoggerProvider))]
	public class BuildDiagnosticsLoggerProvider : IVsBuildLoggerProvider
	{
		public BuildDiagnosticsLoggerProvider()
		{
		}

		/// <summary>
		/// The shared diagnostics service that aggregates data from all logger instances.
		/// Set by the package during initialization.
		/// </summary>
		public static BuildDiagnosticsService DiagnosticsService { get; set; }

		public LoggerVerbosity Verbosity => LoggerVerbosity.Diagnostic;

		public BuildLoggerEvents Events =>
			BuildLoggerEvents.BuildStartedEvent |
			BuildLoggerEvents.BuildFinishedEvent |
			BuildLoggerEvents.ErrorEvent |
			BuildLoggerEvents.WarningEvent |
			BuildLoggerEvents.HighMessageEvent |
			BuildLoggerEvents.NormalMessageEvent |
			BuildLoggerEvents.ProjectStartedEvent |
			BuildLoggerEvents.ProjectFinishedEvent |
			BuildLoggerEvents.TargetStartedEvent |
			BuildLoggerEvents.TargetFinishedEvent |
			BuildLoggerEvents.CommandLine |
			BuildLoggerEvents.TaskStartedEvent |
			BuildLoggerEvents.TaskFinishedEvent |
			BuildLoggerEvents.LowMessageEvent |
			BuildLoggerEvents.ProjectEvaluationStartedEvent |
			BuildLoggerEvents.ProjectEvaluationFinishedEvent |
			BuildLoggerEvents.CustomEvent;

		public ILogger GetLogger(string projectPath, IEnumerable<string> targets, IDictionary<string, string> properties, bool isDesignTimeBuild)
		{
			if (isDesignTimeBuild || DiagnosticsService == null)
				return null;

			BuildDiagnosticsLogger logger = new BuildDiagnosticsLogger();
			logger.DiagnosticReceived += DiagnosticsService.OnDiagnosticReceived;
			return logger;
		}
	}
}
