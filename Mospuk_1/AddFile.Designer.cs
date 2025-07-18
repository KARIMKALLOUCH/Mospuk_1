namespace Mospuk_1
{
    partial class AddFile
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AddFile));
            this.panel1 = new System.Windows.Forms.Panel();
            this.image1 = new Guna.UI2.WinForms.Guna2PictureBox();
            this.btnUplaod = new Guna.UI2.WinForms.Guna2Button();
            ((System.ComponentModel.ISupportInitialize)(this.image1)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel1.BackColor = System.Drawing.Color.SeaGreen;
            this.panel1.Location = new System.Drawing.Point(2, 360);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1228, 339);
            this.panel1.TabIndex = 23;
            // 
            // image1
            // 
            this.image1.ImageRotate = 0F;
            this.image1.Location = new System.Drawing.Point(12, 12);
            this.image1.Name = "image1";
            this.image1.Size = new System.Drawing.Size(165, 204);
            this.image1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.image1.TabIndex = 24;
            this.image1.TabStop = false;
            this.image1.Click += new System.EventHandler(this.image1_Click);
            this.image1.DragDrop += new System.Windows.Forms.DragEventHandler(this.image1_DragDrop);
            this.image1.DragEnter += new System.Windows.Forms.DragEventHandler(this.image1_DragEnter);
            // 
            // btnUplaod
            // 
            this.btnUplaod.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUplaod.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnUplaod.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnUplaod.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnUplaod.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnUplaod.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(141)))), ((int)(((byte)(80)))));
            this.btnUplaod.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnUplaod.ForeColor = System.Drawing.Color.White;
            this.btnUplaod.Image = ((System.Drawing.Image)(resources.GetObject("btnUplaod.Image")));
            this.btnUplaod.Location = new System.Drawing.Point(1090, 705);
            this.btnUplaod.Name = "btnUplaod";
            this.btnUplaod.Size = new System.Drawing.Size(140, 38);
            this.btnUplaod.TabIndex = 22;
            this.btnUplaod.Text = "Add";
            this.btnUplaod.Click += new System.EventHandler(this.btnUplaod_Click);
            // 
            // AddFile
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1242, 746);
            this.Controls.Add(this.image1);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnUplaod);
            this.Name = "AddFile";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AddFile";
            ((System.ComponentModel.ISupportInitialize)(this.image1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Guna.UI2.WinForms.Guna2Button btnUplaod;
        private System.Windows.Forms.Panel panel1;
        private Guna.UI2.WinForms.Guna2PictureBox image1;
    }
}