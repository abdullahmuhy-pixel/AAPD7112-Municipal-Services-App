# Municipal Services Application - Part 2 (Local Events and Announcements)

PROG7312 / AAPD7112: Programming 3B: Advanced Application Development
C# .NET Framework (net472) Windows Forms application.

This builds on Part 1. The Report Issues feature from Part 1 is unchanged.
Part 2 adds the Local Events and Announcements feature.

## How to Compile and Run

Same requirements and steps as Part 1 (Visual Studio 2022, .NET desktop
development workload, .NET Framework 4.7.2 targeting pack).

1. Open `MunicipalServicesApp.sln` in Visual Studio.
2. Press F5, or Build then Debug without Debugging.
3. The Main Menu now has two enabled buttons: Report Issues and Local
   Events and Announcements. Service Request Status is still disabled
   (it gets built in the final PoE part).

## How to Use (Local Events and Announcements)

1. From the Main Menu, click **Local Events and Announcements**.
2. The **All Events** tab lists every upcoming event, sorted by date.
3. Use the **Category** dropdown and/or the **Date** picker (tick the
   Date checkbox to enable it) and click **Search** to filter. Every
   search is recorded and feeds the recommendation panel on the right.
4. Double-click any event, in any tab or in the Recommended list, to see
   its full details. This also marks it as viewed, moves it onto the
   Recently Viewed tab, and removes it from future recommendations.
5. Other tabs:
   - **Featured and Urgent**: events ordered by how soon they happen,
     with featured/urgent notices bumped ahead of ordinary events.
   - **Recently Viewed**: the events you have opened, most recent first.
   - **Latest Announcements**: every event in the order it was added to
     the system.
6. The **Recommended for You** panel on the right updates after every
   search and every event you open, based on which category you search
   for most.
7. Click **Back to Main Menu** to return.

## Where Each Required Data Structure Is Used

- `Models/EventRepository.cs`:
  - `SortedDictionary<DateTime, List<MunicipalEvent>>`: keeps events in
    chronological order automatically. Used for the All Events tab and
    as the base collection for searching.
  - `Dictionary<string, List<MunicipalEvent>>`: a hash table of events
    grouped by category, used for fast category search.
  - `HashSet<string>` / `HashSet<DateTime>`: the unique categories and
    dates currently in the system, used to populate the search filters
    without duplicates.
  - `Queue<MunicipalEvent>`: the Latest Announcements feed, in the order
    events were added (first in, first shown).
  - `Stack<MunicipalEvent>`: the Recently Viewed history, most recently
    opened event first.
- `Models/CustomPriorityQueue.cs`: a hand-written binary-heap priority
  queue (net472 does not include .NET's built-in `PriorityQueue` type,
  which only shipped in .NET 6), used to order the Featured and Urgent
  tab by how soon each event happens.
- `Models/RecommendationEngine.cs`: the recommendation feature. Tracks
  every search in a `Dictionary<string, int>` of category search counts,
  keeps a `Queue<string>` log of recent searches, and a `HashSet<Guid>`
  of already-viewed events so recommendations never repeat something the
  citizen has already opened.

## Known Limitations (Part 2 scope)

- Service Request Status remains disabled; it is built in the final PoE
  part.
- Event and search data is in memory only and resets each time the
  application restarts, since no persistence requirement is specified.
- Sample events are seeded on startup so the feature can be demonstrated
  without first using Part 1 to create data; they are not linked to
  issues reported in Part 1 (the two features are independent in this
  part of the brief).
