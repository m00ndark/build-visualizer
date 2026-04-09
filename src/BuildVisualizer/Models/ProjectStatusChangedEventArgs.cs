using System;

namespace BuildVisualizer.Models
{
	public class ProjectStatusChangedEventArgs : EventArgs
	{
		public string ProjectUniqueName { get; }
		public BuildStatus NewStatus { get; }

		public ProjectStatusChangedEventArgs(string projectUniqueName, BuildStatus newStatus)
		{
			ProjectUniqueName = projectUniqueName;
			NewStatus = newStatus;
		}
	}
}
