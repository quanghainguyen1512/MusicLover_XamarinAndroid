using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Java.Lang;
using Music_Lover.Utils;
using Fragment = Android.Support.V4.App.Fragment;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Music_Lover.AppFragments
{
    public class MainFragment : Fragment
    {
        private ViewPager _viewPager;
        private PreferencesUtility _preferences;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _preferences = PreferencesUtility.GetInstance(Activity);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var rootView = inflater.Inflate(Resource.Layout.fragment_main, container, false);

            var toolbar = rootView.FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            ((AppCompatActivity) Activity).SetSupportActionBar(toolbar);

            var actionBar = ((AppCompatActivity) Activity).SupportActionBar;
            actionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_menu);
            actionBar.SetDisplayHomeAsUpEnabled(true);

            _viewPager = rootView.FindViewById<ViewPager>(Resource.Id.viewpager);
            if (_viewPager != null)
            {
                SetUpViewPager(_viewPager);
                _viewPager.OffscreenPageLimit = 2;
            }

            return rootView;
        }

        private void SetUpViewPager(ViewPager viewPager)
        {
            var adapter = new Adapter(ChildFragmentManager);
            adapter.AddFragment("Songs", new SongFragment());
            adapter.AddFragment("Artist", new ArtistFragment());
            adapter.AddFragment("Albums", new AlbumFragment());
            viewPager.Adapter = adapter;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            _viewPager.CurrentItem = _preferences.GetStartPageIndex();
        }

        public override async void OnPause()
        {
            base.OnPause();
            if (_preferences.IsLastOpenedPageAsStart())
                await _preferences.SetStartPageIndex(_viewPager.CurrentItem);
        }

        public override void OnResume()
        {
            base.OnResume();
        }

        public override void OnStart()
        {
            base.OnStart();
        }

        private class Adapter : FragmentPagerAdapter
        {
            private List<Fragment> _fragments = new List<Fragment>();
            private List<string> _titles = new List<string>();

            public Adapter(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }

            public Adapter(FragmentManager fm) : base(fm)
            {
            }

            public void AddFragment(string title, Fragment fragment)
            {
                _fragments.Add(fragment);
                _titles.Add(title);
            }

            public override int Count => _fragments.Count;
            public override Fragment GetItem(int position) => _fragments[position];

            public override ICharSequence GetPageTitleFormatted(int position) =>
                new Java.Lang.String(_titles[position]);
        }
    }
}