using System;
using Android.OS;
namespace DSB.Internal.Compat
{
    public interface IAnimationFrameUpdateListener
    {
        void OnAnimationFrame(float currentValue);
    }
    
    public abstract class AnimatorCompat : Java.Lang.Object
    {
        public abstract bool IsRunning { get; }

        public abstract void Cancel();

        public abstract void SetDuration(int progressAnimationDuration);

        public abstract void Start();

        public static AnimatorCompat Create (float start, float end, IAnimationFrameUpdateListener listener)
        {
            if (Build.VERSION.SdkInt == BuildVersionCodes.Honeycomb)
                return new AnimatorCompatV11(start, end, listener);
            else
                return new AnimatorCompatBase(start, end, listener);
        }
    }

    public class AnimatorCompatBase : AnimatorCompat
    {
        private IAnimationFrameUpdateListener _listener;
        private float _endValue;

        protected bool _isRunning = false;
        public override bool IsRunning
        {
            get
            {
                return _isRunning;
            }
        }

        public AnimatorCompatBase (float start, float end, IAnimationFrameUpdateListener listener)
        {
            _listener = listener;
            _endValue = end;
        }

        public override void Cancel()
        {
            
        }

        public override void SetDuration(int progressAnimationDuration)
        {
            
        }

        public override void Start()
        {
            _listener.OnAnimationFrame(_endValue);
        }
    }
}

