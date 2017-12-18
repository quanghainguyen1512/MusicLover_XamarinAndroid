using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Java.Lang;

namespace Music_Lover.Widgets
{
    public sealed class MusicVisualizer : View, IRunnable
    {
        private readonly Random _rand = new Random();
        private readonly Paint _paint = new Paint();

        #region Constructors

        private MusicVisualizer(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer) { }

        public MusicVisualizer(Context context) : this(context, null) { }

        public MusicVisualizer(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            RemoveCallbacks(this);
            Post(this);
        }

        public MusicVisualizer(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr) { }

        public MusicVisualizer(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes) { }

        #endregion

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            _paint.SetStyle(Paint.Style.Fill);

            canvas.DrawRect(GetDimensionInPixel(0), Height - (20 + _rand.Next((int)(Height / 1.5f - 19))),
                GetDimensionInPixel(7), Height, _paint);
            canvas.DrawRect(GetDimensionInPixel(10), Height - (20 + _rand.Next((int)(Height / 1.5f - 19))),
                GetDimensionInPixel(17), Height, _paint);
            canvas.DrawRect(GetDimensionInPixel(20), Height - (20 + _rand.Next((int)(Height / 1.5f - 19))),
                GetDimensionInPixel(27), Height, _paint);
        }

        private int GetDimensionInPixel(int dp) =>
            (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, dp, Resources.DisplayMetrics);

        protected override void OnWindowVisibilityChanged(ViewStates visibility)
        {
            base.OnWindowVisibilityChanged(visibility);
            if (visibility == ViewStates.Visible)
            {
                RemoveCallbacks(this);
                Post(this);
            }
            else if (visibility == ViewStates.Gone)
            {
                RemoveCallbacks(this);
            }
        }

        public void Run()
        {
            PostDelayed(this, 150);
            Invalidate();
        }

        public void SetColor(int color)
        {
            _paint.Color = new Color(color);
            Invalidate();
        }
    }
}