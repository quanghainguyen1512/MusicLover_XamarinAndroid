using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Animation;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Music_Lover.Widgets
{
    public class FastScroller : LinearLayout
    {
        private TextView _bubble;
        private View _handle;
        private int _height;
        private Animator _animator;
        private RecyclerView _recyclerView;
        private RecyclerView.OnScrollListener scrollListener;

        public FastScroller(Context context) : base(context)
        {
            Initialise(context);
        }

        public FastScroller(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Initialise(context);
        }

        public FastScroller(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            Initialise(context);
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);
            _height = h;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            var action = e.Action;

            switch (action)
            {
                case MotionEventActions.Up:
                case MotionEventActions.Cancel:
                {
                    _handle.Selected = false;
                    HideBubble();
                    return true;
                }
                case MotionEventActions.Down:
                {
                    if (e.GetX() < _handle.GetX())
                        return false;
                    if (_animator != null)
                        _animator.Cancel();
                    if (_bubble.Visibility == ViewStates.Invisible)
                        ShowBubble();
                    _handle.Selected = true;
                    break;
                }
                case MotionEventActions.Move:
                {
                    var y = e.GetY();
                    SetBubbleAndHandlePosition(y);
                    SetRecyclerViewPosition(y);
                    return true;
                }
                default:
                    break;
            }

            return base.OnTouchEvent(e);
        }

        public void SetRecyclerView(RecyclerView recyclerView)
        {
            _recyclerView = recyclerView;
            recyclerView.AddOnScrollListener(scrollListener);
        }

        private void Initialise(Context context)
        {
            Orientation = Orientation.Horizontal;
            SetClipChildren(false);
            var inflater = LayoutInflater.From(context);
            inflater.Inflate(Resource.Layout.recyclerview_fastscroller, this, true);
            _bubble = FindViewById<TextView>(Resource.Id.fastscroller_bubble);
            _handle = FindViewById<View>(Resource.Id.fastscroller_handle);
            _bubble.Visibility = ViewStates.Invisible;
        }

        private void ShowBubble()
        {
            AnimatorSet animatorSet = new AnimatorSet();
            _bubble.Visibility = ViewStates.Visible;
            if (_animator != null)
                _animator.Cancel();
            _animator = ObjectAnimator.OfFloat(_bubble, "alpha", 0f, 1f).SetDuration(100);
            _animator.Start();
        }

        private void HideBubble()
        {
            if (_animator != null)
                _animator.Cancel();
            _animator = ObjectAnimator.OfFloat(_bubble, "alpha", 1f, 0f).SetDuration(100);
            _animator.AddListener(new AnimatorListener
            {
                OnEnd = () =>
                {
                    _bubble.Visibility = ViewStates.Invisible;
                    _animator = null;
                },
                OnCancel = () =>
                {
                    _bubble.Visibility = ViewStates.Invisible;
                    _animator = null;
                }
            });
            _animator.Start();
        }

        private int GetValueInRange(int min, int max, int value)
        {
            int minimum = Math.Max(min, value);
            return Math.Min(minimum, max);
        }

        private void SetBubbleAndHandlePosition(float y)
        {
            int bubbleHeight = _bubble.Height;
            int handleHeight = _handle.Height;
            _handle.SetY(GetValueInRange(0, _height - handleHeight, (int) (y - handleHeight / 2)));
            _bubble.SetY(GetValueInRange(0, _height - bubbleHeight - handleHeight / 2, (int) (y - bubbleHeight)));
        }

        private void SetRecyclerViewPosition(float y)
        {
            if (_recyclerView != null)
            {
                int itemCount = _recyclerView.GetAdapter().ItemCount;
                float proportion;
                if (_handle.GetY() == 0)
                    proportion = 0f;
                else if (_handle.GetY() + _handle.Height >= _height - 5)
                    proportion = 1f;
                else
                    proportion = y / (float) _height;
                int targetPos = GetValueInRange(0, itemCount - 1, (int) (proportion * (float) itemCount));
                ((LinearLayoutManager) _recyclerView.GetLayoutManager()).ScrollToPositionWithOffset(targetPos, 0);
                //      _recyclerView.oPositionWithOffset(targetPos);
                var bubbleText = ((IBubbleTextGetter) _recyclerView.GetAdapter()).GetTextToShowInBubble(targetPos);
                _bubble.Text = bubbleText;
            }
        }

        private class AnimatorListener : AnimatorListenerAdapter
        {
            public Action OnEnd { get; set; }
            public Action OnCancel { get; set; }

            public override void OnAnimationEnd(Animator animation)
            {
                base.OnAnimationEnd(animation);
                OnEnd();
            }

            public override void OnAnimationCancel(Animator animation)
            {
                base.OnAnimationCancel(animation);
                OnCancel();
            }
        }
    }
}