using System.Text.Json;

namespace InstallerBuilder
{
    public static class HistoryManager
    {
        private const string HistoryFolderName = "history";
        private const string LastUsedFileName = "_lastused.json";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true
        };

        public static string GetHistoryFolder()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            string historyPath = Path.Combine(exePath, HistoryFolderName);

            if (!Directory.Exists(historyPath))
            {
                Directory.CreateDirectory(historyPath);
            }

            return historyPath;
        }

        public static string GetHistoryFileName(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                return string.Empty;

            string projectName = Path.GetFileNameWithoutExtension(projectPath);
            return $"{projectName}.json";
        }

        public static void Save(ProjectHistory history)
        {
            if (string.IsNullOrWhiteSpace(history.ProjectPath))
                return;

            try
            {
                string historyFolder = GetHistoryFolder();
                string fileName = GetHistoryFileName(history.ProjectPath);
                string filePath = Path.Combine(historyFolder, fileName);

                history.LastModified = DateTime.Now;
                string json = JsonSerializer.Serialize(history, JsonOptions);
                File.WriteAllText(filePath, json);

                SaveLastUsed(history.ProjectPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save history: {ex.Message}");
            }
        }

        public static ProjectHistory? Load(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                return null;

            try
            {
                string historyFolder = GetHistoryFolder();
                string fileName = GetHistoryFileName(projectPath);
                string filePath = Path.Combine(historyFolder, fileName);

                if (!File.Exists(filePath))
                    return null;

                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<ProjectHistory>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load history: {ex.Message}");
                return null;
            }
        }

        public static bool HasHistory(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                return false;

            string historyFolder = GetHistoryFolder();
            string fileName = GetHistoryFileName(projectPath);
            string filePath = Path.Combine(historyFolder, fileName);

            return File.Exists(filePath);
        }

        public static ProjectHistory? GetLastUsed()
        {
            try
            {
                string historyFolder = GetHistoryFolder();
                string lastUsedPath = Path.Combine(historyFolder, LastUsedFileName);

                if (!File.Exists(lastUsedPath))
                    return null;

                string json = File.ReadAllText(lastUsedPath);
                var lastUsedInfo = JsonSerializer.Deserialize<LastUsedInfo>(json);

                if (lastUsedInfo == null || string.IsNullOrWhiteSpace(lastUsedInfo.ProjectPath))
                    return null;

                return Load(lastUsedInfo.ProjectPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get last used: {ex.Message}");
                return null;
            }
        }

        private static void SaveLastUsed(string projectPath)
        {
            try
            {
                string historyFolder = GetHistoryFolder();
                string lastUsedPath = Path.Combine(historyFolder, LastUsedFileName);

                var lastUsedInfo = new LastUsedInfo
                {
                    ProjectPath = projectPath,
                    LastUsedTime = DateTime.Now
                };

                string json = JsonSerializer.Serialize(lastUsedInfo, JsonOptions);
                File.WriteAllText(lastUsedPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save last used: {ex.Message}");
            }
        }

        public static List<ProjectHistory> GetAllHistories()
        {
            var histories = new List<ProjectHistory>();

            try
            {
                string historyFolder = GetHistoryFolder();
                var files = Directory.GetFiles(historyFolder, "*.json")
                    .Where(f => !Path.GetFileName(f).StartsWith("_"));

                foreach (var file in files)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        var history = JsonSerializer.Deserialize<ProjectHistory>(json);
                        if (history != null)
                        {
                            histories.Add(history);
                        }
                    }
                    catch
                    {
                        // Skip invalid files
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get all histories: {ex.Message}");
            }

            return histories.OrderByDescending(h => h.LastModified).ToList();
        }

        public static void Delete(string projectPath)
        {
            if (string.IsNullOrWhiteSpace(projectPath))
                return;

            try
            {
                string historyFolder = GetHistoryFolder();
                string fileName = GetHistoryFileName(projectPath);
                string filePath = Path.Combine(historyFolder, fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete history: {ex.Message}");
            }
        }
    }
}
