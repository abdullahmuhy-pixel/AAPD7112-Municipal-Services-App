# Municipal Services Application - PoE Part 3 (Service Request Status)

PROG7312 / AAPD7112: Programming 3B: Advanced Application Development
C# .NET Framework (net472) Windows Forms application.

This builds on Part 1 (Report Issues) and Part 2 (Local Events and Announcements).
Part 3 adds the Service Request Status feature, so all three Main Menu options now work.

## How to Compile and Run

Requirements: Windows 10/11, Visual Studio 2022 with the .NET desktop development
workload, and the .NET Framework 4.7.2 targeting pack.

1. Open `MunicipalServicesApp.sln` in Visual Studio.
2. If Visual Studio complains about a missing project.assets.json file, right click the
   solution and choose Restore NuGet Packages (no extra packages are needed, this just
   sets up the project), then rebuild.
3. Press F5, or Build then Debug, Start Without Debugging.
4. The Main Menu opens with all three buttons enabled.

## How to Use Service Request Status

Click **Service Request Status** on the Main Menu. Twelve sample requests are loaded
(SR-1001 to SR-1012). Any issue you report through **Report Issues** is added too, using the
reference number shown after you submit it, so you can track your own reports.

- **All Requests**: every request in date order. Use the status dropdown to filter.
  Double click a row to track it.
- **Track a Request**: type a request ID (for example SR-1002) and press Track or Enter.
  You see the details, a progress bar showing the stage (1 to 4), the status history, how
  many comparisons the AVL tree needed compared with a plain binary search tree, and a list
  of related requests. "Advance Status" simulates a staff update so you can watch the status
  change.
- **Urgent Queue**: requests waiting to be picked up, most urgent first. "Process Next
  Request" takes the top one and marks it Assigned.
- **Request Network**: pick a request, then run a Breadth First or Depth First search, or
  build the Minimum Spanning Tree of all open requests.
- **Category Tree**: the municipality, its categories, and the requests under each one.
- **Structure Stats**: heights, rotations and sizes of every structure.

## What You Should See on a Fresh Start

- Structure Stats: BST height 12, AVL height 4 (8 rotations), Red-Black height 5 with black
  height 3 and rules satisfied, 12 graph vertices, 19 edges, 1 connected group.
- Urgent Queue order: SR-1008, SR-1007, SR-1011, SR-1003, SR-1004.
- Minimum Spanning Tree: 10 open requests, 9 links, 28.29 km in total.

## Data Structures and Their Role

All custom structures are in the `Structures` folder and were written from scratch,
because .NET Framework 4.7.2 has no built in AVL tree, red-black tree, heap or graph.

**General tree (`GeneralTree.cs`)**: a node can have any number of children. It models the
hierarchy Municipality, then Category, then Request, and feeds the Category Tree tab.
Example: "Roads (3)" has three child nodes, one for each roads request.

**Binary search tree (`BinarySearchTree.cs`)**: each node has a left child with a smaller
key and a right child with a larger key. It is the baseline. Because the request IDs
arrive in order (SR-1001, SR-1002, ...), it turns into a chain of height 12, so finding
SR-1012 takes 12 comparisons. The Track tab shows this number next to the AVL result.

**AVL tree (`AvlTree.cs`)**: a binary search tree that checks the balance factor of every
node after an insert and fixes it with rotations. With the same 12 IDs its height is 4, so
any request is found in at most 4 comparisons. It is the main index for tracking by ID.

**Red-black tree (`RedBlackTree.cs`)**: nodes are coloured red or black following rules that
keep the tree balanced (black root, no red node with a red child, equal black height on every
path). It is keyed by date submitted, so an in-order walk gives requests oldest first, which is
what the All Requests tab shows. It rebalances with fewer rotations than AVL, which suits data
that keeps arriving. The Structure Stats tab runs a check of all three rules.

**Min-heap (`BinaryHeap.cs`)**: a binary tree stored in a list where the smallest item is
always at the top. Requests are ordered by priority number (1 is critical) and then by oldest
date. Inserting and removing the top both take O(log n). It powers the Urgent Queue and the
Process Next Request button. Requests that were already moved on by hand are skipped when they
reach the top (lazy removal).

**Graph (`RequestGraph.cs`)**: each request is a vertex and an edge links two requests that share
a category or are within 4 km of each other, weighted by the distance. It is stored as an
adjacency list.
- Breadth first search (a queue) finds related requests level by level. The Track tab uses it
  to list requests within 2 hops.
- Depth first search (recursion) follows one chain of links as far as it goes before backing up.
- Kruskal's minimum spanning tree sorts the edges by distance and adds each one unless it would
  make a loop (checked with union-find). Over the open requests it gives the cheapest set of
  links joining them all, which could be used to plan one crew route.

## How Part 1 Connects to Part 3

When an issue is submitted in Report Issues, `ServiceRequestRepository.AddFromIssue` creates a
matching service request with the same reference number. Free text locations cannot be turned
into real map positions without a mapping service, so a repeatable position on a grid is worked
out from the location text. The same text always gives the same position.

## Changes Made From Lecturer Feedback (Part 2)

Part 2 feedback: the category search did not let the user type into a text field, and the
interface should be more user friendly. In Local Events and Announcements:

- There is now a search box where the user can type any word. It matches the title, category,
  location and description, and several words must all match. It shows grey hint text, and
  pressing Enter runs the search.
- The search bar now spans the whole window, so the Search and Clear buttons are no longer cut off.
- The typed search, category dropdown and date dropdown can be used alone or together.
- The results line explains what was searched, and shows a clear message when nothing is found.
- Recommended events are drawn as small cards (title, then category and date) so text is no longer
  cut off, and typed searches also feed the recommendations.
- Tooltips explain each search control.

## Known Limitations

- Data is held in memory only and resets when the application closes.
- The red-black tree supports insert and search only, because requests are never deleted.
- "Advance Status" simulates a municipal staff update, since the application has no staff side.
