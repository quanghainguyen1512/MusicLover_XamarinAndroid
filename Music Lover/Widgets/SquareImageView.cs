using System;
using Android.Content;
using Android.Runtime;
using Android.Util;

namespace Music_Lover.Widgets
{
    public class SquareImageView : Android.Support.V7.Widget.AppCompatImageView
    {
        protected SquareImageView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public SquareImageView(Context context) : base(context)
        {
        }

        public SquareImageView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public SquareImageView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            SetMeasuredDimension(MeasuredWidth, MeasuredWidth);
        }
    }
}