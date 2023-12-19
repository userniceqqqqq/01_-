using Ookii.Dialogs.WinForms;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using QiShiLog;
using QiShiLog.Log;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;

namespace FamilyManager.MainModule.SubExport.ViewModels
{
    class ExportSetContentViewModel : BindableBase, IDialogAware, INotifyDataErrorInfo
    {
        public ExportSetContentViewModel()
        {
            ErrorsContainer = new ErrorsContainer<string>(OnErrorsContainerCreate);
            for (int i = 1; i < 10; i++)
            {
                backupCounts.Add(i.ToString());
            }
        }

        #region INotifyDataErrorInfo接口实现

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public IEnumerable GetErrors(string propertyName)
        {
            return ErrorsContainer.GetErrors(propertyName);
        }

        public bool HasErrors => ErrorsContainer.HasErrors;


        //自定义成员

        public ErrorsContainer<string> ErrorsContainer;

        private void OnErrorsContainerCreate(string propName)
        {
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propName));
        }

        #endregion

        #region IDialogAware接口实现
        public string Title => "导出设置";

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed() { }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            ExportSet curExportSet = parameters.GetValue<ExportSet>("当前导出设置");
            curExportSet.SetStorageFolders.ToList().ForEach(item =>
            {
                if (item.IsVisibility==Visibility.Collapsed)
                {
                    item.IsVisibility = Visibility.Visible;
                }
                StorageFolders.Add(item);
            });
            IsCatalogAsFolder = curExportSet.SetIsCatagoryAsFolder;
            IsSaveBackup = curExportSet.SetIsSaveBackup;
            MaxBackupCount = curExportSet.SetMaxBackupCount.ToString();
        }
        #endregion


        #region DataGrid交互

        //存储导出文件夹
        public ObservableCollection<StorageFolder> StorageFolders { get; set; } = new ObservableCollection<StorageFolder>();

        private string _SrerchText;
        public string SrerchText
        {
            get { return _SrerchText; }
            set { SetProperty(ref _SrerchText, value, SrerchTextChanged); }
        }

        public void SrerchTextChanged()
        {
            if (SrerchText == "" || SrerchText == null)
            {
                StorageFolders.ToList().ForEach(item =>
                {
                    item.IsVisibility = Visibility.Visible;
                });
                return;
            }
            StorageFolders.ToList().ForEach(item =>
            {
                if (item.FolderPath.Contains(SrerchText.Trim()) || item.FolderName.Contains(SrerchText.Trim()))
                {
                    item.IsVisibility = Visibility.Visible;
                }
                else
                {
                    item.IsVisibility = Visibility.Collapsed;
                }
            });
        }

        private bool? _DataGridAllIsCheck = false;
        public bool? DataGridAllIsCheck //null表示未全选该节点的所有子节点
        {
            get { return _DataGridAllIsCheck; }
            set { SetProperty(ref _DataGridAllIsCheck, value); }
        }

        public ICommand RemoveAllItemCommand
        {
            get => new DelegateCommand(() =>
            {
                StorageFolders.Clear();               
            });
        }

        public ICommand RemoveCurItemCommand
        {
            get => new DelegateCommand<Object>((obj) =>
            {
                StorageFolder curItem = obj as StorageFolder;
                StorageFolders.Remove(curItem);

                //var items = from item in StorageFolders
                //            where item.IsSelect == false || item.IsVisibility == Visibility.Collapsed
                //            select item;
                //StorageFolders.Clear();
                //【注意：当清空数据源时，使用Linq表达式查询的结果也会清空】
                for (int i = StorageFolders.Count-1; i >= 0; i--)
                {
                    if (StorageFolders[i].IsSelect == true && StorageFolders[i].IsVisibility==Visibility.Visible)
                    {
                        StorageFolders.RemoveAt(i);
                    }
                }               
            });
        }

        public ICommand AddStorageFolderCommand
        {
            get => new DelegateCommand(() =>
            {
                //FolderBrowserDialog dialog = new FolderBrowserDialog();系统默认的选择文件夹对话框中无法粘贴文件夹路径               
                VistaFolderBrowserDialog vistaOpenFileDialog = new VistaFolderBrowserDialog();//引用第三方库：Ookii.Dialogs.WinForms——会自动处理不正确输入
                vistaOpenFileDialog.Description = "请选择导出文件夹";
                if (vistaOpenFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string savePath = vistaOpenFileDialog.SelectedPath;
                    StorageFolders.Add(new StorageFolder()
                    {
                        GuidId = Guid.NewGuid(),
                        FolderPath = savePath,
                        FolderName = Path.GetFileNameWithoutExtension(savePath)
                    });
                }                
            });
        }


        public ICommand EditCurFolderPathCommand
        {
            get => new DelegateCommand<Object>((obj) =>
            {
                StorageFolder curItem = obj as StorageFolder;
                int curIndex = StorageFolders.IndexOf(curItem);

                VistaFolderBrowserDialog vistaOpenFileDialog = new VistaFolderBrowserDialog();
                vistaOpenFileDialog.Description = "请选择导出文件夹";
                vistaOpenFileDialog.SelectedPath = curItem.FolderPath;
                if (vistaOpenFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {                    
                    StorageFolders[curIndex].FolderPath= vistaOpenFileDialog.SelectedPath;
                }
            });
        }

        #endregion



        #region 导出设置
        private readonly IList<string> backupCounts = new List<string>();

        //是否按类别分组
        private bool _IsCatalogAsStorageFolder;
        public bool IsCatalogAsFolder
        {
            get { return _IsCatalogAsStorageFolder; }
            set { SetProperty(ref _IsCatalogAsStorageFolder, value); }
        }

        //是否保存备份文件
        private bool _IsSaveBackup;
        public bool IsSaveBackup
        {
            get { return _IsSaveBackup; }
            set { SetProperty(ref _IsSaveBackup, value, UpdateMaxBackupCount); }
        }

        public void UpdateMaxBackupCount()
        {
            if (IsSaveBackup==false)
            {
                MaxBackupCount = "1";
            }
        }

        //最大备份数
        private string _MaxBackupCount;
        public string MaxBackupCount
        {
            get { return _MaxBackupCount; }
            set
            {
                var result = backupCounts.FirstOrDefault(x => x == value.Trim());
                if (result == null)
                {
                    IsAllowSave = false;
                    ErrorsContainer.SetErrors("MaxBackupCount", new string[] { "请输入1~9区间的数字" });
                }
                else
                {
                    IsAllowSave = true;
                    ErrorsContainer.ClearErrors("MaxBackupCount");
                }
                SetProperty(ref _MaxBackupCount, value);
            }
        }

        //是否允许保存导出设置
        private bool _IsAllowSave = true;
        public bool IsAllowSave
        {
            get { return _IsAllowSave; }
            set { SetProperty(ref _IsAllowSave, value); }
        }

        //保存导出设置并关闭弹窗
        public ICommand SaveAndCloseCommand
        {
            get => new DelegateCommand(() =>
            {
                DialogParameters dialogParameters = new DialogParameters();
                ExportSet exportSet = new ExportSet()
                {
                    SetStorageFolders = StorageFolders,                   
                    SetIsCatagoryAsFolder = IsCatalogAsFolder,
                    SetIsSaveBackup= IsSaveBackup,
                    SetMaxBackupCount = Convert.ToInt32(MaxBackupCount)
                };
                dialogParameters.Add("更新导出设置", exportSet);
                IDialogResult dialogResult = new Prism.Services.Dialogs.DialogResult(ButtonResult.OK, dialogParameters);
                RequestClose?.Invoke(dialogResult);
            });
        } 
        #endregion


    }
}
