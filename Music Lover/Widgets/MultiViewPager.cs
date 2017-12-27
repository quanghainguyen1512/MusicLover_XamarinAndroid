using System;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Java.Lang;
using Math = System.Math;

namespace Music_Lover.Widgets
{
    public class MultiViewPager : ViewPager
    {
        private Point _size, _maxSize;
        private int _maxWidth = -1;
        private int _maxHeight = -1;
        private int _matchWidthChildResId;
        private bool _needsMeasurePage;
        protected MultiViewPager(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public MultiViewPager(Context context) : base(context)
        {
            _size = new Point();
            _maxSize = new Point();
        }

        public MultiViewPager(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Init(context, attrs);
            _size = new Point();
            _maxSize = new Point();
        }
        private static void ConstrainTo(Point size, Point maxSize)
        {
            if (maxSize.X >= 0)
            {
                if (size.X > maxSize.X)
                {
                    size.X = maxSize.X;
                }
            }
            if (maxSize.Y >= 0)
            {
                if (size.Y > maxSize.Y)
                {
                    size.Y = maxSize.Y;
                }
            }
        }
        private void Init(Context context, IAttributeSet attrs)
        {
            SetClipChildren(false);
            var typedArray = context.ObtainStyledAttributes(attrs, Resource.Styleable.MultiViewPager);
            SetMaxWidth(typedArray.GetDimensionPixelSize(Resource.Styleable.MultiViewPager_android_maxWidth, -1));
            SetMaxHeight(typedArray.GetDimensionPixelSize(Resource.Styleable.MultiViewPager_android_maxHeight, -1));
            SetMaxChildWidth(typedArray.GetResourceId(Resource.Styleable.MultiViewPager_matchChildWidth, 0));
            typedArray.Recycle();
        }

        public void SetMaxWidth(int width)
        {
            _maxWidth = width;
        }

        public void SetMaxHeight(int height)
        {
            _maxHeight = height;
        }

        public void SetMaxChildWidth(int matchChildWidthResId)
        {
            if (_matchWidthChildResId == matchChildWidthResId) return;
            _matchWidthChildResId = matchChildWidthResId;
            _needsMeasurePage = true;
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            _needsMeasurePage = true;
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            _size.Set(MeasureSpec.GetSize(widthMeasureSpec), MeasureSpec.GetSize(heightMeasureSpec));
            if (_maxWidth >= 0 || _maxHeight >= 0)
            {
                _maxSize.Set(_maxWidth, _maxHeight);
                ConstrainTo(_size, _maxSize);
                widthMeasureSpec = MeasureSpec.MakeMeasureSpec(_size.X, MeasureSpecMode.Exactly);
                heightMeasureSpec = MeasureSpec.MakeMeasureSpec(_size.Y, MeasureSpecMode.Exactly);
            }
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

        }

        protected void OnMeasurePage(int widthMeasureSpec, int heightMeasureSpec)
        {
            if (!_needsMeasurePage)
                return;

            if (_matchWidthChildResId == 0)
                _needsMeasurePage = true;
            else if (this.ChildCount > 0)
            {
                var child = GetChildAt(0);
                child.Measure(widthMeasureSpec, heightMeasureSpec);
                var pageWidth = child.MeasuredWidth;
                var match = child.FindViewById(_matchWidthChildResId);
                if (match is null)
                {
                    throw new NullPointerException(
                        "MatchWithChildResId did not find that ID in the first fragment of the ViewPager; "
                        + "is that view defined in the child view's layout? Note that MultiViewPager "
                        + "only measures the child for index 0.");
                }
                var childWidth = match.MeasuredWidth;
                if (childWidth > 0)
                {
                    _needsMeasurePage = false;
                    PageMargin = childWidth - pageWidth;
                    var offScreen = (int)Math.Ceiling((float)pageWidth / childWidth) + 1;
                    OffscreenPageLimit = offScreen;
                    RequestLayout();
                }
            }
        }
    }
}