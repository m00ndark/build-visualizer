namespace BuildVisualizer.Services
{
	public class ReferenceInfo
	{
		public string Name { get; set; }
		public string Path { get; set; }
		public bool IsResolved { get; set; }
		public string Version { get; set; }
		public ReferenceKind ReferenceKind { get; set; }
		public string OriginalItemSpec { get; set; }

		public override string ToString()
			=> $"[{ReferenceKind}] {Name} → {Path ?? "(unresolved)"}";
	}
}