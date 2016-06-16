namespace DonateMiner
{
	partial class DonateMinerGUI
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DonateMinerGUI));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.panel1 = new System.Windows.Forms.Panel();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.StopButton = new System.Windows.Forms.Button();
			this.StartButton = new System.Windows.Forms.Button();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.label10 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.MinerStatusBar = new System.Windows.Forms.StatusStrip();
			this.MinerStatusText = new System.Windows.Forms.ToolStripStatusLabel();
			this.DataRefreshTimer = new System.Windows.Forms.Timer(this.components);
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.MinerStatusBar.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Font = new System.Drawing.Font("Arial", 15.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label1.Location = new System.Drawing.Point(70, 13);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(147, 24);
			this.label1.TabIndex = 3;
			this.label1.Text = "Spende Zeit...";
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(73, 38);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(401, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "Ein Bitcoin - Miner Plugin um die Entwicklung des WischiLaunchers zu Unterstützen" +
    "";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label3.Location = new System.Drawing.Point(7, 6);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(58, 16);
			this.label3.TabIndex = 5;
			this.label3.Text = "Hinweis";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(8, 22);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(543, 31);
			this.label4.TabIndex = 6;
			this.label4.Text = resources.GetString("label4.Text");
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.SystemColors.Info;
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add(this.label4);
			this.panel1.Controls.Add(this.label3);
			this.panel1.Location = new System.Drawing.Point(8, 75);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(554, 60);
			this.panel1.TabIndex = 7;
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label5.Location = new System.Drawing.Point(13, 146);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(139, 16);
			this.label5.TabIndex = 8;
			this.label5.Text = "Wie funktioniert es...";
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(14, 163);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(563, 56);
			this.label6.TabIndex = 9;
			this.label6.Text = resources.GetString("label6.Text");
			// 
			// label7
			// 
			this.label7.Location = new System.Drawing.Point(14, 218);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(563, 46);
			this.label7.TabIndex = 10;
			this.label7.Text = resources.GetString("label7.Text");
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label8.Location = new System.Drawing.Point(13, 264);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(124, 16);
			this.label8.TabIndex = 11;
			this.label8.Text = "Wo ist der Haken?";
			// 
			// label9
			// 
			this.label9.Location = new System.Drawing.Point(14, 280);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(563, 60);
			this.label9.TabIndex = 12;
			this.label9.Text = resources.GetString("label9.Text");
			// 
			// StopButton
			// 
			this.StopButton.Enabled = false;
			this.StopButton.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.StopButton.Image = global::DonateMiner.Properties.Resources.ghastface1;
			this.StopButton.Location = new System.Drawing.Point(472, 356);
			this.StopButton.Name = "StopButton";
			this.StopButton.Size = new System.Drawing.Size(127, 44);
			this.StopButton.TabIndex = 2;
			this.StopButton.Text = "  Stoppen";
			this.StopButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.StopButton.UseVisualStyleBackColor = true;
			// 
			// StartButton
			// 
			this.StartButton.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.StartButton.Image = global::DonateMiner.Properties.Resources.Grid_Diamond_Pickaxe;
			this.StartButton.Location = new System.Drawing.Point(251, 356);
			this.StartButton.Name = "StartButton";
			this.StartButton.Size = new System.Drawing.Size(215, 44);
			this.StartButton.TabIndex = 1;
			this.StartButton.Text = "Starte Unterstützung";
			this.StartButton.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.StartButton.UseVisualStyleBackColor = true;
			this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = global::DonateMiner.Properties.Resources.bitcoin64;
			this.pictureBox1.Location = new System.Drawing.Point(0, 0);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(64, 64);
			this.pictureBox1.TabIndex = 0;
			this.pictureBox1.TabStop = false;
			this.pictureBox1.Click += new System.EventHandler(this.pictureBox1_Click);
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Font = new System.Drawing.Font("Arial", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.label10.Location = new System.Drawing.Point(14, 341);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(122, 16);
			this.label10.TabIndex = 13;
			this.label10.Text = "Firewall - Hinweis";
			// 
			// label11
			// 
			this.label11.Location = new System.Drawing.Point(15, 358);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(230, 43);
			this.label11.TabIndex = 14;
			this.label11.Text = "Daten werden an den Pool-Server stratum.bitcoin.cz:3333 gesendet. Eventuell ersch" +
    "eint ein Firewall-Hinweis beim ersten Start";
			// 
			// MinerStatusBar
			// 
			this.MinerStatusBar.BackColor = System.Drawing.Color.White;
			this.MinerStatusBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MinerStatusText});
			this.MinerStatusBar.Location = new System.Drawing.Point(0, 408);
			this.MinerStatusBar.Name = "MinerStatusBar";
			this.MinerStatusBar.Size = new System.Drawing.Size(608, 22);
			this.MinerStatusBar.TabIndex = 15;
			this.MinerStatusBar.Text = "statusStrip1";
			// 
			// MinerStatusText
			// 
			this.MinerStatusText.Name = "MinerStatusText";
			this.MinerStatusText.Size = new System.Drawing.Size(40, 17);
			this.MinerStatusText.Text = "Bereit.";
			// 
			// DataRefreshTimer
			// 
			this.DataRefreshTimer.Interval = 2500;
			// 
			// DonateMinerGUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackColor = System.Drawing.Color.White;
			this.Controls.Add(this.MinerStatusBar);
			this.Controls.Add(this.label11);
			this.Controls.Add(this.label10);
			this.Controls.Add(this.label9);
			this.Controls.Add(this.label8);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.StopButton);
			this.Controls.Add(this.StartButton);
			this.Controls.Add(this.pictureBox1);
			this.Name = "DonateMinerGUI";
			this.Size = new System.Drawing.Size(608, 430);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.MinerStatusBar.ResumeLayout(false);
			this.MinerStatusBar.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.PictureBox pictureBox1;
		public System.Windows.Forms.Button StartButton;
		public System.Windows.Forms.Button StopButton;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.StatusStrip MinerStatusBar;
		public System.Windows.Forms.ToolStripStatusLabel MinerStatusText;
		public System.Windows.Forms.Timer DataRefreshTimer;
	}
}
