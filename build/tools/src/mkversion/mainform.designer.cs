namespace zuki.build.tools
{
    partial class MainForm
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
			System.Windows.Forms.Label m_productLabel;
			System.Windows.Forms.Label m_versionLabel;
			this.m_ok = new System.Windows.Forms.Button();
			this.m_cancel = new System.Windows.Forms.Button();
			this.m_productText = new System.Windows.Forms.TextBox();
			this.m_versionText = new System.Windows.Forms.TextBox();
			m_productLabel = new System.Windows.Forms.Label();
			m_versionLabel = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// m_productLabel
			// 
			m_productLabel.AutoSize = true;
			m_productLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			m_productLabel.Location = new System.Drawing.Point(8, 8);
			m_productLabel.Name = "m_productLabel";
			m_productLabel.Size = new System.Drawing.Size(115, 13);
			m_productLabel.TabIndex = 3;
			m_productLabel.Text = "Product (Baseline):";
			// 
			// m_versionLabel
			// 
			m_versionLabel.AutoSize = true;
			m_versionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			m_versionLabel.Location = new System.Drawing.Point(8, 56);
			m_versionLabel.Name = "m_versionLabel";
			m_versionLabel.Size = new System.Drawing.Size(182, 13);
			m_versionLabel.TabIndex = 5;
			m_versionLabel.Text = "MAJ.MIN[.BUILD[.REVISION]]:";
			// 
			// m_ok
			// 
			this.m_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_ok.Enabled = false;
			this.m_ok.Location = new System.Drawing.Point(96, 112);
			this.m_ok.Name = "m_ok";
			this.m_ok.Size = new System.Drawing.Size(75, 23);
			this.m_ok.TabIndex = 3;
			this.m_ok.Text = "OK";
			this.m_ok.UseVisualStyleBackColor = true;
			this.m_ok.Click += new System.EventHandler(this.OnOK);
			// 
			// m_cancel
			// 
			this.m_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_cancel.Location = new System.Drawing.Point(176, 112);
			this.m_cancel.Name = "m_cancel";
			this.m_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_cancel.TabIndex = 4;
			this.m_cancel.Text = "Cancel";
			this.m_cancel.UseVisualStyleBackColor = true;
			this.m_cancel.Click += new System.EventHandler(this.OnCancel);
			// 
			// m_productText
			// 
			this.m_productText.Location = new System.Drawing.Point(8, 24);
			this.m_productText.Name = "m_productText";
			this.m_productText.Size = new System.Drawing.Size(240, 20);
			this.m_productText.TabIndex = 0;
			this.m_productText.TextChanged += new System.EventHandler(this.OnItemTextChanged);
			// 
			// m_versionText
			// 
			this.m_versionText.Location = new System.Drawing.Point(8, 72);
			this.m_versionText.Name = "m_versionText";
			this.m_versionText.Size = new System.Drawing.Size(240, 20);
			this.m_versionText.TabIndex = 1;
			this.m_versionText.TextChanged += new System.EventHandler(this.OnItemTextChanged);
			// 
			// MainForm
			// 
			this.AcceptButton = this.m_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_cancel;
			this.ClientSize = new System.Drawing.Size(262, 144);
			this.ControlBox = false;
			this.Controls.Add(this.m_versionText);
			this.Controls.Add(m_versionLabel);
			this.Controls.Add(m_productLabel);
			this.Controls.Add(this.m_productText);
			this.Controls.Add(this.m_cancel);
			this.Controls.Add(this.m_ok);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MainForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "MKVERSION";
			this.ResumeLayout(false);
			this.PerformLayout();

        }

        #endregion

		private System.Windows.Forms.Button m_ok;
		private System.Windows.Forms.Button m_cancel;
		private System.Windows.Forms.TextBox m_productText;
		private System.Windows.Forms.TextBox m_versionText;
    }
}

