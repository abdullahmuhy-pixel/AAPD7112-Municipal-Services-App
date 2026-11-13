using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MunicipalServicesApp.Models;
using MunicipalServicesApp.Structures;

namespace MunicipalServicesApp.Forms
{
    /// <summary>
    /// Service Request Status page (PoE Part 3).
    ///
    /// Tabs and the data structure behind each one:
    ///   All Requests    : red-black tree (requests in date order)
    ///   Track a Request : AVL tree lookup by ID (compared with a plain BST),
    ///                     plus breadth first search for related requests
    ///   Urgent Queue    : binary min-heap
    ///   Request Network : graph with BFS, DFS and a minimum spanning tree
    ///   Category Tree   : general tree
    ///   Structure Stats : heights, rotations and sizes of every structure
    /// </summary>
    public class ServiceRequestStatusForm : Form
    {
        // A small wrapper so list boxes and combo boxes can show friendly text but still carry the request ID.
        private class IdItem
        {
            public string Id;
            public string Text;
            public override string ToString() { return Text; }
        }

        private TabControl tabs;
        private TabPage tabAll;
        private TabPage tabTrack;

        // All Requests tab
        private ComboBox cboStatusFilter;
        private ListView lvAll;
        private Label lblAllCount;

        // Track tab
        private TextBox txtTrackId;
        private Label lblDetails;
        private Label lblStage;
        private ProgressBar pbStatus;
        private ListBox lstHistory;
        private ListBox lstRelated;
        private Label lblLookup;
        private Button btnAdvance;
        private ServiceRequest _tracked;

        // Urgent queue tab
        private ListView lvUrgent;
        private Label lblProcessed;

        // Graph tab
        private ComboBox cboGraphStart;
        private ListView lvGraph;
        private Label lblGraphSummary;

        // Tree tab and stats tab
        private TreeView tvCategories;
        private TextBox txtStats;

        public ServiceRequestStatusForm()
        {
            InitializeComponent();
            RefreshAll();
        }

        // =====================================================================
        // Layout
        // =====================================================================

        private void InitializeComponent()
        {
            this.Text = "Service Request Status";
            this.ClientSize = new Size(1000, 680);
            this.MinimumSize = new Size(900, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 9.5F);

            var root = NewColumn();
            root.Padding = new Padding(16);

            var lblHeading = new Label
            {
                Text = "Service Request Status",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = MainMenuForm.PrimaryColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            AddRow(root, lblHeading, SizeType.Absolute, 40);

            tabs = new TabControl { Dock = DockStyle.Fill };
            tabAll = new TabPage("All Requests");
            tabTrack = new TabPage("Track a Request");
            var tabUrgent = new TabPage("Urgent Queue");
            var tabGraph = new TabPage("Request Network");
            var tabTree = new TabPage("Category Tree");
            var tabStats = new TabPage("Structure Stats");

            BuildAllTab(tabAll);
            BuildTrackTab(tabTrack);
            BuildUrgentTab(tabUrgent);
            BuildGraphTab(tabGraph);
            BuildTreeTab(tabTree);
            BuildStatsTab(tabStats);

            tabs.TabPages.Add(tabAll);
            tabs.TabPages.Add(tabTrack);
            tabs.TabPages.Add(tabUrgent);
            tabs.TabPages.Add(tabGraph);
            tabs.TabPages.Add(tabTree);
            tabs.TabPages.Add(tabStats);
            AddRow(root, tabs, SizeType.Percent, 100);

            var btnBack = new Button
            {
                Text = "Back to Main Menu",
                Width = 160,
                Height = 34,
                BackColor = Color.Gainsboro,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.Click += (s, e) => this.Close();
            AddRow(root, btnBack, SizeType.Absolute, 46);

            this.Controls.Add(root);
        }

        private void BuildAllTab(TabPage page)
        {
            var table = NewColumn();

            var filterRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            filterRow.Controls.Add(new Label { Text = "Show status:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) });
            cboStatusFilter = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170, Margin = new Padding(0, 4, 0, 0) };
            cboStatusFilter.Items.Add("(All Statuses)");
            foreach (RequestStatus status in Enum.GetValues(typeof(RequestStatus)))
                cboStatusFilter.Items.Add(ServiceRequest.StatusToText(status));
            cboStatusFilter.SelectedIndex = 0;
            cboStatusFilter.SelectedIndexChanged += (s, e) => FillAllRequests();
            filterRow.Controls.Add(cboStatusFilter);
            filterRow.Controls.Add(new Label
            {
                Text = "Sorted by date submitted. Double click a request to track it.",
                AutoSize = true,
                ForeColor = Color.Gray,
                Margin = new Padding(16, 8, 0, 0)
            });
            AddRow(table, filterRow, SizeType.Absolute, 38);

            lvAll = MakeListView(("Request ID", 100), ("Category", 140), ("Location", 200), ("Priority", 80), ("Status", 100), ("Submitted", 120));
            lvAll.MouseDoubleClick += RequestList_DoubleClick;
            AddRow(table, lvAll, SizeType.Percent, 100);

            lblAllCount = new Label { Dock = DockStyle.Fill, ForeColor = Color.Gray };
            AddRow(table, lblAllCount, SizeType.Absolute, 24);

            page.Controls.Add(table);
        }

        private void BuildTrackTab(TabPage page)
        {
            var outer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60F));
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40F));
            outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var inputRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            inputRow.Controls.Add(new Label { Text = "Request ID:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) });
            txtTrackId = new TextBox { Width = 170, Margin = new Padding(0, 4, 8, 0) };
            txtTrackId.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    TrackRequest(txtTrackId.Text);
                }
            };
            inputRow.Controls.Add(txtTrackId);

            var btnTrack = new Button
            {
                Text = "Track",
                Width = 90,
                Height = 28,
                BackColor = MainMenuForm.PrimaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnTrack.FlatAppearance.BorderSize = 0;
            btnTrack.Click += (s, e) => TrackRequest(txtTrackId.Text);
            inputRow.Controls.Add(btnTrack);
            inputRow.Controls.Add(new Label
            {
                Text = "Example: SR-1002, or the reference number you got after reporting an issue.",
                AutoSize = true,
                ForeColor = Color.Gray,
                Margin = new Padding(12, 8, 0, 0)
            });
            outer.Controls.Add(inputRow, 0, 0);
            outer.SetColumnSpan(inputRow, 2);

            // Left column: details, progress, history, lookup note, advance button
            var left = NewColumn();
            lblDetails = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                Text = "Enter a request ID above to see its status."
            };
            AddRow(left, lblDetails, SizeType.Absolute, 150);

            lblStage = new Label
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = MainMenuForm.AccentColor,
                TextAlign = ContentAlignment.MiddleLeft
            };
            AddRow(left, lblStage, SizeType.Absolute, 28);

            pbStatus = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 4, Value = 0, Margin = new Padding(3, 3, 3, 8) };
            AddRow(left, pbStatus, SizeType.Absolute, 30);

            AddRow(left, new Label
            {
                Text = "Status history",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.BottomLeft
            }, SizeType.Absolute, 26);

            lstHistory = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            AddRow(left, lstHistory, SizeType.Percent, 100);

            lblLookup = new Label { Dock = DockStyle.Fill, ForeColor = Color.Gray, Font = new Font("Segoe UI", 8.5F, FontStyle.Italic) };
            AddRow(left, lblLookup, SizeType.Absolute, 40);

            btnAdvance = new Button
            {
                Text = "Advance Status (demo staff update)",
                Width = 280,
                Height = 34,
                BackColor = MainMenuForm.AccentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Enabled = false,
                Anchor = AnchorStyles.Left | AnchorStyles.Top
            };
            btnAdvance.FlatAppearance.BorderSize = 0;
            btnAdvance.Click += BtnAdvance_Click;
            AddRow(left, btnAdvance, SizeType.Absolute, 44);

            outer.Controls.Add(left, 0, 1);

            // Right column: related requests found by breadth first search
            var right = NewColumn();
            right.Padding = new Padding(12, 0, 0, 0);
            AddRow(right, new Label
            {
                Text = "Related requests (within 2 hops, found with breadth first search). Double click to track one.",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = MainMenuForm.AccentColor
            }, SizeType.Absolute, 48);

            lstRelated = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
            lstRelated.DoubleClick += (s, e) =>
            {
                var item = lstRelated.SelectedItem as IdItem;
                if (item != null)
                {
                    txtTrackId.Text = item.Id;
                    TrackRequest(item.Id);
                }
            };
            AddRow(right, lstRelated, SizeType.Percent, 100);
            outer.Controls.Add(right, 1, 1);

            page.Controls.Add(outer);
        }

        private void BuildUrgentTab(TabPage page)
        {
            var table = NewColumn();

            AddRow(table, new Label
            {
                Text = "Requests still waiting to be picked up, held in a min-heap. The most critical request, and the oldest when priorities tie, is always at the top.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray
            }, SizeType.Absolute, 44);

            lvUrgent = MakeListView(("Request ID", 100), ("Category", 140), ("Location", 200), ("Priority", 80), ("Status", 100), ("Submitted", 120));
            lvUrgent.MouseDoubleClick += RequestList_DoubleClick;
            AddRow(table, lvUrgent, SizeType.Percent, 100);

            var actionRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            var btnProcess = new Button
            {
                Text = "Process Next Request",
                Width = 200,
                Height = 34,
                BackColor = MainMenuForm.PrimaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false
            };
            btnProcess.FlatAppearance.BorderSize = 0;
            btnProcess.Click += BtnProcess_Click;
            actionRow.Controls.Add(btnProcess);

            lblProcessed = new Label { AutoSize = true, ForeColor = MainMenuForm.AccentColor, Margin = new Padding(14, 9, 0, 0) };
            actionRow.Controls.Add(lblProcessed);
            AddRow(table, actionRow, SizeType.Absolute, 46);

            page.Controls.Add(table);
        }

        private void BuildGraphTab(TabPage page)
        {
            var table = NewColumn();

            var controlRow = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
            controlRow.Controls.Add(new Label { Text = "Start from:", AutoSize = true, Margin = new Padding(0, 8, 6, 0) });
            cboGraphStart = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230, Margin = new Padding(0, 4, 10, 0) };
            controlRow.Controls.Add(cboGraphStart);
            controlRow.Controls.Add(MakeGraphButton("Breadth First", BtnBfs_Click));
            controlRow.Controls.Add(MakeGraphButton("Depth First", BtnDfs_Click));
            controlRow.Controls.Add(MakeGraphButton("Minimum Spanning Tree", BtnMst_Click, 190));
            AddRow(table, controlRow, SizeType.Absolute, 44);

            AddRow(table, new Label
            {
                Text = "Each request is a vertex. Two requests are linked when they share a category or are within "
                       + ServiceRequestRepository.LinkDistanceKm.ToString("0.#")
                       + " km of each other, and the link is weighted by the distance between them.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray
            }, SizeType.Absolute, 44);

            lvGraph = MakeListView(("Pick a search or the spanning tree above", 400));
            AddRow(table, lvGraph, SizeType.Percent, 100);

            lblGraphSummary = new Label { Dock = DockStyle.Fill, ForeColor = MainMenuForm.AccentColor, Font = new Font("Segoe UI", 9.5F, FontStyle.Bold) };
            AddRow(table, lblGraphSummary, SizeType.Absolute, 48);

            page.Controls.Add(table);
        }

        private Button MakeGraphButton(string text, EventHandler handler, int width = 130)
        {
            var btn = new Button
            {
                Text = text,
                Width = width,
                Height = 30,
                BackColor = MainMenuForm.PrimaryColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 2, 8, 2)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += handler;
            return btn;
        }

        private void BuildTreeTab(TabPage page)
        {
            var table = NewColumn();
            AddRow(table, new Label
            {
                Text = "A general tree: the municipality at the top, then each category, then the requests in that category.",
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray
            }, SizeType.Absolute, 30);

            tvCategories = new TreeView { Dock = DockStyle.Fill, Font = new Font("Segoe UI", 10F) };
            AddRow(table, tvCategories, SizeType.Percent, 100);
            page.Controls.Add(table);
        }

        private void BuildStatsTab(TabPage page)
        {
            txtStats = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 10F),
                BackColor = Color.White
            };
            page.Controls.Add(txtStats);
        }

        // ---------- small layout helpers ----------

        private static TableLayoutPanel NewColumn()
        {
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1 };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            return table;
        }

        private static void AddRow(TableLayoutPanel table, Control control, SizeType type, float size)
        {
            table.RowStyles.Add(new RowStyle(type, size));
            table.RowCount = table.RowStyles.Count;
            table.Controls.Add(control, 0, table.RowStyles.Count - 1);
        }

        private ListView MakeListView(params (string Header, int Width)[] columns)
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
            foreach (var column in columns)
                lv.Columns.Add(column.Header, column.Width);
            return lv;
        }

        // =====================================================================
        // Refreshing the screen
        // =====================================================================

        private void RefreshAll()
        {
            FillAllRequests();
            FillUrgentQueue();
            FillGraphStartList();
            FillCategoryTree();
            FillStats();

            if (_tracked != null)
                ShowRequest(_tracked);
        }

        private void FillAllRequests()
        {
            IEnumerable<ServiceRequest> requests = ServiceRequestRepository.AllByDate();

            if (cboStatusFilter.SelectedIndex > 0)
            {
                var wanted = (RequestStatus)cboStatusFilter.SelectedIndex; // list order matches the enum values
                requests = requests.Where(r => r.Status == wanted);
            }

            var list = requests.ToList();
            FillRequestList(lvAll, list);
            lblAllCount.Text = "Showing " + list.Count + " of " + ServiceRequestRepository.Count + " requests.";
        }

        private void FillUrgentQueue()
        {
            var waiting = ServiceRequestRepository.UrgentQueue();
            FillRequestList(lvUrgent, waiting);
        }

        private void FillRequestList(ListView lv, IEnumerable<ServiceRequest> requests)
        {
            lv.BeginUpdate();
            lv.Items.Clear();
            foreach (var request in requests)
            {
                var item = new ListViewItem(request.Id);
                item.SubItems.Add(request.Category);
                item.SubItems.Add(request.Location);
                item.SubItems.Add(request.PriorityText);
                item.SubItems.Add(request.StatusText);
                item.SubItems.Add(request.DateSubmitted.ToString("dd MMM yyyy"));
                item.ForeColor = StatusColour(request.Status);
                item.Tag = request;
                lv.Items.Add(item);
            }
            lv.EndUpdate();
        }

        private void FillGraphStartList()
        {
            string previous = null;
            var selected = cboGraphStart.SelectedItem as IdItem;
            if (selected != null) previous = selected.Id;

            cboGraphStart.Items.Clear();
            foreach (var request in ServiceRequestRepository.AllByDate())
            {
                cboGraphStart.Items.Add(new IdItem { Id = request.Id, Text = request.Id + "  (" + request.Category + ")" });
            }

            int index = 0;
            for (int i = 0; i < cboGraphStart.Items.Count; i++)
            {
                if (((IdItem)cboGraphStart.Items[i]).Id == previous)
                {
                    index = i;
                    break;
                }
            }

            if (cboGraphStart.Items.Count > 0)
                cboGraphStart.SelectedIndex = index;
        }

        private void FillCategoryTree()
        {
            tvCategories.BeginUpdate();
            tvCategories.Nodes.Clear();
            var root = ServiceRequestRepository.BuildCategoryTree();
            tvCategories.Nodes.Add(ConvertNode(root));
            tvCategories.ExpandAll();
            tvCategories.EndUpdate();
            if (tvCategories.Nodes.Count > 0)
                tvCategories.Nodes[0].EnsureVisible();
        }

        private static TreeNode ConvertNode(GeneralTreeNode<string> node)
        {
            var winNode = new TreeNode(node.Value);
            foreach (var child in node.Children)
                winNode.Nodes.Add(ConvertNode(child));
            return winNode;
        }

        private void FillStats()
        {
            var avl = ServiceRequestRepository.AvlIndex;
            var bst = ServiceRequestRepository.BstIndex;
            var rb = ServiceRequestRepository.DateIndex;
            var graph = ServiceRequestRepository.Graph;

            int n = ServiceRequestRepository.Count;
            int idealHeight = n == 0 ? 0 : (int)Math.Ceiling(Math.Log(n + 1, 2));
            string nl = Environment.NewLine;

            var sb = new StringBuilder();
            sb.Append("DATA STRUCTURES BEHIND THIS SCREEN").Append(nl).Append(nl);
            sb.Append("Requests stored: ").Append(n).Append(nl).Append(nl);

            sb.Append("Search trees (all hold the same requests)").Append(nl);
            sb.Append("  ").Append(bst.Name.PadRight(20)).Append("height ").Append(bst.Height.ToString().PadRight(4))
              .Append("(unbalanced)").Append(nl);
            sb.Append("  ").Append(avl.Name.PadRight(20)).Append("height ").Append(avl.Height.ToString().PadRight(4))
              .Append("rotations so far: ").Append(avl.Rotations).Append(nl);
            sb.Append("  ").Append(rb.Name.PadRight(20)).Append("height ").Append(rb.Height.ToString().PadRight(4))
              .Append("black height: ").Append(rb.BlackHeight)
              .Append("   rules satisfied: ").Append(rb.IsValid() ? "Yes" : "No").Append(nl);
            sb.Append("  Best possible height for ").Append(n).Append(" nodes is about ").Append(idealHeight).Append(".").Append(nl);
            sb.Append("  Request IDs arrive in sorted order, which is the worst case for the plain BST.").Append(nl).Append(nl);

            sb.Append("Min-heap (urgent queue)").Append(nl);
            sb.Append("  Requests waiting to be picked up: ").Append(ServiceRequestRepository.UrgentQueue().Count).Append(nl).Append(nl);

            sb.Append("Graph (request network)").Append(nl);
            sb.Append("  Vertices (requests): ").Append(graph.VertexCount).Append(nl);
            sb.Append("  Edges (links): ").Append(graph.EdgeCount).Append(nl);
            sb.Append("  Connected groups: ").Append(graph.CountComponents()).Append(nl).Append(nl);

            var tree = ServiceRequestRepository.BuildCategoryTree();
            sb.Append("General tree (category view)").Append(nl);
            sb.Append("  Nodes: ").Append(tree.CountNodes()).Append("   Levels: ").Append(tree.Height()).Append(nl);

            txtStats.Text = sb.ToString();
        }

        private static Color StatusColour(RequestStatus status)
        {
            switch (status)
            {
                case RequestStatus.Submitted: return Color.DimGray;
                case RequestStatus.Assigned: return Color.FromArgb(200, 110, 0);
                case RequestStatus.InProgress: return MainMenuForm.PrimaryColor;
                default: return MainMenuForm.AccentColor;
            }
        }

        // =====================================================================
        // Tracking a request
        // =====================================================================

        private void RequestList_DoubleClick(object sender, MouseEventArgs e)
        {
            var lv = sender as ListView;
            if (lv == null || lv.SelectedItems.Count == 0) return;

            var request = lv.SelectedItems[0].Tag as ServiceRequest;
            if (request == null) return;

            txtTrackId.Text = request.Id;
            TrackRequest(request.Id);
        }

        private void TrackRequest(string rawId)
        {
            string id = (rawId ?? string.Empty).Trim().ToUpperInvariant();

            if (id.Length == 0)
            {
                MessageBox.Show(this, "Please enter a request ID to track.", "Missing information",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtTrackId.Focus();
                return;
            }

            ServiceRequest request;
            int avlComparisons;
            int bstComparisons;
            if (!ServiceRequestRepository.TryFind(id, out request, out avlComparisons, out bstComparisons))
            {
                _tracked = null;
                ClearTrackPanel();
                tabs.SelectedTab = tabTrack;
                MessageBox.Show(this,
                    "No service request was found with the ID \"" + id + "\".\n\nCheck the ID and try again.",
                    "Request not found",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _tracked = request;
            txtTrackId.Text = request.Id;
            ShowRequest(request);
            tabs.SelectedTab = tabTrack;
        }

        private void ShowRequest(ServiceRequest request)
        {
            ServiceRequest ignored;
            int avlComparisons;
            int bstComparisons;
            ServiceRequestRepository.TryFind(request.Id, out ignored, out avlComparisons, out bstComparisons);

            var sb = new StringBuilder();
            sb.Append("Request ID:  ").Append(request.Id).AppendLine();
            sb.Append("Category:  ").Append(request.Category).AppendLine();
            sb.Append("Location:  ").Append(request.Location).AppendLine();
            sb.Append("Priority:  ").Append(request.PriorityText).AppendLine();
            sb.Append("Submitted:  ").Append(request.DateSubmitted.ToString("dd MMM yyyy HH:mm")).AppendLine();
            sb.Append("Description:  ").Append(request.Description).AppendLine();
            if (!string.IsNullOrEmpty(request.AttachmentPath))
                sb.Append("Attachment:  ").Append(System.IO.Path.GetFileName(request.AttachmentPath)).AppendLine();
            lblDetails.Text = sb.ToString();

            int stage = (int)request.Status;
            pbStatus.Value = stage;
            lblStage.Text = "Stage " + stage + " of 4: " + request.StatusText;
            lblStage.ForeColor = StatusColour(request.Status);

            lstHistory.Items.Clear();
            foreach (var line in request.History)
                lstHistory.Items.Add(line);

            lblLookup.Text = "Found with the AVL tree in " + avlComparisons + " comparison(s). A plain binary search tree needed "
                             + bstComparisons + " for the same ID.";

            btnAdvance.Enabled = request.Status != RequestStatus.Resolved;

            lstRelated.Items.Clear();
            var related = ServiceRequestRepository.Related(request.Id, 2);
            foreach (var entry in related)
            {
                ServiceRequest other = ServiceRequestRepository.Find(entry.Id);
                if (other == null) continue;

                string hops = entry.Depth == 1 ? "1 hop" : entry.Depth + " hops";
                lstRelated.Items.Add(new IdItem
                {
                    Id = other.Id,
                    Text = other.Id + "  (" + hops + ")  " + other.Category + ": " + other.Location + "  [" + other.StatusText + "]"
                });
            }
            if (related.Count == 0)
                lstRelated.Items.Add("No related requests.");
        }

        private void ClearTrackPanel()
        {
            lblDetails.Text = "Enter a request ID above to see its status.";
            lblStage.Text = string.Empty;
            pbStatus.Value = 0;
            lstHistory.Items.Clear();
            lstRelated.Items.Clear();
            lblLookup.Text = string.Empty;
            btnAdvance.Enabled = false;
        }

        private void BtnAdvance_Click(object sender, EventArgs e)
        {
            if (_tracked == null) return;

            if (_tracked.AdvanceStatus())
            {
                MessageBox.Show(this,
                    "Request " + _tracked.Id + " is now: " + _tracked.StatusText + ".\n\n(This simulates an update from municipal staff, so you can see the tracking screen change.)",
                    "Status updated",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            RefreshAll();
        }

        // =====================================================================
        // Urgent queue (heap)
        // =====================================================================

        private void BtnProcess_Click(object sender, EventArgs e)
        {
            ServiceRequest processed = ServiceRequestRepository.ProcessNext();

            if (processed == null)
            {
                lblProcessed.Text = "No requests are waiting to be picked up.";
            }
            else
            {
                lblProcessed.Text = "Picked up " + processed.Id + " (" + processed.PriorityText + " priority, "
                                    + processed.Category + ", " + processed.Location + "). It is now Assigned.";
            }

            RefreshAll();
        }

        // =====================================================================
        // Graph tools
        // =====================================================================

        private string SelectedGraphStart()
        {
            var item = cboGraphStart.SelectedItem as IdItem;
            return item == null ? null : item.Id;
        }

        private void SetGraphColumns(params (string Header, int Width)[] columns)
        {
            lvGraph.BeginUpdate();
            lvGraph.Items.Clear();
            lvGraph.Columns.Clear();
            foreach (var column in columns)
                lvGraph.Columns.Add(column.Header, column.Width);
            lvGraph.EndUpdate();
        }

        private void BtnBfs_Click(object sender, EventArgs e)
        {
            string start = SelectedGraphStart();
            if (start == null) return;

            var order = ServiceRequestRepository.Graph.BreadthFirst(start);
            SetGraphColumns(("Step", 60), ("Request ID", 100), ("Hops from start", 110), ("Category", 140), ("Location", 200), ("Status", 100));

            lvGraph.BeginUpdate();
            int step = 1;
            foreach (var entry in order)
            {
                ServiceRequest request = ServiceRequestRepository.Find(entry.Id);
                if (request == null) continue;

                var item = new ListViewItem(step.ToString());
                item.SubItems.Add(request.Id);
                item.SubItems.Add(entry.Depth.ToString());
                item.SubItems.Add(request.Category);
                item.SubItems.Add(request.Location);
                item.SubItems.Add(request.StatusText);
                item.ForeColor = StatusColour(request.Status);
                lvGraph.Items.Add(item);
                step++;
            }
            lvGraph.EndUpdate();

            lblGraphSummary.Text = "Breadth first search from " + start + " reached " + order.Count
                                   + " request(s), nearest links first, level by level.";
        }

        private void BtnDfs_Click(object sender, EventArgs e)
        {
            string start = SelectedGraphStart();
            if (start == null) return;

            var order = ServiceRequestRepository.Graph.DepthFirst(start);
            SetGraphColumns(("Step", 60), ("Request ID", 100), ("Category", 140), ("Location", 200), ("Status", 100));

            lvGraph.BeginUpdate();
            int step = 1;
            foreach (var id in order)
            {
                ServiceRequest request = ServiceRequestRepository.Find(id);
                if (request == null) continue;

                var item = new ListViewItem(step.ToString());
                item.SubItems.Add(request.Id);
                item.SubItems.Add(request.Category);
                item.SubItems.Add(request.Location);
                item.SubItems.Add(request.StatusText);
                item.ForeColor = StatusColour(request.Status);
                lvGraph.Items.Add(item);
                step++;
            }
            lvGraph.EndUpdate();

            lblGraphSummary.Text = "Depth first search from " + start + " followed one chain of links as far as it could, "
                                   + "then backed up. It visited " + order.Count + " request(s).";
        }

        private void BtnMst_Click(object sender, EventArgs e)
        {
            MstResult mst = ServiceRequestRepository.Graph.MinimumSpanningTree(id => ServiceRequestRepository.IsOpen(id));

            SetGraphColumns(("Link", 60), ("From", 100), ("To", 100), ("Distance (km)", 110), ("From location", 190), ("To location", 190));

            lvGraph.BeginUpdate();
            int number = 1;
            foreach (var edge in mst.Edges)
            {
                ServiceRequest fromRequest = ServiceRequestRepository.Find(edge.From);
                ServiceRequest toRequest = ServiceRequestRepository.Find(edge.To);

                var item = new ListViewItem(number.ToString());
                item.SubItems.Add(edge.From);
                item.SubItems.Add(edge.To);
                item.SubItems.Add(edge.Weight.ToString("0.00"));
                item.SubItems.Add(fromRequest == null ? string.Empty : fromRequest.Location);
                item.SubItems.Add(toRequest == null ? string.Empty : toRequest.Location);
                lvGraph.Items.Add(item);
                number++;
            }
            lvGraph.EndUpdate();

            string groups = mst.Components == 1
                ? "All open requests are connected."
                : "The open requests form " + mst.Components + " separate groups, so this is a spanning forest.";
            lblGraphSummary.Text = "Minimum spanning tree of the " + mst.VertexCount + " open requests: " + mst.Edges.Count
                                   + " links, " + mst.TotalWeight.ToString("0.00") + " km in total. " + groups;
        }
    }
}
