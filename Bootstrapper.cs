using Prism.Ioc;
using Prism.Modularity;
using Prism.Unity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FamilyManager.MainModule
{
    public class Bootstrapper : PrismBootstrapper
    {
        protected override DependencyObject CreateShell()
        {
            return Container.Resolve<Views.MainWindow>();
        }

        /// <summary>
        /// 用于确认窗口的启动模式：非模态 
        /// </summary>
        protected override void OnInitialized()
        {
            if (Shell is Window window)
            {
                window.Show();
            }
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.ImportContentView>();
            //containerRegistry.RegisterForNavigation<Views.LoadContentView>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<SubEdit.SubEditModule>("Edit", InitializationMode.OnDemand);
            moduleCatalog.AddModule<SubExport.SubExportModule>("Export", InitializationMode.OnDemand);
        }

        //protected override IModuleCatalog CreateModuleCatalog()
        //{
        //    return new DirectoryModuleCatalog() { ModulePath = @".\Modules" };
        //}
        // revit无法获取扫描目录中的类库文件——待后续解决
    }
}
