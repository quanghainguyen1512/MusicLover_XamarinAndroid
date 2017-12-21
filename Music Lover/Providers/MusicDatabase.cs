using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
    public class MusicDatabase : SQLiteOpenHelper
    {
        public const string DatabaseName = "music.db";
        private static int VERSION = 4;
        private static MusicDatabase _instance;
        private readonly Context _context;

        private static readonly object Padlock = new object();

        private MusicDatabase(Context context) : base(context, DatabaseName, null, VERSION)
        {
            _context = context;
        }
        
        public static MusicDatabase GetInstance(Context context)
        {
            lock (Padlock)
            {
                if (_instance is null)
                    _instance = new MusicDatabase(context.ApplicationContext);
            }
            return _instance;
        }

        #region Constructor

        public MusicDatabase(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public MusicDatabase(Context context, string name, SQLiteDatabase.ICursorFactory factory, int version) : base(context, name, factory, version)
        {
        }

        public MusicDatabase(Context context, string name, SQLiteDatabase.ICursorFactory factory, int version, IDatabaseErrorHandler errorHandler) : base(context, name, factory, version, errorHandler)
        {
        }

        #endregion

        public override void OnCreate(SQLiteDatabase db)
        {
            MusicPlaybackState.GetInstance(_context).Create(db);
            RecentPlayedStore.GetInstance(_context).Create(db);

        }

        public override void OnUpgrade(SQLiteDatabase db, int oldVersion, int newVersion)
        {
            MusicPlaybackState.GetInstance(_context).Upgrade(db, oldVersion, newVersion);
        }

        public override void OnDowngrade(SQLiteDatabase db, int oldVersion, int newVersion)
        {
            MusicPlaybackState.GetInstance(_context).Downgrade(db, oldVersion, newVersion);
            RecentPlayedStore.GetInstance(_context).Downgrade(db, oldVersion, newVersion);
        }
    }
}