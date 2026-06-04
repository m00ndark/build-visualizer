using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BuildVisualizer.Services
{
	internal static class ProjectDataHelper
	{
		private static readonly Guid SolutionFolderGuid = new Guid("2150E333-8FDC-42A3-9474-1A3956D46DE8");

		public static ProjectMetadata GetProjectData(IVsHierarchy hierarchy, IVsSolution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			hierarchy.GetProperty(
				VSConstants.VSITEMID_ROOT,
				(int)__VSHPROPID.VSHPROPID_Name,
				out object nameObj);

			solution.GetUniqueNameOfProject(hierarchy, out string uniqueName);

			hierarchy.GetCanonicalName(
				VSConstants.VSITEMID_ROOT, out string path);

			string outputType = null;
			(hierarchy as IVsBuildPropertyStorage)?
				.GetPropertyValue("OutputType", null, (uint)_PersistStorageType.PST_PROJECT_FILE, out outputType);

			bool isTestProject = IsTestProject(path);

			return new ProjectMetadata
				{
					Name = nameObj as string ?? "(unknown)",
					UniqueName = uniqueName ?? "(unknown)",
					Path = path ?? string.Empty,
					OutputType = isTestProject
						? "Test Library"
						: ConvertOutputType(outputType) ?? "(unknown)"
				};
		}

		public static string ConvertOutputType(string outputType)
		{
			switch (outputType?.ToLowerInvariant())
			{
				case null: return null;
				case "exe": return "Executable";
				case "winexe": return "Windows App";
				case "library": return "Library";
				case "module": return "Module";
				default: return outputType;
			}
		}

		public static bool IsTestProject(string projectPath)
		{
			if (string.IsNullOrEmpty(projectPath) || !File.Exists(projectPath))
				return false;

			try
			{
				XDocument doc = XDocument.Load(projectPath);
				XNamespace ns = doc.Root.GetDefaultNamespace();

				// Check for <IsTestProject>true</IsTestProject> (set by Microsoft.NET.Test.Sdk)
				XElement isTestProp = doc.Root
					.Descendants(ns + "IsTestProject")
					.FirstOrDefault();

				if (isTestProp != null
					&& string.Equals(isTestProp.Value.Trim(), "true", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}

				// Check for test framework PackageReference entries
				string[] testPackages = new[]
					{
						"Microsoft.NET.Test.Sdk",
						"xunit",
						"xunit.core",
						"NUnit",
						"NUnit3TestAdapter",
						"MSTest.TestFramework",
						"MSTest.TestAdapter"
					};

				bool hasTestPackage = doc.Root
					.Descendants(ns + "PackageReference")
					.Any(el =>
						{
							string include = (string)el.Attribute("Include");
							return include != null
								&& testPackages.Any(tp =>
									string.Equals(include, tp, StringComparison.OrdinalIgnoreCase));
						});

				return hasTestPackage;
			}
			catch (Exception ex)
			{
				Debug.WriteLine($"[ProjectData] Could not detect test project for '{projectPath}': {ex.Message}");
				return false;
			}
		}

		public static IEnumerable<IVsHierarchy> EnumerateLoadedProjects(IVsSolution solution)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			Guid guid = Guid.Empty;
			solution.GetProjectEnum(
				(uint)__VSENUMPROJFLAGS.EPF_LOADEDINSOLUTION,
				ref guid,
				out IEnumHierarchies enumerator);

			IVsHierarchy[] hierarchy = new IVsHierarchy[1];

			while (enumerator.Next(1, hierarchy, out uint fetched) == VSConstants.S_OK
				&& fetched == 1)
			{
				if (!IsSolutionFolder(hierarchy[0]))
				{
					yield return hierarchy[0];
				}
			}
		}

		public static bool IsSolutionFolder(IVsHierarchy hierarchy)
		{
			ThreadHelper.ThrowIfNotOnUIThread();

			return hierarchy.GetGuidProperty(
					VSConstants.VSITEMID_ROOT,
					(int)__VSHPROPID.VSHPROPID_TypeGuid,
					out Guid typeGuid) == VSConstants.S_OK
				&& typeGuid == SolutionFolderGuid;
		}
	}
}
