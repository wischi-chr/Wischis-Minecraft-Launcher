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
        public ExceptionForm()
        {
            InitializeComponent();
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
