using System;

namespace BuildVisualizer.Models
{
	public class ProjectStatusChangedEventArgs : EventArgs
	{
		public string ProjectUniqueName { get; }
		public BuildStatus NewStatus { get; }
		public DateTime Timestamp { get; }
		public string Configuration { get; }
		public string Platform { get; }

		public ProjectStatusChangedEventArgs(string projectUniqueName, BuildStatus newStatus, DateTime timestamp, string configuration, string platform)
		{
			ProjectUniqueName = projectUniqueName;
			NewStatus = newStatus;
			Timestamp = timestamp;
			Configuration = configuration;
			Platform = platform;
		}
	}
}
