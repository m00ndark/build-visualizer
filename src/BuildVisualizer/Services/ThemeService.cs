using Microsoft.VisualStudio.PlatformUI;
using System;
using System.Drawing;

namespace BuildVisualizer.Services
{
	public class ThemeService : IDisposable
	{
		private bool _disposed;

		public event EventHandler ThemeChanged;

		public bool IsDarkTheme => GetIsDarkTheme();

		public ThemeService()
		{
			VSColorTheme.ThemeChanged += OnVsThemeChanged;
		}

		private void OnVsThemeChanged(ThemeChangedEventArgs e)
		{
			ThemeChanged?.Invoke(this, EventArgs.Empty);
		}

		private static bool GetIsDarkTheme()
		{
			Color background = VSColorTheme.GetThemedColor(EnvironmentColors.ToolWindowBackgroundColorKey);
			double luminance = (0.299 * background.R + 0.587 * background.G + 0.114 * background.B) / 255.0;
			return luminance < 0.5;
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				VSColorTheme.ThemeChanged -= OnVsThemeChanged;
				_disposed = true;
			}
		}
	}
}
