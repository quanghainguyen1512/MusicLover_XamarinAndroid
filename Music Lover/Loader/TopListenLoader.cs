using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Compilation;
using System.Web.WebSockets;
using Android.App;
using Android.Content;
using Android.Database;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace Music_Lover.Loader
{
    public class TopListenLoader : SongLoader
    {
        private static int LIST_CAPACITY = 20;
        private static QueryType _queryType;
        private Context _context;

        public TopListenLoader(Context context, QueryType type)
        {
            _context = context;
            _queryType = type;
        }

//        public static ICursor GetCursor()
//        {
//
//        }

        public class SortedCursor : AbstractCursor
        {
            private ICursor _cursor;
            private List<int> _orderedPos = new List<int>();
            public List<long> MissingIds { get; }
            private Dictionary<long, int> _cursorPos = new Dictionary<long, int>();
            private List<object> _extraData = new List<object>();

            public SortedCursor(ICursor cursor, long[] order, string colName, List<object> extraData)
            {
                _cursor = cursor;
                MissingIds = BuildCursorPositions(order, colName, extraData);
            }

            private List<long> BuildCursorPositions(long[] order, string colName, List<object> extraData)
            {
                var missingIds = new List<long>();
                
                _orderedPos = new List<int>(_cursor.Count);
                var idPos = _cursor.GetColumnIndex(colName);

                if (_cursor.MoveToFirst())
                {
                    do
                    {
                        _cursorPos.Add(_cursor.GetLong(idPos), _cursor.Position);
                    } while (_cursor.MoveToNext());

                    for (var i = 0; order != null && i < order.Length; i++)
                    {
                        var id = order[i];
                        if (_cursorPos.ContainsKey(id))
                        {
                            _orderedPos.Add(_cursorPos[id]);
                            _cursorPos.Remove(id);
                            if (extraData != null)
                            {
                                _extraData.Add(extraData[i]);
                            }
                        }
                    }

                    _cursor.MoveToFirst();
                }

                return missingIds;
            }

            public override void Close()
            {
                _cursor.Close();
                base.Close();
            }

            public override string[] GetColumnNames()
            {
                throw new NotImplementedException();
            }

            public override double GetDouble(int column)
            {
                throw new NotImplementedException();
            }

            public override float GetFloat(int column)
            {
                throw new NotImplementedException();
            }

            public override int GetInt(int column)
            {
                throw new NotImplementedException();
            }

            public override long GetLong(int column)
            {
                throw new NotImplementedException();
            }

            public override short GetShort(int column)
            {
                throw new NotImplementedException();
            }

            public override string GetString(int column)
            {
                throw new NotImplementedException();
            }

            public override bool IsNull(int column)
            {
                throw new NotImplementedException();
            }

            public override int Count => _orderedPos.Count;
        }

        public enum QueryType
        {
            TopTracks,
            RecentSongs
        }
    }
}