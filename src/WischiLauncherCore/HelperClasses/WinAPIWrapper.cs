using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace WischisMinecraftLauncherCoreDLL
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;        // x position of upper-left corner
        public int Top;         // y position of upper-left corner
        public int Right;       // x position of lower-right corner
        public int Bottom;      // y position of lower-right corner
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public static implicit operator System.Drawing.Point(POINT p)
        {
            return new System.Drawing.Point(p.X, p.Y);
        }

        public static implicit operator POINT(System.Drawing.Point p)
        {
            return new POINT(p.X, p.Y);
        }
    }


    static class APICalls
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        public static void ClientResize(IntPtr hWnd,int Left, int Top, int nWidth, int nHeight)
        {
            RECT rcClient, rcWindow;
            POINT ptDiff;
            GetClientRect(hWnd, out rcClient);
            GetWindowRect(hWnd, out rcWindow);
            ptDiff.X = (rcWindow.Right - rcWindow.Left) - rcClient.Right;
            ptDiff.Y = (rcWindow.Bottom - rcWindow.Top) - rcClient.Bottom;
            MoveWindow(hWnd, Left, Top, nWidth + ptDiff.X, nHeight + ptDiff.Y, true);
        }



    }
}
