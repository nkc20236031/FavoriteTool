#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using UnityEngine;
using UnityEditor;

namespace Editor.FavoriteTool
{
	public class FavoriteTool : EditorWindow
	{
		private string _currentUrl = "";
		private string _favoriteNameInput = "";
		private FileType _currentFileType = FileType.Browser;
		private List<FavoriteURL> _favorites = new();
		private Vector2 _scrollPosition;
		private string _favoritesFilePath = "Assets/FavoriteTool/Editor/Data/FavoriteData.json";
		private int _editingIndex = -1;
		private string _editingName = "";
		private string _editingUrl = "";
		private bool _autoSave = true;
		private bool _editingNameOnly;
		private bool _editingUrlOnly;
		private bool _editingTypeOnly;
		private FileType _editingFileType;

		[MenuItem("Tools/FavoriteTool Window")]
		public static void ShowWindow()
		{
			FavoriteTool window = GetWindow<FavoriteTool>();
			window.minSize = new Vector2(400, 300);
			window.Show();
		}

		private void OnEnable()
		{
			LoadFavorites();
		}

		private void OnGUI()
		{
			GUILayout.Space(10);

			// ファイルパス設定セクション
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("設定", EditorStyles.boldLabel);

			// ファイルパス設定
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("保存先:", GUILayout.Width(50));
			string newPath = EditorGUILayout.TextField(_favoritesFilePath);
			if (newPath != _favoritesFilePath)
			{
				_favoritesFilePath = newPath;
			}

			if (GUILayout.Button("参照", GUILayout.Width(50)))
			{
				string selectedPath =
						EditorUtility.SaveFilePanel("お気に入り保存先を選択", "Assets", "Favorite", "json");
				if (!string.IsNullOrEmpty(selectedPath))
				{
					// Assetsフォルダからの相対パスに変換
					if (selectedPath.StartsWith(Application.dataPath))
					{
						_favoritesFilePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
					}
					else
					{
						_favoritesFilePath = selectedPath;
					}
				}
			}

			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);

			// オートセーブ設定
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("オートセーブ:", GUILayout.Width(80));
			_autoSave = EditorGUILayout.Toggle(_autoSave);
			EditorGUILayout.EndHorizontal();

			// インポート/エクスポートボタン
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("インポート"))
			{
				ImportFavorites();
			}

			if (GUILayout.Button("エクスポート"))
			{
				ExportFavorites();
			}

			if (!_autoSave && GUILayout.Button("保存"))
			{
				SaveFavorites();
			}

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.EndVertical();

			GUILayout.Space(5);

			// URL入力セクション
			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("パス/URL を入力:", EditorStyles.label);

			EditorGUILayout.BeginHorizontal();
			_currentUrl = EditorGUILayout.TextField(_currentUrl);

			GUI.enabled = !string.IsNullOrEmpty(_currentUrl);
			if (GUILayout.Button("開く", GUILayout.Width(60)))
			{
				OpenItem(_currentUrl, _currentFileType);
			}

			GUI.enabled = true;

			EditorGUILayout.EndHorizontal();

			GUILayout.Space(3);

			// ファイル種類選択
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("種類:", GUILayout.Width(40));
			_currentFileType = (FileType)EditorGUILayout.EnumPopup(_currentFileType);

			// 参照ボタン
			if (_currentFileType is FileType.Folder or FileType.Application)
			{
				if (GUILayout.Button("参照", GUILayout.Width(50)))
				{
					string selectedPath = "";
					switch (_currentFileType)
					{
						case FileType.Folder:
							selectedPath = EditorUtility.OpenFolderPanel("フォルダを選択", "", "");
							break;
						case FileType.Application:
							selectedPath = EditorUtility.OpenFilePanel("アプリケーションを選択", "", "exe");
							break;
						case FileType.Browser:
							// ブラウザの場合は参照しない
							break;
					}
				
					if (!string.IsNullOrEmpty(selectedPath))
					{
						_currentUrl = selectedPath;
					}
				}
			}

			EditorGUILayout.EndHorizontal();

			GUILayout.Space(5);

			// お気に入り追加セクション
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("名前:", GUILayout.Width(40));
			_favoriteNameInput = EditorGUILayout.TextField(_favoriteNameInput);

			GUI.enabled = !string.IsNullOrEmpty(_currentUrl) && !string.IsNullOrEmpty(_favoriteNameInput);
			if (GUILayout.Button("お気に入りに追加", GUILayout.Width(120)))
			{
				AddToFavorites();
			}

			GUI.enabled = true;

			EditorGUILayout.EndHorizontal();
			EditorGUILayout.EndVertical();

			GUILayout.Space(10);

			// お気に入りセクション
			EditorGUILayout.LabelField("★お気に入り", EditorStyles.boldLabel);

			if (_favorites.Count == 0)
			{
				EditorGUILayout.HelpBox("お気に入りがありません。URLを入力してお気に入りに追加してください。", MessageType.Info);
			}
			else
			{
				_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

				for (int i = 0; i < _favorites.Count; i++)
				{
					EditorGUILayout.BeginVertical("box");
					EditorGUILayout.BeginHorizontal();

					// 並び替えボタン
					EditorGUILayout.BeginVertical(GUILayout.Width(30));

					// 上矢印ボタン（一番上でない場合のみ表示）
					if (i > 0)
					{
						GUILayout.Space(2);
						if (GUILayout.Button(" ▲", GUILayout.Width(25), GUILayout.Height(20)))
						{
							MoveItemUp(i);
						}
					}
					else
					{
						GUILayout.Space(22);
					}

					// 下矢印ボタン（一番下でない場合のみ表示）
					if (i < _favorites.Count - 1)
					{
						GUILayout.Space(4);
						if (GUILayout.Button(" ▼", GUILayout.Width(25), GUILayout.Height(20)))
						{
							MoveItemDown(i);
						}
					}
					else
					{
						GUILayout.Space(22);
					}

					EditorGUILayout.EndVertical();

					// お気に入り名とURL表示/編集
					EditorGUILayout.BeginVertical();

					if (_editingIndex == i)
					{
						// 編集モード
						if (_editingNameOnly)
						{
							// 名前のみ編集
							_editingName = EditorGUILayout.TextField(_editingName);
							EditorGUILayout.LabelField(_favorites[i]._url, EditorStyles.miniLabel);
							EditorGUILayout.LabelField($"タイプ: {_favorites[i]._fileType}", EditorStyles.miniLabel);
						}
						else if (_editingUrlOnly)
						{
							// URLのみ編集
							EditorGUILayout.LabelField(_favorites[i]._name, EditorStyles.boldLabel);
							_editingUrl = EditorGUILayout.TextField(_editingUrl);
							EditorGUILayout.LabelField($"タイプ: {_favorites[i]._fileType}", EditorStyles.miniLabel);
						}
						else if (_editingTypeOnly)
						{
							// タイプのみ編集
							EditorGUILayout.LabelField(_favorites[i]._name, EditorStyles.boldLabel);
							EditorGUILayout.LabelField(_favorites[i]._url, EditorStyles.miniLabel);
							EditorGUILayout.BeginHorizontal();
							EditorGUILayout.LabelField("タイプ:", GUILayout.Width(40));
							_editingFileType = (FileType)EditorGUILayout.EnumPopup(_editingFileType, GUILayout.Width(100));
							EditorGUILayout.EndHorizontal();
						}
						else
						{
							// 両方編集（編集ボタンクリック時）
							_editingName = EditorGUILayout.TextField(_editingName);
							_editingUrl = EditorGUILayout.TextField(_editingUrl);
							EditorGUILayout.BeginHorizontal();
							EditorGUILayout.LabelField("タイプ:", GUILayout.Width(40));
							_editingFileType = (FileType)EditorGUILayout.EnumPopup(_editingFileType, GUILayout.Width(100));
							EditorGUILayout.EndHorizontal();
						}
					}
					else
					{
						// 表示モード
						Rect nameRect = EditorGUILayout.GetControlRect();
						EditorGUI.LabelField(nameRect, _favorites[i]._name, EditorStyles.boldLabel);

						Rect urlRect = EditorGUILayout.GetControlRect(GUILayout.Height(16));
						EditorGUI.LabelField(urlRect, _favorites[i]._url, EditorStyles.miniLabel);

						Rect typeRect = EditorGUILayout.GetControlRect(GUILayout.Height(16));
						EditorGUI.LabelField(typeRect, $"タイプ: {_favorites[i]._fileType}", EditorStyles.miniLabel);

						Event currentEvent = Event.current;

						// 名前のダブルクリック検知
						if (nameRect.Contains(currentEvent.mousePosition))
						{
							if (currentEvent.type == EventType.MouseDown && currentEvent.clickCount == 2)
							{
								StartEditNameOnly(i);
								currentEvent.Use();
							}
							else if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.F2)
							{
								StartEditNameOnly(i);
								currentEvent.Use();
							}
						}

						// URLのダブルクリック検知
						if (urlRect.Contains(currentEvent.mousePosition))
						{
							if (currentEvent.type == EventType.MouseDown && currentEvent.clickCount == 2)
							{
								StartEditUrlOnly(i);
								currentEvent.Use();
							}
							else if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.F2)
							{
								StartEditUrlOnly(i);
								currentEvent.Use();
							}
						}

						// タイプのダブルクリック検知
						if (typeRect.Contains(currentEvent.mousePosition))
						{
							if (currentEvent.type == EventType.MouseDown && currentEvent.clickCount == 2)
							{
								StartEditTypeOnly(i);
								currentEvent.Use();
							}
							else if (currentEvent.type == EventType.KeyDown && currentEvent.keyCode == KeyCode.F2)
							{
								StartEditTypeOnly(i);
								currentEvent.Use();
							}
						}
					}

					EditorGUILayout.EndVertical();

					EditorGUILayout.BeginVertical(GUILayout.Width(80));

					if (_editingIndex == i)
					{
						// Enterキーを検知して保存
						if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
						{
							SaveEdit(i);
							Event.current.Use();
						}

						// 関係のない場所でクリックした場合は保存
						if (Event.current.type == EventType.MouseDown)
						{
							SaveEdit(i);
							Event.current.Use();
						}

						// 編集モードのボタン
						if (GUILayout.Button("保存"))
						{
							SaveEdit(i);
						}

						if (GUILayout.Button("キャンセル"))
						{
							CancelEdit();
						}

						GUILayout.Space(22);
					}
					else
					{
						// 通常モードのボタン
						if (GUILayout.Button("開く"))
						{
							OpenItem(_favorites[i]._url, _favorites[i]._fileType);
						}

						if (GUILayout.Button("編集"))
						{
							StartEdit(i);
						}

						// 削除ボタン
						var originalColor = GUI.backgroundColor;
						var originalContentColor = GUI.contentColor;
						GUI.backgroundColor = Color.red;
						GUI.contentColor = Color.white;

						if (GUILayout.Button("削除"))
						{
							if (EditorUtility.DisplayDialog("確認",
									    $"'{_favorites[i]._name}' をお気に入りから削除しますか？",
									    "削除", "キャンセル"))
							{
								_favorites.RemoveAt(i);
								TrySave();
								// 編集中のアイテムが削除された場合、編集をキャンセル
								if (_editingIndex == i)
								{
									CancelEdit();
								}
								else if (_editingIndex > i)
								{
									_editingIndex--;
								}

								EditorGUILayout.EndVertical();
								EditorGUILayout.EndHorizontal();
								EditorGUILayout.EndVertical();
								break;
							}
						}

						GUI.backgroundColor = originalColor;
						GUI.contentColor = originalContentColor;
					}

					EditorGUILayout.EndVertical();
					EditorGUILayout.EndHorizontal();
					EditorGUILayout.EndVertical();

					GUILayout.Space(5);
				}

				EditorGUILayout.EndScrollView();
			}

			GUILayout.Space(10);

			// フッター
			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("お気に入りをクリア"))
			{
				if (EditorUtility.DisplayDialog("確認",
						    "すべてのお気に入りを削除しますか？",
						    "削除", "キャンセル"))
				{
					_favorites.Clear();
					CancelEdit();
					TrySave();
				}
			}

			EditorGUILayout.EndHorizontal();
		}

		private void MoveItemUp(int index)
		{
			if (index > 0)
			{
				(_favorites[index], _favorites[index - 1]) = (_favorites[index - 1], _favorites[index]);

				// 編集中のアイテムのインデックスも更新
				if (_editingIndex == index)
				{
					_editingIndex = index - 1;
				}
				else if (_editingIndex == index - 1)
				{
					_editingIndex = index;
				}

				TrySave();
			}
		}

		private void MoveItemDown(int index)
		{
			if (index < _favorites.Count - 1)
			{
				(_favorites[index], _favorites[index + 1]) = (_favorites[index + 1], _favorites[index]);

				// 編集中のアイテムのインデックスも更新
				if (_editingIndex == index)
				{
					_editingIndex = index + 1;
				}
				else if (_editingIndex == index + 1)
				{
					_editingIndex = index;
				}

				TrySave();
			}
		}

		private void TrySave()
		{
			if (_autoSave)
			{
				SaveFavorites();
			}
		}

		private void ImportFavorites()
		{
			string selectedPath = EditorUtility.OpenFilePanel("お気に入りファイルを選択", "Assets", "json");
			if (!string.IsNullOrEmpty(selectedPath))
			{
				try
				{
					string json = File.ReadAllText(selectedPath);
					FavoritesData data = JsonUtility.FromJson<FavoritesData>(json);

					if (data != null && data._favorites != null)
					{
						if (EditorUtility.DisplayDialog("確認",
								    $"{data._favorites.Count}件のお気に入りをインポートしますか？\n現在のお気に入りは上書きされます。",
								    "インポート", "キャンセル"))
						{
							_favorites = data._favorites;
							CancelEdit();
							TrySave();
							EditorUtility.DisplayDialog("成功", $"{_favorites.Count}件のお気に入りをインポートしました。", "OK");
						}
					}
					else
					{
						EditorUtility.DisplayDialog("エラー", "無効なファイル形式です。", "OK");
					}
				}
				catch (Exception e)
				{
					EditorUtility.DisplayDialog("エラー", $"インポートに失敗しました: {e.Message}", "OK");
				}
			}
		}

		private void ExportFavorites()
		{
			string selectedPath =
					EditorUtility.SaveFilePanel("お気に入りをエクスポート", "Assets", "Favorite_Export", "json");
			if (!string.IsNullOrEmpty(selectedPath))
			{
				try
				{
					FavoritesData data = new FavoritesData();
					data._favorites = _favorites;

					string json = JsonUtility.ToJson(data, true);
					File.WriteAllText(selectedPath, json);

					EditorUtility.DisplayDialog("成功", $"{_favorites.Count}件のお気に入りをエクスポートしました。", "OK");
				}
				catch (Exception e)
				{
					EditorUtility.DisplayDialog("エラー", $"エクスポートに失敗しました: {e.Message}", "OK");
				}
			}
		}

		private void StartEdit(int index)
		{
			_editingIndex = index;
			_editingName = _favorites[index]._name;
			_editingUrl = _favorites[index]._url;
			_editingFileType = _favorites[index]._fileType;
			_editingNameOnly = false;
			_editingUrlOnly = false;
		}

		private void StartEditNameOnly(int index)
		{
			_editingIndex = index;
			_editingName = _favorites[index]._name;
			_editingUrl = _favorites[index]._url;
			_editingFileType = _favorites[index]._fileType;
			_editingNameOnly = true;
			_editingUrlOnly = false;
			_editingTypeOnly = false;
		}

		private void StartEditUrlOnly(int index)
		{
			_editingIndex = index;
			_editingName = _favorites[index]._name;
			_editingUrl = _favorites[index]._url;
			_editingFileType = _favorites[index]._fileType;
			_editingNameOnly = false;
			_editingUrlOnly = true;
			_editingTypeOnly = false;
		}

		private void StartEditTypeOnly(int index)
		{
			_editingIndex = index;
			_editingName = _favorites[index]._name;
			_editingUrl = _favorites[index]._url;
			_editingFileType = _favorites[index]._fileType;
			_editingNameOnly = false;
			_editingUrlOnly = false;
			_editingTypeOnly = true;
		}

		private void SaveEdit(int index)
		{
			string nameToCheck = _editingNameOnly ? _editingName.Trim() : _favorites[index]._name;
			string urlToCheck = _editingUrlOnly ? _editingUrl.Trim() : _favorites[index]._url;

			if (string.IsNullOrEmpty(nameToCheck) || string.IsNullOrEmpty(urlToCheck))
			{
				EditorUtility.DisplayDialog("エラー", "名前とURLの両方を入力してください。", "OK");
				return;
			}

			// 重複チェック（名前とURLのみ）
			for (int i = 0; i < _favorites.Count; i++)
			{
				if (i != index && (_favorites[i]._name == nameToCheck || _favorites[i]._url == urlToCheck))
				{
					EditorUtility.DisplayDialog("エラー", "同じ名前またはURLのお気に入りが既に存在します。", "OK");
					return;
				}
			}

			if (_editingNameOnly)
			{
				_favorites[index]._name = _editingName.Trim();
			}
			else if (_editingUrlOnly)
			{
				_favorites[index]._url = _editingUrl.Trim();
			}
			else if (_editingTypeOnly)
			{
				_favorites[index]._fileType = _editingFileType;
			}
			else
			{
				_favorites[index]._name = _editingName.Trim();
				_favorites[index]._url = _editingUrl.Trim();
				_favorites[index]._fileType = _editingFileType;
			}

			TrySave();
			CancelEdit();
		}

		private void CancelEdit()
		{
			_editingIndex = -1;
			_editingName = "";
			_editingUrl = "";
			_editingNameOnly = false;
			_editingUrlOnly = false;
			_editingTypeOnly = false;
		}

		private void OpenItem(string path, FileType fileType)
		{
			if (string.IsNullOrEmpty(path))
			{
				EditorUtility.DisplayDialog("エラー", "パスが入力されていません。", "OK");
				return;
			}

			try
			{
				switch (fileType)
				{
					case FileType.Browser:
						OpenURL(path);
						break;
					
					case FileType.Folder:
						if (Directory.Exists(path))
						{
							Process.Start("explorer.exe", path);
						}
						else
						{
							EditorUtility.DisplayDialog("エラー", $"フォルダが見つかりません: {path}", "OK");
						}
						break;
					
					case FileType.Application:
						if (File.Exists(path))
						{
							Process.Start(path);
						}
						else
						{
							EditorUtility.DisplayDialog("エラー", $"アプリケーションが見つかりません: {path}", "OK");
						}
						break;
				}
			}
			catch (Exception e)
			{
				EditorUtility.DisplayDialog("エラー", $"ファイルを開けませんでした: {e.Message}", "OK");
			}
		}

		private void OpenURL(string url)
		{
			if (string.IsNullOrEmpty(url))
			{
				EditorUtility.DisplayDialog("エラー", "URLが入力されていません。", "OK");
				return;
			}

			// HTTPまたはHTTPSプロトコルが含まれていない場合は追加
			if (!url.StartsWith("http://") && !url.StartsWith("https://"))
			{
				url = "https://" + url;
			}

			try
			{
				Application.OpenURL(url);
			}
			catch (Exception e)
			{
				EditorUtility.DisplayDialog("エラー", $"URLを開けませんでした: {e.Message}", "OK");
			}
		}

		private void AddToFavorites()
		{
			string url = _currentUrl;
			string name = _favoriteNameInput;

			// 重複チェック
			foreach (var fav in _favorites)
			{
				if (fav._url == url || fav._name == name)
				{
					EditorUtility.DisplayDialog("エラー", "同じ名前またはURLのお気に入りが既に存在します。", "OK");
					return;
				}
			}

			_favorites.Add(new FavoriteURL(name, url, _currentFileType));
			TrySave();

			// 入力フィールドをクリア
			_favoriteNameInput = "";

			// 編集中の場合はキャンセル
			if (_editingIndex != -1)
			{
				CancelEdit();
			}

			EditorUtility.DisplayDialog("成功", $"'{name}' をお気に入りに追加しました。", "OK");
		}

		private void SaveFavorites()
		{
			try
			{
				// Editorフォルダが存在しない場合は作成
				string editorDir = Path.GetDirectoryName(_favoritesFilePath);
				if (!Directory.Exists(editorDir))
				{
					Directory.CreateDirectory(editorDir);
				}

				FavoritesData data = new FavoritesData();
				data._favorites = _favorites;

				string json = JsonUtility.ToJson(data, true);
				File.WriteAllText(_favoritesFilePath, json);
			}
			catch (Exception e)
			{
				throw new Exception($"お気に入りの保存に失敗しました: {e.Message}");
			}
		}

		private void LoadFavorites()
		{
			try
			{
				if (File.Exists(_favoritesFilePath))
				{
					string json = File.ReadAllText(_favoritesFilePath);
					FavoritesData data = JsonUtility.FromJson<FavoritesData>(json);

					if (data != null && data._favorites != null)
					{
						_favorites = data._favorites;
					}
				}
			}
			catch (Exception e)
			{
				_favorites = new List<FavoriteURL>();
				throw new Exception($"お気に入りの読み込みに失敗しました: {e.Message}");
			}
		}
	}
}
#endif
