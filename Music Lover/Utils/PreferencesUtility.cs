using System.Threading.Tasks;
using Android.Content;
using Android.Preferences;

namespace Music_Lover.Utils
{
    public class PreferencesUtility
    {
        private const string ARTIST_SORT_ORDER = "artist_sort_order";
        private const string ARTIST_SONG_SORT_ORDER = "artist_song_sort_order";
        private const string ARTIST_ALBUM_SORT_ORDER = "artist_album_sort_order";
        private const string ALBUM_SORT_ORDER = "album_sort_order";
        private const string ALBUM_SONG_SORT_ORDER = "album_song_sort_order";
        private const string SONG_SORT_ORDER = "song_sort_order";
        private const string TOGGLE_ANIMATIONS = "toggle_animations";
        private const string TOGGLE_SYSTEM_ANIMATIONS = "toggle_system_animations";
        private const string TOGGLE_ARTIST_GRID = "toggle_artist_grid";
        private const string TOGGLE_ALBUM_GRID = "toggle_album_grid";
        private const string TOGGLE_HEADPHONE_PAUSE = "toggle_headphone_pause";
        private const string START_PAGE_INDEX = "start_page_index";
        private const string START_PAGE_PREFERENCE_LASTOPENED = "start_page_preference_latopened";
        private const string NOW_PLAYNG_THEME_VALUE = "now_playing_theme_value";
        private const string NOW_PLAYING_SELECTOR = "now_paying_selector";
        private const string THEME_PREFERNCE = "theme_preference";
        private const string LAST_ADDED_CUTOFF = "last_added_cutoff";
        private const string GESTURE = "gesture";


        private static PreferencesUtility _instance;
        private static ISharedPreferences _preferences;

        public PreferencesUtility(Context context)
        {
            _preferences = PreferenceManager.GetDefaultSharedPreferences(context);
        }

        public static PreferencesUtility GetInstance(Context context) => _instance ?? (_instance = new PreferencesUtility(context.ApplicationContext));

        public void SetOnSharedPreferenceChangeListener(ISharedPreferencesOnSharedPreferenceChangeListener listener)
        {
            _preferences.RegisterOnSharedPreferenceChangeListener(listener);
        }

        public bool GetAnimations() => _preferences.GetBoolean(TOGGLE_ANIMATIONS, true);

        public bool GetSystemAnimations() => _preferences.GetBoolean(TOGGLE_SYSTEM_ANIMATIONS, true);

        public bool IsArtistInGrid() => _preferences.GetBoolean(TOGGLE_ARTIST_GRID, true);

        public async Task SetArtistInGrid(bool b)
        {
            await Task.Run(() =>
            {
                var editor = _preferences.Edit();
                editor.PutBoolean(TOGGLE_ARTIST_GRID, b);
                editor.Apply();
            });
        }

        public bool IsGestureEnabled()
        {
            return _preferences.GetBoolean(GESTURE, true);
        }

        public bool IsAlbumInGrid() => _preferences.GetBoolean(TOGGLE_ALBUM_GRID, true);

        public async Task SetAlbumInGrid(bool b)
        {
            await Task.Run(() =>
            {
                var editor = _preferences.Edit();
                editor.PutBoolean(TOGGLE_ALBUM_GRID, b);
                editor.Apply();
            });
        }

        public bool PauseOnDetach() => _preferences.GetBoolean(TOGGLE_HEADPHONE_PAUSE, true);

        public int GetStartPageIndex() => _preferences.GetInt(START_PAGE_INDEX, 0);

        public async Task SetStartPageIndex(int index)
        {
            await Task.Run(() =>
            {
                var editor = _preferences.Edit();
                editor.PutInt(START_PAGE_INDEX, index);
                editor.Apply();
            });
        }

        public bool IsLastOpenedPageAsStart() => _preferences.GetBoolean(START_PAGE_PREFERENCE_LASTOPENED, true);

        public async Task SetLastOpenedPageAsStart(bool b)
        {
            await Task.Run(() =>
            {
                var editor = _preferences.Edit();
                editor.PutBoolean(START_PAGE_PREFERENCE_LASTOPENED, b);
                editor.Apply();
            });
        }

        private async Task SetSortOrder(string key, string value)
        {
            await Task.Run(() =>
            {
                var editor = _preferences.Edit();
                editor.PutString(key, value);
                editor.Apply();
            });
        }

        public string GetArtistSortOrder() => _preferences.GetString(ARTIST_SORT_ORDER, SortOrder.Artist.ARTIST_A_Z);

        public async Task SetArtistSortOrder(string value)
        {
            await SetSortOrder(ARTIST_SORT_ORDER, value);
        }

        public string GetArtistSongSortOrder() =>
            _preferences.GetString(ARTIST_SONG_SORT_ORDER, SortOrder.ArtistSong.SONG_A_Z);

        public string GetAlbumSortOrder() => _preferences.GetString(ALBUM_SORT_ORDER, SortOrder.Album.ALBUM_A_Z);

        public async Task SetAlbumSortOrder(string value)
        {
            await SetSortOrder(ALBUM_SORT_ORDER, value);
        }

        public string GetAlbumSongSortOrder() =>
            _preferences.GetString(ALBUM_SONG_SORT_ORDER, SortOrder.AlbumSong.SONG_TRACK_LIST);

        public async Task SetAlbumSongSortOrder(string value)
        {
            await SetSortOrder(ALBUM_SONG_SORT_ORDER, value);
        }

        public string GetSongSortOrder() => _preferences.GetString(SONG_SORT_ORDER, SortOrder.Song.SONG_A_Z);

        public async Task SetSongSortOrder(string value)
        {
            await SetSortOrder(SONG_SORT_ORDER, value);
        }

        public bool NowPlayingThemeChanged() => _preferences.GetBoolean(NOW_PLAYNG_THEME_VALUE, false);

        public async Task SetNowPlayingTheme(bool value)
        {
            await Task.Run(() =>
            {
                var editor = _preferences.Edit();
                editor.PutBoolean(NOW_PLAYNG_THEME_VALUE, value);
                editor.Apply();
            });
        }

        public void SetLastAddedCutoff(long mil)
        {
            _preferences.Edit().PutLong(LAST_ADDED_CUTOFF, mil).Apply();
        }

        public long GetLastAddedCutoff()
        {
            return _preferences.GetLong(LAST_ADDED_CUTOFF, 0L);
        }
    }
}