using BuildVisualizer.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;

namespace BuildVisualizer.Services
{
	public class ListViewStateService
	{
		private const string Collection = "BuildVisualizer\\ListView";
		private const string WidthKeyPrefix = "Width_";
		private const string OrderKey = "ColumnOrder";

		private readonly UserSettingsService _settings;
		private bool _suppressSave;

		public ListViewStateService(UserSettingsService settings)
		{
			_settings = settings;
		}

		/// <summary>
		/// Applies persisted widths and order to the GridView columns, then hooks
		/// change listeners so future edits are saved automatically.
		/// The Tag property on each GridViewColumn must be set to its stable key.
		/// </summary>
		public void Attach(GridView gridView)
		{
			ApplyState(gridView);
			HookChanges(gridView);
		}

		private void ApplyState(GridView gridView)
		{
			_suppressSave = true;

			try
			{
				// Restore widths
				foreach (GridViewColumn column in gridView.Columns)
				{
					string key = GridViewColumnTag.GetTag(column);
					if (string.IsNullOrEmpty(key)) continue;

					string raw = _settings.GetString(Collection, WidthKeyPrefix + key);
					if (raw != null && double.TryParse(raw, System.Globalization.NumberStyles.Float,
						System.Globalization.CultureInfo.InvariantCulture, out double width) && width > 0)
					{
						column.Width = width;
					}
				}

				// Restore column order
				string orderRaw = _settings.GetString(Collection, OrderKey);
				if (!string.IsNullOrEmpty(orderRaw))
				{
					string[] keys = orderRaw.Split(',');
					List<GridViewColumn> ordered = new List<GridViewColumn>();
					List<GridViewColumn> remaining = gridView.Columns.ToList();

					foreach (string key in keys)
					{
						GridViewColumn match = remaining.FirstOrDefault(c => (GridViewColumnTag.GetTag(c)) == key);
						if (match != null)
						{
							ordered.Add(match);
							remaining.Remove(match);
						}
					}

					// Append any columns not in the saved order (newly added columns)
					ordered.AddRange(remaining);

					// Reorder by removing and re-inserting
					for (int i = 0; i < ordered.Count; i++)
					{
						int current = gridView.Columns.IndexOf(ordered[i]);
						if (current != i)
						{
							gridView.Columns.RemoveAt(current);
							gridView.Columns.Insert(i, ordered[i]);
						}
					}
				}
			}
			finally
			{
				_suppressSave = false;
			}
		}

		private void HookChanges(GridView gridView)
		{
			DependencyPropertyDescriptor widthDescriptor =
				DependencyPropertyDescriptor.FromProperty(GridViewColumn.WidthProperty, typeof(GridViewColumn));

			foreach (GridViewColumn column in gridView.Columns)
			{
				GridViewColumn captured = column;
				widthDescriptor.AddValueChanged(captured, (s, e) => OnColumnWidthChanged(captured));
			}

			gridView.Columns.CollectionChanged += (s, e) => SaveColumnOrder(gridView);
		}

		private void OnColumnWidthChanged(GridViewColumn column)
		{
			if (_suppressSave) return;

			string key = GridViewColumnTag.GetTag(column);
			if (string.IsNullOrEmpty(key) || double.IsNaN(column.Width)) return;

			_settings.SetString(Collection, WidthKeyPrefix + key,
				column.Width.ToString(System.Globalization.CultureInfo.InvariantCulture));
		}

		private void SaveColumnOrder(GridView gridView)
		{
			if (_suppressSave) return;

			string order = string.Join(",", gridView.Columns
				.Select(c => GridViewColumnTag.GetTag(c))
				.Where(k => !string.IsNullOrEmpty(k)));

			_settings.SetString(Collection, OrderKey, order);
		}
	}
}
