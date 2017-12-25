using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Music_Lover.Models;
using Music_Lover.Utils;
using static Android.Provider.MediaStore.Audio;

namespace Music_Lover.Loader
{
    public class LastAddedLoader
    {
        private const long SECONDS_IN_WEEK = 4 * 7 * 24 * 3600;
        private static string[] PROJECTION =
        {
            BaseColumns.Id,
            AudioColumns.Title,
            AudioColumns.Artist,
            AudioColumns.Album,
            AudioColumns.Duration,
            AudioColumns.Track,
            AudioColumns.ArtistId,
            AudioColumns.AlbumId
        };

        public static List<Song> GetLastAddedSongs(Context context)
        {
            var result = new List<Song>();
            var cursor = CreateCursor(context);
            if (cursor != null && cursor.MoveToFirst())
            {
                do
                {
                    result.Add(new Song
                    {
                        Id = cursor.GetLong(0),
                        Title = cursor.GetString(1),
                        ArtistName = cursor.GetString(2),
                        AlbumName = cursor.GetString(3),
                        Duration = cursor.GetInt(4),
                        TrackNumber = cursor.GetInt(5),
                        ArtistId = cursor.GetInt(6),
                        AlbumId = cursor.GetLong(7)
                    });
                } while (cursor.MoveToNext());
                cursor.Close();
            }

            return result;
        }

        private static ICursor CreateCursor(Context context)
        {
            var limitAddedTime = Java.Lang.JavaSystem.CurrentTimeMillis() / 1000 - SECONDS_IN_WEEK;
            var cutoff = PreferencesUtility.GetInstance(context).GetLastAddedCutoff();

            if (limitAddedTime > cutoff)
            {
                cutoff = limitAddedTime;
                PreferencesUtility.GetInstance(context).SetLastAddedCutoff(cutoff);
            }

            var selection = $"{AudioColumns.IsMusic} = 1 AND {AudioColumns.Title} != '' " +
                            $"AND {AudioColumns.DateAdded} > {cutoff}";
            return context.ContentResolver.Query(Media.ExternalContentUri, PROJECTION, selection, 
                null, $"{AudioColumns.DateAdded} DESC");
        }
    }
}