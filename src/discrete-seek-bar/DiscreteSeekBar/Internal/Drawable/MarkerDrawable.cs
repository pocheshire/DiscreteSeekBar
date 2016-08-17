using System;
using Android.Graphics;
using Android.Content.Res;
using System.Diagnostics.Contracts;
using Android.Views.Animations;
using Android.OS;
using Java.Lang;

namespace DSB.Internal.Drawable
{
    public interface IMarkerAnimationListener
    {
        void OnClosingComplete();

        void OnOpeningComplete();
    }
    
    public class MarkerDrawable : StateDrawable, Android.Graphics.Drawables.IAnimatable
    {
        private const long FRAME_DURATION = 1000 / 60;
        private const int ANIMATION_DURATION = 250;

        private float _currentScale = 0f;
        private IInterpolator _interpolator;
        private long _startTime;
        private bool _reverse = false;
        private bool _running = false;
        private int _duration = ANIMATION_DURATION;

        private float _closedStateSize;

        private float _animationInitialValue;

        private int _externalOffset;

        private int _startColor;
        private int _endColor;

        protected Path _path = new Path();
        protected RectF _rect = new RectF();
        protected Matrix _matrix = new Matrix();

        private IMarkerAnimationListener _markerListener;

        private Runnable _updater;

        public bool IsRunning
        {
            get
            {
                return _running;
            }
        }

        public MarkerDrawable(ColorStateList tintList, int closedSize)
            : base (tintList)
        {
            Contract.Requires(tintList != null);

            _interpolator = new AccelerateDecelerateInterpolator();
            _closedStateSize = closedSize;
            _startColor = tintList.GetColorForState(new int[] { Android.Resource.Attribute.StateEnabled, Android.Resource.Attribute.StatePressed }, new Color(tintList.DefaultColor));
            _endColor = tintList.DefaultColor;

            _updater = new Runnable(Run);
        }

        public void SetExternalOffset (int offset)
        {
            _externalOffset = offset;
        }

        public void SetColors(int startColor, int endColor)
        {
            _startColor = startColor;
            _endColor = endColor;
        }

        protected override void DoDraw(Canvas canvas, Paint paint)
        {
            if (!_path.IsEmpty)
            {
                paint.SetStyle(Paint.Style.Fill);
                var color = BlendColors(_startColor, _endColor, _currentScale);
                paint.Color = new Color(color);
                canvas.DrawPath(_path, paint);
            }
        }

        public Path GetPath ()
        {
            return _path;
        }

        protected override void OnBoundsChange(Rect bounds)
        {
            base.OnBoundsChange(bounds);
            ComputePath(bounds);
        }

        private void ComputePath (Rect bounds)
        {
            var currentScale = _currentScale;
            var path = _path;
            var rect = _rect;
            var matrix = _matrix;

            path.Reset();
            int totalSize = System.Math.Min(bounds.Width(), bounds.Height());

            var initial = _closedStateSize;
            var destination = totalSize;
            var currentSize = initial + (destination - initial) * currentScale;

            var halfSize = currentSize / 2f;
            var inverseScale = 1f - _currentScale;
            var cornerSize = halfSize * inverseScale;
            float[] corners = new float[] { halfSize, halfSize, halfSize, halfSize, halfSize, halfSize, cornerSize, cornerSize };
            rect.Set(bounds.Left, bounds.Top, bounds.Left + currentSize, bounds.Top + currentSize);
            path.AddRoundRect(rect, corners, Path.Direction.Ccw);
            matrix.Reset();
            matrix.PostRotate(-45, bounds.Left + halfSize, bounds.Top + halfSize);
            matrix.PostTranslate((bounds.Width() - currentSize) / 2, 0);
            var hDiff = (bounds.Bottom - currentSize - _externalOffset) * inverseScale;
            matrix.PostTranslate(0, hDiff);
            path.Transform(matrix);
        }

        private void UpdateAnimation (float factor)
        {
            var initial = _animationInitialValue;
            var destination = _reverse ? 0f : 1f;
            _currentScale = initial + (destination - initial) * factor;
            ComputePath(Bounds);
            InvalidateSelf();
        }

        public void AnimateToPressed()
        {
            UnscheduleSelf(_updater);
            _reverse = false;
            if (_currentScale < 1)
            {
                _running = true;
                _animationInitialValue = _currentScale;
                var durationFactor = 1f - _currentScale;
                _duration = (int)(ANIMATION_DURATION * durationFactor);
                _startTime = SystemClock.UptimeMillis();
                ScheduleSelf(_updater, _startTime + FRAME_DURATION);
            }
            else
                NotifyFinishedToListener();
        }

        public void AnimateToNormal()
        {
            _reverse = true;
            UnscheduleSelf(_updater);
            if (_currentScale > 0)
            {
                _running = true;
                _animationInitialValue = _currentScale;
                var durationFactor = 1f - _currentScale;
                _duration = ANIMATION_DURATION - (int)(ANIMATION_DURATION * durationFactor);
                _startTime = SystemClock.UptimeMillis();
                ScheduleSelf(_updater, _startTime + FRAME_DURATION);
            }
            else
                NotifyFinishedToListener();
        }

        public void Run()
        {
            var currentTime = SystemClock.UptimeMillis();
            var diff = currentTime - _startTime;

            if (diff < _duration)
            {
                var interpolation = _interpolator.GetInterpolation((float)diff / (float)_duration);
                ScheduleSelf(_updater, currentTime + FRAME_DURATION);
                UpdateAnimation(interpolation);
            }
            else
            {
                UnscheduleSelf(_updater);
                _running = false;
                UpdateAnimation(1f);
                NotifyFinishedToListener();
            }
        }

        public void SetMarkerListener (IMarkerAnimationListener listener)
        {
            _markerListener = listener;
        }

        private void NotifyFinishedToListener()
        {
            if (_markerListener != null)
            {
                if (_reverse)
                    _markerListener.OnClosingComplete();
                else
                    _markerListener.OnOpeningComplete();
            }
        }

        public void Start()
        {
            
        }

        public void Stop()
        {
            UnscheduleSelf(_updater);
        }

        private static int BlendColors(int color1, int color2, float factor)
        {
            float inverseFactor = 1f - factor;

            float a = (Color.GetAlphaComponent(color1) * factor) + (Color.GetAlphaComponent(color2) * inverseFactor);
            float r = (Color.GetRedComponent(color1) * factor) + (Color.GetRedComponent(color2) * inverseFactor);
            float g = (Color.GetGreenComponent(color1) * factor) + (Color.GetGreenComponent(color2) * inverseFactor);
            float b = (Color.GetBlueComponent(color1) * factor) + (Color.GetBlueComponent(color2) * inverseFactor);

            return Color.Argb((int)a, (int)r, (int)g, (int)b).ToArgb();
        }
    }
}