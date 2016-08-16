using System;
using Android.Views;
using DiscreteSeekBar.Internal.Drawable;
using Android.OS;
using Android.Content.Res;
using Drawables = Android.Graphics.Drawables;
using Android.Support.V4.Graphics.Drawable;
using Android.Widget;

namespace DiscreteSeekBar.Internal.Compat
{
    public static class SeekBarCompat
    {
        public static void SetOutlineProvider(View view, MarkerDrawable markerDrawable)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                SeekBarCompatDontCrash.SetOutlineProvider(view, markerDrawable);
        }

        public static Android.Graphics.Drawables.Drawable GetRipple(ColorStateList colorStateList)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                return SeekBarCompatDontCrash.GetRipple(colorStateList);

            return new AlmostRippleDrawable(colorStateList);
        }

        public static void SetRippleColor(Android.Graphics.Drawables.Drawable drawable, ColorStateList colorStateList)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                ((Drawables.RippleDrawable)drawable).SetColor(colorStateList);
            else
                ((AlmostRippleDrawable)drawable).SetColor(colorStateList);
        }

        public static void SetHotspotBounds(Drawables.Drawable drawable, int left, int top, int right, int bottom)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                //We don't want the full size rect, Lollipop ripple would be too big
                int size = (right - left) / 8;
                DrawableCompat.SetHotspotBounds(drawable, left + size, top + size, right - size, bottom - size);
            }
            else 
            {
                drawable.SetBounds(left, top, right, bottom);
            }
        }

        [Obsolete("deprecation")]
        public static void SetBackground(View view, Drawables.Drawable background)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                SeekBarCompatDontCrash.SetBackground(view, background);
            }
            else
            {
                view.SetBackgroundDrawable(background);
            }
        }

        public static void SetTextDirection(TextView textView, int textDirection)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.JellyBeanMr1)
            {
                SeekBarCompatDontCrash.SetTextDirection(textView, textDirection);
            }
        }

        public static bool IsInScrollingContainer(IViewParent p)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.IceCreamSandwich)
            {
                return SeekBarCompatDontCrash.IsInScrollingContainer(p);
            }
            return false;
        }

        public static bool IsHardwareAccelerated(View view)
        {
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Honeycomb)
            {
                return SeekBarCompatDontCrash.IsHardwareAccelerated(view);
            }
            return false;
        }
    }
}

