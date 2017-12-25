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
    public class SongByArtistLoader
    {
        private static string[] PROJECTION =
        {
            BaseColumns.Id,
            AudioColumns.Title,
            AudioColumns.Artist,
            AudioColumns.Album,
            AudioColumns.Duration,
            AudioColumns.Track,
            AudioColumns.AlbumId
        };

        public static List<Song> GetAllSongsByArtist(Context context, long artistId)
        {
            var cursor = CreateCursor(context, artistId);
            var result = new List<Song>();
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
                        ArtistId = artistId,
                        AlbumId = cursor.GetLong(6)
                    });
                } while (cursor.MoveToNext());

                cursor.Close();
            }

            return result;
        }

        private static ICursor CreateCursor(Context context, long artistId)
        {
            var sortOrder = PreferencesUtility.GetInstance(context).GetArtistSongSortOrder();
            var uri = Media.ExternalContentUri;
            var selection = "is_music=1 AND title != '' AND artist_id=" + artistId;

            return context.ContentResolver.Query(uri, PROJECTION, selection, null, sortOrder);
        }
    }
}