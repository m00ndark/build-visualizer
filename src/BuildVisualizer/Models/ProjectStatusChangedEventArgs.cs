using System;

namespace BuildVisualizer.Models
{
	public class ProjectStatusChangedEventArgs : EventArgs
	{
		public string ProjectUniqueName { get; }
		public BuildStatus NewStatus { get; }
		public DateTime Timestamp { get; }

		public ProjectStatusChangedEventArgs(string projectUniqueName, BuildStatus newStatus, DateTime timestamp)
		{
			ProjectUniqueName = projectUniqueName;
			NewStatus = newStatus;
			Timestamp = timestamp;
		}
	}
}
