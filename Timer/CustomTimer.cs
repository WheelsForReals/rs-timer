using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
                return (double) (initialTotalSeconds - secondsRemaining) / initialTotalSeconds;
            }
        }

        private int minutes;
        private int seconds;
        private int initialTotalSeconds;
        private int secondsRemaining { get { return minutes * 60 + seconds; } }

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

            OnSecondsTick(new EventArgs());

            if (minutes + seconds > 0)
            {
                if (seconds == 0)
                {
                    minutes--;
                    seconds = 60;

                    OnMinutesTick(new EventArgs());
                }
            }
            else
            {
                OnTimerEnded(new EventArgs());
            }
        }

        public bool IsTimerAllZero()
        {
            return minutes + seconds == 0;
        }

        protected virtual void OnSecondsTick(EventArgs e)
        {
            EventHandler handler = SecondsTick;

            if (handler != null)
                handler(this, e);
        }

        protected virtual void OnTimerEnded(EventArgs e)
        {
            EventHandler handler = TimerEnded;

            if (handler != null)
                handler(this, e);
        }

        protected virtual void OnMinutesTick(EventArgs e)
        {
            EventHandler handler = MinutesTick;

            if (handler != null)
                handler(this, e);
        }
    }
}
