using System;
using System.Windows.Forms;
using MunicipalServicesApp.Forms;

namespace MunicipalServicesApp
{
    /// <summary>
    /// Application entry point. Launches the Main Menu form, from which
    /// the Report Issues, Local Events, and Service Request Status
    /// features are accessed.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainMenuForm());
        }
    }
}
