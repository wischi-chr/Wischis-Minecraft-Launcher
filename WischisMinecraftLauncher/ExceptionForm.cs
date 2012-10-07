using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WischisMinecraftLauncher
{
    public partial class ExceptionForm : Form
    {
        public ExceptionForm(Exception ex)
        {
            InitializeComponent();

            Exception curr = ex;
            string txt = "";

            while (curr != null)
            {
                txt += "========================" + Environment.NewLine +
                       "Mess: " + curr.Message + Environment.NewLine +
                       "src:" + curr.Source + Environment.NewLine +
                       "stack:" + curr.StackTrace+ Environment.NewLine + Environment.NewLine;

                curr = curr.InnerException;
            }

            textBox1.Text = txt;
        }

        public new void Show()
        {
            if (Application.MessageLoop)
                base.Show();
            else
                Application.Run(this);
        }

        private void ExceptionForm_FormClosing(object sender, FormClosingEventArgs e)
        {

        }

        private void ExceptionForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(-666);
        }
    }
}
