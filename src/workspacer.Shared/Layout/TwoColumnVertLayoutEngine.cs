using System;
using System.Collections.Generic;
using System.Linq;

namespace workspacer
{
    public class TwoColumnVertLayoutEngine : ILayoutEngine
    {
        private readonly VertLayoutEngine _vertLayoutEngine = new VertLayoutEngine();

        public string Name { get; set; } = "two-column-rows";

        public IEnumerable<IWindowLocation> CalcLayout(
            IEnumerable<IWindow> windows,
            int spaceWidth,
            int spaceHeight)
        {
            var windowList = windows.ToList();
            var numWindows = windowList.Count;

            if (numWindows == 0)
            {
                return Enumerable.Empty<IWindowLocation>();
            }

            // Behave exactly like Vert for up to four windows.
            if (numWindows <= 4)
            {
                return _vertLayoutEngine.CalcLayout(
                    windowList,
                    spaceWidth,
                    spaceHeight);
            }

            var locations = new List<IWindowLocation>(numWindows);

            // Put the extra window in the first column for odd counts.
            var numInLeftColumn = (numWindows + 1) / 2;
            var numInRightColumn = numWindows - numInLeftColumn;

            var leftWidth = spaceWidth / 2;
            var rightWidth = spaceWidth - leftWidth;

            AddColumn(
                locations,
                startIndex: 0,
                numWindows: numInLeftColumn,
                x: 0,
                width: leftWidth,
                spaceHeight: spaceHeight);

            AddColumn(
                locations,
                startIndex: numInLeftColumn,
                numWindows: numInRightColumn,
                x: leftWidth,
                width: rightWidth,
                spaceHeight: spaceHeight);

            return locations;
        }

        private static void AddColumn(
            ICollection<IWindowLocation> locations,
            int startIndex,
            int numWindows,
            int x,
            int width,
            int spaceHeight)
        {
            if (numWindows == 0)
            {
                return;
            }

            var baseHeight = spaceHeight / numWindows;
            var y = 0;

            for (var i = 0; i < numWindows; i++)
            {
                // Let the last row absorb any pixels left over by integer division.
                var height = i == numWindows - 1
                    ? spaceHeight - y
                    : baseHeight;

                locations.Add(
                    new WindowLocation(
                        x,
                        y,
                        width,
                        height,
                        WindowState.Normal));

                y += height;
            }
        }

        public void IncrementNumInPrimary()
        {
            _vertLayoutEngine.IncrementNumInPrimary();
        }

        public void DecrementNumInPrimary()
        {
            _vertLayoutEngine.DecrementNumInPrimary();
        }

        public void ExpandPrimaryArea()
        {
            _vertLayoutEngine.ExpandPrimaryArea();
        }

        public void ShrinkPrimaryArea()
        {
            _vertLayoutEngine.ShrinkPrimaryArea();
        }

        public void ResetPrimaryArea()
        {
            _vertLayoutEngine.ResetPrimaryArea();
        }
    }
}