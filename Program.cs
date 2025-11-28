using System.Text;

namespace InstallerBuilder;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // CP949 (한글 인코딩) 지원을 위한 인코딩 프로바이더 등록
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}