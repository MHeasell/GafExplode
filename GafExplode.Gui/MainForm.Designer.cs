
namespace GafExplode.Gui
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
            this.gafFileTextBox = new System.Windows.Forms.TextBox();
            this.gafFileBrowseButton = new System.Windows.Forms.Button();
            this.gafFileLabel = new System.Windows.Forms.Label();
            this.directoryLabel = new System.Windows.Forms.Label();
            this.directoryBrowseButton = new System.Windows.Forms.Button();
            this.directoryTextBox = new System.Windows.Forms.TextBox();
            this.explodeButton = new System.Windows.Forms.Button();
            this.unexplodeButton = new System.Windows.Forms.Button();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.statusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // gafFileTextBox
            // 
            this.gafFileTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gafFileTextBox.Location = new System.Drawing.Point(65, 6);
            this.gafFileTextBox.Name = "gafFileTextBox";
            this.gafFileTextBox.Size = new System.Drawing.Size(406, 20);
            this.gafFileTextBox.TabIndex = 0;
            // 
            // gafFileBrowseButton
            // 
            this.gafFileBrowseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gafFileBrowseButton.Location = new System.Drawing.Point(477, 4);
            this.gafFileBrowseButton.Name = "gafFileBrowseButton";
            this.gafFileBrowseButton.Size = new System.Drawing.Size(75, 23);
            this.gafFileBrowseButton.TabIndex = 1;
            this.gafFileBrowseButton.Text = "Browse...";
            this.gafFileBrowseButton.UseVisualStyleBackColor = true;
            this.gafFileBrowseButton.Click += new System.EventHandler(this.gafFileBrowseButtonClick);
            // 
            // gafFileLabel
            // 
            this.gafFileLabel.AutoSize = true;
            this.gafFileLabel.Location = new System.Drawing.Point(12, 9);
            this.gafFileLabel.Name = "gafFileLabel";
            this.gafFileLabel.Size = new System.Drawing.Size(47, 13);
            this.gafFileLabel.TabIndex = 2;
            this.gafFileLabel.Text = "GAF File";
            // 
            // directoryLabel
            // 
            this.directoryLabel.AutoSize = true;
            this.directoryLabel.Location = new System.Drawing.Point(12, 35);
            this.directoryLabel.Name = "directoryLabel";
            this.directoryLabel.Size = new System.Drawing.Size(49, 13);
            this.directoryLabel.TabIndex = 5;
            this.directoryLabel.Text = "Directory";
            // 
            // directoryBrowseButton
            // 
            this.directoryBrowseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.directoryBrowseButton.Location = new System.Drawing.Point(477, 30);
            this.directoryBrowseButton.Name = "directoryBrowseButton";
            this.directoryBrowseButton.Size = new System.Drawing.Size(75, 23);
            this.directoryBrowseButton.TabIndex = 4;
            this.directoryBrowseButton.Text = "Browse...";
            this.directoryBrowseButton.UseVisualStyleBackColor = true;
            this.directoryBrowseButton.Click += new System.EventHandler(this.directoryBrowseButtonClick);
            // 
            // directoryTextBox
            // 
            this.directoryTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.directoryTextBox.Location = new System.Drawing.Point(65, 32);
            this.directoryTextBox.Name = "directoryTextBox";
            this.directoryTextBox.Size = new System.Drawing.Size(406, 20);
            this.directoryTextBox.TabIndex = 3;
            // 
            // explodeButton
            // 
            this.explodeButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.explodeButton.Location = new System.Drawing.Point(12, 85);
            this.explodeButton.Name = "explodeButton";
            this.explodeButton.Size = new System.Drawing.Size(540, 62);
            this.explodeButton.TabIndex = 6;
            this.explodeButton.Text = "Explode! (GAF File -> Directory)";
            this.explodeButton.UseVisualStyleBackColor = true;
            this.explodeButton.Click += new System.EventHandler(this.explodeButtonClick);
            // 
            // unexplodeButton
            // 
            this.unexplodeButton.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.unexplodeButton.Location = new System.Drawing.Point(12, 183);
            this.unexplodeButton.Name = "unexplodeButton";
            this.unexplodeButton.Size = new System.Drawing.Size(540, 62);
            this.unexplodeButton.TabIndex = 7;
            this.unexplodeButton.Text = "Unexplode! (Directory -> GAF File)";
            this.unexplodeButton.UseVisualStyleBackColor = true;
            this.unexplodeButton.Click += new System.EventHandler(this.unexplodeButtonClick);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 278);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(564, 22);
            this.statusStrip.TabIndex = 8;
            this.statusStrip.Text = "statusStrip1";
            // 
            // statusLabel
            // 
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(39, 17);
            this.statusLabel.Text = "Ready";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(564, 300);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.unexplodeButton);
            this.Controls.Add(this.explodeButton);
            this.Controls.Add(this.directoryLabel);
            this.Controls.Add(this.directoryBrowseButton);
            this.Controls.Add(this.directoryTextBox);
            this.Controls.Add(this.gafFileLabel);
            this.Controls.Add(this.gafFileBrowseButton);
            this.Controls.Add(this.gafFileTextBox);
            this.Name = "MainForm";
            this.Text = "GAF Explode";
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox gafFileTextBox;
        private System.Windows.Forms.Button gafFileBrowseButton;
        private System.Windows.Forms.Label gafFileLabel;
        private System.Windows.Forms.Label directoryLabel;
        private System.Windows.Forms.Button directoryBrowseButton;
        private System.Windows.Forms.TextBox directoryTextBox;
        private System.Windows.Forms.Button explodeButton;
        private System.Windows.Forms.Button unexplodeButton;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel statusLabel;
    }
}

