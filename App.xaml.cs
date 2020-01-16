using MahApps.Metro;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace EtherCAT_Master
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {

            //ThemeManager.ChangeAppStyle(this,
            //                        ThemeManager.GetAccent("Red"),
            //                        ThemeManager.GetAppTheme("BaseDark"));

            //MahApps.Metro.Controls.SliderHelper.

            //// add custom accent and theme resource dictionaries to the ThemeManager
            //// you should replace MahAppsMetroThemesSample with your application name
            //// and correct place where your custom accent lives
            //ThemeManager.AddAccent("CustomAccent1", new Uri("pack://application:,,,/MahAppsMetroThemesSample;component/CustomAccents/CustomAccent1.xaml"));

            //// get the current app style (theme and accent) from the application
            //Tuple<AppTheme, Accent> theme = ThemeManager.DetectAppStyle(Application.Current);

            //// now change app style to the custom accent and current theme
            //ThemeManager.ChangeAppStyle(Application.Current,
            //                            ThemeManager.GetAccent("CustomAccent1"),
            //                            theme.Item1);

            //base.OnStartup(e);
        }
    }
}
