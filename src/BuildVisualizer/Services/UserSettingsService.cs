using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;

namespace BuildVisualizer.Services
{
	public class UserSettingsService
	{
		private readonly IVsWritableSettingsStore _store;

		public UserSettingsService(IVsWritableSettingsStore store)
		{
			_store = store;
		}

		public void SetString(string collection, string key, string value)
		{
			if (_store == null) return;

			EnsureCollection(collection);
			_store.SetString(collection, key, value);
		}

		public string GetString(string collection, string key, string defaultValue = null)
		{
			if (_store == null) return defaultValue;

			if (_store.PropertyExists(collection, key, out int exists) != VSConstants.S_OK || exists == 0)
				return defaultValue;

			return _store.GetString(collection, key, out string value) == VSConstants.S_OK
				? value
				: defaultValue;
		}

		private void EnsureCollection(string collection)
		{
			if (_store.CollectionExists(collection, out int exists) != VSConstants.S_OK || exists == 0)
				_store.CreateCollection(collection);
		}
	}
}
