using System;
using Android.Graphics.Drawables;
using Android.Views.Animations;
using Android.Content.Res;
using System.Diagnostics.Contracts;
using Android.Graphics;
using Java.Lang;
using Android.OS;

namespace DiscreteSeekBar.Internal.Drawable
{
    public class AlmostRippleDrawable : StateDrawable, IAnimatable
    {
        private const long FRAME_DURATION = 1000 / 60;
        private const int ANIMATION_DURATION = 250;

        private const float INACTIVE_SCALE = 0f;
        private const float ACTIVE_SCALE = 1f;
        private float _currentScale = INACTIVE_SCALE;
        private IInterpolator _interpolator;
        private long _startTime;
        private bool _reverse = false;
        private bool _running = false;
        private int _duration = ANIMATION_DURATION;
        private float _animationInitialValue;

        private int _pressedColor;
        private int _focusedColor;
        private int _disabledColor;
        private int _rippleColor;
        private int _rippleBgColor;

        private Runnable _updater;

        public AlmostRippleDrawable(ColorStateList tintStateList)
            : base (tintStateList)
        {
            Contract.Requires(tintStateList != null);

            _interpolator = new AccelerateDecelerateInterpolator();
            SetColor(tintStateList);

            _updater = new Runnable(Run);
        }

        public void SetColor (ColorStateList tintStateList)
        {
            Contract.Requires(tintStateList != null);

            int defaultColor = tintStateList.DefaultColor;
            _focusedColor = tintStateList.GetColorForState(new int[] { Android.Resource.Attribute.StateEnabled, Android.Resource.Attribute.StateFocused }, new Color(defaultColor));
            _pressedColor = tintStateList.GetColorForState(new int[] { Android.Resource.Attribute.StateEnabled, Android.Resource.Attribute.StatePressed }, new Color(defaultColor));
            _disabledColor = tintStateList.GetColorForState(new int[] { Android.Resource.Attribute.StateEnabled }, new Color(defaultColor));

            //The ripple should be partially transparent
            _focusedColor = GetModulatedAlphaColor(130, _focusedColor);
            _pressedColor = GetModulatedAlphaColor(130, _pressedColor);
            _disabledColor = GetModulatedAlphaColor(130, _disabledColor);
        }

        private static int GetModulatedAlphaColor(int alphaValue, int originalColor)
        {
            int alpha = Color.GetAlphaComponent(originalColor);
            int scale = alphaValue + (alphaValue >> 7);
            alpha = alpha * scale >> 8;
            return Color.Argb(alpha, Color.GetRedComponent(originalColor), Color.GetGreenComponent(originalColor), Color.GetBlueComponent(originalColor)).ToArgb();
        }

        protected override void DoDraw(Canvas canvas, Paint paint)
        {
            Rect bounds = Bounds;
            int size = System.Math.Min(bounds.Width(), bounds.Height());
            float scale = _currentScale;
            int rippleColor = _rippleColor;
            int bgColor = _rippleBgColor;
            float radius = (size / 2);
            float radiusAnimated = radius * scale;
            if (scale > INACTIVE_SCALE)
            {
                if (bgColor != 0)
                {
                    paint.Color = new Color(bgColor);
                    paint.Alpha = (DecreasedAlpha(Color.GetAlphaComponent(bgColor)));
                    canvas.DrawCircle(bounds.CenterX(), bounds.CenterY(), radius, paint);
                }
                if (rippleColor != 0)
                {
                    paint.Color = new Color(rippleColor);
                    paint.Alpha = (ModulateAlpha(Color.GetAlphaComponent(rippleColor)));
                    canvas.DrawCircle(bounds.CenterX(), bounds.CenterY(), radiusAnimated, paint);
                }
            }
        }

        private int DecreasedAlpha(int alpha)
        {
            int scale = 100 + (100 >> 7);
            return alpha * scale >> 8;
        }

        public override bool SetState(int[] stateSet)
        {
            int[] oldState = GetState();
            bool oldPressed = false;
            foreach (int i in oldState)
            {
                if (i == Android.Resource.Attribute.StatePressed)
                {
                    oldPressed = true;
                }
            }
            base.SetState(stateSet);
            bool focused = false;
            bool pressed = false;
            bool disabled = true;
            foreach (int i in stateSet)
            {
                if (i == Android.Resource.Attribute.StateFocused)
                {
                    focused = true;
                }
                else if (i == Android.Resource.Attribute.StatePressed)
                {
                    pressed = true;
                }
                else if (i == Android.Resource.Attribute.StateEnabled)
                {
                    disabled = false;
                }
            }

            if (disabled)
            {
                UnscheduleSelf(_updater);
                _rippleColor = _disabledColor;
                _rippleBgColor = 0;
                _currentScale = ACTIVE_SCALE / 2;
                InvalidateSelf();
            }
            else {
                if (pressed)
                {
                    AnimateToPressed();
                    _rippleColor = _rippleBgColor = _pressedColor;
                }
                else if (oldPressed)
                {
                    _rippleColor = _rippleBgColor = _pressedColor;
                    AnimateToNormal();
                }
                else if (focused)
                {
                    _rippleColor = _focusedColor;
                    _rippleBgColor = 0;
                    _currentScale = ACTIVE_SCALE;
                    InvalidateSelf();
                }
                else {
                    _rippleColor = 0;
                    _rippleBgColor = 0;
                    _currentScale = INACTIVE_SCALE;
                    InvalidateSelf();
                }
            }
            return true;
        }

        public void AnimateToPressed()
        {
            UnscheduleSelf(_updater);
            if (_currentScale < ACTIVE_SCALE)
            {
                _reverse = false;
                _running = true;
                _animationInitialValue = _currentScale;
                float durationFactor = 1f - ((_animationInitialValue - INACTIVE_SCALE) / (ACTIVE_SCALE - INACTIVE_SCALE));
                _duration = (int)(ANIMATION_DURATION * durationFactor);
                _startTime = SystemClock.UptimeMillis();
                ScheduleSelf(_updater, _startTime + FRAME_DURATION);
            }
        }

        public void AnimateToNormal()
        {
            UnscheduleSelf(_updater);
            if (_currentScale > INACTIVE_SCALE)
            {
                _reverse = true;
                _running = true;
                _animationInitialValue = _currentScale;
                float durationFactor = 1f - ((_animationInitialValue - ACTIVE_SCALE) / (INACTIVE_SCALE - ACTIVE_SCALE));
                _duration = (int)(ANIMATION_DURATION * durationFactor);
                _startTime = SystemClock.UptimeMillis();
                ScheduleSelf(_updater, _startTime + FRAME_DURATION);
            }
        }

        private void UpdateAnimation(float factor)
        {
            float initial = _animationInitialValue;
            float destination = _reverse ? INACTIVE_SCALE : ACTIVE_SCALE;
            _currentScale = initial + (destination - initial) * factor;
            InvalidateSelf();
        }

        private void Run ()
        {
            long currentTime = SystemClock.UptimeMillis();
            long diff = currentTime - _startTime;
            if (diff < _duration)
            {
                float interpolation = _interpolator.GetInterpolation((float)diff / (float)_duration);
                ScheduleSelf(_updater, currentTime + FRAME_DURATION);
                UpdateAnimation(interpolation);
            }
            else {
                UnscheduleSelf(_updater);
                _running = false;
                UpdateAnimation(1f);
            }
        }

        public void Start ()
        {
            
        }

        public void Stop()
        {
            
        }

        public bool IsRunning { get { return _running; } }
    }
}

