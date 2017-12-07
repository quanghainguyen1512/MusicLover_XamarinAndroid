using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V4.Widget;
using Android.Support;
using Android.Support.V7.App;
using Android.Support.Design.Widget;
using Android.Views;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Music_Lover
{
    [Activity(Label = "Music_Lover", MainLauncher = true, Theme = "@style/AppTheme.NoActionBar")]
    public class MainActivity : AppCompatActivity, NavigationView.IOnNavigationItemSelectedListener
    {
        private DrawerLayout _drawerLayout;
        private NavigationView _navigationView;
        private Toolbar _toolbar;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            _drawerLayout = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);
            _toolbar = FindViewById<Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(_toolbar);
            SupportActionBar.SetTitle(Resource.String.app_name);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);
            SupportActionBar.SetDisplayShowHomeEnabled(true);

            var toggle = new ActionBarDrawerToggle(this, _drawerLayout, _toolbar, 
                Resource.String.navigation_drawer_open, Resource.String.navigation_drawer_close);
            _drawerLayout.AddDrawerListener(toggle);
            toggle.SyncState();

            _navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            _navigationView.SetNavigationItemSelectedListener(this);
        }

        public bool OnNavigationItemSelected(IMenuItem menuItem)
        {
            return true;
        }
    }
}

