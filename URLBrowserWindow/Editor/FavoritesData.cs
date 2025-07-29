#if UNITY_EDITOR
using System.Collections.Generic;

namespace Editor.URLBrowserWindow
{
	[System.Serializable]
	public class FavoritesData
	{
		public List<FavoriteURL> _favorites = new();
	}
}
#endif