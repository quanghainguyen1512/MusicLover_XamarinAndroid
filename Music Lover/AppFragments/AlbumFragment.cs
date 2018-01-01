using Android.Graphics;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V7.Widget;
using Android.Views;
using Music_Lover.Adapters;
using Music_Lover.Loader;
using Music_Lover.Utils;
using Music_Lover.Widgets;
using System;
using System.Threading.Tasks;

namespace Music_Lover.AppFragments
{
    public class AlbumFragment : Fragment
    {
        private AlbumAdapter _adapter;
        private RecyclerView _recyclerView;
        private FastScroller _fastScoller;
        private GridLayoutManager _gridLayoutManager;
        private RecyclerView.ItemDecoration _decoration;
        private PreferencesUtility _preferences;
        private bool _isGrid;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _preferences = PreferencesUtility.GetInstance(Activity);
            _isGrid = _preferences.IsAlbumInGrid();
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var rootView = inflater.Inflate(Resource.Layout.fragment_recyclerview, container, false);

            _recyclerView = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerview);
            _fastScoller = rootView.FindViewById<FastScroller>(Resource.Id.fastscroller);

            _gridLayoutManager = new GridLayoutManager(Activity, 2);
            SetLayoutManager();

            if (Activity != null)
            {
                Task.Run(async () =>
                {
                    await LoadAlbums();
                });
            }

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
            inflater.Inflate(Resource.Menu.album_sort_by, menu);
            inflater.Inflate(Resource.Menu.menu_show_as, menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.menu_sort_by_az:
                {
                    Task.Run(async () =>
                    {
                        await _preferences.SetAlbumSortOrder(SortOrder.Album.ALBUM_A_Z);
                        await ReloadAdapter();
                    });
                    return true;
                }
                case Resource.Id.menu_sort_by_za:
                {
                    Task.Run(async () =>
                    {
                        await _preferences.SetAlbumSortOrder(SortOrder.Album.ALBUM_Z_A);
                        await ReloadAdapter();
                    });
                    return true;
                }
                case Resource.Id.menu_sort_by_artist:
                {
                    Task.Run(async () =>
                    {
                        await _preferences.SetAlbumSortOrder(SortOrder.Album.ALBUM_ARTIST);
                        await ReloadAdapter();
                    });
                    return true;
                }
                case Resource.Id.menu_sort_by_year:
                {
                    Task.Run(async () =>
                    {
                        await _preferences.SetAlbumSortOrder(SortOrder.Album.ALBUM_YEAR);
                        await ReloadAdapter();
                    });
                    return true;
                }
                case Resource.Id.menu_sort_by_number_of_songs:
                {
                    Task.Run(async () =>
                    {
                        await _preferences.SetAlbumSongSortOrder(SortOrder.Album.ALBUM_NUMBER_OF_SONGS);
                        await ReloadAdapter();
                    });
                    return true;
                }
                case Resource.Id.menu_show_as_list:
                {
                    _isGrid = false;
                    Task.Run(async () =>
                    {
                        await _preferences.SetAlbumInGrid(false);
                        await UpdateLayoutManager(1);
                    });
                    return true;
                }
                case Resource.Id.menu_show_as_grid:
                {
                    _isGrid = true;
                    Task.Run(async () =>
                    {
                        await _preferences.SetAlbumInGrid(true);
                        await UpdateLayoutManager(2);
                    });
                    return true;
                }
            }
            return base.OnOptionsItemSelected(item);
        }

        private async Task LoadAlbums()
        {
            await Task.Run(() =>
            {
                if (Activity is null)
                    return;
                _adapter = new AlbumAdapter(Activity, AlbumLoader.GetAllAlbums(Activity));
                SetItemDecoration();
                _recyclerView.SetAdapter(_adapter);
            });
        }

        private void SetLayoutManager()
        {
            if (_isGrid)
            {
                _gridLayoutManager.SpanCount = 2;
                _fastScoller.Visibility = ViewStates.Gone;
            }
            else
            {
                _gridLayoutManager.SpanCount = 1;
                _fastScoller.Visibility = ViewStates.Visible;
                _fastScoller.SetRecyclerView(_recyclerView);
            }
            _recyclerView.SetLayoutManager(_gridLayoutManager);
        }

        private void SetItemDecoration()
        {
            if (_isGrid)
            {
                var space = Activity.Resources.GetDimensionPixelSize(Resource.Dimension.spacing_card_album_grid);
                _decoration = new SpacesItemDecoration(space);
            }
            else
                _decoration = new DividerItemDecoration(Activity, DividerItemDecoration.Vertical);
            _recyclerView.AddItemDecoration(_decoration);
        }

        private async Task UpdateLayoutManager(int col)
        {
            SetLayoutManager();
            _recyclerView.RemoveItemDecoration(_decoration);
            await LoadAlbums();
        }

        private async Task ReloadAdapter()
        {
            await Task.Run(() => 
            {
                var list = AlbumLoader.GetAllAlbums(Activity);
                _adapter.UpdateData(list);
            });
            _adapter.NotifyDataSetChanged();
        }

        public class SpacesItemDecoration : RecyclerView.ItemDecoration
        {
            private int _space;

            public SpacesItemDecoration(int space)
            {
                _space = space;
            }

            public override void GetItemOffsets(Rect outRect, View view, RecyclerView parent, RecyclerView.State state)
            {
                outRect.Left = _space;
                outRect.Top = _space;
                outRect.Right = _space;
                outRect.Bottom = _space;
            }
        }
    }
}