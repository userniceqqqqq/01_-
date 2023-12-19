using Autodesk.Revit.DB;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using QiShiLog;
using QiShiLog.Log;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Unity;
using Visibility = System.Windows.Visibility;
using MessageBox = System.Windows.MessageBox;
using System.Threading;
using Revit.Async;
using System.Text.RegularExpressions;
using System.Windows;

namespace FamilyManager.MainModule.SubExport.ViewModels
{
    class ExportContentViewModel : BindableBase
    {

        public ExportContentViewModel()
        {
            string curPath = Assembly.GetExecutingAssembly().Location;
            string curFolder = Path.GetDirectoryName(curPath);
            string saveFolder = curFolder + "\\Customer";

            CustomerStorageFolder = new StorageFolder()
            {
                GuidId = Guid.NewGuid(),
                FolderPath = saveFolder,
                FolderName = Path.GetFileNameWithoutExtension(saveFolder)
            };

            FamilyExportSet = new ExportSet()
            {
                SetMaxBackupCount = 1,
                SetIsSaveBackup = true,
                SetIsCatagoryAsFolder = true
            };

            StorageFolders.Add(CustomerStorageFolder);
            SysCacheSubExport.Instance.FamilyTreeSourceList.Clear();
            SysCacheSubExport.Instance.LoadFamilyTreeSourceEventHandler.Execute(SysCacheSubExport.Instance.ExternEventExecuteApp);
            InitTree();
        }

        #region 获取族交互
        public List<FamilySourceWay> FamilySourceWays { get; set; } = new List<FamilySourceWay>()
        {
            new FamilySourceWay(){Display="项目文档",Select=FamilySource.Document},
            new FamilySourceWay(){Display="激活视图",Select=FamilySource.ActiveView},
            new FamilySourceWay(){ Display = "选择集", Select =FamilySource.Selection}
        };

        private FamilySource _CurFamilySource = FamilySource.Document;
        public FamilySource CurFamilySource
        {
            get { return _CurFamilySource; }
            set { SetProperty(ref _CurFamilySource, value, FamilyWayChanged); }
        }

        public void FamilyWayChanged()
        {
            SysCacheSubExport.Instance.FamilyTreeSourceList.Clear();
            SysCacheSubExport.Instance.LoadFamilyTreeSourceEventHandler.LoadSource = CurFamilySource;
            SysCacheSubExport.Instance.LoadFamilyTreeSourceEventHandler.Execute(SysCacheSubExport.Instance.ExternEventExecuteApp);
            InitTree();
        }
        #endregion


        #region TreeView交互        

        // TreeView的根节点        
        public ObservableCollection<CatagoryTreeNode> RootTreeModels { get; set; } = new ObservableCollection<CatagoryTreeNode>();

        private bool? _TreeAllIsCheck = false;
        public bool? TreeAllIsCheck //null表示未全选该节点的所有子节点
        {
            get { return _TreeAllIsCheck; }
            set { SetProperty(ref _TreeAllIsCheck, value); }
        }


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
                RootTreeModels.ToList().ForEach(x =>
                {
                    RecursiveTraverseTree(x, (item) =>
                     {
                         item.NodeVisibility = Visibility.Visible;
                         if (item.IsCheck != false)
                         {
                             item.IsExpand = true;
                         }
                         else
                         {
                             item.IsExpand = false;
                         }
                     });
                });
                return;
            }
            RootTreeModels.ToList().ForEach(x =>
            {
                RecursiveTraverseTree(x, (item) =>
                {
                    if (item.Name.Contains(SrerchText.Trim()))
                    {
                        item.NodeVisibility = Visibility.Visible;
                        item.IsExpand = true;
                        ShowSearchUp(item);
                        ShowSearchDown(item);
                    }
                    else
                    {
                        if (item.NodeType == TreeNodeType.Catagory)
                        {
                            item.NodeVisibility = Visibility.Collapsed;
                        }
                        item.IsExpand = false;
                    }
                });
            });
        }


        public ICommand NodeCheckChangedCommand
        {
            get => new DelegateCommand<IBaseModel>((treeNode) =>
            {
                try
                {
                    bool newIsCheckValue;
                    if (treeNode.IsCheck == true)
                    {
                        newIsCheckValue = false;
                    }
                    else
                    {
                        newIsCheckValue = true;
                    }
                    //递归检查
                    SelectDown(treeNode, newIsCheckValue);
                    SelectUp(treeNode);                   
                }
                catch (Exception ex)
                {
                    Logger.Instance.Info($"报错信息,{ex}");
                    Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
                }
            });
        }

        public ICommand AllNodeCheckChangedCommand
        {
            get => new DelegateCommand(() =>
            {
                foreach (var peerNode in RootTreeModels)
                {
                    if (peerNode.IsCheck != TreeAllIsCheck && peerNode.NodeVisibility == Visibility.Visible)
                    {
                        SelectDown(peerNode, (bool)TreeAllIsCheck);
                    }
                }
            });
        }
        #endregion


        #region DataGrid交互
        public ObservableCollection<DatagridModel> DatagridModels { get; set; } = new ObservableCollection<DatagridModel>();

        public ObservableCollection<StorageFolder> StorageFolders { get; set; } = new ObservableCollection<StorageFolder>();

        public StorageFolder CustomerStorageFolder { get; set; } 

        private bool? _DataGridAllIsCheck = false;
        public bool? DataGridAllIsCheck //null表示未全选该节点的所有子节点
        {
            get { return _DataGridAllIsCheck; }
            set { SetProperty(ref _DataGridAllIsCheck, value); }
        }

        public ICommand AllItemCheckChangedCommand
        {
            get => new DelegateCommand(() =>
            {
                foreach (var item in DatagridModels)
                {
                    item.IsCheck = DataGridAllIsCheck;
                }
            });
        }

        public ICommand ItemCheckChangedCommand
        {
            get => new DelegateCommand<Object>((obj) =>
            {
                DatagridModel curItem = obj as DatagridModel;
                DataGridAllIsCheck = curItem.IsCheck;
                foreach (var item in DatagridModels)
                {
                    if (item.IsSelect == true)//操作同步至选择项
                    {
                        item.IsCheck = curItem.IsCheck;
                    }
                    if (item.IsCheck != curItem.IsCheck && DataGridAllIsCheck != null)
                    {
                        DataGridAllIsCheck = null;
                    }
                }
            });
        }

        public ICommand SelectionChangedCommand
        {
            get => new DelegateCommand<DatagridModel>((curItem) =>
            {
                if (curItem.Folder==null)
                {
                    curItem.Folder= CustomerStorageFolder;
                    return;
                }
                for (int i = 0; i < DatagridModels.Count(); i++)
                {
                    if (DatagridModels[i].IsSelect == true)
                    {
                        DatagridModels[i].Folder = curItem.Folder;
                    }
                }             
            });
        }        

        #endregion



        [Dependency]
        public IDialogService dialogService { get; set; }

        public ExportSet FamilyExportSet { get; set; }

        /// 打开子窗口
        public ICommand ExportSetCommand
        {
            get => new DelegateCommand(() =>
            {
                DialogParameters dialogParameters = new DialogParameters();                
                IList<StorageFolder> storageFolders = new List<StorageFolder>();
                StorageFolders.ToList().ForEach(item =>
                {
                    if (item!= CustomerStorageFolder)
                    {
                        storageFolders.Add(item);
                    }                    
                });
                ExportSet exportSet = new ExportSet()
                {
                    SetStorageFolders = storageFolders,                   
                    SetIsCatagoryAsFolder = FamilyExportSet.SetIsCatagoryAsFolder,
                    SetIsSaveBackup = FamilyExportSet.SetIsSaveBackup,
                    SetMaxBackupCount = FamilyExportSet.SetMaxBackupCount
                };
                dialogParameters.Add("当前导出设置", exportSet);
                dialogService.ShowDialog("ExportSetContentView", dialogParameters, DoDialogResult, "win1");               
            });
        }

        /// 弹窗回调
        public void DoDialogResult(IDialogResult dialogResult)
        {
            if (dialogResult.Parameters.Count > 0)//保存并关闭
            {
                this.FamilyExportSet = dialogResult.Parameters.GetValue<ExportSet>("更新导出设置");

                // 添加新增的导出文件夹
                FamilyExportSet.SetStorageFolders.ToList().ForEach(item =>
                {
                    var result = StorageFolders.FirstOrDefault(x => x.GuidId == item.GuidId);
                    if (result == null)
                    {
                        StorageFolders.Add(item);
                    }
                });

                // 删除不存在的导出文件夹路径
                for (int i = StorageFolders.Count - 1; i > 0; i--)
                {
                    var result = FamilyExportSet.SetStorageFolders.FirstOrDefault(x => x.GuidId == StorageFolders[i].GuidId);
                    if (result == null)
                    {
                        StorageFolders.RemoveAt(i);
                    }
                }
            }
        }


        /// 进度条属性       
        private double _CurProgressBarValue = 0;
        public double CurProgressBarValue
        {
            get { return _CurProgressBarValue; }
            set { SetProperty(ref _CurProgressBarValue, value); }
        }

        private Visibility _ProBarVisible = Visibility.Collapsed;
        public Visibility ProBarVisible
        {
            get { return _ProBarVisible; }
            set { SetProperty(ref _ProBarVisible, value); }
        }


        private string _CurProgressBarDisplay;
        public string CurProgressBarDisplay
        {
            get { return _CurProgressBarDisplay; }
            set { SetProperty(ref _CurProgressBarDisplay, value); }
        }


        /// 导出族文件
        public ICommand ExportCommand
        {
            get => new DelegateCommand(async() =>
            {
                
                List<DatagridModel> familyModels = new List<DatagridModel>();                
                DatagridModels.ToList().ForEach((item) =>
                {
                    if (item.IsCheck == true)
                    {                        
                        familyModels.Add(item);
                    }
                });
                if (familyModels.Count < 1)
                {
                    return;
                }

                double curExportCount = 0;
                CurProgressBarValue = 0;                               
                ProBarVisible = Visibility.Visible;

                foreach (DatagridModel curFmilyModel in familyModels)
                {
                    string[] strArray = curFmilyModel.Name.Split(new char[3] { '【', '、', '】' });
                    string curFamilyName = strArray[0];
                    IList<string> curFamilyTypeNames = new List<string>();
                    for (int i = 1; i < strArray.Count()-1; i++)
                    {
                        curFamilyTypeNames.Add(strArray[i]);
                    }
                    CurProgressBarValue = (curExportCount / familyModels.Count) * 100;
                    CurProgressBarDisplay = $"进度：{CurProgressBarValue:f1}%-{curFamilyName}";
                    //【采用Revit.Async 替代外部事件】
                    await RevitTask.RunAsync((uiApp) =>
                    {
                        // 【在这里执行同步代码，不返回任何结果，通过uiApp参数访问Revit模型数据库】
                        Document doc = uiApp.ActiveUIDocument.Document;                                               
                        Family family = new FilteredElementCollector(doc).OfClass(typeof(Family)).FirstOrDefault(x => x.Name == curFamilyName) as Family;//根据族名字过滤获取项目中的族
                        if (family == null)
                        {
                            return;
                        }

                        // 注意：
                        // doc.IsModifiable为true时（文档的事务未关闭），将不会调用EditFamily方法【IsModifiable不等于IsModified】
                        // doc.IsReadOnly为true时（只读状态），也不会调用EditFamily方法
                        // family为内建族(family.IsInPlace==true)或不可编辑族(family.IsEditable==false)时会抛出该族，无法获取族文档
                        Document familyDoc = doc.EditFamily(family);
                        if (familyDoc == null)
                        {
                            return;
                        }

                        // 移除不需保存的族类型   
                        Autodesk.Revit.DB.FamilyManager familyManager = familyDoc.FamilyManager;
                        if (familyManager.CurrentType.Name != " " && familyManager.CurrentType.Name != null)//排除族文档无类型的情况
                        {                           
                            FamilyTypeSet familyTypeSet = familyManager.Types;
                            Transaction trans = new Transaction(familyDoc, "保存族");
                            trans.Start();
                            foreach (FamilyType familyType in familyTypeSet)
                            {
                                var result = curFamilyTypeNames.FirstOrDefault(x => x == familyType.Name);
                                if (result == null)
                                {
                                    familyManager.CurrentType = familyType;
                                    familyManager.DeleteCurrentType();
                                }
                            }
                            trans.Commit();
                        }

                        // 获取导出文件夹
                        string exportFolderPath = ((curFmilyModel.Folder) as StorageFolder).FolderPath;
                        if (FamilyExportSet.SetIsCatagoryAsFolder)
                        {
                            exportFolderPath += $"\\{curFmilyModel.CatagoryName}";
                        }
                        if (!Directory.Exists(exportFolderPath))
                        {
                            Directory.CreateDirectory(exportFolderPath);
                        }

                        // 保存文件
                        SaveAsOptions saveAsOptions = new SaveAsOptions();// SaveAsOptions类                       
                        saveAsOptions.MaximumBackups = FamilyExportSet.SetMaxBackupCount;//属性：要保留的最大备份数
                        saveAsOptions.OverwriteExistingFile = true;//属性：是否覆盖原文件

                        string filePath = exportFolderPath + $"\\{curFamilyName}.rfa";
                        familyDoc.SaveAs(filePath, saveAsOptions);
                        familyDoc.Close();
                        curExportCount++;

                        string backupPath = exportFolderPath + "\\Backups";                       
                        if (!FamilyExportSet.SetIsSaveBackup) // 删除备份文件及文件夹
                        {
                            if (Directory.Exists(backupPath))
                            {
                                Directory.Delete(backupPath,true);
                            }
                            string[] exportFiles = Directory.GetFiles(exportFolderPath);
                            foreach (var item in exportFiles)
                            {
                                Regex regex = new Regex(@"[a-zA-Z0-9]+00+[0-9]+\.rfa");//构造方法里面的参数就是匹配规则——正则表达式

                                if (Regex.IsMatch(item, @"000+[0-9]+\.rfa$"))
                                {
                                    File.Delete(item);
                                }
                            }
                        }
                        else // 移动备份文件至Backups文件夹   
                        {
                            if (!Directory.Exists(backupPath))
                            {
                                Directory.CreateDirectory(backupPath);
                            }
                            foreach (var item in Directory.GetFiles(exportFolderPath))
                            {
                                Regex regex = new Regex(@"[a-zA-Z0-9]+00+[0-9]+\.rfa");//构造方法里面的参数就是匹配规则——正则表达式

                                if (Regex.IsMatch(item, @"000+[0-9]+\.rfa$"))//筛选备份文件
                                {
                                    //获取备份族文件的名称
                                    string oldFileName = Path.GetFileName(item);
                                    string newFileName;
                                    string[] files = Directory.GetFiles(backupPath, $"*{curFamilyName}*");
                                    int curCount = files.Count();
                                    if (curCount < FamilyExportSet.SetMaxBackupCount)
                                    {
                                        newFileName = $"{curFamilyName}.000{curCount + 1}.rfa";
                                        File.Move(item, backupPath + "\\" + newFileName);
                                    }
                                    else
                                    {
                                        int extraCount = curCount - FamilyExportSet.SetMaxBackupCount;
                                        for (int i = 0; i < curCount; i++)
                                        {
                                            if (i <= extraCount)//删除多余备份
                                            {
                                                File.Delete(files[i]);
                                            }
                                            else//重命名已备份
                                            {
                                                File.Move(files[i], files[i].Replace(Path.GetFileName(files[i]), $"{curFamilyName}.000{i - extraCount}.rfa"));
                                            }
                                        }
                                        newFileName = $"{curFamilyName}.000{FamilyExportSet.SetMaxBackupCount}.rfa";
                                        File.Move(item, backupPath + "\\" + newFileName);
                                    }
                                }
                            }
                        }                                                                                                              
                    });
                                        
                }
                CurProgressBarValue = (curExportCount / familyModels.Count) * 100;
                CurProgressBarDisplay = $"进度：{CurProgressBarValue:f1}%";
                MessageBoxResult exportResult=MessageBox.Show($"成功导出{curExportCount}个族", "提示");
                if (exportResult == MessageBoxResult.OK)
                {
                    ProBarVisible = Visibility.Collapsed;//隐藏进度条
                }
                #region 试错方法
                // 试错方法一：async-await 只会执行一次外部事件
                //await Task.Run(() =>
                //{
                //    SysCacheSubExport.Instance.ExportFamilyEvent.Raise();
                //}).ContinueWith((ts) =>
                //{
                //    CurExportCount += 1;

                //});
                //await Task.Run(() =>
                //{
                //    SysCacheSubExport.Instance.ExportFamilyEvent.Raise();
                //}).ContinueWith((ts) =>
                //{
                //    CurExportCount += 1;

                //});


                // 试错方法二：回调+延续任务 只会执行一次外部事件                       
                //ProssBar(excuteIndex); 

                //public Task ProssBar(int i)
                //{
                //    SysCacheSubExport.Instance.ExportFamilyEventHandler.ExcuteQueue.Enqueue(i);
                //    i += 1;
                //    if (i >= SysCacheSubExport.Instance.ExportFamilyEventHandler.Familys.Count)
                //    {
                //        return Task.Run(() =>
                //        {
                //            SysCacheSubExport.Instance.ExportFamilyEvent.Raise();
                //        }).ContinueWith((t) =>
                //        {
                //            CurExportCount += 1;
                //        });
                //    }
                //    Task tsk = ProssBar(i);
                //    return tsk.ContinueWith((t) => { SysCacheSubExport.Instance.ExportFamilyEvent.Raise(); }).ContinueWith((t) =>
                //    {
                //        CurExportCount += 1;
                //    });
                //}


                #endregion

            });
        }
       
        #region 辅助函数

        public void InitTree()
        {
            RootTreeModels.Clear();

            foreach (var curfamilyType in SysCacheSubExport.Instance.FamilyTreeSourceList)
            {
                var result = RootTreeModels.FirstOrDefault(x => x.Name == curfamilyType.Category.Name);
                if (result == null)
                {
                    CatagoryTreeNode catagoryTreeModel = new CatagoryTreeNode()//类别节点
                    {
                        Name = curfamilyType.Category.Name
                    };
                    var familyElements = from element in SysCacheSubExport.Instance.FamilyTreeSourceList
                                         where element.Category.Name == curfamilyType.Category.Name
                                         select element;

                    IList<string> FamilyNameList = new List<string>();//临时存储该类别所有族名
                    foreach (var CurFamilyElement in familyElements)
                    {
                        string curFamilyName = (CurFamilyElement as ElementType).FamilyName;
                        if (curFamilyName != "" && !FamilyNameList.Contains(curFamilyName))//过滤掉无族名的情况
                        {
                            FamilyNameList.Add(curFamilyName);
                        }
                    }

                    foreach (var familyName in FamilyNameList)
                    {
                        FamilyTreeNode familyTreeModel = new FamilyTreeNode()//族节点
                        {
                            Name = familyName,
                            Parent = catagoryTreeModel
                        };

                        foreach (var familyElement in familyElements)
                        {
                            string currentFamilyName = (familyElement as ElementType).FamilyName;
                            if (currentFamilyName == familyName)
                            {
                                FamilySymbolTreeNode familySymbolTreeModel = new FamilySymbolTreeNode()//族类型节点
                                {
                                    Name = familyElement.Name,
                                    Parent = familyTreeModel
                                };
                                familyTreeModel.TreeNodes.Add(familySymbolTreeModel);
                                //AllNodes.Add(familySymbolTreeModel);
                            }
                        }
                        catagoryTreeModel.TreeNodes.Add(familyTreeModel);
                    }
                    RootTreeModels.Add(catagoryTreeModel);
                }
            }
        }

        public void SelectDown(IBaseModel baseModel, bool newValue)
        {
            try
            {
                baseModel.IsCheck = newValue;
                baseModel.IsExpand = newValue;

                if (baseModel.TreeNodes == null)//同步至DataGrid
                {
                    if (newValue == true)//加选
                    {
                        var result = DatagridModels.FirstOrDefault(x => x.FamilyName == baseModel.Parent.Name);
                        if (result == null)//DataGrid中不存在该族
                        {
                            DatagridModel newDatagridModel = new DatagridModel()//类别节点
                            {
                                Name = $"{baseModel.Parent.Name}【{baseModel.Name}】",
                                CatagoryName = baseModel.Parent.Parent.Name,
                                FamilyName = baseModel.Parent.Name,
                                Folder = CustomerStorageFolder
                            };
                            DatagridModels.Add(newDatagridModel);

                            DataGridAllIsCheck = true;//更新DataGridAllIsCheck的值
                            var resultTwo = DatagridModels.FirstOrDefault(x => x.IsCheck == false);
                            if (resultTwo != null)
                            {
                                DataGridAllIsCheck = null;
                            }
                        }
                        else//DataGrid中已存在该族
                        {
                            DatagridModel curDatagridModel = result as DatagridModel;
                            curDatagridModel.Name = curDatagridModel.Name.Substring(0, curDatagridModel.Name.Length - 1);//去掉最后的“】”
                            curDatagridModel.Name += $"、{baseModel.Name}】";
                        }
                    }
                    else//减选
                    {
                        DatagridModel curDatagridModel = DatagridModels.FirstOrDefault(x => x.FamilyName == baseModel.Parent.Name);
                        string[] strArray = curDatagridModel.Name.Split(new char[3] { '【', '、', '】' });//最后一项为""——需排除该项干扰
                        if (strArray.Length > 3)//还存在其他族类型
                        {
                            curDatagridModel.Name = strArray[0] + "【";
                            for (int i = 1; i < strArray.Length - 1; i++)
                            {
                                if (strArray[i] != baseModel.Name)
                                {
                                    curDatagridModel.Name = curDatagridModel.Name + strArray[i] + "、";
                                }
                            }
                            curDatagridModel.Name = curDatagridModel.Name.Substring(0, curDatagridModel.Name.Length - 1);//去掉最后的“、”
                            curDatagridModel.Name += "】";
                        }
                        else//不存在其他族类型——直接移除该族
                        {
                            DatagridModels.Remove(curDatagridModel);

                            DataGridAllIsCheck = false;//更新DataGridAllIsCheck的值
                            var resultIsTrue = DatagridModels.FirstOrDefault(x => x.IsCheck == true);
                            var resultIsFalse = DatagridModels.FirstOrDefault(x => x.IsCheck == false);
                            if (resultIsTrue != null)
                            {
                                if (resultIsFalse == null)
                                {
                                    DataGridAllIsCheck = true;
                                }
                                else
                                {
                                    DataGridAllIsCheck = null;
                                }
                            }
                        }
                    }
                    return;
                }
                else
                {
                    foreach (var node in baseModel.TreeNodes)
                    {
                        if (node.IsCheck != newValue && node.NodeVisibility == Visibility.Visible)//仅仅当前选择可见节点
                        {
                            SelectDown(node, newValue);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Instance.Info($"报错信息,{ex}");
                Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
            }
        }

        public void SelectUp(IBaseModel baseModel)
        {
            if (baseModel.Parent == null)
            {
                //是否全选判断
                TreeAllIsCheck = true;
                foreach (var peerNode in RootTreeModels)
                {
                    if (peerNode.IsCheck != true)
                    {
                        TreeAllIsCheck = false;
                        break;
                    }
                }
                return;
            }
            if (baseModel.IsCheck != null)
            {
                baseModel.Parent.IsCheck = baseModel.IsCheck;                
                foreach (var peerNode in baseModel.Parent.TreeNodes)
                {
                    if (peerNode.IsCheck != baseModel.IsCheck)
                    {
                        baseModel.Parent.IsCheck = null;
                        break;
                    }
                }
                if (baseModel.Parent.IsCheck==false)//若其子节点都未选择，则折叠该节点
                {
                    baseModel.Parent.IsExpand = false;
                }
            }
            else
            {
                baseModel.Parent.IsCheck = null;
            }
            SelectUp(baseModel.Parent);
        }

        /// 递归遍历树
        public void RecursiveTraverseTree(IBaseModel baseModel, Action<IBaseModel> action)
        {
            action(baseModel);
            if (baseModel.TreeNodes==null)
            {
                return;
            }
            baseModel.TreeNodes.ToList().ForEach(x =>
            {
                RecursiveTraverseTree(x, action);
            });
        }

        public void ShowSearchUp(IBaseModel baseModel)
        {
            if (baseModel.Parent == null)
            {
                return;
            }
            baseModel.Parent.NodeVisibility = Visibility.Visible;
            baseModel.Parent.IsExpand = true;
            ShowSearchUp(baseModel.Parent);
        }

        public void ShowSearchDown(IBaseModel baseModel)
        {
            if (baseModel.TreeNodes == null)
            {
                return;
            }
            baseModel.TreeNodes.ToList().ForEach(x =>
            {
                x.NodeVisibility = Visibility.Visible;
                x.IsExpand = true;
                ShowSearchDown(x);
            });
        }

        #endregion

    }
}
