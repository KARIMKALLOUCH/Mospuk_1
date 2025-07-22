namespace Mospuk_1
{
    partial class OrcTest
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
            this.btnSaveWord = new System.Windows.Forms.Button();
            this.btnExtractText = new System.Windows.Forms.Button();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.btnLoadImage = new System.Windows.Forms.Button();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnSaveWord
            // 
            this.btnSaveWord.Location = new System.Drawing.Point(608, 355);
            this.btnSaveWord.Name = "btnSaveWord";
            this.btnSaveWord.Size = new System.Drawing.Size(105, 26);
            this.btnSaveWord.TabIndex = 1;
            this.btnSaveWord.Text = "word";
            this.btnSaveWord.UseVisualStyleBackColor = true;
            this.btnSaveWord.Click += new System.EventHandler(this.btnSaveWord_Click);
            // 
            // btnExtractText
            // 
            this.btnExtractText.Location = new System.Drawing.Point(304, 355);
            this.btnExtractText.Name = "btnExtractText";
            this.btnExtractText.Size = new System.Drawing.Size(105, 26);
            this.btnExtractText.TabIndex = 2;
            this.btnExtractText.Text = "text";
            this.btnExtractText.UseVisualStyleBackColor = true;
            this.btnExtractText.Click += new System.EventHandler(this.btnExtractText_Click);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(30, 116);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.textBox1.Size = new System.Drawing.Size(282, 220);
            this.textBox1.TabIndex = 3;
            // 
            // btnLoadImage
            // 
            this.btnLoadImage.Location = new System.Drawing.Point(458, 355);
            this.btnLoadImage.Name = "btnLoadImage";
            this.btnLoadImage.Size = new System.Drawing.Size(105, 26);
            this.btnLoadImage.TabIndex = 4;
            this.btnLoadImage.Text = "upload";
            this.btnLoadImage.UseVisualStyleBackColor = true;
            this.btnLoadImage.Click += new System.EventHandler(this.btnLoadImage_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.Location = new System.Drawing.Point(336, 50);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(358, 268);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // OrcTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.btnLoadImage);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.btnExtractText);
            this.Controls.Add(this.btnSaveWord);
            this.Controls.Add(this.pictureBox1);
            this.Name = "OrcTest";
            this.Text = "OrcTest";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Button btnSaveWord;
        private System.Windows.Forms.Button btnExtractText;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button btnLoadImage;
    }
}