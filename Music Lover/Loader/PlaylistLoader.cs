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
using static Android.Provider.MediaStore.Audio;

namespace Music_Lover.Loader
{
    public class PlaylistLoader
    {
        private static string[] PROJECTION =
        {
            BaseColumns.Id,
            PlaylistsColumns.Name
        };

        private static List<Playlist> _playlists = new List<Playlist>();
        private static ICursor _cursor;

        public static List<Playlist> GetPlaylists(Context context, bool isDefaulIncluded)
        {
            if (isDefaulIncluded)
                CreateDefaultPlaylists(context);

            _cursor = CreateCursor(context);
            if (_cursor != null && _cursor.MoveToFirst())
            {
                do
                {
                    _playlists.Add(new Playlist
                    {
                        Id = _cursor.GetLong(0),
                        Title = _cursor.GetString(1),
                        SongCount = _cursor.GetInt(2)
                    });
                } while (_cursor.MoveToNext());
                _cursor.Close();
            }

            return _playlists;
        }

        public static void CreateDefaultPlaylists(Context context)
        {
            var lastAdded = new Playlist
            {
                Id = -1,
                Title = "Last Added",
                SongCount = -1
            };

            var recentlyPlayed = new Playlist
            {
                Id = -2,
                Title = "Recently played"
            };

            _playlists.Add(lastAdded);
            _playlists.Add(recentlyPlayed);
        }

        public static void DeletePlaylist(Context context, long playlistId)
        {
            var uri = Playlists.ExternalContentUri;
            var where = $"_id IN ({playlistId})";
            context.ContentResolver.Delete(uri, where, null);
        }

        private static ICursor CreateCursor(Context context)
        {
            var uri = Playlists.ExternalContentUri;
            var sortOrder = Playlists.DefaultSortOrder;

            return context.ContentResolver.Query(uri, PROJECTION, null, null, sortOrder);
        }
    }
}