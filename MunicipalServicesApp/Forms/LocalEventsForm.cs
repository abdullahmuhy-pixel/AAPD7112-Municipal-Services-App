using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using MunicipalServicesApp.Models;

namespace MunicipalServicesApp.Forms
{
    /// <summary>
    /// Local Events and Announcements page.
    ///
    /// Citizens can type a keyword into the search box and/or pick a category and
    /// a date, then press Search (or Enter). The page also shows featured and
    /// urgent announcements, a recently viewed history, the announcement feed,
    /// and recommends events based on what the citizen has searched for.
    /// See EventRepository and RecommendationEngine for where each required
    /// data structure (stacks, queues, priority queue, dictionaries, sorted
    /// dictionary, sets) is used.
    /// </summary>
    public class LocalEventsForm : Form
    {
        // Lets the search box show grey hint text ("cue banner") while it is empty.
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
        private const int EM_SETCUEBANNER = 0x1501;

        private readonly RecommendationEngine _recommender = new RecommendationEngine();
        private readonly ToolTip _tips = new ToolTip();
        private List<DateTime> _availableDates = new List<DateTime>();

        private TextBox txtSearch;
        private ComboBox cboCategory;
        private ComboBox cboDate;
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

        // =====================================================================
        // Layout
        // =====================================================================

        private void InitializeComponent()
        {
            this.Text = "Local Events and Announcements";
            this.ClientSize = new Size(1040, 680);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5F);

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                Padding = new Padding(16)
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));

            // ---- Heading ----
            var lblHeading = new Label
            {
                Text = "Local Events and Announcements",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = MainMenuForm.PrimaryColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            root.Controls.Add(lblHeading, 0, 0);
            root.SetColumnSpan(lblHeading, 2);

            // ---- Search card (spans the full width so nothing gets cut off) ----
            var searchCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 246, 252),
                Padding = new Padding(12, 8, 12, 4),
                Margin = new Padding(0, 0, 0, 10)
            };

            var cardLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            cardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            cardLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            var rowSearch = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };

            txtSearch = new TextBox
            {
                Width = 430,
                Font = new Font("Segoe UI", 11F),
                Margin = new Padding(0, 4, 10, 0)
            };
            txtSearch.HandleCreated += (s, e) =>
                SendMessage(txtSearch.Handle, EM_SETCUEBANNER, (IntPtr)1, "Type a keyword, for example: health, water, youth, market");
            _tips.SetToolTip(txtSearch, "Type any word from an event's title, category, location or description, then press Enter.");
            rowSearch.Controls.Add(txtSearch);

            btnSearch = new Button
            {
                Text = "Search",
                Width = 100,
                Height = 32,
                BackColor = MainMenuForm.PrimaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 3, 8, 0)
            };
            btnSearch.FlatAppearance.BorderSize = 0;
            btnSearch.Click += BtnSearch_Click;
            rowSearch.Controls.Add(btnSearch);

            btnClear = new Button
            {
                Text = "Clear",
                Width = 90,
                Height = 32,
                BackColor = Color.Gainsboro,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 3, 0, 0)
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += BtnClear_Click;
            rowSearch.Controls.Add(btnClear);

            var rowFilters = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            rowFilters.Controls.Add(new Label { Text = "Category:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
            cboCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Margin = new Padding(0, 4, 18, 0) };
            _tips.SetToolTip(cboCategory, "Narrow the results to one category.");
            rowFilters.Controls.Add(cboCategory);

            rowFilters.Controls.Add(new Label { Text = "Date:", AutoSize = true, Margin = new Padding(0, 8, 4, 0) });
            cboDate = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 150, Margin = new Padding(0, 4, 18, 0) };
            _tips.SetToolTip(cboDate, "Only dates that have an event are listed.");
            rowFilters.Controls.Add(cboDate);

            rowFilters.Controls.Add(new Label
            {
                Text = "Tip: use the search box, the filters, or both together.",
                AutoSize = true,
                ForeColor = Color.Gray,
                Margin = new Padding(0, 8, 0, 0)
            });

            cardLayout.Controls.Add(rowSearch, 0, 0);
            cardLayout.Controls.Add(rowFilters, 0, 1);
            searchCard.Controls.Add(cardLayout);
            root.Controls.Add(searchCard, 0, 1);
            root.SetColumnSpan(searchCard, 2);

            // ---- Tabs ----
            tabs = new TabControl { Dock = DockStyle.Fill };

            var tabAll = new TabPage("All Events");
            var allTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            allTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            allTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            allTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
            lvAllEvents = MakeListView();
            lblResultsCount = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, ForeColor = Color.Gray };
            allTable.Controls.Add(lvAllEvents, 0, 0);
            allTable.Controls.Add(lblResultsCount, 0, 1);
            tabAll.Controls.Add(allTable);

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
            root.Controls.Add(tabs, 0, 2);

            // ---- Recommended panel ----
            var recTable = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12, 0, 0, 0) };
            recTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            recTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            recTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));
            recTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            lblRecommendedHeading = new Label
            {
                Text = "Recommended for You",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = MainMenuForm.AccentColor,
                TextAlign = ContentAlignment.MiddleLeft
            };
            var lblRecommendedHint = new Label
            {
                Text = "Based on what you search for. Double click a suggestion to see its details.",
                Dock = DockStyle.Fill,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic)
            };

            lstRecommended = new ListBox
            {
                Dock = DockStyle.Fill,
                IntegralHeight = false,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 46,
                BorderStyle = BorderStyle.FixedSingle
            };
            lstRecommended.DrawItem += LstRecommended_DrawItem;
            lstRecommended.DoubleClick += LstRecommended_DoubleClick;

            recTable.Controls.Add(lblRecommendedHeading, 0, 0);
            recTable.Controls.Add(lblRecommendedHint, 0, 1);
            recTable.Controls.Add(lstRecommended, 0, 2);
            root.Controls.Add(recTable, 1, 2);

            // ---- Back button ----
            var btnBack = new Button
            {
                Text = "Back to Main Menu",
                Width = 160,
                Height = 34,
                BackColor = Color.Gainsboro,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(0, 10, 0, 0)
            };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Click += (s, e) => this.Close();
            root.Controls.Add(btnBack, 0, 3);

            this.Controls.Add(root);

            // Pressing Enter anywhere on the page runs the search.
            this.AcceptButton = btnSearch;

            // ---- Dropdown contents ----
            cboCategory.Items.Add("(All Categories)");
            foreach (var cat in EventRepository.Categories.OrderBy(c => c))
                cboCategory.Items.Add(cat);
            cboCategory.SelectedIndex = 0;

            // The Date dropdown only offers dates that actually have an event
            // (built from the HashSet<DateTime> of unique dates), so choosing a
            // date always narrows the results.
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
            lv.Columns.Add("Date", 95);
            lv.Columns.Add("Title", 205);
            lv.Columns.Add("Category", 150);
            lv.Columns.Add("Location", 150);
            lv.Columns.Add("Featured", 70);
            lv.MouseDoubleClick += Lv_MouseDoubleClick;
            return lv;
        }

        // =====================================================================
        // Filling the lists
        // =====================================================================

        private void PopulateListView(ListView lv, IEnumerable<MunicipalEvent> events)
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
            var all = EventRepository.AllEventsChronological();
            PopulateListView(lvAllEvents, all);
            ShowResultsMessage("Showing all " + all.Count + " upcoming events.", false);

            PopulateListView(lvFeatured, EventRepository.FeaturedByUrgency(10));

            // Stack<T> enumerates last in first out, so the most recently viewed event is first.
            PopulateListView(lvRecentlyViewed, EventRepository.RecentlyViewed);

            // Queue<T> enumerates first in first out: the order events were announced.
            PopulateListView(lvLatestAnnouncements, EventRepository.AnnouncementFeed);
        }

        private void ShowResultsMessage(string message, bool isWarning)
        {
            lblResultsCount.Text = message;
            lblResultsCount.ForeColor = isWarning ? Color.Firebrick : Color.Gray;
        }

        // =====================================================================
        // Searching
        // =====================================================================

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            string keyword = txtSearch.Text.Trim();
            string category = cboCategory.SelectedIndex > 0 ? cboCategory.SelectedItem.ToString() : null;
            DateTime? date = cboDate.SelectedIndex > 0 ? (DateTime?)_availableDates[cboDate.SelectedIndex - 1] : null;

            var results = EventRepository.Search(category, date, keyword);
            PopulateListView(lvAllEvents, results);
            tabs.SelectedIndex = 0;

            if (results.Count == 0)
            {
                ShowResultsMessage("No events found. Try a different word, or set the filters back to All.", true);
            }
            else
            {
                ShowResultsMessage("Showing " + results.Count + " matching event(s)" + DescribeFilters(keyword, category, date) + ".", false);
            }

            RecordSearchForRecommendations(results, keyword, category);
            RefreshRecommendations();
        }

        private static string DescribeFilters(string keyword, string category, DateTime? date)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(keyword)) parts.Add("for \"" + keyword + "\"");
            if (category != null) parts.Add("in " + category);
            if (date.HasValue) parts.Add("on " + date.Value.ToString("dd MMM yyyy"));
            return parts.Count == 0 ? string.Empty : " " + string.Join(" ", parts);
        }

        /// <summary>
        /// Feeds the search into the recommendation engine. A category search counts for
        /// that category. A typed keyword search counts once for each category among the
        /// events it found, so typing "water" still teaches the app what the citizen cares about.
        /// </summary>
        private void RecordSearchForRecommendations(List<MunicipalEvent> results, string keyword, string category)
        {
            if (category != null)
            {
                _recommender.RecordSearch(category);
            }
            else if (!string.IsNullOrEmpty(keyword))
            {
                foreach (var foundCategory in results.Select(r => r.Category).Distinct())
                    _recommender.RecordSearch(foundCategory);
            }
        }

        private void BtnClear_Click(object sender, EventArgs e)
        {
            txtSearch.Clear();
            cboCategory.SelectedIndex = 0;
            cboDate.SelectedIndex = 0;

            var all = EventRepository.AllEventsChronological();
            PopulateListView(lvAllEvents, all);
            ShowResultsMessage("Showing all " + all.Count + " upcoming events.", false);
            txtSearch.Focus();
        }

        // =====================================================================
        // Event details and recommendations
        // =====================================================================

        private void Lv_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            var lv = sender as ListView;
            if (lv != null && lv.SelectedItems.Count > 0)
            {
                var ev = lv.SelectedItems[0].Tag as MunicipalEvent;
                if (ev != null) ShowEventDetails(ev);
            }
        }

        private void LstRecommended_DoubleClick(object sender, EventArgs e)
        {
            var ev = lstRecommended.SelectedItem as MunicipalEvent;
            if (ev != null) ShowEventDetails(ev);
        }

        private void ShowEventDetails(MunicipalEvent ev)
        {
            MessageBox.Show(this,
                ev.Title + "\n\nCategory: " + ev.Category + "\nDate: " + ev.EventDate.ToString("dddd, dd MMMM yyyy HH:mm")
                + "\nLocation: " + ev.Location + "\n\n" + ev.Description,
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
                lstRecommended.Items.Add("Search by keyword or category to get recommendations.");
                lstRecommended.Enabled = false;
                return;
            }

            lstRecommended.Enabled = true;
            lblRecommendedHeading.Text = "Recommended: " + _recommender.TopSearchedCategory();

            foreach (var ev in recommendations)
                lstRecommended.Items.Add(ev);
        }

        // Draws each suggestion as a small card: bold title, then category and date underneath.
        private void LstRecommended_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.DrawBackground();

            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            object item = lstRecommended.Items[e.Index];
            var area = new RectangleF(e.Bounds.X + 8, e.Bounds.Y + 4, e.Bounds.Width - 16, e.Bounds.Height - 8);

            using (var titleFont = new Font("Segoe UI", 10F, FontStyle.Bold))
            using (var subFont = new Font("Segoe UI", 8.5F))
            using (var oneLine = new StringFormat { Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            using (var wrapped = new StringFormat { Trimming = StringTrimming.EllipsisWord })
            {
                var ev = item as MunicipalEvent;
                if (ev == null)
                {
                    using (var grey = new SolidBrush(Color.Gray))
                    {
                        e.Graphics.DrawString(item.ToString(), subFont, grey, area, wrapped);
                    }
                }
                else
                {
                    using (var titleBrush = new SolidBrush(selected ? SystemColors.HighlightText : Color.Black))
                    using (var subBrush = new SolidBrush(selected ? SystemColors.HighlightText : Color.Gray))
                    {
                        e.Graphics.DrawString(ev.Title, titleFont, titleBrush,
                            new RectangleF(area.X, area.Y, area.Width, 20), oneLine);
                        e.Graphics.DrawString(ev.Category + "  |  " + ev.EventDate.ToString("ddd, dd MMM yyyy"), subFont, subBrush,
                            new RectangleF(area.X, area.Y + 20, area.Width, 18), oneLine);
                    }
                }
            }

            using (var divider = new Pen(Color.Gainsboro))
            {
                e.Graphics.DrawLine(divider, e.Bounds.X, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }
        }
    }
}
