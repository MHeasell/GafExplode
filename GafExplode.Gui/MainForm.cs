using System;
using System.Windows.Forms;
using System.IO;

namespace GafExplode.Gui
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
            this.Icon = Properties.Resources.ProgramIcon;
        }

        private void gafFileBrowseButtonClick(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog();
            dialog.CheckFileExists = false;
            var result = dialog.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                this.gafFileTextBox.Text = dialog.FileName;
            }
        }

        private void directoryBrowseButtonClick(object sender, EventArgs e)
        {
            var dialog = new Ookii.Dialogs.WinForms.VistaFolderBrowserDialog();
            var result = dialog.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                this.directoryTextBox.Text = dialog.SelectedPath;
            }
        }

        private void explodeButtonClick(object sender, EventArgs e)
        {
            if (this.gafFileTextBox.Text == string.Empty || this.directoryTextBox.Text == string.Empty)
            {
                this.statusLabel.Text = "Enter paths first!";
                return;
            }

            if (!Path.IsPathRooted(this.gafFileTextBox.Text) || !Path.IsPathRooted(this.directoryTextBox.Text))
            {
                this.statusLabel.Text = "Paths must start with a root!";
                return;
            }

            this.statusLabel.Text = "Exploding...";
            this.Refresh();
            try
            {
                GafExplode.Program.ExplodeGaf(this.gafFileTextBox.Text, this.directoryTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error exploding", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.statusLabel.Text = "Something went wrong while exploding :(";
                return;
            }
            this.statusLabel.Text = "Finished exploding!";
        }

        private void unexplodeButtonClick(object sender, EventArgs e)
        {
            if (this.gafFileTextBox.Text == string.Empty || this.directoryTextBox.Text == string.Empty)
            {
                this.statusLabel.Text = "Enter paths first!";
                return;
            }

            if (!Path.IsPathRooted(this.gafFileTextBox.Text) || !Path.IsPathRooted(this.directoryTextBox.Text))
            {
                this.statusLabel.Text = "Paths must start with a root!";
                return;
            }

            this.statusLabel.Text = "Unexploding...";
            this.Refresh();
            try
            {
                var tempFile = Path.GetTempFileName();
                GafExplode.Program.UnexplodeGaf(this.directoryTextBox.Text, tempFile, this.trimCheckbox.Checked);
                File.Move(tempFile, this.gafFileTextBox.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error unexploding", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.statusLabel.Text = "Something went wrong while unexploding :(";
                return;
            }
            this.statusLabel.Text = "Finished unexploding!";
        }
    }
}
