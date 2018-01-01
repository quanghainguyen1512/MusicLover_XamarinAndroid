using System.Collections.Generic;
using Android.Content;
using Android.Database;
using Android.Provider;
using Music_Lover.Models;
using Music_Lover.Utils;
using static Android.Provider.MediaStore.Audio;

namespace Music_Lover.Loader
{
    public class SongLoader
    {
        private static readonly string[] PROJECTION =
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
        
        public static List<Song> GetSongsByCursor(ICursor cursor)
        {
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
                        ArtistId = cursor.GetLong(6),
                        AlbumId = cursor.GetLong(7)
                    });
                } while (cursor.MoveToNext());
                cursor.Close();
            }

            return result;
        }

        public static long[] GetSongListByCursor(ICursor cursor)
        {
            if (cursor is null)
                return new long[0];
            var length = cursor.Count;
            var list = new long[length];

            cursor.MoveToFirst();
            var colIndex = -1;
            try
            {
                colIndex = cursor.GetColumnIndexOrThrow(Playlists.Members.AudioId);
            }
            catch
            {
                colIndex = cursor.GetColumnIndexOrThrow(BaseColumns.Id);
            }

            for (var i = 0; i < length; i++)
            {
                list[i] = cursor.GetLong(colIndex);
                cursor.MoveToNext();
            }
            cursor.Close();
            return list;
        }

        public static Song GetSongFromPath(string path, Context context)
        {
            var cr = context.ContentResolver;
            var uri = Media.ExternalContentUri;
            var selection = AudioColumns.Data;
            string[] args = {path};

            var sortOrder = AudioColumns.Title + " ASC";

            var cursor = cr.Query(uri, PROJECTION, $"{selection} = ?", args, sortOrder);

            if (cursor == null || cursor.Count <= 0) return new Song();

            var song = GetSongByCursor(cursor);
            cursor.Close();
            return song;
        }

        public static List<Song> GetAllSongs(Context context)
        {
            return GetSongsByCursor(CreateSongCursor(context, null, null));
        }

        public static Song GetSongById(Context context, long id)
        {
            return GetSongByCursor(CreateSongCursor(context, $"_id = {id}", null));
        }

        public static List<Song> SearchSongByTitle(Context context, string queryString, int limit)
        {
            var result = GetSongsByCursor(CreateSongCursor(context, "title LIKE ?", new[] {$"{queryString}%"}));
            if (result.Count < limit)
            {
                result.AddRange(GetSongsByCursor(CreateSongCursor(context, "title LIKE ?", new []{$"%_{queryString}%"})));
            }

            return result.Count < limit ? result : result.GetRange(0, limit);
        }

        #region Private method

        private static Song GetSongByCursor(ICursor cursor)
        {
            var song = new Song();
            if (cursor != null && cursor.MoveToFirst())
            {
                song = new Song
                {
                    Id = cursor.GetLong(0),
                    Title = cursor.GetString(1),
                    ArtistName = cursor.GetString(2),
                    AlbumName = cursor.GetString(3),
                    Duration = cursor.GetInt(4),
                    TrackNumber = cursor.GetInt(5),
                    ArtistId = cursor.GetInt(6),
                    AlbumId = cursor.GetInt(7)
                };
                cursor.Close();
            }

            return song;
        }

        public static ICursor CreateSongCursor(Context context, string selection, string[] param, string sortOrder)
        {
            var statement = "is_music = 1 AND title != ''";
            var uri = Media.ExternalContentUri;
            if (!string.IsNullOrEmpty(selection))
            {
                statement = $"{statement} AND {selection}";
            }

            return context.ContentResolver.Query(uri, PROJECTION, statement, param, sortOrder);
        }

        public static ICursor CreateSongCursor(Context context, string selection, string[] param)
        {
            var songSortOrder = PreferencesUtility.GetInstance(context).GetSongSortOrder();
            return CreateSongCursor(context, selection, param, songSortOrder);
        }

        #endregion
    }
}