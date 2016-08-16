using System;
using Android.Content.Res;
using Android.Graphics;
namespace DiscreteSeekBar.Internal.Drawable
{
    public class TrackOvalDrawable : StateDrawable
    {
        private RectF mRectF = new RectF();
        
        public TrackOvalDrawable(ColorStateList tintStateList)
            : base (tintStateList)
        {
        }

        protected override void DoDraw(Canvas canvas, Paint paint)
        {
            mRectF.Set(Bounds);
            canvas.DrawOval(mRectF, paint);
        }
    }
}

