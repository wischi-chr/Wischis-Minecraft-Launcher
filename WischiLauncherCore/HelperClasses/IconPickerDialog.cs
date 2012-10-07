using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ExternalMinecraftLauncher
{
    public class IconPickerDialog : CommonDialog
    {
        private const int MAX_PATH = 260;

        [DllImport("shell32.dll", EntryPoint = "#62", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool SHPickIconDialog(IntPtr hWnd, StringBuilder pszFilename, int cchFilenameMax, out int pnIconIndex);

        private string _filename = null;

        [DefaultValue(default(string))]
        public string Filename
        {
            get { return this._filename; }
            set { this._filename = value; }
        }

        private int _iconIndex = 0;

        [DefaultValue(0)]
        public int IconIndex
        {
            get { return this._iconIndex; }
            set { this._iconIndex = value; }
        }

        protected override bool RunDialog(IntPtr hwndOwner)
        {
            StringBuilder buf = new StringBuilder(this._filename, MAX_PATH);
            int iconIndex;

            bool ok = SHPickIconDialog(hwndOwner, buf, MAX_PATH, out iconIndex);
            if (ok)
            {
                this._filename = Environment.ExpandEnvironmentVariables(buf.ToString());
                this._iconIndex = iconIndex;
            }

            return ok;
        }

        public override void Reset()
        {
            this._filename = null;
            this._iconIndex = 0;
        }
    }
}
