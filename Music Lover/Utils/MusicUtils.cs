using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using Java.Security.Acl;
using Music_Lover.Permissions;
using Android.Net;
using Uri = Android.Net.Uri;
using Android.Graphics.Drawables;
using Android.Graphics;
using Android.Support.V8.Renderscript;
using System.IO;
using Android.Media.Audiofx;
using Android.Support.V7.App;
using Android.Provider;
using Music_Lover.Providers;
using Android.Util;

namespace Music_Lover.Utils
{
    public class MusicUtils
    {

        public enum SourceTypeId
        {
            NA = 0,
            Artist,
            Album,
            Playlist
        }
        public static Uri GetAlbumArtUri(long albumId)
        {
            return ContentUris.WithAppendedId(Uri.Parse("content://media/external/audio/albumart"), albumId);
        }

        public static Drawable CreateBlurredImageFromBitmap(Bitmap bm, Context context, int size)
        {
            var rs = RenderScript.Create(context);
            var options = new BitmapFactory.Options
            {
                InSampleSize = size
            };
            byte[] imageInBytes;
            using (var stream = new MemoryStream())
            {
                bm.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
                imageInBytes = stream.ToArray();
            }

            var blurred = BitmapFactory.DecodeByteArray(imageInBytes, 0, imageInBytes.Length);

            var input = Allocation.CreateFromBitmap(rs, blurred);
            var output = Allocation.CreateTyped(rs, input.Type);

            var script = ScriptIntrinsicBlur.Create(rs, Element.U8_4(rs));
            script.SetRadius(8f);
            script.SetInput(input);
            script.ForEach(output);

            output.CopyTo(blurred);

            return new BitmapDrawable(context.Resources, blurred);
        }

        public static bool HasPanel(Activity activity)
        {
            var pm = activity.PackageManager;
            return pm.ResolveActivity(CreateEffectsIntent(), PackageInfoFlags.MatchDefaultOnly) != null;
        }

        private static Intent CreateEffectsIntent()
        {
            var effects = new Intent(AudioEffect.ActionDisplayAudioEffectControlPanel);
            effects.PutExtra(AudioEffect.ExtraAudioSession, MusicPlayer.GetAudioSessionId());
            return effects;
        }

        public static void RemoveFromPlaylist(AppCompatActivity context, long id, long playlistId)
        {
            var uri = MediaStore.Audio.Playlists.Members.GetContentUri("external", playlistId);
            var r = context.ContentResolver;
            r.Delete(uri, MediaStore.Audio.Playlists.Members.AudioId + " = ?", new[] { id.ToString() });
        }

        public static void ShareSong(Context context, long id)
        {
            var projection = new[]
            {
                BaseColumns.Id,
                MediaStore.MediaColumns.Data,
                MediaStore.Audio.AudioColumns.AlbumId
            };

            var selection = $"{projection[0]} IN ({id})";
            var c = context.ContentResolver.Query(MediaStore.Audio.Media.ExternalContentUri, projection, selection, null, null);
            if (c is null)
                return;
            c.MoveToFirst();
            try
            {
                var share = new Intent(Intent.ActionSend);
                share.SetType("audio/*");
                share.PutExtra(Intent.ExtraStream, Uri.FromFile(new Java.IO.File(c.GetString(1))));
                context.StartActivity(Intent.CreateChooser(share, "Share the song"));
                c.Close();
            }
            catch { }
        }

        public static void ShowDeleteSongDialog(Context context, long[] list)
        {
            var dialog = new Android.Support.V7.App.AlertDialog.Builder(context)
                .SetTitle("Confirm delete")
                .SetMessage("Are you sure you want to delete the file ?")
                .SetPositiveButton("Delete", (s, e) =>
                {
                    DeleteSongFiles(context, list);
                    Toast.MakeText(context, "Deleted !", ToastLength.Short).Show();
                })
                .SetNegativeButton("Cancel", (s, e) => { })
                .Create();
            dialog.Show();
        }

        private static void DeleteSongFiles(Context context, long[] list)
        {
            var projection = new[]
            {
                BaseColumns.Id,
                MediaStore.MediaColumns.Data,
                MediaStore.Audio.AudioColumns.AlbumId
            };
            var strBuilder = new StringBuilder();
            strBuilder.Append($"{projection[0]} IN (");
            for (var i = 0; i < list.Length - 1; i++)
            {
                strBuilder.Append($"{list[i]}, ");
            }
            strBuilder.Append(list[list.Length - 1]);
            var c = context.ContentResolver.Query(MediaStore.Audio.Media.ExternalContentUri, projection, strBuilder.ToString(), null, null);
            if (c != null)
            {
                // remove from current playlist and album art cache
                c.MoveToFirst();
                while (!c.IsAfterLast)
                {
                    var name = c.GetString(1);
                    var file = new Java.IO.File(name);
                    try
                    {
                        if (!file.Delete())
                        {
                            Log.Error("DELETE MUSIC FILE", "DELETE FAILED");
                            return;
                        }
                        c.MoveToNext();
                    }
                    catch
                    {
                        c.MoveToNext();
                        Toast.MakeText(context, "Exception thrown", ToastLength.Short).Show();
                        return;
                    }
                    var id = c.GetLong(0);
                    MusicPlayer.RemoveTrack(id);
                    RecentPlayedStore.GetInstance(context).RemoveSong((int) id);
                }
                
                c.Close();
            }
            //remove from database
            context.ContentResolver.Delete(MediaStore.Audio.Media.ExternalContentUri, strBuilder.ToString(), null);
            Toast.MakeText(context, "Delete successfully", ToastLength.Short).Show();
            MusicPlayer.Refresh();
        }

        public static int GetAccentColor(Context context)
        {
            var val = new TypedValue();
            context.Theme.ResolveAttribute(Resource.Attribute.accentColor, val, true);
            return val.Data;
        }

        public static int GetTextColorPrimary(Context context)
        {
            var val = new TypedValue();
            context.Theme.ResolveAttribute(Resource.Attribute.textColorPrimary, val, true);
            return val.Data;
        }

        public static Color GetSuitableTextColor(int color)
        {
            var luminance = (0.299 * Math.Pow(Color.GetRedComponent(color), 2) 
                            + 0.587 * Math.Pow(Color.GetGreenComponent(color), 2) 
                            + 0.114 * Math.Pow(Color.GetBlueComponent(color), 2)) / 255f;
            return luminance <= 0.5 ? Color.ParseColor("#F7F7F7") : Color.ParseColor("#202020");    
        }
    }
}