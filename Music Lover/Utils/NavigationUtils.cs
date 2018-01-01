using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Media.Audiofx;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Music_Lover.Activities;
using Fragment = Android.Support.V4.App.Fragment;
using TransitionInflater = Android.Transitions.TransitionInflater;

namespace Music_Lover.Utils
{
    public class NavigationUtils
    {
        public static void NavigateToAlbum(Activity context, long albumId, KeyValuePair<string, View> transitionViews)
        {
            var transaction = ((AppCompatActivity) context).SupportFragmentManager.BeginTransaction();
            Fragment fragment = null;

            if (PreferencesUtility.GetInstance(context).GetAnimations())
            {
                var changeImage = TransitionInflater.From(context)
                    .InflateTransition(Resource.Transition.image_transform);
                transaction.AddSharedElement(transitionViews.Value, transitionViews.Key);
                // fragment =
                fragment.SharedElementEnterTransition = changeImage;
            }
            else
            {
                transaction.SetCustomAnimations(Resource.Animation.activity_fade_in,
                    Resource.Animation.activity_fade_out, Resource.Animation.activity_fade_in,
                    Resource.Animation.activity_fade_out);
                // fragment = 
            }

            transaction.Hide(((AppCompatActivity) context).SupportFragmentManager.FindFragmentById(Resource.Id.fragment_container));
            transaction.Add(Resource.Id.fragment_container, fragment);
            transaction.AddToBackStack(null).Commit();

        }

        public static void NavigateToArtist(Activity context, long artistId, Tuple<string, View> transitionViews)
        {
            var transaction = ((AppCompatActivity) context).SupportFragmentManager.BeginTransaction();
            Fragment fragment = null;

            if (transitionViews != null && PreferencesUtility.GetInstance(context).GetAnimations())
            {
                var changeImage = TransitionInflater.From(context)
                    .InflateTransition(Resource.Transition.image_transform);
                transaction.AddSharedElement(transitionViews.Item2, transitionViews.Item1);
                // fragment =
                fragment.SharedElementEnterTransition = changeImage;
            }
            else
            {
                transaction.SetCustomAnimations(Resource.Animation.activity_fade_in,
                    Resource.Animation.activity_fade_out, Resource.Animation.activity_fade_in,
                    Resource.Animation.activity_fade_out);
                // fragment = 
            }

            transaction.Hide(
                ((AppCompatActivity) context).SupportFragmentManager.FindFragmentById(Resource.Id.fragment_container));
            transaction.Add(Resource.Id.fragment_container, fragment);
            transaction.AddToBackStack(null).Commit();
        }

        public static void GoToArtist(Context context, long artistId)
        {
            var intent = new Intent(context, typeof(MainActivity));
            intent.SetAction(AppConstants.AppConstants.NAVIGATE_ARTIST);
            intent.PutExtra(AppConstants.AppConstants.ARTIST_ID, artistId);
            context.StartActivity(intent);
        }

        public static void GoToAlbum(Context context, long albumId)
        {
            var intent = new Intent(context, typeof(MainActivity));
            intent.SetAction(AppConstants.AppConstants.NAVIGATE_ALBUM);
            intent.PutExtra(AppConstants.AppConstants.ALBUM_ID, albumId);
            context.StartActivity(intent);
        }

        public static void NavigateToNowPlaying(Activity context)
        {
            var intent = new Intent(context, typeof(NowPlayingActivity));
            if (!PreferencesUtility.GetInstance(context).GetSystemAnimations())
                intent.SetFlags(ActivityFlags.NoAnimation);
            context.StartActivity(intent);
        }

        public static Intent GetNowPlayingIntent(Context context)
        {
            var intent = new Intent(context, typeof(MainActivity));
            intent.SetAction(AppConstants.AppConstants.NAVIGATE_NOWPLAYING);
            return intent;
        }

        public static void NavigateToSearch(Activity context)
        {
            var intent = new Intent(context, typeof(SearchActivity));
            intent.SetFlags(ActivityFlags.NoAnimation);
            intent.SetAction(AppConstants.AppConstants.NAVIGATE_SEARCH);
            context.StartActivity(intent);
        }

        public static void NavigateToSettings(Activity context)
        {
            var intent = new Intent(context, typeof(SettingsActivity));
            intent.SetFlags(ActivityFlags.NoAnimation);
            intent.SetAction(AppConstants.AppConstants.NAVIGATE_SETTINGS);
            context.StartActivity(intent);
        }

        public static void NavigateToEqualizer(Activity context)
        {
            try
            {
                var intent = new Intent(AudioEffect.ActionDisplayAudioEffectControlPanel);
//                intent.PutExtra(AudioEffect.ExtraAudioSession, MusicPlayer.)
                context.StartActivityForResult(intent, 123);
            }
            catch (ActivityNotFoundException)
            {
                Toast.MakeText(context, "Equalizer not available", ToastLength.Long).Show();
            }
        }

//        public static Fragment GetFragmentByNowPlayingId(string fragmentId)
//        {
//            switch (fragmentId)
//            {
//                case AppConstants.AppConstants.NOWPLAYING1:
//                    return
//            }
//        }

        public static int GetIdOfCurrentNowPlaying(string nowPlaying)
        {
            switch (nowPlaying)
            {
                case AppConstants.AppConstants.NOWPLAYING1:
                    return 0;
                case AppConstants.AppConstants.NOWPLAYING2:
                    return 1;
                default:
                    return 0;
            }
        }
    }
}