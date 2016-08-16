using System;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Android.Widget;
using DiscreteSeekBar.Internal.Compat;
using DiscreteSeekBar.Internal.Drawable;
namespace DiscreteSeekBar.Internal
{
    public class Marker : ViewGroup, IMarkerAnimationListener
    {
        private const int PADDING_DP = 4;
        private const int ELEVATION_DP = 8;
        //The TextView to show the info
        private TextView mNumber;
        //The max width of this View
        private int mWidth;
        //some distance between the thumb and our bubble marker.
        //This will be added to our measured height
        private int mSeparation;
        MarkerDrawable mMarkerDrawable;
        
        public Marker(Context context, IAttributeSet attrs, int defStyleAttr, String maxValue, int thumbSize, int separation)
            : base (context, attrs, defStyleAttr)
        {
            Visibility = ViewStates.Visible;

            DisplayMetrics displayMetrics = context.Resources.DisplayMetrics;
            TypedArray a = context.ObtainStyledAttributes(attrs, Resource.Styleable.DiscreteSeekBar,
                                                          Resource.Attribute.discreteSeekBarStyle, Resource.Style.Widget_DiscreteSeekBar);

            int padding = (int)(PADDING_DP * displayMetrics.Density) * 2;
            int textAppearanceId = a.GetResourceId(Resource.Styleable.DiscreteSeekBar_dsb_indicatorTextAppearance,
                    Resource.Style.Widget_DiscreteIndicatorTextAppearance);
            mNumber = new TextView(context);
            //Add some padding to this textView so the bubble has some space to breath
            mNumber.SetPadding(padding, 0, padding, 0);
            mNumber.SetTextAppearance(context, textAppearanceId);
            mNumber.Gravity = GravityFlags.Center;
            mNumber.Text = maxValue;
            mNumber.SetMaxLines(1);
            mNumber.SetSingleLine(true);
            SeekBarCompat.SetTextDirection(mNumber, (int)TextDirectionLocale);
            mNumber.Visibility = ViewStates.Invisible;

            //add some padding for the elevation shadow not to be clipped
            //I'm sure there are better ways of doing this...
            SetPadding(padding, padding, padding, padding);

            ResetSizes(maxValue);

            mSeparation = separation;
            ColorStateList color = a.GetColorStateList(Resource.Styleable.DiscreteSeekBar_dsb_indicatorColor);
            mMarkerDrawable = new MarkerDrawable(color, thumbSize);
            mMarkerDrawable.SetCallback(this);
            mMarkerDrawable.SetMarkerListener(this);
            mMarkerDrawable.SetExternalOffset(padding);

            //Elevation for anroid 5+
            float elevation = a.GetDimension(Resource.Styleable.DiscreteSeekBar_dsb_indicatorElevation, ELEVATION_DP * displayMetrics.Density);
            ViewCompat.SetElevation(this, elevation);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
            {
                SeekBarCompat.SetOutlineProvider(this, mMarkerDrawable);
            }
            a.Recycle();
        }

        public void ResetSizes(String maxValue)
        {
            DisplayMetrics displayMetrics = Resources.DisplayMetrics;
            //Account for negative numbers... is there any proper way of getting the biggest string between our range????
            mNumber.Text = $"-{maxValue}";
            //Do a first forced measure call for the TextView (with the biggest text content),
            //to calculate the max width and use always the same.
            //this avoids the TextView from shrinking and growing when the text content changes
            int wSpec = MeasureSpec.MakeMeasureSpec(displayMetrics.WidthPixels, MeasureSpecMode.AtMost);
            int hSpec = MeasureSpec.MakeMeasureSpec(displayMetrics.HeightPixels, MeasureSpecMode.AtMost);
            mNumber.Measure(wSpec, hSpec);
            mWidth = Math.Max(mNumber.MeasuredWidth, mNumber.MeasuredHeight);
            RemoveView(mNumber);
            AddView(mNumber, new FrameLayout.LayoutParams(mWidth, mWidth, GravityFlags.Left | GravityFlags.Top));
        }

        protected override void DispatchDraw(Android.Graphics.Canvas canvas)
        {
            mMarkerDrawable.Draw(canvas);
            base.DispatchDraw(canvas);
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            MeasureChildren(widthMeasureSpec, heightMeasureSpec);
            int widthSize = mWidth + PaddingLeft + PaddingRight;
            int heightSize = mWidth + PaddingTop + PaddingBottom;
            //This diff is the basic calculation of the difference between
            //a square side size and its diagonal
            //this helps us account for the visual offset created by MarkerDrawable
            //when leaving one of the corners un-rounded
            int diff = (int)((1.41f * mWidth) - mWidth) / 2;
            SetMeasuredDimension(widthSize, heightSize + diff + mSeparation);
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            int left = PaddingLeft;
            int top = PaddingTop;
            int right = Width - PaddingRight;
            int bottom = Height - PaddingBottom;
            //the TetView is always layout at the top
            mNumber.Layout(left, top, left + mWidth, top + mWidth);
            //the MarkerDrawable uses the whole view, it will adapt itself...
            // or it seems so...
            mMarkerDrawable.SetBounds(left, top, right, bottom);
        }

        protected override bool VerifyDrawable(Android.Graphics.Drawables.Drawable who)
        {
            return who == mMarkerDrawable || base.VerifyDrawable(who);
        }

        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();

            //HACK: Sometimes, the animateOpen() call is made before the View is attached
            //so the drawable cannot schedule itself to run the animation
            //I think we can call it here safely.
            //I've seen it happen in android 2.3.7
            AnimateOpen();
        }

        public void SetValue(string value)
        {
            mNumber.Text = value;
        }

        public string getValue()
        {
            return mNumber.Text;
        }

        public void AnimateOpen()
        {
            mMarkerDrawable.Stop();
            mMarkerDrawable.AnimateToPressed();
        }

        public void AnimateClose()
        {
            mMarkerDrawable.Stop();
            mNumber.Visibility = ViewStates.Invisible;
            mMarkerDrawable.AnimateToNormal();
        }

        public void SetColors(int startColor, int endColor)
        {
            mMarkerDrawable.SetColors(startColor, endColor);
        }

        public void OnOpeningComplete()
        {
            mNumber.Visibility = ViewStates.Visible;
            if (Parent is IMarkerAnimationListener)
                ((IMarkerAnimationListener)Parent).OnOpeningComplete();
        }

        public void OnClosingComplete()
        {
            if (Parent is IMarkerAnimationListener)
                ((IMarkerAnimationListener)Parent).OnClosingComplete();
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();
            mMarkerDrawable.Stop();
        }
    }
}

