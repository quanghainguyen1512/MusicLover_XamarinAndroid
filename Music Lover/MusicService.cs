#region Using libraries

using System;
using System.Collections.Generic;
using System.Linq;
using Android;
using Android.App;
using Android.Content;
using Android.Database;
using Android.Graphics;
using Android.Media;
using Android.Media.Audiofx;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Media;
using Android.Support.V4.Media.Session;
using Android.Support.V7.Graphics;
using Android.Util;
using Android.Widget;
using Com.Nostra13.Universalimageloader.Core;
using Java.IO;
using Java.Lang;
using Music_Lover.Helpers;
using Music_Lover.Permissions;
using Music_Lover.Providers;
using Music_Lover.Utils;
using Math = System.Math;
using MediaButtonReceiver = Music_Lover.Helpers.MediaButtonReceiver;
using Uri = Android.Net.Uri;
using static Android.Provider.MediaStore.Audio;
using Object = Java.Lang.Object;


#endregion

namespace Music_Lover
{
    public class MusicService : Service
    {
        private readonly object _padlock = new object();

        #region Const fields

        private const string PLAYSTATE_CHANGED = "Music_Lover.playstatechanged";
        private const string POSITION_CHANGED = "Music_Lover.positionchanged";
        private const string META_CHANGED = "Music_Lover.metachanged";
        private const string QUEUE_CHANGED = "Music_Lover.queuechanged";
        private const string PLAYLIST_CHANGED = "Music_Lover.playlistchanged";
        private const string REPEATMODE_CHANGED = "Music_Lover.repeatmodechanged";
        private const string SHUFFLEMODE_CHANGED = "Music_Lover.shufflemodechanged";
        private const string TRACK_ERROR = "Music_Lover.trackerror";
        private const string APP_PACKAGE_NAME = "Music_Lover";
        private const string MUSIC_PACKAGE_NAME = "com.android.music";
        private const string SERVICECMD = "Music_Lover.musicservicecommand";
        private const string TOGGLEPAUSE_ACTION = "Music_Lover.togglepause";
        private const string PAUSE_ACTION = "Music_Lover.pause";
        private const string STOP_ACTION = "Music_Lover.stop";
        private const string PREVIOUS_ACTION = "Music_Lover.previous";
        private const string PREVIOUS_FORCE_ACTION = "Music_Lover.previous.force";
        private const string NEXT_ACTION = "fMusic_Lover.next";
        private const string REPEAT_ACTION = "Music_Lover.repeat";
        private const string SHUFFLE_ACTION = "Music_Lover.shuffle";
        private const string FROM_MEDIA_BUTTON = "frommediabutton";
        private const string REFRESH = "Music_Lover.refresh";
        private const string UPDATE_LOCKSCREEN = "Music_Lover.updatelockscreen";
        private const string CMDNAME = "command";
        private const string CMDTOGGLEPAUSE = "togglepause";
        private const string CMDSTOP = "stop";
        private const string CMDPAUSE = "pause";
        private const string CMDPLAY = "play";
        private const string CMDPREVIOUS = "previous";
        private const string CMDNEXT = "next";
        private const string UPDATE_PREFERENCES = "updatepreferences";

        private const int NEXT = 2;
        private const int LAST = 3;
        private const int SHUFFLE_NONE = 0;
        private const int SHUFFLE_NORMAL = 1;
        private const int SHUFFLE_AUTO = 2;
        private const int REPEAT_NONE = 0;
        private const int REPEAT_CURRENT = 1;
        private const int REPEAT_ALL = 2;
        private const int MAX_HISTORY_SIZE = 1000;
        private const string SHUTDOWN = "Music_Lover.shutdown";
        private const int IDCOLIDX = 0;
        private const int TRACK_ENDED = 1;
        private const int TRACK_WENT_TO_NEXT = 2;
        private const int RELEASE_WAKELOCK = 3;
        private const int SERVER_DIED = 4;
        private const int FOCUSCHANGE = 5;
        private const int FADEDOWN = 6;
        private const int FADEUP = 7;
        private const int IDLE_DELAY = 5 * 60 * 1000;
        private const long REWIND_INSTEAD_PREVIOUS_THRESHOLD = 3000;
        private static readonly string[] PROJECTION = {
            "audio._id AS _id",
            AudioColumns.Artist,
            AudioColumns.Album,
            AudioColumns.Title,
            AudioColumns.Data,
            AudioColumns.MimeType,
            AudioColumns.AlbumId,
            AudioColumns.ArtistId
        };
        private static readonly string[] ALBUM_PROJECTION = {
            AlbumColumns.Album,
            AlbumColumns.Artist,
            AlbumColumns.LastYear
        };
        private static string[] NOTIFICATION_PROJECTION = {
            "audio._id AS _id",
            AudioColumns.AlbumId,
            AudioColumns.Title,
            AudioColumns.Artist,
            AudioColumns.Duration
        };

        private const int NOTIFY_MODE_NONE = 0;
        private const int NOTIFY_MODE_FOREGROUND = 1;
        private const int NOTIFY_MODE_BACKGROUND = 2;
        private static readonly string[] PROJECTION_MATRIX = {
            "_id",
            AudioColumns.Artist,
            AudioColumns.Album,
            AudioColumns.Title,
            AudioColumns.Data,
            AudioColumns.MimeType,
            AudioColumns.AlbumId,
            AudioColumns.ArtistId
        };

        #endregion

        #region Fields
        
        private static readonly Shuffler _shuffler = new Shuffler();
        private static List<int> _history = new List<int>();
        private long _lastPlayedTime;
        private int _notifyMode = NOTIFY_MODE_NONE;
        private long _notificationPostTime = 0;
        private bool _queueIsSaveable = true;
        private bool _pausedByTransientLossOfFocus = false;
        private int _cardId;
        private int _playPos = -1;
        private int _nextPlayPos = -1;
        private int _openFailedCounter = 0;
        private int _serviceStartId = -1;
        private long[] _autoShuffleList = null;
        private RecentPlayedStore _recentPlayed;
        private BroadcastReceiver _intenReceiver;
        private ContentObserver _mediaStoreObserver;
        private IBinder _binder;
        private MediaSessionCompat _session;
        private PowerManager.WakeLock _wakeLock;
        private AlarmManager _alarmManager;
        private PendingIntent _shutdownIntent;
        private MultiPlayer _player;
        private NotificationManagerCompat _notificationManager;
        private ICursor _cursor;
        private ICursor _albumCursor;
        private AudioManager _audioManager;
        private ISharedPreferences _preferences;
        private BroadcastReceiver _unmountReceiver;
        private ComponentName _mediaButtonReceiver;
        private MusicPlaybackState _playbackState;
        private MusicPlayerHandler _playerHandler;
        private AudioManager.IOnAudioFocusChangeListener _audioFocusListener;
        private List<MusicPlaybackTrack> _playlist = new List<MusicPlaybackTrack>();
        private HandlerThread _handlerThread;
        #endregion

        #region Properties

        public long Position => _player.IsInitialized ? _player.Position : -1;
        public int Duration => _player.IsInitialized ? _player.Duration : -1;
        public bool ShutdownScheduled { get; private set; }
        public bool ServiceInUse { get; private set; } = false;
        public bool IsPlaying { get; private set; } = false;
        public int ShuffleMode { get; private set; } = SHUFFLE_NONE;
        public int MediaMountedCount { get; private set; } = 0;
        public int RepeatMode { get; private set; } = REPEAT_NONE;

        #endregion

        #region Overrided methods
        public override IBinder OnBind(Intent intent)
        {
            _binder = new ServiceStub(this);
            ServiceInUse = true;
            return _binder;
        }

        public override bool OnUnbind(Intent intent)
        {
            ServiceInUse = false;
            _binder = null;
            SaveQueue(true);
            if (IsPlaying || _pausedByTransientLossOfFocus)
                return true;
            if (_playlist.Count > 0 || _playerHandler.HasMessages(TRACK_ENDED))
            {
                ScheduleDelayedShutdown();
                return true;
            }
            StopSelf(_serviceStartId);
            return true;
        }

        public override void OnRebind(Intent intent)
        {
            CancelShutdown();
            ServiceInUse = true;
        }

        public override void OnCreate()
        {
            base.OnCreate();
            _notificationManager = NotificationManagerCompat.From(this);

            _playbackState = MusicPlaybackState.GetInstance(this);
            _recentPlayed = RecentPlayedStore.GetInstance(this);

            _handlerThread = new HandlerThread("MusicPlayerHandler", (int)ThreadPriority.Background);
            _handlerThread.Start();

            _playerHandler = new MusicPlayerHandler(this, _handlerThread.Looper);

            _audioManager = (AudioManager) GetSystemService(Context.AudioService);
            _mediaButtonReceiver = new ComponentName(PackageName, typeof(MediaButtonReceiver).Name);

            SetUpMediaSession();

            _preferences = GetSharedPreferences("Service", 0);
            _cardId = GetCardIdAfterGranted();

            _preferences = GetSharedPreferences("Service", 0);
            _cardId = GetCardId();

            RegisterExternalStorageLisenter();

            _player = new MultiPlayer(this) {Handler = _playerHandler};

            _intenReceiver = new IntentReceiver()
            {
                Action = HandleCommandIntent
            };

            var filter = new IntentFilter();
            filter.AddAction(SERVICECMD);
            filter.AddAction(TOGGLEPAUSE_ACTION);
            filter.AddAction(PAUSE_ACTION);
            filter.AddAction(STOP_ACTION);
            filter.AddAction(NEXT_ACTION);
            filter.AddAction(PREVIOUS_ACTION);
            filter.AddAction(PREVIOUS_FORCE_ACTION);
            filter.AddAction(REPEAT_ACTION);
            filter.AddAction(SHUFFLE_ACTION);
            RegisterReceiver(_intenReceiver, filter);

            _mediaStoreObserver = new MediaStoreObserver(_playerHandler)
            {
                Refresh = () => NotifyChange(REFRESH)
            };

            ContentResolver.RegisterContentObserver(Media.InternalContentUri, true, _mediaStoreObserver);
            ContentResolver.RegisterContentObserver(Media.ExternalContentUri, true, _mediaStoreObserver);

            var powerManger = (PowerManager) GetSystemService(PowerService);
            _wakeLock = powerManger.NewWakeLock(WakeLockFlags.Partial, Class.Name);
            _wakeLock.SetReferenceCounted(false);

            var shutdownIntent = new Intent(this, typeof(MusicService));
            shutdownIntent.SetAction(SHUTDOWN);

            _alarmManager = (AlarmManager) GetSystemService(AlarmService);
            _shutdownIntent = PendingIntent.GetService(this, 0, shutdownIntent, 0);

            ScheduleDelayedShutdown();

            ReloadQueueAfterPermissionCheck();

            NotifyChange(QUEUE_CHANGED);
            NotifyChange(META_CHANGED);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            var audioEffIntent = new Intent(AudioEffect.ActionCloseAudioEffectControlSession);
            audioEffIntent.PutExtra(AudioEffect.ExtraAudioSession, GetAudioSessionId());
            audioEffIntent.PutExtra(AudioEffect.ExtraPackageName, PackageName);
            SendBroadcast(audioEffIntent);

            _alarmManager.Cancel(_shutdownIntent);
            _playerHandler.RemoveCallbacksAndMessages(null);

            _handlerThread.Quit();

            _player.Release();
            _player = null;

            _audioManager.AbandonAudioFocus(_audioFocusListener);
            _session.Release();

            ContentResolver.UnregisterContentObserver(_mediaStoreObserver);

            CloseCursor();

            UnregisterReceiver(_intenReceiver);
            if (_unmountReceiver != null)
            {
                UnregisterReceiver(_unmountReceiver);
                _unmountReceiver = null;
            }

            _wakeLock.Release();
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            _serviceStartId = startId;

            if (intent != null)
            {
                var action = intent.Action;
                if (action.Equals(SHUTDOWN))
                {
                    ShutdownScheduled = false;
                    ReleaseServiceUiAndStop();
                    return StartCommandResult.NotSticky;
                }

                HandleCommandIntent(intent);
            }
            ScheduleDelayedShutdown();
            if (intent != null && intent.GetBooleanExtra(FROM_MEDIA_BUTTON, false))
            {
                MediaButtonReceiver.CompleteWakefulIntent(intent);
            }
            return StartCommandResult.NotSticky;
        }

        #endregion

        #region Get Methods
        private int GetAudioSessionId()
        {
            lock (_padlock)
            {
                return _player.AudioSessionId;
            }
        }

        public long GetAudioId()
        {
            var track = GetCurrentTrack();
            return track?.Id ?? -1;
        }

        public long GetArtistId()
        {
            lock (_padlock)
            {
                return _cursor?.GetLong(_cursor.GetColumnIndexOrThrow(AlbumColumns.AlbumId)) ?? -1;
            }
        }

        private int GetCardId()
        {
            var r = ContentResolver;
            var cursor = r.Query(Uri.Parse("content://media/external/fs_id"), null, null, null, null);
            var cardId = -1;
            if (cursor != null && cursor.MoveToFirst())
            {
                cardId = cursor.GetInt(0);
                cursor.Close();
            }

            return cardId;
        }

        public long GetAlbumId()
        {
            lock (_padlock)
            {
                return _cursor?.GetLong(_cursor.GetColumnIndexOrThrow(AudioColumns.AlbumId)) ?? -1;
            }
        }

        public MusicPlaybackTrack GetCurrentTrack() => GetTrack(_playPos);

        public MusicPlaybackTrack GetTrack(int index)
        {
            if (index >= 0 && index < _playlist.Count && _player.IsInitialized)
            {
                lock (_padlock)
                {
                    return _playlist[index];
                }
            }

            return null;
        }

        public int GetQueuePosition()
        {
            lock (_padlock)
            {
                return _playPos;
            }
        }

        public int GetQueueHistorySize()
        {
            lock (_padlock)
            {
                return _history.Count;
            }
        }

        public int GetQueueHistoryPosition(int pos)
        {
            lock (_padlock)
            {
                if (pos >= 0 && pos < _history.Count)
                    return _history[pos];
                return -1;
            }
        }

        public int[] GetQueueHistoryList()
        {
            lock (_padlock)
            {
                return _history.ToArray();
            }
        }

        public string GetPath()
        {
            lock (_padlock)
            {
                return _cursor?.GetString(_cursor.GetColumnIndexOrThrow(AudioColumns.Data));
            }
        }

        public string GetArtistName()
        {
            lock (_padlock)
            {
                return _cursor?.GetString(_cursor.GetColumnIndexOrThrow(AudioColumns.Artist));
            }
        }

        public string GetTrackName()
        {
            lock (_padlock)
            {
                return _cursor?.GetString(_cursor.GetColumnIndexOrThrow(AudioColumns.Title));
            }
        }

        public string GetAlbumName()
        {
            lock (_padlock)
            {
                return _cursor?.GetString(_cursor.GetColumnIndexOrThrow(AudioColumns.Album));
            }
        }

        public string GetGenreName()
        {
            lock (_padlock)
            {
                if (_cursor == null || _playPos < 0 || _playPos >= _playlist.Count) return null;

                var genreProjection = new[] {GenresColumns.Name};
                var uri = Genres.GetContentUriForAudioId("external", (int) _playlist[_playPos].Id);
                var genreCursor = ContentResolver.Query(uri, genreProjection, null, null, null);
                if (genreCursor != null)
                {
                    try
                    {
                        if (genreCursor.MoveToFirst())
                            return genreCursor.GetString(
                                genreCursor.GetColumnIndexOrThrow(GenresColumns.Name));
                    }
                    finally
                    {
                        genreCursor.Close();
                    }
                }

                return null;
            }
        }

        public string GetAlbumArtistName()
        {
            lock (_padlock)
            {
                return _albumCursor?.GetString(
                    _albumCursor.GetColumnIndexOrThrow(AlbumColumns.Artist));
            }
        }
        
        private int GetCardIdAfterGranted()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M
                && !PermissionCenter.CheckPermission(Android.Manifest.Permission.ReadExternalStorage))
            {
                return 0;
            }

            return GetCardId();
        }

        private string GetValueForDownloadedFile(Context context, Uri uri, string column)
        {
            ICursor cursor = null;
            var projection = new[] { column };
            try
            {
                cursor = context.ContentResolver.Query(uri, projection, null, null, null);
                if (cursor != null && cursor.MoveToFirst())
                {
                    return cursor.GetString(0);
                }
            }
            finally
            {
                cursor?.Close();
            }

            return null;
        }

        private int GetNextPosition(bool force)
        {
            if (_playlist is null || _playlist.Count == 0)
                return -1;
            if (!force && RepeatMode == REPEAT_CURRENT)
                return _playPos < 0 ? 0 : _playPos;

            if (ShuffleMode == SHUFFLE_NORMAL)
            {
                var numTracks = _playlist.Count;
                var trackNumPlays = new int[numTracks];
                for (var i = 0; i < numTracks; i++)
                {
                    trackNumPlays[i] = 0;
                }

                var numHistory = _history.Count;
                for (var i = 0; i < numHistory; i++)
                {
                    var index = _history[i];
                    if (index >= 0 && index < numTracks)
                        trackNumPlays[index]++;
                }

                if (_playPos >= 0 && _playPos < numTracks)
                {
                    trackNumPlays[_playPos]++;
                }

                var minNumPlay = int.MaxValue;
                var numTracksWithMinNumPlay = 0;
                for (var i = 0; i < numTracks; i++)
                {
                    if (trackNumPlays[i] < minNumPlay)
                    {
                        minNumPlay = trackNumPlays[i];
                        numTracksWithMinNumPlay = 1;
                    }
                    else if (trackNumPlays[i] == minNumPlay)
                    {
                        numTracksWithMinNumPlay++;
                    }
                }

                if (minNumPlay > 0 && numTracksWithMinNumPlay == numTracks
                                   && RepeatMode != REPEAT_ALL && !force)
                {
                    return -1;
                }

                var skip = _shuffler.NextInt(numTracksWithMinNumPlay);
                for (var i = 0; i < numTracks; i++)
                {
                    if (trackNumPlays[i] == minNumPlay)
                    {
                        if (skip == 0)
                            return i;
                        skip--;
                    }
                }
                return -1;
            }

            if (ShuffleMode == SHUFFLE_AUTO)
            {
                DoAutoShuffleUpdate();
                return _playPos + 1;
            }

            if (_playPos >= _playlist.Count - 1)
            {
                if (RepeatMode == REPEAT_NONE && !force)
                    return -1;
                if (RepeatMode == REPEAT_ALL || force)
                    return 0;
                return -1;
            }

            return _playPos + 1;

        }
        
        public long[] GetQueue()
        {
            lock (_padlock)
            {
                return _playlist.Select(p => p.Id).ToArray();
            }
        }

        public long GetQueueItemAtPosition(int pos)
        {
            lock (_padlock)
            {
                if (pos >= 0 && pos < _playlist.Count)
                    return _playlist[pos].Id;
                return -1;
            }
        }

        public int GetQueueSize()
        {
            lock(_padlock)
            {
                return _playlist.Count;
            }
        }

        public long GetNextAudioId()
        {
            lock (_padlock)
            {
                if (_nextPlayPos >= 0 && _nextPlayPos < _playlist.Count && _player.IsInitialized)
                {
                    return _playlist[_nextPlayPos].Id;
                }
                return -1;
            }
        }

        public long GetPreviousAudioId()
        {
            lock (_padlock)
            {
                if (_player.IsInitialized)
                {
                    var pos = GetPreviousPlayPosition(false);
                    if (pos >= 0 && pos < _playlist.Count)
                        return _playlist[pos].Id;
                }
                return -1;
            }
        }

        #endregion

        #region Set Methods

        private void SetUpMediaSession()
        {
            _session = new MediaSessionCompat(this, "MusicLover");
            var intent = new Intent(Intent.ActionMediaButton);
            intent.SetComponent(_mediaButtonReceiver);
            var mediaPendingIntent = PendingIntent.GetBroadcast(this, 0, intent, 0);
            _session.SetMediaButtonReceiver(mediaPendingIntent);
            _session.SetCallback(new MediaSessionCompatCallback
            {
                Play = Play,
                Pause = () =>
                {
                    Pause();
                    _pausedByTransientLossOfFocus = false;
                },
                SeekTo = Seek,
                SkipToNext = () => GoToNext(true),
                SkipToPrevious = () => Prev(false),
                Stop = () =>
                {
                    Pause();
                    _pausedByTransientLossOfFocus = false;
                    Seek(0);
                    ReleaseServiceUiAndStop();
                }
            });
            _session.SetFlags(MediaSessionCompat.FlagHandlesTransportControls);
        }

        public void SetQueuePosition(int index)
        {
            lock (_padlock)
            {
                Stop(false);
                _playPos = index;
                OpenCurrentAndNext();
                Play();
                NotifyChange(META_CHANGED);
                if (ShuffleMode == SHUFFLE_AUTO)
                    DoAutoShuffleUpdate();
            }
        }

        private void SetRepeatMode(int repeatMode)
        {
            lock (_padlock)
            {
                RepeatMode = repeatMode;
                SetNextTrack();
                SaveQueue(false);
                NotifyChange(REPEATMODE_CHANGED);
            }
        }

        private void SetAndRecordPlayPos(int nextPos)
        {
            lock (_padlock)
            {
                if (ShuffleMode != SHUFFLE_NONE)
                {
                    _history.Add(_playPos);
                    if (_history.Count > MAX_HISTORY_SIZE)
                        _history.RemoveAt(0);
                }

                _playPos = nextPos;
            }
        }

        private void SetNextTrack()
        {
            SetNextTrack(GetNextPosition(false));
        }

        private void SetNextTrack(int pos)
        {
            _nextPlayPos = pos;
            if (_nextPlayPos >= 0 && _playlist != null & _nextPlayPos < _playlist.Count)
            {
                var id = _playlist[_nextPlayPos].Id;
                _player.SetNextDataSource($"{Media.ExternalContentUri}/{id}");
            }
            else
            {
                _player.SetNextDataSource(null);
            }
        }

        private void SetIsSupposedToBePlaying(bool val, bool notify)
        {
            if (IsPlaying != val)
            {
                IsPlaying = val;

                if (!IsPlaying)
                {
                    ScheduleDelayedShutdown();
                    _lastPlayedTime = Java.Lang.JavaSystem.CurrentTimeMillis();
                }
                if (notify)
                    NotifyChange(PLAYSTATE_CHANGED);
            }
        }

        private void SetShuffleMode(int shuffleMode)
        {
            lock (_padlock)
            {
                if (ShuffleMode == shuffleMode && _playlist.Count > 0) return;

                ShuffleMode = shuffleMode;
                if (ShuffleMode == SHUFFLE_AUTO)
                {
                    if (MakeAutoShuffleList())
                    {
                        _playlist.Clear();
                        DoAutoShuffleUpdate();
                        _playPos = 0;
                        OpenCurrentAndNext();
                        Play();
                        NotifyChange(META_CHANGED);
                        return;
                    }

                    ShuffleMode = SHUFFLE_NONE;
                }
                else
                    SetNextTrack();
                SaveQueue(false);
                NotifyChange(SHUFFLEMODE_CHANGED);
            }
        }

        #endregion

        #region Update methods

        private void UpdateCursor(long trackId)
        {
            UpdateCursor($"_id = {trackId}", null);
        }

        private void UpdateCursor(string selection, string[] selectionArgs)
        {
            lock (_padlock)
            {
                CloseCursor();
                _cursor = OpenCursorAndGoToFirst(Media.ExternalContentUri, PROJECTION, selection, selectionArgs);
            }
            UpdateAlbumCursor();
        }

        private void UpdateCursor(Uri uri)
        {
            lock (_padlock)
            {
                CloseCursor();
                _cursor = OpenCursorAndGoToFirst(uri, PROJECTION, null, null);
            }
            UpdateAlbumCursor();
        }

        private void UpdateAlbumCursor()
        {
            var albumId = GetAlbumId();
            _albumCursor = albumId >= 0
                ? OpenCursorAndGoToFirst(Albums.ExternalContentUri, ALBUM_PROJECTION, $"_id = {albumId}", null)
                : null;
        }

        private void UpdateMediaSession(string what)
        {
            var playState = IsPlaying ? PlaybackStateCompat.StatePlaying : PlaybackStateCompat.StatePaused;

            if (what.Equals(PLAYLIST_CHANGED) || what.Equals(POSITION_CHANGED))
            {
                _session.SetPlaybackState(new PlaybackStateCompat.Builder()
                    .SetState(playState, Position, 1f)
                    .SetActions(PlaybackStateCompat.ActionSkipToNext | PlaybackStateCompat.ActionPlay | PlaybackStateCompat.ActionPause |
                            PlaybackStateCompat.ActionPlayPause | PlaybackStateCompat.ActionSkipToPrevious)
                    .Build());
            }
            else if (what.Equals(META_CHANGED) || what.Equals(QUEUE_CHANGED))
            {
                _session.SetMetadata(new MediaMetadataCompat.Builder()
                    .PutString(MediaMetadataCompat.MetadataKeyArtist, GetArtistName())
                    .PutString(MediaMetadataCompat.MetadataKeyAlbumArtist, GetAlbumArtistName())
                    .PutString(MediaMetadataCompat.MetadataKeyAlbum, GetAlbumName())
                    .PutString(MediaMetadataCompat.MetadataKeyTitle, GetTrackName())
                    .PutLong(MediaMetadataCompat.MetadataKeyDuration, Duration)
                    .PutLong(MediaMetadataCompat.MetadataKeyTrackNumber, GetQueuePosition() + 1)
                    .PutLong(MediaMetadataCompat.MetadataKeyNumTracks, GetQueue().Length)
                    .PutString(MediaMetadataCompat.MetadataKeyGenre, GetGenreName())
                    .Build()
                );

                _session.SetPlaybackState(new PlaybackStateCompat.Builder()
                    .SetState(playState, Position, 1f)
                    .SetActions(PlaybackStateCompat.ActionPlay | PlaybackStateCompat.ActionPause | PlaybackStateCompat.ActionPlayPause |
                                PlaybackStateCompat.ActionSkipToNext | PlaybackStateCompat.ActionSkipToPrevious)
                    .Build()
                );
            }
        }

        private void UpdateNotification()
        {
            int newNotiMode;
            if (IsPlaying)
                newNotiMode = NOTIFY_MODE_FOREGROUND;
            else if (RecentlyPlayed())
                newNotiMode = NOTIFY_MODE_BACKGROUND;
            else
                newNotiMode = NOTIFY_MODE_NONE;

            var notiId = GetHashCode();

            if (_notifyMode == newNotiMode)
            {
                if (_notifyMode == NOTIFY_MODE_FOREGROUND)
                {
                    StopForeground(newNotiMode == NOTIFY_MODE_NONE);
                }
                else if (newNotiMode == NOTIFY_MODE_NONE)
                {
                    _notificationManager.Cancel(notiId);
                    _notificationPostTime = 0;
                }
            }
            var noti = BuildNotification();
            if (newNotiMode == NOTIFY_MODE_FOREGROUND)
            {
                StartForeground(notiId, noti);
            }
            else if (newNotiMode == NOTIFY_MODE_BACKGROUND)
            {
                _notificationManager.Notify(notiId, noti);
            }
            _notifyMode = newNotiMode;
        }

        private void UpdateCursorForDownloadedFile(Context context, Uri uri)
        {
            lock (_padlock)
            {
                CloseCursor();
                var cursor = new MatrixCursor(PROJECTION_MATRIX);
                var title = GetValueForDownloadedFile(this, uri, "title");
                cursor.AddRow(new Object[]
                {
                    null, null, null, title, null, null, null, null
                });
                _cursor = cursor;
                _cursor.MoveToFirst();
            }
        }

        #endregion

        #region Music Navigation

        public void Play()
        {
            Play(true);
        }

        public void Play(bool createNewNextTrack)
        {
            _audioFocusListener = new OnAudioFocusChangeListener(_playerHandler);
            var status = _audioManager.RequestAudioFocus(_audioFocusListener, Stream.Music, AudioFocus.Gain);
            if (status != AudioFocusRequest.Granted) return;
            var intent = new Intent(AudioEffect.ActionOpenAudioEffectControlSession);
            intent.PutExtra(AudioEffect.ExtraAudioSession, GetAudioSessionId());
            intent.PutExtra(AudioEffect.ExtraPackageName, PackageName);
            SendBroadcast(intent);

            var component = new ComponentName(PackageName, typeof(MediaButtonReceiver).Name);
            _audioManager.RegisterMediaButtonEventReceiver(component);

            _session.Active = true;

            if (createNewNextTrack)
                SetNextTrack();
            else
                SetNextTrack(_nextPlayPos);

            if (_player.IsInitialized)
            {
                var dur = _player.Duration;
                if (RepeatMode != REPEAT_CURRENT && Duration > 2000 && _player.Position >= Duration - 2000)
                    GoToNext(true);
                _player.Start();
                _playerHandler.RemoveMessages(FADEDOWN);
                _playerHandler.SendEmptyMessage(FADEUP);

                SetIsSupposedToBePlaying(true, true);

                CancelShutdown();
                UpdateNotification();
                NotifyChange(META_CHANGED);
            }
            else if (_playlist.Count <= 0)
                SetShuffleMode(SHUFFLE_AUTO);
        }

        private void Pause()
        {
            lock (_padlock)
            {
                _playerHandler.RemoveMessages(FADEUP);
                if (!IsPlaying) return;

                var intent = new Intent(AudioEffect.ActionCloseAudioEffectControlSession);
                intent.PutExtra(AudioEffect.ExtraAudioSession, GetAudioSessionId());
                intent.PutExtra(AudioEffect.ExtraPackageName, PackageName);
                SendBroadcast(intent);
                _player.Pause();
                NotifyChange(META_CHANGED);
                SetIsSupposedToBePlaying(false, true);
            }
        }

        private int Seek(long pos)
        {
            return Seek((int)pos);
        }

        private int Seek(int pos)
        {
            if (!_player.IsInitialized) return -1;

            if (pos < 0)
                pos = 0;
            else if (pos > _player.Duration)
                pos = _player.Duration;
            var result = _player.Seek(pos);
            NotifyChange(POSITION_CHANGED);
            return result;
        }

        private void Stop(bool goToIdle)
        {
            //            var dur = _player.Duration;
            //            var pos = _player.Position;
            //            if (dur > 30000 && pos >= dur / 2 || pos > 240000)
            //                Scrobble();
            if (_player.IsInitialized)
                _player.Stop();
            CloseCursor();
            if (goToIdle)
                SetIsSupposedToBePlaying(false, false);
            else
                StopForeground(false);
        }

        private void Prev(bool forcePrevious)
        {
            lock (_padlock)
            {
                var goPrev = RepeatMode != REPEAT_CURRENT && Position < REWIND_INSTEAD_PREVIOUS_THRESHOLD ||
                             forcePrevious;
                if (goPrev)
                {
                    var pos = GetPreviousPlayPosition(true);
                    if (pos < 0)
                        return;

                    _nextPlayPos = _playPos;
                    _playPos = pos;
                    Stop(false);
                    OpenCurrent();
                    Play(false);
                    NotifyChange(META_CHANGED);
                }
                else
                {
                    Seek(0);
                    Play(false);
                }
            }
        }

        private void GoToPosition(int pos)
        {
            lock (_padlock)
            {
                if (_playlist.Count <= 0)
                {
                    ScheduleDelayedShutdown();
                    return;
                }

                if (pos < 0) return;
                if (pos == _playPos)
                {
                    if (!IsPlaying)
                        Play();
                    return;
                }
                Stop(false);
                SetAndRecordPlayPos(pos);
                OpenCurrentAndNext();
                Play();
                NotifyChange(META_CHANGED);
            }
        }

        private void GoToNext(bool force)
        {
            lock (_padlock)
            {
                if (_playlist.Count <= 0)
                {
                    ScheduleDelayedShutdown();
                    return;
                }

                var pos = _nextPlayPos;
                if (pos < 0)
                    pos = GetNextPosition(force);
                if (pos < 0)
                {
                    SetIsSupposedToBePlaying(false, true);
                    return;
                }
                Stop(false);
                SetAndRecordPlayPos(pos);
                OpenCurrentAndNext();
                Play();
                NotifyChange(META_CHANGED);
            }
        }

        #endregion

        private Notification BuildNotification()
        {
            var albumName = GetAlbumName();
            var artistName = GetArtistName();
            var isPlaying = IsPlaying;
            var text = string.IsNullOrEmpty(albumName) ? artistName : $"{artistName} - {albumName}";
            var playBtnResId = isPlaying ? Resource.Drawable.ic_pause_white_36dp : Resource.Drawable.ic_play_white_36dp;

            var nowPlayingIntent = NavigationUtils.GetNowPlayingIntent(this);
            var clickIntent = PendingIntent.GetActivity(this, 0, nowPlayingIntent, PendingIntentFlags.UpdateCurrent);

            var artPic = ImageLoader.Instance.LoadImageSync(Utils.Utils.GetAlbumArtUri(GetAlbumId()).ToString()) ??
                         ImageLoader.Instance.LoadImageSync($"drawable://{Resource.Drawable.ic_empty_music2}");

            if (_notificationPostTime == 0)
                _notificationPostTime = JavaSystem.CurrentTimeMillis();

            var builder = new NotificationCompat.Builder(this)
                .SetSmallIcon(Resource.Drawable.ic_notification)
                .SetLargeIcon(artPic)
                .SetContentIntent(clickIntent)
                .SetContentText(text)
                .SetWhen(_notificationPostTime)
                .AddAction(Resource.Drawable.ic_skip_previous_white_36dp,
                    "", RetrievePlaybackAction(PREVIOUS_ACTION))
                .AddAction(playBtnResId, "", RetrievePlaybackAction(TOGGLEPAUSE_ACTION))
                .AddAction(Resource.Drawable.ic_skip_next_white_36dp, "",
                    RetrievePlaybackAction(NEXT_ACTION));

            builder.SetVisibility((int)NotificationVisibility.Public);

            var style = new Android.Support.V4.Media.App.NotificationCompat.MediaStyle()
                .SetMediaSession(_session.SessionToken)
                .SetShowActionsInCompactView(0, 1, 2, 3);
            builder.SetStyle(style);
            if (artPic != null)
                builder.SetColor(Palette.From(artPic).Generate().GetVibrantColor(Color.ParseColor("#403f4d")));
            return builder.Build();
        }

        private PendingIntent RetrievePlaybackAction(string action)
        {
            var serviceName = new ComponentName(this, typeof(MusicService).Name);
            var i = new Intent(action);
            i.SetComponent(serviceName);
            return PendingIntent.GetService(this, 0, i, 0);
        }

        public void RegisterExternalStorageLisenter()
        {
            if (_unmountReceiver != null) return;

            _unmountReceiver = new UnMountReceiver()
            {
                DelegateAction = (context, intent) =>
                {
                    var action = intent.Action;
                    if (action.Equals(Intent.ActionMediaEject))
                    {
                        SaveQueue(true);
                        _queueIsSaveable = false;
                        CloseExternalStorageFiles(intent.Data.Path);
                    }
                    else if (action.Equals(Intent.ActionMediaMounted))
                    {
                        MediaMountedCount++;
                        _cardId = GetCardId();
                        ReloadQueueAfterPermissionCheck();
                        _queueIsSaveable = true;
                        NotifyChange(QUEUE_CHANGED);
                        NotifyChange(META_CHANGED);
                    }
                }
            };

            var filter = new IntentFilter();
            filter.AddAction(Intent.ActionMediaEject);
            filter.AddAction(Intent.ActionMediaMounted);
            filter.AddDataScheme("file");
            RegisterReceiver(_unmountReceiver, filter);
        }

        private void ReloadQueueAfterPermissionCheck()
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M && 
                !PermissionCenter.CheckPermission(Manifest.Permission.ReadExternalStorage))
                return;
            ReloadQueue();
        }

        private void ReloadQueue()
        {
            var id = _cardId;
            if (_preferences.Contains("cardid"))
                id = _preferences.GetInt("cardid", _cardId);
            if (id == _cardId)
                _playlist = _playbackState.GetQueue();
            if (_playlist.Count > 0)
            {
                var pos = _preferences.GetInt("curpos", 0);
                if (pos < 0 || pos >= _playlist.Count)
                {
                    _playlist.Clear();
                    return;
                }

                _playPos = pos;
                UpdateCursor(_playlist[_playPos].Id);
                if (_cursor == null)
                {
                    SystemClock.Sleep(3000);
                    UpdateCursor(_playlist[_playPos].Id);
                }

                lock (_padlock)
                {
                    CloseCursor();
                    _openFailedCounter = 20;
                    OpenCurrentAndNext();
                }

                if (!_player.IsInitialized)
                {
                    _playlist.Clear();
                    return;
                }

                var seekPos = (int)_preferences.GetLong("seekpos", 0);
                Seek(seekPos >= 0 && seekPos < Duration ? seekPos : 0);

                int repMode = _preferences.GetInt("repeatmode", REPEAT_NONE);
                if (repMode != REPEAT_ALL && repMode != REPEAT_CURRENT)
                    repMode = REPEAT_NONE;
                RepeatMode = repMode;

                var shufMode = _preferences.GetInt("shufflemode", SHUFFLE_NONE);
                if (shufMode != SHUFFLE_AUTO && shufMode != SHUFFLE_NORMAL)
                    shufMode = SHUFFLE_NONE;
                if (shufMode != SHUFFLE_NONE)
                    _history = _playbackState.GetHistory(_playlist.Count);
                if (shufMode == SHUFFLE_AUTO)
                    if (!MakeAutoShuffleList())
                        shufMode = SHUFFLE_NONE;
                ShuffleMode = shufMode;
            }
        }

        private void CloseExternalStorageFiles(string dataPath)
        {
            Stop(true);
            NotifyChange(QUEUE_CHANGED);
            NotifyChange(META_CHANGED);
        }

        public void HandleCommandIntent(Intent intent)
        {
            var action = intent.Action;
            var cmd = action.Equals(SERVICECMD) ? intent.GetStringExtra(CMDNAME) : null;

            if (intent.Action != null && intent.Action.Equals("SWITCH_TRACK"))
            {
                GoToPosition(_playPos + intent.GetIntExtra("new_queue_position", 0));
                return;
            }

            if (cmd.Equals(CMDNEXT) || action.Equals(NEXT_ACTION))
                GoToNext(true);
            else if (cmd.Equals(CMDPREVIOUS) || action.Equals(PREVIOUS_ACTION) || action.Equals(PREVIOUS_FORCE_ACTION))
                Prev(PREVIOUS_FORCE_ACTION.Equals(action));
            else if (cmd.Equals(CMDTOGGLEPAUSE) || action.Equals(TOGGLEPAUSE_ACTION))
            {
                if (IsPlaying)
                {
                    Pause();
                    _pausedByTransientLossOfFocus = false;
                }
                else
                {
                    Play();
                }
            }
            else if (cmd.Equals(CMDPAUSE) || action.Equals(PAUSE_ACTION))
            {
                Pause();
                _pausedByTransientLossOfFocus = false;
            }
            else if (cmd.Equals(CMDPLAY))
            {
                Play();
            }
            else if (cmd.Equals(CMDSTOP) || action.Equals(STOP_ACTION))
            {
                Pause();
                _pausedByTransientLossOfFocus = false;
                Seek(0);
                ReleaseServiceUiAndStop();
            }
            else if (action.Equals(REPEAT_ACTION))
            {
                CycleRepeat();
            }
            else if (action.Equals(SHUFFLE_ACTION))
            {
                CycleShuffle();
            }
            else if (action.Equals(UPDATE_PREFERENCES))
            {
                NotifyChange(META_CHANGED);
            }
        }

        private void CycleRepeat()
        {
            if (RepeatMode == REPEAT_NONE)
            {
                SetRepeatMode(REPEAT_CURRENT);
                if (ShuffleMode != SHUFFLE_NONE)
                    SetShuffleMode(SHUFFLE_NONE);
            }
            else
                SetRepeatMode(REPEAT_NONE);
        }

        private bool MakeAutoShuffleList()
        {
            ICursor cursor = null;
            try
            {
                cursor = ContentResolver.Query(Media.ExternalContentUri, new[] {MediaStore.MediaColumns.Id},
                    $"{AudioColumns.IsMusic} = 1", null, null);
                if (cursor == null || cursor.Count == 0)
                    return false;
                var len = cursor.Count;
                var list = new long[len];
                for (int i = 0; i < len; i++)
                {
                    cursor.MoveToNext();
                    list[i] = cursor.GetLong(0);
                }

                _autoShuffleList = list;
                return true;
            }
            catch (RuntimeException)
            {
            }
            finally
            {
                cursor?.Close();
            }

            return false;
        }

        private void CycleShuffle()
        {
            if (ShuffleMode == SHUFFLE_NONE)
                SetShuffleMode(SHUFFLE_NORMAL);
            else if (ShuffleMode == SHUFFLE_NORMAL || ShuffleMode == SHUFFLE_AUTO)
                SetShuffleMode(SHUFFLE_NONE);
        }

        private void ReleaseServiceUiAndStop()
        {
            if (IsPlaying || _pausedByTransientLossOfFocus ||
                _playerHandler.HasMessages(TRACK_ENDED))
                return;

            CancelNotification();
            _audioManager.AbandonAudioFocus(_audioFocusListener);
            _session.Active = false;

            if (!ServiceInUse)
            {
                SaveQueue(true);
                StopSelf(_serviceStartId);
            }
        }

        private void CancelNotification()
        {
            StopForeground(true);
            _notificationManager.Cancel(GetHashCode());
            _notificationPostTime = 0;
            _notifyMode = NOTIFY_MODE_NONE;
        }

        private int GetPreviousPlayPosition(bool removeFromHistory)
        {
            lock (_padlock)
            {
                if (ShuffleMode == SHUFFLE_NORMAL)
                {
                    var hisSize = _history.Count;
                    if (hisSize == 0)
                        return -1;
                    var pos = _history[hisSize - 1];
                    if (removeFromHistory)
                        _history.RemoveAt(hisSize - 1);
                    return pos;
                }

                if (_playPos > 0)
                    return _playPos - 1;
                return _playlist.Count - 1;
            }
        }

        private void OpenCurrent()
        {
            OpenCurrentAndMaybeNext(false);
        }

        private void CancelShutdown()
        {
            if (ShutdownScheduled)
            {
                _alarmManager.Cancel(_shutdownIntent);
                ShutdownScheduled = false;
            }
        }

        private void ScheduleDelayedShutdown()
        {
            _alarmManager.Set(AlarmType.ElapsedRealtimeWakeup, SystemClock.ElapsedRealtime() + IDLE_DELAY, _shutdownIntent);
            ShutdownScheduled = true;
        }
        
        private bool RecentlyPlayed()
        {
            return IsPlaying || JavaSystem.CurrentTimeMillis() - _lastPlayedTime < IDLE_DELAY;
        }

        private void OpenCurrentAndNext()
        {
            OpenCurrentAndMaybeNext(true);
        }

        private void OpenCurrentAndMaybeNext(bool openNext)
        {
            lock (_padlock)
            {
                CloseCursor();
                if (_playlist.Count == 0) return;

                Stop(false);
                var shutdown = false;
                
                UpdateCursor(_playlist[_playPos].Id);
                while (true)
                {
                    if (_cursor != null && OpenFile($"{Media.ExternalContentUri}/{_cursor.GetLong(IDCOLIDX)}"))
                    {
                        break;
                    }
                    CloseCursor();
                    if (_openFailedCounter++ < 10 && _playlist.Count > 1)
                    {
                        var pos = GetNextPosition(false);
                        if (pos < 0)
                        {
                            shutdown = true;
                            break;
                        }

                        _playPos = pos;
                        Stop(false);
                        UpdateCursor(_playlist[_playPos].Id);
                    }
                    else
                    {
                        _openFailedCounter = 0;
                        shutdown = true;
                        break;
                    }
                }
            }
        }

        private void AddToPlayList(long[] list, int pos, long srcId, Utils.Utils.SourceTypeId srcType)
        {
            var len = list.Length;
            if (pos < 0)
            {
                _playlist.Clear();
                pos = 0;
            }

            _playlist.Capacity = _playlist.Count + len;
            if (pos > _playlist.Count)
                pos = _playlist.Count;

            var arr = new List<MusicPlaybackTrack>(len);
            for (int i = 0; i < list.Length; i++)
            {
                arr.Add(new MusicPlaybackTrack(list[i], srcId, srcType, i));
            }

            _playlist.AddRange(arr);

            if (_playlist.Count == 0)
            {
                CloseCursor();
                NotifyChange(META_CHANGED);
            }
        }
        
        private ICursor OpenCursorAndGoToFirst(Uri uri, string[] projection, string selection, string[] selectionArgs)
        {
            var c = ContentResolver.Query(uri, projection, selection, selectionArgs, null);
            if (c is null) return null;
            if (!c.MoveToFirst())
            {
                c.Close();
                return null;
            }

            return c;
        }

        private void SaveQueue(bool full)
        {
            if (!_queueIsSaveable)
                return;

            var editor = _preferences.Edit();
            if (full)
            {
                _playbackState.SaveState(_playlist, ShuffleMode != SHUFFLE_NONE ? _history : null);
                editor.PutInt("cardid", _cardId);
            }

            editor.PutInt("curpos", _playPos);
            if (_player.IsInitialized)
                editor.PutLong("seekingpos", _player.Position);
            editor.PutInt("repeatmode", RepeatMode);
            editor.PutInt("shufflemode", ShuffleMode);
            editor.Apply();
        }

        private void SendErrorMessage(string trackName)
        {
            var i = new Intent(TRACK_ERROR);
            i.PutExtra("trackname", trackName);
            SendBroadcast(i);
        }

        private int RemoveTrack(long trackId)
        {
            var result = 0;
            lock (_padlock)
            {
                for (int i = 0; i < _playlist.Count; i++)
                {
                    if (_playlist[i].Id == trackId)
                    {
                        result += RemoveTracksInternal((int)trackId, (int)trackId);
                        i--;
                    }
                }
            }

            return result;
        }

        private int RemoveTracksInternal(int first, int last)
        {
            lock (_padlock)
            {
                if (last < first)
                    return 0;
                else if (first < 0)
                    first = 0;
                else if (last >= _playlist.Count)
                    last = _playlist.Count - 1;

                var toNext = false;
                if (first <= _playPos && _playPos <= last)
                {
                    _playPos = first;
                    toNext = true;
                }
                else if (_playPos > last)
                {
                    _playPos -= last - first + 1;
                }

                var removeIndex = last - first + 1;
                if (first == 0 && last == _playlist.Count - 1)
                {
                    _playPos = -1;
                    _nextPlayPos = -1;
                    _playlist.Clear();
                    _history.Clear();
                }
                else
                {
                    for (var i = 0; i < removeIndex; i++)
                    {
                        _playlist.RemoveAt(first);
                    }

                    for (int i = 0; i < _history.Count; i++)
                    {
                        var pos = _history[i];
                        if (pos >= first && pos <= last)
                            _history.RemoveAt(i);
                        else if (pos > last)
                            _history[i] = pos - removeIndex;
                    }
                }

                if (toNext)
                {
                    if (_playlist.Count == 0)
                    {
                        Stop(true);
                        _playPos = -1;
                        CloseCursor();
                    }
                    else
                    {
                        if (!ShuffleMode.Equals(SHUFFLE_NONE))
                            _playPos = GetNextPosition(true);
                        else if (_playPos >= _playlist.Count)
                            _playPos = 0;
                        var wasPlaying = IsPlaying;
                        Stop(false);
                        OpenCurrentAndNext();
                        if (wasPlaying)
                            Play();
                    }
                    NotifyChange(META_CHANGED);
                }

                return last - first + 1;
            } 
        }
        
        private bool RemoveTrackAtPosition(long id, int pos)
        {
            lock (_padlock)
            {
                if (pos >= 0 && pos < _playlist.Count && _playlist[pos].Id == id)
                    return RemoveTracks(pos, pos) > 0;
            }
            return false;
        }

        public bool OpenFile(string path)
        {
            lock (_padlock)
            {
                if (string.IsNullOrEmpty(path))
                    return false;

                if (_cursor is null)
                {
                    var uri = Uri.Parse(path);
                    var shouldAddToPlaylist = true;
                    long id = -1;
                    try
                    {
                        id = Long.ParseLong(uri.LastPathSegment);
                    }
                    catch(NumberFormatException) { }

                    if (id != -1 && path.StartsWith(Media.ExternalContentUri.ToString()))
                        UpdateCursor(uri);
                    else if (id != -1 && path.StartsWith(MediaStore.Files.GetContentUri("external").ToString()))
                        UpdateCursor(id);
                    else if (path.StartsWith("content://downloads/"))
                    {
                        var mpUri = GetValueForDownloadedFile(this, uri, "mediaprovider_uri");
                        if (!string.IsNullOrEmpty(mpUri))
                        {
                            if (OpenFile(mpUri))
                            {
                                NotifyChange(META_CHANGED);
                                return true;
                            }

                            return false;
                        }

                        UpdateCursorForDownloadedFile(this, uri);
                        shouldAddToPlaylist = false;
                    }
                    else
                    {
                        const string @where = MediaStore.MediaColumns.Data + "=?";
                        var selArgs = new[] {path};
                        UpdateCursor(where, selArgs);
                    }
                    try
                    {
                        if (_cursor != null && shouldAddToPlaylist)
                        {
                            _playlist.Clear();
                            _playlist.Add(new MusicPlaybackTrack(_cursor.GetLong(IDCOLIDX), -1, Utils.Utils.SourceTypeId.NA, -1));
                            NotifyChange(QUEUE_CHANGED);
                            _playPos = 0;
                            _history.Clear();
                        }
                    }
                    catch(UnsupportedOperationException) { }
                }

                _player.SetDataSource(path);
                if (_player.IsInitialized)
                {
                    _openFailedCounter = 0;
                    return true;
                }

                var trackName = GetTrackName();
                if (string.IsNullOrEmpty(trackName))
                    trackName = path;
                SendErrorMessage(trackName);

                Stop(true);
                return false;
            }
        }

        private void DoAutoShuffleUpdate()
        {
            var notify = false;
            if (_playPos > 10)
            {
                RemoveTracks(0, _playPos - 9);
                notify = true;
            }

            var toAdd = 7 - (_playlist.Count - (_playPos < 0 ? -1 : _playPos));
            for (var i = 0; i < toAdd; i++)
            {
                var lookback = _history.Count;
                var index = -1;
                while (true)
                {
                    index = _shuffler.NextInt(_autoShuffleList.Length);
                    if (!WasRecentlyUsed(index, lookback))
                    {
                        break; 
                    }

                    lookback /= 2;
                }
                _history.Add(index);
                if (_history.Count > MAX_HISTORY_SIZE)
                {
                    _history.RemoveAt(0);
                }
                _playlist.Add(new MusicPlaybackTrack(_autoShuffleList[index], -1, Utils.Utils.SourceTypeId.NA, -1));
                notify = true;
            }

            if (notify) NotifyChange(QUEUE_CHANGED);
        }

        private bool WasRecentlyUsed(int index, int lookbackSize)
        {
            if (lookbackSize == 0)
                return false;
            var hisSize = _history.Count;
            if (hisSize < lookbackSize)
                lookbackSize = hisSize;

            var maxIndex = hisSize - 1;
            for (var i = 0; i < lookbackSize; i++)
            {
                var entry = _history[maxIndex - 1];
                if (entry == index)
                    return true;
            }

            return false;
        }

        public void NotifyChange(string what)
        {
            UpdateMediaSession(what);
            if (what.Equals(POSITION_CHANGED)) return;

            var intent = new Intent(what);
            var bundle = new Bundle();
            bundle.PutLong("id", GetAudioId());
            bundle.PutString("artist", GetArtistName());
            bundle.PutString("album", GetAlbumName());
            bundle.PutLong("albumid", GetAlbumId());
            bundle.PutString("track", GetTrackName());
            bundle.PutBoolean("playing", IsPlaying);

            intent.PutExtras(bundle);

            SendBroadcast(intent);

            var musicIntent = new Intent(intent);
            musicIntent.SetAction(what.Replace(APP_PACKAGE_NAME, MUSIC_PACKAGE_NAME));
            SendBroadcast(musicIntent);

            if (what.Equals(META_CHANGED))
            {
                _recentPlayed.AddSongId(GetAudioId());
            }
            else if (what.Equals(QUEUE_CHANGED))
            {
                SaveQueue(true);
                if (IsPlaying)
                {
                    if (_nextPlayPos >= 0 && _nextPlayPos < _playlist.Count && ShuffleMode != SHUFFLE_NONE)
                        SetNextTrack(_nextPlayPos);
                    else
                        SetNextTrack();
                }
            }
            else
            {
                SaveQueue(false);
            }

            if (what.Equals(PLAYLIST_CHANGED))
                UpdateNotification();
        }

        private int RemoveTracks(int first, int last)
        {
            var numRemoved = RemoveTracksInternal(first, last);
            if (numRemoved > 0)
            {
                NotifyChange(QUEUE_CHANGED);
            }

            return numRemoved;
        }

        private void ShufflePlaylist()
        {
            if (_playlist.Count == 0) return;
            var shuffledList = new List<MusicPlaybackTrack>();
            var r = new Random();
            while (_playlist.Count > 0)
            {
                var item = _playlist[r.Next(_playlist.Count)];
                _playlist.Remove(item);
                shuffledList.Add(item);
            }

            _playlist = shuffledList;
        }
        
        private void CloseCursor()
        {
            lock(_padlock)
            {
                if (_cursor != null)
                {
                    _cursor.Close();
                    _cursor = null;
                }

                if (_albumCursor != null)
                {
                    _albumCursor.Close();
                    _albumCursor = null;
                }
            }
        }

        public void Open(long[] list, int position, long srcId, Utils.Utils.SourceTypeId srcType)
        {
            if (ShuffleMode == SHUFFLE_AUTO)
                ShuffleMode = SHUFFLE_AUTO;
            var oldId = GetAudioId();
            var len = list.Length;
            var isNewList = true;
            if (_playlist.Count == len)
            {
                isNewList = list.Any(e => !_playlist.Select(p => p.Id).Contains(e));
            }
            if (isNewList)
            {
                AddToPlayList(list, -1, srcId, srcType);
                NotifyChange(QUEUE_CHANGED);
            }
            if (position >= 0)
            {
                _playPos = position;
            }
            else
            {
                _playPos = _shuffler.NextInt(_playlist.Count);
            }
            _history.Clear();
            OpenCurrentAndNext();
            if (oldId != GetAudioId())
            {
                NotifyChange(META_CHANGED);
            }
        }

        public void Enqueue(long[] list, int action, long sourceId, Utils.Utils.SourceTypeId sourceType)
        {
            lock (_padlock)
            {
                if (action == NEXT && _playPos + 1 < _playlist.Count)
                {
                    AddToPlayList(list, _playPos + 1, sourceId, sourceType);
                    _nextPlayPos = _playPos + 1;
                    NotifyChange(QUEUE_CHANGED);
                }
                else
                {
                    AddToPlayList(list, int.MaxValue, sourceId, sourceType);
                    NotifyChange(QUEUE_CHANGED);
                }

                if (_playPos < 0)
                {
                    _playPos = 0;
                    OpenCurrentAndNext();
                    Play();
                    NotifyChange(META_CHANGED);
                }
            }
        }

        public void MoveQueueItem(int from, int to)
        {
            lock (_padlock)
            {
                if (from >= _playlist.Count)
                    from = _playlist.Count - 1;
                if (to >= _playlist.Count)
                    to = _playlist.Count - 1;

                if (from == to) return;

                var track = _playlist[from];
                _playlist.RemoveAt(from);

                if (from < to)
                {
                    _playlist.Insert(to, track);
                    if (_playPos == from)
                        _playPos = to;
                    else if (_playPos >= to && _playPos <= from)
                        _playPos++;
                }
                NotifyChange(QUEUE_CHANGED);
            }
        }

        public void Refresh()
        {
            NotifyChange(REFRESH);
        }

        public void PlaylistChanged()
        {
            NotifyChange(PLAYLIST_CHANGED);
        }
        
        public void SeekRelative(long deltaInMs)
        {
            lock (_padlock)
            {
                if (!_player.IsInitialized)
                    return;
                var newPos = Position + deltaInMs;
                var dur = Duration;
                if (newPos < 0)
                {
                    Prev(true);
                    Seek(dur + newPos);
                }
                else if (newPos >= dur)
                {
                    GoToNext(true);
                    Seek(newPos - dur);
                }
                else
                    Seek(newPos);
            }
        }

        #region Nested classes

        private class Shuffler
        {
            private readonly Queue<int> _historyOfNums = new Queue<int>();
            private readonly HashSet<int> _previousNums = new HashSet<int>();
            private readonly Random _random = new Random();
            private int _previous;
            public int NextInt(int max)
            {
                var next = 0;
                do
                {
                    next = _random.Next(max);
                } while (next == _previous && max > 1 && !_previousNums.Contains(next));

                _previous = next;
                _historyOfNums.Enqueue(_previous);
                _previousNums.Add(_previous);

                if (_historyOfNums.Count != 0 && _historyOfNums.Count >= MAX_HISTORY_SIZE)
                {
                    for (var i = 0; i < Math.Max(1, MAX_HISTORY_SIZE / 2); i++)
                    {
                        _previousNums.Remove(_historyOfNums.Dequeue());
                    }
                }

                return next;
            }
        }

        private class MediaSessionCompatCallback : MediaSessionCompat.Callback
        {
            #region Properties (Actions)

            public Action Play { private get; set; }
            public Action Pause { private get; set; }
            public Action Stop { private get; set; }
            public Func<int, int> SeekTo { private get; set; }
            public Action SkipToNext { private get; set; }
            public Action SkipToPrevious { private get; set; }

            #endregion

            public override void OnPause() => Pause(); 
            public override void OnPlay() => Play();
            public override void OnSeekTo(long pos) => SeekTo((int)pos);
            public override void OnSkipToNext() => SkipToNext();
            public override void OnSkipToPrevious() => SkipToPrevious();
            public override void OnStop() => Stop();
        }

        private class OnAudioFocusChangeListener : Java.Lang.Object, AudioManager.IOnAudioFocusChangeListener
        {
            private readonly MusicPlayerHandler _playerHandler;

            public OnAudioFocusChangeListener(MusicPlayerHandler playerHandler)
            {
                _playerHandler = playerHandler;
            }

            public void OnAudioFocusChange(AudioFocus focusChange)
            {
                _playerHandler.ObtainMessage(FOCUSCHANGE, (int)focusChange, 0).SendToTarget();
            }
        }

        private class MultiPlayer : Java.Lang.Object,MediaPlayer.IOnErrorListener, MediaPlayer.IOnCompletionListener
        {
            private readonly MusicService _service;

            private MediaPlayer _curPlayer = new MediaPlayer();
            private MediaPlayer _nextPlayer;
            public bool IsInitialized { get; private set; }

            public Handler Handler { private get; set; }
            public int Duration => _curPlayer.Duration;
            public int Position => _curPlayer.CurrentPosition;
            public int AudioSessionId => _curPlayer.AudioSessionId;

            public MultiPlayer(MusicService service)
            {
                _service = service;
                _curPlayer.SetWakeMode(_service, WakeLockFlags.Partial);
            }

            public void SetNextDataSource(string path)
            {
                try
                {
                    _curPlayer.SetNextMediaPlayer(null);
                }
                catch
                {
                    return;
                }

                if (_nextPlayer != null)
                {
                    _nextPlayer.Release();
                    _nextPlayer = null;
                }

                if (path == null)
                    return;
                _nextPlayer = new MediaPlayer();
                _nextPlayer.SetWakeMode(_service, WakeLockFlags.Partial);
                _nextPlayer.AudioSessionId = _curPlayer.AudioSessionId;
                try
                {
                    if (SetDataSource(_curPlayer, path))
                        _curPlayer.SetNextMediaPlayer(_nextPlayer);
                    else
                    {
                        if (_nextPlayer != null)
                        {
                            _nextPlayer.Release();
                            _nextPlayer = null;
                        }
                    }
                }
                catch (IllegalStateException)
                {

                }
            }

            public void SetDataSource(string path)
            {
                try
                {
                    IsInitialized = SetDataSource(_curPlayer, path);
                    if (IsInitialized)
                        SetNextDataSource(null);
                }
                catch (IllegalStateException)
                {
                    Toast.MakeText(_service, "Something wrong", ToastLength.Short).Show();
                }
            }

            private bool SetDataSource(MediaPlayer player, string path)
            {
                try
                {
                    player.Reset();
                    player.SetOnPreparedListener(null);
                    if (path.StartsWith("content://"))
                        player.SetDataSource(_service, Uri.Parse(path));
                    else
                        player.SetDataSource(path);
                    player.SetAudioStreamType(Stream.Music);
                    player.Prepare();
                }
                catch (IOException)
                {
                    return false;
                }
                catch (IllegalArgumentException)
                {
                    return false;
                }
                player.SetOnCompletionListener(this);
                player.SetOnErrorListener(this);
                return true;
            }

            public void Start() => _curPlayer.Start();

            public void Stop()
            {
                _curPlayer.Reset();
                IsInitialized = false;
            }

            public void Release() => _curPlayer.Release();

            public void Pause() => _curPlayer.Pause();

            public int Seek(int nextPos)
            {
                _curPlayer.SeekTo(nextPos);
                return nextPos;
            }

            public void SetVolume(float val)
            {
                try
                {
                    _curPlayer.SetVolume(val, val);
                }
                catch(IllegalStateException) { }
            }

            public bool OnError(MediaPlayer mp, MediaError what, int extra)
            {
                Log.Verbose("MultiPlayer", $"Error: {what}, extra: {extra}");

                switch (what)
                {
                    case MediaError.ServerDied:
                    {
                        IsInitialized = false;
                        _curPlayer.Release();
                        _curPlayer = new MediaPlayer();
                        _curPlayer.SetWakeMode(_service, WakeLockFlags.Partial);
                        var msg = Handler.ObtainMessage(SERVER_DIED);
                        Handler.SendMessageDelayed(msg, 2000);
                        return true;
                    }
                    default:
                        return false;
                }
            }

            public void OnCompletion(MediaPlayer mp)
            {
                if (mp == _curPlayer && _nextPlayer != null)
                {
                    _curPlayer.Release();
                    _curPlayer = _nextPlayer;
                    _nextPlayer = null;
                    Handler.SendEmptyMessage(TRACK_WENT_TO_NEXT);
                }
                else
                {
                    _service._wakeLock.Acquire(30000);
                    Handler.SendEmptyMessage(TRACK_ENDED);
                    Handler.SendEmptyMessage(RELEASE_WAKELOCK);
                }
            }
        }

        private class ServiceStub : IMusicServiceStub
        {
            private MusicService _service;

            public ServiceStub(MusicService service)
            {
                _service = service;
            }
            
            public override void OpenFile(string path) => _service.OpenFile(path);

            public override void Open(long[] list, int position, long sourceId, int sourceType)
                => _service.Open(list, position, sourceId, (Utils.Utils.SourceTypeId)sourceType);
            

            public override void Stop() => _service.Stop(true);

            public override void Pause() => _service.Pause();

            public override void Play() => _service.Play();

            public override void Prev(bool forcePrevious) => _service.Prev(forcePrevious);

            public override void Next() => _service.GoToNext(true);

            public override void Enqueue(long[] list, int action, long sourceId, int sourceType) 
                => _service.Enqueue(list, action, sourceId, (Utils.Utils.SourceTypeId)sourceType);

            public override void SetQueuePosition(int index) => _service.SetQueuePosition(index);

            public override void SetShuffleMode(int shufflemode) => _service.SetShuffleMode(shufflemode);

            public override void SetRepeatMode(int repeatmode) => _service.SetRepeatMode(repeatmode);

            public override void MoveQueueItem(int @from, int to) => _service.MoveQueueItem(from, to);

            public override void Refresh() => _service.Refresh();

            public override void PlaylistChanged() => _service.PlaylistChanged();

            public override bool IsPlaying() => _service.IsPlaying;

            public override long[] GetQueue() => _service.GetQueue();

            public override long GetQueueItemAtPosition(int position) => _service.GetQueueItemAtPosition(position);

            public override int GetQueueSize() => _service.GetQueueSize();

            public override int GetQueuePosition() => _service.GetQueuePosition();

            public override int GetQueueHistoryPosition(int position) => _service.GetQueueHistoryPosition(position);

            public override int GetQueueHistorySize() => _service.GetQueueHistorySize();

            public override int[] GetQueueHistoryList() => _service.GetQueueHistoryList();

            public override long Duration() => _service.Duration;

            public override long Position() => _service.Position; 

            public override long Seek(long pos) => _service.Seek(pos);

            public override void SeekRelative(long deltaInMs) => _service.SeekRelative(deltaInMs);

            public override long GetAudioId() => _service.GetAudioId();

            public override MusicPlaybackTrack GetCurrentTrack() =>_service.GetCurrentTrack();

            public override MusicPlaybackTrack GetTrack(int index) => _service.GetTrack(index);
            

            public override long GetNextAudioId() => _service.GetNextAudioId();

            public override long GetPreviousAudioId() => _service.GetPreviousAudioId();

            public override long GetArtistId() => _service.GetArtistId();

            public override long GetAlbumId() => _service.GetAlbumId();

            public override string GetArtistName() => _service.GetArtistName();

            public override string GetTrackName() => _service.GetTrackName();

            public override string GetAlbumName() => _service.GetAlbumName();

            public override string GetPath() => _service.GetPath();

            public override int GetShuffleMode() => _service.ShuffleMode;

            public override int RemoveTracks(int first, int last) => _service.RemoveTracks(first, last);

            public override int RemoveTrack(long id) => _service.RemoveTrack(id);

            public override bool RemoveTrackAtPosition(long id, int position) => _service.RemoveTrackAtPosition(id, position);

            public override int GetRepeatMode() => _service.RepeatMode;

            public override int GetMediaMountedCount() => _service.MediaMountedCount;

            public override int GetAudioSessionId() => _service.GetAudioSessionId();
        }

        private class UnMountReceiver : BroadcastReceiver
        {
            public Action<Context, Intent> DelegateAction { get; set; }
            public override void OnReceive(Context context, Intent intent)
            {
                DelegateAction(context, intent);
            }
        }

        private class IntentReceiver : BroadcastReceiver
        {
            public Action<Intent> Action { private get; set; }
            public override void OnReceive(Context context, Intent intent)
            {
                Action(intent);
            }
        }

        private class MusicPlayerHandler : Handler
        {
            private readonly MusicService _service;
            private float _curVolume = 1f;
            private readonly object _padlock = new object();

            public MusicPlayerHandler(MusicService service, Looper looper) : base(looper)
            {
                _service = service;
            }

            public override void HandleMessage(Message msg)
            {
                if (_service is null) return;
                lock (_padlock)
                {
                    switch (msg.What)
                    {
                        case FADEDOWN:
                        {
                            _curVolume -= 0.05f;
                            if (_curVolume > 0.2f)
                                SendEmptyMessageDelayed(FADEDOWN, 10);
                            else
                                _curVolume = 0.2f;
                            _service._player.SetVolume(_curVolume);
                            break;
                        }
                        case FADEUP:
                        {
                            _curVolume += 0.01f;
                            if (_curVolume < 1f)
                                SendEmptyMessageDelayed(FADEUP, 10);
                            else
                                _curVolume = 1f;
                            _service._player.SetVolume(_curVolume);
                            break;
                        }
                        case SERVER_DIED:
                        {
                            if (_service.IsPlaying)
                            {
                                var err = (TrackErrorInfo)msg.Obj;
                                _service.SendErrorMessage(err.Info);
                                _service.RemoveTrack(err.Id);
                            }
                            else
                            {
                                _service.OpenCurrentAndNext();
                            }
                            break;
                        }
                        case TRACK_WENT_TO_NEXT:
                        {
                            _service.SetAndRecordPlayPos(_service._nextPlayPos);
                            _service.SetNextTrack();
                            if (_service._cursor != null)
                            {
                                _service._cursor.Close();
                                _service._cursor = null;
                            }
                            _service.UpdateCursor(_service._playlist[_service._playPos].Id);
                            _service.NotifyChange(META_CHANGED);
                            _service.UpdateNotification();
                            break;
                        }
                        case TRACK_ENDED:
                        {
                            if (_service.RepeatMode == REPEAT_CURRENT)
                            {
                                _service.Seek(0);
                                _service.Play();
                            }
                            else
                            {
                                _service.GoToNext(false);
                            }
                            break;
                        }
                        case RELEASE_WAKELOCK:
                            _service._wakeLock.Release();
                            break;
                        case FOCUSCHANGE:
                        {
                            switch ((AudioFocus)msg.Arg1)
                            {
                                case AudioFocus.Loss:
                                case AudioFocus.LossTransient:
                                {
                                    if (_service.IsPlaying)
                                    {
                                        _service._pausedByTransientLossOfFocus = (AudioFocus)msg.Arg1 == AudioFocus.LossTransient;
                                    }
                                    _service.Pause();
                                    break;
                                }
                                case AudioFocus.LossTransientCanDuck:
                                {
                                    RemoveMessages(FADEUP);
                                    SendEmptyMessage(FADEDOWN);
                                    break;
                                }
                                case AudioFocus.Gain:
                                {
                                    if (!_service.IsPlaying && _service._pausedByTransientLossOfFocus)
                                    {
                                        _service._pausedByTransientLossOfFocus = false;
                                        _curVolume = 0f;
                                        _service._player.SetVolume(_curVolume);
                                        _service.Play();
                                    }
                                    else
                                    {
                                        RemoveMessages(FADEDOWN);
                                        SendEmptyMessage(FADEUP);
                                    }
                                    break;
                                }
                                default:
                                    break;
                            }
                            break;
                        }
                        default:
                            break;
                    }
                }
                
            }
        }

        private class TrackErrorInfo : Java.Lang.Object
        {
            public long Id { get; }
            public string Info { get; }

            public TrackErrorInfo(long id, string info)
            {
                Id = id;
                Info = info;
            }
        }

        private class MediaStoreObserver : ContentObserver, IRunnable
        {
            private readonly Handler _handler;
            public Action Refresh { private get; set; }
            public MediaStoreObserver(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }

            public MediaStoreObserver(Handler handler) : base(handler)
            {
                _handler = handler;
            }

            public override void OnChange(bool selfChange)
            {
                _handler.RemoveCallbacks(this);
                _handler.PostDelayed(this, 500);
            }

            public void Run()
            {
                Refresh();
            }
        }

        #endregion
    }    

    public class MusicBinder : Binder
    {
        public MusicService MusicService { get; }

        public MusicBinder(MusicService ms)
        {
            MusicService = ms;
        }
    }
}