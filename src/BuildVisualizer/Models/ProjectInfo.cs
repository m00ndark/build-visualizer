namespace BuildVisualizer.Models
{
	public class ProjectInfo
	{
		public string Name { get; set; }
		public string UniqueName { get; set; }
		public BuildStatus Status { get; set; }

		public ProjectInfo(string name, string uniqueName)
		{
			Name = name;
			UniqueName = uniqueName;
			Status = BuildStatus.NotBuilt;
		}
	}
}
