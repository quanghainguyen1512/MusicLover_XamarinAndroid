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
using Object = Java.Lang.Object;

namespace Music_Lover.Helpers
{
    public class MusicPlaybackTrack : Java.Lang.Object, IParcelable
    {
        public static MyParcelableCreator InitCreator()
        {
            return new MyParcelableCreator();
        }

        public long Id { get; set; }
        public long SourceId { get; set; }
        public int SourcePos { get; set; }
        public Utils.Utils.SourceTypeId SourceType { get; set; }

        public MusicPlaybackTrack(long id, long sourceId, Utils.Utils.SourceTypeId sourceType, int sourcePos)
        {
            Id = id;
            SourceId = sourceId;
            SourceType = sourceType;
            SourcePos = sourcePos;
        }

        public MusicPlaybackTrack(Parcel parcel)
        {
            Id = parcel.ReadInt();
            SourceId = parcel.ReadInt();
            SourceType = (Utils.Utils.SourceTypeId) parcel.ReadInt();
            SourcePos = parcel.ReadInt();
        }

        public int DescribeContents()
        {
            return 0;
        }

        public void WriteToParcel(Parcel dest, ParcelableWriteFlags flags)
        {
            dest.WriteLong(Id);
            dest.WriteLong(SourceId);
            dest.WriteInt((int)SourceType);
            dest.WriteInt(SourcePos);
        }

        public override bool Equals(Object obj)
        {
            if (obj is MusicPlaybackTrack mpt)
            {
                return Id == mpt.Id && 
                    SourceId == mpt.SourceId && 
                    SourceType == mpt.SourceType && 
                    SourcePos == mpt.SourcePos;
            }
            return base.Equals(obj);
        }
    }
    public class MyParcelableCreator : Object, IParcelableCreator
    {
        public Object CreateFromParcel(Parcel source)
        {
            return new MusicPlaybackTrack(source);
        }

        public Object[] NewArray(int size)
        {
            return new Object[size];
        }
    }
}