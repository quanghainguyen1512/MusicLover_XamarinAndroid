using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Music_Lover.Utils;
using Fragment = Android.Support.V4.App.Fragment;

namespace Music_Lover.AppFragments
{
    public class SongFragment : Fragment
    {
        private RecyclerView _recyclerView;
        private PreferencesUtility _preferences;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _preferences = PreferencesUtility.GetInstance(Activity);
        }


    }
}