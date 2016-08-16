using System;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Util;
using Android.Views;
using DiscreteSeekBar.Internal.Drawable;
using Android.Widget;
using DiscreteSeekBar.Internal.Compat;

namespace DiscreteSeekBar.Internal
{
    public class PopupIndicator
    {
        private IWindowManager mWindowManager;
        private bool mShowing;
        private Floater mPopupView;
        //Outside listener for the DiscreteSeekBar to get MarkerDrawable animation events.
        //The whole chain of events goes this way:
        //MarkerDrawable->Marker->Floater->mListener->DiscreteSeekBar....
        //... phew!
        private IMarkerAnimationListener mListener;
        private int[] mDrawingLocation = new int[2];
        Point screenSize = new Point();

        public PopupIndicator(Context context, IAttributeSet attrs, int defStyleAttr, String maxValue, int thumbSize, int separation)
        {
            mWindowManager = (IWindowManager)context.GetSystemService(Context.WindowService);
            mPopupView = new Floater(context, attrs, defStyleAttr, maxValue, thumbSize, separation, mListener, DismissComplete);
        }

        public void UpdateSizes(String maxValue)
        {
            DismissComplete();
            if (mPopupView != null)
            {
                mPopupView.mMarker.ResetSizes(maxValue);
            }
        }

        public void SetListener(IMarkerAnimationListener listener)
        {
            mListener = listener;
            mPopupView._listener = listener;
        }

        private void MeasureFloater()
        {
            int specWidth = View.MeasureSpec.MakeMeasureSpec(screenSize.X, MeasureSpecMode.Exactly);
            int specHeight = View.MeasureSpec.MakeMeasureSpec(screenSize.Y, MeasureSpecMode.AtMost);
            mPopupView.Measure(specWidth, specHeight);
        }

        public void SetValue(string value)
        {
            mPopupView.mMarker.SetValue(value);
        }

        public bool IsShowing()
        {
            return mShowing;
        }

        public void showIndicator(View parent, Rect touchBounds)
        {
            if (IsShowing())
            {
                mPopupView.mMarker.AnimateOpen();
                return;
            }

            var windowToken = parent.WindowToken;
            if (windowToken != null)
            {
                var p = CreatePopupLayout(windowToken);

                p.Gravity = GravityFlags.Top | GravityFlags.Start;
                UpdateLayoutParamsForPosiion(parent, p, touchBounds.Bottom);
                mShowing = true;

                TranslateViewIntoPosition(touchBounds.CenterX());
                InvokePopup(p);
            }
        }

        public void Move(int x)
        {
            if (!IsShowing())
            {
                return;
            }
            TranslateViewIntoPosition(x);
        }

        public void SetColors(int startColor, int endColor)
        {
            mPopupView.SetColors(startColor, endColor);
        }

        /**
         * This will start the closing animation of the Marker and call onClosingComplete when finished
         */
        public void Dismiss()
        {
            mPopupView.mMarker.AnimateClose();
        }

        /**
         * FORCE the popup window to be removed.
         * You typically calls this when the parent view is being removed from the window to avoid a Window Leak
         */
        public void DismissComplete()
        {
            if (IsShowing())
            {
                mShowing = false;
                try
                {
                    mWindowManager.RemoveViewImmediate(mPopupView);
                }
                finally
                {
                }
            }
        }

        private void UpdateLayoutParamsForPosiion(View anchor, WindowManagerLayoutParams p, int yOffset)
        {
            DisplayMetrics displayMetrics = anchor.Resources.DisplayMetrics;
            screenSize.Set(displayMetrics.WidthPixels, displayMetrics.HeightPixels);

            MeasureFloater();
            int measuredHeight = mPopupView.MeasuredHeight;
            int paddingBottom = mPopupView.mMarker.PaddingBottom;
            anchor.GetLocationInWindow(mDrawingLocation);
            p.X = 0;
            p.Y = mDrawingLocation[1] - measuredHeight + yOffset + paddingBottom;
            p.Width = screenSize.X;
            p.Height = measuredHeight;
        }

        private void TranslateViewIntoPosition(int x)
        {
            mPopupView.SetFloatOffset(x + mDrawingLocation[0]);
        }

        private void InvokePopup(WindowManagerLayoutParams p)
        {
            mWindowManager.AddView(mPopupView, p);
            mPopupView.mMarker.AnimateOpen();
        }

        private WindowManagerLayoutParams CreatePopupLayout(IBinder token)
        {
            WindowManagerLayoutParams p = new WindowManagerLayoutParams();
            p.Gravity = GravityFlags.Start | GravityFlags.Top;
            p.Width = ViewGroup.LayoutParams.MatchParent;
            p.Height = ViewGroup.LayoutParams.MatchParent;
            p.Format = Format.Translucent;
            p.Flags = ComputeFlags(p.Flags);
            p.Type = WindowManagerTypes.ApplicationPanel;
            p.Token = token;
            p.SoftInputMode = SoftInput.StateAlwaysHidden;
            p.Title = $"DiscreteSeekBar Indicator:{Guid.NewGuid()}";

            return p;
        }

        /**
     * I'm NOT completely sure how all this bitwise things work...
     *
     * @param curFlags
     * @return
     */
        private WindowManagerFlags ComputeFlags(WindowManagerFlags curFlags)
        {
            curFlags &= ~(
                WindowManagerFlags.IgnoreCheekPresses |
                WindowManagerFlags.NotFocusable |
                WindowManagerFlags.NotTouchable |
                WindowManagerFlags.WatchOutsideTouch |
                WindowManagerFlags.LayoutNoLimits |
                WindowManagerFlags.AltFocusableIm);
            curFlags |= WindowManagerFlags.IgnoreCheekPresses;
            curFlags |= WindowManagerFlags.NotFocusable;
            curFlags |= WindowManagerFlags.NotTouchable;
            curFlags |= WindowManagerFlags.LayoutNoLimits;

            return curFlags;
        }

        private class Floater : FrameLayout, IMarkerAnimationListener
        {
            public Marker mMarker;
            private int mOffset;

            public IMarkerAnimationListener _listener;
            private Action _dismissCompleted;

            public Floater(Context context, IAttributeSet attrs, int defStyleAttr, String maxValue, int thumbSize, int separation, IMarkerAnimationListener listener, Action dismissCompleted)
                : base (context, attrs, defStyleAttr)
            {
                _listener = listener;
                _dismissCompleted = dismissCompleted;

                mMarker = new Marker(context, attrs, defStyleAttr, maxValue, thumbSize, separation);
                AddView(mMarker, new LayoutParams(ViewGroup.LayoutParams.WrapContent, ViewGroup.LayoutParams.WrapContent, GravityFlags.Left | GravityFlags.Top));
            }

            protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
            {
                MeasureChildren(widthMeasureSpec, heightMeasureSpec);
                int widthSize = MeasureSpec.GetSize(widthMeasureSpec);
                int heightSie = mMarker.MeasuredHeight;
                SetMeasuredDimension(widthSize, heightSie);
            }

            protected override void OnLayout(bool changed, int l, int t, int r, int b)
            {
                int centerDiffX = mMarker.MeasuredWidth / 2;
                int offset = (mOffset - centerDiffX);
                mMarker.Layout(offset, 0, offset + mMarker.MeasuredWidth, mMarker.MeasuredHeight);
            }

            public void SetFloatOffset(int x)
            {
                mOffset = x;
                int centerDiffX = mMarker.MeasuredWidth / 2;
                int offset = (x - centerDiffX);
                mMarker.OffsetLeftAndRight(offset - mMarker.Left);
                //Without hardware acceleration (or API levels<11), offsetting a view seems to NOT invalidate the proper area.
                //We should calc the proper invalidate Rect but this will be for now...
                if (!SeekBarCompat.IsHardwareAccelerated(this))
                {
                    Invalidate();
                }
            }

            public void OnClosingComplete()
            {
                if (_listener != null)
                {
                    _listener.OnClosingComplete();
                }
                _dismissCompleted();
            }

            public void OnOpeningComplete()
            {
                if (_listener != null)
                {
                    _listener.OnOpeningComplete();
                }
            }

            public void SetColors(int startColor, int endColor)
            {
                mMarker.SetColors(startColor, endColor);
            }
        }
    }
}

