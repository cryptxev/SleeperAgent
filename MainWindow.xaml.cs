using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows.Controls.Primitives;

namespace autoOff
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        
        bool sleeperIsActive = false;
        private Thread backgroundThread, timer;
        
        public MainWindow()
        {
            InitializeComponent();
           
            CheckIfSleeperIsActive();
        }
        Process sleeperProcess = null;
        private void CheckIfSleeperIsActive()
        {
            Process[] processes = Process.GetProcessesByName("sleeperAgent");
            if (processes.Length == 0)
            {
                return;
            }
            int currProcessID = Process.GetCurrentProcess().Id;
            for (int i = 0; i< processes.Length; i++)
            {
                if (sleeperProcess != null && i < processes.Length-1)
                {
                    this.Close();
                }
                if (currProcessID != processes[i].Id)
                {
                    sleeperProcess = processes[i];
                }
                
            }
           


            

            if(sleeperProcess != null)
            {
                startStopBtn.Content = "Stop";
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            if (sleeperIsActive)
            {
                this.Hide();
            }
            else
            {
                if(sleeperProcess!= null)
                {
                    sleeperProcess.Kill();
                }
                this.Close();
            }
        }
        private void startTimeHour_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckHours(sender);
        }

        private void endTimeHour_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckHours(sender);
        }

        private void CheckHours(object sender)
        {
            CheckDigits(sender, '2', '3');
        }

        private void CheckMinutes(object sender)
        {
            CheckDigits(sender, '5');
        }
        private void CheckDigits(object sender, char firstMax, char? secondMax = null)
        {
            TextBox tb = sender as TextBox;
            string text = tb.Text;

            if (text == "")
            {
                return;
            }

            int cursorPosition = tb.SelectionStart;
            string modText = "";
            char character = text[0];

            if (text.Length > 2)
            {
                tb.Text = text.Substring(0, 2);
                tb.SelectionStart = 2;
                return;
            }
            char firstDig = '0';
            for (int i = 0; i < text.Length; i++)
            {
                character = text[i];
                if (i == 0)
                {
                    if (character >= '0' && character <= firstMax)
                    {
                        modText += character;
                    }
                    else
                    {
                        cursorPosition--;
                    }
                    firstDig = character;
                }
                else
                {
                    if (secondMax != null && firstDig == '2')
                    {
                        if (character >= '0' && character <= secondMax)
                        {
                            modText += character;
                        }
                        else
                        {
                            cursorPosition--;
                        }
                    }
                    else
                    {
                        if (character >= '0' && character <= '9')
                        {
                            modText += character;
                        }
                        else
                        {
                            cursorPosition--;
                        }
                    }
                }
            }
            tb.Text = modText;
            tb.SelectionStart = cursorPosition;
        }

        private void startTimeMinute_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckMinutes(sender);
        }

        private void endTimeMinute_TextChanged(object sender, TextChangedEventArgs e)
        {
            CheckMinutes(sender);
        }

        private void intervalMinutes_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            string text = tb.Text;

            if (text == "")
            {
                return;
            }

            int cursorPosition = tb.SelectionStart;
            string modText = "";
            char character = text[0];
            if (text.Length > 3)
            {
                tb.Text = text.Substring(0, 3);
                tb.SelectionStart = 3;
                return;
            }

            for (int i = 0; i < text.Length; i++)
            {
                character = text[i];

                if (i == 0 && character == '0')
                {
                    cursorPosition--;
                    continue;
                }
                if (character >= '0' && character <= '9')
                {
                    modText += character;
                }
                else
                {
                    cursorPosition--;
                }
            }
            tb.Text = modText;
            tb.SelectionStart = cursorPosition;
        }

        byte startHour;
        byte startMinute;
        byte endHour;
        byte endMinute;
        int interval;

        DateTime startTime;
        DateTime endTime;

        static AutoResetEvent terminate = new AutoResetEvent(false);
        static AutoResetEvent terminateTimer = new AutoResetEvent(false);

        int action = 0;

        private void startStopBtn_Click(object sender, RoutedEventArgs e)
        {
            Button startStopBtn = sender as Button;

            if (startStopBtn.Content.ToString() == "Stop")
            {
                if (backgroundThread != null && backgroundThread.IsAlive)
                {
                    UnhookWindowsHookEx(_hookID);
                    sleeperIsActive = false;
                    terminate.Set();
                    backgroundThread.Join();
                    startStopBtn.Content = "Start";
                    return;
                }

                if(sleeperProcess != null && !sleeperProcess.HasExited)
                {
                    UnhookWindowsHookEx(_hookID);
                    sleeperProcess.Kill();
                    sleeperProcess = null;
                    startStopBtn.Content = "Start";
                }

            }
            else
            {
                TextBox[] inputs =
                {
                startTimeHour, startTimeMinute, endTimeHour, endTimeMinute, intervalMinutes
                };

                for (int i = 0; i < inputs.Length; i++)
                {
                    if (inputs[i].Text == "")
                    {
                        switch (i)
                        {
                            case 0:
                                showError("Please enter a valid start hour.");
                                startTimeHour.Focus();
                                return;
                            case 1:
                                showError("Please enter a valid start minute.");
                                startTimeMinute.Focus();
                                return;
                            case 2:
                                showError("Please enter a valid end hour.");
                                endTimeHour.Focus();
                                return;
                            case 3:
                                showError("Please enter a valid end minute.");
                                endTimeMinute.Focus();
                                return;
                            case 4:
                                showError("Please enter a valid interval in minutes.");
                                intervalMinutes.Focus();
                                return;
                        }
                    }
                }

                 startHour = Convert.ToByte(startTimeHour.Text);
                 startMinute = Convert.ToByte(startTimeMinute.Text);
                 endHour = Convert.ToByte(endTimeHour.Text);
                 endMinute = Convert.ToByte(endTimeMinute.Text);
                 interval = Convert.ToInt32(intervalMinutes.Text);

                if(shutdownRadio.IsChecked == true)
                {
                    action = 0;
                }
                else if (hibernateRadio.IsChecked == true)
                {
                    action = 1;
                }
                else if (sleepeRadio.IsChecked == true)
                {
                    action = 2;
                }
                else
                {
                    showError("Please select an action to perform.");
                    return;
                }

                DateTime currentTime = DateTime.Now;

                startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, startHour, startMinute, 0);

                if(startTime.Hour < currentTime.Hour)
                {
                    startTime.AddDays(1);
                }

                endTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, endHour, endMinute, 0);

                if (endHour<startHour|| endHour==startHour&& endMinute< startMinute || endHour==startHour&& endMinute == startMinute || endHour < currentTime.Hour)
                {
                    endTime.AddDays(1);
                }

                if (!sleeperIsActive)
                {
                    sleeperIsActive = true;
                    startStopBtn.Content = "Stop";
                    _hookID = SetHook(_proc);
                    backgroundThread = new Thread(BackgroundWorker);
                    backgroundThread.Name = "SleeperThread";
                    backgroundThread.IsBackground = true;
                    backgroundThread.Start();
                }

            }
        }

        private void showError(string message)
        {
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT mousePosition);

        [DllImport("Powrprof.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hiberate, bool forceCritical, bool disableWakeEvent);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private static bool keyIsPressed = false;
        private void BackgroundWorker() {
            bool isInactive = false;
            Stopwatch stopwatch = new Stopwatch();
            POINT mousePos, lastPos;
            lastPos.X = 0;
            lastPos.Y = 0;
            int intervalMs = interval * 60 * 1000;
                     
                while (sleeperIsActive)
                {
                    bool isInTime = DateTime.Now >= startTime && DateTime.Now <= endTime;
                
                while (isInTime && sleeperIsActive)
                {
                    if(GetCursorPos(out mousePos))
                    {
                        if(mousePos.X != lastPos.X && mousePos.Y != lastPos.Y)
                        {
                            stopwatch.Restart();
                        }
                        lastPos = mousePos;
                    }

                    if (keyIsPressed)
                    {
                        keyIsPressed = false;
                        stopwatch.Restart();
                    }


                    if (stopwatch.Elapsed >= TimeSpan.FromMilliseconds(intervalMs))
                    {
                        isInactive = true;
                    }

                    if (isInactive)
                    {
                        timer = new Thread(() => BackgroundWorkerTimer(action));
                        timer.Name = "SleeperTimer";
                        timer.IsBackground = true;
                        timer.Start();
                        if (MessageBox.Show("Are you still there?\n If you won't respond your computer will shut down in 30 seconds\n Press OK to cancel shutdown", "Sleeper Alert", MessageBoxButton.OK, MessageBoxImage.Warning) == MessageBoxResult.OK)
                        {
                            isInactive = false;
                            stopwatch.Restart();
                            terminateTimer.Set();
                        }
                        

                    }
                    if (terminate.WaitOne(1))
                    {
                        UnhookWindowsHookEx(_hookID);
                        return;
                    }

                    isInTime = DateTime.Now >= startTime && DateTime.Now <= endTime;
                }
            }
        }

        private void BackgroundWorkerTimer(int action)
        {
            if (terminateTimer.WaitOne(30000))
            {
                return;
            }

            switch (action)
            {
                case 0:
                    Process.Start("shutdown", "/s /t 0");
                    break;
                case 1:
                    Process.Start("shutdown", "/h");
                    break;
                case 2:
                    SetSuspendState(false, true, true);
                    break;
                default:
                    MessageBox.Show("Invalid action specified.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);   
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if(nCode>=0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                keyIsPressed = true;
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
        LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}