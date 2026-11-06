using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace workspacer
{
    public class Workspace : IWorkspace
    {
        private static Logger Logger = Logger.Create();

        public IEnumerable<IWindow> Windows
        {
            get
            {
                lock (_windows)
                {
                    // Return a copy to prevent race conditions.
                    return _windows.ToList();
                }
            }
        }

        public IList<IWindow> ManagedWindows
        {
            get
            {
                lock (_windows)
                {
                    return _windows
                        .Where(window =>
                            window.CanLayout &&
                            !(_context.CanMinimizeWindows &&
                              window.IsMinimized))
                        .ToList();
                }
            }
        }

        public IWindow FocusedWindow
        {
            get
            {
                lock (_windows)
                {
                    return _windows.FirstOrDefault(
                        window => window.IsFocused);
                }
            }
        }

        public IWindow LastFocusedWindow => _lastFocused;

        public string Name { get; set; }

        public string LayoutName =>
            _layoutEngines[_layoutIndex].Name;

        public bool IsIndicating { get; set; }

        private readonly IConfigContext _context;
        private readonly List<IWindow> _windows;
        private readonly ILayoutEngine[] _layoutEngines;

        private int _layoutIndex;
        private IWindow _lastFocused;

        public Workspace(
            IConfigContext context,
            string name,
            ILayoutEngine[] layoutEngines)
        {
            _context = context;
            _layoutEngines = layoutEngines;
            _layoutIndex = 0;
            _windows = new List<IWindow>();

            _lastFocused = null;
            Name = name;
        }

        public void AddWindow(
            IWindow window,
            bool layout = true)
        {
            lock (_windows)
            {
                if (_lastFocused == null &&
                    window.IsFocused)
                {
                    _lastFocused = window;
                }

                if (_context.NewWindowOrder ==
                    WindowOrder.NewWindowsFirst)
                {
                    _windows.Insert(0, window);
                }
                else
                {
                    _windows.Add(window);
                }

                if (layout)
                {
                    DoLayout();
                }
            }
        }

        public void RemoveWindow(
            IWindow window,
            bool layout = true)
        {
            lock (_windows)
            {
                if (_lastFocused == window)
                {
                    var windows = ManagedWindows;

                    var next = windows.Count > 1
                        ? windows[
                            (windows.IndexOf(window) + 1) %
                            windows.Count]
                        : null;

                    _lastFocused = next;
                }

                _windows.Remove(window);

                if (layout)
                {
                    DoLayout();
                }
            }
        }

        public void SetWindows(IEnumerable<IWindow> windows)
        {
            lock (_windows)
            {
                _windows.Clear();
                _windows.AddRange(windows);

                if (_lastFocused != null && !_windows.Contains(_lastFocused))
                {
                    _lastFocused = _windows.FirstOrDefault(window => window.IsFocused);
                }
            }
        }

        public void UpdateWindow(IWindow window, WindowUpdateType type, bool layout = true)
        {
            if (type == WindowUpdateType.Foreground)
            {
                _lastFocused = window;
            }

            if (layout)
            {
                DoLayout();
            }
        }

        public void CloseFocusedWindow()
        {
            var window = ManagedWindows
                .FirstOrDefault(window => window.IsFocused);

            window?.Close();
        }

        public void PreviousLayoutEngine()
        {
            if (_layoutIndex == 0)
            {
                _layoutIndex = _layoutEngines.Length - 1;
            }
            else
            {
                _layoutIndex--;
            }

            DoLayout();
        }

        public void NextLayoutEngine()
        {
            if (_layoutIndex + 1 == _layoutEngines.Length)
            {
                _layoutIndex = 0;
            }
            else
            {
                _layoutIndex++;
            }

            DoLayout();
        }

        public void ResetLayout()
        {
            GetLayoutEngine().ResetPrimaryArea();
            DoLayout();
        }

        public void FocusLastFocusedWindow()
        {
            if (_lastFocused != null)
            {
                _lastFocused.Focus();
            }
            else
            {
                FocusPrimaryWindow();
            }
        }

        public void FocusNextWindow()
        {
            var windows = ManagedWindows;
            var didFocus = false;

            for (var index = 0;
                 index < windows.Count;
                 index++)
            {
                var window = windows[index];

                if (!window.IsFocused)
                {
                    continue;
                }

                if (index + 1 == windows.Count)
                {
                    windows[0].Focus();
                }
                else
                {
                    windows[index + 1].Focus();
                }

                didFocus = true;
                break;
            }

            if (!didFocus && windows.Count > 0)
            {
                if (_lastFocused != null)
                {
                    _lastFocused.Focus();
                }
                else
                {
                    windows[0].Focus();
                }
            }
        }

        public void FocusPreviousWindow()
        {
            var windows = ManagedWindows;
            var didFocus = false;

            for (var index = 0;
                 index < windows.Count;
                 index++)
            {
                var window = windows[index];

                if (!window.IsFocused)
                {
                    continue;
                }

                if (index == 0)
                {
                    windows[windows.Count - 1].Focus();
                }
                else
                {
                    windows[index - 1].Focus();
                }

                didFocus = true;
                break;
            }

            if (!didFocus && windows.Count > 0)
            {
                if (_lastFocused != null)
                {
                    _lastFocused.Focus();
                }
                else
                {
                    windows[0].Focus();
                }
            }
        }

        public void FocusPrimaryWindow()
        {
            var window = ManagedWindows.FirstOrDefault();
            window?.Focus();
        }

        public void SwapFocusAndPrimaryWindow()
        {
            var windows = ManagedWindows;

            if (windows.Count <= 1)
            {
                return;
            }

            var primary = windows[0];
            var focused = windows.FirstOrDefault(
                window => window.IsFocused);

            if (focused != null)
            {
                SwapWindows(primary, focused);
            }
        }

        public void SwapFocusAndNextWindow()
        {
            var windows = ManagedWindows;

            for (var index = 0;
                 index < windows.Count;
                 index++)
            {
                var window = windows[index];

                if (!window.IsFocused)
                {
                    continue;
                }

                if (index + 1 == windows.Count)
                {
                    SwapWindows(window, windows[0]);
                }
                else
                {
                    SwapWindows(
                        window,
                        windows[index + 1]);
                }

                break;
            }
        }

        public void SwapFocusAndPreviousWindow()
        {
            var windows = ManagedWindows;

            for (var index = 0;
                 index < windows.Count;
                 index++)
            {
                var window = windows[index];

                if (!window.IsFocused)
                {
                    continue;
                }

                if (index == 0)
                {
                    SwapWindows(
                        window,
                        windows[windows.Count - 1]);
                }
                else
                {
                    SwapWindows(
                        window,
                        windows[index - 1]);
                }

                break;
            }
        }

        public void ShrinkPrimaryArea()
        {
            GetLayoutEngine().ShrinkPrimaryArea();
            DoLayout();
        }

        public void ExpandPrimaryArea()
        {
            GetLayoutEngine().ExpandPrimaryArea();
            DoLayout();
        }

        public void IncrementNumberOfPrimaryWindows()
        {
            GetLayoutEngine().IncrementNumInPrimary();
            DoLayout();
        }

        public void DecrementNumberOfPrimaryWindows()
        {
            GetLayoutEngine().DecrementNumInPrimary();
            DoLayout();
        }

        public void SwapWindowToPoint(
            IWindow window,
            int x,
            int y)
        {
            if (!ManagedWindows.Contains(window))
            {
                return;
            }

            var target = GetDropTarget(window,
                x,
                y,
                out _,
                out bool center,
                out bool before,
                out _);

            if (target != null)
            {
                ApplyDrop(window, target, center, before);
            }
        }

        /// <summary>
        /// Applies an already resolved drop operation.
        ///
        /// This allows WorkspaceManager to cache the target and its
        /// original rectangle while previewing a drag. The target does
        /// not need to be resolved again after the layout has changed.
        /// </summary>
        public bool ApplyDrop(
            IWindow window,
            IWindow target,
            bool center,
            bool before)
        {
            if (target == null ||
                target == window)
            {
                return false;
            }

            if (center)
            {
                Logger.Debug(
                    "ApplyDrop[{0} -> {1}] center",
                    window,
                    target);

                return SwapWindows(window, target);
            }

            if (!InsertWindow(window, target, before))
            {
                return false;
            }

            Logger.Debug(
                "ApplyDrop[{0} -> {1}] edge, before={2}",
                window,
                target,
                before);

            DoLayout();
            return true;
        }

        public IWindow GetDropTarget(IWindow draggedWindow,
            int x,
            int y,
            out int index,
            out bool center,
            out bool before,
            out Rectangle bounds)
        {
            index = -1;
            center = false;
            before = true;
            bounds = Rectangle.Empty;

            var windows = ManagedWindows.ToList();
            var monitor = _context.WorkspaceContainer
                .GetCurrentMonitorForWorkspace(this);

            if (monitor == null)
            {
                return null;
            }

            /*
             * Add the dragged window as a placeholder. For another workspace this
             * gives an empty workspace its first slot. For the current workspace
             * the duplicate exposes the next unused slot in count-based layouts.
             */
            windows.Add(draggedWindow);

            var layout = GetLayoutEngine()
                .CalcLayout(windows, monitor.Width, monitor.Height)
                .ToList();

            /*
             * A layout engine should return one location per supplied window,
             * but limit the loop defensively in case it does not.
             */
            var count = System.Math.Min(
                windows.Count,
                layout.Count);

            for (var slotIndex = 0; slotIndex < count; slotIndex++)
            {
                var slot = layout[slotIndex];

                if (slot.Width <= 0 ||
                    slot.Height <= 0)
                {
                    continue;
                }

                var slotBounds = new Rectangle(
                    monitor.X + slot.X,
                    monitor.Y + slot.Y,
                    slot.Width,
                    slot.Height);

                if (!slotBounds.Contains(x, y))
                {
                    continue;
                }

                index = slotIndex;
                bounds = slotBounds;

                double u =
                    (double)(x - bounds.Left) /
                    bounds.Width;

                double v =
                    (double)(y - bounds.Top) /
                    bounds.Height;

                center =
                    u >= 0.25 &&
                    u <= 0.75 &&
                    v >= 0.25 &&
                    v <= 0.75;

                /*
                 * Outside the center, choose the closest logical edge.
                 *
                 * A greater horizontal displacement means left/right.
                 * Otherwise the pointer indicates above/below.
                 */
                before =
                    System.Math.Abs(u - 0.5) >
                    System.Math.Abs(v - 0.5)
                        ? u < 0.5
                        : v < 0.5;

                /*
                 * Either occurrence of draggedWindow represents an empty slot for
                 * this drag. The caller uses index to insert it there.
                 */
                return windows[slotIndex] == draggedWindow
                    ? null
                    : windows[slotIndex];
            }

            return null;
        }

        private bool InsertWindow(
            IWindow window,
            IWindow target,
            bool before)
        {
            lock (_windows)
            {
                if (window == null ||
                    target == null ||
                    window == target)
                {
                    return false;
                }

                var windowIndex =
                    _windows.IndexOf(window);

                var targetIndex =
                    _windows.IndexOf(target);

                if (windowIndex < 0 ||
                    targetIndex < 0)
                {
                    return false;
                }

                /*
                 * Remove the moved window first because this may change
                 * the target's index.
                 */
                _windows.RemoveAt(windowIndex);

                targetIndex = _windows.IndexOf(target);

                if (targetIndex < 0)
                {
                    /*
                     * Defensively restore the window to its previous
                     * position if the target unexpectedly disappeared.
                     */
                    _windows.Insert(
                        System.Math.Min(
                            windowIndex,
                            _windows.Count),
                        window);

                    return false;
                }

                var insertionIndex =
                    targetIndex +
                    (before ? 0 : 1);

                _windows.Insert(
                    insertionIndex,
                    window);

                return true;
            }
        }

        public bool IsPointInside(int x, int y)
        {
            var monitor = _context.WorkspaceContainer
                .GetCurrentMonitorForWorkspace(this);

            if (monitor == null)
            {
                return false;
            }

            return
                monitor.X <= x &&
                x < monitor.X + monitor.Width &&
                monitor.Y <= y &&
                y < monitor.Y + monitor.Height;
        }

        public bool TryMoveFocusedWindow(Direction direction)
        {
            var windows = ManagedWindows.ToList();
            var focused = windows.FirstOrDefault(window => window.IsFocused);

            if (focused == null || windows.Count <= 1)
            {
                return false;
            }

            var monitor = _context.WorkspaceContainer
                .GetCurrentMonitorForWorkspace(this);

            if (monitor == null)
            {
                return false;
            }

            var locations = GetLayoutEngine()
                .CalcLayout(
                    windows,
                    monitor.Width,
                    monitor.Height)
                .ToList();

            var count = System.Math.Min(
                windows.Count,
                locations.Count);

            var focusedIndex = windows.IndexOf(focused);

            if (focusedIndex < 0 || focusedIndex >= count)
            {
                return false;
            }

            var focusedBounds = ToRectangle(locations[focusedIndex]);

            IWindow target;
            bool before;

            switch (direction)
            {
                case Direction.Left:
                case Direction.Right:
                    target = FindHorizontalMoveTarget(
                        windows,
                        locations,
                        count,
                        focusedIndex,
                        focusedBounds,
                        direction,
                        out before);
                    break;

                case Direction.Up:
                case Direction.Down:
                    target = FindVerticalMoveTarget(
                        windows,
                        locations,
                        count,
                        focusedIndex,
                        focusedBounds,
                        direction,
                        out before);
                    break;

                default:
                    return false;
            }

            if (target == null)
            {
                return false;
            }

            if (!InsertWindow(focused, target, before))
            {
                return false;
            }

            DoLayout();

            return true;
        }

        private static IWindow FindHorizontalMoveTarget(
            IList<IWindow> windows,
            IList<IWindowLocation> locations,
            int count,
            int focusedIndex,
            Rectangle focusedBounds,
            Direction direction,
            out bool before)
        {
            before = true;

            var focusedCenterX =
                focusedBounds.Left + focusedBounds.Width / 2.0;

            var candidates = Enumerable.Range(0, count)
                .Where(index => index != focusedIndex)
                .Select(index => new
                {
                    Index = index,
                    Bounds = ToRectangle(locations[index])
                })
                .Where(candidate =>
                {
                    var centerX =
                        candidate.Bounds.Left +
                        candidate.Bounds.Width / 2.0;

                    return direction == Direction.Left
                        ? centerX < focusedCenterX
                        : centerX > focusedCenterX;
                })
                .ToList();

            if (candidates.Count == 0)
            {
                return null;
            }

            /*
             * Select the nearest column in the requested direction.
             * For Tall this selects primary when moving left from secondary,
             * and secondary when moving right from primary.
             */
            var targetColumnX = direction == Direction.Left
                ? candidates.Max(candidate =>
                    candidate.Bounds.Left +
                    candidate.Bounds.Width / 2.0)
                : candidates.Min(candidate =>
                    candidate.Bounds.Left +
                    candidate.Bounds.Width / 2.0);

            const double tolerance = 1.0;

            var column = candidates
                .Where(candidate =>
                    System.Math.Abs(
                        candidate.Bounds.Left +
                        candidate.Bounds.Width / 2.0 -
                        targetColumnX) < tolerance)
                .OrderBy(candidate => candidate.Bounds.Top)
                .ToList();

            if (column.Count == 0)
            {
                return null;
            }

            if (direction == Direction.Left)
            {
                /*
                 * Insert immediately before the final window in the column
                 * to the left.
                 */
                before = true;
                return windows[column[column.Count - 1].Index];
            }

            /*
             * Insert immediately after the first window in the column
             * to the right.
             */
            before = false;
            return windows[column[0].Index];
        }

        private static IWindow FindVerticalMoveTarget(
            IList<IWindow> windows,
            IList<IWindowLocation> locations,
            int count,
            int focusedIndex,
            Rectangle focusedBounds,
            Direction direction,
            out bool before)
        {
            before = direction == Direction.Up;

            var focusedCenterX =
                focusedBounds.Left + focusedBounds.Width / 2.0;
            var focusedCenterY =
                focusedBounds.Top + focusedBounds.Height / 2.0;

            var candidate = Enumerable.Range(0, count)
                .Where(index => index != focusedIndex)
                .Select(index => new
                {
                    Index = index,
                    Bounds = ToRectangle(locations[index])
                })
                /*
                 * Require horizontal overlap. This keeps movement within
                 * the current Tall column.
                 */
                .Where(item =>
                    item.Bounds.Left < focusedBounds.Right &&
                    item.Bounds.Right > focusedBounds.Left)
                .Select(item => new
                {
                    item.Index,
                    item.Bounds,
                    CenterX =
                        item.Bounds.Left +
                        item.Bounds.Width / 2.0,
                    CenterY =
                        item.Bounds.Top +
                        item.Bounds.Height / 2.0
                })
                .Where(item =>
                    direction == Direction.Up
                        ? item.CenterY < focusedCenterY
                        : item.CenterY > focusedCenterY)
                .OrderBy(item =>
                    System.Math.Abs(item.CenterY - focusedCenterY))
                .ThenBy(item =>
                    System.Math.Abs(item.CenterX - focusedCenterX))
                .FirstOrDefault();

            return candidate == null
                ? null
                : windows[candidate.Index];
        }

        private static Rectangle ToRectangle(
            IWindowLocation location)
        {
            return new Rectangle(
                location.X,
                location.Y,
                location.Width,
                location.Height);
        }

        private IEnumerable<IWindowLocation> CalcLayout()
        {
            var windows = ManagedWindows;

            var monitor = _context.WorkspaceContainer
                .GetCurrentMonitorForWorkspace(this);

            if (monitor == null)
            {
                return null;
            }

            return GetLayoutEngine().CalcLayout(
                windows,
                monitor.Width,
                monitor.Height);
        }

        public void DoLayout()
        {
            // Skip layout if the focused window is fullscreen.
            if (FocusedWindow?.IsFullscreen ?? false)
            {
                OnLayoutCompleted?.Invoke(this);
                return;
            }

            var windows = ManagedWindows.ToList();

            if (_context.Enabled)
            {
                var monitor = _context.WorkspaceContainer
                    .GetCurrentMonitorForWorkspace(this);

                if (monitor != null)
                {
                    windows.ForEach(
                        window =>
                            window.ShowInCurrentState());

                    var locations = GetLayoutEngine()
                        .CalcLayout(
                            windows,
                            monitor.Width,
                            monitor.Height)
                        .ToArray();

                    /*
                     * Be defensive if a layout engine returns a different
                     * number of locations than the number of windows.
                     */
                    var count = System.Math.Min(
                        windows.Count,
                        locations.Length);

                    using (var handle =
                        _context.Windows.DeferWindowsPos(count))
                    {
                        for (var index = 0;
                             index < count;
                             index++)
                        {
                            var window = windows[index];
                            var location = locations[index];

                            var adjustedLocation =
                                new WindowLocation(
                                    location.X + monitor.X,
                                    location.Y + monitor.Y,
                                    location.Width,
                                    location.Height,
                                    location.State);

                            /*
                             * While dragging, the layout is previewed by
                             * moving all other windows but leaving the
                             * dragged window under the mouse.
                             */
                            if (!window.IsMouseMoving &&
                                !window.IsFullscreen)
                            {
                                handle.DeferWindowPos(
                                    window,
                                    adjustedLocation);
                            }
                        }
                    }
                }
                else
                {
                    windows.ForEach(
                        window => window.Hide());
                }

                OnLayoutCompleted?.Invoke(this);
            }
            else
            {
                windows.ForEach(
                    window =>
                        window.ShowInCurrentState());
            }
        }

        public event OnLayoutCompletedDelegate OnLayoutCompleted;

        public override string ToString()
        {
            return Name;
        }

        private bool SwapWindows(
            IWindow left,
            IWindow right)
        {
            if (left == null ||
                right == null ||
                left == right)
            {
                return false;
            }

            lock (_windows)
            {
                Logger.Trace(
                    "SwapWindows[{0},{1}]",
                    left,
                    right);

                var leftIndex =
                    _windows.IndexOf(left);

                var rightIndex =
                    _windows.IndexOf(right);

                if (leftIndex < 0 ||
                    rightIndex < 0)
                {
                    return false;
                }

                _windows[leftIndex] = right;
                _windows[rightIndex] = left;

                right.NotifyUpdated();
                left.NotifyUpdated();
            }

            DoLayout();
            return true;
        }

        private ILayoutEngine GetLayoutEngine()
        {
            return _layoutEngines[_layoutIndex];
        }
    }
}