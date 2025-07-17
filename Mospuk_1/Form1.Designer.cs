namespace Mospuk_1
{
    partial class Form1
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
            this.label6 = new System.Windows.Forms.Label();
            this.swtchPassword = new Guna.UI2.WinForms.Guna2ToggleSwitch();
            this.btnConnect = new Guna.UI2.WinForms.Guna2Button();
            this.label5 = new System.Windows.Forms.Label();
            this.txtboxPassword = new Guna.UI2.WinForms.Guna2TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtboxUsername = new Guna.UI2.WinForms.Guna2TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.txtboxDatabase = new Guna.UI2.WinForms.Guna2TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtboxPort = new Guna.UI2.WinForms.Guna2TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtboxServer = new Guna.UI2.WinForms.Guna2TextBox();
            this.SuspendLayout();
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(54, 281);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(120, 13);
            this.label6.TabIndex = 26;
            this.label6.Text = "Afficher le mot de passe";
            // 
            // swtchPassword
            // 
            this.swtchPassword.CheckedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.swtchPassword.CheckedState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.swtchPassword.CheckedState.InnerBorderColor = System.Drawing.Color.White;
            this.swtchPassword.CheckedState.InnerColor = System.Drawing.Color.White;
            this.swtchPassword.Location = new System.Drawing.Point(12, 277);
            this.swtchPassword.Name = "swtchPassword";
            this.swtchPassword.Size = new System.Drawing.Size(35, 20);
            this.swtchPassword.TabIndex = 25;
            this.swtchPassword.UncheckedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(137)))), ((int)(((byte)(149)))));
            this.swtchPassword.UncheckedState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(125)))), ((int)(((byte)(137)))), ((int)(((byte)(149)))));
            this.swtchPassword.UncheckedState.InnerBorderColor = System.Drawing.Color.White;
            this.swtchPassword.UncheckedState.InnerColor = System.Drawing.Color.White;
            this.swtchPassword.CheckedChanged += new System.EventHandler(this.swtchPassword_CheckedChanged);
            // 
            // btnConnect
            // 
            this.btnConnect.BorderRadius = 6;
            this.btnConnect.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnConnect.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnConnect.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnConnect.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnConnect.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnConnect.ForeColor = System.Drawing.Color.White;
            this.btnConnect.Location = new System.Drawing.Point(15, 305);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(200, 40);
            this.btnConnect.TabIndex = 24;
            this.btnConnect.Text = "Connect";
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label5.Location = new System.Drawing.Point(12, 222);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(69, 17);
            this.label5.TabIndex = 23;
            this.label5.Text = "Password";
            // 
            // txtboxPassword
            // 
            this.txtboxPassword.BorderRadius = 6;
            this.txtboxPassword.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtboxPassword.DefaultText = "";
            this.txtboxPassword.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtboxPassword.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtboxPassword.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtboxPassword.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtboxPassword.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtboxPassword.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtboxPassword.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtboxPassword.Location = new System.Drawing.Point(12, 242);
            this.txtboxPassword.Name = "txtboxPassword";
            this.txtboxPassword.PlaceholderText = "Password";
            this.txtboxPassword.SelectedText = "";
            this.txtboxPassword.Size = new System.Drawing.Size(203, 29);
            this.txtboxPassword.TabIndex = 22;
            this.txtboxPassword.UseSystemPasswordChar = true;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label4.Location = new System.Drawing.Point(9, 170);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(73, 17);
            this.label4.TabIndex = 21;
            this.label4.Text = "Username";
            // 
            // txtboxUsername
            // 
            this.txtboxUsername.BorderRadius = 6;
            this.txtboxUsername.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtboxUsername.DefaultText = "";
            this.txtboxUsername.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtboxUsername.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtboxUsername.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtboxUsername.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtboxUsername.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtboxUsername.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtboxUsername.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtboxUsername.Location = new System.Drawing.Point(9, 190);
            this.txtboxUsername.Name = "txtboxUsername";
            this.txtboxUsername.PlaceholderText = "Username";
            this.txtboxUsername.SelectedText = "";
            this.txtboxUsername.Size = new System.Drawing.Size(203, 29);
            this.txtboxUsername.TabIndex = 20;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label3.Location = new System.Drawing.Point(9, 118);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 17);
            this.label3.TabIndex = 19;
            this.label3.Text = "Database";
            // 
            // txtboxDatabase
            // 
            this.txtboxDatabase.BorderRadius = 6;
            this.txtboxDatabase.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtboxDatabase.DefaultText = "";
            this.txtboxDatabase.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtboxDatabase.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtboxDatabase.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtboxDatabase.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtboxDatabase.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtboxDatabase.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtboxDatabase.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtboxDatabase.Location = new System.Drawing.Point(9, 138);
            this.txtboxDatabase.Name = "txtboxDatabase";
            this.txtboxDatabase.PlaceholderText = "Database";
            this.txtboxDatabase.SelectedText = "";
            this.txtboxDatabase.Size = new System.Drawing.Size(203, 29);
            this.txtboxDatabase.TabIndex = 18;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label2.Location = new System.Drawing.Point(12, 66);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(34, 17);
            this.label2.TabIndex = 17;
            this.label2.Text = "Port";
            // 
            // txtboxPort
            // 
            this.txtboxPort.BorderRadius = 6;
            this.txtboxPort.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtboxPort.DefaultText = "";
            this.txtboxPort.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtboxPort.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtboxPort.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtboxPort.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtboxPort.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtboxPort.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtboxPort.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtboxPort.Location = new System.Drawing.Point(12, 86);
            this.txtboxPort.Name = "txtboxPort";
            this.txtboxPort.PlaceholderText = "Port";
            this.txtboxPort.SelectedText = "";
            this.txtboxPort.Size = new System.Drawing.Size(203, 29);
            this.txtboxPort.TabIndex = 16;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label1.Location = new System.Drawing.Point(12, 14);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 17);
            this.label1.TabIndex = 15;
            this.label1.Text = "Server";
            // 
            // txtboxServer
            // 
            this.txtboxServer.BorderRadius = 6;
            this.txtboxServer.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtboxServer.DefaultText = "";
            this.txtboxServer.DisabledState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(208)))), ((int)(((byte)(208)))), ((int)(((byte)(208)))));
            this.txtboxServer.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(226)))), ((int)(((byte)(226)))), ((int)(((byte)(226)))));
            this.txtboxServer.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtboxServer.DisabledState.PlaceholderForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(138)))), ((int)(((byte)(138)))), ((int)(((byte)(138)))));
            this.txtboxServer.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtboxServer.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtboxServer.HoverState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(94)))), ((int)(((byte)(148)))), ((int)(((byte)(255)))));
            this.txtboxServer.Location = new System.Drawing.Point(12, 34);
            this.txtboxServer.Name = "txtboxServer";
            this.txtboxServer.PlaceholderText = "Server";
            this.txtboxServer.SelectedText = "";
            this.txtboxServer.Size = new System.Drawing.Size(203, 29);
            this.txtboxServer.TabIndex = 14;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(240, 374);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.swtchPassword);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtboxPassword);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtboxUsername);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtboxDatabase);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtboxPort);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtboxServer);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label6;
        private Guna.UI2.WinForms.Guna2ToggleSwitch swtchPassword;
        private Guna.UI2.WinForms.Guna2Button btnConnect;
        private System.Windows.Forms.Label label5;
        private Guna.UI2.WinForms.Guna2TextBox txtboxPassword;
        private System.Windows.Forms.Label label4;
        private Guna.UI2.WinForms.Guna2TextBox txtboxUsername;
        private System.Windows.Forms.Label label3;
        private Guna.UI2.WinForms.Guna2TextBox txtboxDatabase;
        private System.Windows.Forms.Label label2;
        private Guna.UI2.WinForms.Guna2TextBox txtboxPort;
        private System.Windows.Forms.Label label1;
        private Guna.UI2.WinForms.Guna2TextBox txtboxServer;
    }
}

