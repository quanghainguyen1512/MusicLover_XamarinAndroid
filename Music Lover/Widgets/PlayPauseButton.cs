using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace Music_Lover.Widgets
{
    public class PlayPauseButton : View, ValueAnimator.IAnimatorUpdateListener
    {
        private static float SQRT_3 = (float)System.Math.Sqrt(3);
        private static int SPEED = 1;
        private MyPoint _point;
        private Paint _paint;
        private Path _leftPath;
        private Path _rightPath;
        private ValueAnimator _centerEdgeAnimator;
        private ValueAnimator _leftEdgeAnimator;
        private ValueAnimator _rightEdgeAnimator;
        public bool IsPlayed { get; private set; }
        private int _backgroundColor = Color.Black;
        private ValueAnimator.IAnimatorUpdateListener _animatorUpdateListener;

        public PlayPauseButton(Context context) : base(context)
        {
        }

        public PlayPauseButton(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public PlayPauseButton(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            _point = new MyPoint();
            InitView();
        }

        private void InitView()
        {
            SetUpPaint();
            SetUpPath();
            SetUpAnimator();
        }

        public void OnAnimationUpdate(ValueAnimator animation)
        {
            Invalidate();
        }

        private void SetUpAnimator()
        {
            if (IsPlayed)
            {
                _centerEdgeAnimator = ValueAnimator.OfFloat(1f, 1f);
                _leftEdgeAnimator = ValueAnimator.OfFloat((float) (-0.2f * SQRT_3), (float) (-0.2f * SQRT_3));
                _rightEdgeAnimator = ValueAnimator.OfFloat(1f, 1f);
            }
            else
            {
                _centerEdgeAnimator = ValueAnimator.OfFloat(0.5f, 0.5f);
                _leftEdgeAnimator = ValueAnimator.OfFloat(0f, 0f);
                _rightEdgeAnimator = ValueAnimator.OfFloat(0f, 0f);
            }

            _centerEdgeAnimator.Start();
            _leftEdgeAnimator.Start();
            _rightEdgeAnimator.Start();
        }

        private void SetUpPaint()
        {
            _paint = new Paint
            {
                Color = new Color(_backgroundColor),
                AntiAlias = true
            };
            _paint.SetStyle(Paint.Style.Fill);
        }

        private void SetUpPath()
        {
            _leftPath = new Path();
            _rightPath = new Path();
        }

        protected override void OnDraw(Canvas canvas)
        {
            _point.Height = canvas.Height;
            _point.Width = canvas.Width;

            _leftPath.Reset();
            _rightPath.Reset();

            _leftPath.MoveTo(_point.GetX(-0.5f * SQRT_3), _point.GetY(1f));
            _leftPath.LineTo(_point.GetY((float) _leftEdgeAnimator.AnimatedValue) + 0.7f,
                    _point.GetY((float) _centerEdgeAnimator.AnimatedValue));
            _leftPath.LineTo(_point.GetY((float) _leftEdgeAnimator.AnimatedValue) + 0.7f,
                    _point.GetY(-1 * (float) _centerEdgeAnimator.AnimatedValue));
            _leftPath.LineTo(_point.GetX(-0.5f * SQRT_3), _point.GetY(-1f));

            _rightPath.MoveTo(_point.GetY(-1 * (float) _leftEdgeAnimator.AnimatedValue),
                    _point.GetY((float) _centerEdgeAnimator.AnimatedValue));
            _rightPath.LineTo(_point.GetX(0.5f * SQRT_3),
                    _point.GetY((float) _rightEdgeAnimator.AnimatedValue));
            _rightPath.LineTo(_point.GetX(0.5f * SQRT_3),
                    _point.GetY(-1 * (float) _rightEdgeAnimator.AnimatedValue));
            _rightPath.LineTo(_point.GetY(-1 * (float) _leftEdgeAnimator.AnimatedValue),
                    _point.GetY(-1 * (float) _centerEdgeAnimator.AnimatedValue));

            canvas.DrawPath(_leftPath, _paint);
            canvas.DrawPath(_rightPath, _paint);
        }

        protected override IParcelable OnSaveInstanceState()
        {
            var baseState = base.OnSaveInstanceState();
            var savedState = new SavedState(baseState)
            {
                Play = IsPlayed
            };
            return savedState;
        }

        protected override void OnRestoreInstanceState(IParcelable state)
        {
            var savedState = (SavedState) state;
            base.OnRestoreInstanceState(savedState.SuperState);
            SetPlayed(savedState.Play);
            SetUpAnimator();
            Invalidate();
        }

        public void StartAnimation()
        {
            _centerEdgeAnimator = ValueAnimator.OfFloat(1f, 0.5f);
            _centerEdgeAnimator.SetDuration(100 * SPEED);
            _centerEdgeAnimator.AddUpdateListener(_animatorUpdateListener);

            _leftEdgeAnimator = ValueAnimator.OfFloat((float) (-0.2 * SQRT_3), 0f);
            _leftEdgeAnimator.SetDuration(100 * SPEED);
            _leftEdgeAnimator.AddUpdateListener(_animatorUpdateListener);

            _rightEdgeAnimator = ValueAnimator.OfFloat(1f, 0f);
            _rightEdgeAnimator.SetDuration(150 * SPEED);
            _rightEdgeAnimator.AddUpdateListener(_animatorUpdateListener);

            if (!IsPlayed)
            {
                _centerEdgeAnimator.Start();
                _leftEdgeAnimator.Start();
                _rightEdgeAnimator.Start();
            }
            else
            {
                _centerEdgeAnimator.Reverse();
                _leftEdgeAnimator.Reverse();
                _rightEdgeAnimator.Reverse();
            }
        }

        public void SetPlayed(bool played)
        {
            if (IsPlayed != played)
            {
                IsPlayed = played;
                Invalidate();
            }
        }

        public void SetColor(int color)
        {
            _backgroundColor = color;
            _paint.Color = new Color(_backgroundColor);
            Invalidate();
        }

        private class SavedState : BaseSavedState, IParcelableCreator
        {
            public bool Play { get; set; }
            public new static IParcelableCreator Creator;

            public SavedState(Parcel source) : base(source)
            {
                Play = (bool)source.ReadValue(null);
                Creator = this;
            }

            public SavedState(IParcelable superState) : base(superState)
            {
            }

            public Java.Lang.Object CreateFromParcel(Parcel source)
            {
                return new SavedState(source);
            }

            public Java.Lang.Object[] NewArray(int size)
            {
                return new SavedState[size];
            }

            public override void WriteToParcel(Parcel dest, [GeneratedEnum] ParcelableWriteFlags flags)
            {
                base.WriteToParcel(dest, flags);
                dest.WriteValue(Play);
            }
        }

        private class MyPoint
        {
            public int Height { private get; set; }
            public int Width { private get; set; }

            public float GetX(float x) => Width / 2 * (x + 1);
            public float GetY(float y) => Height / 2 * (y + 1);
        }
    }
}