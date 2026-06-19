using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace WindowOpacityController
{
    public partial class MainWindow : Window
    {
        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_LAYERED = 0x00080000;
        private const uint LWA_ALPHA = 0x00000002;

        private const int WM_HOTKEY = 0x0312;

        private const int HOTKEY_TARGET_WINDOW = 9000; // Ctrl + ]
        private const int HOTKEY_APP_WINDOW = 9001;    // Ctrl + [

        private const uint MOD_CONTROL = 0x0002;

        private const uint VK_OEM_6 = 0xDD; // ] 키
        private const uint VK_OEM_4 = 0xDB; // [ 키

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int SW_RESTORE = 9;

        private bool isLoaded = false;
        private bool isAppHidden = false;

        private readonly Dictionary<IntPtr, bool> hiddenWindows = new Dictionary<IntPtr, bool>();

        public MainWindow()
        {
            InitializeComponent();
            LoadWindowList();
            isLoaded = true;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            HwndSource source = HwndSource.FromHwnd(windowHandle);

            if (source != null)
            {
                source.AddHook(HwndHook);
            }

            bool targetHotKeyResult = RegisterHotKey(
                windowHandle,
                HOTKEY_TARGET_WINDOW,
                MOD_CONTROL,
                VK_OEM_6
            );

            bool appHotKeyResult = RegisterHotKey(
                windowHandle,
                HOTKEY_APP_WINDOW,
                MOD_CONTROL,
                VK_OEM_4
            );

            if (targetHotKeyResult && appHotKeyResult)
            {
                ResultText.Text = "단축키 등록 완료: Ctrl + ] / Ctrl + [";
            }
            else
            {
                int errorCode = Marshal.GetLastWin32Error();
                ResultText.Text = $"단축키 등록 실패. 오류 코드: {errorCode}";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;

            UnregisterHotKey(windowHandle, HOTKEY_TARGET_WINDOW);
            UnregisterHotKey(windowHandle, HOTKEY_APP_WINDOW);

            base.OnClosed(e);
        }

        private IntPtr HwndHook(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled
        )
        {
            if (msg == WM_HOTKEY)
            {
                int hotKeyId = wParam.ToInt32();

                if (hotKeyId == HOTKEY_TARGET_WINDOW)
                {
                    ToggleSelectedWindowVisibility();
                    handled = true;
                }
                else if (hotKeyId == HOTKEY_APP_WINDOW)
                {
                    ToggleAppWindowVisibility();
                    handled = true;
                }
            }

            return IntPtr.Zero;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch
            {
                // 드래그 중 예외 방지
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadWindowList();
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (OpacityValueText != null)
            {
                OpacityValueText.Text = $"현재 값: {(int)e.NewValue}";
            }

            if (!isLoaded)
            {
                return;
            }

            if (WindowComboBox.SelectedItem is not WindowInfo selectedWindow)
            {
                if (ResultText != null)
                {
                    ResultText.Text = "창을 선택하세요.";
                }

                return;
            }

            int opacity = (int)e.NewValue;
            bool result = SetWindowOpacity(selectedWindow.Handle, opacity);

            if (result)
            {
                ResultText.Text = $"'{selectedWindow.DisplayName}' 투명도 변경: {opacity}";
            }
            else
            {
                int errorCode = Marshal.GetLastWin32Error();
                ResultText.Text = $"투명도 변경 실패. 오류 코드: {errorCode}";
            }
        }

        private void ToggleSelectedWindowVisibility()
        {
            if (WindowComboBox.SelectedItem is not WindowInfo selectedWindow)
            {
                ResultText.Text = "숨기거나 보일 창을 선택하세요.";
                return;
            }

            IntPtr targetHandle = selectedWindow.Handle;

            if (targetHandle == IntPtr.Zero)
            {
                ResultText.Text = "잘못된 창입니다.";
                return;
            }

            bool isHidden = false;

            if (hiddenWindows.ContainsKey(targetHandle))
            {
                isHidden = hiddenWindows[targetHandle];
            }

            if (isHidden)
            {
                ShowWindow(targetHandle, SW_SHOW);
                ShowWindow(targetHandle, SW_RESTORE);

                hiddenWindows[targetHandle] = false;
                ResultText.Text = $"'{selectedWindow.DisplayName}' 창을 다시 보이게 했습니다.";
            }
            else
            {
                ShowWindow(targetHandle, SW_HIDE);

                hiddenWindows[targetHandle] = true;
                ResultText.Text = $"'{selectedWindow.DisplayName}' 창을 숨겼습니다.";
            }
        }

        private void ToggleAppWindowVisibility()
        {
            if (isAppHidden)
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();

                Topmost = true;
                Topmost = false;

                isAppHidden = false;

                if (ResultText != null)
                {
                    ResultText.Text = "투명도 조절 프로그램을 다시 보이게 했습니다.";
                }
            }
            else
            {
                isAppHidden = true;
                Hide();
            }
        }

        private void LoadWindowList()
        {
            List<WindowInfo> windows = new List<WindowInfo>();

            int currentProcessId = Process.GetCurrentProcess().Id;

            EnumWindows((hWnd, lParam) =>
            {
                if (!IsWindowVisible(hWnd))
                {
                    return true;
                }

                int length = GetWindowTextLength(hWnd);

                if (length == 0)
                {
                    return true;
                }

                StringBuilder builder = new StringBuilder(length + 1);
                GetWindowText(hWnd, builder, builder.Capacity);

                string title = builder.ToString();

                if (string.IsNullOrWhiteSpace(title))
                {
                    return true;
                }

                uint processId;
                GetWindowThreadProcessId(hWnd, out processId);

                // 현재 만든 프로그램은 목록에서 제외
                if (processId == currentProcessId)
                {
                    return true;
                }

                string processName = "Unknown";

                try
                {
                    Process process = Process.GetProcessById((int)processId);
                    processName = process.ProcessName;
                }
                catch
                {
                    processName = "Unknown";
                }

                windows.Add(new WindowInfo
                {
                    Handle = hWnd,
                    Title = title,
                    ProcessName = processName
                });

                return true;
            }, IntPtr.Zero);

            WindowComboBox.ItemsSource = windows;

            if (windows.Count > 0)
            {
                WindowComboBox.SelectedIndex = 0;
            }

            ResultText.Text = $"창 목록 {windows.Count}개를 불러왔습니다. Ctrl + ]: 선택 창 숨김 / Ctrl + [: 앱 숨김";
        }

        private bool SetWindowOpacity(IntPtr hWnd, int opacity)
        {
            if (opacity < 1)
            {
                opacity = 1;
            }

            if (opacity > 255)
            {
                opacity = 255;
            }

            IntPtr extendedStyle = GetWindowLongPtr(hWnd, GWL_EXSTYLE);
            long newStyle = extendedStyle.ToInt64() | WS_EX_LAYERED;

            SetWindowLongPtr(hWnd, GWL_EXSTYLE, new IntPtr(newStyle));

            bool result = SetLayeredWindowAttributes(
                hWnd,
                0,
                (byte)opacity,
                LWA_ALPHA
            );

            return result;
        }

        public class WindowInfo
        {
            public IntPtr Handle { get; set; }
            public string Title { get; set; } = "";
            public string ProcessName { get; set; } = "";

            public string DisplayName
            {
                get
                {
                    return $"{ProcessName} - {Title}";
                }
            }
        }

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        private static extern IntPtr GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern IntPtr SetWindowLong32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8)
            {
                return GetWindowLongPtr64(hWnd, nIndex);
            }

            return GetWindowLong32(hWnd, nIndex);
        }

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
            {
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            }

            return SetWindowLong32(hWnd, nIndex, dwNewLong);
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetLayeredWindowAttributes(
            IntPtr hWnd,
            uint crKey,
            byte bAlpha,
            uint dwFlags
        );

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(
            IntPtr hWnd,
            int id,
            uint fsModifiers,
            uint vk
        );

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(
            IntPtr hWnd,
            int id
        );

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(
            IntPtr hWnd,
            int nCmdShow
        );
    }
}