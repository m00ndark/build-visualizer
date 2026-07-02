using BuildVisualizer.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;

namespace BuildVisualizer.Services
{
	public class ListViewColumnDefinition
	{
		public string Key { get; }
		public string Header { get; }

		public ListViewColumnDefinition(string key, string header)
		{
			Key = key;
			Header = header;
		}
	}

	public class ListViewStateService
	{

		public static readonly IReadOnlyList<ListViewColumnDefinition> AllColumns = new[]
		{
			new ListViewColumnDefinition("Name",          "Project Name"),
			new ListViewColumnDefinition("Status",        "Build Status"),
			new ListViewColumnDefinition("Errors",        "Errors"),
			new ListViewColumnDefinition("Warnings",      "Warnings"),
			new ListViewColumnDefinition("Messages",      "Messages"),
			new ListViewColumnDefinition("BuildStart",    "Build Start"),
			new ListViewColumnDefinition("BuildFinish",   "Build Finish"),
			new ListViewColumnDefinition("Duration",      "Duration"),
			new ListViewColumnDefinition("Configuration", "Configuration"),
			new ListViewColumnDefinition("Platform",      "Platform"),
			new ListViewColumnDefinition("ProjectType",   "Project Type"),
			new ListViewColumnDefinition("Dependencies",  "Dependencies"),
		};

		private readonly UserSettingsService _settings;

		// Full ordered list including hidden columns, preserved so we can re-insert at the right position
		private readonly List<GridViewColumn> _allColumns = new List<GridViewColumn>();

		private bool _suppressSave;

		public ListViewStateService(UserSettingsService settings)
		{
			_settings = settings;
		}

		/// <summary>
		/// Applies persisted widths, order, and visibility to the GridView columns, then hooks
		/// change listeners so future edits are saved automatically.
		/// </summary>
		public void Attach(GridView gridView)
		{
			_allColumns.AddRange(gridView.Columns);
			ApplyState(gridView);
			HookChanges(gridView);
		}

		public void SetColumnVisible(GridView gridView, string key, bool visible)
		{
			GridViewColumn column = _allColumns.FirstOrDefault(c => GridViewColumnTag.GetTag(c) == key);
			if (column == null) return;

			bool currentlyVisible = gridView.Columns.Contains(column);
			if (currentlyVisible == visible) return;

			_suppressSave = true;
			try
			{
				if (visible)
				{
					// Re-insert at the position it holds in _allColumns, after the last visible predecessor
					int allIndex = _allColumns.IndexOf(column);
					int insertAt = 0;
					for (int i = allIndex - 1; i >= 0; i--)
					{
						int visibleIdx = gridView.Columns.IndexOf(_allColumns[i]);
						if (visibleIdx >= 0)
						{
							insertAt = visibleIdx + 1;
							break;
						}
					}
					gridView.Columns.Insert(insertAt, column);
				}
				else
				{
					gridView.Columns.Remove(column);
				}
			}
			finally
			{
				_suppressSave = false;
			}

			SaveHiddenColumns(gridView);
		}

		private void ApplyState(GridView gridView)
		{
			_suppressSave = true;

			try
			{
				// Restore widths
				foreach (GridViewColumn column in _allColumns)
				{
					string key = GridViewColumnTag.GetTag(column);
					if (string.IsNullOrEmpty(key)) continue;

					string raw = _settings.GetString(UserSettings.Collections.ListView, UserSettings.Keys.WidthPrefix + key);
					if (raw != null && double.TryParse(raw, System.Globalization.NumberStyles.Float,
						System.Globalization.CultureInfo.InvariantCulture, out double width) && width > 0)
					{
						column.Width = width;
					}
				}

				// Restore column order (applies to the visible set)
				string orderRaw = _settings.GetString(UserSettings.Collections.ListView, UserSettings.Keys.ColumnOrder);
				if (!string.IsNullOrEmpty(orderRaw))
				{
					string[] keys = orderRaw.Split(',');

					// Reorder _allColumns to match saved order, appending any new columns at the end
					List<GridViewColumn> reordered = new List<GridViewColumn>();
					List<GridViewColumn> remaining = _allColumns.ToList();

					foreach (string key in keys)
					{
						GridViewColumn match = remaining.FirstOrDefault(c => GridViewColumnTag.GetTag(c) == key);
						if (match != null)
						{
							reordered.Add(match);
							remaining.Remove(match);
						}
					}

					reordered.AddRange(remaining);
					_allColumns.Clear();
					_allColumns.AddRange(reordered);
				}

				// Apply hidden columns — remove from GridView
				string hiddenRaw = _settings.GetString(UserSettings.Collections.ListView, UserSettings.Keys.HiddenColumns);
				HashSet<string> hiddenKeys = string.IsNullOrEmpty(hiddenRaw)
					? new HashSet<string>()
					: new HashSet<string>(hiddenRaw.Split(','), StringComparer.Ordinal);

				// Rebuild GridView.Columns in _allColumns order, skipping hidden
				gridView.Columns.Clear();
				foreach (GridViewColumn column in _allColumns)
				{
					string key = GridViewColumnTag.GetTag(column);
					if (!hiddenKeys.Contains(key))
						gridView.Columns.Add(column);
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

			foreach (GridViewColumn column in _allColumns)
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

			_settings.SetString(UserSettings.Collections.ListView, UserSettings.Keys.WidthPrefix + key,
				column.Width.ToString(System.Globalization.CultureInfo.InvariantCulture));
		}

		private void SaveColumnOrder(GridView gridView)
		{
			if (_suppressSave) return;

			// Rebuild _allColumns: walk the current _allColumns list, replacing visible-column slots
			// with the new drag order while leaving hidden columns at their existing positions.
			List<GridViewColumn> newVisible = gridView.Columns.ToList();
			int visibleIdx = 0;
			for (int i = 0; i < _allColumns.Count; i++)
			{
				if (gridView.Columns.Contains(_allColumns[i]))
				{
					if (visibleIdx < newVisible.Count)
						_allColumns[i] = newVisible[visibleIdx++];
				}
				// Hidden columns stay where they are
			}

			string order = string.Join(",", _allColumns
				.Select(c => GridViewColumnTag.GetTag(c))
				.Where(k => !string.IsNullOrEmpty(k)));

			_settings.SetString(UserSettings.Collections.ListView, UserSettings.Keys.ColumnOrder, order);
		}

		private void SaveHiddenColumns(GridView gridView)
		{
			IEnumerable<string> hiddenKeys = _allColumns
				.Select(c => GridViewColumnTag.GetTag(c))
				.Where(k => !string.IsNullOrEmpty(k))
				.Where(k => !gridView.Columns.Any(c => GridViewColumnTag.GetTag(c) == k));

			_settings.SetString(UserSettings.Collections.ListView, UserSettings.Keys.HiddenColumns, string.Join(",", hiddenKeys));
		}
	}
}
