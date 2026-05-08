using BuildVisualizer.Models;
using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;

namespace BuildVisualizer.Resources
{
	public static class Colors
	{
		// Light mode base colors — pastel tones readable on light backgrounds
		private static readonly WpfColor NotBuiltColor  = WpfColor.FromRgb(212, 212, 212);
		private static readonly WpfColor BuildingColor  = WpfColor.FromRgb(147, 197, 253);
		private static readonly WpfColor SuccessColor   = WpfColor.FromRgb(134, 239, 172);
		private static readonly WpfColor FailedColor    = WpfColor.FromRgb(252, 165, 165);
		private static readonly WpfColor SkippedColor   = WpfColor.FromRgb(158, 158, 158);

		// Dark mode base colors — saturated tones readable on dark backgrounds
		private static readonly WpfColor DarkNotBuiltColor  = WpfColor.FromRgb( 60,  60,  65);
		private static readonly WpfColor DarkBuildingColor  = WpfColor.FromRgb( 20,  60, 170);
		private static readonly WpfColor DarkSuccessColor   = WpfColor.FromRgb( 15,  95,  45);
		private static readonly WpfColor DarkFailedColor    = WpfColor.FromRgb(150,  20,  20);
		private static readonly WpfColor DarkSkippedColor   = WpfColor.FromRgb( 75,  75,  80);

		public static bool IsDarkTheme { get; set; }

		// Light mode brushes
		private static readonly SolidColorBrush NotBuiltBrush;
		private static readonly SolidColorBrush BuildingBrush;
		private static readonly SolidColorBrush SuccessBrush;
		private static readonly SolidColorBrush FailedBrush;
		private static readonly SolidColorBrush SkippedBrush;

		private static readonly SolidColorBrush NotBuiltHighlightBrush;
		private static readonly SolidColorBrush BuildingHighlightBrush;
		private static readonly SolidColorBrush SuccessHighlightBrush;
		private static readonly SolidColorBrush FailedHighlightBrush;
		private static readonly SolidColorBrush SkippedHighlightBrush;

		private static readonly SolidColorBrush NotBuiltDependencyBorderBrush;
		private static readonly SolidColorBrush BuildingDependencyBorderBrush;
		private static readonly SolidColorBrush SuccessDependencyBorderBrush;
		private static readonly SolidColorBrush FailedDependencyBorderBrush;
		private static readonly SolidColorBrush SkippedDependencyBorderBrush;

		private static readonly SolidColorBrush NotBuiltDependencyBackgroundBrush;
		private static readonly SolidColorBrush BuildingDependencyBackgroundBrush;
		private static readonly SolidColorBrush SuccessDependencyBackgroundBrush;
		private static readonly SolidColorBrush FailedDependencyBackgroundBrush;
		private static readonly SolidColorBrush SkippedDependencyBackgroundBrush;

		private static readonly SolidColorBrush NotBuiltTextBrush;
		private static readonly SolidColorBrush BuildingTextBrush;
		private static readonly SolidColorBrush SuccessTextBrush;
		private static readonly SolidColorBrush FailedTextBrush;
		private static readonly SolidColorBrush SkippedTextBrush;

		// Dark mode brushes
		private static readonly SolidColorBrush DarkNotBuiltBrush;
		private static readonly SolidColorBrush DarkBuildingBrush;
		private static readonly SolidColorBrush DarkSuccessBrush;
		private static readonly SolidColorBrush DarkFailedBrush;
		private static readonly SolidColorBrush DarkSkippedBrush;

		private static readonly SolidColorBrush DarkNotBuiltHighlightBrush;
		private static readonly SolidColorBrush DarkBuildingHighlightBrush;
		private static readonly SolidColorBrush DarkSuccessHighlightBrush;
		private static readonly SolidColorBrush DarkFailedHighlightBrush;
		private static readonly SolidColorBrush DarkSkippedHighlightBrush;

		private static readonly SolidColorBrush DarkNotBuiltDependencyBorderBrush;
		private static readonly SolidColorBrush DarkBuildingDependencyBorderBrush;
		private static readonly SolidColorBrush DarkSuccessDependencyBorderBrush;
		private static readonly SolidColorBrush DarkFailedDependencyBorderBrush;
		private static readonly SolidColorBrush DarkSkippedDependencyBorderBrush;

		private static readonly SolidColorBrush DarkNotBuiltDependencyBackgroundBrush;
		private static readonly SolidColorBrush DarkBuildingDependencyBackgroundBrush;
		private static readonly SolidColorBrush DarkSuccessDependencyBackgroundBrush;
		private static readonly SolidColorBrush DarkFailedDependencyBackgroundBrush;
		private static readonly SolidColorBrush DarkSkippedDependencyBackgroundBrush;

		private static readonly SolidColorBrush DarkNotBuiltTextBrush;
		private static readonly SolidColorBrush DarkBuildingTextBrush;
		private static readonly SolidColorBrush DarkSuccessTextBrush;
		private static readonly SolidColorBrush DarkFailedTextBrush;
		private static readonly SolidColorBrush DarkSkippedTextBrush;

		static Colors()
		{
			// Light mode
			NotBuiltBrush          = CreateFrozen(NotBuiltColor);
			BuildingBrush          = CreateFrozen(BuildingColor);
			SuccessBrush           = CreateFrozen(SuccessColor);
			FailedBrush            = CreateFrozen(FailedColor);
			SkippedBrush           = CreateFrozen(SkippedColor);

			NotBuiltHighlightBrush = CreateFrozen(Darken(NotBuiltColor));
			BuildingHighlightBrush = CreateFrozen(Darken(BuildingColor));
			SuccessHighlightBrush  = CreateFrozen(Darken(SuccessColor));
			FailedHighlightBrush   = CreateFrozen(Darken(FailedColor));
			SkippedHighlightBrush  = CreateFrozen(Darken(SkippedColor));

			NotBuiltDependencyBorderBrush = CreateFrozen(Darken(NotBuiltColor, 150));
			BuildingDependencyBorderBrush = CreateFrozen(Darken(BuildingColor, 150));
			SuccessDependencyBorderBrush  = CreateFrozen(Darken(SuccessColor,  150));
			FailedDependencyBorderBrush   = CreateFrozen(Darken(FailedColor,   150));
			SkippedDependencyBorderBrush  = CreateFrozen(Darken(SkippedColor,  150));

			NotBuiltDependencyBackgroundBrush = CreateFrozen(Darken(NotBuiltColor, 100));
			BuildingDependencyBackgroundBrush = CreateFrozen(Darken(BuildingColor, 100));
			SuccessDependencyBackgroundBrush  = CreateFrozen(Darken(SuccessColor,  100));
			FailedDependencyBackgroundBrush   = CreateFrozen(Darken(FailedColor,   100));
			SkippedDependencyBackgroundBrush  = CreateFrozen(Darken(SkippedColor,  100));

			NotBuiltTextBrush  = CreateFrozen(GetTextColor(NotBuiltColor));
			BuildingTextBrush  = CreateFrozen(GetTextColor(BuildingColor));
			SuccessTextBrush   = CreateFrozen(GetTextColor(SuccessColor));
			FailedTextBrush    = CreateFrozen(GetTextColor(FailedColor));
			SkippedTextBrush   = CreateFrozen(GetTextColor(SkippedColor));

			// Dark mode — highlight and dependency use lighten instead of darken
			DarkNotBuiltBrush          = CreateFrozen(DarkNotBuiltColor);
			DarkBuildingBrush          = CreateFrozen(DarkBuildingColor);
			DarkSuccessBrush           = CreateFrozen(DarkSuccessColor);
			DarkFailedBrush            = CreateFrozen(DarkFailedColor);
			DarkSkippedBrush           = CreateFrozen(DarkSkippedColor);

			DarkNotBuiltHighlightBrush = CreateFrozen(Lighten(DarkNotBuiltColor));
			DarkBuildingHighlightBrush = CreateFrozen(Lighten(DarkBuildingColor));
			DarkSuccessHighlightBrush  = CreateFrozen(Lighten(DarkSuccessColor));
			DarkFailedHighlightBrush   = CreateFrozen(Lighten(DarkFailedColor));
			DarkSkippedHighlightBrush  = CreateFrozen(Lighten(DarkSkippedColor));

			DarkNotBuiltDependencyBorderBrush = CreateFrozen(Lighten(DarkNotBuiltColor, 120));
			DarkBuildingDependencyBorderBrush = CreateFrozen(Lighten(DarkBuildingColor, 120));
			DarkSuccessDependencyBorderBrush  = CreateFrozen(Lighten(DarkSuccessColor,  120));
			DarkFailedDependencyBorderBrush   = CreateFrozen(Lighten(DarkFailedColor,   120));
			DarkSkippedDependencyBorderBrush  = CreateFrozen(Lighten(DarkSkippedColor,  120));

			DarkNotBuiltDependencyBackgroundBrush = CreateFrozen(Lighten(DarkNotBuiltColor, 60));
			DarkBuildingDependencyBackgroundBrush = CreateFrozen(Lighten(DarkBuildingColor, 60));
			DarkSuccessDependencyBackgroundBrush  = CreateFrozen(Lighten(DarkSuccessColor,  60));
			DarkFailedDependencyBackgroundBrush   = CreateFrozen(Lighten(DarkFailedColor,   60));
			DarkSkippedDependencyBackgroundBrush  = CreateFrozen(Lighten(DarkSkippedColor,  60));

			DarkNotBuiltTextBrush  = CreateFrozen(GetTextColor(DarkNotBuiltColor));
			DarkBuildingTextBrush  = CreateFrozen(GetTextColor(DarkBuildingColor));
			DarkSuccessTextBrush   = CreateFrozen(GetTextColor(DarkSuccessColor));
			DarkFailedTextBrush    = CreateFrozen(GetTextColor(DarkFailedColor));
			DarkSkippedTextBrush   = CreateFrozen(GetTextColor(DarkSkippedColor));
		}

		public static SolidColorBrush GetStatusBrush(BuildStatus status)
		{
			if (IsDarkTheme)
			{
				switch (status)
				{
					case BuildStatus.Building: return DarkBuildingBrush;
					case BuildStatus.Success:  return DarkSuccessBrush;
					case BuildStatus.Failed:   return DarkFailedBrush;
					case BuildStatus.Skipped:  return DarkSkippedBrush;
					default:                   return DarkNotBuiltBrush;
				}
			}
			switch (status)
			{
				case BuildStatus.Building: return BuildingBrush;
				case BuildStatus.Success:  return SuccessBrush;
				case BuildStatus.Failed:   return FailedBrush;
				case BuildStatus.Skipped:  return SkippedBrush;
				default:                   return NotBuiltBrush;
			}
		}

		public static SolidColorBrush GetStatusHighlightBrush(BuildStatus status)
		{
			if (IsDarkTheme)
			{
				switch (status)
				{
					case BuildStatus.Building: return DarkBuildingHighlightBrush;
					case BuildStatus.Success:  return DarkSuccessHighlightBrush;
					case BuildStatus.Failed:   return DarkFailedHighlightBrush;
					case BuildStatus.Skipped:  return DarkSkippedHighlightBrush;
					default:                   return DarkNotBuiltHighlightBrush;
				}
			}
			switch (status)
			{
				case BuildStatus.Building: return BuildingHighlightBrush;
				case BuildStatus.Success:  return SuccessHighlightBrush;
				case BuildStatus.Failed:   return FailedHighlightBrush;
				case BuildStatus.Skipped:  return SkippedHighlightBrush;
				default:                   return NotBuiltHighlightBrush;
			}
		}

		public static SolidColorBrush GetStatusDependencyBorderBrush(BuildStatus status)
		{
			if (IsDarkTheme)
			{
				switch (status)
				{
					case BuildStatus.Building: return DarkBuildingDependencyBorderBrush;
					case BuildStatus.Success:  return DarkSuccessDependencyBorderBrush;
					case BuildStatus.Failed:   return DarkFailedDependencyBorderBrush;
					case BuildStatus.Skipped:  return DarkSkippedDependencyBorderBrush;
					default:                   return DarkNotBuiltDependencyBorderBrush;
				}
			}
			switch (status)
			{
				case BuildStatus.Building: return BuildingDependencyBorderBrush;
				case BuildStatus.Success:  return SuccessDependencyBorderBrush;
				case BuildStatus.Failed:   return FailedDependencyBorderBrush;
				case BuildStatus.Skipped:  return SkippedDependencyBorderBrush;
				default:                   return NotBuiltDependencyBorderBrush;
			}
		}

		public static SolidColorBrush GetStatusDependencyBackgroundBrush(BuildStatus status)
		{
			if (IsDarkTheme)
			{
				switch (status)
				{
					case BuildStatus.Building: return DarkBuildingDependencyBackgroundBrush;
					case BuildStatus.Success:  return DarkSuccessDependencyBackgroundBrush;
					case BuildStatus.Failed:   return DarkFailedDependencyBackgroundBrush;
					case BuildStatus.Skipped:  return DarkSkippedDependencyBackgroundBrush;
					default:                   return DarkNotBuiltDependencyBackgroundBrush;
				}
			}
			switch (status)
			{
				case BuildStatus.Building: return BuildingDependencyBackgroundBrush;
				case BuildStatus.Success:  return SuccessDependencyBackgroundBrush;
				case BuildStatus.Failed:   return FailedDependencyBackgroundBrush;
				case BuildStatus.Skipped:  return SkippedDependencyBackgroundBrush;
				default:                   return NotBuiltDependencyBackgroundBrush;
			}
		}

		public static SolidColorBrush GetStatusTextBrush(BuildStatus status)
		{
			if (IsDarkTheme)
			{
				switch (status)
				{
					case BuildStatus.Building: return DarkBuildingTextBrush;
					case BuildStatus.Success:  return DarkSuccessTextBrush;
					case BuildStatus.Failed:   return DarkFailedTextBrush;
					case BuildStatus.Skipped:  return DarkSkippedTextBrush;
					default:                   return DarkNotBuiltTextBrush;
				}
			}
			switch (status)
			{
				case BuildStatus.Building: return BuildingTextBrush;
				case BuildStatus.Success:  return SuccessTextBrush;
				case BuildStatus.Failed:   return FailedTextBrush;
				case BuildStatus.Skipped:  return SkippedTextBrush;
				default:                   return NotBuiltTextBrush;
			}
		}

		private static SolidColorBrush CreateFrozen(WpfColor color)
		{
			SolidColorBrush brush = new SolidColorBrush(color);
			brush.Freeze();
			return brush;
		}

		private static WpfColor Darken(WpfColor color, byte amount = 50)
		{
			return WpfColor.FromRgb(
				(byte) System.Math.Max(0, color.R - amount),
				(byte) System.Math.Max(0, color.G - amount),
				(byte) System.Math.Max(0, color.B - amount));
		}

		private static WpfColor Lighten(WpfColor color, byte amount = 50)
		{
			return WpfColor.FromRgb(
				(byte) System.Math.Min(255, color.R + amount),
				(byte) System.Math.Min(255, color.G + amount),
				(byte) System.Math.Min(255, color.B + amount));
		}

		// Returns black or white depending on which has better contrast against the given color.
		// Uses relative luminance per WCAG 2.1.
		private static WpfColor GetTextColor(WpfColor background)
		{
			double luminance = GetRelativeLuminance(background);
			// Contrast ratio with black  = (luminance + 0.05) / 0.05
			// Contrast ratio with white  = 1.05 / (luminance + 0.05)
			// Black wins when luminance > sqrt(1.05 * 0.05) - 0.05 ≈ 0.1791
			return luminance > 0.1791
				? WpfColor.FromRgb(0, 0, 0)
				: WpfColor.FromRgb(255, 255, 255);
		}

		private static double GetRelativeLuminance(WpfColor color)
		{
			return 0.2126 * ToLinear(color.R)
				 + 0.7152 * ToLinear(color.G)
				 + 0.0722 * ToLinear(color.B);
		}

		private static double ToLinear(byte channel)
		{
			double srgb = channel / 255.0;
			return srgb <= 0.04045
				? srgb / 12.92
				: System.Math.Pow((srgb + 0.055) / 1.055, 2.4);
		}
	}
}
