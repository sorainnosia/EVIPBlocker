namespace EVIPBlocker
{
    partial class ucSetting
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
            this.label1 = new System.Windows.Forms.Label();
            this.txtLogGroup = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.txtEventID = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.txtScanName = new System.Windows.Forms.TextBox();
            this.txtWordDetected = new System.Windows.Forms.TextBox();
            this.btnDel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(5, 74);
            this.label1.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(113, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "Log Group";
            // 
            // txtLogGroup
            // 
            this.txtLogGroup.Location = new System.Drawing.Point(172, 68);
            this.txtLogGroup.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.txtLogGroup.Name = "txtLogGroup";
            this.txtLogGroup.Size = new System.Drawing.Size(196, 31);
            this.txtLogGroup.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(5, 124);
            this.label2.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(87, 25);
            this.label2.TabIndex = 2;
            this.label2.Text = "EventID";
            // 
            // txtEventID
            // 
            this.txtEventID.Location = new System.Drawing.Point(172, 118);
            this.txtEventID.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.txtEventID.Name = "txtEventID";
            this.txtEventID.Size = new System.Drawing.Size(196, 31);
            this.txtEventID.TabIndex = 2;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(5, 174);
            this.label3.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(155, 25);
            this.label3.TabIndex = 5;
            this.label3.Text = "Word Detected";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(5, 24);
            this.label4.Margin = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(123, 25);
            this.label4.TabIndex = 6;
            this.label4.Text = "Scan Name";
            // 
            // txtScanName
            // 
            this.txtScanName.Location = new System.Drawing.Point(172, 18);
            this.txtScanName.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.txtScanName.Name = "txtScanName";
            this.txtScanName.Size = new System.Drawing.Size(196, 31);
            this.txtScanName.TabIndex = 0;
            // 
            // txtWordDetected
            // 
            this.txtWordDetected.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtWordDetected.Location = new System.Drawing.Point(172, 168);
            this.txtWordDetected.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.txtWordDetected.Multiline = true;
            this.txtWordDetected.Name = "txtWordDetected";
            this.txtWordDetected.Size = new System.Drawing.Size(521, 71);
            this.txtWordDetected.TabIndex = 3;
            // 
            // btnDel
            // 
            this.btnDel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDel.Location = new System.Drawing.Point(567, 18);
            this.btnDel.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.btnDel.Name = "btnDel";
            this.btnDel.Size = new System.Drawing.Size(126, 81);
            this.btnDel.TabIndex = 4;
            this.btnDel.Text = "Delete";
            this.btnDel.UseVisualStyleBackColor = true;
            this.btnDel.Click += new System.EventHandler(this.btnDel_Click);
            // 
            // ucSetting
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(12F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.btnDel);
            this.Controls.Add(this.txtScanName);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.txtWordDetected);
            this.Controls.Add(this.txtEventID);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtLogGroup);
            this.Controls.Add(this.label1);
            this.Margin = new System.Windows.Forms.Padding(5, 6, 5, 6);
            this.Name = "ucSetting";
            this.Size = new System.Drawing.Size(703, 248);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtLogGroup;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtEventID;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtScanName;
        private System.Windows.Forms.TextBox txtWordDetected;
        private System.Windows.Forms.Button btnDel;
    }
}
