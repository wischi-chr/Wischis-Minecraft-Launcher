using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using ExternalMinecraftLauncher;

namespace WischisMinecraftLauncherCoreDLL
{
    public partial class ExternalLog : Form
    {
        WischiLauncherMainForm form = null;
        public ExternalLog(WischiLauncherMainForm Form)
        {
            InitializeComponent();
            form = Form;
            form.LogEvent += new WischiLauncherMainForm.LogEventDelegate(form_LogEvent);
        }

        void form_LogEvent(object sender, string Text)
        {
            textBox1.Text += DateTime.Now.ToString() + ": " + Text + Environment.NewLine;
        }

        private void textBox1_DoubleClick(object sender, EventArgs e)
        {
            textBox1.Text = "";
        }

    }
}
