using System;

namespace Timer
{
    class CustomTimer
    {
        public event EventHandler MinutesTick;
        public event EventHandler SecondsTick;
        public event EventHandler TimerEnded;

        public int Minutes { get { return minutes; } }
        public int Seconds { get { return seconds; } }
        public double PercentSecondsRemaining
        {
            get
            {
                return (double) (initialTotalSeconds - SecondsRemaining) / initialTotalSeconds;
            }
        }

        private int minutes;
        private int seconds;
        private int initialTotalSeconds;
        private int SecondsRemaining { get { return minutes * 60 + seconds; } }

        public CustomTimer(int minutes, int seconds)
        {
            if (minutes < 0 || seconds < 0)
                throw new ArgumentException("Minutes and seconds must be greater than 0");

            this.minutes = minutes + seconds / 60;
            this.seconds = seconds % 60;
            this.initialTotalSeconds = minutes * 60 + seconds;
        }

        public void DecrementSeconds()
        {
            if (seconds != 0)
                seconds--;
            else
            {
                minutes--;
                seconds = 59;
            }

            OnSecondsTick(EventArgs.Empty);

            if (minutes > 0 || seconds > 0)
            {
                if (seconds == 0)
                {
                    minutes--;
                    seconds = 60;

                    OnMinutesTick(EventArgs.Empty);
                }
            }
            else
            {
                OnTimerEnded(EventArgs.Empty);
            }
        }

        public bool IsTimerAllZero()
        {
            return minutes == 0 && seconds == 0;
        }

        protected virtual void OnSecondsTick(EventArgs e)
        {
            SecondsTick?.Invoke(this, e);
        }

        protected virtual void OnTimerEnded(EventArgs e)
        {
            TimerEnded?.Invoke(this, e);
        }

        protected virtual void OnMinutesTick(EventArgs e)
        {
            MinutesTick?.Invoke(this, e);
        }
    }
}
