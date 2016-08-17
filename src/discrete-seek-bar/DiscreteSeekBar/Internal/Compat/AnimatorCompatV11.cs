using System;
using Android.Annotation;
using Android.OS;
using Android.Animation;
namespace DSB.Internal.Compat
{
    [TargetApi(Value = (int)BuildVersionCodes.Honeycomb)]
    public class AnimatorCompatV11 : AnimatorCompat, ValueAnimator.IAnimatorUpdateListener
    {
        private ValueAnimator _animator;
        private IAnimationFrameUpdateListener _listener;

        public override bool IsRunning
        {
            get
            {
                return _animator.IsRunning;
            }
        }
        
        public AnimatorCompatV11(float start, float end, IAnimationFrameUpdateListener listener)
        : base ()
        {
            _listener = listener;

            _animator = ValueAnimator.OfFloat(start, end);
            _animator.AddUpdateListener(this);
        }

        public override void Cancel()
        {
            _animator.Cancel();
        }

        public override void SetDuration(int progressAnimationDuration)
        {
            _animator.SetDuration(progressAnimationDuration);
        }

        public override void Start()
        {
            _animator.Start();
        }

        public void OnAnimationUpdate(ValueAnimator animation)
        {
            _listener.OnAnimationFrame((float)animation.AnimatedValue);
        }
    }
}

