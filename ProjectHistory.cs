using System.Text.Json.Serialization;

namespace InstallerBuilder
{
    public class ProjectHistory
    {
        public string ProjectPath { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string DefaultInstallPath { get; set; } = string.Empty;
        public string DllDestPath { get; set; } = string.Empty;
        public string AdditionalFilesDestPath { get; set; } = string.Empty;
        public List<string> DllFiles { get; set; } = new List<string>();
        public List<string> AdditionalFiles { get; set; } = new List<string>();
        public bool OverwriteFiles { get; set; } = true;
        public bool DeleteFilesOnUninstall { get; set; } = true;
        public bool EnglishInstaller { get; set; } = false;
        public DateTime LastModified { get; set; } = DateTime.Now;
    }

    public class LastUsedInfo
    {
        public string ProjectPath { get; set; } = string.Empty;
        public DateTime LastUsedTime { get; set; } = DateTime.Now;
    }
}
