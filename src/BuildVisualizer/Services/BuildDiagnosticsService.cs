using BuildVisualizer.Models;
using System;
using System.Collections.Generic;

namespace BuildVisualizer.Services
{
	public class BuildDiagnosticsService
	{
		private readonly object _lock = new object();

		private readonly List<BuildDiagnostic> _diagnostics = new List<BuildDiagnostic>();
		private readonly Dictionary<string, List<BuildDiagnostic>> _diagnosticsByProject
			= new Dictionary<string, List<BuildDiagnostic>>(StringComparer.OrdinalIgnoreCase);

		public int ErrorCount { get; private set; }
		public int WarningCount { get; private set; }
		public int MessageCount { get; private set; }

		/// <summary>
		/// Fired whenever diagnostics change.
		/// The string parameter is the ProjectFile that was updated, or null when cleared.
		/// May fire on any thread — subscribers should marshal to UI thread.
		/// </summary>
		public event Action<string> DiagnosticsChanged;

		/// <summary>
		/// Clears all diagnostics and resets counts. Call at build start.
		/// </summary>
		public void Clear()
		{
			lock (_lock)
			{
				_diagnostics.Clear();
				_diagnosticsByProject.Clear();
				ErrorCount = 0;
				WarningCount = 0;
				MessageCount = 0;
			}

			DiagnosticsChanged?.Invoke(null);
		}

		/// <summary>
		/// Returns all diagnostics for the given project file path,
		/// or an empty list if none.
		/// </summary>
		public IReadOnlyList<BuildDiagnostic> GetDiagnosticsForProject(string projectFile)
		{
			lock (_lock)
			{
				if (projectFile != null
					&& _diagnosticsByProject.TryGetValue(projectFile, out List<BuildDiagnostic> list))
				{
					return list.ToArray();
				}

				return Array.Empty<BuildDiagnostic>();
			}
		}

		/// <summary>
		/// Returns the error, warning, and message counts for a specific project file.
		/// Thread-safe. More efficient than GetDiagnosticsForProject when only counts are needed.
		/// </summary>
		public (int Errors, int Warnings, int Messages) GetDiagnosticCountsForProject(string projectFile)
		{
			lock (_lock)
			{
				if (projectFile == null
					|| !_diagnosticsByProject.TryGetValue(projectFile, out List<BuildDiagnostic> list))
				{
					return (0, 0, 0);
				}

				int errors = 0;
				int warnings = 0;
				int messages = 0;

				foreach (BuildDiagnostic diagnostic in list)
				{
					switch (diagnostic.Severity)
					{
						case DiagnosticSeverity.Error:
							errors++;
							break;
						case DiagnosticSeverity.Warning:
							warnings++;
							break;
						case DiagnosticSeverity.Message:
							messages++;
							break;
					}
				}

				return (errors, warnings, messages);
			}
		}

		/// <summary>
		/// Called by logger instances on build threads when a diagnostic is received.
		/// Thread-safe.
		/// </summary>
		public void OnDiagnosticReceived(BuildDiagnostic diagnostic)
		{
			lock (_lock)
			{
				_diagnostics.Add(diagnostic);

				switch (diagnostic.Severity)
				{
					case DiagnosticSeverity.Error:
						ErrorCount++;
						break;
					case DiagnosticSeverity.Warning:
						WarningCount++;
						break;
					case DiagnosticSeverity.Message:
						MessageCount++;
						break;
				}

				if (!string.IsNullOrEmpty(diagnostic.ProjectFile))
				{
					if (!_diagnosticsByProject.TryGetValue(diagnostic.ProjectFile, out List<BuildDiagnostic> list))
					{
						list = new List<BuildDiagnostic>();
						_diagnosticsByProject[diagnostic.ProjectFile] = list;
					}

					list.Add(diagnostic);
				}
			}

			DiagnosticsChanged?.Invoke(diagnostic.ProjectFile);
		}
	}
}
