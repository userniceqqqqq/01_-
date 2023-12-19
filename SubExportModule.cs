using Ookii.Dialogs.WinForms;
using Prism.Ioc;
using Prism.Modularity;
using QiShiLog.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyManager.MainModule.SubExport
{
    [Module(ModuleName = "Export", OnDemand = true)]
    public class SubExportModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            //开启日志功能
            Logger.Instance.EnableInfoFile = true;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.ExportContentView>();
            containerRegistry.RegisterDialog<Views.ExportSetContentView>();

            containerRegistry.RegisterDialogWindow<Views.Base.DialogWindowBase>("win1");


        }
    }
}
