using System;
using Android.Graphics.Drawables;
using Android.Content.Res;
using Java.Lang;
using Android.Graphics;
using Android.OS;

namespace DiscreteSeekBar.Internal.Drawable
{
    public class ThumbDrawable : StateDrawable, IAnimatable
    {
        public const int DEFAULT_SIZE_DP = 12;
        private int mSize;
        private bool mOpen;
        private bool mRunning;

        private Runnable opener;

        public override int IntrinsicWidth
        {
            get
            {
                return mSize;
            }
        }

        public override int IntrinsicHeight
        {
            get
            {
                return mSize;
            }
        }

        public ThumbDrawable(ColorStateList tintStateList, int size)
            : base (tintStateList)
        {
            mSize = size;

            opener = new Runnable(Run);
        }

        protected override void DoDraw(Canvas canvas, Paint paint)
        {
            if (!mOpen)
            {
                Rect bounds = Bounds;
                float radius = (mSize / 2);
                canvas.DrawCircle(bounds.CenterX(), bounds.CenterY(), radius, paint);
            }
        }

        public void AnimateToPressed()
        {
            ScheduleSelf(opener, SystemClock.UptimeMillis() + 100);
            mRunning = true;
        }

        public void AnimateToNormal()
        {
            mOpen = false;
            mRunning = false;
            UnscheduleSelf(opener);
            InvalidateSelf();
        }

        private void Run()
        {
            mOpen = true;
            InvalidateSelf();
            mRunning = false;
        }

        public void Start()
        {
            //NOOP
        }

        public void Stop()
        {
            AnimateToNormal();
        }

        public bool IsRunning
        {
            get
            {
                return mRunning;
            }
        }
    }
}

