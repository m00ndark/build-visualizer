using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace BuildVisualizer.Services
{
	public sealed class ResolvedReferenceWatcher : IDisposable
	{
		private readonly List<IDisposable> _subscriptions = new List<IDisposable>();
		private readonly TaskCompletionSource<bool> _firstUpdateTcs = new TaskCompletionSource<bool>();
		private readonly object _lock = new object();
		private bool _disposed;

		private IProjectRuleSnapshot _resolvedSnapshot;
		private IProjectRuleSnapshot _evaluationSnapshot;

		private bool _hasResolvedData;
		private bool _hasEvaluationData;

		public ResolvedReferenceWatcher(string projectName, string projectUniqueName, string projectPath)
		{
			ProjectName = projectName;
			ProjectUniqueName = projectUniqueName;
			ProjectPath = projectPath;
		}

		public string ProjectName { get; }
		public string ProjectUniqueName { get; }
		public string ProjectPath { get; }

		private static readonly ImmutableHashSet<string> ResolvedRules =
			ImmutableHashSet<string>.Empty
				.Add("ResolvedProjectReference");

		private static readonly ImmutableHashSet<string> EvaluationRules =
			ImmutableHashSet<string>.Empty
				.Add("ProjectReference");

		/// <summary>
		/// Completes when the first full snapshot of resolved references
		/// has been received from the CPS dataflow pipeline.
		/// </summary>
		public Task ReferencesReady => _firstUpdateTcs.Task;

		/// <summary>
		/// Fired whenever any resolved reference changes after the
		/// initial snapshot.
		/// </summary>
		public event Action<IReadOnlyList<ReferenceInfo>> ReferencesChanged;

		/// <summary>
		/// Subscribes to the CPS dataflow pipeline for resolved
		/// reference updates.
		/// </summary>
		public async Task SubscribeAsync(
			UnconfiguredProject unconfiguredProject,
			CancellationToken cancellationToken)
		{
			ConfiguredProject configuredProject = await unconfiguredProject
				.GetSuggestedConfiguredProjectAsync();

			IProjectSubscriptionService subscriptionService = configuredProject
				.Services
				.ExportProvider
				.GetExportedValue<IProjectSubscriptionService>();

			IReceivableSourceBlock<
				IProjectVersionedValue<IProjectSubscriptionUpdate>> evalSource =
				subscriptionService
					.ProjectRuleSource
					.SourceBlock;

			ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>
				evalAction =
					new ActionBlock<
						IProjectVersionedValue<IProjectSubscriptionUpdate>>(
						OnEvaluationUpdate);

			_subscriptions.Add(
				evalSource.LinkTo(
					evalAction,
					new DataflowLinkOptions { PropagateCompletion = true },
					initialDataAsNew: true,
					suppressVersionOnlyUpdates: true,
					ruleNames: EvaluationRules));

			IReceivableSourceBlock<
				IProjectVersionedValue<IProjectSubscriptionUpdate>> resolvedSource =
				subscriptionService
					.JointRuleSource
					.SourceBlock;

			ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>
				resolvedAction =
					new ActionBlock<
						IProjectVersionedValue<IProjectSubscriptionUpdate>>(
						OnResolvedUpdate);

			_subscriptions.Add(
				resolvedSource.LinkTo(
					resolvedAction,
					new DataflowLinkOptions { PropagateCompletion = true },
					initialDataAsNew: true,
					suppressVersionOnlyUpdates: true,
					ruleNames: ResolvedRules));

			cancellationToken.Register(() =>
				{
					_firstUpdateTcs.TrySetCanceled();
					Dispose();
				});
		}

		/// <summary>
		/// Returns the latest known resolved references.
		/// </summary>
		public IReadOnlyList<ReferenceInfo> GetCurrentReferences()
		{
			lock (_lock)
			{
				return ExtractDirectReferences(
					_resolvedSnapshot, _evaluationSnapshot);
			}
		}

		private void OnEvaluationUpdate(
			IProjectVersionedValue<IProjectSubscriptionUpdate> update)
		{
			lock (_lock)
			{
				if (update.Value.ProjectChanges
					.TryGetValue("ProjectReference", out IProjectChangeDescription change))
				{
					_evaluationSnapshot = change.After;
					_hasEvaluationData = true;

					Debug.WriteLine($"[DirectRef/Eval] {change.After.Items.Count} declared ProjectReference item(s)");
				}

				TrySignalReady();
			}

			NotifyIfReady();
		}

		private void OnResolvedUpdate(
			IProjectVersionedValue<IProjectSubscriptionUpdate> update)
		{
			lock (_lock)
			{
				if (update.Value.ProjectChanges
					.TryGetValue("ResolvedProjectReference", out IProjectChangeDescription change))
				{
					_resolvedSnapshot = change.After;
					_hasResolvedData = true;

					Debug.WriteLine($"[DirectRef/Resolved] {change.After.Items.Count} resolved project reference(s)");
				}

				TrySignalReady();
			}

			NotifyIfReady();
		}

		private void TrySignalReady()
		{
			if (_hasEvaluationData && _hasResolvedData)
			{
				_firstUpdateTcs.TrySetResult(true);
			}
		}

		private void NotifyIfReady()
		{
			if (_hasEvaluationData && _hasResolvedData)
			{
				IReadOnlyList<ReferenceInfo> refs = GetCurrentReferences();
				Action<IReadOnlyList<ReferenceInfo>> handler = ReferencesChanged;
				if (handler != null)
				{
					handler(refs);
				}
			}
		}

		private static IReadOnlyList<ReferenceInfo> ExtractDirectReferences(
			IProjectRuleSnapshot resolvedSnapshot,
			IProjectRuleSnapshot evaluationSnapshot)
		{
			if (resolvedSnapshot == null || evaluationSnapshot == null)
			{
				return Array.Empty<ReferenceInfo>();
			}

			HashSet<string> directPaths = new HashSet<string>(
				evaluationSnapshot.Items.Keys,
				StringComparer.OrdinalIgnoreCase);

			List<ReferenceInfo> results = new List<ReferenceInfo>();

			foreach (KeyValuePair<string, IImmutableDictionary<string, string>>
				kvp in resolvedSnapshot.Items)
			{
				string itemName = kvp.Key;
				IImmutableDictionary<string, string> metadata = kvp.Value;

				if (!metadata.TryGetValue("OriginalItemSpec", out string originalSpec))
				{
					originalSpec = itemName;
				}

				bool isDirect = directPaths.Contains(originalSpec)
					|| directPaths.Contains(itemName);

				if (!isDirect)
				{
					continue;
				}

				if (!metadata.TryGetValue("ResolvedPath", out string resolvedPath))
				{
					resolvedPath = string.Empty;
				}

				metadata.TryGetValue("Version", out string version);

				results.Add(new ReferenceInfo
					{
						Name = itemName,
						Path = resolvedPath,
						IsResolved = metadata.ContainsKey("ResolvedPath"),
						Version = version,
						ReferenceKind = ReferenceKind.Project,
						OriginalItemSpec = originalSpec
					});
			}

			Debug.WriteLine($"[DirectRef] {results.Count} direct of {resolvedSnapshot.Items.Count} total resolved");

			return results;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			foreach (IDisposable sub in _subscriptions)
			{
				sub.Dispose();
			}

			_subscriptions.Clear();
			_firstUpdateTcs.TrySetCanceled();
		}
	}
}
