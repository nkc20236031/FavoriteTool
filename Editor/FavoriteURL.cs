#if UNITY_EDITOR
namespace Editor.FavoriteTool
{
	[System.Serializable]
	public class FavoriteURL
	{
		public string _name;
		public string _url;
		public FileType _fileType;
        
		public FavoriteURL(string name, string url)
		{
			_name = name;
			_url = url;
			_fileType = FileType.Browser; // デフォルトはブラウザ
		}

		public FavoriteURL(string name, string url, FileType fileType)
		{
			_name = name;
			_url = url;
			_fileType = fileType;
		}
	}
}
#endif