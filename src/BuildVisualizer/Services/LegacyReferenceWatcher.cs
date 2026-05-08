using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using VSLangProj;

namespace BuildVisualizer.Services
{
	public sealed class LegacyReferenceWatcher : IDisposable
	{
		private ReferencesEvents _refEvents;
		private VSProject _vsProject;
		private bool _disposed;

		public event Action<string, string> ReferenceAdded;
		public event Action<string, string> ReferenceRemoved;
		public event Action<string, string> ReferenceChanged;
		public LegacyReferenceWatcher(ProjectMetadata projectMetadata)
		{
			ProjectMetadata = projectMetadata;
		}

		public ProjectMetadata ProjectMetadata { get; }

		/// <summary>
		/// Subscribes to reference events on a legacy (old-style) project.
		/// Returns false if the project doesn't support VSLangProj.
		/// </summary>
		public bool Subscribe(Project project)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (!(project?.Object is VSProject vsProject))
				return false;

			_vsProject = vsProject;

			// Must hold a strong reference to prevent GC
			// of the COM event sink
			_refEvents = vsProject.Events.ReferencesEvents;

			_refEvents.ReferenceAdded += OnReferenceAdded;
			_refEvents.ReferenceRemoved += OnReferenceRemoved;
			_refEvents.ReferenceChanged += OnReferenceChanged;

			return true;
		}

		/// <summary>
		/// Returns a snapshot of all currently known references.
		/// </summary>
		public IReadOnlyList<ReferenceInfo> GetCurrentReferences()
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			if (_vsProject == null)
				return Array.Empty<ReferenceInfo>();

			var results = new List<ReferenceInfo>();

			foreach (Reference r in _vsProject.References)
			{
				results.Add(new ReferenceInfo
					{
						Name = r.Name,
						Path = r.Path,
						IsResolved = !string.IsNullOrEmpty(r.Path),
						Version = TryGetVersion(r),
						ReferenceKind = ClassifyReference(r)
					});
			}

			return results;
		}

		/// <summary>
		/// Polls until all references have a non-empty Path, indicating
		/// they have been resolved. Uses exponential backoff.
		/// </summary>
		public async Task WaitForReferencesAsync(
			CancellationToken cancellationToken = default,
			int timeoutMs = 30000)
		{
			var sw = Stopwatch.StartNew();
			int delay = 100;

			while (sw.ElapsedMilliseconds < timeoutMs)
			{
				cancellationToken.ThrowIfCancellationRequested();

				bool allResolved = true;
				int unresolved = 0;

				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(
					cancellationToken);

				if (_vsProject == null)
					return;

				try
				{
					foreach (Reference r in _vsProject.References)
					{
						if (string.IsNullOrEmpty(r.Path))
						{
							allResolved = false;
							unresolved++;
						}
					}
				}
				catch (COMException ex)
				{
					// Project may be mid-load; swallow and retry
					Debug.WriteLine(
						$"[Legacy] COM error during reference check: {ex.Message}");
					allResolved = false;
				}

				if (allResolved)
				{
					Debug.WriteLine(
						$"[Legacy] All references resolved in {sw.ElapsedMilliseconds}ms");
					return;
				}

				Debug.WriteLine(
					$"[Legacy] {unresolved} unresolved reference(s), " +
					$"retrying in {delay}ms...");

				await Task.Delay(delay, cancellationToken);
				delay = Math.Min(delay * 2, 2000);
			}

			Debug.WriteLine(
				$"[Legacy] Timed out after {timeoutMs}ms waiting for " +
				"reference resolution.");
		}

		private void OnReferenceAdded(Reference r)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Debug.WriteLine($"[Legacy] + {r.Name} at {r.Path}");
			ReferenceAdded?.Invoke(r.Name, r.Path);
		}

		private void OnReferenceRemoved(Reference r)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Debug.WriteLine($"[Legacy] - {r.Name}");
			ReferenceRemoved?.Invoke(r.Name, r.Path);
		}

		private void OnReferenceChanged(Reference r)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			Debug.WriteLine($"[Legacy] ~ {r.Name} at {r.Path}");
			ReferenceChanged?.Invoke(r.Name, r.Path);
		}

		private static string TryGetVersion(Reference r)
		{
			try
			{
				return r.Version;
			}
			catch
			{
				return null;
			}
		}

		private static ReferenceKind ClassifyReference(Reference r)
		{
			try
			{
				return r.Type == prjReferenceType.prjReferenceTypeAssembly
					? ReferenceKind.Assembly
					: r.Type == prjReferenceType.prjReferenceTypeActiveX
						? ReferenceKind.COM
						: ReferenceKind.Unknown;
			}
			catch
			{
				return ReferenceKind.Unknown;
			}
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;

			ThreadHelper.ThrowIfNotOnUIThread();

			if (_refEvents != null)
			{
				_refEvents.ReferenceAdded -= OnReferenceAdded;
				_refEvents.ReferenceRemoved -= OnReferenceRemoved;
				_refEvents.ReferenceChanged -= OnReferenceChanged;
				_refEvents = null;
			}

			_vsProject = null;
		}
	}
}