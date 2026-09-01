using System;
using System.Drawing;
using System.Windows.Forms;

namespace MunicipalServicesApp.Forms
{
    /// <summary>
    /// Application landing page. Presents the three top-level tasks
    /// required by the brief: Report Issues (implemented), Local Events
    /// and Announcements (disabled — future part), and Service Request
    /// Status (disabled — future part).
    /// </summary>
    public class MainMenuForm : Form
    {
        // Shared colour scheme so every form in the app looks consistent.
        public static readonly Color PrimaryColor = Color.FromArgb(0, 90, 156);   // municipal blue
        public static readonly Color AccentColor = Color.FromArgb(0, 153, 68);    // accent green
        public static readonly Color BackgroundColor = Color.White;

        private Label lblTitle;
        private Label lblSubtitle;
        private Button btnReportIssues;
        private Button btnLocalEvents;
        private Button btnServiceStatus;

        public MainMenuForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Municipal Services Application";
            this.ClientSize = new Size(480, 420);
            this.MinimumSize = new Size(420, 420);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = BackgroundColor;
            this.Font = new Font("Segoe UI", 10F);

            lblTitle = new Label
            {
                Text = "Municipal Services",
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = PrimaryColor,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 70
            };

            lblSubtitle = new Label
            {
                Text = "How can we help you today?",
                Font = new Font("Segoe UI", 11F, FontStyle.Italic),
                ForeColor = Color.Gray,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 36
            };

            var buttonPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(40, 10, 40, 30)
            };
            for (int i = 0; i < 3; i++)
            {
                buttonPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            }

            btnReportIssues = MakeMenuButton("Report Issues", true);
            btnLocalEvents = MakeMenuButton("Local Events and Announcements", false);
            btnServiceStatus = MakeMenuButton("Service Request Status", false);

            btnReportIssues.Click += BtnReportIssues_Click;

            buttonPanel.Controls.Add(btnReportIssues, 0, 0);
            buttonPanel.Controls.Add(btnLocalEvents, 0, 1);
            buttonPanel.Controls.Add(btnServiceStatus, 0, 2);

            // Dock order matters: Fill must be added first so Top-docked
            // labels correctly reserve their space above it.
            this.Controls.Add(buttonPanel);
            this.Controls.Add(lblSubtitle);
            this.Controls.Add(lblTitle);
        }

        private Button MakeMenuButton(string text, bool enabled)
        {
            var btn = new Button
            {
                Text = enabled ? text : $"{text}  (coming soon)",
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 0, 8),
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Enabled = enabled,
                BackColor = enabled ? PrimaryColor : Color.Gainsboro,
                ForeColor = enabled ? Color.White : Color.DimGray,
                Cursor = enabled ? Cursors.Hand : Cursors.Default,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = enabled
                ? ControlPaint.Light(PrimaryColor)
                : Color.Gainsboro;
            return btn;
        }

        private void BtnReportIssues_Click(object sender, EventArgs e)
        {
            using (var form = new ReportIssueForm())
            {
                form.ShowDialog(this);
            }
        }
    }
}
