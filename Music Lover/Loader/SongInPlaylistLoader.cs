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
using Uri = Android.Net.Uri;

namespace Music_Lover.Loader
{
    public class SongInPlaylistLoader
    {
        private static readonly string[] PROJECTION =
        {
            Playlists.Members.Id,
            Playlists.Members.AudioId,
            AudioColumns.Title,
            AudioColumns.Artist,
            AudioColumns.AlbumId,
            AudioColumns.ArtistId,
            AudioColumns.Album,
            AudioColumns.Duration,
            AudioColumns.Track,
            Playlists.Members.PlayOrder
        };

        public static List<Song> GetSongsInPlaylist(Context context, long playlistId)
        {
            var result = new List<Song>();
            var numberOfSong = CountSongInPlaylist(context, playlistId);
            var cursor = CreateCursor(context, playlistId);

            if (cursor != null)
            {
                var cleanup = cursor.Count != numberOfSong;

                if (!cleanup && cursor.MoveToFirst())
                {
                    var playOrderCol = cursor.GetColumnIndexOrThrow(Playlists.Members.PlayOrder);
                    var lastPlay = -1;

                    do
                    {
                        var playOrder = cursor.GetInt(playOrderCol);
                        if (playOrder == lastPlay)
                        {
                            cleanup = true;
                            break;
                        }
                    } while (cursor.MoveToNext());
                }

                if (cleanup)
                {
                    CleanupPlaylist(context, playlistId, cursor);

                    cursor.Close();
                    cursor = CreateCursor(context, playlistId);
                }
            }

            if (cursor != null && cursor.MoveToFirst())
            {
                do
                {
                    result.Add(new Song
                    {
                        Id = cursor.GetLong(cursor.GetColumnIndexOrThrow(Playlists.Members.AudioId)),
                        Title = cursor.GetString(cursor.GetColumnIndexOrThrow(AudioColumns.Title)),
                        ArtistName = cursor.GetString(cursor.GetColumnIndexOrThrow(AudioColumns.Artist)),
                        AlbumId = cursor.GetLong(cursor.GetColumnIndexOrThrow(AudioColumns.AlbumId)),
                        ArtistId = cursor.GetLong(cursor.GetColumnIndexOrThrow(AudioColumns.ArtistId)),
                        AlbumName = cursor.GetString(cursor.GetColumnIndexOrThrow(AudioColumns.Album)),
                        Duration = cursor.GetInt(cursor.GetColumnIndexOrThrow(AudioColumns.Duration)),
                        TrackNumber = cursor.GetInt(cursor.GetColumnIndexOrThrow(AudioColumns.Track))
                    });
                } while (cursor.MoveToNext());
                cursor.Close();
            }

            return result;
        }

        private static void CleanupPlaylist(Context context, long playlistId, ICursor cursor)
        {
            var idCol = cursor.GetColumnIndexOrThrow(Playlists.Members.AudioId);
            var uri = Uri.Parse($"content://media/external/audio/playlists/{playlistId}/members");

            var ops = new List<ContentProviderOperation> {ContentProviderOperation.NewDelete(uri).Build()};
            
            var f = 100;

            if (cursor.MoveToFirst() && cursor.Count > 0)
            {
                do
                {
                    var builder = ContentProviderOperation.NewInsert(uri)
                        .WithValue(Playlists.Members.PlayOrder, cursor.Position)
                        .WithValue(Playlists.Members.AudioId, cursor.GetLong(idCol));

                    if ((cursor.Position + 1) % f == 0)
                    {
                        builder.WithYieldAllowed(true);
                    }

                    ops.Add(builder.Build());
                } while (cursor.MoveToNext());
            }

            context.ContentResolver.ApplyBatch(MediaStore.Authority, ops);
        }

        private static int CountSongInPlaylist(Context context, long playlistId)
        {
            ICursor c = null;
            try
            {
                c = context.ContentResolver.Query(
                    Uri.Parse($"content://media/external/audio/playlists/{playlistId}/members"),
                    new[]{ Playlists.Members.AudioId }, 
                    null, null,
                    Playlists.Members.DefaultSortOrder);

                if (c != null)
                {
                    return c.Count;
                }
            }
            finally
            {
                if (c != null)
                {
                    c.Close();
                    c = null;
                }
            }

            return 0;
        }

        private static ICursor CreateCursor(Context context, long playlistId)
        {
            var sortOrder = Playlists.Members.DefaultSortOrder;
            var selection = $"{AudioColumns.IsMusic} = 1 AND {AudioColumns.Title} != ''";
            var uri = Uri.Parse($"content://media/external/audio/playlists/{playlistId}/members");
            return context.ContentResolver.Query(uri, PROJECTION, selection, null, sortOrder);
        }
    }
}