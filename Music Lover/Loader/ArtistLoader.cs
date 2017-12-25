using System.Collections.Generic;
using Android.Content;
using Android.Database;
using Android.Provider;
using Music_Lover.Models;
using Music_Lover.Utils;
using static Android.Provider.MediaStore.Audio;

namespace Music_Lover.Loader
{
    public class ArtistLoader
    {
        private static readonly string[] PROJECTION =
        {
            BaseColumns.Id,
            ArtistColumns.Artist,
            ArtistColumns.NumberOfAlbums,
            ArtistColumns.NumberOfTracks
        };
        public static List<Artist> GetAllArtists(Context context)
        {
            return GetArtistsByCursor(CreateCursor(context, null, null));
        }

        public static Artist GetArtistById(Context context, long id)
        {
            return GetArtistByCursor(CreateCursor(context, "_id = ?", new[] {$"{id}"}));
        }

        public static List<Artist> GetArtistsByTitle(Context context, string queryString, int limit)
        {
            var result = GetArtistsByCursor(CreateCursor(context, "artist LIKE ?", new[] {$"{queryString}%"}));
            if (result.Count < limit)
            {
                result.AddRange(GetArtistsByCursor(CreateCursor(context, "artist LIKE ?", new []{$"%_{queryString}%"})));
            }

            return result.Count < limit ? result : result.GetRange(0, limit);
        }

        #region Private method

        private static Artist GetArtistByCursor(ICursor cursor)
        {
            var artist = new Artist();
            if (cursor != null && cursor.MoveToFirst())
            {
                artist = new Artist
                {
                    Id = cursor.GetLong(0),
                    Name = cursor.GetString(1),
                    AlbumCount = cursor.GetInt(2),
                    SongCount = cursor.GetInt(3)
                };
                cursor.Close();
            }

            return artist;
        }

        private static List<Artist> GetArtistsByCursor(ICursor cursor)
        {
            var result = new List<Artist>();
            if (cursor != null && cursor.MoveToFirst())
            {
                do
                {
                    result.Add(new Artist
                    {
                        Id = cursor.GetLong(0),
                        Name = cursor.GetString(1),
                        AlbumCount = cursor.GetInt(2),
                        SongCount = cursor.GetInt(3)
                    });
                } while (cursor.MoveToNext());
                cursor.Close();
            }
            return result;
        }

        private static ICursor CreateCursor(Context context, string selection, string[] param)
        {
            var sortOrder = PreferencesUtility.GetInstance(context).GetArtistSortOrder();
            var uri = Artists.ExternalContentUri;
            return context.ContentResolver.Query(uri, PROJECTION, selection, param, sortOrder);
        }

        #endregion
    }
}