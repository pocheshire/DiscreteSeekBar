using Android.Views;
using DiscreteSeekBar.Internal.Drawable;
using Android.Content.Res;
using Android.Graphics.Drawables;
using Android.Widget;

namespace DiscreteSeekBar.Internal.Compat
{
    internal class SeekBarViewOutlineProdiver : ViewOutlineProvider
    {
        public MarkerDrawable MarkerDrawable { get; set; }

        public override void GetOutline(View view, Android.Graphics.Outline outline)
        {
            outline.SetConvexPath(MarkerDrawable.GetPath());
        }
    }
    
    public class SeekBarCompatDontCrash
    {
        public static void SetOutlineProvider(View marker, MarkerDrawable markerDrawable)
        {
            marker.OutlineProvider = new SeekBarViewOutlineProdiver { MarkerDrawable = markerDrawable };
        }

        public static Android.Graphics.Drawables.Drawable GetRipple (ColorStateList colorStateList)
        {
            return new RippleDrawable(colorStateList, null, null);
        }

        public static void SetBackground (View view, Android.Graphics.Drawables.Drawable background)
        {
            view.Background = background;
        }

        public static void SetTextDirection(TextView number, int textDirection)
        {
            number.TextDirection = (TextDirection)textDirection;
        }

        public static bool IsInScrollingContainer (IViewParent p)
        {
            while (p != null && p is ViewGroup)
            {
                if (((ViewGroup)p).ShouldDelayChildPressedState())
                    return true;

                p = p.Parent;
            }

            return false;
        }

        public static bool IsHardwareAccelerated (View view)
        {
            return view.IsHardwareAccelerated;
        }
    }
}