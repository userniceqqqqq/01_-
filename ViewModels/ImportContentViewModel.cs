using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Ookii.Dialogs.WinForms;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using QiShiLog;
using QiShiLog.Log;
using Revit.Async;
using RevitStorage.StructuredStorage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using TextBox = System.Windows.Controls.TextBox;

namespace FamilyManager.MainModule.ViewModels
{
    class ImportContentViewModel : BindableBase
    {
        //除去"+"的Tab索引（从1开始）——“标签tabIndex”
        int tabIndex = 1;

        public ObservableCollection<TabModel> TabModels { get; set; }

        //临时存储TabModel对象
        private IList<TabModel> tabModels = new List<TabModel>();

        public ImportContentViewModel()
        {
            TabModels = new ObservableCollection<TabModel>();
            var firstTabM = new TabModel()
            {
                Header = $"新标签{tabIndex}  ",
            };
            TabModels.Add(firstTabM);
            AddNewPlusButton();
        }

        #region 功能辅助函数
        private void AddNewPlusButton()
        {
            var plusTab = new TabModel()
            {
                Header = "+",
                IsPlaceholder = true
            };
            TabModels.Add(plusTab);
        }
        void ConvertPlusToNewTab(TabModel tab)
        {
            tabIndex++;
            tab.Header = $"新标签{tabIndex}  ";
            tab.IsPlaceholder = false;
        }

        // 递归遍历族库路径
        private void GetNewTabModels(string path)
        {
            string[] familyFiles = Directory.GetFiles(path, "*.rfa");//遍历获取路径中所有.rfa族文件的完整名称（包括其路径信息）
            if (familyFiles.Count() > 0)
            {
                tabModels.Add(new TabModel()
                {
                    IsPlaceholder = false,
                    FilePath = path,
                    Header = Path.GetFileNameWithoutExtension(path) + "  ",
                    fileList = familyFiles.ToList()
                });
            }
            foreach (string familyFolder in Directory.GetDirectories(path))// 遍历获取路径中所有子目录的完整名称（包括其路径信息）
            {
                GetNewTabModels(familyFolder);
            }
            return;
        }

        #endregion



        //没有除去"+"的Tab索引（从0开始）
        private int _TabIndex=0;
        public int TabIndex
        {
            get { return _TabIndex; }
            set { SetProperty(ref _TabIndex, value); }
        }


        public ICommand SelectTabCommand
        {
            get => new DelegateCommand(() =>
            {               
                if (TabIndex != 0 && TabIndex == TabModels.Count - 1) //last tab
                {
                    var tab = TabModels.Last();
                    ConvertPlusToNewTab(tab);
                    AddNewPlusButton();
                }
            });
        }

        public ICommand CloseTabCommand
        {
            get => new DelegateCommand<object>((obj) =>
            {
                if (TabModels.Count > 2)
                {
                    var index = TabModels.IndexOf(obj as TabModel);
                    if (index == (TabModels.Count - 2))//last tab before [+]
                    {
                        TabIndex--;
                    }
                    //TabModels.RemoveAt(index);不建议采用RemoveAt
                    TabModels.Remove(obj as TabModel);
                }
            });
        }

        public ICommand CloseAllTabCommand
        {
            get => new DelegateCommand(() =>
            {
                tabIndex = 1;
                TabIndex = 0;
                for (int a = TabModels.Count - 2; a >= 0; a--)
                {
                    if (a == 0)
                    {
                        var firstTabM = new TabModel()
                        {
                            Header = $"新标签{tabIndex}  ",
                        };
                        TabModels[a] = firstTabM;                        
                        continue;
                    }
                    TabModels.RemoveAt(a);
                }
                TabIndex = 0;
            });
        }

        public ICommand LoadFileCommand
        {
            // 注意KeyEventArgs命名空间的引用： System.Windows.Input，而非System.Windows.Forms
            get => new DelegateCommand<KeyEventArgs>((e) =>
            {
                if (e.Key!= Key.Enter)
                {
                    return;
                }
                // 【1】获取当前Tab页面对象
                TextBox textBox = (e.Source) as TextBox;
                TabModel currentTabM = (textBox.DataContext) as TabModel;// 注意TextBox命名空间的引用：System.Windows.Controls，而非System.Windows.Forms                    
                var curIndex = TabModels.IndexOf(currentTabM);

                string loadPath = currentTabM.FilePath;
                if (!Directory.Exists(loadPath))
                {
                    MessageBox.Show($"不存在该文件路径：\n{ currentTabM.FilePath}", "提示");
                    return;
                }

                //【2】清空已有的族文件路径list、族模型list及检索关键词
                currentTabM.FamilyObjects.Clear();
                currentTabM.fileList.Clear();
                currentTabM.Keyword = null;

                // 【3】初始化族库UI
                tabModels.Clear();
                GetNewTabModels(loadPath);//递归获取族库路径下所有包含族文件的目录  
                                          // 【 2.1】族库路径下没有包含族文件的目录
                if (tabModels.Count < 1)
                {
                    currentTabM.FilePath = loadPath;
                    currentTabM.Header = Path.GetFileNameWithoutExtension(loadPath) + "  ";
                    currentTabM.CurrentPage = "文件夹不含族文件！";
                    currentTabM.CurrentFamilyObjects.Clear();
                    TabModels[curIndex] = currentTabM;
                    TabIndex = curIndex;
                    return;
                }
                // 【 3.2】族库路径下存在族文件的目录
                if (tabModels.Count > 1)
                {
                    for (int i = 0; i < tabModels.Count - 2; i++)//添加新Tab页面
                    {
                        TabModels.Add(new TabModel());
                        tabIndex++;
                    }
                    tabIndex++;
                    AddNewPlusButton();
                    int j = tabModels.Count - 1;
                    for (int i = tabIndex - tabModels.Count; i > curIndex; i--)//自尾向前移项覆盖
                    {
                        TabModels[i + j] = TabModels[i];
                    }
                }
                foreach (var curTabModel in tabModels)
                {
                    curTabModel.GetFamilyObjects(curTabModel.fileList);//获取族模型列表
                    curTabModel.InitPage();//根据族对象列表~初始化页面(方法封装)
                    if (tabModels.IndexOf(curTabModel) == 0)
                    {
                        currentTabM.FilePath = curTabModel.FilePath;
                        currentTabM.Header = Path.GetFileNameWithoutExtension(curTabModel.FilePath) + "  ";
                        TabModels[curIndex] = curTabModel;
                        TabIndex = curIndex;//设为当前选择Tab页面
                        curIndex++;
                        continue;
                    }
                    TabModels[curIndex] = curTabModel;
                    curIndex++;
                }
            });
        }

        public ICommand OpenFileCommand
        {
            get => new DelegateCommand<object>((obj) =>
            {
                var curIndex = TabModels.IndexOf(obj as TabModel);
                TabModel currentTabM = obj as TabModel;
                string loadPath;

                //【1】清空已有的族文件路径list、族模型list及检索关键词
                currentTabM.FamilyObjects.Clear();
                currentTabM.fileList.Clear();
                currentTabM.Keyword = null;

                //【2】通过系统对话框~获取族库路径                
                VistaFolderBrowserDialog vistaOpenFileDialog = new VistaFolderBrowserDialog();//引用第三方库：Ookii.Dialogs.WinForms——会自动处理不正确输入
                vistaOpenFileDialog.Description = "请选择一个文件夹路径";//设置对话框上方显示的说明性文本
                vistaOpenFileDialog.SelectedPath = currentTabM.FilePath;//设置选定路径（当不存在该路径时，默认打开电脑文件首个路径）
                                                                        //会在“/”自动补充一个“/”，无需再手动添加“@”
                                                                        //C:\ProgramData\Autodesk\RVT 2020\Libraries\China\建筑\柱
                if (vistaOpenFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    loadPath= vistaOpenFileDialog.SelectedPath;                   
                    tabModels.Clear();
                    GetNewTabModels(loadPath);//递归获取族库路径下所有包含族文件的目录                   
                }
                else
                {
                    return;
                }

                // 【3】初始化族库UI
                // 【 3.1】族库路径下没有包含族文件的目录
                if (tabModels.Count<1)
                {
                    currentTabM.FilePath = loadPath;
                    currentTabM.Header = Path.GetFileNameWithoutExtension(loadPath) + "  ";
                    currentTabM.CurrentPage = "文件夹不含族文件！";
                    currentTabM.CurrentFamilyObjects.Clear();
                    TabModels[curIndex] = currentTabM;
                    TabIndex = curIndex;
                    return;
                }
                // 【 3.2】族库路径下存在族文件的目录
                if (tabModels.Count>1)
                {                   
                    for (int i=0;i< tabModels.Count-2;i++)//添加新Tab页面
                    {
                        TabModels.Add(new TabModel());
                        tabIndex++;
                    }
                    tabIndex++;
                    AddNewPlusButton();
                   
                    int preLastIndex = TabModels.Count - tabModels.Count;
                    int j = tabModels.Count - 1;
                    for (int i = preLastIndex; i>curIndex; i--)//自尾向前移项覆盖
                    {                       
                        TabModels[i+j] = TabModels[i];
                    }
                }
                foreach (var curTabModel in tabModels)
                {
                    curTabModel.GetFamilyObjects(curTabModel.fileList);//获取族模型列表
                    curTabModel.InitPage();//根据族对象列表~初始化页面(方法封装)
                    if (tabModels.IndexOf(curTabModel)==0)
                    {
                        currentTabM.FilePath = curTabModel.FilePath;
                        currentTabM.Header = Path.GetFileNameWithoutExtension(curTabModel.FilePath) + "  ";
                        TabModels[curIndex] = curTabModel;
                        TabIndex = curIndex;//设为当前选择Tab页面
                        curIndex++;
                        continue;
                    }
                    TabModels[curIndex] = curTabModel;
                    curIndex++;
                }                               
            });
        }

        public ICommand LoadFamilyCommand
        {
            get => new DelegateCommand<object>(async (obj) =>
            {
                FamilyObject currentFamily = obj as FamilyObject;
                string path = currentFamily.Locatoin;//从界面上获取的值
                string rfaName = Path.GetFileNameWithoutExtension(path);//族的名称

                //SysCache.Instance.CurrentRfaLocation = currentFamily.Locatoin;//将族模型路径存储至缓存类中（便于在外部事件中直接通过缓存类调用族模型路径）
                //SysCache.Instance.LoadEvent.Raise();//通过缓存类直接启动外部事件
                //                                    //MessageBox.Show($"载入族文件路径：\n{ SysCache.Instance.CurrentRfaLocation}", "准备载入");


                try
                {
                    await RevitTask.RunAsync((uiApp) =>
                    {
                        //【1】获取当前文档
                        Document doc = uiApp.ActiveUIDocument.Document;
                        UIDocument uiDocument = uiApp.ActiveUIDocument;
                        //【2】获取族路径、族名称                    

                        if (string.IsNullOrWhiteSpace(path))//判断是否存在该族文件路径
                        {
                            return;
                        }

                        //【3】获取族——项目中没有，则将路径中的族载入到项目中
                        Family family = new FilteredElementCollector(doc).OfClass(typeof(Family)).FirstOrDefault(x => x.Name == rfaName) as Family;//根据族名字过滤获取项目中的族
                        if (family == null)
                        {
                            try
                            {
                                Transaction transaction = new Transaction(doc, "载入族"); //事务
                                transaction.Start();                                                               
                                doc.LoadFamily(path, out family); //【注意】：共享族直接通过doc.LoadFamily(path, out family)载入会返回null
                                if (family == null)
                                {
                                    doc.LoadFamily(path, new shareFamilyLoadOption(), out family); //【解决方法】：实现IFamilyLoadOptions接口
                                }
                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                Logger.Instance.Info($"载入出错{doc},{ex}");
                                Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
                            }
                        }
                        //【4.1】获取族类型
                        FamilySymbol familySymbol = doc.GetElement(family.GetFamilySymbolIds().First<ElementId>()) as FamilySymbol;

                        //【4.2】开始放置
                        try
                        {
                            Logger.Instance.Info($"开始放置");
                            uiDocument.PromptForFamilyInstancePlacement(familySymbol);
                            //注意：
                            //1）此方法打开自己的事务（不需要再新建事务）
                            //2）在单个调用中，用户可以放置多个实例，直到完成放置
                            //（取消放置：使用“取消”或“ESC”或单击UI中的其他位置————需通过try-catch进行异常捕获~取消放置事件，否在则取消之前放置的实例无效）
                            //3）在放置族操作期间，不允许用户更改当前活动视图
                        }
                        catch (Autodesk.Revit.Exceptions.OperationCanceledException e)
                        {
                            Logger.Instance.Info($"用户取消了放置");
                        }
                        //Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
                    });
                }
                catch (Exception ex)
                {
                    Logger.Instance.Info($"报错信息,{ex}");
                    Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
                }
              
            });
        }       


        public ICommand FirstCommand
        {
            get => new DelegateCommand<object>((obj) =>
            {
                var index = TabModels.IndexOf(obj as TabModel);
                TabModel currentTabM = obj as TabModel;

                List<FamilyObject> currentFamilyObjects = currentTabM.pagedTable.First(currentTabM.FamilyObjects, currentTabM.NumberOfRecord[currentTabM.ComboIndex]);//初始化首页的族模型显示
                if (currentFamilyObjects.Count > 0)
                {
                    currentTabM.CurrentFamilyObjects.Clear();
                    foreach (var familyObject in currentFamilyObjects)
                    {
                        currentTabM.CurrentFamilyObjects.Add(familyObject);
                    }
                }
                currentTabM.CurrentPage = currentTabM.PageNumberDisplay();//设置当前页显示状态

                TabModels[index] = currentTabM;
                TabIndex = index;
            });
        }
        public ICommand BackwardsCommand
        {
            get => new DelegateCommand<object>((obj) =>
            {
                var index = TabModels.IndexOf(obj as TabModel);
                TabModel currentTabM = obj as TabModel;

                List<FamilyObject> currentFamilyObjects = currentTabM.pagedTable.Previous(currentTabM.FamilyObjects, currentTabM.NumberOfRecord[currentTabM.ComboIndex]);//初始化首页的族模型显示
                if (currentFamilyObjects.Count > 0)
                {
                    currentTabM.CurrentFamilyObjects.Clear();
                    foreach (var familyObject in currentFamilyObjects)
                    {
                        currentTabM.CurrentFamilyObjects.Add(familyObject);
                    }
                }
                currentTabM.CurrentPage = currentTabM.PageNumberDisplay();//设置当前页显示状态

                TabModels[index] = currentTabM;
                TabIndex = index;
            });
        }
        public ICommand ForwardCommand
        {
            get => new DelegateCommand<object>((obj) =>
            {
                var index = TabModels.IndexOf(obj as TabModel);
                TabModel currentTabM = obj as TabModel;

                List<FamilyObject> currentFamilyObjects = currentTabM.pagedTable.Next(currentTabM.FamilyObjects, currentTabM.NumberOfRecord[currentTabM.ComboIndex]);//初始化首页的族模型显示
                if (currentFamilyObjects.Count > 0)
                {
                    currentTabM.CurrentFamilyObjects.Clear();
                    foreach (var familyObject in currentFamilyObjects)
                    {
                        currentTabM.CurrentFamilyObjects.Add(familyObject);
                    }
                }
                currentTabM.CurrentPage = currentTabM.PageNumberDisplay();//设置当前页显示状态

                TabModels[index] = currentTabM;
                TabIndex = index;

            });
        }
        public ICommand LastCommand
        {
            get => new DelegateCommand<object>((obj) =>
            {
                var index = TabModels.IndexOf(obj as TabModel);
                TabModel currentTabM = obj as TabModel;

                List<FamilyObject> currentFamilyObjects = currentTabM.pagedTable.Last(currentTabM.FamilyObjects, currentTabM.NumberOfRecord[currentTabM.ComboIndex]);//初始化首页的族模型显示
                if (currentFamilyObjects.Count > 0)
                {
                    currentTabM.CurrentFamilyObjects.Clear();
                    foreach (var familyObject in currentFamilyObjects)
                    {
                        currentTabM.CurrentFamilyObjects.Add(familyObject);
                    }
                }
                currentTabM.CurrentPage = currentTabM.PageNumberDisplay();//设置当前页显示状态

                TabModels[index] = currentTabM;
                TabIndex = index;
            });
        }
    }
}
