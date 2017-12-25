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
using Music_Lover.Utils;
using static Android.Provider.MediaStore.Audio;
using Uri = Android.Net.Uri;

namespace Music_Lover.Loader
{
    public class AlbumByArtistLoader
    {
        private static string[] PROJECTION =
        {
            BaseColumns.Id,
            AlbumColumns.Album,
            AlbumColumns.Artist,
            AlbumColumns.NumberOfSongs,
            AlbumColumns.FirstYear
        };

        private static ICursor CreateCursor(Context context, long artistId)
        {
            var uri = Uri.Parse($"content://{MediaStore.Authority}/external/audio/artists/{artistId}/albums");
            var sortOrder = Albums.DefaultSortOrder;
            return context.ContentResolver.Query(uri, PROJECTION, null, null, sortOrder);
        }
    }
}