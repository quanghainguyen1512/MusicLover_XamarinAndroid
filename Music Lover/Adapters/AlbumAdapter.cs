using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Com.Nostra13.Universalimageloader.Core;
using Com.Nostra13.Universalimageloader.Core.Assist;
using Com.Nostra13.Universalimageloader.Core.Display;
using Com.Nostra13.Universalimageloader.Core.Listener;
using Music_Lover.Models;
using Music_Lover.Utils;

namespace Music_Lover.Adapters
{
    public class AlbumAdapter : RecyclerView.Adapter, Palette.IPaletteAsyncListener
    {
        private List<Album> _albums;
        private Activity _context;
        private bool _isGrid;
        private AlbumItemHolder _itemHolder;

        public AlbumAdapter(Activity context, List<Album> albums)
        {
            _albums = albums;
            _context = context;
        }

        public override int ItemCount => _albums != null ? _albums.Count : 0;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (!(holder is AlbumItemHolder itemHolder))
                return;
            var localAlbum = _albums[position];

            _itemHolder = itemHolder;
            itemHolder.Title.Text = localAlbum.Title;
            itemHolder.Artist.Text = localAlbum.ArtistName;

            ImageLoader.Instance.DisplayImage(MusicUtils.GetAlbumArtUri(localAlbum.Id).ToString(), itemHolder.AlbumArt,
                new DisplayImageOptions.Builder().CacheInMemory(true)
                .ShowImageOnFail(Resource.Drawable.ic_empty_music2)
                .ResetViewBeforeLoading(true)
                .Displayer(new FadeInBitmapDisplayer(400))
                .Build(), 
                new ImageLoadingListener()
                {
                    LoadingComplete = (loadedImage) =>
                    {
                        if (_isGrid)
                            new Palette.Builder(loadedImage).Generate(this);
                    },
                    LoadingFailed = () =>
                    {
                        if (_isGrid)
                        {
                            itemHolder.Footer.SetBackgroundColor(Color.White);
                            if (_context != null)
                            {
                                var textColor = new Color(MusicUtils.GetTextColorPrimary(_context));
                                itemHolder.Title.SetTextColor(textColor);
                                itemHolder.Artist.SetTextColor(textColor);
                            }
                        }
                    }
                }
            );
        }

        public void UpdateData(List<Album> list)
        {
            _albums = list;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var view = _isGrid
                ? LayoutInflater.From(parent.Context).Inflate(Resource.Layout.item_album_grid, null)
                : LayoutInflater.From(parent.Context).Inflate(Resource.Layout.item_album_list, null);
            return new AlbumItemHolder(view)
            {
                OnClickAction = (albumArt, adapterPos) =>
                {
                    NavigationUtils.NavigateToAlbum(_context, _albums[adapterPos].Id, new KeyValuePair<string, View>($"transition_album_art{adapterPos}", albumArt));
                }
            };
        }

        public void OnGenerated(Palette palette)
        {
            var swatch = palette.VibrantSwatch;
            Color textColor = Color.White;
            Color backgoundColor = Color.Black;
            if (swatch != null)
            {
                backgoundColor = new Color(swatch.Rgb);
            }
            else
            {
                swatch = palette.MutedSwatch;
                backgoundColor = new Color(swatch.Rgb);
            }
            _itemHolder.Footer.SetBackgroundColor(backgoundColor);
            textColor = MusicUtils.GetSuitableTextColor(backgoundColor);
            _itemHolder.Title.SetTextColor(textColor);
            _itemHolder.Artist.SetTextColor(textColor);
        }

        private class AlbumItemHolder : RecyclerView.ViewHolder, View.IOnClickListener
        {
            public TextView Title { get; set; }
            public TextView Artist { get; set; }
            public ImageView AlbumArt { get; set; }
            public View Footer { get; set; }

            public Action<ImageView, int> OnClickAction { private get; set; }

            public AlbumItemHolder(View itemView) : base(itemView)
            {
                Title = itemView.FindViewById<TextView>(Resource.Id.album_title);
                Artist = itemView.FindViewById<TextView>(Resource.Id.album_artist);
                AlbumArt = itemView.FindViewById<ImageView>(Resource.Id.album_art);
                Footer = itemView.FindViewById(Resource.Id.footer);

                itemView.SetOnClickListener(this);
            }

            public void OnClick(View v)
            {
                OnClickAction(AlbumArt, AdapterPosition);
            }
        }
        
        private class ImageLoadingListener : SimpleImageLoadingListener
        {
            public Action<Bitmap> LoadingComplete { get; set; }
            public Action LoadingFailed { get; set; }

            public override void OnLoadingComplete(string p0, View p1, Bitmap p2)
            {
                LoadingComplete(p2);
            }
            public override void OnLoadingFailed(string p0, View p1, FailReason p2)
            {
                LoadingFailed();
            }
        }
    }
}