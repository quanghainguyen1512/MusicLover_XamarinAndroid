using System.Collections.Generic;
using Android.Content;
using Android.Database;
using Android.Database.Sqlite;
using Music_Lover.Helpers;

namespace Music_Lover.Providers
{
    public class MusicPlaybackState
    {
        private static MusicPlaybackState _instance;
        private readonly MusicDatabase _musicDatabase;
        private static readonly object Padlock = new object();

        public MusicPlaybackState(Context context)
        {
            _musicDatabase = MusicDatabase.GetInstance(context);
        }

        public static MusicPlaybackState GetInstance(Context context)
        {
            lock (Padlock)
            {
                if (_instance is null)
                    _instance = new MusicPlaybackState(context.ApplicationContext);
            }
            return _instance;
        }

        public void Create(SQLiteDatabase db)
        {
            var sql = $"CREATE TABLE IF NOT EXISTS {PlaybackQueueColumns.NAME} " +
                      $"({PlaybackQueueColumns.TRACK_ID} LONG NOT NULL," +
                      $"{PlaybackQueueColumns.SOURCE_ID} LONG NOT NULL," +
                      $"{PlaybackQueueColumns.SOURCE_TYPE} INT NOT NULL," +
                      $"{PlaybackQueueColumns.SOURCE_POSITION} INT NOT NULL );";
            db.ExecSQL(sql);

            sql = $"CREATE TABLE IF NOT EXISTS {PlaybackHistoryColumns.NAME} " +
                  $"({PlaybackHistoryColumns.POSITION} INT NOT NULL);";
            db.ExecSQL(sql);
        }

        public void Upgrade(SQLiteDatabase db, int oldVersion, int newVersion)
        {
            if (oldVersion < 2 && newVersion >= 2)
                Create(db);
        }

        public void Downgrade(SQLiteDatabase db, int oldVersion, int newVersion)
        {
            db.ExecSQL($"DROP TABLE IF EXISTS {PlaybackQueueColumns.NAME}");
            db.ExecSQL($"DROP TABLE IF EXISTS {PlaybackHistoryColumns.NAME}");
            Create(db);
        }

        public void SaveState(List<MusicPlaybackTrack> queue, List<int> history)
        {
            var db = _musicDatabase.WritableDatabase;
            db.BeginTransaction();

            try
            {
                db.Delete(PlaybackQueueColumns.NAME, null, null);
                db.Delete(PlaybackHistoryColumns.NAME, null, null);
                db.SetTransactionSuccessful();
            }
            finally
            {
                db.EndTransaction();
            }

            var numProcess = 20;
            var pos = 0;
            while (pos < queue.Count)
            {
                db.BeginTransaction();
                try
                {
                    for (var i = pos; i < queue.Count && i < pos + numProcess; i++)
                    {
                        var track = queue[i];
                        var val = new ContentValues(4);
                        val.Put(PlaybackQueueColumns.TRACK_ID, track.Id);
                        val.Put(PlaybackQueueColumns.SOURCE_ID, track.SourceId);
                        val.Put(PlaybackQueueColumns.SOURCE_TYPE, (int)track.SourceType);
                        val.Put(PlaybackQueueColumns.SOURCE_POSITION, track.SourcePos);

                        db.Insert(PlaybackQueueColumns.NAME, null, val);
                    }
                    db.SetTransactionSuccessful();
                }
                finally
                {
                    db.EndTransaction();
                    pos += numProcess;
                }
            }

            if (history != null)
            {
                db.BeginTransaction();
                
                try
                {
                    for (var i = 0; i < history.Count && i < numProcess; i++)
                    {
                        var val = new ContentValues(1);
                        val.Put(PlaybackHistoryColumns.POSITION, i);

                        db.Insert(PlaybackHistoryColumns.NAME, null, val);
                    }

                    db.SetTransactionSuccessful();
                }
                finally
                {
                    db.EndTransaction();
                }
            }
        }

        public List<MusicPlaybackTrack> GetQueue()
        {
            var result = new List<MusicPlaybackTrack>();
            ICursor cursor = null;

            try
            {
                cursor = _musicDatabase.ReadableDatabase.Query(PlaybackQueueColumns.NAME,
                    null, null, null, null, null, null);

                if (cursor != null && cursor.MoveToFirst())
                {
                    do
                    {
                        result.Add(new MusicPlaybackTrack(cursor.GetLong(0), cursor.GetLong(1),
                            (Utils.Utils.SourceTypeId) cursor.GetInt(2), cursor.GetInt(3)));
                    } while (cursor.MoveToNext());
                }

                return result;
            }
            finally
            {
                cursor?.Close();
            }
        }

        public List<int> GetHistory(int playlistSize)
        {
            var result = new List<int>();
            ICursor cursor = null;

            try
            {
                cursor = _musicDatabase.ReadableDatabase.Query(PlaybackHistoryColumns.NAME, 
                    null, null, null, null, null, null);
                if (cursor != null && cursor.MoveToFirst())
                {
                    do
                    {
                        var pos = cursor.GetInt(0);
                        if (pos >= 0 && pos < playlistSize)
                        {
                            result.Add(pos);
                        }
                    } while (cursor.MoveToNext());
                }

                return result;
            }
            finally
            {
                cursor?.Close();
            }
        }

        public class PlaybackQueueColumns
        {
            public static string NAME = "playbackqueue";
            public static string TRACK_ID = "trackid";
            public static string SOURCE_ID = "sourceid";
            public static string SOURCE_TYPE = "sourcetype";
            public static string SOURCE_POSITION = "sourceposition";
        }

        public class PlaybackHistoryColumns
        {
            public static string NAME = "playbackhistory";
            public static string POSITION = "position";
        }
    }
}