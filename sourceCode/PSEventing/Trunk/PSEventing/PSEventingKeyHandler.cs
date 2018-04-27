using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Nivot.PowerShell.Eventing
{
    // see: http://msdn2.microsoft.com/en-us/library/ms644985.aspx
    // hooking WH_KEYBOARD_LL (or mouse variation) has thread affinity:
    // the same thread is used for callback - and most importantly, the same
    // thread _must_ be used to unhook the handler.

    internal sealed class PSEventingKeyHandler : IDisposable
    {
        internal delegate IntPtr LowLevelKeyboardProcHandler(int nCode, IntPtr wParam, IntPtr lParam);

        private bool m_passThru = true;
        private IntPtr m_hook = IntPtr.Zero;
        private readonly IntPtr m_hCWnd;
        private readonly LowLevelKeyboardProcHandler m_handler;

        private static readonly string s_emptyGuid;

        static PSEventingKeyHandler()
        {
            s_emptyGuid = Guid.Empty.ToString();
        }

        internal PSEventingKeyHandler(bool hookNow)
        {
            Dump(".ctor");

            m_handler = new LowLevelKeyboardProcHandler(HookCallback);
            m_hCWnd = NativeMethods.GetConsoleWindow();

            if (hookNow)
            {
                Hook();
            }
        }

        private bool IsHooked
        {
            get { return (m_hook != IntPtr.Zero); }
        }

        internal bool PassThru
        {
            get { return m_passThru; }
            set { m_passThru = value; }
        }

        private bool Hook()
        {
            if (!IsHooked)
            {
                Dump("Hooking...");

                 m_hook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL,
                    m_handler, new HandleRef(null, NativeMethods.GetModuleHandle(null)), 0);

                Dump("Hooked: {0}", IsHooked);
            }
            return IsHooked;
        }

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
        private bool Unhook()
        {
            bool succeeded = true;

            if (IsHooked)
            {
                Dump("Unhooking...");

                if (NativeMethods.UnhookWindowsHookEx(new HandleRef(null, m_hook)) == false)
                {
                    // failed

                    int hresult = Marshal.GetLastWin32Error();

                    if (hresult == 0x57c)
                    {
                        Dump("Unhook fail: Invalid hook handle {0}", m_hook);
                    }
                    else
                    {
                        Dump("Unhook fail: hresult: {0:X8}", hresult);
                    }

                    succeeded = false;
                    //throw Marshal.GetExceptionForHR(hresult);
                }
                else
                {
                    // ok

                    Dump("Unhook success.");
                    m_hook = IntPtr.Zero;
                }
            }
            return succeeded;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            IntPtr hFocus = NativeMethods.GetForegroundWindow();

            Dump("GFW: {0} ; hCWnd: {1}", hFocus, m_hCWnd);

            // are we focused? (e.g. was this our input?)
            if (m_hCWnd == hFocus)
            {
                if (nCode >= 0 && (wParam == (IntPtr) NativeMethods.WM_KEYDOWN))
                {

                    // TODO: interpret wParam for keyup,keydown etc
                    KBDLLHOOKSTRUCT keyInfo = (KBDLLHOOKSTRUCT) Marshal.PtrToStructure(lParam, typeof (KBDLLHOOKSTRUCT));
                    Dump("Keys.{3} - vkeycode: {0}, scanCode: {1}, flags: {2}", keyInfo.vkCode, keyInfo.scanCode, keyInfo.flags, ((Keys)keyInfo.vkCode));

                    //int vkCode = Marshal.ReadInt32(lParam); // at the head of the struct

                    KeyEventArgs args = new KeyEventArgs((Keys)keyInfo.vkCode);
                    args.SuppressKeyPress = (PassThru == false); // more for informational purposes
                    
                    PSQueueHelper.Instance.DefaultQueue.EnqueueEvent(s_emptyGuid, typeof(PSEventingKeyHandler), "KeyDown", args);

                    Dump("Key: {0}", ((Keys)keyInfo.vkCode));
                }
            }

            if (m_passThru)
            {
                return NativeMethods.CallNextHookEx(m_hook, nCode, wParam, lParam);
            }

            // soak up keystroke
            return IntPtr.Zero;
        }

        [Conditional("DEBUG")]
        internal static void Dump(string format, params object[] parameters)
        {
            int tid = Thread.CurrentThread.ManagedThreadId;
            Debug.WriteLine(string.Format(format, parameters), "PSEventingKeyHandler tid:" + tid);
        }

        ~PSEventingKeyHandler()
        {
            bool success = Unhook();
            Trace.WriteLine("unhooked: " + success, "PSEventingKeyHandler.Finalize");
        }

        void IDisposable.Dispose()
        {
            bool success = Unhook();
            Trace.WriteLine("unhooked: " + success, "PSEventingKeyHandler.Dispose");
            GC.SuppressFinalize(this);
        }

        [StructLayout(LayoutKind.Sequential)]
        private class KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }
    }
}
