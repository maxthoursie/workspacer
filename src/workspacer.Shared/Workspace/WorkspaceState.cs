using System.Collections.Generic;

namespace workspacer
{
    public class WorkspaceState
    {
        public List<string> WorkspaceLayouts { get; set; }
        public List<List<SavedWindowState>> WorkspacesToWindows { get; set; }
        public List<int> MonitorsToWorkspaces { get; set; }
        public int FocusedMonitor { get; set; }
        public int FocusedWorkspace { get; set; }
        public int FocusedWindow { get; set; }
    }
    public class SavedWindowState
    {
        public long Handle { get; set; }
        public string ProcessName { get; set; }
        public string Class { get; set; }
        public string Title { get; set; }
    }
}
