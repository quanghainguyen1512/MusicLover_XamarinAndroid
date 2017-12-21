using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using Android.Animation;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Graphics.Drawables.Shapes;
using Android.OS;
using Android.Runtime;
using Android.Transitions;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Java.Nio.FileNio;
using Object = Java.Lang.Object;
using View = Android.Views.View;

namespace Music_Lover.Transitions
{
    public class PlayButtonTransition : Transition
    {
        private static string PROPERTY_BOUNDS = "circleTransition:bounds";
        private static string PROPERTY_POSITION = "circleTransition:position";
        private static string PROPERTY_IMAGE = "circleTransition:image";

        private static readonly string[] TRANSITION_PROPERTIES =
        {
            PROPERTY_BOUNDS,
            PROPERTY_POSITION,
        };

        private int _color = Color.ParseColor("#6c1622");
        private static View _shrinkingView, _startView, _circleView, _growingView, _endView;

        public PlayButtonTransition(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            var arr = context.ObtainStyledAttributes(attrs, new []{ Resource.Attribute.colorCT });
            _color = arr.GetColor(Resource.Attribute.colorCT, _color);
            arr.Recycle();
        }

        public override string[] GetTransitionProperties()
        {
            return TRANSITION_PROPERTIES;
        }

        public override void CaptureEndValues(TransitionValues transitionValues)
        {
            var view = transitionValues.View;
            if (view.Width <= 0 || view.Height <= 0)
            {
                return;
            }
            CaptureValues(transitionValues);
        }

        public override void CaptureStartValues(TransitionValues transitionValues)
        {
            var view = transitionValues.View;
            if (view.Width <= 0 || view.Height <= 0)
            {
                return;
            }
            CaptureValues(transitionValues);
            var bitmap = Bitmap.CreateBitmap(view.Width, view.Height,
                Bitmap.Config.Argb8888);
            var canvas = new Canvas(bitmap);
            view.Draw(canvas);
            transitionValues.Values.Add(PROPERTY_IMAGE, bitmap);
        }

        public override Animator CreateAnimator(ViewGroup sceneRoot, TransitionValues startValues, TransitionValues endValues)
        {
            if (startValues == null || endValues == null)
            {
                return null;
            }
            var startBounds = (Rect)startValues.Values[PROPERTY_BOUNDS];
            var endBounds = (Rect)endValues.Values[PROPERTY_BOUNDS];
            if (startBounds == null || endBounds == null || startBounds.Equals(endBounds))
            {
                return null;
            }

            var startImage = (Bitmap)startValues.Values[PROPERTY_IMAGE];
            Drawable startBackground = new BitmapDrawable(sceneRoot.Context.Resources, startImage);
            _startView = AddViewToOverlay(sceneRoot, startImage.Width,
                startImage.Height, startBackground);
            Drawable shrinkingBackground = new ColorDrawable(new Color(_color));
            _shrinkingView = AddViewToOverlay(sceneRoot, startImage.Width,
                startImage.Height, shrinkingBackground);

            var sceneRootLoc = new int[2];
            sceneRoot.GetLocationInWindow(sceneRootLoc);
            var startLoc = (int[])startValues.Values[PROPERTY_POSITION];
            var startTranslationX = startLoc[0] - sceneRootLoc[0];
            var startTranslationY = startLoc[1] - sceneRootLoc[1];

            _startView.TranslationX = startTranslationX;
            _startView.TranslationY = startTranslationY;
            _shrinkingView.TranslationX = startTranslationX;
            _shrinkingView.TranslationY = startTranslationY;

            _endView = endValues.View;
            var startRadius = CalculateMaxRadius(_shrinkingView);
            var minRadius = Math.Min(CalculateMinRadius(_shrinkingView), CalculateMinRadius(_endView));

            var circleBackground = new ShapeDrawable(new OvalShape());
            circleBackground.Paint.Color = new Color(_color);
            _circleView = AddViewToOverlay(sceneRoot, minRadius * 2, minRadius * 2,
                circleBackground);
            float circleStartX = startLoc[0] - sceneRootLoc[0] +
                                 ((_startView.Width - _circleView.Width) / 2);
            float circleStartY = startLoc[1] - sceneRootLoc[1] +
                                 ((_startView.Height - _circleView.Height) / 2);
            _circleView.TranslationX = circleStartX;
            _circleView.TranslationY = circleStartY;

            _circleView.Visibility = ViewStates.Invisible;
            _shrinkingView.Alpha = 0f;
            _endView.Alpha = 0f;

            var shrinkingAnimator = CreateCircularReveal(_shrinkingView, startRadius, minRadius);

            shrinkingAnimator.AddListener(new ShrinkingAnimator());

            var startAnimator = CreateCircularReveal(_startView, startRadius, minRadius);
            var fadeInAnimator = ObjectAnimator.OfFloat(_shrinkingView, "angle", 0, 1);  // <<<<==================

            var shrinkFadeSet = new AnimatorSet();
            shrinkFadeSet.PlayTogether(shrinkingAnimator, startAnimator, fadeInAnimator);

            var endLoc = (int[])endValues.Values[PROPERTY_POSITION];
            float circleEndX = endLoc[0] - sceneRootLoc[0] +
                               ((_endView.Width - _circleView.Width) / 2);
            float circleEndY = endLoc[1] - sceneRootLoc[1] +
                               ((_endView.Height - _circleView.Height) / 2);
            var circlePath = PathMotion.GetPath(circleStartX, circleStartY, circleEndX, circleEndY);
            Animator circleAnimator = ObjectAnimator.OfFloat(_circleView, View.X,
                View.Y, circlePath);

            _growingView = AddViewToOverlay(sceneRoot, _endView.Width,
                _endView.Height, shrinkingBackground);
            _growingView.Visibility = ViewStates.Invisible;
            float endTranslationX = endLoc[0] - sceneRootLoc[0];
            float endTranslationY = endLoc[1] - sceneRootLoc[1];
            _growingView.TranslationX = endTranslationX;
            _growingView.TranslationY = endTranslationY;

            var endRadius = CalculateMaxRadius(_endView);

            circleAnimator.AddListener(new CircleAnimator());

            Animator fadeOutAnimator = ObjectAnimator.OfFloat(_growingView, "angle", 1, 0); //<<<============
            var endAnimator = CreateCircularReveal(_endView, minRadius, endRadius);
            var growingAnimator = CreateCircularReveal(_growingView, minRadius, endRadius);

            growingAnimator.AddListener(new GrowingAnimator(sceneRoot));
            var growingFadeSet = new AnimatorSet();
            growingFadeSet.PlayTogether(fadeOutAnimator, endAnimator, growingAnimator);

            var animatorSet = new AnimatorSet();
            animatorSet.PlaySequentially(shrinkFadeSet, circleAnimator, growingFadeSet);
            return animatorSet;
        }

        private View AddViewToOverlay(ViewGroup sceneRoot, int width, int height, Drawable background)
        {
            var view = new NoOverlapView(sceneRoot.Context)
            {
                Background = background
            };
            var widthSpec = View.MeasureSpec.MakeMeasureSpec(width, MeasureSpecMode.Exactly);
            var heightSpec = View.MeasureSpec.MakeMeasureSpec(height, MeasureSpecMode.Exactly);
            view.Measure(widthSpec, heightSpec);
            view.Layout(0, 0, width, height);
            sceneRoot.Overlay.Add(ViewToDrawable(view));
            return view;
        }

        private void CaptureValues(TransitionValues transitionValues)
        {
            var view = transitionValues.View;
            transitionValues.Values.Add(PROPERTY_BOUNDS, new Rect(view.Left, view.Top, view.Right, view.Bottom));
            var position = new int[2];
            transitionValues.View.GetLocationInWindow(position);
            transitionValues.Values.Add(PROPERTY_POSITION, position);
        }

        private Drawable ViewToDrawable(View view)
        {
            view.DrawingCacheEnabled = true;
            view.BuildDrawingCache();
            var bm = view.DrawingCache;
            return new BitmapDrawable(bm);
        }

        private Animator CreateCircularReveal(View view, float startRadius, float endRadius)
        {
            var centerX = view.Width / 2;
            var centerY = view.Height / 2;

            var reveal = ViewAnimationUtils.CreateCircularReveal(view, centerX, centerY,
                startRadius, endRadius);
            return new NoPauseAnimator(reveal);
        }

        private float CalculateMaxRadius(View view)
        {
            float widthSquared = view.Width * view.Width;
            float heightSquared = view.Height * view.Height;
            var radius = (float)Math.Sqrt(widthSquared + heightSquared) / 2;
            return radius;
        }

        private int CalculateMinRadius(View view)
        {
            return Math.Min(view.Width / 2, view.Height / 2);
        }
        
        private class NoPauseAnimator : Animator
        {
            private readonly Animator _animator;

            private Dictionary<IAnimatorListener, IAnimatorListener> _listeners =
                new Dictionary<IAnimatorListener, IAnimatorListener>();

            public NoPauseAnimator(Animator anim)
            {
                _animator = anim;
            }

            public override void AddListener(IAnimatorListener listener)
            {
                var wrapper = new AnimatorListenerWrapper(this, listener);
                if (_listeners.ContainsKey(listener))
                {
                    _listeners.Add(listener, wrapper);
                    _animator.AddListener(listener);
                }
            }

            public override void Cancel()
            {
                _animator.Cancel();
            }

            public override void End()
            {
                _animator.End();
            }

            public override ITimeInterpolator Interpolator => _animator.Interpolator;
            public override IList<IAnimatorListener> Listeners => new List<IAnimatorListener>(_listeners.Keys);
            public override bool IsPaused => _animator.IsPaused;
            public override bool IsStarted => _animator.IsStarted;
            public override long Duration => _animator.Duration;
            public override bool IsRunning => _animator.IsRunning;

            public override void RemoveAllListeners()
            {
                _listeners.Clear();
                _animator.RemoveAllListeners();
            }

            public override void RemoveListener(IAnimatorListener listener)
            {
                var wrapper = _listeners[listener];
                if (wrapper is null) return;
                _listeners.Remove(wrapper);
                _animator.RemoveListener(wrapper);
            }

            public override Animator SetDuration(long duration)
            {
                _animator.SetDuration(duration);
                return this;
            }

            public override void SetInterpolator(ITimeInterpolator value)
            {
                _animator.SetInterpolator(value);
            }

            public override long StartDelay
            {
                get => _animator.StartDelay;
                set => _animator.StartDelay = value;
            }

            public override void Start()
            {
                _animator.Start();
            }

            public override void SetTarget(Object target)
            {
                _animator.SetTarget(target);
            }

            public override void SetupEndValues()
            {
                _animator.SetupEndValues();
            }

            public override void SetupStartValues()
            {
                _animator.SetupStartValues();
            }
        }

        private class AnimatorListenerWrapper : Java.Lang.Object, Animator.IAnimatorListener
        {
            private readonly Animator _animator;
            private readonly Animator.IAnimatorListener _listener;

            public AnimatorListenerWrapper(Animator anim, Animator.IAnimatorListener listener)
            {
                _animator = anim;
                _listener = listener;
            }

            public void OnAnimationCancel(Animator animation)
            {
                _listener.OnAnimationCancel(_animator);
            }

            public void OnAnimationEnd(Animator animation)
            {
                _listener.OnAnimationEnd(_animator);
            }

            public void OnAnimationRepeat(Animator animation)
            {
                _listener.OnAnimationRepeat(_animator);
            }

            public void OnAnimationStart(Animator animation)
            {
                _listener.OnAnimationStart(_animator);
            }
        }

        private class NoOverlapView : View
        {
            public NoOverlapView(Context context) : base(context) { }

            public override bool HasOverlappingRendering => false;
        }

        private class ShrinkingAnimator : AnimatorListenerAdapter
        {
            public override void OnAnimationEnd(Animator animation)
            {
                _shrinkingView.Visibility = _startView.Visibility = ViewStates.Invisible;
                _circleView.Visibility = ViewStates.Visible;
            }
        }

        private class CircleAnimator : AnimatorListenerAdapter
        {
            public override void OnAnimationEnd(Animator animation)
            {
                _circleView.Visibility = ViewStates.Invisible;
                _growingView.Visibility = ViewStates.Invisible;
                _endView.Alpha = 1f;
            }
        }

        private class GrowingAnimator : AnimatorListenerAdapter
        {
            private readonly ViewGroup _sceneRootClone;
            public GrowingAnimator(ViewGroup vg)
            {
                _sceneRootClone = vg;
            }
            public override void OnAnimationEnd(Animator animation)
            {
                _sceneRootClone.RemoveView(_startView);
                _sceneRootClone.RemoveView(_shrinkingView);
                _sceneRootClone.RemoveView(_circleView);
                _sceneRootClone.RemoveView(_growingView);
            }
        }
    }
}