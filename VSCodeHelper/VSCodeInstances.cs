// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace Flow.Plugin.CursorWorkspaces.VSCodeHelper
{
    public static class VSCodeInstances
    {
        private static readonly string _userAppDataPath = Environment.GetEnvironmentVariable("AppData");

        public static List<VSCodeInstance> Instances { get; set; } = new();

        private static BitmapImage Bitmap2BitmapImage(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }


        private static Bitmap BitmapOverlayToCenter(Bitmap bitmap1, Bitmap overlayBitmap)
        {
            if (overlayBitmap == null)
                return bitmap1;

            int bitmap1Width = bitmap1.Width;
            int bitmap1Height = bitmap1.Height;

            Bitmap overlayBitmapResized = new Bitmap(overlayBitmap, new System.Drawing.Size(bitmap1Width / 2, bitmap1Height / 2));

            float marginLeft = (float)((bitmap1Width * 0.7) - (overlayBitmapResized.Width * 0.5));
            float marginTop = (float)((bitmap1Height * 0.7) - (overlayBitmapResized.Height * 0.5));

            Bitmap finalBitmap = new Bitmap(bitmap1Width, bitmap1Height);
            using (Graphics g = Graphics.FromImage(finalBitmap))
            {
                g.DrawImage(bitmap1, System.Drawing.Point.Empty);
                g.DrawImage(overlayBitmapResized, marginLeft, marginTop);
            }

            return finalBitmap;
        }

        private static string FindCursorExecutable()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "cursor", "Cursor.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "cursor", "Cursor.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "cursor", "Cursor.exe"),
            };

            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var dir in pathEnv.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var cursorExe = Path.Combine(dir, "cursor.exe");
                if (File.Exists(cursorExe))
                    return cursorExe;

                var cursorCmd = Path.Combine(dir, "cursor.cmd");
                if (!File.Exists(cursorCmd))
                    continue;

                var fromCmd = Path.GetFullPath(Path.Combine(dir, "..", "..", "..", "Cursor.exe"));
                if (File.Exists(fromCmd))
                    return fromCmd;
            }

            return null;
        }

        private static Bitmap TryExtractIconBitmap(string exePath)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
                return null;

            try
            {
                return Icon.ExtractAssociatedIcon(exePath)?.ToBitmap();
            }
            catch (IOException)
            {
                return null;
            }
        }

        public static void LoadVSCodeInstances()
        {
            Instances = [];

            var cursorInstance = new VSCodeInstance
            {
                AppData = Path.Combine(_userAppDataPath, "Cursor"),
                VSCodeVersion = VSCodeVersion.Stable
            };

            var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                ?? throw new InvalidOperationException("Could not resolve plugin directory.");
            var pluginImagesPath = Path.Combine(pluginDir, "Images");
            var cursorBitmapIcon = TryExtractIconBitmap(FindCursorExecutable());
            var cursorFolderIcon = (Bitmap)Image.FromFile(Path.Combine(pluginImagesPath, "folder.png"));
            var cursorMonitorIcon = (Bitmap)Image.FromFile(Path.Combine(pluginImagesPath, "monitor.png"));
            cursorInstance.WorkspaceIconBitMap = Bitmap2BitmapImage(BitmapOverlayToCenter(cursorFolderIcon, cursorBitmapIcon));
            cursorInstance.RemoteIconBitMap = Bitmap2BitmapImage(BitmapOverlayToCenter(cursorMonitorIcon, cursorBitmapIcon));

            Instances.Add(cursorInstance);
        }
    }
}
