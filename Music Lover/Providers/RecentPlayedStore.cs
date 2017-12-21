using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Database;
using Android.Database.Sqlite;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Music_Lover.Providers
{
    public class RecentPlayedStore
    {
        private static readonly object Padlock = new object();
        private int MAX_RECENT_ITEM = 20;
        private MusicDatabase _musicDatabase = null;
        private static RecentPlayedStore _instance = null;

        public RecentPlayedStore(Context context) 
        {
            _musicDatabase = MusicDatabase.GetInstance(context);
        }

        public static RecentPlayedStore GetInstance(Context context)
        {
            lock (Padlock)
            {
                if (_instance is null)
                    _instance = new RecentPlayedStore(context.ApplicationContext);
            }

            return _instance;
        }

        public void Create(SQLiteDatabase db)
        {
            var sql = $"CREATE TABLE IF NOT EXISTS {RecentStoreColumns.NAME} " +
                      $"({RecentStoreColumns.ID} INT NOT NULL, {RecentStoreColumns.TIMEPLAYED} LONG NOT NULL);";
            db.ExecSQL(sql);
        }

        public void Downgrade(SQLiteDatabase db, int oldVersion, int newVersion)
        {
            db.ExecSQL($"DROP TABLE IF EXISTS {RecentStoreColumns.NAME}");
            Create(db);
        }

        public void AddSongId(int songId)
        {
            var db = _musicDatabase.WritableDatabase;
            db.BeginTransaction();

            try
            {
                ICursor mostRecent = null;
                try
                {
                    mostRecent = GetRecentIds("1");
                    if (mostRecent != null && mostRecent.MoveToFirst() && songId == mostRecent.GetInt(0))
                    {
                        return;
                    }
                }
                finally
                {
                    mostRecent?.Close();
                }

                var val = new ContentValues(2);
                val.Put(RecentStoreColumns.ID, songId);
                val.Put(RecentStoreColumns.TIMEPLAYED, SystemClock.CurrentThreadTimeMillis());
                db.Insert(RecentStoreColumns.NAME, null, val);

                ICursor oldest = null;
                try
                {
                    oldest = db.Query(RecentStoreColumns.NAME, new[] {RecentStoreColumns.TIMEPLAYED}, null, null,
                        null, null, $"{RecentStoreColumns.TIMEPLAYED} ASC");
                    if (oldest != null && oldest.Count > MAX_RECENT_ITEM)
                    {
                        oldest.MoveToPosition(oldest.Count - MAX_RECENT_ITEM);
                        var limitTime = oldest.GetLong(0);

                        db.Delete(RecentStoreColumns.NAME, $"{RecentStoreColumns.TIMEPLAYED} < ?",
                            new[] {limitTime.ToString()});
                    }
                }
                finally
                {
                    oldest?.Close();
                }
            }
            finally
            {
                db.SetTransactionSuccessful();
                db.EndTransaction();
            }
        }

        public void RemoveSong(int songId)
        {
            var db = _musicDatabase.WritableDatabase;
            db.Delete(RecentStoreColumns.NAME, $"{RecentStoreColumns.ID} = ?", new[] {songId.ToString()});
        }

        public void DeleteAll()
        {
            var db = _musicDatabase.WritableDatabase;
            db.Delete(RecentStoreColumns.NAME, null, null);
        }

        public ICursor GetRecentIds(string limit)
        {
            var db = _musicDatabase.ReadableDatabase;
            return db.Query(RecentStoreColumns.NAME, new[] {RecentStoreColumns.ID}, null, null, null, null,
                $"{RecentStoreColumns.TIMEPLAYED} DESC", limit);
        }

        public class RecentStoreColumns
        {
            public const string NAME = "recenthistory";
            public const string ID = "songid";
            public const string TIMEPLAYED = "timeplayed";
        }
    }
}