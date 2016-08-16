using System;
using Android.Graphics.Drawables;
using Android.Content.Res;
using Android.Graphics;
using System.Diagnostics.Contracts;

namespace DiscreteSeekBar.Internal.Drawable
{
    public abstract class StateDrawable : Android.Graphics.Drawables.Drawable
    {
        private ColorStateList _tintStateList;
        private Paint _paint;
        private int _currentColor;
        private int _alpha = 255;

        public override bool IsStateful { get { return _tintStateList.IsStateful || base.IsStateful; } }

        public override int Opacity { get { return (int)Format.Translucent; } }

        public override int Alpha
        {
            get
            {
                return _alpha;
            }
            set
            {
                SetAlpha(value);
            }
        }

        protected abstract void DoDraw(Canvas canvas, Paint paint);

        public StateDrawable (ColorStateList tintStateList)
        : base ()
        {
            Contract.Requires(tintStateList != null);

            SetColorStateList(tintStateList);
            _paint = new Paint(PaintFlags.AntiAlias);
        }

        public void SetColorStateList (ColorStateList tintStateList)
        {
            Contract.Requires(tintStateList != null);

            _tintStateList = tintStateList;
            _currentColor = tintStateList.DefaultColor;
        }

        public bool UpdateTint(int[] state)
        {
            var color = _tintStateList.GetColorForState(state, new Color(_currentColor));
            if (color != _currentColor)
            {
                _currentColor = color;
                InvalidateSelf();
                return true;
            }
            return false;
        }

        public override bool SetState(int[] stateSet)
        {
            var handled = base.SetState(stateSet);
            handled = UpdateTint(stateSet) || handled;
            return handled;
        }

        public override void Draw(Canvas canvas)
        {
            _paint.Color = new Color(_currentColor);
            var alpha = ModulateAlpha(Color.GetAlphaComponent(_currentColor));
            _paint.Alpha = alpha;
            DoDraw(canvas, _paint);
        }

        public override void SetAlpha(int alpha)
        {
            _alpha = alpha;
            InvalidateSelf();
        }

        public override void SetColorFilter(ColorFilter colorFilter)
        {
            _paint.SetColorFilter(colorFilter);
        }

        protected int ModulateAlpha(int alpha)
        {
            int scale = _alpha + (_alpha >> 7);
            return alpha * scale >> 8;
        }
    }
}

