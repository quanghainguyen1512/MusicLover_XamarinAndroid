using System.Collections.Generic;
using Android.Content;
using Android.Database;
using Android.Provider;
using Music_Lover.Models;
using Music_Lover.Utils;
using static Android.Provider.MediaStore.Audio;

namespace Music_Lover.Loader
{
    public class AlbumLoader
    {
        private static readonly string[] PROJECTION =
        {
            BaseColumns.Id,
            AlbumColumns.Album,
            AlbumColumns.Artist,
            AudioColumns.ArtistId,
            AlbumColumns.NumberOfSongs,
            AlbumColumns.FirstYear
        };
        public static List<Album> GetAllAlbums(Context context)
        {
            return GetAlbumsListByCursor(CreateCursor(context, null, null));
        }

        public static Album GetAlbumById(Context context, long id)
        {
            return GetAlbum(CreateCursor(context, "_id = ?", new[] {$"{id}"}));
        }

        public static List<Album> GetAlbums(Context context, string param, int limit)
        {
            var result = GetAlbumsListByCursor(CreateCursor(context, "album LIKE ?", new[] {$"{param}%"}));
            if (result.Count < limit)
            {
                result.AddRange(GetAlbumsListByCursor(CreateCursor(context, "album LIKE ?", new []{$"%_{param}%"})));
            }
            return result.Count < limit ? result : result.GetRange(0, limit);
        }

        #region Private method

        private static Album GetAlbum(ICursor cursor)
        {
            var album = new Album();
            if (cursor != null)
            {
                if (cursor.MoveToFirst())
                {
                    album = new Album
                    {
                        Id = cursor.GetLong(0),
                        Title = cursor.GetString(1),
                        ArtistName = cursor.GetString(2),
                        ArtistId = cursor.GetLong(3),
                    };
                }
                cursor.Close();
            }

            return album;
        }

        private static List<Album> GetAlbumsListByCursor(ICursor cursor)
        {
            var result = new List<Album>();
            if (cursor != null && cursor.MoveToFirst())
            {
                do
                {
                    result.Add(new Album
                    {
                        Id = cursor.GetLong(0),
                        Title = cursor.GetString(1),
                        ArtistName = cursor.GetString(2),
                        ArtistId = cursor.GetLong(3),
                    });
                } while (cursor.MoveToNext());
            }

            return result;
        }

        private static ICursor CreateCursor(Context context, string selection, string[] param)
        {
            var albumSortOrder = PreferencesUtility.GetInstance(context).GetAlbumSortOrder();
            var cursor = context.ContentResolver.Query(Albums.ExternalContentUri,
                PROJECTION, selection, param, albumSortOrder);
            return cursor;
        }
        
        #endregion
    }
}