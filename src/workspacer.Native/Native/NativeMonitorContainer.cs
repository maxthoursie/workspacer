using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace workspacer
{
    public class NativeMonitorContainer : IMonitorContainer
    {
        private Monitor[] _monitors;

        private Dictionary<IMonitor, int> _monitorMap;

        private static Logger Logger = Logger.Create();

        public NativeMonitorContainer()
        {
            var screens = Screen.AllScreens
                .Where(screen => !screen.Primary)
                .OrderBy(screen => screen.Bounds.X)
                .ThenBy(screen => screen.Bounds.Y)
                .Prepend(Screen.PrimaryScreen)
                .ToArray();

            _monitors = new Monitor[screens.Length];
            _monitorMap = new Dictionary<IMonitor, int>();

            var primaryMonitor = new Monitor(0, Screen.PrimaryScreen);
            _monitors[0] = primaryMonitor;
            _monitorMap[primaryMonitor] = 0;

            for (var index = 0; index < screens.Length; index++)
            {
                var monitor = new Monitor(index, screens[index]);
                _monitors[index] = monitor;
                _monitorMap[monitor] = index;
            }

            FocusedMonitor = _monitors[0];
            LogMonitorConfiguration();
            LogMonitorLayout();
        }

        public int NumMonitors => _monitors.Length;

        public IMonitor FocusedMonitor { get; set; }

        public IMonitor[] GetAllMonitors()
        {
            return _monitors.ToArray();
        }
        public IMonitor GetMonitorAtIndex(int index)
        {
            return _monitors[index % _monitors.Length];
        }

        public IMonitor GetMonitorAtPoint(int x, int y)
        {
            var screen = Screen.FromPoint(new Point(x, y));
            return _monitors.FirstOrDefault(m => m.Screen.DeviceName == screen.DeviceName) ?? _monitors[0];
        }

        public IMonitor GetMonitorAtRect(int x, int y, int width, int height)
        {
            var screen = Screen.FromRectangle(new Rectangle(x, y, width, height));
            return _monitors.FirstOrDefault(m => m.Screen.DeviceName == screen.DeviceName) ?? _monitors[0];
        }

        public IMonitor GetNextMonitor()
        {
            var index = _monitorMap[FocusedMonitor];
            if (index >= _monitors.Length - 1)
                index = 0;
            else
                index = index + 1;

            return _monitors[index];
        }

        public IMonitor GetPreviousMonitor()
        {
            var index = _monitorMap[FocusedMonitor];
            if (index == 0)
                index = _monitors.Length - 1;
            else
                index = index - 1;

            return _monitors[index];
        }

        private static void LogMonitorConfiguration()
        {
            var screens = Screen.AllScreens;

            Logger.Info("Current Windows monitor configuration: count={0}", screens.Length);

            for (var index = 0; index < screens.Length; index++)
            {
                var screen = screens[index];
                var area = screen.WorkingArea;

                Logger.Info("Windows monitor {0}: name={1}, workingArea=({2},{3}) {4}x{5}, primary={6}",
                    index,
                    screen.DeviceName,
                    area.X,
                    area.Y,
                    area.Width,
                    area.Height,
                    screen.Primary);
            }
        }

        private void LogMonitorLayout()
        {
            const int canvasWidth = 80;
            const int canvasHeight = 24;

            if (_monitors.Length == 0)
            {
                Logger.Info("Monitor layout: no monitors");
                return;
            }

            var left = _monitors.Min(m => m.X);
            var top = _monitors.Min(m => m.Y);
            var right = _monitors.Max(m => m.X + m.Width);
            var bottom = _monitors.Max(m => m.Y + m.Height);

            var virtualWidth = Math.Max(1, right - left);
            var virtualHeight = Math.Max(1, bottom - top);
            var scaleX = (canvasWidth - 1.0) / virtualWidth;
            var scaleY = (canvasHeight - 1.0) / virtualHeight;
            var scale = Math.Min(scaleX, scaleY * 2.0);

            var width = Math.Min(canvasWidth, Math.Max(3, (int)Math.Ceiling(virtualWidth * scale) + 1));
            var height = Math.Min(canvasHeight, Math.Max(3, (int)Math.Ceiling(virtualHeight * scale / 2.0) + 1));
            var canvas = Enumerable.Range(0, height).Select(_ => Enumerable.Repeat(' ', width).ToArray()).ToArray();

            foreach (var monitor in _monitors)
            {
                var x1 = Clamp((int)Math.Round((monitor.X - left) * scale), 0, width - 1);
                var y1 = Clamp((int)Math.Round((monitor.Y - top) * scale / 2.0), 0, height - 1);
                var x2 = Clamp((int)Math.Round((monitor.X + monitor.Width - left) * scale), x1 + 2, width - 1);
                var y2 = Clamp((int)Math.Round((monitor.Y + monitor.Height - top) * scale / 2.0), y1 + 2, height - 1);

                DrawBox(canvas, x1, y1, x2, y2);

                var primary = monitor.Screen.Primary ? "*" : "";
                var label = string.Format("{0}{1}", monitor.Index, primary);
                DrawText(canvas, x1 + Math.Max(1, (x2 - x1 - label.Length) / 2), y1 + Math.Max(1, (y2 - y1) / 2), label);
            }

            Logger.Info("Monitor layout, *=primary:");

            foreach (var line in canvas)
                Logger.Info(new string(line).TrimEnd());
        }

        private static void DrawBox(char[][] canvas, int x1, int y1, int x2, int y2)
        {
            for (var x = x1 + 1; x < x2; x++)
            {
                canvas[y1][x] = '-';
                canvas[y2][x] = '-';
            }

            for (var y = y1 + 1; y < y2; y++)
            {
                canvas[y][x1] = '|';
                canvas[y][x2] = '|';
            }

            canvas[y1][x1] = '+';
            canvas[y1][x2] = '+';
            canvas[y2][x1] = '+';
            canvas[y2][x2] = '+';
        }

        private static void DrawText(char[][] canvas, int x, int y, string text)
        {
            if (y < 0 || y >= canvas.Length)
                return;

            for (var index = 0; index < text.Length && x + index < canvas[y].Length; index++)
            {
                if (x + index >= 0)
                    canvas[y][x + index] = text[index];
            }
        }

        private static int Clamp(int value, int min, int max)
        {
            if (max < min)
                return max;

            return Math.Max(min, Math.Min(max, value));
        }

    }
}
