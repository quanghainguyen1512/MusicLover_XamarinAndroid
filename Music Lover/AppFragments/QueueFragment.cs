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
using Android.Util;
using Android.Views;
using Android.Widget;
using Music_Lover.Activities;
using Music_Lover.Adapters;
using Music_Lover.Listeners;
using Music_Lover.Loader;
using Music_Lover.Widgets;
using Fragment = Android.Support.V4.App.Fragment;

namespace Music_Lover.AppFragments
{
    public class QueueFragment : Fragment, IMusicStateListener
    {
        private PlayingQueueAdapter _adapter;
        private RecyclerView _recyclerView;


        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var rootView = inflater.Inflate(Resource.Layout.fragment_queue, container, false);

            var toolbar = rootView.FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            ((AppCompatActivity) Activity).SetSupportActionBar(toolbar);

            var ab = ((AppCompatActivity) Activity).SupportActionBar;
            ab.SetHomeAsUpIndicator(Resource.Drawable.ic_menu);
            ab.SetDisplayHomeAsUpEnabled(true);
            ab.Title = Resources.GetString(Resource.String.playing_queue);

            _recyclerView = rootView.FindViewById<RecyclerView>(Resource.Id.recyclerview);
            _recyclerView.SetLayoutManager(new LinearLayoutManager(Activity));
            _recyclerView.SetItemAnimator(null);

            Task.Run(async () =>
            {
                await LoadQueueSongs();
            });

            ((BaseActivity) Activity).SetMusicStateListener(this);

            return rootView;
        }

        private async Task LoadQueueSongs()
        {
            await Task.Run(() => 
            {
                _adapter = new PlayingQueueAdapter(Activity, QueueLoader.GetQueue(Activity));
            });
            _recyclerView.SetAdapter(_adapter);
            // var drag = new DragRecycler();

            /// TODO: create dragable reorder

            _recyclerView.GetLayoutManager().ScrollToPosition(_adapter.CurrentPosition);
        }

        public void OnMetaChanged()
        {
            if (_adapter != null)
                _adapter.NotifyDataSetChanged();
        }

        public void OnPlaylistChanged()
        {
        }

        public void RestartLoader()
        {
        }
    }
}