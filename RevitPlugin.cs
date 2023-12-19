using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using QiShiLog.Log;
using RevitStorage;
using FamilyManager.MainModule.SubEdit;
using FamilyManager.MainModule.SubExport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ookii.Dialogs.WinForms;
using Revit.Async;
using System.Windows;

namespace FamilyManager.MainModule
{
    [Transaction(TransactionMode.Manual)]
    public class RevitPlugin : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, Autodesk.Revit.DB.ElementSet elements)
        {
            //引入下第三方库：防止出现找不到程序集的错误——在外部命令入口处引用即可，子Moudle(子命名空间)中无需再引用（但需要安装第三方库）
            PlatformType platform = PlatformType.x64;//RevitStorge库

            TimerEventArgs loadOtherDll = new TimerEventArgs(1);

            RevitTask.Initialize(commandData.Application);// 作用一：不一定非要采用构造函数引入第三方——视具体情况而定
                                                          // 作用二：初始化RevitTask（需要在Revit的上下文环境中执行）

            var _ = new Microsoft.Xaml.Behaviors.DefaultTriggerAttribute(typeof(Trigger), typeof(Microsoft.Xaml.Behaviors.TriggerBase), null);//解决bug——使用外部应用.addin文件载入插件时，会报错:Could not load file or assembly Microsoft.Xaml.Behaviors（外部命令载入插件不报错） 
            //Theme theme = new Theme();  //UI库

            //开启日志功能
            Logger.Instance.EnableInfoFile = true;

            //注册外部事件至缓存中——获取Revit族库数据至WPF【EditFamily】
            LoadFamilySource loadFamilySource = new LoadFamilySource();//获取外部命令            
            ExternalEvent _externalEventLoadFamilySource = ExternalEvent.Create(loadFamilySource);//注册外部事件;
            SysCacheSubEdit.Instance.LoadFamilySourceEventHandler = loadFamilySource;//存储至缓存类中
            SysCacheSubEdit.Instance.LoadFamilySourceEvent = _externalEventLoadFamilySource;            

            // 注册外部事件至缓存中——获取Revit族库数据至WPF【ExportFamily】
            LoadFamilyTreeSource loadFamilyTreeSource = new LoadFamilyTreeSource();//获取外部命令            
            ExternalEvent _externalEventLoadFamilyTreeSource = ExternalEvent.Create(loadFamilyTreeSource);//注册外部事件;
            SysCacheSubExport.Instance.LoadFamilyTreeSourceEventHandler = loadFamilyTreeSource;//存储至缓存类中
            SysCacheSubExport.Instance.LoadFamilyTreeSourceEvent = _externalEventLoadFamilyTreeSource;

            //获取Revit应用进程——用于手动执行外部事件
            SysCacheSubEdit.Instance.ExternEventExecuteApp = commandData.Application;
            SysCacheSubExport.Instance.ExternEventExecuteApp = commandData.Application;

            //打开非模态窗口
            Bootstrapper bootstrapper = new Bootstrapper();
            bootstrapper.Run();

            return Result.Succeeded;
        }
    }
}
