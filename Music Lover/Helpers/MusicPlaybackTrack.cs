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

        private int _id, _sourceId, _sourcePos;
        private Utils.Utils.SourceTypeId _sourceType;

        public MusicPlaybackTrack(int id, int sourceId, Utils.Utils.SourceTypeId sourceType, int sourcePos)
        {
            _id = id;
            _sourceId = sourceId;
            _sourceType = sourceType;
            _sourcePos = sourcePos;
        }

        public MusicPlaybackTrack(Parcel parcel)
        {
            _id = parcel.ReadInt();
            _sourceId = parcel.ReadInt();
            _sourceType = (Utils.Utils.SourceTypeId) parcel.ReadInt();
            _sourcePos = parcel.ReadInt();
        }

        public int DescribeContents()
        {
            return 0;
        }

        public void WriteToParcel(Parcel dest, ParcelableWriteFlags flags)
        {
            dest.WriteInt(_id);
            dest.WriteInt(_sourceId);
            dest.WriteInt((int)_sourceType);
            dest.WriteInt(_sourcePos);
        }

        public override bool Equals(Object obj)
        {
            if (obj is MusicPlaybackTrack mpt)
            {
                return _id == mpt._id && 
                    _sourceId == mpt._sourceId && 
                    _sourceType == mpt._sourceType && 
                    _sourcePos == mpt._sourcePos;
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