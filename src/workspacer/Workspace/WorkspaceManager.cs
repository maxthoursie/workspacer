using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace workspacer
{
    public class WorkspaceManager : IWorkspaceManager
    {
        private static Logger Logger = Logger.Create();

        private readonly IConfigContext _context;
        private IWorkspace _lastWorkspace;
        private readonly Dictionary<IWindow, IWorkspace> _windowsToWorkspaces;

        private IWindow _draggedWindow;
        private IWorkspace _dropWorkspace;
        private IWindow _dropTarget;
        private Rectangle _dropRect;
        private bool _dropCenter;
        private bool _dropBefore;
        private Dictionary<IWorkspace, List<IWindow>> _dropBaseline;

        public IWorkspace FocusedWorkspace =>
            _context.WorkspaceContainer.GetWorkspaceForMonitor(
                _context.MonitorContainer.FocusedMonitor);

        public event WorkspaceUpdatedDelegate WorkspaceUpdated;
        public event WindowAddedDelegate WindowAdded;
        public event WindowUpdatedDelegate WindowUpdated;
        public event WindowRemovedDelegate WindowRemoved;
        public event WindowMovedDelegate WindowMoved;
        public event FocusedMonitorUpdatedDelegate FocusedMonitorUpdated;

        public WorkspaceManager(IConfigContext context)
        {
            _context = context;

            _windowsToWorkspaces =
                new Dictionary<IWindow, IWorkspace>();
        }

        public void SwitchToWindow(IWindow window)
        {
            Logger.Debug("SwitchToWindow({0})", window);

            if (_windowsToWorkspaces.ContainsKey(window))
            {
                var workspace = _windowsToWorkspaces[window];

                SwitchToWorkspace(workspace);
                window.Focus();
            }
        }

        public void SwitchToWorkspace(int index)
        {
            Logger.Debug("SwitchToWorkspace({0})", index);

            var currentWorkspace = FocusedWorkspace;

            var targetWorkspace =
                _context.WorkspaceContainer.GetWorkspaceAtIndex(
                    currentWorkspace,
                    index);

            SwitchToWorkspace(targetWorkspace);
        }

        public void SwitchToWorkspace(IWorkspace targetWorkspace)
        {
            Logger.Debug(
                "SwitchToWorkspace({0})",
                targetWorkspace);

            if (targetWorkspace == null)
            {
                return;
            }

            var focusedMonitor =
                _context.MonitorContainer.FocusedMonitor;

            var destinationMonitor =
                _context.WorkspaceContainer
                    .GetDesiredMonitorForWorkspace(targetWorkspace)
                ?? focusedMonitor;

            var currentWorkspace =
                _context.WorkspaceContainer
                    .GetWorkspaceForMonitor(destinationMonitor);

            var sourceMonitor =
                _context.WorkspaceContainer
                    .GetCurrentMonitorForWorkspace(targetWorkspace);

            if (targetWorkspace == currentWorkspace)
            {
                return;
            }

            _lastWorkspace = currentWorkspace;

            _context.WorkspaceContainer.AssignWorkspaceToMonitor(
                currentWorkspace,
                sourceMonitor);

            _context.WorkspaceContainer.AssignWorkspaceToMonitor(
                targetWorkspace,
                destinationMonitor);

            currentWorkspace.DoLayout();
            targetWorkspace.DoLayout();

            WorkspaceUpdated?.Invoke();

            targetWorkspace.FocusLastFocusedWindow();
        }

        public void SwitchToLastFocusedWorkspace()
        {
            Logger.Debug(
                "SwitchToLastWorkspace({0})",
                _lastWorkspace);

            var targetWorkspace = _lastWorkspace;

            _lastWorkspace = FocusedWorkspace;

            SwitchToWorkspace(targetWorkspace);
        }

        public void SwitchMonitorToWorkspace(
            int monitorIndex,
            int workspaceIndex)
        {
            Logger.Debug(
                "SwitchMonitorToWorkspace(" +
                "monitorIndex: {0}, workspaceIndex: {1})",
                monitorIndex,
                workspaceIndex);

            if (monitorIndex >=
                _context.MonitorContainer.NumMonitors)
            {
                return;
            }

            var destinationMonitor =
                _context.MonitorContainer.GetMonitorAtIndex(
                    monitorIndex);

            var currentWorkspace =
                _context.WorkspaceContainer.GetWorkspaceForMonitor(
                    destinationMonitor);

            var targetWorkspace =
                _context.WorkspaceContainer.GetWorkspaceAtIndex(
                    currentWorkspace,
                    workspaceIndex);

            var sourceMonitor =
                _context.WorkspaceContainer
                    .GetCurrentMonitorForWorkspace(targetWorkspace);

            _lastWorkspace = currentWorkspace;

            _context.WorkspaceContainer.AssignWorkspaceToMonitor(
                currentWorkspace,
                sourceMonitor);

            _context.WorkspaceContainer.AssignWorkspaceToMonitor(
                targetWorkspace,
                destinationMonitor);

            currentWorkspace.DoLayout();
            targetWorkspace.DoLayout();

            WorkspaceUpdated?.Invoke();

            targetWorkspace.FocusLastFocusedWindow();
        }

        public void SwitchToNextWorkspace()
        {
            Logger.Debug("SwitchToNextWorkspace");

            var destinationMonitor =
                _context.MonitorContainer.FocusedMonitor;

            var currentWorkspace =
                _context.WorkspaceContainer.GetWorkspaceForMonitor(
                    destinationMonitor);

            var targetWorkspace =
                _context.WorkspaceContainer.GetNextWorkspace(
                    currentWorkspace);

            var sourceMonitor =
                _context.WorkspaceContainer
                    .GetCurrentMonitorForWorkspace(targetWorkspace);

            _lastWorkspace = currentWorkspace;

            _context.WorkspaceContainer.AssignWorkspaceToMonitor(
                currentWorkspace,
                sourceMonitor);

            _context.WorkspaceContainer.AssignWorkspaceToMonitor(
                targetWorkspace,
                destinationMonitor);

            currentWorkspace.DoLayout();
            targetWorkspace.DoLayout();

            WorkspaceUpdated?.Invoke();

            targetWorkspace.FocusLastFocusedWindow();
        }

        public void SwitchToPreviousWorkspace()
        {
            Logger.Debug("SwitchToPreviousWorkspace");

            var destinationMonitor =
                _context.MonitorContainer.FocusedMonitor;

            var currentWorkspace =
                _context.WorkspaceContainer.GetWorkspaceForMonitor(
                    destinationMonitor);

            var targetWorkspace =
                _context.WorkspaceContainer.GetPreviousWorkspace(
                    currentWorkspace);

            var sourceMonitor =
                _context.WorkspaceContainer
                    .GetCurrentMonitorForWorkspace(targetWorkspace);

            _lastWorkspace = currentWorkspace;

            _context.WorkspaceContainer.AssignWorkspaceToMonitor(
                currentWorkspace,
                sourceMonitor);

            _context.WorkspaceContainer.AssignWorkspaceToMonitor(
                targetWorkspace,
                destinationMonitor);

            currentWorkspace.DoLayout();
            targetWorkspace.DoLayout();

            WorkspaceUpdated?.Invoke();

            targetWorkspace.FocusLastFocusedWindow();
        }

        public void SwitchFocusedMonitor(int index)
        {
            Logger.Debug(
                "SwitchFocusedMonitor({0})",
                index);

            var focusedMonitor =
                _context.MonitorContainer.FocusedMonitor;

            if (index < 0 ||
                index >= _context.MonitorContainer.NumMonitors)
            {
                return;
            }

            var monitor =
                _context.MonitorContainer.GetMonitorAtIndex(index);

            if (focusedMonitor == monitor)
            {
                return;
            }

            _context.MonitorContainer.FocusedMonitor = monitor;

            FocusedWorkspace.FocusLastFocusedWindow();

            FocusedMonitorUpdated?.Invoke();
        }

        public void SwitchFocusToNextMonitor()
        {
            var focusedMonitor =
                _context.MonitorContainer.FocusedMonitor;

            var targetMonitor =
                _context.MonitorContainer.GetNextMonitor();

            if (focusedMonitor == targetMonitor)
            {
                return;
            }

            _context.MonitorContainer.FocusedMonitor =
                targetMonitor;

            FocusedWorkspace.FocusLastFocusedWindow();

            FocusedMonitorUpdated?.Invoke();
        }

        public void SwitchFocusToPreviousMonitor()
        {
            var focusedMonitor =
                _context.MonitorContainer.FocusedMonitor;

            var targetMonitor =
                _context.MonitorContainer.GetPreviousMonitor();

            if (focusedMonitor == targetMonitor)
            {
                return;
            }

            _context.MonitorContainer.FocusedMonitor =
                targetMonitor;

            FocusedWorkspace.FocusLastFocusedWindow();

            FocusedMonitorUpdated?.Invoke();
        }

        public void SwitchFocusedMonitorToMouseLocation()
        {
            Logger.Debug(
                "SwitchFocusedMonitorToMouseLocation");

            var location = Control.MousePosition;

            var monitor =
                _context.MonitorContainer.GetMonitorAtPoint(
                    location.X,
                    location.Y);

            _context.MonitorContainer.FocusedMonitor =
                monitor;

            FocusedMonitorUpdated?.Invoke();
        }

        public void MoveFocusedWindowToWorkspace(int index)
        {
            Logger.Debug(
                "MoveFocusedWindowToWorkspace({0})",
                index);

            var window = FocusedWorkspace.FocusedWindow;

            var targetWorkspace =
                _context.WorkspaceContainer.GetWorkspaceAtIndex(
                    FocusedWorkspace,
                    index);

            if (window == null ||
                targetWorkspace == null)
            {
                return;
            }

            var windows = FocusedWorkspace.ManagedWindows;

            var nextWindow = windows
                .SkipWhile(candidate => candidate != window)
                .Skip(1)
                .FirstOrDefault();

            if (nextWindow == null)
            {
                nextWindow = windows
                    .TakeWhile(candidate => candidate != window)
                    .LastOrDefault();
            }

            var sourceWorkspace = FocusedWorkspace;

            sourceWorkspace.RemoveWindow(window);
            targetWorkspace.AddWindow(window);

            _windowsToWorkspaces[window] =
                targetWorkspace;

            WindowMoved?.Invoke(
                window,
                sourceWorkspace,
                targetWorkspace);

            nextWindow?.Focus();
        }

        public void MoveFocusedWindowAndSwitchToNextWorkspace()
        {
            Logger.Debug(
                "MoveFocusedWindowAndSwitchToNextWorkspace()");

            var targetWorkspaceIndex =
                _context.WorkspaceContainer
                    .GetNextWorkspaceIndex(FocusedWorkspace);

            _context.Workspaces.MoveFocusedWindowToWorkspace(
                targetWorkspaceIndex);

            _context.Workspaces.SwitchToNextWorkspace();
        }

        public void MoveFocusedWindowAndSwitchToPreviousWorkspace()
        {
            Logger.Debug(
                "MoveFocusedWindowAndSwitchToPreviousWorkspace()");

            var targetWorkspaceIndex =
                _context.WorkspaceContainer
                    .GetPreviousWorkspaceIndex(FocusedWorkspace);

            _context.Workspaces.MoveFocusedWindowToWorkspace(
                targetWorkspaceIndex);

            _context.Workspaces.SwitchToPreviousWorkspace();
        }

        public void MoveFocusedWindowToMonitor(int index)
        {
            Logger.Debug(
                "MoveFocusedWindowToMonitor({0})",
                index);

            if (index >=
                _context.MonitorContainer.NumMonitors)
            {
                return;
            }

            var sourceWorkspace = FocusedWorkspace;
            var window = sourceWorkspace.FocusedWindow;

            var targetMonitor =
                _context.MonitorContainer.GetMonitorAtIndex(index);

            var targetWorkspace =
                _context.WorkspaceContainer.GetWorkspaceForMonitor(
                    targetMonitor);

            if (window == null ||
                targetWorkspace == null)
            {
                return;
            }

            var windows = sourceWorkspace.ManagedWindows;

            var nextWindow = windows
                .SkipWhile(candidate => candidate != window)
                .Skip(1)
                .FirstOrDefault();

            if (nextWindow == null)
            {
                nextWindow = windows
                    .TakeWhile(candidate => candidate != window)
                    .LastOrDefault();
            }

            sourceWorkspace.RemoveWindow(window);
            targetWorkspace.AddWindow(window);

            _windowsToWorkspaces[window] =
                targetWorkspace;

            WindowMoved?.Invoke(
                window,
                sourceWorkspace,
                targetWorkspace);

            nextWindow?.Focus();
        }

        public void MoveFocusedWindowToNextMonitor()
        {
            Logger.Debug("MoveFocusedWindowToNextMonitor");

            var sourceWorkspace = FocusedWorkspace;
            var window = sourceWorkspace.FocusedWindow;

            var focusedMonitor =
                _context.MonitorContainer.FocusedMonitor;

            var targetMonitor =
                _context.MonitorContainer.GetNextMonitor();

            var targetWorkspace =
                _context.WorkspaceContainer.GetWorkspaceForMonitor(
                    targetMonitor);

            if (window == null ||
                targetWorkspace == null)
            {
                return;
            }

            sourceWorkspace.RemoveWindow(window);
            targetWorkspace.AddWindow(window);

            _windowsToWorkspaces[window] =
                targetWorkspace;

            WindowMoved?.Invoke(
                window,
                sourceWorkspace,
                targetWorkspace);

            if (focusedMonitor != targetMonitor)
            {
                window.Focus();

                _context.MonitorContainer.FocusedMonitor =
                    targetMonitor;

                FocusedMonitorUpdated?.Invoke();
            }
        }

        public void MoveFocusedWindowToPreviousMonitor()
        {
            Logger.Debug(
                "MoveFocusedWindowToPreviousMonitor");

            var sourceWorkspace = FocusedWorkspace;
            var window = sourceWorkspace.FocusedWindow;

            var focusedMonitor =
                _context.MonitorContainer.FocusedMonitor;

            var targetMonitor =
                _context.MonitorContainer.GetPreviousMonitor();

            var targetWorkspace =
                _context.WorkspaceContainer.GetWorkspaceForMonitor(
                    targetMonitor);

            if (window == null ||
                targetWorkspace == null)
            {
                return;
            }

            sourceWorkspace.RemoveWindow(window);
            targetWorkspace.AddWindow(window);

            _windowsToWorkspaces[window] =
                targetWorkspace;

            WindowMoved?.Invoke(
                window,
                sourceWorkspace,
                targetWorkspace);

            if (focusedMonitor != targetMonitor)
            {
                _context.MonitorContainer.FocusedMonitor =
                    targetMonitor;

                window.Focus();

                FocusedMonitorUpdated?.Invoke();
            }
        }

        public void MoveAllWindows(
            IWorkspace source,
            IWorkspace destination)
        {
            var windows = source.Windows.ToList();

            foreach (var window in windows)
            {
                RemoveWindow(window);
            }

            foreach (var window in windows)
            {
                AddWindowToWorkspace(
                    window,
                    destination);
            }
        }

        public void ForceWorkspaceUpdate()
        {
            WorkspaceUpdated?.Invoke();
        }

        public void AddWindow(
            IWindow window,
            bool firstCreate)
        {
            AddWindow(
                window,
                false,
                firstCreate);
        }

        private void AddWindow(
            IWindow window,
            bool switchToWorkspace,
            bool firstCreate)
        {
            Logger.Debug(
                "AddWindow({0})",
                window);

            if (_windowsToWorkspaces.ContainsKey(window))
            {
                return;
            }

            var workspace = firstCreate
                ? _context.WindowRouter.RouteWindow(window)
                : GetWorkspaceForWindowLocation(window);

            if (workspace == null)
            {
                return;
            }

            AddWindowToWorkspace(
                window,
                workspace);

            if (switchToWorkspace &&
                window.CanLayout)
            {
                SwitchToWorkspace(workspace);
            }
        }

        private void AddWindowToWorkspace(
            IWindow window,
            IWorkspace workspace)
        {
            Logger.Debug(
                "AddWindowToWorkspace({0}, {1})",
                window,
                workspace);

            workspace.AddWindow(window);

            _windowsToWorkspaces[window] =
                workspace;

            if (window.IsFocused)
            {
                var monitor =
                    _context.WorkspaceContainer
                        .GetCurrentMonitorForWorkspace(workspace);

                if (monitor != null)
                {
                    _context.MonitorContainer.FocusedMonitor =
                        monitor;
                }
            }

            WindowAdded?.Invoke(
                window,
                workspace);
        }

        public void RemoveWindow(IWindow window)
        {
            if (!_windowsToWorkspaces.TryGetValue(window, out var workspace))
            {
                return;
            }

            Logger.Debug("RemoveWindow({0})", window);

            if (_draggedWindow == window)
            {
                EndDropPreview();
            }

            workspace.RemoveWindow(window);
            _windowsToWorkspaces.Remove(window);
            WindowRemoved?.Invoke(window, workspace);
        }

        public void UpdateWindow(IWindow window, WindowUpdateType type)
        {
            if (!_windowsToWorkspaces.TryGetValue(window, out var workspace))
            {
                return;
            }

            Logger.Trace("UpdateWindow({0})", window);

            if (_draggedWindow == window && !window.IsMouseMoving)
            {
                EndDropPreview();
            }

            if (!window.IsFocused)
            {
                return;
            }

            var monitor = _context.WorkspaceContainer.GetCurrentMonitorForWorkspace(workspace);
            if (monitor != null)
            {
                _context.MonitorContainer.FocusedMonitor = monitor;
            }
            else if (type == WindowUpdateType.Foreground)
            {
                Logger.Trace("workspace.IsIndicating = true for workspace {0}", workspace);
                workspace.IsIndicating = true;
                WorkspaceUpdated?.Invoke();
            }

            if (type == WindowUpdateType.Move && window.IsMouseMoving)
            {
                TrySwapWindowToMouse(window);
            }

            if (_windowsToWorkspaces.TryGetValue(window, out var currentWorkspace))
            {
                currentWorkspace.UpdateWindow(window, type);
                WindowUpdated?.Invoke(window, currentWorkspace);
            }
        }

        public void HandleWindowUpdated(IWindow window)
        {
            if (_windowsToWorkspaces.TryGetValue(window, out var workspace))
            {
                Logger.Trace("UpdateWindow({0})", window);
                WindowUpdated?.Invoke(window, workspace);
            }
        }

        private void TrySwapWindowToMouse(IWindow window)
        {
            var point = Cursor.Position;
            var x = point.X;
            var y = point.Y;

            if (!_windowsToWorkspaces.ContainsKey(window))
            {
                EndDropPreview();
                return;
            }

            BeginDropPreview(window);

            var destination = GetWorkspaceAtPoint(x, y);
            if (destination == null)
            {
                ResetDropPreview();
                return;
            }

            IWindow target;
            Rectangle bounds;
            bool center;
            bool before;
            var restored = false;

            if (_dropWorkspace == destination && _dropTarget != null && _dropRect.Contains(x, y))
            {
                target = _dropTarget;
                bounds = _dropRect;
                GetDropPosition(bounds, x, y, out center, out before);
            }
            else
            {
                RestoreDropBaseline();
                restored = true;

                target = destination.GetDropTarget(x, y, out center, out before, out bounds);
                if (target == null || target == window)
                {
                    LayoutDropBaseline();
                    ClearDropTarget();
                    return;
                }
            }

            if (!restored && _dropWorkspace == destination && _dropTarget == target &&
                _dropCenter == center && _dropBefore == before)
            {
                return;
            }

            if (!restored)
            {
                RestoreDropBaseline();
            }

            if (!_windowsToWorkspaces.TryGetValue(window, out var source))
            {
                ResetDropPreview();
                return;
            }

            _dropWorkspace = destination;
            _dropTarget = target;
            _dropRect = bounds;
            _dropCenter = center;
            _dropBefore = before;

            if (source == destination)
            {
                destination.ApplyDrop(window, target, center, before);
            }
            else if (center)
            {
                SwapWindowsBetweenWorkspaces(source, destination, window, target);
            }
            else
            {
                MoveWindowBetweenWorkspaces(source, destination, window, target, before);
            }

            LayoutDropBaseline();
        }

        private IWorkspace GetWorkspaceAtPoint(int x, int y)
        {
            return _context.WorkspaceContainer.GetAllWorkspaces().FirstOrDefault(workspace =>
                _context.WorkspaceContainer.GetCurrentMonitorForWorkspace(workspace) != null &&
                workspace.IsPointInside(x, y));
        }

        private static void GetDropPosition(Rectangle bounds, int x, int y, out bool center, out bool before)
        {
            var u = (double)(x - bounds.Left) / bounds.Width;
            var v = (double)(y - bounds.Top) / bounds.Height;

            center = u >= 0.25 && u <= 0.75 && v >= 0.25 && v <= 0.75;
            before = System.Math.Abs(u - 0.5) > System.Math.Abs(v - 0.5) ? u < 0.5 : v < 0.5;
        }

        private void BeginDropPreview(IWindow window)
        {
            if (_draggedWindow == window && _dropBaseline != null)
            {
                return;
            }

            _draggedWindow = window;
            _dropBaseline = _context.WorkspaceContainer.GetAllWorkspaces()
                .ToDictionary(workspace => workspace, workspace => workspace.Windows.ToList());
            ClearDropTarget();
        }

        private void RestoreDropBaseline()
        {
            if (_dropBaseline == null)
            {
                return;
            }

            foreach (var entry in _dropBaseline)
            {
                entry.Key.SetWindows(entry.Value);
                foreach (var window in entry.Value)
                {
                    _windowsToWorkspaces[window] = entry.Key;
                }
            }
        }

        private void LayoutDropBaseline()
        {
            if (_dropBaseline == null)
            {
                return;
            }

            foreach (var workspace in _dropBaseline.Keys)
            {
                workspace.DoLayout();
            }
        }

        private void ResetDropPreview()
        {
            RestoreDropBaseline();
            LayoutDropBaseline();
            ClearDropTarget();
        }

        private void EndDropPreview()
        {
            _draggedWindow = null;
            _dropBaseline = null;
            ClearDropTarget();
        }

        private void ClearDropTarget()
        {
            _dropWorkspace = null;
            _dropTarget = null;
            _dropRect = Rectangle.Empty;
            _dropCenter = false;
            _dropBefore = false;
        }

        private bool SwapWindowsBetweenWorkspaces(IWorkspace source, IWorkspace destination,
            IWindow window, IWindow target)
        {
            var sourceWindows = source.Windows.ToList();
            var destinationWindows = destination.Windows.ToList();
            var sourceIndex = sourceWindows.IndexOf(window);
            var destinationIndex = destinationWindows.IndexOf(target);

            if (sourceIndex < 0 || destinationIndex < 0)
            {
                Logger.Warn("Could not swap window {0} with {1}", window, target);
                return false;
            }

            sourceWindows[sourceIndex] = target;
            destinationWindows[destinationIndex] = window;
            source.SetWindows(sourceWindows);
            destination.SetWindows(destinationWindows);

            _windowsToWorkspaces[target] = source;
            _windowsToWorkspaces[window] = destination;

            WindowMoved?.Invoke(target, destination, source);
            WindowMoved?.Invoke(window, source, destination);
            return true;
        }

        private bool MoveWindowBetweenWorkspaces(IWorkspace source, IWorkspace destination,
            IWindow window, IWindow target, bool before)
        {
            var sourceWindows = source.Windows.ToList();
            var destinationWindows = destination.Windows.ToList();

            if (!sourceWindows.Remove(window))
            {
                Logger.Warn("Could not remove window {0} from source workspace", window);
                return false;
            }

            destinationWindows.Remove(window);
            var targetIndex = destinationWindows.IndexOf(target);
            if (targetIndex < 0)
            {
                Logger.Warn("Could not insert window {0} relative to {1}", window, target);
                return false;
            }

            destinationWindows.Insert(targetIndex + (before ? 0 : 1), window);
            source.SetWindows(sourceWindows);
            destination.SetWindows(destinationWindows);
            _windowsToWorkspaces[window] = destination;

            WindowMoved?.Invoke(window, source, destination);
            return true;
        }

        private IWorkspace GetWorkspaceForWindowLocation(
            IWindow window)
        {
            var location = window.Location;

            var monitor =
                _context.MonitorContainer.GetMonitorAtRect(
                    location.X,
                    location.Y,
                    location.Width,
                    location.Height);

            return _context.WorkspaceContainer
                .GetWorkspaceForMonitor(monitor);
        }

        public WorkspaceState GetState()
        {
            var workspacesToWindows =
                new List<List<int>>();

            var monitorsToWorkspaces =
                new List<int>();

            var allWorkspaces =
                _context.WorkspaceContainer
                    .GetAllWorkspaces()
                    .ToList();

            var focusedWindow = 0;

            foreach (var workspace in allWorkspaces)
            {
                var windows = new List<int>();

                foreach (var window in workspace.Windows)
                {
                    windows.Add((int)window.Handle);

                    if (window.IsFocused)
                    {
                        focusedWindow =
                            (int)window.Handle;
                    }
                }

                workspacesToWindows.Add(windows);
            }

            var monitors =
                _context.MonitorContainer.GetAllMonitors();

            for (var monitorIndex = 0;
                 monitorIndex < monitors.Length;
                 monitorIndex++)
            {
                for (var workspaceIndex = 0;
                     workspaceIndex < allWorkspaces.Count;
                     workspaceIndex++)
                {
                    var monitor =
                        monitors[monitorIndex];

                    var workspace =
                        allWorkspaces[workspaceIndex];

                    if (_context.WorkspaceContainer
                            .GetCurrentMonitorForWorkspace(
                                workspace)
                        == monitor)
                    {
                        monitorsToWorkspaces.Insert(
                            monitorIndex,
                            workspaceIndex);
                    }
                }
            }

            var focusedMonitor =
                _context.MonitorContainer
                    .FocusedMonitor
                    .Index;

            return new WorkspaceState
            {
                WorkspacesToWindows =
                    workspacesToWindows,

                MonitorsToWorkspaces =
                    monitorsToWorkspaces,

                FocusedMonitor =
                    focusedMonitor,

                FocusedWindow =
                    focusedWindow
            };
        }

        public void InitializeWithState(
            WorkspaceState state,
            IEnumerable<IWindow> allWindows)
        {
            var workspacesToWindows =
                state.WorkspacesToWindows;

            var allWorkspaces =
                _context.WorkspaceContainer
                    .GetAllWorkspaces()
                    .ToList();

            var focusedMonitor =
                _context.MonitorContainer.GetMonitorAtIndex(
                    state.FocusedMonitor);

            _context.MonitorContainer.FocusedMonitor =
                focusedMonitor;

            var monitorsToWorkspaces =
                state.MonitorsToWorkspaces;

            if (monitorsToWorkspaces.Count ==
                _context.MonitorContainer.NumMonitors)
            {
                for (var index = 0;
                     index <
                     _context.MonitorContainer.NumMonitors;
                     index++)
                {
                    var workspaceIndex =
                        monitorsToWorkspaces[index];

                    var workspace =
                        allWorkspaces[workspaceIndex];

                    var monitor =
                        _context.MonitorContainer
                            .GetMonitorAtIndex(index);

                    _context.WorkspaceContainer
                        .AssignWorkspaceToMonitor(
                            workspace,
                            monitor);
                }
            }
            else
            {
                for (var index = 0;
                     index <
                     _context.MonitorContainer.NumMonitors;
                     index++)
                {
                    var monitor =
                        _context.MonitorContainer
                            .GetMonitorAtIndex(index);

                    var workspace =
                        allWorkspaces[index];

                    _context.WorkspaceContainer
                        .AssignWorkspaceToMonitor(
                            workspace,
                            monitor);
                }
            }

            for (var workspaceIndex = 0;
                 workspaceIndex < workspacesToWindows.Count;
                 workspaceIndex++)
            {
                var workspaceWindows =
                    workspacesToWindows[workspaceIndex];

                for (var windowIndex = 0;
                     windowIndex < workspaceWindows.Count;
                     windowIndex++)
                {
                    var handle =
                        workspaceWindows[windowIndex];

                    var window = allWindows.FirstOrDefault(
                        candidate =>
                            (int)candidate.Handle == handle);

                    if (window == null)
                    {
                        continue;
                    }

                    var routedWorkspace =
                        _context.WindowRouter.RouteWindow(
                            window);

                    if (routedWorkspace == null)
                    {
                        continue;
                    }

                    if (workspaceIndex <
                        allWorkspaces.Count)
                    {
                        /*
                         * Ignore the routed workspace because the saved
                         * state indicates that the user deliberately put
                         * the window in this workspace.
                         */
                        var workspace =
                            allWorkspaces[workspaceIndex];

                        AddWindowToWorkspace(
                            window,
                            workspace);
                    }
                    else
                    {
                        AddWindowToWorkspace(
                            window,
                            routedWorkspace);
                    }

                    if (state.FocusedWindow == handle)
                    {
                        window.Focus();
                    }
                }
            }
        }

        public void Initialize(
            IEnumerable<IWindow> windows)
        {
            var allWorkspaces =
                _context.WorkspaceContainer
                    .GetAllWorkspaces()
                    .ToList();

            for (var index = 0;
                 index <
                 _context.MonitorContainer.NumMonitors;
                 index++)
            {
                var monitor =
                    _context.MonitorContainer
                        .GetMonitorAtIndex(index);

                var workspace =
                    allWorkspaces[index];

                _context.WorkspaceContainer
                    .AssignWorkspaceToMonitor(
                        workspace,
                        monitor);
            }

            foreach (var window in windows)
            {
                var locationWorkspace =
                    GetWorkspaceForWindowLocation(window);

                var destinationWorkspace =
                    _context.WindowRouter.RouteWindow(
                        window,
                        locationWorkspace);

                if (destinationWorkspace == null)
                {
                    continue;
                }

                AddWindowToWorkspace(
                    window,
                    destinationWorkspace);

                AddWindow(
                    window,
                    false);
            }
        }
    }
}