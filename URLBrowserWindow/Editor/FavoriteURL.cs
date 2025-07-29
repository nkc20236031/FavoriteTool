#if UNITY_EDITOR
namespace Editor.URLBrowserWindow
{
	[System.Serializable]
	public class FavoriteURL
	{
		public string _name;
		public string _url;
        
		public FavoriteURL(string name, string url)
		{
			_name = name;
			_url = url;
		}
	}
}
#endif