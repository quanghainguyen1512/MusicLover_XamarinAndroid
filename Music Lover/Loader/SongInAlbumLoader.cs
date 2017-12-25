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
    public class SongInAlbumLoader
    {
        private static string[] PROJECTION =
        {
            BaseColumns.Id,
            AudioColumns.Title,
            AudioColumns.Artist,
            AudioColumns.Album,
            AudioColumns.Duration,
            AudioColumns.Track,
            AudioColumns.ArtistId
        };

        public static List<Song> GetAllSongsInAlbum(Context context, long albumId)
        {
            var cursor = CreateCursor(context, albumId);
            var result = new List<Song>();
            if (cursor != null && cursor.MoveToFirst())
            {
                do
                {
                    var trackNum = cursor.GetInt(5);
                    if (trackNum >= 1000)
                        trackNum = trackNum % 1000;
                    result.Add(new Song
                    {
                        Id = cursor.GetLong(0),
                        Title = cursor.GetString(1),
                        ArtistName = cursor.GetString(2),
                        AlbumName = cursor.GetString(3),
                        Duration = cursor.GetInt(4),
                        TrackNumber = trackNum,
                        ArtistId = cursor.GetLong(6),
                        AlbumId = albumId
                    });
                } while (cursor.MoveToNext());

                cursor.Close();
            }

            return result;
        }

        private static ICursor CreateCursor(Context context, long albumId)
        {
            var sortOrder = PreferencesUtility.GetInstance(context).GetAlbumSongSortOrder();
            var uri = MediaStore.Audio.Media.ExternalContentUri;
            var selection = "is_music = 1 AND title != '' AND album_id = " + albumId;

            return context.ContentResolver.Query(uri, PROJECTION, selection, null, sortOrder);
        }
    }
}