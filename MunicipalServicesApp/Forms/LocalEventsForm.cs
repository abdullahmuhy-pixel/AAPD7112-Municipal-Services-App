using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Forms
{
    /// <summary>
    /// Local Events and Announcements page (Part 2, Task 1b).
    ///
    /// Shows upcoming events, lets citizens search by category and date,
    /// surfaces featured/urgent announcements and a recently-viewed history,
    /// and recommends events based on what the citizen has searched for.
    /// See EventRepository and RecommendationEngine for where each required
    /// data structure (stacks, queues, priority queue, dictionaries, sorted
    /// dictionary, sets) is actually used.
    /// </summary>
    public class LocalEventsForm : Form
    {
        private readonly RecommendationEngine _recommender = new RecommendationEngine();

        private ComboBox cboCategory;
        private ComboBox cboDate;
        private System.Collections.Generic.List<DateTime> _availableDates = new System.Collections.Generic.List<DateTime>();
        private Button btnSearch;
        private Button btnClear;

        private TabControl tabs;
        private ListView lvAllEvents;
        private ListView lvFeatured;
        private ListView lvRecentlyViewed;
        private ListView lvLatestAnnouncements;

        private ListBox lstRecommended;
        private Label lblRecommendedHeading;
        private Label lblResultsCount;

        public LocalEventsForm()
        {
            InitializeComponent();
            PopulateAllTabs();
            RefreshRecommendations();
        }

        private void InitializeComponent()
        {
            this.Text = "Local Events and Announcements";
            this.ClientSize = new Size(920, 640);
            this.MinimumSize = new Size(820, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5F);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Padding = new Padding(16)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // ---- Heading ----
            var lblHeading = new Label
            {
                Text = "Local Events and Announcements",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = MainMenuForm.PrimaryColor,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 10)
            };
            root.Controls.Add(lblHeading, 0, 0);
            root.SetColumnSpan(lblHeading, 2);

            // ---- Search bar ----
            var searchPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 10)
            };

            searchPanel.Controls.Add(new Label { Text = "Category:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
            cboCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190, Margin = new Padding(0, 4, 16, 4) };
            searchPanel.Controls.Add(cboCategory);

            searchPanel.Controls.Add(new Label { Text = "Date:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
            cboDate = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Margin = new Padding(0, 4, 16, 4) };
            searchPanel.Controls.Add(cboDate);

            btnSearch = new Button
            {
                Text = "Search",
                Width = 90,
                Height = 30,
                BackColor = MainMenuForm.PrimaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 2, 8, 2)
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += BtnSearch_Click;
            searchPanel.Controls.Add(btnSearch);

            btnClear = new Button
            {
                Text = "Clear",
                Width = 80,
                Height = 30,
                BackColor = Color.Gainsboro,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 2, 0, 2)
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += BtnClear_Click;
            searchPanel.Controls.Add(btnClear);

            // ---- Tabs (All Events / Featured / Recently Viewed / Announcements feed) ----
            tabs = new TabControl { Dock = DockStyle.Fill };

            var tabAll = new TabPage("All Events");
            lvAllEvents = MakeListView();
            lblResultsCount = new Label { Dock = DockStyle.Bottom, Height = 22, Text = "", ForeColor = Color.Gray };
            tabAll.Controls.Add(lvAllEvents);
            tabAll.Controls.Add(lblResultsCount);

            var tabFeatured = new TabPage("Featured && Urgent");
            lvFeatured = MakeListView();
            tabFeatured.Controls.Add(lvFeatured);

            var tabRecent = new TabPage("Recently Viewed");
            lvRecentlyViewed = MakeListView();
            tabRecent.Controls.Add(lvRecentlyViewed);

            var tabFeed = new TabPage("Latest Announcements");
            lvLatestAnnouncements = MakeListView();
            tabFeed.Controls.Add(lvLatestAnnouncements);

            tabs.TabPages.Add(tabAll);
            tabs.TabPages.Add(tabFeatured);
            tabs.TabPages.Add(tabRecent);
            tabs.TabPages.Add(tabFeed);

            var leftColumn = new Panel { Dock = DockStyle.Fill };
            leftColumn.Controls.Add(tabs);
            leftColumn.Controls.Add(searchPanel);
            // searchPanel is Dock.Top so it must be added after tabs (Fill) to sit above it.

            root.Controls.Add(leftColumn, 0, 1);

            // ---- Recommended panel ----
            var recPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 0, 0) };
            lblRecommendedHeading = new Label
            {
                Text = "Recommended for You",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = MainMenuForm.AccentColor
            };
            var lblRecommendedHint = new Label
            {
                Text = "Based on what you search for, we will suggest related events here.",
                Dock = DockStyle.Top,
                Height = 40,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic)
            };
            lstRecommended = new ListBox { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 9.5F), IntegralHeight = false };
            lstRecommended.DoubleClick += LstRecommended_DoubleClick;

            recPanel.Controls.Add(lstRecommended);
            recPanel.Controls.Add(lblRecommendedHint);
            recPanel.Controls.Add(lblRecommendedHeading);
            root.Controls.Add(recPanel, 1, 1);

            // ---- Back button ----
            var btnBack = new Button
            {
                Text = "Back to Main Menu",
                Width = 160,
                Height = 34,
                BackColor = Color.Gainsboro,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 10, 0, 0)
            };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Click += (s, e) => this.Close();
            root.Controls.Add(btnBack, 0, 2);

            this.Controls.Add(root);

            cboCategory.Items.Add("(All Categories)");
            foreach (var cat in EventRepository.Categories.OrderBy(c => c))
                cboCategory.Items.Add(cat);
            cboCategory.SelectedIndex = 0;

            // The Date dropdown only ever offers dates that actually have an
            // event on them (built from the HashSet<DateTime> of unique
            // dates in EventRepository), so picking a date always narrows
            // the results instead of silently doing nothing.
            _availableDates = EventRepository.Dates.OrderBy(d => d).ToList();
            cboDate.Items.Add("(All Dates)");
            foreach (var date in _availableDates)
                cboDate.Items.Add(date.ToString("dd MMM yyyy"));
            cboDate.SelectedIndex = 0;
        }

        private ListView MakeListView()
        {
            var lv = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                Font = new Font("Segoe UI", 9.5F)
            };
            lv.Columns.Add("Date", 110);
            lv.Columns.Add("Title", 220);
            lv.Columns.Add("Category", 160);
            lv.Columns.Add("Location", 180);
            lv.Columns.Add("Featured", 70);
            lv.MouseDoubleClick += Lv_MouseDoubleClick;
            return lv;
        }

        private void PopulateListView(ListView lv, System.Collections.Generic.IEnumerable<MunicipalEvent> events)
        {
            lv.BeginUpdate();
            lv.Items.Clear();
            foreach (var ev in events)
            {
                var item = new ListViewItem(ev.EventDate.ToString("dd MMM yyyy"));
                item.SubItems.Add(ev.Title);
                item.SubItems.Add(ev.Category);
                item.SubItems.Add(ev.Location);
                item.SubItems.Add(ev.IsFeatured ? "Yes" : "");
                item.Tag = ev;
                if (ev.IsFeatured) item.ForeColor = MainMenuForm.AccentColor;
                lv.Items.Add(item);
            }
            lv.EndUpdate();
        }

        private void PopulateAllTabs()
        {
            PopulateListView(lvAllEvents, EventRepository.AllEventsChronological());
            lblResultsCount.Text = $"Showing all {EventRepository.AllEventsChronological().Count} upcoming events.";

            PopulateListView(lvFeatured, EventRepository.FeaturedByUrgency(10));

            PopulateListView(lvRecentlyViewed, EventRepository.RecentlyViewed);

            // Queue<T> enumerates front-to-back, i.e. in the order items were
            // added, which is exactly the "latest announcements feed" order.
            PopulateListView(lvLatestAnnouncements, EventRepository.AnnouncementFeed);
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            string category = cboCategory.SelectedIndex > 0 ? cboCategory.SelectedItem.ToString() : null;
            DateTime? date = cboDate.SelectedIndex > 0 ? (DateTime?)_availableDates[cboDate.SelectedIndex - 1] : null;

            var results = EventRepository.Search(category, date);
            PopulateListView(lvAllEvents, results);
            lblResultsCount.Text = $"Showing {results.Count} matching event(s).";
            tabs.SelectedIndex = 0;

            if (!string.IsNullOrEmpty(category))
            {
                _recommender.RecordSearch(category);
                RefreshRecommendations();
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            cboCategory.SelectedIndex = 0;
            cboDate.SelectedIndex = 0;
            PopulateListView(lvAllEvents, EventRepository.AllEventsChronological());
            lblResultsCount.Text = $"Showing all {EventRepository.AllEventsChronological().Count} upcoming events.";
        }

        private void Lv_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var lv = sender as ListView;
            if (lv?.SelectedItems.Count > 0 && lv.SelectedItems[0].Tag is MunicipalEvent ev)
            {
                ShowEventDetails(ev);
            }
        }

        private void LstRecommended_DoubleClick(object sender, EventArgs e)
        {
            if (lstRecommended.SelectedItem is MunicipalEvent ev)
            {
                ShowEventDetails(ev);
            }
        }

        private void ShowEventDetails(MunicipalEvent ev)
        {
            MessageBox.Show(this,
                $"{ev.Title}\n\nCategory: {ev.Category}\nDate: {ev.EventDate:dddd, dd MMMM yyyy HH:mm}\nLocation: {ev.Location}\n\n{ev.Description}",
                "Event Details",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            EventRepository.MarkViewed(ev);
            _recommender.RecordViewed(ev);

            PopulateListView(lvRecentlyViewed, EventRepository.RecentlyViewed);
            RefreshRecommendations();
        }

        private void RefreshRecommendations()
        {
            var recommendations = _recommender.GetRecommendations(6);
            lstRecommended.Items.Clear();

            if (recommendations.Count == 0)
            {
                lblRecommendedHeading.Text = "Recommended for You";
                lstRecommended.Items.Add("Search for a category to get recommendations.");
                lstRecommended.Enabled = false;
                return;
            }

            lstRecommended.Enabled = true;
            var topCategory = _recommender.TopSearchedCategory();
            lblRecommendedHeading.Text = $"Recommended: {topCategory}";

            foreach (var ev in recommendations)
                lstRecommended.Items.Add(ev);
        }
    }
}
