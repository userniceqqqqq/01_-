using Prism.Commands;
using Prism.Modularity;
using Prism.Mvvm;
using Prism.Regions;
using QiShiLog;
using QiShiLog.Log;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Unity;

namespace FamilyManager.MainModule.ViewModels
{
    class MainWindowViewModel : BindableBase
    {

        public MainWindowViewModel()
        {
            brushConverter = new BrushConverter();
            IsClickImport = false;
            IsClickEdit = false;
            IsClickExport = false;
            IsSelectedImportColor = (Brush)brushConverter.ConvertFrom("#FF61666D");
            IsSelectedEditColor = (Brush)brushConverter.ConvertFrom("#FF61666D");
            IsSelectedExportColor = (Brush)brushConverter.ConvertFrom("#FF61666D");
        }

        [Dependency]
        public IRegionManager regionManager { get; set; }

        [Dependency]
        public IModuleManager moduleManager { get; set; }

        public ICommand LoadCommand
        {
            get => new DelegateCommand<string >((viewName) =>
            {
                try
                {
                    switch (viewName)
                    {
                        case "ImportContentView":
                            IsClickImport =true; 
                            IsClickEdit = false;
                            IsClickExport = false;
                            IsSelectedImportColor = (Brush)brushConverter.ConvertFrom("#FFFD6011");
                            IsSelectedEditColor = (Brush)brushConverter.ConvertFrom("#FF61666D");
                            IsSelectedExportColor = (Brush)brushConverter.ConvertFrom("#FF61666D");
                            regionManager.RequestNavigate("MainContent", viewName);
                            break;
                        case "EditContentView":
                            IsClickImport = false;
                            IsClickEdit = true;
                            IsClickExport = false;
                            IsSelectedImportColor = (Brush)brushConverter.ConvertFrom("#FF61666D");
                            IsSelectedEditColor = (Brush)brushConverter.ConvertFrom("#FFFD6011");
                            IsSelectedExportColor = (Brush)brushConverter.ConvertFrom("#FF61666D");
                            moduleManager.LoadModule("Edit");
                            regionManager.RequestNavigate("MainContent", viewName);
                            break;
                        case "ExportContentView":
                            IsClickImport =false;
                            IsClickEdit = false;
                            IsClickExport = true;
                            IsSelectedImportColor = (Brush)brushConverter.ConvertFrom("#FF61666D");
                            IsSelectedEditColor = (Brush)brushConverter.ConvertFrom("#FF61666D");
                            IsSelectedExportColor = (Brush)brushConverter.ConvertFrom("#FFFD6011");
                            moduleManager.LoadModule("Export");
                            regionManager.RequestNavigate("MainContent", viewName);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Info($"报错信息,{ex}");
                    Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
                }               
            });
        }


        private bool _IsClickImport;
        public bool IsClickImport
        {
            get { return _IsClickImport; }
            set { SetProperty(ref _IsClickImport, value); }
        }

        private bool _IsClickEdit;
        public bool IsClickEdit
        {
            get { return _IsClickEdit; }
            set { SetProperty(ref _IsClickEdit, value); }
        }

        private bool _IsClickExport;
        public bool IsClickExport
        {
            get { return _IsClickExport; }
            set { SetProperty(ref _IsClickExport, value); }
        }

        private static BrushConverter brushConverter ;

        private Brush _IsSelectedImportColor;
        public Brush IsSelectedImportColor
        {
            get { return _IsSelectedImportColor; }
            set { SetProperty(ref _IsSelectedImportColor, value); }
        }


        private Brush _IsSelectedEditColor ;
        public Brush IsSelectedEditColor
        {
            get { return _IsSelectedEditColor; }
            set { SetProperty(ref _IsSelectedEditColor, value); }
        }


        private Brush _IsSelectedExportColor;
        public Brush IsSelectedExportColor
        {
            get { return _IsSelectedExportColor; }
            set { SetProperty(ref _IsSelectedExportColor, value); }
        }
    }
}
