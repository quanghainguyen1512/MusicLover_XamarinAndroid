using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Music_Lover.Widgets
{
    public class DragRecycler : RecyclerView.ItemDecoration, RecyclerView.IOnItemTouchListener
    {
        public bool OnInterceptTouchEvent(RecyclerView rv, MotionEvent e)
        {
            throw new NotImplementedException();
        }

        public void OnRequestDisallowInterceptTouchEvent(bool disallowIntercept)
        {
            throw new NotImplementedException();
        }

        public void OnTouchEvent(RecyclerView rv, MotionEvent e)
        {
            throw new NotImplementedException();
        }
    }
}