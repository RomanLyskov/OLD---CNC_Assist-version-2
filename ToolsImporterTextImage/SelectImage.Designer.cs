﻿namespace CNCImporterGkode
{
    partial class SelectImage
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
            this.buttonSelectFile = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.btShowOriginalImage = new System.Windows.Forms.Button();
            this.textBoxFileName = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // buttonSelectFile
            // 
            this.buttonSelectFile.Location = new System.Drawing.Point(379, 59);
            this.buttonSelectFile.Name = "buttonSelectFile";
            this.buttonSelectFile.Size = new System.Drawing.Size(39, 21);
            this.buttonSelectFile.TabIndex = 6;
            this.buttonSelectFile.Text = "...";
            this.buttonSelectFile.UseVisualStyleBackColor = true;
            this.buttonSelectFile.Click += new System.EventHandler(this.buttonSelectFile_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(77, 44);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(78, 13);
            this.label6.TabIndex = 7;
            this.label6.Text = "Выбор файла:";
            // 
            // btShowOriginalImage
            // 
            this.btShowOriginalImage.Location = new System.Drawing.Point(71, 86);
            this.btShowOriginalImage.Name = "btShowOriginalImage";
            this.btShowOriginalImage.Size = new System.Drawing.Size(252, 37);
            this.btShowOriginalImage.TabIndex = 5;
            this.btShowOriginalImage.Text = "Показать рисунок";
            this.btShowOriginalImage.UseVisualStyleBackColor = true;
            this.btShowOriginalImage.Click += new System.EventHandler(this.btShowOriginalImage_Click);
            // 
            // textBoxFileName
            // 
            this.textBoxFileName.Location = new System.Drawing.Point(71, 60);
            this.textBoxFileName.Name = "textBoxFileName";
            this.textBoxFileName.Size = new System.Drawing.Size(302, 20);
            this.textBoxFileName.TabIndex = 4;
            // 
            // SelectImage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.buttonSelectFile);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.btShowOriginalImage);
            this.Controls.Add(this.textBoxFileName);
            this.Name = "SelectImage";
            this.Size = new System.Drawing.Size(600, 240);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button buttonSelectFile;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btShowOriginalImage;
        public System.Windows.Forms.TextBox textBoxFileName;
    }
}
