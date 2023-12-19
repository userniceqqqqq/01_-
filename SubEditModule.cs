using Prism.Ioc;
using Prism.Modularity;
using QiShiLog.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyManager.MainModule.SubEdit
{
    [Module(ModuleName = "Edit", OnDemand = true)]
    public class SubEditModule : IModule
    {
        public void OnInitialized(IContainerProvider containerProvider)
        {
            //开启日志功能
            Logger.Instance.EnableInfoFile = true;
        }

        public void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterForNavigation<Views.EditContentView>();
        }
    }
}
