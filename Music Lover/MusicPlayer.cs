using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Music_Lover.Helpers;
using Music_Lover.Loader;
using static Android.Provider.MediaStore.Audio;
using Music_Lover.Utils;
using Android.Database;

namespace Music_Lover
{
    public class MusicPlayer
    {
        public static IMusicService Service { get; private set; }
        private static Dictionary<Context, ServiceConnection> _connectionMap = new Dictionary<Context, ServiceConnection>();
        private static long[] _emptyList = new long[0];
        private static ContentValues[] _valuesCache;

        public static ServiceToken BindToService(Context context, IServiceConnection connection)
        {
            var activity = ((Activity) context).Parent;
            if (activity is null)
                activity = (Activity) context;

            var wrapper = new ContextWrapper(activity);
            wrapper.StartService(new Intent(wrapper, typeof(MusicService)));
            var conn = new ServiceConnection(connection);
            if (wrapper.BindService(new Intent(wrapper, typeof(MusicService)), conn, 0))
            {
                _connectionMap.Add(wrapper, conn);
                return new ServiceToken(wrapper);
            }
            return null;
        }

        public static void UnbindFromService(ServiceToken token)
        {
            if (token is null)
                return;

            var wrapper = token.Wrapper;
            var conn = _connectionMap[wrapper];
            if (conn is null)
                return;
            _connectionMap.Remove(wrapper);
            wrapper.UnbindService(conn);
            if (_connectionMap.Count == 0)
                Service = null;

        }

        public static bool IsPlaybackConnected => Service != null;

        public static void Next()
        {
            try
            {
                if (IsPlaybackConnected)
                    Service.Next();
            }
            catch { }
        }

        public static void Prev(Context context, bool force)
        {
            var prevIntent = new Intent(context, typeof(MusicService));
            if (force)
                prevIntent.SetAction(MusicService.PREVIOUS_FORCE_ACTION);
            else
                prevIntent.SetAction(MusicService.PREVIOUS_ACTION);
            context.StartService(prevIntent);
        }

        public static void PlayPause()
        {
            try
            {
                if (IsPlaybackConnected)
                {
                    if (Service.IsPlaying())
                        Service.Pause();
                    else
                        Service.Play();
                }
            }
            catch { }
        }

        public static void InvokeRepeat()
        {
            try
            {
                if (!IsPlaybackConnected)
                    return;
                switch (Service.GetRepeatMode())
                {
                    case MusicService.REPEAT_NONE:
                    {
                        Service.SetRepeatMode(MusicService.REPEAT_ALL);
                        break;
                    }
                    case MusicService.REPEAT_ALL:
                    {
                        Service.SetRepeatMode(MusicService.REPEAT_CURRENT);
                        break;
                    }
                    default:
                    {
                        Service.SetRepeatMode(MusicService.REPEAT_NONE);
                        break;
                    }
                }
            }
            catch { }
        }

        public static void InvokeShuffle()
        {
            try
            {
                if (!IsPlaybackConnected)
                    return;
                switch (Service.GetShuffleMode())
                {
                    case MusicService.SHUFFLE_NONE:
                    {
                        Service.SetShuffleMode(MusicService.SHUFFLE_NORMAL);
                        break;
                    }
                    case MusicService.SHUFFLE_NORMAL:
                    {
                        Service.SetShuffleMode(MusicService.SHUFFLE_NONE);
                        break;
                    }
                    case MusicService.SHUFFLE_AUTO:
                    {
                        Service.SetShuffleMode(MusicService.SHUFFLE_NONE);
                        break;
                    }
                    default:
                        break;
                }
            }
            catch { }
        }

        public static bool IsPlaying()
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    return Service.IsPlaying();
                }
                catch { }
            }
            return false;
        }

        public static int GetShuffleMode()
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    return Service.GetShuffleMode();
                }
                catch { }
            }
            return 0;
        }

        public static int GetRepeatMode()
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    return Service.GetRepeatMode();
                }
                catch { }
            }
            return 0;
        }

        public static void SetShuffleMode(int shuffleMode)
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    Service.SetShuffleMode(shuffleMode);
                }
                catch { }
            }
        }

        public static void SetRepeatMode(int repeatMode)
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    Service.SetRepeatMode(repeatMode);
                }
                catch { }
            }
        }

        public static string GetTrackName()
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    return Service.GetTrackName();
                }
                catch { }
            }
            return "";
        }

        public static string GetArtistName()
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    return Service.GetArtistName();
                }
                catch { }
            }
            return "";
        }

        public static string GetAlbumName()
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    return Service.GetAlbumName();
                }
                catch { }
            }
            return "";
        }

        public static long GetCurrentAlbumId()
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    return Service.GetAlbumId();
                }
                catch { }
            }
            return -1;
        }

        public static long GetCurrentAudioId()
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    return Service.GetAudioId();
                }
                catch { }
            }
            return -1;
        }

        public static long GetNextAudioId()
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    return Service.GetNextAudioId();
                }
                catch { }
            }
            return -1;
        }

        public static long GetAudioSessionId()
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    return Service.GetAudioSessionId();
                }
                catch { }
            }
            return -1;
        }

        public static long[] GetQueue()
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    return Service.GetQueue();
                }
                catch { }
            }
            return new long[0];
        }

        public static int GetQueuePosition()
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    return Service.GetQueuePosition();
                }
                catch { }
            }
            return -1;
        }

        public static void Refresh()
        {
            try
            {
                if (IsPlaybackConnected)
                    Service.Refresh();
            }
            catch { }
        }

        public static int RemoveTrack(long id)
        {
            try
            {
                if (IsPlaybackConnected)
                {
                    return Service.RemoveTrack(id);
                }
            }
            catch { }
            return 0;
        }

        public static void MoveItem(int from, int to)
        {
            try
            {
                if (IsPlaybackConnected)
                    Service.MoveQueueItem(from, to);
            }
            catch { }
        }

        public static void PlayAll(Context context, long[] list, int position, long sourceId, Utils.MusicUtils.SourceTypeId sourceType, bool forceShuffle)
        {
            if (list == null || list.Length == 0 || Service == null)
            {
                return;
            }
            try
            {
                if (forceShuffle)
                {
                    Service.SetShuffleMode(MusicService.SHUFFLE_NORMAL);
                }
                long currentId = Service.GetAudioId();
                int currentQueuePosition = GetQueuePosition();
                if (position != -1 && currentQueuePosition == position && currentId == list[position])
                {
                    long[] playlist = GetQueue();
                    if (Equals(list, playlist))
                    {
                        Service.Play();
                        return;
                    }
                }
                if (position < 0)
                {
                    position = 0;
                }
                Service.Open(list, forceShuffle ? -1 : position, sourceId, (int)sourceType);
                Service.Play();
            }
            catch { }
        }

        public static void PlayNext(Context context, long[] list, long sourceId, MusicUtils.SourceTypeId sourceType)
        {
            if (Service == null)
            {
                return;
            }
            try
            {
                Service.Enqueue(list, MusicService.NEXT, sourceId, (int)sourceType);
                Toast.MakeText(context, "Added successfully", ToastLength.Short).Show();
            }
            catch{ }
        }

        public static void ShuffleAll(Context context)
        {
            var cursor = SongLoader.CreateSongCursor(context, null, null);
            var trackList = SongLoader.GetSongListByCursor(cursor);
            var position = 0;
            if (trackList.Length == 0 || Service == null)
            {
                return;
            }
            try
            {
                Service.SetShuffleMode(MusicService.SHUFFLE_NORMAL);
                var mCurrentId = Service.GetAudioId();
                var mCurrentQueuePosition = GetQueuePosition();
                if (position != -1 && mCurrentQueuePosition == position
                        && mCurrentId == trackList[position])
                {
                    var playlist = GetQueue();
                    if (Equals(trackList, playlist))
                    {
                        Service.Play();
                        return;
                    }
                }
                Service.Open(trackList, -1, -1, (int)MusicUtils.SourceTypeId.NA);
                Service.Play();
                cursor.Close();
                cursor = null;
            }
            catch { }
        }

        public static long[] GetSongListByArtist(Context context, long id)
        {
            var projection = new[] { BaseColumns.Id };
            var selection = $"{AudioColumns.ArtistId} = {id} AND {AudioColumns.IsMusic} = 1";
            var cursor = context.ContentResolver.Query(
                    Media.ExternalContentUri, projection, selection, null,
                    AudioColumns.AlbumKey + "," + AudioColumns.Track);
            if (cursor != null)
            {
                var list = SongLoader.GetSongListByCursor(cursor);
                cursor.Close();
                cursor = null;
                return list;
            }
            return new long[0];
        }

        public static long[] GetSongListByAlbum(Context context, long id)
        {
            var projection = new [] { BaseColumns.Id };
            string selection = $"{AudioColumns.AlbumId} = {id} AND {AudioColumns.IsMusic} = 1";
            var cursor = context.ContentResolver.Query(
                    Media.ExternalContentUri, projection, selection, null,
                    AudioColumns.Track + ", " + Media.DefaultSortOrder);
            if (cursor != null)
            {
                long[] mList = SongLoader.GetSongListByCursor(cursor);
                cursor.Close();
                cursor = null;
                return mList;
            }
            return new long[0];
        }

        public static void Seek(long pos)
        {
            if (!IsPlaybackConnected)
                return;
            try
            {
                Service.Seek(pos);
            }
            catch { }
        }

        public static long Position()
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    return Service.Position();
                }
                catch { }
            }
            return -1;
        }

        public static long Duration()
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    return Service.Duration();
                }
                catch { }
            }
            return -1;
        }

        public static void ClearQueue()
        {
            if (!IsPlaybackConnected)
                return;
            try
            {
                Service.RemoveTracks(0, int.MaxValue);
            }
            catch { };
        }

        public static void AddToQueue(Context context, long[] list, long sourceId, MusicUtils.SourceTypeId sourceType)
        {
            if (!IsPlaybackConnected)
            {
                return;
            }
            try
            {
                Service.Enqueue(list, MusicService.LAST, sourceId, (int)sourceType);
                Toast.MakeText(context, "Added Successfully", ToastLength.Short).Show();
            }
            catch { }
        }

        public static void OpenFile(string path)
        {
            if (IsPlaybackConnected)
            {
                try
                {
                    Service.OpenFile(path);
                }
                catch { }
            }
        }

        public static void AddToPlaylist(Context context, long[] ids, long playlistId)
        {
            var size = ids.Length;
            var projection = new[] { "max(play_order)" };
            var uri = Playlists.Members.GetContentUri("external", playlistId);

            ICursor cursor = null;
            var b = 0;
            
            try
            {
                cursor = context.ContentResolver.Query(uri, projection, null, null, null);
                if (cursor != null && cursor.MoveToFirst())
                    b = cursor.GetInt(0) + 1;
            }
            finally
            {
                if (cursor != null)
                    cursor.Close();
            }

            var inserted = 0;
            for (int offset = 0; offset < size; offset += 1000)
            {
                CreateInsertItems(ids, offset, 1000, b);
                inserted += context.ContentResolver.BulkInsert(uri, _valuesCache);
            }
            Toast.MakeText(context, "Added successfully", ToastLength.Short).Show();
        }

        public static long CreatePlaylist(Context context, string name)
        {
            if (string.IsNullOrEmpty(name))
                return -1;

            var resolver = context.ContentResolver;
            var projection = new[] { PlaylistsColumns.Name };
            var selection = $"{PlaylistsColumns.Name} = '{name}'";
            var cursor = resolver.Query(Playlists.ExternalContentUri, projection, selection, null, null);
            if (cursor != null && cursor.Count > 0)
            {
                cursor.Close();
                return -1;
            }

            var vals = new ContentValues(1);
            vals.Put(PlaylistsColumns.Name, name);
            var uri = resolver.Insert(Playlists.ExternalContentUri, vals);
            return long.Parse(uri.LastPathSegment);
        }

        private static void CreateInsertItems(long[] ids, int offset, int len, int b)
        {
            if (offset + len > ids.Length)
                len = ids.Length - offset;

            if (_valuesCache == null || _valuesCache.Length != len)
                _valuesCache = new ContentValues[len];

            for (int i = 0; i < len; i++)
            {
                if (_valuesCache[i] == null)
                    _valuesCache[i] = new ContentValues();
                _valuesCache[i].Put(Playlists.Members.PlayOrder, b + offset + i);
                _valuesCache[i].Put(Playlists.Members.AudioId, ids[offset + 1]);
            }
        }

        #region Nested class

        public class ServiceConnection : Java.Lang.Object, IServiceConnection
        {
            private IServiceConnection _serviceConnection;

            public ServiceConnection(IServiceConnection serviceConnection)
            {
                _serviceConnection = serviceConnection;
            }

            public void OnServiceConnected(ComponentName name, IBinder service)
            {
                Service = IMusicServiceStub.AsInterface(service);
                if (_serviceConnection != null)
                    _serviceConnection.OnServiceConnected(name, service);
            }

            public void OnServiceDisconnected(ComponentName name)
            {
                if (_serviceConnection != null)
                    _serviceConnection.OnServiceDisconnected(name);
                Service = null;
            }
        }

        public class ServiceToken
        {
            public ContextWrapper Wrapper { get; private set; }

            public ServiceToken(ContextWrapper wrapper)
            {
                Wrapper = wrapper;
            }
        }

        #endregion
    }
}