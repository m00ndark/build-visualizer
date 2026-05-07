using System.Windows.Media;
using WpfColor = System.Windows.Media.Color;

namespace BuildVisualizer.Resources
{
	public static class Colors
	{
		private static readonly WpfColor NotBuiltColor  = WpfColor.FromRgb(212, 212, 212);
		private static readonly WpfColor BuildingColor  = WpfColor.FromRgb(147, 197, 253);
		private static readonly WpfColor SuccessColor   = WpfColor.FromRgb(134, 239, 172);
		private static readonly WpfColor FailedColor    = WpfColor.FromRgb(252, 165, 165);
		private static readonly WpfColor SkippedColor   = WpfColor.FromRgb(158, 158, 158);

		public static readonly SolidColorBrush NotBuiltBrush;
		public static readonly SolidColorBrush BuildingBrush;
		public static readonly SolidColorBrush SuccessBrush;
		public static readonly SolidColorBrush FailedBrush;
		public static readonly SolidColorBrush SkippedBrush;

		public static readonly SolidColorBrush NotBuiltHighlightBrush;
		public static readonly SolidColorBrush BuildingHighlightBrush;
		public static readonly SolidColorBrush SuccessHighlightBrush;
		public static readonly SolidColorBrush FailedHighlightBrush;
		public static readonly SolidColorBrush SkippedHighlightBrush;

		public static readonly SolidColorBrush NotBuiltTextBrush;
		public static readonly SolidColorBrush BuildingTextBrush;
		public static readonly SolidColorBrush SuccessTextBrush;
		public static readonly SolidColorBrush FailedTextBrush;
		public static readonly SolidColorBrush SkippedTextBrush;

		static Colors()
		{
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

			NotBuiltTextBrush  = CreateFrozen(GetTextColor(NotBuiltColor));
			BuildingTextBrush  = CreateFrozen(GetTextColor(BuildingColor));
			SuccessTextBrush   = CreateFrozen(GetTextColor(SuccessColor));
			FailedTextBrush    = CreateFrozen(GetTextColor(FailedColor));
			SkippedTextBrush   = CreateFrozen(GetTextColor(SkippedColor));
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
