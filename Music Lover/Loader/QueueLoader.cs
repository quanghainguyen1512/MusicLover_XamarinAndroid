using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Content;
using Android.Database;
using Android.Provider;
using Music_Lover.Models;
using static Android.Provider.MediaStore.Audio;

namespace Music_Lover.Loader
{
    public class QueueLoader
    {
        private static NowPlayingCursor _cursor;

        public static List<Song> GetQueue(Context context)
        {
            var songs = new List<Song>();
            _cursor = new NowPlayingCursor(context);
            if (_cursor != null && _cursor.MoveToFirst())
            {
                do
                {
                    songs.Add(new Song
                    {
                        Id = _cursor.GetLong(0),
                        Title = _cursor.GetString(1),
                        ArtistName = _cursor.GetString(2),
                        AlbumId = _cursor.GetLong(3),
                        AlbumName = _cursor.GetString(4),
                        Duration = _cursor.GetInt(5),
                        TrackNumber = _cursor.GetInt(6),
                        ArtistId = _cursor.GetInt(7),
                    });
                } while (_cursor.MoveToNext());

                _cursor.Close();
            }

            return songs;
        }

        private class NowPlayingCursor : AbstractCursor
        {
            private static string[] PROJECTION = new[]
            {
                BaseColumns.Id,
                AudioColumns.Title,
                AudioColumns.Artist,
                AudioColumns.AlbumId,
                AudioColumns.Album,
                AudioColumns.Duration,
                AudioColumns.Track,
                AudioColumns.ArtistId,
                AudioColumns.Artist
            };

            private Context _context;
            private long[] _nowPlaying;
            private long[] _cursorIndexes;
            private int _size;
            private int _curPos;
            private ICursor _queueCursor;

            public NowPlayingCursor(Context context)
            {
                _context = context;
                CreateNowPlayingCursor();
            }

            public override int Count => _size;

            public override bool OnMove(int oldPosition, int newPosition)
            {
                if (oldPosition == newPosition)
                    return true;
                if (_nowPlaying == null || _cursorIndexes == null || newPosition >= _nowPlaying.Length)
                    return false;

                var id = _nowPlaying[newPosition];
                var cursorIndex = _cursorIndexes.FirstOrDefault(e => e == id);
                _queueCursor.MoveToPosition((int)cursorIndex);
                _curPos = newPosition;
                return true;
            }

            public override string[] GetColumnNames()
            {
                return PROJECTION;
            }

            public override double GetDouble(int column)
            {
                return _queueCursor.GetDouble(column);
            }

            public override float GetFloat(int column)
            {
                return _queueCursor.GetFloat(column);
            }

            public override int GetInt(int column)
            {
                try
                {
                    return _queueCursor.GetInt(column);
                }
                catch
                {
                    OnChange(true);
                    return 0;
                }
            }

            public override long GetLong(int column)
            {
                try
                {
                    return _queueCursor.GetLong(column);
                }
                catch
                {
                    OnChange(true);
                    return 0;
                }
            }

            public override short GetShort(int column)
            {
                return _queueCursor.GetShort(column);
            }

            public override string GetString(int column)
            {
                try
                {
                    return _queueCursor.GetString(column);
                }
                catch
                {
                    OnChange(true);
                    return "";
                }
            }

            public override bool IsNull(int column)
            {
                return _queueCursor.IsNull(column);
            }

            private void CreateNowPlayingCursor()
            {
                _queueCursor = null;
                //                _nowPlaying = MusicPlayer.Queue
                _size = _nowPlaying.Length;
                if (_size == 0) return;

                var selection = new StringBuilder();
                selection.Append(MediaStore.Audio.AudioColumns.Id + " IN (");
                for (var i = 0; i < _size; i++)
                {
                    selection.Append(_nowPlaying);
                    if (i < _size - 1)
                    {
                        selection.Append(",");
                    }
                }

                selection.Append(")");

                _queueCursor = _context.ContentResolver.Query(
                    Media.ExternalContentUri, PROJECTION, selection.ToString(), null, AudioColumns.Id);

                if (_queueCursor is null)
                {
                    _size = 0;
                    return;
                }

                var playlistSize = _queueCursor.Count;
                _cursorIndexes = new long[playlistSize];
                _queueCursor.MoveToFirst();
                var colIndex = _queueCursor.GetColumnIndexOrThrow(AudioColumns.Id);
                for (var i = 0; i < playlistSize; i++)
                {
                    _cursorIndexes[i] = _queueCursor.GetLong(colIndex);
                    _queueCursor.MoveToNext();
                }

                _queueCursor.MoveToFirst();
                _curPos = -1;

                var removed = 0;
                for (var i = _nowPlaying.Length - 1; i >= 0; i--)
                {
                    var trackId = _nowPlaying[i];
                    var cursorIndex = _cursorIndexes.FirstOrDefault(e => e == trackId);
                    if (cursorIndex < 0)
                    {
                        //                        removed += MusicPlayer.RemoveTrack(trackId);
                    }
                }

                if (removed > 0)
                {
                    //                    _nowPlaying = MusicPlayer.Queue;
                    _size = _nowPlaying.Length;
                    if (_size == 0)
                    {
                        _cursorIndexes = null;
                    }
                }
            }

        }
    }
}