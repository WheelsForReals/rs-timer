using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Shell;
using System.Runtime.InteropServices;
using Gma.System.MouseKeyHook;

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

        CustomTimer customTimer;
        DispatcherTimer dispatcherTimer;
        IKeyboardMouseEvents globalHook;
        bool firstPress = true;

        const string rsBrowserWindowTitle = "RuneScape - MMORPG - The No.1 Free Online Multiplayer Game";
        const string rsClientWindowTitle = "RuneScape";

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

            globalHook.MouseUpExt += globalHook_MouseUpExt;
            globalHook.KeyUp += globalHook_KeyUp;
        }

        /// <summary>
        /// Detaches global hooks and destroys the object.
        /// </summary>
        private void DetachHooks()
        {
            globalHook.MouseUpExt -= globalHook_MouseUpExt;
            globalHook.KeyUp -= globalHook_KeyUp;

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

                return fgWindowTitle.Contains(rsBrowserWindowTitle) || fgWindowTitle == rsClientWindowTitle;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Used by global hooks for MouseUp/KeyPress events.
        /// Restarts the timer and stops the window flashing.
        /// </summary>
        private void GlobalTimerRestart()
        {
            try
            {
                if (IsRuneScapeWindowActive())
                {
                    WindowExtensions.StopFlashingWindow(this);
                    button_Restart_Click(null, null);
                }
            }
            catch (Exception) { }
        }

        /// <summary>
        /// Restarts the timer whenever a MouseUp event happens
        /// on the RS window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void globalHook_MouseUpExt(object sender, MouseEventExtArgs e)
        {
            GlobalTimerRestart();
        }

        /// <summary>
        /// Restarts the timer whenever a KeyUp event happens
        /// in the RS window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void globalHook_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            GlobalTimerRestart();
        }

        private void InitializeDispatcherTimer()
        {
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(timer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1);
        }

        private void window_Timer_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeCustomTimer();

            // Update the time display before starting it
            customTimer_SecondsTick(customTimer, null);
        }

        /// <summary>
        /// Saves settings and disables global hooks before closing the application.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void window_Timer_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            WindowExtensions.StopFlashingWindow(this);
            SaveSettings();

            if (menuItem_Options_AutoRestart.IsChecked)
                DetachHooks();
        }

        /// <summary>
        /// Makes sure TextBoxes only accept numeric input.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                if (sender == textBox_Minutes || sender == textBox_Seconds)
                    button_Restart_Click(null, null);

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
        private void button_Start_Click(object sender, RoutedEventArgs e)
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

            if (button_Start.Content as string == "Start")
                StartTimer();
            else // Button says "Pause"
                PauseTimer();
        }

        /// <summary>
        /// Restarts the timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_Restart_Click(object sender, RoutedEventArgs e)
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
        private void menuItem_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Enables/disables always on top for the MainWindow.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_Options_AlwaysOnTop_Click(object sender, RoutedEventArgs e)
        {
            this.Topmost = menuItem_Options_AlwaysOnTop.IsChecked;
        }

        /// <summary>
        /// Performs necessary modifications for window sizing options.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_Size_Compact_Click(object sender, RoutedEventArgs e)
        {
            if (menuItem_Size_Compact.IsChecked)
            {
                // Hides the minutes/seconds labels and input boxes, removes the
                // window border, and lowers the MinHeight and MinWidth.

                this.WindowStyle = WindowStyle.None;
                Grid.SetRowSpan(viewbox, 3);
                Grid.SetRow(viewbox, 1);
                grid_Timer.RowDefinitions[1].Height = new GridLength(1, GridUnitType.Star);
                this.MinHeight = 70;
                this.MinWidth = 150;
                this.label_Minutes.Visibility = Visibility.Collapsed;
                this.label_Seconds.Visibility = Visibility.Collapsed;
                this.textBox_Minutes.Visibility = Visibility.Collapsed;
                this.textBox_Seconds.Visibility = Visibility.Collapsed;
                this.button_Start.Visibility = Visibility.Collapsed;
                this.button_Restart.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.WindowStyle = WindowStyle.SingleBorderWindow;
                
                if (label_Minutes.Visibility == Visibility.Collapsed)
                {
                    Grid.SetRowSpan(viewbox, 1);
                    Grid.SetRow(viewbox, 2);
                    grid_Timer.RowDefinitions[1].Height = new GridLength(50);
                    this.MinHeight = 175;
                    this.MinWidth = 200;
                }

                this.label_Minutes.Visibility = Visibility.Visible;
                this.label_Seconds.Visibility = Visibility.Visible;
                this.textBox_Minutes.Visibility = Visibility.Visible;
                this.textBox_Seconds.Visibility = Visibility.Visible;
                this.button_Start.Visibility = Visibility.Visible;
                this.button_Restart.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Code to execute on each DispatcherTimer tick.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Tick(object sender, EventArgs e)
        {
            customTimer.DecrementSeconds();
        }

        /// <summary>
        /// Makes a beep noise a specified number of times.
        /// </summary>
        /// <param name="n">Number of times to make the beep noise.</param>
        private void beep(int n)
        {
            // Do this on a new thread so it doesn't block the UI
            new System.Threading.Thread(() =>
            {
                for (int i = 0; i < n; i++)
                    Console.Beep();
            }).Start();
        }

        /// <summary>
        /// Performs all necessary actions for pausing the timer.
        /// </summary>
        private void PauseTimer()
        {
            if (menuItem_Options_ProgressBar.IsChecked)
                taskBarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
            button_Start.Content = "Start";
            dispatcherTimer.Stop();
        }

        /// <summary>
        /// Performs all necessary actions for starting the timer.
        /// </summary>
        private void StartTimer()
        {
            // Need to do this to make the time label update for the 1st second
            customTimer_SecondsTick(customTimer, null);

            if (menuItem_Options_ProgressBar.IsChecked)
                taskBarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            button_Start.Content = "Pause";
            dispatcherTimer.Start();
        }

        /// <summary>
        /// Parses string into an int or returns 0 if the parse fails.
        /// </summary>
        /// <param name="s">String to parse</param>
        /// <returns>Integer representation of the string</returns>
        private int CustomParseInt(string s)
        {
            int n;

            return int.TryParse(s, out n) ? n : 0;
        }

        /// <summary>
        /// Updates the values of the class member variables "minutes" and "seconds"
        /// with values in their corresponding text boxes.
        /// </summary>
        private void InitializeCustomTimer()
        {
            // Assign value in text box to minutes/seconds
            int minutes = CustomParseInt(textBox_Minutes.Text);
            int seconds = CustomParseInt(textBox_Seconds.Text);

            customTimer = new CustomTimer(minutes, seconds);

            // Update the text boxes with new values if necessary
            textBox_Minutes.Text = customTimer.Minutes.ToString();
            textBox_Seconds.Text = customTimer.Seconds.ToString();

            customTimer.TimerEnded += customTimer_TimerEnded;
            customTimer.SecondsTick += customTimer_SecondsTick;
        }

        /// <summary>
        /// Code to execute whenever the CustomTimer raises
        /// a SecondsTick event. Updates the time label/progress bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void customTimer_SecondsTick(object sender, EventArgs e)
        {
            CustomTimer customTimer = sender as CustomTimer;
            if (customTimer == null)
                return;

            if (menuItem_Options_ProgressBar.IsChecked)
                taskBarItemInfo.ProgressValue = customTimer.PercentSecondsRemaining;

            // Update the time label
            label_Time.Text = string.Format("{0}:{1:00}", customTimer.Minutes, customTimer.Seconds);
        }

        /// <summary>
        /// Code to execute whenever the CustomTimer raises
        /// a TimerEnded event. Changes the progress bar state,
        /// stops the timer, and notifies the user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void customTimer_TimerEnded(object sender, EventArgs e)
        {
            // Timer has finished
            taskBarItemInfo.ProgressState = TaskbarItemProgressState.None;
            taskBarItemInfo.ProgressValue = 0.0;
            firstPress = true;
            PauseTimer();

            if (menuItem_Notifications_FlashWindow.IsChecked)
                WindowExtensions.FlashWindow(this, 4);

            if (menuItem_Notifications_Sound.IsChecked == true)
                beep(2);
        }

        /// <summary>
        /// Restores settings previously saved by the program.
        /// </summary>
        private void RestoreSettings()
        {
            textBox_Minutes.Text = Properties.Settings.Default.Minutes;
            textBox_Seconds.Text = Properties.Settings.Default.Seconds;
            this.Topmost = menuItem_Options_AlwaysOnTop.IsChecked = Properties.Settings.Default.AlwaysOnTop;
            menuItem_Options_AutoRestart.IsChecked = Properties.Settings.Default.AutoRestart;
            menuItem_Notifications_Sound.IsChecked = Properties.Settings.Default.NotificationSound;
            menuItem_Notifications_FlashWindow.IsChecked = Properties.Settings.Default.FlashWindow;
            menuItem_Options_ProgressBar.IsChecked = Properties.Settings.Default.ProgressBar;
            menuItem_Size_Compact.IsChecked = Properties.Settings.Default.CompactSize;
            menuItem_Size_Compact_Click(null, null);

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

            if (globalHook == null && menuItem_Options_AutoRestart.IsChecked)
                AttachHooks();
        }

        /// <summary>
        /// Saves settings so they can be restored next time the program is used.
        /// </summary>
        private void SaveSettings()
        {
            Properties.Settings.Default.Minutes = textBox_Minutes.Text;
            Properties.Settings.Default.Seconds = textBox_Seconds.Text;
            Properties.Settings.Default.AlwaysOnTop = menuItem_Options_AlwaysOnTop.IsChecked;
            Properties.Settings.Default.AutoRestart = menuItem_Options_AutoRestart.IsChecked;
            Properties.Settings.Default.NotificationSound = menuItem_Notifications_Sound.IsChecked;
            Properties.Settings.Default.FlashWindow = menuItem_Notifications_FlashWindow.IsChecked;
            Properties.Settings.Default.ProgressBar = menuItem_Options_ProgressBar.IsChecked;
            Properties.Settings.Default.CompactSize = menuItem_Size_Compact.IsChecked;

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
        private void viewbox_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        /// <summary>
        /// Handles shortcut key presses as defined in the menu.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void window_Timer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.C)
            {
                menuItem_Size_Compact.IsChecked = !menuItem_Size_Compact.IsChecked;
                menuItem_Size_Compact_Click(null, null);
            }
            else if (e.Key == Key.Escape)
            {
                menuItem_Size_Compact.IsChecked = false;
                menuItem_Size_Compact_Click(null, null);
            }
            else if (e.Key == Key.Space)
            {
                button_Start_Click(null, null);
            }
            else if (e.Key == Key.R)
            {
                button_Restart_Click(null, null);
            }
        }

        /// <summary>
        /// Stop flashing the window as soon as it gets focus again.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void window_Timer_Activated(object sender, EventArgs e)
        {
            WindowExtensions.StopFlashingWindow(this);
        }

        /// <summary>
        /// Enables/disables the taskbar progress bar.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_Options_ProgressBar_Click(object sender, RoutedEventArgs e)
        {
            if (menuItem_Options_ProgressBar.IsChecked)
            {
                taskBarItemInfo.ProgressValue = customTimer.PercentSecondsRemaining;

                if (dispatcherTimer.IsEnabled)
                    taskBarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
                else
                    taskBarItemInfo.ProgressState = TaskbarItemProgressState.Paused;
            }
            else
                taskBarItemInfo.ProgressState = TaskbarItemProgressState.None;
        }

        /// <summary>
        /// Pauses/starts the timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_StartPause_Click(object sender, RoutedEventArgs e)
        {
            button_Start_Click(null, null);
        }

        /// <summary>
        /// Restarts the timer.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_Restart_Click(object sender, RoutedEventArgs e)
        {
            button_Restart_Click(null, null);
        }

        /// <summary>
        /// Enables/disables global hooks.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuItem_Options_AutoRestart_Click(object sender, RoutedEventArgs e)
        {
            if (menuItem_Options_AutoRestart.IsChecked)
                AttachHooks();
            else
                DetachHooks();
        }

        /// <summary>
        /// Allows focus to leave the TextBoxes so shortcut keys may be used.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void window_Timer_MouseDown(object sender, MouseButtonEventArgs e)
        {
            grid_Timer.Focusable = true;
            grid_Timer.Focus();
            grid_Timer.Focusable = false;
        }
    }
}
