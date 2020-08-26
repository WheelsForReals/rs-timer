using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Shell;
using System.Runtime.InteropServices;
using Gma.System.MouseKeyHook;
using System.Media;
using System.Windows.Interop;

namespace Timer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowRect(IntPtr hwnd, out Rect lpRect);

        public struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        CustomTimer customTimer;
        DispatcherTimer dispatcherTimer;
        IKeyboardMouseEvents globalHook;
        bool firstPress = true;
        readonly string[] equalsMatchWindowTitles = { "RuneScape", "Old School RuneScape", "RuneLite" };
        readonly string[] startsWithMatchWindowTitles = { "RuneLite - " };

        public MainWindow()
        {
            InitializeComponent();
            InitializeDispatcherTimer();
            RestoreSettings();
        }

        /// <summary>
        /// Returns the title of a window
        /// </summary>
        /// <param name="hWnd">Handle for the window to get the title of</param>
        /// <returns>The window's title</returns>
        private static string GetWindowTitle(IntPtr hWnd)
        {
            try
            {
                // Allocate correct string length first
                int length = GetWindowTextLength(hWnd);
                StringBuilder sb = new StringBuilder(length + 1);
                GetWindowText(hWnd, sb, sb.Capacity);

                return sb.ToString();
            }
            catch (Exception)
            {
                return "";
            }
        }

        /// <summary>
        /// Initializes global hooks.
        /// </summary>
        private void AttachHooks()
        {
            globalHook = Hook.GlobalEvents();

            globalHook.MouseUpExt += GlobalHook_MouseUpExt;
            globalHook.KeyUp += GlobalHook_KeyUp;
        }

        /// <summary>
        /// Detaches global hooks and destroys the object.
        /// </summary>
        private void DetachHooks()
        {
            globalHook.MouseUpExt -= GlobalHook_MouseUpExt;
            globalHook.KeyUp -= GlobalHook_KeyUp;

            globalHook.Dispose();
        }

        /// <summary>
        /// Checks if any RuneScape window is the foreground window
        /// </summary>
        /// <returns>True if the RS window is active</returns>
        private bool IsRuneScapeWindowActive()
        {
            try
            {
                string fgWindowTitle = GetWindowTitle(GetForegroundWindow());

                return Array.Exists(equalsMatchWindowTitles, fgWindowTitle.Equals)
                    || Array.Exists(startsWithMatchWindowTitles, fgWindowTitle.StartsWith);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Determines if the event happened while the timer window
        /// was within the same rectangle as the RS window.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool AreWindowBoundsCorrect()
        {
            Rect fgRect = GetForegroundWindowRectangle();
            Rect timerRect = GetTimerRectangle();

            return IsRectWithinOther(timerRect, fgRect);
        }

        /// <summary>
        /// Determines if rect1 is contained within rect2.
        /// </summary>
        /// <param name="rect1">Smaller rect</param>
        /// <param name="rect2">Larger rect</param>
        /// <returns></returns>
        public bool IsRectWithinOther(Rect rect1, Rect rect2)
        {
            return rect1.Left >= rect2.Left &&
                rect1.Right <= rect2.Right &&
                rect1.Top >= rect2.Top &&
                rect1.Bottom <= rect2.Bottom;
        }

        /// <summary>
        /// Gets the screen the foreground window is on.
        /// </summary>
        /// <returns></returns>
        private Rect GetForegroundWindowRectangle()
        {
            Rect rect = new Rect();
            GetWindowRect(GetForegroundWindow(), out rect);

            return rect;
        }

        /// <summary>
        /// Gets the screen the timer window is on.
        /// </summary>
        /// <returns></returns>
        private Rect GetTimerRectangle()
        {
            Rect rect = new Rect();
            GetWindowRect(new WindowInteropHelper(Window.GetWindow(this)).Handle, out rect);

            return rect;
        }

        /// <summary>
        /// Restarts the timer whenever a MouseUp event happens
        /// on the RS window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GlobalHook_MouseUpExt(object sender, MouseEventExtArgs e)
        {
            try
            {
                if (IsRuneScapeWindowActive() && AreWindowBoundsCorrect())
                {
                    WindowExtensions.StopFlashingWindow(this);
                    Button_Restart_Click(null, null);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Restarts the timer whenever a KeyUp event happens
        /// in the RS window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GlobalHook_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            try
            {
                if (IsRuneScapeWindowActive() && AreWindowBoundsCorrect())
                {
                    WindowExtensions.StopFlashingWindow(this);
                    Button_Restart_Click(null, null);
                }
            }
            catch (Exception) { }
        }

        private void InitializeDispatcherTimer()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(Timer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
        }

        private void Window_Timer_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeCustomTimer();

            // Update the time display before starting it
            CustomTimer_SecondsTick(customTimer, null);
        }

        /// <summary>
        /// Saves settings and disables global hooks before closing the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Timer_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowExtensions.StopFlashingWindow(this);
            SaveSettings();

            if (MenuItem_Options_AutoRestart.IsChecked)
                DetachHooks();
        }

        /// <summary>
        /// Makes sure TextBoxes only accept numeric input.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                if (sender == TextBox_Minutes || sender == TextBox_Seconds)
                    Button_Restart_Click(null, null);

            // Only accept numbers and the backspace key as valid key presses
            if (e.Key != Key.Back && e.Key != Key.Tab)
                if (e.Key < Key.D0 || e.Key > Key.D9 || Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    if (e.Key < Key.NumPad0 || e.Key > Key.NumPad9)
                        e.Handled = true;
        }

        /// <summary>
        /// Pauses/starts the timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Start_Click(object sender, RoutedEventArgs e)
        {
            // Only update the CustomTimer if it's our first time pressing the button
            if (firstPress == true)
            {
                InitializeCustomTimer();
                firstPress = false;
            }

            // Don't start the timer if minutes and seconds are 0
            if (customTimer.IsTimerAllZero())
            {
                firstPress = true;
                return;
            }

            if (Button_Start.Content as string == "Start")
                StartTimer();
            else // Button says "Pause"
                PauseTimer();
        }

        /// <summary>
        /// Restarts the timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Restart_Click(object sender, RoutedEventArgs e)
        {
            // Stop the timer so it doesn't update in the middle of a Tick event
            dispatcherTimer.Stop();

            InitializeCustomTimer();
            firstPress = false;

            if (!customTimer.IsTimerAllZero())
                StartTimer();
            else
                firstPress = true;
        }

        /// <summary>
        /// Closes the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Enables/disables always on top for the MainWindow.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Options_AlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = MenuItem_Options_AlwaysOnTop.IsChecked;
        }

        /// <summary>
        /// Performs necessary modifications for window sizing options.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Size_Compact_Click(object sender, RoutedEventArgs e)
        {
            if (MenuItem_Size_Compact.IsChecked)
            {
                // Hides the minutes/seconds labels and input boxes, removes the
                // window border, and lowers the MinHeight and MinWidth.

                this.WindowStyle = WindowStyle.None;
                Grid.SetRowSpan(Viewbox, 3);
                Grid.SetRow(Viewbox, 1);
                Grid_Timer.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                this.MinHeight = 70;
                this.MinWidth = 150;
                this.Label_Minutes.Visibility = Visibility.Collapsed;
                this.Label_Seconds.Visibility = Visibility.Collapsed;
                this.TextBox_Minutes.Visibility = Visibility.Collapsed;
                this.TextBox_Seconds.Visibility = Visibility.Collapsed;
                this.Button_Start.Visibility = Visibility.Collapsed;
                this.Button_Restart.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.WindowStyle = WindowStyle.SingleBorderWindow;

                if (Label_Minutes.Visibility == Visibility.Collapsed)
                {
                    Grid.SetRowSpan(Viewbox, 1);
                    Grid.SetRow(Viewbox, 2);
                    Grid_Timer.RowDefinitions[1].Height = new GridLength(50);
                    this.MinHeight = 175;
                    this.MinWidth = 200;
                }

                this.Label_Minutes.Visibility = Visibility.Visible;
                this.Label_Seconds.Visibility = Visibility.Visible;
                this.TextBox_Minutes.Visibility = Visibility.Visible;
                this.TextBox_Seconds.Visibility = Visibility.Visible;
                this.Button_Start.Visibility = Visibility.Visible;
                this.Button_Restart.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Code to execute on each DispatcherTimer tick.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Timer_Tick(object sender, EventArgs e)
        {
            customTimer.DecrementSeconds();
        }

        /// <summary>
        /// Performs all necessary actions for pausing the timer.
        /// </summary>
        private void PauseTimer()
        {
            if (MenuItem_Options_ProgressBar.IsChecked)
                TaskBarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
            Button_Start.Content = "Start";
            dispatcherTimer.Stop();
        }

        /// <summary>
        /// Performs all necessary actions for starting the timer.
        /// </summary>
        private void StartTimer()
        {
            // Need to do this to make the time label update for the 1st second
            CustomTimer_SecondsTick(customTimer, null);

            if (MenuItem_Options_ProgressBar.IsChecked)
                TaskBarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            Button_Start.Content = "Pause";
            dispatcherTimer.Start();
        }

        /// <summary>
        /// Parses string into an int or returns 0 if the parse fails.
        /// </summary>
        /// <param name="s">String to parse</param>
        /// <returns>Integer representation of the string</returns>
        private int CustomParseInt(string s)
        {
            return int.TryParse(s, out int n) ? n : 0;
        }

        /// <summary>
        /// Updates the values of the class member variables "minutes" and "seconds"
        /// with values in their corresponding text boxes.
        /// </summary>
        private void InitializeCustomTimer()
        {
            // Assign value in text box to minutes/seconds
            int minutes = CustomParseInt(TextBox_Minutes.Text);
            int seconds = CustomParseInt(TextBox_Seconds.Text);

            customTimer = new CustomTimer(minutes, seconds);

            // Update the text boxes with new values if necessary
            TextBox_Minutes.Text = customTimer.Minutes.ToString();
            TextBox_Seconds.Text = customTimer.Seconds.ToString();

            customTimer.TimerEnded += CustomTimer_TimerEnded;
            customTimer.SecondsTick += CustomTimer_SecondsTick;
        }

        /// <summary>
        /// Code to execute whenever the CustomTimer raises
        /// a SecondsTick event. Updates the time label/progress bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CustomTimer_SecondsTick(object sender, EventArgs e)
        {
            if (!(sender is CustomTimer customTimer))
                return;

            if (MenuItem_Options_ProgressBar.IsChecked)
                TaskBarItemInfo.ProgressValue = Math.Max(customTimer.PercentSecondsRemaining, 0.01);

            // Update the time label
            Label_Time.Text = string.Format("{0}:{1:00}", customTimer.Minutes, customTimer.Seconds);
        }

        /// <summary>
        /// Code to execute whenever the CustomTimer raises
        /// a TimerEnded event. Changes the progress bar state,
        /// stops the timer, and notifies the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void CustomTimer_TimerEnded(object sender, EventArgs e)
        {
            // Timer has finished
            TaskBarItemInfo.ProgressState = TaskbarItemProgressState.None;
            TaskBarItemInfo.ProgressValue = 0.0;
            firstPress = true;
            PauseTimer();

            if (MenuItem_Notifications_FlashWindow.IsChecked)
                WindowExtensions.FlashWindow(this, 4);

            if (MenuItem_Notifications_Sound.IsChecked == true)
                SystemSounds.Asterisk.Play();
        }

        /// <summary>
        /// Restores settings previously saved by the program.
        /// </summary>
        private void RestoreSettings()
        {
            TextBox_Minutes.Text = Properties.Settings.Default.Minutes;
            TextBox_Seconds.Text = Properties.Settings.Default.Seconds;
            this.Topmost = MenuItem_Options_AlwaysOnTop.IsChecked = Properties.Settings.Default.AlwaysOnTop;
            MenuItem_Options_AutoRestart.IsChecked = Properties.Settings.Default.AutoRestart;
            MenuItem_Notifications_Sound.IsChecked = Properties.Settings.Default.NotificationSound;
            MenuItem_Notifications_FlashWindow.IsChecked = Properties.Settings.Default.FlashWindow;
            MenuItem_Options_ProgressBar.IsChecked = Properties.Settings.Default.ProgressBar;
            MenuItem_Size_Compact.IsChecked = Properties.Settings.Default.CompactSize;
            MenuItem_Size_Compact_Click(null, null);

            // Check if the window's size is within the screen bounds
            this.Height = Math.Min(SystemParameters.VirtualScreenHeight, Properties.Settings.Default.Height);
            this.Width = Math.Min(SystemParameters.VirtualScreenWidth, Properties.Settings.Default.Width);

            // Check if the window's Top is within the screen's bounds
            if (Properties.Settings.Default.Top + this.Height / 2 > SystemParameters.VirtualScreenHeight)
                this.Top = Math.Max(SystemParameters.VirtualScreenHeight - this.Height, 0);
            else
                this.Top = Properties.Settings.Default.Top;

            // Check if the window's Left is within the screen's bounds
            if (Properties.Settings.Default.Left + this.Width / 2 > SystemParameters.VirtualScreenWidth)
                this.Left = Math.Max(SystemParameters.VirtualScreenWidth - this.Width, 0);
            else
                this.Left = Properties.Settings.Default.Left;

            this.WindowState = Properties.Settings.Default.WindowState;

            if (globalHook == null && MenuItem_Options_AutoRestart.IsChecked)
                AttachHooks();
        }

        /// <summary>
        /// Saves settings so they can be restored next time the program is used.
        /// </summary>
        private void SaveSettings()
        {
            Properties.Settings.Default.Minutes = TextBox_Minutes.Text;
            Properties.Settings.Default.Seconds = TextBox_Seconds.Text;
            Properties.Settings.Default.AlwaysOnTop = MenuItem_Options_AlwaysOnTop.IsChecked;
            Properties.Settings.Default.AutoRestart = MenuItem_Options_AutoRestart.IsChecked;
            Properties.Settings.Default.NotificationSound = MenuItem_Notifications_Sound.IsChecked;
            Properties.Settings.Default.FlashWindow = MenuItem_Notifications_FlashWindow.IsChecked;
            Properties.Settings.Default.ProgressBar = MenuItem_Options_ProgressBar.IsChecked;
            Properties.Settings.Default.CompactSize = MenuItem_Size_Compact.IsChecked;

            // Don't care about the window's settings if it's minimized
            if (this.WindowState != WindowState.Minimized)
            {
                Properties.Settings.Default.Height = this.Height;
                Properties.Settings.Default.Width = this.Width;
                Properties.Settings.Default.Top = this.Top;
                Properties.Settings.Default.Left = this.Left;
                Properties.Settings.Default.WindowState = this.WindowState;
            }

            Properties.Settings.Default.Save();
        }

        /// <summary>
        /// Allows the window to be dragged by clicking and dragging on the time display.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Viewbox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        /// <summary>
        /// Handles shortcut key presses as defined in the menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Timer_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.C)
            {
                MenuItem_Size_Compact.IsChecked = !MenuItem_Size_Compact.IsChecked;
                MenuItem_Size_Compact_Click(null, null);
            }
            else if (e.Key == Key.Escape)
            {
                MenuItem_Size_Compact.IsChecked = false;
                MenuItem_Size_Compact_Click(null, null);
            }
            else if (e.Key == Key.Space)
            {
                Button_Start_Click(null, null);
            }
            else if (e.Key == Key.R)
            {
                Button_Restart_Click(null, null);
            }
        }

        /// <summary>
        /// Stop flashing the window as soon as it gets focus again.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Timer_Activated(object sender, EventArgs e)
        {
            WindowExtensions.StopFlashingWindow(this);
        }

        /// <summary>
        /// Enables/disables the taskbar progress bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Options_ProgressBar_Click(object sender, RoutedEventArgs e)
        {
            if (MenuItem_Options_ProgressBar.IsChecked)
            {
                TaskBarItemInfo.ProgressValue = customTimer.PercentSecondsRemaining;

                if (dispatcherTimer.IsEnabled)
                    TaskBarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                else
                    TaskBarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
            }
            else
                TaskBarItemInfo.ProgressState = TaskbarItemProgressState.None;
        }

        /// <summary>
        /// Pauses/starts the timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_StartPause_Click(object sender, RoutedEventArgs e)
        {
            Button_Start_Click(null, null);
        }

        /// <summary>
        /// Restarts the timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Restart_Click(object sender, RoutedEventArgs e)
        {
            Button_Restart_Click(null, null);
        }

        /// <summary>
        /// Enables/disables global hooks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Options_AutoRestart_Click(object sender, RoutedEventArgs e)
        {
            if (MenuItem_Options_AutoRestart.IsChecked)
                AttachHooks();
            else
                DetachHooks();
        }

        /// <summary>
        /// Allows focus to leave the TextBoxes so shortcut keys may be used.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Timer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid_Timer.Focusable = true;
            Grid_Timer.Focus();
            Grid_Timer.Focusable = false;
        }
    }
}
