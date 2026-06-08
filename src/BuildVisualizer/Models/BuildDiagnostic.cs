namespace BuildVisualizer.Models
{
	public class BuildDiagnostic
	{
		public DiagnosticSeverity Severity { get; }
		public string Code { get; }
		public string Message { get; }
		public string File { get; }
		public int LineNumber { get; }
		public int ColumnNumber { get; }
		public int EndLineNumber { get; }
		public int EndColumnNumber { get; }
		public string ProjectFile { get; }

		public BuildDiagnostic(
			DiagnosticSeverity severity,
			string code,
			string message,
			string file,
			int lineNumber,
			int columnNumber,
			int endLineNumber,
			int endColumnNumber,
			string projectFile)
		{
			Severity = severity;
			Code = code;
			Message = message;
			File = file;
			LineNumber = lineNumber;
			ColumnNumber = columnNumber;
			EndLineNumber = endLineNumber;
			EndColumnNumber = endColumnNumber;
			ProjectFile = projectFile;
		}

		public override string ToString()
			=> $"[{Severity}] {Code}: {Message} ({File}:{LineNumber})";
	}
}
