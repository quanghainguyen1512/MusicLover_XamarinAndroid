using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Music_Lover.Activities;
using Music_Lover.Adapters;
using Music_Lover.Listeners;
using Music_Lover.Loader;
using Music_Lover.Utils;
using Music_Lover.Widgets;
using Fragment = Android.Support.V4.App.Fragment;

namespace Music_Lover.AppFragments
{
    public class SongsFragment : Fragment, IMusicStateListener
    {
        private RecyclerView _recyclerView;
        private PreferencesUtility _preferences;
        private SongListAdapter _songListAdapter;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _preferences = PreferencesUtility.GetInstance(Activity);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var rootView = inflater.Inflate(Resource.Layout.fragment_recyclerview, container, false);
            _recyclerView = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerview);
            _recyclerView.SetLayoutManager(new LinearLayoutManager(Activity));
            var fastcr = rootView.FindViewById<FastScroller>(Resource.Id.fastscroller);
            fastcr.SetRecyclerView(_recyclerView);

            Task.Run(async () =>
            {
                await LoadSong();
            });
            ((BaseActivity) Activity).SetMusicStateListener(this);

            return rootView;
        }

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            HasOptionsMenu = true;
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            base.OnCreateOptionsMenu(menu, inflater);
            inflater.Inflate(Resource.Menu.song_sort_by, menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_sort_by_az:
                {
                    Task.Run(async () =>
                    {
                        await _preferences.SetSongSortOrder(SortOrder.Song.SONG_A_Z);
                        await ReloadAdapter();
                    });
                    return true;
                }
                case Resource.Id.menu_sort_by_za:
                {
                    Task.Run(async () =>
                    {
                        await _preferences.SetSongSortOrder(SortOrder.Song.SONG_Z_A);
                        await ReloadAdapter();
                    });
                    return true;
                }
                case Resource.Id.menu_sort_by_artist:
                {
                    Task.Run(async () =>
                    {
                        await _preferences.SetSongSortOrder(SortOrder.Song.SONG_ARTIST);
                        await ReloadAdapter();
                    });
                    return true;
                }
                case Resource.Id.menu_sort_by_album:
                {
                    Task.Run(async () =>
                    {
                        await _preferences.SetSongSortOrder(SortOrder.Song.SONG_ALBUM);
                        await ReloadAdapter();
                    });
                    return true;
                }
                case Resource.Id.menu_sort_by_year:
                {
                    Task.Run(async () =>
                    {
                        await _preferences.SetSongSortOrder(SortOrder.Song.SONG_YEAR);
                        await ReloadAdapter();
                    });
                    return true;
                }
                case Resource.Id.menu_sort_by_duration:
                {
                    Task.Run(async () =>
                    {
                        await _preferences.SetSongSortOrder(SortOrder.Song.SONG_DURATION);
                        await ReloadAdapter();
                    });
                    return true;
                }
            }
            return base.OnOptionsItemSelected(item);
        }

        public void OnMetaChanged()
        {
            if (_songListAdapter != null)
                _songListAdapter.NotifyDataSetChanged();
        }

        public void OnPlaylistChanged()
        {
        }

        public void RestartLoader()
        {
        }

        private async Task LoadSong()
        {
            if (Activity is null)
                return;

            await Task.Run(() =>
            {
                _songListAdapter = new SongListAdapter((AppCompatActivity) Activity, SongLoader.GetAllSongs(Activity), false);
            });

            _recyclerView.AddItemDecoration(new DividerItemDecoration(Activity, DividerItemDecoration.Vertical));
        }

        private async Task ReloadAdapter()
        {
            await Task.Run(() =>
            {
                var list = SongLoader.GetAllSongs(Activity);
                _songListAdapter.UpdateData(list);
            });
            _songListAdapter.NotifyDataSetChanged();
        }
    }
}