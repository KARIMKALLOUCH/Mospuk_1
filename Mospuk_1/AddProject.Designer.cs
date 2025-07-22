namespace Mospuk_1
{
    partial class AddProject
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
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl4 = new DevExpress.XtraEditors.LabelControl();
            this.Reception_Date = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.Time = new Guna.UI2.WinForms.Guna2DateTimePicker();
            this.Company_Client = new Guna.UI2.WinForms.Guna2ComboBox();
            this.btnAdd = new Guna.UI2.WinForms.Guna2Button();
            this.Delivery_Date = new Guna.UI2.WinForms.Guna2ComboBox();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.SuspendLayout();
            // 
            // labelControl1
            // 
            this.labelControl1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(12, 25);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(112, 18);
            this.labelControl1.TabIndex = 94;
            this.labelControl1.Text = "Client / Company";
            // 
            // labelControl4
            // 
            this.labelControl4.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.labelControl4.Appearance.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl4.Appearance.Options.UseFont = true;
            this.labelControl4.Location = new System.Drawing.Point(12, 118);
            this.labelControl4.Name = "labelControl4";
            this.labelControl4.Size = new System.Drawing.Size(99, 18);
            this.labelControl4.TabIndex = 100;
            this.labelControl4.Text = "Date Reception";
            // 
            // Reception_Date
            // 
            this.Reception_Date.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(141)))), ((int)(((byte)(80)))));
            this.Reception_Date.Checked = true;
            this.Reception_Date.CustomFormat = "dd/MM/yyyy";
            this.Reception_Date.FillColor = System.Drawing.SystemColors.ControlLightLight;
            this.Reception_Date.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Reception_Date.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(141)))), ((int)(((byte)(80)))));
            this.Reception_Date.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.Reception_Date.Location = new System.Drawing.Point(12, 142);
            this.Reception_Date.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.Reception_Date.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.Reception_Date.Name = "Reception_Date";
            this.Reception_Date.Size = new System.Drawing.Size(210, 31);
            this.Reception_Date.TabIndex = 103;
            this.Reception_Date.Value = new System.DateTime(2025, 5, 7, 23, 54, 28, 444);
            // 
            // Time
            // 
            this.Time.BorderColor = System.Drawing.Color.White;
            this.Time.Checked = true;
            this.Time.CustomFormat = "hh:mm";
            this.Time.FillColor = System.Drawing.Color.White;
            this.Time.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.Time.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(141)))), ((int)(((byte)(80)))));
            this.Time.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.Time.Location = new System.Drawing.Point(249, 142);
            this.Time.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
            this.Time.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
            this.Time.Name = "Time";
            this.Time.ShowUpDown = true;
            this.Time.Size = new System.Drawing.Size(210, 31);
            this.Time.TabIndex = 102;
            this.Time.Value = new System.DateTime(2025, 5, 7, 23, 54, 28, 444);
            // 
            // Company_Client
            // 
            this.Company_Client.BackColor = System.Drawing.Color.Transparent;
            this.Company_Client.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(141)))), ((int)(((byte)(80)))));
            this.Company_Client.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.Company_Client.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Company_Client.FocusedColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(141)))), ((int)(((byte)(80)))));
            this.Company_Client.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(141)))), ((int)(((byte)(80)))));
            this.Company_Client.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Company_Client.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(141)))), ((int)(((byte)(80)))));
            this.Company_Client.ItemHeight = 30;
            this.Company_Client.Items.AddRange(new object[] {
            "ndr",
            "bnc"});
            this.Company_Client.Location = new System.Drawing.Point(12, 52);
            this.Company_Client.Name = "Company_Client";
            this.Company_Client.Size = new System.Drawing.Size(210, 36);
            this.Company_Client.TabIndex = 105;
            // 
            // btnAdd
            // 
            this.btnAdd.DisabledState.BorderColor = System.Drawing.Color.DarkGray;
            this.btnAdd.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray;
            this.btnAdd.DisabledState.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(169)))), ((int)(((byte)(169)))), ((int)(((byte)(169)))));
            this.btnAdd.DisabledState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(141)))), ((int)(((byte)(141)))), ((int)(((byte)(141)))));
            this.btnAdd.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(141)))), ((int)(((byte)(80)))));
            this.btnAdd.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.btnAdd.ForeColor = System.Drawing.Color.White;
            this.btnAdd.Location = new System.Drawing.Point(280, 213);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(179, 38);
            this.btnAdd.TabIndex = 101;
            this.btnAdd.Text = "Create New Project";
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // Delivery_Date
            // 
            this.Delivery_Date.BackColor = System.Drawing.Color.Transparent;
            this.Delivery_Date.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(141)))), ((int)(((byte)(80)))));
            this.Delivery_Date.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.Delivery_Date.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.Delivery_Date.FocusedColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(141)))), ((int)(((byte)(80)))));
            this.Delivery_Date.FocusedState.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(141)))), ((int)(((byte)(80)))));
            this.Delivery_Date.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.Delivery_Date.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(141)))), ((int)(((byte)(80)))));
            this.Delivery_Date.ItemHeight = 30;
            this.Delivery_Date.Location = new System.Drawing.Point(249, 52);
            this.Delivery_Date.Name = "Delivery_Date";
            this.Delivery_Date.Size = new System.Drawing.Size(210, 36);
            this.Delivery_Date.TabIndex = 106;
            // 
            // labelControl2
            // 
            this.labelControl2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.labelControl2.Appearance.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Location = new System.Drawing.Point(249, 28);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(89, 18);
            this.labelControl2.TabIndex = 107;
            this.labelControl2.Text = "Delivery Days";
            // 
            // labelControl3
            // 
            this.labelControl3.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.labelControl3.Appearance.Font = new System.Drawing.Font("Tahoma", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl3.Appearance.Options.UseFont = true;
            this.labelControl3.Location = new System.Drawing.Point(249, 118);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(101, 18);
            this.labelControl3.TabIndex = 108;
            this.labelControl3.Text = "Time Reception";
            // 
            // AddProject
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(482, 263);
            this.Controls.Add(this.labelControl3);
            this.Controls.Add(this.labelControl2);
            this.Controls.Add(this.Delivery_Date);
            this.Controls.Add(this.Company_Client);
            this.Controls.Add(this.Reception_Date);
            this.Controls.Add(this.Time);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.labelControl4);
            this.Controls.Add(this.labelControl1);
            this.Name = "AddProject";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AddProject";
            this.Load += new System.EventHandler(this.AddProject_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private Guna.UI2.WinForms.Guna2Button btnAdd;
        private Guna.UI2.WinForms.Guna2DateTimePicker Reception_Date;
        private Guna.UI2.WinForms.Guna2DateTimePicker Time;
        private Guna.UI2.WinForms.Guna2ComboBox Company_Client;
        private Guna.UI2.WinForms.Guna2ComboBox Delivery_Date;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl3;
    }
}