using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Forms
{
    /// <summary>
    /// Report Issues page. Lets a citizen capture the location, category,
    /// and description of an issue, optionally attach a supporting file,
    /// and submit it. Implements the chosen user engagement strategy from
    /// Task 1 (real-time feedback / status-tracking) as a live progress
    /// indicator and encouraging message that updates as the form is
    /// filled in.
    /// </summary>
    public class ReportIssueForm : Form
    {
        private const int TotalSteps = 4; // location, category, description, attachment

        private TextBox txtLocation;
        private ComboBox cboCategory;
        private RichTextBox rtbDescription;
        private Button btnAttach;
        private Label lblAttachment;
        private Button btnSubmit;
        private Button btnBack;
        private ProgressBar progressEngagement;
        private Label lblEngagement;

        private string _attachmentPath;

        public ReportIssueForm()
        {
            InitializeComponent();
            UpdateEngagement();
        }

        private void InitializeComponent()
        {
            this.Text = "Report an Issue";
            this.ClientSize = new Size(560, 640);
            this.MinimumSize = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 10F);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                Padding = new Padding(24),
                AutoSize = false
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var lblHeading = new Label
            {
                Text = "Report an Issue",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = MainMenuForm.PrimaryColor,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 12)
            };

            var lblLocation = MakeFieldLabel("Location");
            txtLocation = new TextBox
            {
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 0, 0, 12)
            };
            txtLocation.TextChanged += (s, e) => UpdateEngagement();

            var lblCategory = MakeFieldLabel("Category");
            cboCategory = new ComboBox
            {
                Dock = DockStyle.Top,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 0, 0, 12)
            };
            cboCategory.Items.AddRange(new object[]
            {
                IssueCategory.Sanitation,
                IssueCategory.Roads,
                IssueCategory.WaterAndSewage,
                IssueCategory.Electricity,
                IssueCategory.Utilities,
                IssueCategory.Other
            });
            cboCategory.SelectedIndexChanged += (s, e) => UpdateEngagement();

            var lblDescription = MakeFieldLabel("Description");
            rtbDescription = new RichTextBox
            {
                Dock = DockStyle.Top,
                Height = 140,
                Font = new Font("Segoe UI", 10F),
                Margin = new Padding(0, 0, 0, 12)
            };
            rtbDescription.TextChanged += (s, e) => UpdateEngagement();

            var attachRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 0, 0, 12)
            };
            btnAttach = new Button
            {
                Text = "Attach Image / Document...",
                AutoSize = true,
                Padding = new Padding(8, 4, 8, 4),
                BackColor = MainMenuForm.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnAttach.FlatAppearance.BorderSize = 0;
            btnAttach.Click += BtnAttach_Click;
            lblAttachment = new Label
            {
                Text = "No file attached",
                ForeColor = Color.Gray,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(12, 8, 0, 0)
            };
            attachRow.Controls.Add(btnAttach);
            attachRow.Controls.Add(lblAttachment);

            var engagementPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Margin = new Padding(0, 4, 0, 12)
            };
            lblEngagement = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                ForeColor = MainMenuForm.AccentColor,
                TextAlign = ContentAlignment.MiddleLeft
            };
            progressEngagement = new ProgressBar
            {
                Dock = DockStyle.Top,
                Height = 20,
                Minimum = 0,
                Maximum = TotalSteps,
                Value = 0
            };
            engagementPanel.Controls.Add(progressEngagement);
            engagementPanel.Controls.Add(lblEngagement);

            var buttonRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Margin = new Padding(0, 8, 0, 0)
            };
            btnSubmit = new Button
            {
                Text = "Submit",
                Width = 120,
                Height = 36,
                BackColor = MainMenuForm.PrimaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.Click += BtnSubmit_Click;

            btnBack = new Button
            {
                Text = "Back to Main Menu",
                Width = 160,
                Height = 36,
                BackColor = Color.Gainsboro,
                ForeColor = Color.Black,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 0, 12, 0)
            };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Click += (s, e) => this.Close();

            buttonRow.Controls.Add(btnSubmit);
            buttonRow.Controls.Add(btnBack);

            // A TableLayoutPanel with a single column and Dock.Top children
            // stacks them in the order they are added, top to bottom.
            layout.Controls.Add(lblHeading);
            layout.Controls.Add(lblLocation);
            layout.Controls.Add(txtLocation);
            layout.Controls.Add(lblCategory);
            layout.Controls.Add(cboCategory);
            layout.Controls.Add(lblDescription);
            layout.Controls.Add(rtbDescription);
            layout.Controls.Add(attachRow);
            layout.Controls.Add(engagementPanel);
            layout.Controls.Add(buttonRow);

            this.Controls.Add(layout);
        }

        private Label MakeFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 22,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.DimGray
            };
        }

        private void BtnAttach_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog
            {
                Title = "Attach an image or document",
                Filter = "Images and documents (*.jpg;*.jpeg;*.png;*.pdf;*.docx)|*.jpg;*.jpeg;*.png;*.pdf;*.docx|All files (*.*)|*.*"
            })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    _attachmentPath = dialog.FileName;
                    lblAttachment.Text = Path.GetFileName(_attachmentPath);
                    lblAttachment.ForeColor = MainMenuForm.AccentColor;
                    UpdateEngagement();
                }
            }
        }

        /// <summary>
        /// Engagement feature: recalculates how many of the four fields are
        /// complete and updates the progress bar and encouraging message
        /// accordingly. This is the real-time feedback strategy chosen and
        /// justified in Task 1.
        /// </summary>
        private void UpdateEngagement()
        {
            int completed = 0;
            if (!string.IsNullOrWhiteSpace(txtLocation.Text)) completed++;
            if (cboCategory.SelectedItem != null) completed++;
            if (!string.IsNullOrWhiteSpace(rtbDescription.Text)) completed++;
            if (!string.IsNullOrEmpty(_attachmentPath)) completed++;

            progressEngagement.Value = completed;

            switch (completed)
            {
                case 0:
                    lblEngagement.Text = "Let's get started — tell us where the issue is.";
                    break;
                case 1:
                    lblEngagement.Text = "Good start! A couple more details will help us route this quickly.";
                    break;
                case 2:
                    lblEngagement.Text = "Halfway there — thank you for helping improve your community.";
                    break;
                case 3:
                    lblEngagement.Text = "Almost done! Add a photo or document if you have one, then submit.";
                    break;
                case TotalSteps:
                    lblEngagement.Text = "All set — you're ready to submit your report.";
                    break;
            }
        }

        private void BtnSubmit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLocation.Text))
            {
                MessageBox.Show(this, "Please enter the location of the issue.", "Missing information",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtLocation.Focus();
                return;
            }

            if (cboCategory.SelectedItem == null)
            {
                MessageBox.Show(this, "Please select a category for the issue.", "Missing information",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cboCategory.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(rtbDescription.Text))
            {
                MessageBox.Show(this, "Please provide a description of the issue.", "Missing information",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                rtbDescription.Focus();
                return;
            }

            var issue = new Issue(
                txtLocation.Text.Trim(),
                (IssueCategory)cboCategory.SelectedItem,
                rtbDescription.Text.Trim(),
                _attachmentPath);

            IssueRepository.Add(issue);

            MessageBox.Show(this,
                $"Thank you! Your issue has been reported.\n\nReference number: {issue.ReferenceNumber}\n" +
                $"Total issues reported this session: {IssueRepository.Count}",
                "Issue submitted",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            ResetForm();
        }

        private void ResetForm()
        {
            txtLocation.Clear();
            cboCategory.SelectedIndex = -1;
            rtbDescription.Clear();
            _attachmentPath = null;
            lblAttachment.Text = "No file attached";
            lblAttachment.ForeColor = Color.Gray;
            UpdateEngagement();
            txtLocation.Focus();
        }
    }
}
