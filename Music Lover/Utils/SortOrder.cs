using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Music_Lover.Utils
{
    public class SortOrder
    {
        public static class Artist
        {
            public const string ARTIST_A_Z = MediaStore.Audio.Artists.DefaultSortOrder;
            public const string ARTIST_Z_A = ARTIST_A_Z + " DESC";
            public const string ARTIST_NUMBER_OF_SONGS = MediaStore.Audio.ArtistColumns.NumberOfTracks + " DESC";
            public const string ARTIST_NUMBER_OF_ALBUMS = MediaStore.Audio.ArtistColumns.NumberOfAlbums + " DESC";
        }

        public static class Album
        {
            public const string ALBUM_A_Z = MediaStore.Audio.Albums.DefaultSortOrder;
            public const string ALBUM_Z_A = ALBUM_A_Z + " DESC";
            public const string ALBUM_NUMBER_OF_SONGS = MediaStore.Audio.AlbumColumns.NumberOfSongs;
            public const string ALBUM_ARTIST = MediaStore.Audio.AlbumColumns.Artist;
            public const string ALBUM_YEAR = MediaStore.Audio.AlbumColumns.FirstYear + " DESC";
        }

        public static class Song
        {
            public const string SONG_A_Z = MediaStore.Audio.Media.DefaultSortOrder;
            public const string SONG_Z_A = SONG_A_Z + " DESC";
            public const string SONG_ARTIST = MediaStore.Audio.AudioColumns.Artist;
            public const string SONG_ALBUM = MediaStore.Audio.AudioColumns.Album;
            public const string SONG_YEAR = MediaStore.Audio.AudioColumns.Year + " DESC";
            public const string SONG_DURATION = MediaStore.Audio.AudioColumns.Duration + " DESC";
            public const string SONG_FILENAME = MediaStore.Audio.AudioColumns.Data;
        }

        public static class AlbumSong
        {
            public const string SONG_A_Z = MediaStore.Audio.Media.DefaultSortOrder;
            public const string SONG_Z_A = SONG_A_Z + " DESC";
            public const string SONG_TRACK_LIST =
                MediaStore.Audio.AudioColumns.Track + ", " + MediaStore.Audio.Media.DefaultSortOrder;
            public const string SONG_DURATION = Song.SONG_DURATION;
            public const string SONG_YEAR = MediaStore.Audio.AudioColumns.Year;
        }

        public static class ArtistSong
        {
            public const string SONG_A_Z = MediaStore.Audio.Media.DefaultSortOrder;
        }
    }
}