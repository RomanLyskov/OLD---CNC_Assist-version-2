﻿namespace CNC_Assist
{
    partial class EditGkode
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
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.numPosA = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.labelposX = new System.Windows.Forms.Label();
            this.labelposY = new System.Windows.Forms.Label();
            this.numPosZ = new System.Windows.Forms.NumericUpDown();
            this.labelposZ = new System.Windows.Forms.Label();
            this.numPosY = new System.Windows.Forms.NumericUpDown();
            this.numPosX = new System.Windows.Forms.NumericUpDown();
            this.checkBoxUseMatrix = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.numericUpDown2 = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
            this.cbCorrection = new System.Windows.Forms.CheckBox();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPosA)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPosZ)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPosY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPosX)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.numPosA);
            this.groupBox2.Controls.Add(this.label3);
            this.groupBox2.Controls.Add(this.labelposX);
            this.groupBox2.Controls.Add(this.labelposY);
            this.groupBox2.Controls.Add(this.numPosZ);
            this.groupBox2.Controls.Add(this.labelposZ);
            this.groupBox2.Controls.Add(this.numPosY);
            this.groupBox2.Controls.Add(this.numPosX);
            this.groupBox2.Location = new System.Drawing.Point(8, 35);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(166, 162);
            this.groupBox2.TabIndex = 14;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Смещение G-кода (мм.)";
            // 
            // numPosA
            // 
            this.numPosA.DecimalPlaces = 3;
            this.numPosA.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.numPosA.Location = new System.Drawing.Point(33, 124);
            this.numPosA.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numPosA.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.numPosA.Name = "numPosA";
            this.numPosA.Size = new System.Drawing.Size(120, 29);
            this.numPosA.TabIndex = 13;
            this.numPosA.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numPosA.ValueChanged += new System.EventHandler(this.numPosA_ValueChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label3.Location = new System.Drawing.Point(7, 129);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(21, 20);
            this.label3.TabIndex = 12;
            this.label3.Text = "A";
            // 
            // labelposX
            // 
            this.labelposX.AutoSize = true;
            this.labelposX.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelposX.Location = new System.Drawing.Point(6, 26);
            this.labelposX.Name = "labelposX";
            this.labelposX.Size = new System.Drawing.Size(21, 20);
            this.labelposX.TabIndex = 6;
            this.labelposX.Text = "X";
            // 
            // labelposY
            // 
            this.labelposY.AutoSize = true;
            this.labelposY.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelposY.Location = new System.Drawing.Point(6, 60);
            this.labelposY.Name = "labelposY";
            this.labelposY.Size = new System.Drawing.Size(21, 20);
            this.labelposY.TabIndex = 7;
            this.labelposY.Text = "Y";
            // 
            // numPosZ
            // 
            this.numPosZ.DecimalPlaces = 3;
            this.numPosZ.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.numPosZ.Location = new System.Drawing.Point(33, 89);
            this.numPosZ.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numPosZ.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.numPosZ.Name = "numPosZ";
            this.numPosZ.Size = new System.Drawing.Size(120, 29);
            this.numPosZ.TabIndex = 11;
            this.numPosZ.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numPosZ.ValueChanged += new System.EventHandler(this.numPosZ_ValueChanged);
            // 
            // labelposZ
            // 
            this.labelposZ.AutoSize = true;
            this.labelposZ.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelposZ.Location = new System.Drawing.Point(7, 94);
            this.labelposZ.Name = "labelposZ";
            this.labelposZ.Size = new System.Drawing.Size(20, 20);
            this.labelposZ.TabIndex = 8;
            this.labelposZ.Text = "Z";
            // 
            // numPosY
            // 
            this.numPosY.DecimalPlaces = 3;
            this.numPosY.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.numPosY.Location = new System.Drawing.Point(33, 55);
            this.numPosY.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numPosY.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.numPosY.Name = "numPosY";
            this.numPosY.Size = new System.Drawing.Size(120, 29);
            this.numPosY.TabIndex = 10;
            this.numPosY.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numPosY.ValueChanged += new System.EventHandler(this.numPosY_ValueChanged);
            // 
            // numPosX
            // 
            this.numPosX.DecimalPlaces = 3;
            this.numPosX.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.numPosX.Location = new System.Drawing.Point(33, 21);
            this.numPosX.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numPosX.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            -2147483648});
            this.numPosX.Name = "numPosX";
            this.numPosX.Size = new System.Drawing.Size(120, 29);
            this.numPosX.TabIndex = 9;
            this.numPosX.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numPosX.ValueChanged += new System.EventHandler(this.numPosX_ValueChanged);
            // 
            // checkBoxUseMatrix
            // 
            this.checkBoxUseMatrix.Enabled = false;
            this.checkBoxUseMatrix.Location = new System.Drawing.Point(180, 137);
            this.checkBoxUseMatrix.Name = "checkBoxUseMatrix";
            this.checkBoxUseMatrix.Size = new System.Drawing.Size(162, 47);
            this.checkBoxUseMatrix.TabIndex = 14;
            this.checkBoxUseMatrix.Text = "Корректировать высоту Z с учетом сканирования поверхности";
            this.checkBoxUseMatrix.UseVisualStyleBackColor = true;
            this.checkBoxUseMatrix.CheckedChanged += new System.EventHandler(this.checkBoxUseMatrix_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.numericUpDown2);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.numericUpDown1);
            this.groupBox1.Enabled = false;
            this.groupBox1.Location = new System.Drawing.Point(180, 35);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(172, 96);
            this.groupBox1.TabIndex = 19;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Пропорции (%)";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label1.Location = new System.Drawing.Point(12, 26);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(21, 20);
            this.label1.TabIndex = 15;
            this.label1.Text = "X";
            // 
            // numericUpDown2
            // 
            this.numericUpDown2.DecimalPlaces = 3;
            this.numericUpDown2.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.numericUpDown2.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numericUpDown2.Location = new System.Drawing.Point(39, 55);
            this.numericUpDown2.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numericUpDown2.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numericUpDown2.Name = "numericUpDown2";
            this.numericUpDown2.Size = new System.Drawing.Size(120, 29);
            this.numericUpDown2.TabIndex = 17;
            this.numericUpDown2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDown2.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown2.ValueChanged += new System.EventHandler(this.numericUpDown2_ValueChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.label2.Location = new System.Drawing.Point(12, 60);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(21, 20);
            this.label2.TabIndex = 16;
            this.label2.Text = "Y";
            // 
            // numericUpDown1
            // 
            this.numericUpDown1.DecimalPlaces = 3;
            this.numericUpDown1.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.numericUpDown1.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numericUpDown1.Location = new System.Drawing.Point(39, 21);
            this.numericUpDown1.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numericUpDown1.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numericUpDown1.Name = "numericUpDown1";
            this.numericUpDown1.Size = new System.Drawing.Size(120, 29);
            this.numericUpDown1.TabIndex = 18;
            this.numericUpDown1.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericUpDown1.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numericUpDown1.ValueChanged += new System.EventHandler(this.numericUpDown1_ValueChanged);
            // 
            // cbCorrection
            // 
            this.cbCorrection.AutoSize = true;
            this.cbCorrection.Location = new System.Drawing.Point(33, 12);
            this.cbCorrection.Name = "cbCorrection";
            this.cbCorrection.Size = new System.Drawing.Size(245, 17);
            this.cbCorrection.TabIndex = 12;
            this.cbCorrection.Text = "ПРИМЕНЯТЬ КОРРЕКТИРОВКУ ДАННЫХ";
            this.cbCorrection.UseVisualStyleBackColor = true;
            this.cbCorrection.CheckedChanged += new System.EventHandler(this.ccbCorrection_CheckedChanged);
            // 
            // EditGkode
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(361, 211);
            this.Controls.Add(this.checkBoxUseMatrix);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.cbCorrection);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "EditGkode";
            this.Text = "Корректировка G-кода";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.EditGkode_Load);
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPosA)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPosZ)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPosY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPosX)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.CheckBox checkBoxUseMatrix;
        private System.Windows.Forms.Label labelposX;
        private System.Windows.Forms.CheckBox cbCorrection;
        private System.Windows.Forms.Label labelposY;
        private System.Windows.Forms.NumericUpDown numPosZ;
        private System.Windows.Forms.Label labelposZ;
        private System.Windows.Forms.NumericUpDown numPosY;
        private System.Windows.Forms.NumericUpDown numPosX;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown numericUpDown2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown numericUpDown1;
        private System.Windows.Forms.NumericUpDown numPosA;
        private System.Windows.Forms.Label label3;
    }
}