// using DustyEngine;
// using DustyEngine.Core;
//
// namespace Editor.Utils
// {
//     public sealed class ComponentFileWatcher : IDisposable
//     {
//         private readonly string _folder;
//         private readonly FileSystemWatcher _watcher;
//         private readonly Timer _debounceTimer;
//
//         public ComponentFileWatcher(string scriptsFolder)
//         {
//             _folder = PathUtility.GetAbsolutePath(scriptsFolder);
//
//             if (!Directory.Exists(_folder))
//                 Directory.CreateDirectory(_folder);
//
//             _watcher = new FileSystemWatcher(_folder)
//             {
//                 IncludeSubdirectories = true,
//                 Filter = "*.*",
//                 NotifyFilter =
//                     NotifyFilters.FileName |
//                     NotifyFilters.DirectoryName |
//                     NotifyFilters.LastWrite |
//                     NotifyFilters.CreationTime
//             };
//
//             _watcher.Created += OnFilesChanged;
//             _watcher.Changed += OnFilesChanged;
//             _watcher.Deleted += OnFilesChanged;
//             _watcher.Renamed += OnFilesRenamed;
//
//             _debounceTimer = new Timer(300);
//             _debounceTimer.AutoReset = false;
//             _debounceTimer.Elapsed += (_, _) => RefreshComponents();
//
//             _watcher.EnableRaisingEvents = true;
//
//             Debug.Log($"Watching component folder: {_folder}", Debug.LogLevel.Info, true);
//         }
//
//         private void OnFilesChanged(object sender, FileSystemEventArgs e)
//         {
//             if (!IsSupportedFile(e.FullPath))
//                 return;
//
//             RestartDebounce();
//         }
//
//         private void OnFilesRenamed(object sender, RenamedEventArgs e)
//         {
//             if (!IsSupportedFile(e.FullPath) && !IsSupportedFile(e.OldFullPath))
//                 return;
//
//             RestartDebounce();
//         }
//
//         private void RestartDebounce()
//         {
//             _debounceTimer.Stop();
//             _debounceTimer.Start();
//         }
//
//         private void RefreshComponents()
//         {
//             try
//             {
//                 ComponentConverter.RefreshExternalComponents(_folder);
//                 Debug.Log("External components refreshed.", Debug.LogLevel.Info, true);
//             }
//             catch (Exception ex)
//             {
//                 Debug.Log($"Failed to refresh external components: {ex.Message}", Debug.LogLevel.Error, true);
//             }
//         }
//
//         private static bool IsSupportedFile(string path)
//         {
//             var ext = Path.GetExtension(path);
//             return ext.Equals(".cs", StringComparison.OrdinalIgnoreCase) ||
//                    ext.Equals(".dll", StringComparison.OrdinalIgnoreCase);
//         }
//
//         public void Dispose()
//         {
//             _watcher.EnableRaisingEvents = false;
//
//             _watcher.Created -= OnFilesChanged;
//             _watcher.Changed -= OnFilesChanged;
//             _watcher.Deleted -= OnFilesChanged;
//             _watcher.Renamed -= OnFilesRenamed;
//
//             _watcher.Dispose();
//             _debounceTimer.Dispose();
//         }
//     }
// }