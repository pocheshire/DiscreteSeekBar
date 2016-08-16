using System;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.OS;
using Android.Support.V4.Graphics.Drawable;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using DiscreteSeekBar.Internal;
using DiscreteSeekBar.Internal.Compat;
using DiscreteSeekBar.Internal.Drawable;
using Java.Lang;
using Java.Util;
using Math = System.Math;

namespace DiscreteSeekBar
{
    public class DiscreteSeekBar : View, IAnimationFrameUpdateListener, IMarkerAnimationListener
    {
        public interface OnProgressChangeListener
        {
            void OnProgressChanged(DiscreteSeekBar seekBar, int value, bool fromUser);

            void OnStartTrackingTouch(DiscreteSeekBar seekBar);

            void OnStopTrackingTouch(DiscreteSeekBar seekBar);
        }

        public abstract class NumericTransformer
        {
            public abstract int Transform(int value);

            public System.String transformToString(int value)
            {
                return value.ToString();
            }

            public bool useStringTransform()
            {
                return false;
            }
        }

        private class DefaultNumericTransformer : NumericTransformer
        {
            public override int Transform(int value)
            {
                return value;
            }
        }

        private bool isLollipopOrGreater = Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop;
        private const System.String DEFAULT_FORMATTER = "%d";

        private const int PRESSED_STATE = Android.Resource.Attribute.StatePressed;
        private const int FOCUSED_STATE = Android.Resource.Attribute.StateFocused;
        private const int PROGRESS_ANIMATION_DURATION = 250;
        private const int INDICATOR_DELAY_FOR_TAPS = 150;
        private int DEFAULT_THUMB_COLOR = (Color.ParseColor("#009688")).ToArgb();
        private const int SEPARATION_DP = 5;
        private ThumbDrawable mThumb;
        private TrackRectDrawable mTrack;
        private TrackRectDrawable mScrubber;
        private Drawable mRipple;

        private int mTrackHeight;
        private int mScrubberHeight;
        private int mAddedTouchBounds;

        private int mMax;
        private int mMin;
        private int mValue;
        private int mKeyProgressIncrement = 1;
        private bool mMirrorForRtl = false;
        private bool mAllowTrackClick = true;
        private bool mIndicatorPopupEnabled = true;
        //We use our own Formatter to avoid creating new instances on every progress change
        Formatter mFormatter;
        private System.String mIndicatorFormatter;
        private NumericTransformer mNumericTransformer;
        private Java.Lang.StringBuilder mFormatBuilder;
        private OnProgressChangeListener mPublicChangeListener;
        private bool mIsDragging;
        private int mDragOffset;

        private Rect mInvalidateRect = new Rect();
        private Rect mTempRect = new Rect();
        private PopupIndicator mIndicator;
        private AnimatorCompat mPositionAnimator;
        private float mAnimationPosition;
        private int mAnimationTarget;
        private float mDownX;
        private float mTouchSlop;

        private Runnable mShowIndicatorRunnable;

        private IMarkerAnimationListener mFloaterListener;

        public DiscreteSeekBar(Context context)
        : this(context, null)
        {

        }

        public DiscreteSeekBar(Context context, IAttributeSet attrs)
            : this(context, attrs, Resource.Attribute.discreteSeekBarStyle)
        {

        }

        public DiscreteSeekBar(Context context, IAttributeSet attrs, int defStyleAttr)
         : base(context, attrs, defStyleAttr)
        {
            mShowIndicatorRunnable = new Runnable(Run);
            mFloaterListener = this;

            Focusable = true;
            SetWillNotDraw(false);

            mTouchSlop = ViewConfiguration.Get(context).ScaledWindowTouchSlop;
            float density = context.Resources.DisplayMetrics.Density;

            TypedArray a = context.ObtainStyledAttributes(attrs, Resource.Styleable.DiscreteSeekBar,
                    defStyleAttr, Resource.Style.Widget_DiscreteSeekBar);

            int max = 100;
            int min = 0;
            int value = 0;
            mMirrorForRtl = a.GetBoolean(Resource.Styleable.DiscreteSeekBar_dsb_mirrorForRtl, mMirrorForRtl);
            mAllowTrackClick = a.GetBoolean(Resource.Styleable.DiscreteSeekBar_dsb_allowTrackClickToDrag, mAllowTrackClick);
            mIndicatorPopupEnabled = a.GetBoolean(Resource.Styleable.DiscreteSeekBar_dsb_indicatorPopupEnabled, mIndicatorPopupEnabled);
            mTrackHeight = a.GetDimensionPixelSize(Resource.Styleable.DiscreteSeekBar_dsb_trackHeight, (int)(1 * density));
            mScrubberHeight = a.GetDimensionPixelSize(Resource.Styleable.DiscreteSeekBar_dsb_scrubberHeight, (int)(4 * density));
            int thumbSize = a.GetDimensionPixelSize(Resource.Styleable.DiscreteSeekBar_dsb_thumbSize, (int)(density * ThumbDrawable.DEFAULT_SIZE_DP));
            int separation = a.GetDimensionPixelSize(Resource.Styleable.DiscreteSeekBar_dsb_indicatorSeparation,
                    (int)(SEPARATION_DP * density));

            //Extra pixels for a minimum touch area of 32dp
            int touchBounds = (int)(density * 32);
            mAddedTouchBounds = System.Math.Max(0, (touchBounds - thumbSize) / 2);

            int indexMax = Resource.Styleable.DiscreteSeekBar_dsb_max;
            int indexMin = Resource.Styleable.DiscreteSeekBar_dsb_min;
            int indexValue = Resource.Styleable.DiscreteSeekBar_dsb_value;
            TypedValue @out = new TypedValue();
            //Not sure why, but we wanted to be able to use dimensions here...
            if (a.GetValue(indexMax, @out))
            {
                if (@out.Type == DataType.Dimension)
                {
                    max = a.GetDimensionPixelSize(indexMax, max);
                }
                else {
                    max = a.GetInteger(indexMax, max);
                }
            }
            if (a.GetValue(indexMin, @out))
            {
                if (@out.Type == DataType.Dimension)
                {
                    min = a.GetDimensionPixelSize(indexMin, min);
                }
                else {
                    min = a.GetInteger(indexMin, min);
                }
            }
            if (a.GetValue(indexValue, @out))
            {
                if (@out.Type == DataType.Dimension)
                {
                    value = a.GetDimensionPixelSize(indexValue, value);
                }
                else {
                    value = a.GetInteger(indexValue, value);
                }
            }

            mMin = min;
            mMax = Math.Max(min + 1, max);
            mValue = Math.Max(min, Math.Min(max, value));
            UpdateKeyboardRange();

            mIndicatorFormatter = a.GetString(Resource.Styleable.DiscreteSeekBar_dsb_indicatorFormatter);

            ColorStateList trackColor = a.GetColorStateList(Resource.Styleable.DiscreteSeekBar_dsb_trackColor);
            ColorStateList progressColor = a.GetColorStateList(Resource.Styleable.DiscreteSeekBar_dsb_progressColor);
            ColorStateList rippleColor = a.GetColorStateList(Resource.Styleable.DiscreteSeekBar_dsb_rippleColor);
            bool editMode = IsInEditMode;
            if (editMode || rippleColor == null)
            {
                rippleColor = new ColorStateList(new int[][] { new int[] { } }, new int[] { Color.DarkGray });
            }
            if (editMode || trackColor == null)
            {
                trackColor = new ColorStateList(new int[][] { new int[] { } }, new int[] { Color.Gray });
            }
            if (editMode || progressColor == null)
            {
                progressColor = new ColorStateList(new int[][] { new int[] { } }, new int[] { DEFAULT_THUMB_COLOR });
            }

            mRipple = SeekBarCompat.GetRipple(rippleColor);
            if (isLollipopOrGreater)
            {
                SeekBarCompat.SetBackground(this, mRipple);
            }
            else {
                mRipple.SetCallback(this);
            }

            TrackRectDrawable shapeDrawable = new TrackRectDrawable(trackColor);
            mTrack = shapeDrawable;
            mTrack.SetCallback(this);

            shapeDrawable = new TrackRectDrawable(progressColor);
            mScrubber = shapeDrawable;
            mScrubber.SetCallback(this);

            mThumb = new ThumbDrawable(progressColor, thumbSize);
            mThumb.SetCallback(this);
            mThumb.SetBounds(0, 0, mThumb.IntrinsicWidth, mThumb.IntrinsicHeight);


            if (!editMode)
            {
                mIndicator = new PopupIndicator(context, attrs, defStyleAttr, ConvertValueToMessage(mMax),
                        thumbSize, thumbSize + mAddedTouchBounds + separation);
                mIndicator.SetListener(mFloaterListener);
            }
            a.Recycle();

            SetNumericTransformer(new DefaultNumericTransformer());
        }

        public void SetIndicatorFormatter(System.String formatter)
        {
            mIndicatorFormatter = formatter;
            UpdateProgressMessage(mValue);
        }

        public void SetNumericTransformer(NumericTransformer transformer)
        {
            mNumericTransformer = transformer != null ? transformer : new DefaultNumericTransformer();
            //We need to refresh the PopupIndicator view
            UpdateIndicatorSizes();
            UpdateProgressMessage(mValue);
        }

        public NumericTransformer getNumericTransformer()
        {
            return mNumericTransformer;
        }

        public void SetMax(int max)
        {
            mMax = max;
            if (mMax < mMin)
            {
                SetMin(mMax - 1);
            }
            UpdateKeyboardRange();

            if (mValue < mMin || mValue > mMax)
            {
                SetProgress(mMin);
            }
            //We need to refresh the PopupIndicator view
            UpdateIndicatorSizes();
        }

        public int GetMax()
        {
            return mMax;
        }

        public void SetMin(int min)
        {
            mMin = min;
            if (mMin > mMax)
            {
                SetMax(mMin + 1);
            }
            UpdateKeyboardRange();

            if (mValue < mMin || mValue > mMax)
            {
                SetProgress(mMin);
            }
        }

        public int GetMin()
        {
            return mMin;
        }

        public void SetProgress(int progress)
        {
            SetProgress(progress, false);
        }

        private void SetProgress(int value, bool fromUser)
        {
            value = Math.Max(mMin, Math.Min(mMax, value));
            if (IsAnimationRunning())
            {
                mPositionAnimator.Cancel();
            }

            if (mValue != value)
            {
                mValue = value;
                NotifyProgress(value, fromUser);
                UpdateProgressMessage(value);
                UpdateThumbPosFromCurrentProgress();
            }
        }

        public int GetProgress()
        {
            return mValue;
        }

        public void SetOnProgressChangeListener(OnProgressChangeListener listener)
        {
            mPublicChangeListener = listener;
        }

        public void SetThumbColor(int thumbColor, int indicatorColor)
        {
            mThumb.SetColorStateList(ColorStateList.ValueOf(new Color(thumbColor)));
            mIndicator.SetColors(indicatorColor, thumbColor);
        }

        public void SetThumbColor(ColorStateList thumbColorStateList, int indicatorColor)
        {
            mThumb.SetColorStateList(thumbColorStateList);
            //we use the "pressed" color to morph the indicator from it to its own color
            int thumbColor = thumbColorStateList.GetColorForState(new int[] { PRESSED_STATE }, new Color(thumbColorStateList.DefaultColor));
            mIndicator.SetColors(indicatorColor, thumbColor);
        }

        public void SetScrubberColor(int color)
        {
            mScrubber.SetColorStateList(ColorStateList.ValueOf(new Color(color)));
        }

        public void SetScrubberColor(ColorStateList colorStateList)
        {
            mScrubber.SetColorStateList(colorStateList);
        }

        public void setRippleColor(int color)
        {
            SetRippleColor(new ColorStateList(new int[][] { new int[] { } }, new int[] { color }));
        }

        public void SetRippleColor(ColorStateList colorStateList)
        {
            SeekBarCompat.SetRippleColor(mRipple, colorStateList);
        }

        public void SetTrackColor(int color)
        {
            mTrack.SetColorStateList(ColorStateList.ValueOf(new Color(color)));
        }

        public void SetTrackColor(ColorStateList colorStateList)
        {
            mTrack.SetColorStateList(colorStateList);
        }

        public void SetIndicatorPopupEnabled(bool enabled)
        {
            this.mIndicatorPopupEnabled = enabled;
        }

        private void UpdateIndicatorSizes()
        {
            if (!IsInEditMode)
            {
                if (mNumericTransformer.useStringTransform())
                {
                    mIndicator.UpdateSizes(mNumericTransformer.transformToString(mMax));
                }
                else {
                    mIndicator.UpdateSizes(ConvertValueToMessage(mNumericTransformer.Transform(mMax)));
                }
            }

        }

        private void NotifyProgress(int value, bool fromUser)
        {
            if (mPublicChangeListener != null)
            {
                mPublicChangeListener.OnProgressChanged(this, value, fromUser);
            }
            OnValueChanged(value);
        }

        private void NotifyBubble(bool open)
        {
            if (open)
            {
                OnShowBubble();
            }
            else
            {
                OnHideBubble();
            }
        }

        protected virtual void OnShowBubble()
        {
        }

        protected virtual void OnHideBubble()
        {
        }

        protected virtual void OnValueChanged(int value)
        {
        }

        private void UpdateKeyboardRange()
        {
            int range = mMax - mMin;
            if ((mKeyProgressIncrement == 0) || (range / mKeyProgressIncrement > 20))
            {
                // It will take the user too long to change this via keys, change it
                // to something more reasonable
                mKeyProgressIncrement = (int)Math.Max(1, Math.Round((float)range / 20));
            }
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            int widthSize = MeasureSpec.GetSize(widthMeasureSpec);
            int height = mThumb.IntrinsicHeight + PaddingTop + PaddingBottom;
            height += (mAddedTouchBounds * 2);
            SetMeasuredDimension(widthSize, height);
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);
            if (changed)
            {
                RemoveCallbacks(mShowIndicatorRunnable);
                if (!IsInEditMode)
                {
                    mIndicator.DismissComplete();
                }
                UpdateFromDrawableState();
            }
        }

        public override void ScheduleDrawable(Drawable who, Java.Lang.IRunnable what, long when)
        {
            base.ScheduleDrawable(who, what, when);
        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(w, h, oldw, oldh);

            int thumbWidth = mThumb.IntrinsicWidth;
            int thumbHeight = mThumb.IntrinsicHeight;
            int addedThumb = mAddedTouchBounds;
            int halfThumb = thumbWidth / 2;
            int paddingLeft = PaddingLeft + addedThumb;
            int paddingRight = PaddingRight;
            int bottom = Height - PaddingBottom - addedThumb;
            mThumb.SetBounds(paddingLeft, bottom - thumbHeight, paddingLeft + thumbWidth, bottom);
            int trackHeight = Math.Max(mTrackHeight / 2, 1);
            mTrack.SetBounds(paddingLeft + halfThumb, bottom - halfThumb - trackHeight,
                    Width - halfThumb - paddingRight - addedThumb, bottom - halfThumb + trackHeight);
            int scrubberHeight = Math.Max(mScrubberHeight / 2, 2);
            mScrubber.SetBounds(paddingLeft + halfThumb, bottom - halfThumb - scrubberHeight,
                    paddingLeft + halfThumb, bottom - halfThumb + scrubberHeight);

            //Update the thumb position after size changed
            UpdateThumbPosFromCurrentProgress();
        }

        protected override void OnDraw(Canvas canvas)
        {
            if (!isLollipopOrGreater)
            {
                mRipple.Draw(canvas);
            }

            base.OnDraw(canvas);

            mTrack.Draw(canvas);
            mScrubber.Draw(canvas);
            mThumb.Draw(canvas);
        }

        protected override void DrawableStateChanged()
        {
            base.DrawableStateChanged();

            UpdateFromDrawableState();
        }

        private void UpdateFromDrawableState()
        {
            int[] state = GetDrawableState();
            bool focused = false;
            bool pressed = false;
            foreach (int i in state)
            {
                if (i == FOCUSED_STATE)
                {
                    focused = true;
                }
                else if (i == PRESSED_STATE)
                {
                    pressed = true;
                }
            }
            if (Enabled && (focused || pressed) && mIndicatorPopupEnabled)
            {
                //We want to add a small delay here to avoid
                //poping in/out on simple taps
                RemoveCallbacks(mShowIndicatorRunnable);
                PostDelayed(mShowIndicatorRunnable, INDICATOR_DELAY_FOR_TAPS);
            }
            else {
                HideFloater();
            }
            mThumb.SetState(state);
            mTrack.SetState(state);
            mScrubber.SetState(state);
            mRipple.SetState(state);
        }

        private void UpdateProgressMessage(int value)
        {
            if (!IsInEditMode)
            {
                if (mNumericTransformer.useStringTransform())
                {
                    mIndicator.SetValue(mNumericTransformer.transformToString(value));
                }
                else {
                    mIndicator.SetValue(ConvertValueToMessage(mNumericTransformer.Transform(value)));
                }
            }
        }

        private System.String ConvertValueToMessage(int value)
        {
            System.String format = mIndicatorFormatter != null ? mIndicatorFormatter : DEFAULT_FORMATTER;
            //We're trying to re-use the Formatter here to avoid too much memory allocations
            //But I'm not completey sure if it's doing anything good... :(
            //Previously, this condition was wrong so the Formatter was always re-created
            //But as I fixed the condition, the formatter started outputting trash characters from previous
            //calls, so I mark the StringBuilder as empty before calling format again.

            //Anyways, I see the memory usage still go up on every call to this method
            //and I have no clue on how to fix that... damn Strings...
            if (mFormatter == null || !mFormatter.Locale().Equals(Locale.Default))
            {
                int bufferSize = format.Length + Java.Lang.String.ValueOf(mMax).Length;
                if (mFormatBuilder == null)
                {
                    mFormatBuilder = new Java.Lang.StringBuilder(bufferSize);
                }
                else {
                    mFormatBuilder.EnsureCapacity(bufferSize);
                }
                mFormatter = new Formatter(mFormatBuilder, Locale.Default);
            }
            else {
                mFormatBuilder.SetLength(0);
            }
            return mFormatter.Format(format, value).ToString();
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (!Enabled)
            {
                return false;
            }
            int actionMasked = MotionEventCompat.GetActionMasked(e);
            switch (actionMasked)
            {
                case (int)MotionEventActions.Down:
                    mDownX = e.GetX();
                    StartDragging(e, IsInScrollingContainer());
                    break;
                case (int)MotionEventActions.Move:
                    if (IsDragging())
                    {
                        UpdateDragging(e);
                    }
                    else {
                        float x = e.GetX();
                        if (Math.Abs(x - mDownX) > mTouchSlop)
                        {
                            StartDragging(e, false);
                        }
                    }
                    break;
                case (int)MotionEventActions.Up:
                    if (!IsDragging() && mAllowTrackClick)
                    {
                        StartDragging(e, false);
                        UpdateDragging(e);
                    }
                    StopDragging();
                    break;
                case (int)MotionEventActions.Cancel:
                    StopDragging();
                    break;
            }

            return true;
        }

        private bool IsInScrollingContainer()
        {
            return SeekBarCompat.IsInScrollingContainer(Parent);
        }

        private bool StartDragging(MotionEvent ev, bool ignoreTrackIfInScrollContainer)
        {
            Rect bounds = mTempRect;
            mThumb.CopyBounds(bounds);
            //Grow the current thumb rect for a bigger touch area
            bounds.Inset(-mAddedTouchBounds, -mAddedTouchBounds);
            mIsDragging = (bounds.Contains((int)ev.GetX(), (int)ev.GetY()));
            if (!mIsDragging && mAllowTrackClick && !ignoreTrackIfInScrollContainer)
            {
                //If the user clicked outside the thumb, we compute the current position
                //and force an immediate drag to it.
                mIsDragging = true;
                mDragOffset = (bounds.Width() / 2) - mAddedTouchBounds;
                UpdateDragging(ev);
                //As the thumb may have moved, get the bounds again
                mThumb.CopyBounds(bounds);
                bounds.Inset(-mAddedTouchBounds, -mAddedTouchBounds);
            }
            if (mIsDragging)
            {
                Pressed = true;
                AttemptClaimDrag();
                SetHotspot(ev.GetX(), ev.GetY());
                mDragOffset = (int)(ev.GetX() - bounds.Left - mAddedTouchBounds);
                if (mPublicChangeListener != null)
                {
                    mPublicChangeListener.OnStartTrackingTouch(this);
                }
            }
            return mIsDragging;
        }

        private bool IsDragging()
        {
            return mIsDragging;
        }

        private void StopDragging()
        {
            if (mPublicChangeListener != null)
            {
                mPublicChangeListener.OnStopTrackingTouch(this);
            }
            mIsDragging = false;
            Pressed = false;
        }

        public override bool OnKeyDown(Keycode keyCode, KeyEvent e)
        {
            bool handled = false;
            if (Enabled)
            {
                int progress = GetAnimatedProgress();
                switch (keyCode)
                {
                    case Keycode.DpadLeft:
                        handled = true;
                        if (progress <= mMin) break;
                        AnimateSetProgress(progress - mKeyProgressIncrement);
                        break;
                    case Keycode.DpadRight:
                        handled = true;
                        if (progress >= mMax) break;
                        AnimateSetProgress(progress + mKeyProgressIncrement);
                        break;
                }
            }

            return handled || base.OnKeyDown(keyCode, e);
        }

        private int GetAnimatedProgress()
        {
            return IsAnimationRunning() ? GetAnimationTarget() : mValue;
        }

        bool IsAnimationRunning()
        {
            return mPositionAnimator != null && mPositionAnimator.IsRunning;
        }

        void AnimateSetProgress(int progress)
        {
            float curProgress = IsAnimationRunning() ? GetAnimationPosition() : GetProgress();

            if (progress < mMin)
            {
                progress = mMin;
            }
            else if (progress > mMax)
            {
                progress = mMax;
            }
            //setProgressValueOnly(progress);

            if (mPositionAnimator != null)
            {
                mPositionAnimator.Cancel();
            }

            mAnimationTarget = progress;
            mPositionAnimator = AnimatorCompat.Create(curProgress,
                                                      progress, this);
            
            mPositionAnimator.SetDuration(PROGRESS_ANIMATION_DURATION);
            mPositionAnimator.Start();
        }

        public void OnAnimationFrame(float currentValue)
        {
            SetAnimationPosition(currentValue);
        }

        private int GetAnimationTarget()
        {
            return mAnimationTarget;
        }

        void SetAnimationPosition(float position)
        {
            mAnimationPosition = position;
            float currentScale = (position - mMin) / (float)(mMax - mMin);
            UpdateProgressFromAnimation(currentScale);
        }

        float GetAnimationPosition()
        {
            return mAnimationPosition;
        }

        private void UpdateDragging(MotionEvent ev)
        {
            SetHotspot(ev.GetX(), ev.GetY());
            int x = (int)ev.GetX();
            Rect oldBounds = mThumb.Bounds;
            int halfThumb = oldBounds.Width() / 2;
            int addedThumb = mAddedTouchBounds;
            int newX = x - mDragOffset + halfThumb;
            int left = PaddingLeft + halfThumb + addedThumb;
            int right = Width - (PaddingRight + halfThumb + addedThumb);
            if (newX < left)
            {
                newX = left;
            }
            else if (newX > right)
            {
                newX = right;
            }

            int available = right - left;
            float scale = (float)(newX - left) / (float)available;
            if (IsRtl())
            {
                scale = 1f - scale;
            }
            int progress = (int)Math.Round((scale * (mMax - mMin)) + mMin);
            SetProgress(progress, true);
        }

        private void UpdateProgressFromAnimation(float scale)
        {
            Rect bounds = mThumb.Bounds;
            int halfThumb = bounds.Width() / 2;
            int addedThumb = mAddedTouchBounds;
            int left = PaddingLeft + halfThumb + addedThumb;
            int right = Width - (PaddingRight + halfThumb + addedThumb);
            int available = right - left;
            int progress = (int)Math.Round((scale * (mMax - mMin)) + mMin);
            //we don't want to just call setProgress here to avoid the animation being cancelled,
            //and this position is not bound to a real progress value but interpolated
            if (progress != GetProgress())
            {
                mValue = progress;
                NotifyProgress(mValue, true);
                UpdateProgressMessage(progress);
            }
            int thumbPos = (int)(scale * available + 0.5f);
            UpdateThumbPos(thumbPos);
        }

        private void UpdateThumbPosFromCurrentProgress()
        {
            int thumbWidth = mThumb.IntrinsicWidth;
            int addedThumb = mAddedTouchBounds;
            int halfThumb = thumbWidth / 2;
            float scaleDraw = (mValue - mMin) / (float)(mMax - mMin);

            //This doesn't matter if RTL, as we just need the "avaiable" area
            int left = PaddingLeft + halfThumb + addedThumb;
            int right = Width - (PaddingRight + halfThumb + addedThumb);
            int available = right - left;

            int thumbPos = (int)(scaleDraw * available + 0.5f);
            UpdateThumbPos(thumbPos);
        }

        private void UpdateThumbPos(int posX)
        {
            int thumbWidth = mThumb.IntrinsicWidth;
            int halfThumb = thumbWidth / 2;
            int start;
            if (IsRtl())
            {
                start = Width - PaddingRight - mAddedTouchBounds;
                posX = start - posX - thumbWidth;
            }
            else {
                start = PaddingLeft + mAddedTouchBounds;
                posX = start + posX;
            }
            mThumb.CopyBounds(mInvalidateRect);
            mThumb.SetBounds(posX, mInvalidateRect.Top, posX + thumbWidth, mInvalidateRect.Bottom);
            if (IsRtl())
            {
                mScrubber.Bounds.Right = start - halfThumb;
                mScrubber.Bounds.Left = posX + halfThumb;
            }
            else {
                mScrubber.Bounds.Left = start + halfThumb;
                mScrubber.Bounds.Right = posX + halfThumb;
            }
            Rect finalBounds = mTempRect;
            mThumb.CopyBounds(finalBounds);
            if (!IsInEditMode)
            {
                mIndicator.Move(finalBounds.CenterX());
            }

            mInvalidateRect.Inset(-mAddedTouchBounds, -mAddedTouchBounds);
            finalBounds.Inset(-mAddedTouchBounds, -mAddedTouchBounds);
            mInvalidateRect.Union(finalBounds);
            SeekBarCompat.SetHotspotBounds(mRipple, finalBounds.Left, finalBounds.Top, finalBounds.Right, finalBounds.Bottom);
            Invalidate(mInvalidateRect);
        }

        private void SetHotspot(float x, float y)
        {
            DrawableCompat.SetHotspot(mRipple, x, y);
        }

        protected override bool VerifyDrawable(Drawable who)
        {
            return who == mThumb || who == mTrack || who == mScrubber || who == mRipple || base.VerifyDrawable(who);
        }

        private void AttemptClaimDrag()
        {
            IViewParent parent = Parent;
            if (parent != null)
            {
                parent.RequestDisallowInterceptTouchEvent(true);
            }
        }

        private void Run ()
        {
            ShowFloater();
        }

        private void ShowFloater()
        {
            if (!IsInEditMode)
            {
                mThumb.AnimateToPressed();
                mIndicator.showIndicator(this, mThumb.Bounds);
                NotifyBubble(true);
            }
        }

        private void HideFloater()
        {
            RemoveCallbacks(mShowIndicatorRunnable);
            if (!IsInEditMode)
            {
                mIndicator.Dismiss();
                NotifyBubble(false);
            }
        }

        public void OnClosingComplete()
        {
            mThumb.AnimateToNormal();
        }

        public void OnOpeningComplete()
        {
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();

            RemoveCallbacks(mShowIndicatorRunnable);
            if (!IsInEditMode)
            {
                mIndicator.DismissComplete();
            }
        }

        public bool IsRtl()
        {
            return (ViewCompat.GetLayoutDirection(this) == (int)LayoutDirectionRtl) && mMirrorForRtl;
        }

        protected override IParcelable OnSaveInstanceState()
        {
            var superState = base.OnSaveInstanceState();
            var state = new CustomState(superState);
            state.progress = GetProgress();
            state.max = mMax;
            state.min = mMin;
            return base.OnSaveInstanceState();
        }

        protected override void OnRestoreInstanceState(IParcelable state)
        {
            if (state == null || !(state is CustomState)) 
            {
                base.OnRestoreInstanceState(state);
                return;
            }

            var customState = (CustomState)state;
            SetMin(customState.min);
            SetMax(customState.max);
            SetProgress(customState.progress, false);

            base.OnRestoreInstanceState(customState.SuperState);
        }

        private class CustomState : BaseSavedState
        {
            public int progress;
            public int max;
            public int min;

            public CustomState(Parcel source)
                : base (source)
            {
                progress = source.ReadInt();
                max = source.ReadInt();
                min = source.ReadInt();
            }

            public CustomState(IParcelable superState)
                : base (superState)
            {
                
            }

            public override void WriteToParcel(Parcel dest, ParcelableWriteFlags flags)
            {
                base.WriteToParcel(dest, flags);

                dest.WriteInt(progress);
                dest.WriteInt(max);
                dest.WriteInt(min);
            }

        //    public static Creator<CustomState> CREATOR =
        //            new Creator<CustomState>() {

        //            @Override
        //                public CustomState[] newArray(int size)
        //    {
        //        return new CustomState[size];
        //    }

        //    @Override
        //            public CustomState createFromParcel(Parcel incoming)
        //    {
        //        return new CustomState(incoming);
        //    }
        //};
        }
    }
}

