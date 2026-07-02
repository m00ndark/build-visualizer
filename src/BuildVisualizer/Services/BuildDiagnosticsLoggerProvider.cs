using Microsoft.Build.Framework;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Shell.BuildLogging;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;

namespace BuildVisualizer.Services
{
	[AppliesTo(ProjectCapabilities.AlwaysApplicable)]
	[Export(typeof(IBuildLoggerProviderAsync))]
	[Export(typeof(IVsBuildLoggerProvider))]
	public class BuildDiagnosticsLoggerProvider : IBuildLoggerProviderAsync, IVsBuildLoggerProvider
	{
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

		/// <summary>
		/// Called for legacy (non-SDK-style) projects.
		/// </summary>
		public ILogger GetLogger(string projectPath, IEnumerable<string> targets, IDictionary<string, string> properties, bool isDesignTimeBuild)
		{
			return CreateLogger(isDesignTimeBuild);
		}

		/// <summary>
		/// Called for CPS-based (SDK-style) projects.
		/// </summary>
		public Task<IImmutableSet<ILogger>> GetLoggersAsync(
			IReadOnlyList<string> targets,
			IImmutableDictionary<string, string> properties,
			CancellationToken cancellationToken)
		{
			bool isDesignTimeBuild = properties.TryGetValue("DesignTimeBuild", out string value)
				&& string.Equals(value, "true", System.StringComparison.OrdinalIgnoreCase);

			ILogger logger = CreateLogger(isDesignTimeBuild);

			if (logger == null)
			{
				return Task.FromResult<IImmutableSet<ILogger>>(ImmutableHashSet<ILogger>.Empty);
			}

			IImmutableSet<ILogger> loggers = ImmutableHashSet<ILogger>.Empty.Add(logger);
			return Task.FromResult(loggers);
		}

		private ILogger CreateLogger(bool isDesignTimeBuild)
		{
			if (isDesignTimeBuild || DiagnosticsService == null)
				return null;

			BuildDiagnosticsLogger logger = new BuildDiagnosticsLogger();
			logger.DiagnosticReceived += DiagnosticsService.OnDiagnosticReceived;
			return logger;
		}
	}
}
