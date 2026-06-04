using System;
using EnvDTE;

namespace BuildVisualizer.Models
{
	public class BuildEventArgs : EventArgs
	{
		public vsBuildScope Scope { get; }
		public vsBuildAction Action { get; }

		public BuildEventArgs(vsBuildScope scope, vsBuildAction action)
		{
			Scope = scope;
			Action = action;
		}
	}
}
