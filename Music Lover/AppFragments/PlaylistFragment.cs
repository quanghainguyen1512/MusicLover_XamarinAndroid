using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Music_Lover.Widgets;
using Fragment = Android.App.Fragment;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Music_Lover.AppFragments
{
    public class PlaylistFragment : Android.Support.V4.App.Fragment
    {
        private int _numberOfPlaylist;
        private FragmentStatePagerAdapter _adapter;
        private MultiViewPager _pager;

        public override void OnActivityCreated(Bundle savedInstanceState)
        {
            base.OnActivityCreated(savedInstanceState);
            HasOptionsMenu = true;
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            base.OnCreateOptionsMenu(menu, inflater);
            inflater.Inflate(Resource.Menu.menu_playlist, menu);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var rootView = inflater.Inflate(Resource.Layout.fragment_playlist, container, false);
            var toolbar = rootView.FindViewById<Toolbar>(Resource.Id.toolbar);
            ((AppCompatActivity) Activity).SetSupportActionBar(toolbar);
            var ab = ((AppCompatActivity) Activity).SupportActionBar;
            ab.SetHomeAsUpIndicator(Resource.Drawable.ic_menu);
            ab.SetDisplayHomeAsUpEnabled(true);
            ab.Title = "Playlist";
//            final List<Playlist> playlists = PlaylistLoader.getPlaylists(getActivity(), true);
//            playlistcount = playlists.size();
//
//            pager = (MultiViewPager)rootView.findViewById(R.id.playlistpager);
//
//            adapter = new FragmentStatePagerAdapter(getChildFragmentManager()) {
//
//                @Override
//                public int getCount()
//                {
//                return playlistcount;
//            }
//
//            @Override
//            public Fragment getItem(int position)
//            {
//                return PlaylistPagerFragment.newInstance(position);
//            }
//
//            };
//            pager.setAdapter(adapter);
//            pager.setOffscreenPageLimit(3);

            return rootView;
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            return base.OnOptionsItemSelected(item);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
        }
    }
}