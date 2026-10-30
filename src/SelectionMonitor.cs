using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Automation;

namespace SelectionExplainer {
    internal static class Native {
        internal delegate IntPtr HookProc(int code, IntPtr wParam, IntPtr lParam);
        [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; }
        [StructLayout(LayoutKind.Sequential)] internal struct MouseData { public Point Position; public uint Mouse, Flags, Time; public UIntPtr Extra; }
        [DllImport("user32.dll", SetLastError=true)] internal static extern IntPtr SetWindowsHookEx(int id, HookProc proc, IntPtr module, uint thread);
        [DllImport("user32.dll")] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
        [DllImport("user32.dll")] internal static extern uint GetDoubleClickTime();
        [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
        internal static int ForegroundProcess() { uint id; GetWindowThreadProcessId(GetForegroundWindow(), out id); return (int)id; }
    }
    public sealed class SelectionMonitor : IDisposable {
        readonly Native.HookProc callback;
        readonly IntPtr hook;
        Point down, lastUp;
        int previousTime;
        bool pressed;
        public event Action<Point, bool> MouseReleased;
        public event Action MousePressed;
        public SelectionMonitor() {
            callback = Handle;
            hook = Native.SetWindowsHookEx(14, callback, Native.GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        }
        IntPtr Handle(int code, IntPtr message, IntPtr data) {
            if (code >= 0 && (message.ToInt32() == 0x201 || message.ToInt32() == 0x202)) {
                var mouse = (Native.MouseData)Marshal.PtrToStructure(data, typeof(Native.MouseData));
                var point = new Point(mouse.Position.X, mouse.Position.Y);
                if (message.ToInt32() == 0x201) {
                    down = point; pressed = true;
                    var handler = MousePressed; if (handler != null) handler();
                } else if (pressed) {
                    pressed = false;
                    bool drag = Math.Abs(point.X - down.X) + Math.Abs(point.Y - down.Y) >= 4;
                    bool twice = unchecked((uint)(Environment.TickCount - previousTime)) <= Native.GetDoubleClickTime() && Math.Abs(point.X - lastUp.X) + Math.Abs(point.Y - lastUp.Y) <= 6;
                    previousTime = Environment.TickCount; lastUp = point;
                    var handler = MouseReleased; if (handler != null) handler(point, drag || twice);
                }
            }
            return Native.CallNextHookEx(hook, code, message, data);
        }
        public void Dispose() { Native.UnhookWindowsHookEx(hook); }
    }
    public static class SelectionReader {
        static int busy;
        public static bool ReadAsync(Point point, int processId, int maxChars, Action<string> completed) {
            if (Interlocked.CompareExchange(ref busy, 1, 0) != 0) return false;
            ThreadPool.QueueUserWorkItem(delegate {
                string text = null;
                try {
                    // All UI Automation calls remain on an MTA worker; never block the mouse hook/UI.
                    var element = AutomationElement.FromPoint(new System.Windows.Point(point.X, point.Y));
                    text = FromAncestors(element, processId, maxChars);
                    if (String.IsNullOrWhiteSpace(text)) text = FromAncestors(AutomationElement.FocusedElement, processId, maxChars);
                } catch (Exception) { /* Unsupported providers fail closed: never copy or scan the desktop. */ }
                finally { Interlocked.Exchange(ref busy, 0); }
                completed(text);
            });
            return true;
        }
        static string FromAncestors(AutomationElement element, int processId, int maxChars) {
            for (int depth = 0; element != null && depth < 6; depth++) {
                if (element.Current.ProcessId != processId || element.Current.IsPassword) return null;
                object pattern;
                if (element.TryGetCurrentPattern(TextPattern.Pattern, out pattern)) {
                    var ranges = ((TextPattern)pattern).GetSelection();
                    if (ranges.Length == 1) {
                        string text = ranges[0].GetText(maxChars + 1);
                        if (!String.IsNullOrWhiteSpace(text)) return text.Trim();
                    }
                }
                element = TreeWalker.ControlViewWalker.GetParent(element);
            }
            return null;
        }
    }
}
