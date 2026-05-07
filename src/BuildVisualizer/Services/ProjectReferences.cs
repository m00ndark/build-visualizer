using System.Collections.Generic;

namespace BuildVisualizer.Services
{
	public class ProjectReferences
	{
		public string ProjectName { get; set; }
		public string ProjectPath { get; set; }
		public string ProjectStyle { get; set; }
		public IReadOnlyList<ReferenceInfo> References { get; set; }

		public override string ToString()
			=> $"{ProjectName} ({ProjectStyle}): {References?.Count ?? 0} ref(s)";
	}
}
