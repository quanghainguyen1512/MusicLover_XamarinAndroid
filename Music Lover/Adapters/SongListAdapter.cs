using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Com.Nostra13.Universalimageloader.Core;
using Music_Lover.Dialogs;
using Music_Lover.Models;
using Music_Lover.Utils;
using Music_Lover.Widgets;
using PopupMenu = Android.Widget.PopupMenu;
using Uri = Android.Net.Uri;

namespace Music_Lover.Adapters
{
    public class SongListAdapter : RecyclerView.Adapter, IBubbleTextGetter
    {
        private int _curPlayingPos;
        private List<Song> _songs;
        private AppCompatActivity _context;
        private long[] _songIds;
        private bool _isPlaylist = false;
        private int _lastPos = -1;
        private long _playlistId;

        public override int ItemCount => _songs?.Count ?? 0;

        public SongListAdapter(AppCompatActivity activity, IEnumerable<Song> songs, bool isPlaylist)
        {
            _songs = songs.ToList();
            _context = activity;
            _isPlaylist = isPlaylist;
            _songIds = GetSongIds();
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var layoutInflater = LayoutInflater.From(parent.Context);
            var view = _isPlaylist
                ? layoutInflater.Inflate(Resource.Layout.item_song_playlist, null)
                : layoutInflater.Inflate(Resource.Layout.item_song, null);
            return new ItemHolder(view)
            {
                OnClickAction = (adapterPos) =>
                {
                    MusicPlayer.PlayAll(_context, _songIds, adapterPos, -1, MusicUtils.SourceTypeId.NA, false);
                    var h = new Handler();
                    h.PostDelayed(() =>
                    {
                        NotifyItemChanged(_curPlayingPos);
                        NotifyItemChanged(adapterPos);
                    }, 50);
                }
            };
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var localSong = _songs[position];
            if (!(holder is ItemHolder itemHolder))
                return;
            itemHolder.Title.Text = localSong.Title;
            itemHolder.Artist.Text = localSong.ArtistName;

            var builder = new DisplayImageOptions.Builder().CacheInMemory(true).ShowImageOnFail(Resource.Drawable.ic_empty_music2).ResetViewBeforeLoading(true).Build();
            ImageLoader.Instance.DisplayImage(MusicUtils.GetAlbumArtUri(localSong.AlbumId).ToString(), itemHolder.AlbumArt, builder);
            var color = new Color(MusicUtils.GetAccentColor(_context));
            if (MusicPlayer.GetCurrentAudioId() == localSong.Id)
            {
                itemHolder.Title.SetTextColor(color);
                if (MusicPlayer.IsPlaying())
                {
                    itemHolder.Visualizer.SetColor(color);
                    itemHolder.Visualizer.Visibility = ViewStates.Visible;
                }
            }
            else
            {
                if (_isPlaylist)
                {
                    itemHolder.Title.SetTextColor(Color.White);
                }
                else
                {
                    itemHolder.Title.SetTextColor(new Color(MusicUtils.GetTextColorPrimary(_context)));
                }
                itemHolder.Visualizer.Visibility = ViewStates.Gone;
            }

            SetPopupMenuListener(itemHolder, position);
        }

        public string GetTextToShowInBubble(int pos)
        {
            if (_songs == null || _songs.Count == 0)
                return "";

            var c = _songs[pos].Title[0];

            return char.IsDigit(c) ? "#" : c.ToString();
        }

        public void UpdateData(List<Song> songs)
        {
            _songs = songs;
            _songIds = (from song in songs select song.Id).ToArray();
        }

        private void SetAnimation(View view, int pos)
        {
            if (pos <= _lastPos) return;

            var anim = AnimationUtils.LoadAnimation(_context, Resource.Animation.abc_slide_in_bottom);
            view.StartAnimation(anim);
            _lastPos = pos;
        }

        private long[] GetSongIds()
        {
            var result = new long[ItemCount];
            for (var i = 0; i < ItemCount; i++)
            {
                result[i] = _songs[i].Id;
            }

            return result;
        }

        #region Handle Popup Menu

        private void SetPopupMenuListener(RecyclerView.ViewHolder holder, int pos)
        {
            if (!(holder is ItemHolder itemHolder))
                return;

            itemHolder.PopupMenu.Click += (s, e) => OnClick((View)s, pos);
        }
        // Onclick Pop Up menu
        public void OnClick(View v, int pos)
        {
            var menu = new PopupMenu(_context, v);

            menu.MenuItemClick += (s, arg) =>
            {
                switch (arg.Item.ItemId)
                {
                    case Resource.Id.popup_song_remove_playlist:
                    {
                        MusicUtils.RemoveFromPlaylist(_context, _songs[pos].Id, _playlistId);
                        _songs.RemoveAt(pos);
                        NotifyItemRemoved(pos);
                        break;
                    }
                    case Resource.Id.popup_song_play:
                    {
                        MusicPlayer.PlayAll(_context, _songIds, pos, -1, MusicUtils.SourceTypeId.NA, false);
                        break;
                    }
                    case Resource.Id.popup_song_play_next:
                    {
                        MusicPlayer.PlayNext(_context, new long[] { _songs[pos].Id }, -1, MusicUtils.SourceTypeId.NA);
                        break;
                    }
                    case Resource.Id.popup_song_goto_album:
                    {
                        NavigationUtils.GoToAlbum(_context, _songs[pos].AlbumId);
                        break;
                    }
                    case Resource.Id.popup_song_goto_artist:
                    {
                        NavigationUtils.GoToArtist(_context, _songs[pos].ArtistId);
                        break;
                    }
                    case Resource.Id.popup_song_addto_queue:
                    {
                        MusicPlayer.AddToQueue(_context, new long[] { _songs[pos].Id }, -1, MusicUtils.SourceTypeId.NA);
                        break;
                    }
                    case Resource.Id.popup_song_addto_playlist:
                    {
                        // TODO: Create add to dialog
                        AddPlaylistDialog.GetInstance(_songs[pos]).Show(_context.SupportFragmentManager, "Add to playlist"); // not finish
                        break;
                    }
                    case Resource.Id.popup_song_share:
                    {
                        MusicUtils.ShareSong(_context, _songs[pos].Id);
                        break;
                    }
                    case Resource.Id.popup_song_delete:
                    {
                        MusicUtils.ShowDeleteSongDialog(_context, new long[] { _songs[pos].Id });
                        break;
                    }
                    default:
                        break;
                }
            };
            menu.Inflate(Resource.Menu.popup_song);
            menu.Show();
            if (_isPlaylist)
                menu.Menu.FindItem(Resource.Id.popup_song_remove_playlist).SetVisible(true);
        }

        #endregion

        public class ItemHolder : RecyclerView.ViewHolder, View.IOnClickListener
        {
            public TextView Title { get; set; }
            public TextView Artist { get; set; }
            public ImageView AlbumArt { get; set; }
            public MusicVisualizer Visualizer { get; set; } 
            public ImageView PopupMenu { get; }

            public Action<int> OnClickAction { get; set; }

            public ItemHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

            public ItemHolder(View itemView) : base(itemView)
            {
                Title = itemView.FindViewById<TextView>(Resource.Id.song_title);
                Artist = itemView.FindViewById<TextView>(Resource.Id.song_artist);
                Visualizer = itemView.FindViewById<MusicVisualizer>(Resource.Id.visualizer);
                AlbumArt = itemView.FindViewById<ImageView>(Resource.Id.albumArt);
                PopupMenu = itemView.FindViewById<ImageView>(Resource.Id.popup_menu);

                itemView.SetOnClickListener(this);
            }

            public void DisplayImage(long albumId)
            {
                ImageLoader.Instance.DisplayImage(
                    ContentUris.WithAppendedId(Uri.Parse("content://media/external/audio/albumart"), albumId)
                        .ToString(), AlbumArt,
                    new DisplayImageOptions.Builder().CacheInMemory(true).ShowImageOnFail(Resource.Drawable.ic_empty_music2)
                        .ResetViewBeforeLoading(true).Build());
            }

            public void OnClick(View v)
            {
                var handler = new Handler();
                handler.PostDelayed(() => OnClickAction(AdapterPosition), 100);
            }
        }

    }
}