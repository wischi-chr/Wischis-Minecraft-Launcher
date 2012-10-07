using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WischisMinecraftLauncherCoreDLL
{
    public partial class ProfileName : Form
    {
        public string ProfilName { get; set; }


        public ProfileName(string text)
        {
            InitializeComponent();
            ProfilName = text;
            textBox1.Text = ProfilName;
            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ProfilName = textBox1.Text;
            this.Dispose();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        private void ProfileName_Activated(object sender, EventArgs e)
        {
            textBox1.Focus();
            textBox1.SelectAll();
        }

        
    }
}
