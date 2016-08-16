using System;
using Android.Content.Res;
namespace DiscreteSeekBar.Internal.Drawable
{
    public class TrackRectDrawable : StateDrawable
    {
        public TrackRectDrawable(ColorStateList tinsStateList)
            : base (tinsStateList)
        {
        }

        protected override void DoDraw(Android.Graphics.Canvas canvas, Android.Graphics.Paint paint)
        {
            canvas.DrawRect(Bounds, paint);
        }
    }
}

