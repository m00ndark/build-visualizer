namespace BuildVisualizer.Services
{
	public static class UserSettings
	{
		public static class Collections
		{
			public const string Settings = "BuildVisualizer\\Settings";
			public const string ListView = "BuildVisualizer\\ListView";
		}

		public static class Keys
		{
			public const string ShowWindowOnBuildStart = "ShowWindowOnBuildStart";
			public const string LastView = "LastView";
			public const string ShowTransitiveDependencies = "ShowTransitiveDependencies";
			public const string ShowProjectDetails = "ShowProjectDetails";
			public const string DetailsPanelWidth = "DetailsPanelWidth";
			public const string ShowErrors = "ShowErrors";
			public const string ShowWarnings = "ShowWarnings";
			public const string ShowMessages = "ShowMessages";
			public const string GroupBySeverity = "GroupBySeverity";
			public const string WidthPrefix = "Width_";
			public const string ColumnOrder = "ColumnOrder";
			public const string HiddenColumns = "HiddenColumns";
		}

		public static class Values
		{
			public const string On = "1";
			public const string Off = "0";
			public const string GraphView = "Graph";
			public const string ListView = "List";
		}
	}
}
