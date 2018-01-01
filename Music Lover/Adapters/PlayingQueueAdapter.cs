using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Com.Nostra13.Universalimageloader.Core;
using Music_Lover.Dialogs;
using Music_Lover.Models;
using Music_Lover.Utils;
using Music_Lover.Widgets;

namespace Music_Lover.Adapters
{
    public class PlayingQueueAdapter : RecyclerView.Adapter
    {
        public int CurrentPosition { get; private set; }
        private List<Song> _songs;
        private Activity _context;

        public PlayingQueueAdapter(Activity context, List<Song> songs)
        {
            CurrentPosition = MusicPlayer.GetQueuePosition();
            _songs = songs;
            _context = context;
        }

        public override int ItemCount => _songs != null ? _songs.Count : 0;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (!(holder is QueueItemHolder itemHolder))
                return;
            var local = _songs[position];
            itemHolder.Title.Text = local.Title;
            itemHolder.Artist.Text = local.ArtistName;

            if (MusicPlayer.GetCurrentAudioId() == local.Id)
            {

                itemHolder.Title.SetTextColor(new Color(MusicUtils.GetAccentColor(_context)));
                if (MusicPlayer.IsPlaying())
                {
                    itemHolder.Visualizer.SetColor(MusicUtils.GetAccentColor(_context));
                    itemHolder.Visualizer.Visibility = ViewStates.Visible;
                }
            }
            else
            {
                itemHolder.Title.SetTextColor(new Color(MusicUtils.GetTextColorPrimary(_context)));
                itemHolder.Visualizer.Visibility = ViewStates.Gone;
            }
            var builder = new DisplayImageOptions.Builder()
                .CacheInMemory(true)
                .ShowImageOnFail(Resource.Drawable.ic_empty_music2)
                .ResetViewBeforeLoading(true)
                .Build();
            ImageLoader.Instance.DisplayImage(MusicUtils.GetAlbumArtUri(local.AlbumId).ToString(), itemHolder.AlbumArt, builder);
            itemHolder.PopupMenu.Click += (s, e) => OnPopupMenuClick(s, e, position);
        }

        private void OnPopupMenuClick(object sender, EventArgs e, int pos)
        {
            var menu = new Android.Widget.PopupMenu(_context, (View)sender);
            menu.MenuItemClick += (s, args) =>
            {
                switch (args.Item.ItemId)
                {
                    case Resource.Id.popup_song_play:
                    {
                        MusicPlayer.PlayAll(_context, GetSongIds(), pos, -1, MusicUtils.SourceTypeId.NA, false);
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
                    case Resource.Id.popup_song_addto_playlist:
                    {
                        // TODO: Create add to dialog
                        AddPlaylistDialog.GetInstance(_songs[pos]).Show(((AppCompatActivity)_context).SupportFragmentManager, "Add to playlist"); // not finish
                        break;
                    }
                    case Resource.Id.popup_song_delete:
                    {
                        MusicUtils.ShowDeleteSongDialog(_context, new long[] { _songs[pos].Id });
                        break;
                    }
                }
            };
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.item_playing_queue, null);
            return new QueueItemHolder(view)
            {
                OnClickAction = (adapterPos) =>
                {
                    var handler = new Handler();
                    handler.PostDelayed(() =>
                    {
                        NotifyItemChanged(CurrentPosition);
                        NotifyItemChanged(adapterPos);
                    }, 100);
                }
            };
        }

        private long[] GetSongIds()
        {
            return _songs.Select(s => s.Id).ToArray();
        }


        private class QueueItemHolder : RecyclerView.ViewHolder, View.IOnClickListener
        {
            public Action<int> OnClickAction { private get; set; }
            public TextView Title { get; set; } 
            public TextView Artist { get; set; }
            public ImageView AlbumArt { get; set; }
            public ImageView Reorder { get; set; }
            public ImageView PopupMenu { get; set; }
            public MusicVisualizer Visualizer { get; set; }

            public QueueItemHolder(View itemView) : base(itemView)
            {
                Title = itemView.FindViewById<TextView>(Resource.Id.song_title);
                Artist = itemView.FindViewById<TextView>(Resource.Id.song_artist);
                AlbumArt = itemView.FindViewById<ImageView>(Resource.Id.albumArt);
                PopupMenu = itemView.FindViewById<ImageView>(Resource.Id.popup_menu);
                Reorder = itemView.FindViewById<ImageView>(Resource.Id.reorder);
                Visualizer = itemView.FindViewById<MusicVisualizer>(Resource.Id.visualizer);

                itemView.SetOnClickListener(this);
            }

            public void OnClick(View v)
            {
                OnClickAction(AdapterPosition);
            }
        }
    }
}