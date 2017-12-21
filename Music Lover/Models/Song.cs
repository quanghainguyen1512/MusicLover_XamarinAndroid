using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Music_Lover.Models
{
    public class Song
    {
        public long Id { get; set; }
        public long AlbumId { get; set; }
        public string AlbumName { get; set; }
        public long  ArtistId { get; set; }
        public string ArtistName { get; set; }
        public int Duration { get; set; }
        public string Title { get; set; }
        public int TrackNumber { get; set; }
    }
}