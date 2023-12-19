using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Prism.Commands;
using Prism.Mvvm;
using QiShiLog;
using QiShiLog.Log;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TextBox = System.Windows.Controls.TextBox;
using MessageBox = System.Windows.MessageBox;
using Revit.Async;

namespace FamilyManager.MainModule.SubEdit.ViewModels
{
    class EditContentViewModel : BindableBase
    {
        public EditContentViewModel()
        {
            SysCacheSubEdit.Instance.FamilySourceList.Clear();
            SysCacheSubEdit.Instance.LoadFamilySourceEventHandler.Execute(SysCacheSubEdit.Instance.ExternEventExecuteApp);
            InitTree();
            InitDataGrid();
        }

        #region 获取族
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


        public List<FamilyTypeWay> FamilyTypeWays { get; set; } = new List<FamilyTypeWay>()
        {
            new FamilyTypeWay(){Display="系统族",Select=FamilyType.SystemFamily},
            new FamilyTypeWay(){Display="载入族",Select=FamilyType.LoadFamily}
        };

        private FamilyType _CurFamilyType = FamilyType.SystemFamily;
        public FamilyType CurFamilyType
        {
            get { return _CurFamilyType; }
            set { SetProperty(ref _CurFamilyType, value, FamilyWayChanged); }
        }

        public void FamilyWayChanged()
        {
            if (CurFamilyType == FamilyType.LoadFamily)//验证：族名是否可编辑
            {
                IsLoadFamily = true;
            }
            else
            {
                IsLoadFamily = false;
                CurEditName = EditName.TypeName;
            }
            selectQueue.Clear();
            SelectQueueIsNotNull = false;

            SysCacheSubEdit.Instance.LoadFamilySourceEventHandler.LoadSource = CurFamilySource;
            SysCacheSubEdit.Instance.LoadFamilySourceEventHandler.LoadType = CurFamilyType;
            SysCacheSubEdit.Instance.FamilySourceList.Clear();
            SysCacheSubEdit.Instance.LoadFamilySourceEventHandler.Execute(SysCacheSubEdit.Instance.ExternEventExecuteApp);

            //SysCacheSubEdit.Instance.LoadFamilySourceEvent.Raise();// Revit会在下个闲置时间到时来才调用IExternalEventHandler.Execute（）方法，
            //                                                       // 去执行外部事件(更新SysCacheSubEdit.Instance.FamilySourceList)
            InitTree();
            InitDataGrid();

            curFamilysDataForward.Clear();//清空前进栈
            curFamilyIdsDataForward.Clear();
            if (ForwardIsNotNull == true)
            {
                ForwardIsNotNull = false;
            }

            curFamilysDataBack.Clear();//清空后退栈
            curFamilyIdsDataBack.Clear();
            if (BackIsNotNull == true)
            {
                BackIsNotNull = false;
            }

        }
        #endregion


        public List<EditNameWay> EditNameWays { get; set; } = new List<EditNameWay>()
        {
            new EditNameWay(){Display="族名",Select=EditName.FamilyName},
            new EditNameWay(){Display="族类型名",Select=EditName.TypeName}
        };

        private EditName _CurEditName = EditName.TypeName;
        public EditName CurEditName
        {
            get { return _CurEditName; }
            set { SetProperty(ref _CurEditName, value); }
        }


        #region 类别选择并且初始化DataGrid

        // 存储勾选的子节点       
        public List<CatagoryTreeNode> SelectNodes { get; set; } = new List<CatagoryTreeNode>();

        // 存储所有的子节点 
        public List<CatagoryTreeNode> AllNodes { get; set; } = new List<CatagoryTreeNode>();

        // TreeView的根节点        
        public ObservableCollection<CatagoryTreeNode> RootTreeModels { get; set; } = new ObservableCollection<CatagoryTreeNode>();

        public ICommand CatagorySelectCommand
        {
            get => new DelegateCommand<CatagoryTreeNode>((treeModel) =>
            {
                try
                {                    
                    // 更新选择的类别节点
                    var result = RootTreeModels.FirstOrDefault(x => x.Name == treeModel.Name);
                    if (result != null)
                    {
                        SelectAll(result);//选择的是父节点，则选择其所有的子节点，或取消选择所以子节点（子节点已经全部被选择）
                    }
                    else
                    {
                        Select(treeModel);//选择的是子节点，则选择该子节点，或取消选择该子节点
                    }

                    //更新当前DataGrid数据源
                    selectQueue.Clear();
                    SelectQueueIsNotNull = false;

                    CurFamilysData.Clear();
                    foreach (var curCategory in SelectNodes)//初始化DataGrid
                    {
                        foreach (var curFamilyData in familysData)
                        {
                            if (curFamilyData.CategoryName == curCategory.Name)
                            {
                                CurFamilysData.Add(curFamilyData);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Instance.Info($"报错信息,{ex}");
                    Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
                }
            });
        }
        #endregion


        #region DataGrid交互

        public List<DatagridModel> familysData = new List<DatagridModel>();
        public ObservableCollection<DatagridModel> CurFamilysData { get; set; } = new ObservableCollection<DatagridModel>();
        //【注意：UI绑定的是内存对象[new ObservableCollection<DatagridModel>()],而不是引用[CurFamilysData]——故直接更改引用[浅拷贝]是不会传值到UI】        

        // 用于判断族名列是否可编辑       
        private bool _IsLoadFamily = false;
        public bool IsLoadFamily
        {
            get { return _IsLoadFamily; }
            set { SetProperty(ref _IsLoadFamily, value); }
        }

        public Stack<IList<DatagridModel>> curFamilysDataBack = new Stack<IList<DatagridModel>>();//后退栈
        public Stack<IList<int>> curFamilyIdsDataBack = new Stack<IList<int>>();
        public Stack<IList<DatagridModel>> curFamilysDataForward = new Stack<IList<DatagridModel>>();//前进栈
        public Stack<IList<int>> curFamilyIdsDataForward = new Stack<IList<int>>();
        public List<string> theSameFamilyNames = new List<string>();//重名的名称
        public List<string> theSameFamilyTypeNames = new List<string>();


        public ICommand CellEditEndingCommand
        {
            get => new DelegateCommand<DataGridCellEditEndingEventArgs>((e) =>
            {
                string editColumHeader = (e.Column.Header) as string;
                DatagridModel editModel = (e.Row.Item) as DatagridModel;                

                if (editColumHeader.Equals("族名"))//1、编辑列为族名——【同类别的族重名修改会报错、同族名的一起变化】
                {
                    if (IsLoadFamily == false)//排除单元格不可编辑情况
                    {
                        return;
                    }

                    //【总结：获取newValue三种方式】
                    // 1>、  设置单元格绑定：UpdateSourceTrigger=PropertyChanged，弊端：需在CellBeginningEditCommand中提前获取oldValue,且会导致CellEditEndingCommand无法取消执行：e.Cancel = true
                    // 2>、 (e.EditingElement as TextBox).Text——对应<DataGridTextColumn/>
                    // 3>、 适用于：<DataGridTextColumn>——<DataGridTemplateColumn.CellEditingTemplate>—— <DataTemplate>——<TextBox>
                    //      ContentPresenter contentPresenter = e.EditingElement as ContentPresenter;
                    //      DataTemplate editingTemplate = contentPresenter.ContentTemplate;
                    //      var myTextBox = editingTemplate.FindName("EditTextBox", contentPresenter);
                    //      string newFamilyName = (myTextBox as TextBox).Text;                    
                    // 4>、 适用于：<DataGridTextColumn>——<DataGridTemplateColumn.CellEditingTemplate>—— <DataTemplate>——<ContentPresenter>—— <DataTemplate>——<TextBox>
                    ContentPresenter contentPresenter = e.EditingElement as ContentPresenter;
                    DataTemplate editingTemplate = contentPresenter.ContentTemplate;
                    ContentPresenter contentPresenterOne = (editingTemplate.FindName("Presenter", contentPresenter)) as ContentPresenter;
                    DataTemplate editingTemplateOne = contentPresenterOne.ContentTemplate;
                    var myTextBox = editingTemplateOne.FindName("EditTextBox", contentPresenterOne);
                    string newFamilyName = (myTextBox as TextBox).Text.Trim();

                    if (editModel.FamilyName != newFamilyName)//发生修改
                    {
                        IList<DatagridModel> oldFamilysData = new List<DatagridModel>();
                        IList<int> oldFamilyIdsData = new List<int>();//存储此轮已经修改过的族Id

                        var result = CurFamilysData.FirstOrDefault(x => x.CategoryName == editModel.CategoryName && x.FamilyName == newFamilyName);                                                
                        foreach (var curFamilyModel in CurFamilysData)
                        {
                            if (result == null)//无重名情况
                            {                                
                                if (curFamilyModel.CategoryName == editModel.CategoryName && curFamilyModel.FamilyName == editModel.FamilyName && curFamilyModel.Id != editModel.Id)//1.2同族名的一起变化（包含了族名正确修改）
                                {
                                    oldFamilysData.Add(new DatagridModel()
                                    {
                                        CategoryName = curFamilyModel.CategoryName,
                                        FamilyName = curFamilyModel.FamilyName,
                                        FamilyTypeName = curFamilyModel.FamilyTypeName,
                                        IsSelected = curFamilyModel.IsSelected,
                                        Id = curFamilyModel.Id,
                                        IsEdit= curFamilyModel.IsEdit
                                    });//引用类型：深拷贝                                   
                                    oldFamilyIdsData.Add(curFamilyModel.Id);

                                    curFamilyModel.FamilyName = newFamilyName;
                                    curFamilyModel.IsEdit = true;
                                }
                                else if (curFamilyModel.Id == editModel.Id)
                                {
                                    oldFamilysData.Add(new DatagridModel()
                                    {
                                        CategoryName = curFamilyModel.CategoryName,
                                        FamilyName = curFamilyModel.FamilyName,
                                        FamilyTypeName = curFamilyModel.FamilyTypeName,
                                        IsSelected = curFamilyModel.IsSelected,
                                        Id = curFamilyModel.Id,
                                        IsEdit = curFamilyModel.IsEdit
                                    });//引用类型：深拷贝
                                    oldFamilyIdsData.Add(curFamilyModel.Id);

                                    curFamilyModel.IsEdit = true;
                                }
                            }
                            else if (curFamilyModel.CategoryName == editModel.CategoryName && curFamilyModel.FamilyName == newFamilyName)//1.2同类别的族重名修改会报错
                            {
                                curFamilyModel.IsReportError = true;//显示重名项                               
                                e.Cancel = true;//取消修改                                
                            }
                        }
                        EditInStack(oldFamilysData, oldFamilyIdsData);
                        if (result != null)//有重名情况——记录到后退栈、清空前进栈
                        {
                            theSameFamilyNames.Add(newFamilyName);
                        }                       
                    }
                    else//未修改（需取消当前重名项）
                    {
                        foreach (var theSameFamilyName in theSameFamilyNames)
                        {
                            foreach (var curFamilyModel in CurFamilysData)
                            {
                                if (curFamilyModel.CategoryName == editModel.CategoryName && curFamilyModel.FamilyName == theSameFamilyName)
                                {
                                    curFamilyModel.IsReportError = false;//取消显示重名项
                                }
                            }
                        }
                        theSameFamilyNames.Clear();
                    }
                }
                else//2、编辑族类型名字——【存在同族的族类型重名报错】
                {
                    string newFamilyTypeName = (e.EditingElement as TextBox).Text.Trim();

                    if (editModel.FamilyTypeName != newFamilyTypeName)//发生修改
                    {
                        IList<DatagridModel> oldFamilysData = new List<DatagridModel>();
                        IList<int> oldFamilyIdsData = new List<int>();//存储此轮已经修改过的族Id

                        var result = CurFamilysData.FirstOrDefault(x => x.CategoryName == editModel.CategoryName && x.FamilyName == editModel.FamilyName && x.FamilyTypeName == newFamilyTypeName);                        
                        foreach (var curFamilyModel in CurFamilysData)
                        {
                            if (result == null && curFamilyModel.Id == editModel.Id)//无重名情况
                            {
                                oldFamilysData.Add(new DatagridModel()
                                {
                                    CategoryName = curFamilyModel.CategoryName,
                                    FamilyName = curFamilyModel.FamilyName,
                                    FamilyTypeName = curFamilyModel.FamilyTypeName,
                                    IsSelected = curFamilyModel.IsSelected,
                                    Id = curFamilyModel.Id,
                                    IsEdit = curFamilyModel.IsEdit
                                });//引用类型：深拷贝
                                oldFamilyIdsData.Add(curFamilyModel.Id);

                                curFamilyModel.IsEdit = true;                               
                            }
                            else if (curFamilyModel.CategoryName == editModel.CategoryName && curFamilyModel.FamilyName == editModel.FamilyName && curFamilyModel.FamilyTypeName == newFamilyTypeName)//2.1存在同族的族类型重名报错
                            {
                                curFamilyModel.IsReportError = true;//显示重名项                               
                                e.Cancel = true;//取消修改
                            }
                        }
                        EditInStack(oldFamilysData, oldFamilyIdsData);
                        if (result != null)//有重名情况——记录到后退栈、清空前进栈
                        {
                            theSameFamilyTypeNames.Add(newFamilyTypeName);
                        }
                    }
                    else//未修改（需取消当前重名项）
                    {
                        foreach (var theSameFamilyTypeName in theSameFamilyTypeNames)
                        {
                            foreach (var curFamilyModel in CurFamilysData)
                            {
                                if (curFamilyModel.CategoryName == editModel.CategoryName && curFamilyModel.FamilyName == editModel.FamilyName && curFamilyModel.FamilyTypeName == theSameFamilyTypeName)
                                {
                                    curFamilyModel.IsReportError = false;//取消显示重名项
                                }
                            }
                        }
                        theSameFamilyTypeNames.Clear();
                    }
                }
            });
        }

        // 控制后退按钮的Enable
        private bool _BackIsNotNull = false;
        public bool BackIsNotNull
        {
            get { return _BackIsNotNull; }
            set { SetProperty(ref _BackIsNotNull, value); }
        }

        // 控制前进按钮的Enable
        private bool _ForwardIsNotNull = false;
        public bool ForwardIsNotNull
        {
            get { return _ForwardIsNotNull; }
            set { SetProperty(ref _ForwardIsNotNull, value); }
        }

        public ICommand BackCommand
        {
            get => new DelegateCommand(() =>
            {
                IList<DatagridModel> newFamilysData = new List<DatagridModel>();                                              
                IList<int> newFamilyIdsData = curFamilyIdsDataBack.Pop();
                curFamilyIdsDataForward.Push(newFamilyIdsData);

                foreach (var id in newFamilyIdsData)
                {
                    DatagridModel curFamily = familysData.FirstOrDefault(x => x.Id == id);
                    newFamilysData.Add(curFamily);//无需新建内存对象——引用类型：浅拷贝即可（其余同理）curFamilyIdsDataBack
                }
                curFamilysDataForward.Push(newFamilysData);//入栈（前进）                
                if (ForwardIsNotNull == false)
                {
                    ForwardIsNotNull = true;
                }

                IList<DatagridModel> oldFamilysData = curFamilysDataBack.Pop();//出栈（后退）                
                if (curFamilysDataBack.Count <= 0)
                {
                    BackIsNotNull = false;
                }               
                foreach (var oldFamilyModel in oldFamilysData)
                {
                    DatagridModel familyData = familysData.FirstOrDefault(x => x.Id == oldFamilyModel.Id);//注意：CurFamilysData与familysData要保持一致
                    int familyIndex= familysData.IndexOf(familyData);
                    familysData[familyIndex] = oldFamilyModel;
                    DatagridModel curFamily = CurFamilysData.FirstOrDefault(x => x.Id == oldFamilyModel.Id);
                    if (curFamily != null)
                    {
                        int curFamilyIndex = CurFamilysData.IndexOf(curFamily);
                        CurFamilysData[curFamilyIndex] = oldFamilyModel;
                    }

                    DatagridModel selectFamily = selectQueue.FirstOrDefault(x => x.Id == oldFamilyModel.Id);
                    if (selectFamily!=null)
                    {
                        while (selectQueue.Peek().Id != oldFamilyModel.Id)
                        {
                            selectQueue.Enqueue(selectQueue.Dequeue());
                        }
                        selectQueue.Dequeue();
                        selectQueue.Enqueue(oldFamilyModel);
                    }
                    
                }               
            });
        }

        public ICommand ForwardCommand
        {
            get => new DelegateCommand(() =>
            {
                IList<DatagridModel> oldFamilysData = new List<DatagridModel>();
                IList<int> oldFamilyIdsData = curFamilyIdsDataForward.Pop();
                curFamilyIdsDataBack.Push(oldFamilyIdsData);

                foreach (var id in oldFamilyIdsData)
                {
                    DatagridModel curFamily = familysData.FirstOrDefault(x => x.Id == id);
                    oldFamilysData.Add(curFamily);//无需新建内存对象——引用类型：浅拷贝即可（其余同理）curFamilyIdsDataBack
                }
                curFamilysDataBack.Push(oldFamilysData);//入栈（后退）
                if (BackIsNotNull == false)
                {
                    BackIsNotNull = true;
                }

                IList<DatagridModel> newFamilysData = curFamilysDataForward.Pop();//出栈（前进）
                if (curFamilysDataForward.Count <= 0)
                {
                    ForwardIsNotNull = false;
                }
                foreach (var newFamilyModel in newFamilysData)
                {
                    DatagridModel familyData = familysData.FirstOrDefault(x => x.Id == newFamilyModel.Id);
                    int familyIndex = familysData.IndexOf(familyData);
                    familysData[familyIndex] = newFamilyModel;
                    DatagridModel curFamily = CurFamilysData.FirstOrDefault(x => x.Id == newFamilyModel.Id);
                    if (curFamily != null)
                    {
                        int curFamilyIndex = CurFamilysData.IndexOf(curFamily);
                        CurFamilysData[curFamilyIndex] = newFamilyModel;
                    }

                    DatagridModel selectFamily = selectQueue.FirstOrDefault(x => x.Id == newFamilyModel.Id);
                    if (selectFamily!=null)
                    {
                        while (selectQueue.Peek().Id != newFamilyModel.Id)
                        {
                            selectQueue.Enqueue(selectQueue.Dequeue());
                        }
                        selectQueue.Dequeue();
                        selectQueue.Enqueue(newFamilyModel);
                    }
                    
                }                
            });
        }

        public ICommand UnioCommand
        {
            get => new DelegateCommand(() =>
            {
                UpadateUIDatabase();
            });
        }

        public void UpadateUIDatabase()
        {
            //更新当前DataGrid数据源
            InitDataGrid();

            curFamilysDataForward.Clear();//清空前进栈
            curFamilyIdsDataForward.Clear();
            if (ForwardIsNotNull == true)
            {
                ForwardIsNotNull = false;
            }
            curFamilysDataBack.Clear();//清空后退栈
            curFamilyIdsDataBack.Clear();
            if (BackIsNotNull == true)
            {
                BackIsNotNull = false;
            }
            selectQueue.Clear();//清空选择队列
            SelectQueueIsNotNull = false;
        }

        public ICommand ApplyCommand
        {
            get => new DelegateCommand(async () =>
            {
                SysCacheSubEdit.Instance.EditedFamilyNameList.Clear();
                SysCacheSubEdit.Instance.EditedSystemFamilyTypeNameList.Clear();
                SysCacheSubEdit.Instance.EditedFamilyTypeNameList.Clear();
                List<string> tempLIstForEditFamily = new List<string>();

                bool isSuccessEdit = false;

                //【1】更新修改项至Revit
                foreach (DatagridModel editedFamily in familysData)
                {                   
                    if (editedFamily.OldFamilyName != editedFamily.FamilyName)//载入族修改族名
                    {
                        if (!tempLIstForEditFamily.Contains(editedFamily.FamilyName))
                        {
                            tempLIstForEditFamily.Add(editedFamily.FamilyName);
                            SysCacheSubEdit.Instance.EditedFamilyNameList.Add(new DatagridModel
                            {
                                CategoryName = editedFamily.CategoryName,
                                FamilyName = editedFamily.FamilyName,
                                OldFamilyName = editedFamily.OldFamilyName,
                                FamilyTypeName = editedFamily.FamilyTypeName,
                                OldFamilyTypeName = editedFamily.OldFamilyTypeName,
                                FamilyTypeEnum = editedFamily.FamilyTypeEnum,
                                Id = editedFamily.Id
                            });
                            editedFamily.IsEdit = false;
                            editedFamily.OldFamilyName = editedFamily.FamilyName;
                        }                                              
                    }
                    if (editedFamily.OldFamilyTypeName != editedFamily.FamilyTypeName)
                    {
                        if (editedFamily.FamilyTypeEnum == FamilyType.SystemFamily)//系统族修改族类型名
                        {
                            SysCacheSubEdit.Instance.EditedSystemFamilyTypeNameList.Add(new DatagridModel
                            {
                                CategoryName = editedFamily.CategoryName,
                                FamilyName = editedFamily.FamilyName,
                                OldFamilyName = editedFamily.OldFamilyName,
                                FamilyTypeName = editedFamily.FamilyTypeName,
                                OldFamilyTypeName = editedFamily.OldFamilyTypeName,
                                FamilyTypeEnum = editedFamily.FamilyTypeEnum,
                                Id = editedFamily.Id
                            });

                            editedFamily.IsEdit = false;
                            editedFamily.OldFamilyTypeName = editedFamily.FamilyTypeName;
                            continue;
                        }
                        SysCacheSubEdit.Instance.EditedFamilyTypeNameList.Add(new DatagridModel
                        {
                            CategoryName = editedFamily.CategoryName,
                            FamilyName = editedFamily.FamilyName,
                            OldFamilyName = editedFamily.OldFamilyName,
                            FamilyTypeName = editedFamily.FamilyTypeName,
                            OldFamilyTypeName = editedFamily.OldFamilyTypeName,
                            FamilyTypeEnum = editedFamily.FamilyTypeEnum,
                            Id = editedFamily.Id
                        });//载入族修改族类型名
                        editedFamily.IsEdit = false;
                        editedFamily.OldFamilyTypeName = editedFamily.FamilyTypeName;
                    }
                }

                //SysCacheSubEdit.Instance.EditFamilyNameToRevitEvent.Raise();
                //SysCacheSubEdit.Instance.EditFamilyNameToRevitEventHandler.Execute(SysCacheSubEdit.Instance.ExternEventExecuteApp);
                //在非模态对话框打开的状态下，调用Transaction必须通过Idling或者ExternalEvent API进行相关代码调用——故不能通过_EventHandler.Execute()执行
                await RevitTask.RunAsync((uiApp) =>
                {
                    List<Element> familyList = new List<Element>();
                    Document doc = uiApp.ActiveUIDocument.Document;
                    Transaction trans = new Transaction(doc, "修改族名");
                    try
                    {                       
                        if (SysCacheSubEdit.Instance.EditedSystemFamilyTypeNameList.Count > 0)
                        {
                            familyList.Clear();
                            ElementClassFilter filter = new ElementClassFilter(typeof(FamilySymbol), true);
                            IList<Element> listSystemFamilyType = new FilteredElementCollector(doc).WhereElementIsElementType().WherePasses(filter).ToElements();

                            foreach (var element in listSystemFamilyType)
                            {
                                if (element.Category == null)
                                {
                                    continue;
                                }
                                if (element.Category.CategoryType == CategoryType.Model && !familyList.Contains(element))
                                {
                                    familyList.Add(element);
                                }
                            }
                        }

                        foreach (DatagridModel curFamilyModel in SysCacheSubEdit.Instance.EditedFamilyNameList)
                        {
                            Element eleFamily = new FilteredElementCollector(doc).OfClass(typeof(Family)).FirstOrDefault(x => x.Name == curFamilyModel.OldFamilyName);
                            try
                            {
                                trans.Start();
                                eleFamily.Name = curFamilyModel.FamilyName;
                                trans.Commit();
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("封装异常",ex);                                
                            }

                        }

                        foreach (DatagridModel curFamilyModel in SysCacheSubEdit.Instance.EditedFamilyTypeNameList)
                        {
                            Element eleFamily = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).FirstOrDefault(x => x.Name == curFamilyModel.OldFamilyTypeName);
                            try
                            {
                                trans.Start();
                                eleFamily.Name = curFamilyModel.FamilyTypeName;
                                trans.Commit();
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("封装异常", ex);
                            }
                        }

                        foreach (DatagridModel curFamilyModel in SysCacheSubEdit.Instance.EditedSystemFamilyTypeNameList)
                        {

                            Element eleFamily = familyList.FirstOrDefault(x => x.Name == curFamilyModel.OldFamilyTypeName);
                            try
                            {
                                trans.Start();
                                eleFamily.Name = curFamilyModel.FamilyTypeName;
                                trans.Commit();
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("封装异常", ex);
                            }
                        }

                        isSuccessEdit = true;
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.Info($"载入出错{doc},{ex}");
                        Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
                    }                                      
                });

                if (isSuccessEdit)
                {
                    MessageBox.Show("族名编辑成功","提示");
                }
                else
                {
                    MessageBox.Show("族名编辑失败", "提示");
                }
                //【2】更新UI界面数据                               
                UpadateUIDatabase();
            });
        }

        #endregion


        #region 查找替换、前缀后缀

        private string _SrerchText;
        public string SrerchText
        {
            get { return _SrerchText; }
            set { SetProperty(ref _SrerchText, value, () => { selectQueue.Clear(); SelectQueueIsNotNull = false; }); }
        }

        private string _ReplaceText;
        public string ReplaceText
        {
            get { return _ReplaceText; }
            set { SetProperty(ref _ReplaceText, value); }
        }

        private string _PrefixText;
        public string PrefixText
        {
            get { return _PrefixText; }
            set { SetProperty(ref _PrefixText, value); }
        }

        private string _SuffixText;
        public string SuffixText
        {
            get { return _SuffixText; }
            set { SetProperty(ref _SuffixText, value); }
        }

        private bool _SelectQueueIsNotNull = false;
        public bool SelectQueueIsNotNull
        {
            get { return _SelectQueueIsNotNull; }
            set { SetProperty(ref _SelectQueueIsNotNull, value); }
        }


        public ICommand ClearCommand
        {
            get => new DelegateCommand(() =>
            {
                SrerchText = null;
                ReplaceText = null;
                PrefixText = null;
                SuffixText = null;
                selectQueue.Clear();//清空选择队列
                SelectQueueIsNotNull = false;

            });
        }

        public Queue<DatagridModel> selectQueue = new Queue<DatagridModel>();
        public ICommand FindNextCommand
        {
            get => new DelegateCommand(() =>
            {
                if (SrerchText == null || SrerchText == "")
                {
                    return;
                }

                if (selectQueue.Count <= 0)
                {
                    GetSelectItem();
                    if (selectQueue.Count <= 0)
                    {
                        return;
                    }
                }

                foreach (var curFamilyModel in CurFamilysData)//去除干扰的选择项
                {
                    if (curFamilyModel.IsSelected == false)
                    {
                        continue;
                    }
                    var result = selectQueue.FirstOrDefault<DatagridModel>(x => x.Id == curFamilyModel.Id);
                    if (result == null)
                    {
                        curFamilyModel.IsSelected = false;
                    }
                }

                DatagridModel curFamilyItem;
                var selectItems = from selectItem in selectQueue
                                  where selectItem.IsSelected == true
                                  select selectItem;

                if (selectItems.Count<DatagridModel>() > 1)//多个选择项——复位，然后队首为选择项
                {
                    for (int i = 0; i < selectQueue.Count; i++)
                    {
                        curFamilyItem = selectQueue.Dequeue();
                        curFamilyItem.IsSelected = false;
                        selectQueue.Enqueue(curFamilyItem);
                    }
                    curFamilyItem = selectQueue.Dequeue();
                    curFamilyItem.IsSelected = true;
                    selectQueue.Enqueue(curFamilyItem);
                    return;
                }

                if (selectItems.Count<DatagridModel>() == 0)//无选择项——队首为选择项
                {
                    curFamilyItem = selectQueue.Dequeue();
                    curFamilyItem.IsSelected = true;
                    selectQueue.Enqueue(curFamilyItem);
                    return;
                }

                if (selectItems.Count<DatagridModel>() == 1)//一个选择项——调整到队首为选择项，然后将下一项移到队首为选择项
                {
                    while (selectQueue.Peek().IsSelected == false)
                    {
                        selectQueue.Enqueue(selectQueue.Dequeue());
                    }

                    curFamilyItem = selectQueue.Dequeue();//出队
                    curFamilyItem.IsSelected = false;
                    selectQueue.Enqueue(curFamilyItem);//再入队

                    curFamilyItem = selectQueue.Dequeue();
                    curFamilyItem.IsSelected = true;
                    selectQueue.Enqueue(curFamilyItem);
                    return;
                }
            });
        }

        public ICommand FindAllCommand
        {
            get => new DelegateCommand(() =>
            {
                foreach (var curFamilyModel in CurFamilysData)
                {
                    curFamilyModel.IsSelected = false;
                }

                if (SrerchText == null || SrerchText == "")
                {
                    return;
                }

                if (selectQueue.Count <= 0)
                {
                    GetSelectItem();
                    if (selectQueue.Count <= 0)
                    {
                        return;
                    }
                }
                for (int i = 0; i < selectQueue.Count; i++)//选择所有项
                {
                    DatagridModel curFamilyItem = selectQueue.Dequeue();//出队
                    curFamilyItem.IsSelected = true;
                    selectQueue.Enqueue(curFamilyItem);//再入队
                }
            });
        }

        public ICommand ReplaceSelectCommand
        {
            get => new DelegateCommand(() =>
            {                
                DatagridModel selectFamilyItem;
                string replace;
                if (ReplaceText == null || ReplaceText == "")
                {
                    replace = "";
                }
                else
                {
                    replace = ReplaceText.Trim();
                }

                IList<DatagridModel> oldFamilysData = new List<DatagridModel>();
                IList<int> oldFamilyIdsData = new List<int>();//存储此轮已经修改过的族Id
                string newName;

                int queueCount = selectQueue.Count;
                for (int i = 0; i < queueCount; i++)//只需考虑重名报错即可
                {
                    selectFamilyItem = selectQueue.Dequeue();

                    if (selectFamilyItem.IsSelected == false)
                    {
                        selectQueue.Enqueue(selectFamilyItem);
                        continue;
                    }
                   
                    if (CurEditName == EditName.FamilyName)
                    {
                        newName = selectFamilyItem.FamilyName.Replace(SrerchText.Trim(), replace);
                        bool IsEdit = EditCurFamilyName(newName, selectFamilyItem, oldFamilysData, oldFamilyIdsData);
                        if (IsEdit == false)
                        {
                            selectQueue.Enqueue(selectFamilyItem);
                        }
                    }
                    if (CurEditName == EditName.TypeName)
                    {
                        newName = selectFamilyItem.FamilyTypeName.Replace(SrerchText.Trim(), replace);
                        bool IsEdit = EditCurFamilyTypeName(newName, selectFamilyItem, oldFamilysData, oldFamilyIdsData);
                        if (IsEdit == false)
                        {
                            selectQueue.Enqueue(selectFamilyItem);
                        }
                    }
                }
                if (selectQueue.Count <= 0)
                {
                    SelectQueueIsNotNull = true;
                }
                EditInStack(oldFamilysData, oldFamilyIdsData);
            });
        }

        public ICommand PrefixSelectCommand
        {
            get => new DelegateCommand(() =>
            {
                if (PrefixText == null || PrefixText == "")
                {
                    return;
                }

                if (PrefixText.Trim() == null || PrefixText.Trim() == "")
                {
                    return;
                }

                IList<DatagridModel> oldFamilysData = new List<DatagridModel>();
                IList<int> oldFamilyIdsData = new List<int>();//存储此轮已经修改过的族Id
                string newName;

                foreach (var curFamilyModel in CurFamilysData)
                {
                    if (curFamilyModel.IsSelected == false)
                    {
                        continue;
                    }
                    if (CurEditName == EditName.FamilyName)//只需考虑重名报错即可
                    {
                        newName = String.Concat(PrefixText.Trim(),curFamilyModel.FamilyName);
                        EditCurFamilyName(newName, curFamilyModel, oldFamilysData, oldFamilyIdsData);
                    }
                    else if (CurEditName == EditName.TypeName)//只需考虑重名报错即可
                    {
                        newName = String.Concat(PrefixText.Trim(), curFamilyModel.FamilyTypeName);
                        EditCurFamilyTypeName(newName, curFamilyModel, oldFamilysData, oldFamilyIdsData);
                    }
                }
                EditInStack(oldFamilysData, oldFamilyIdsData);
            });
        }

        public ICommand PrefixAllCommand
        {
            get => new DelegateCommand(() =>
            {
                if (PrefixText == null || PrefixText == "")
                {
                    return;
                }
                if (PrefixText.Trim() == null || PrefixText.Trim() == "")
                {
                    return;
                }

                IList<DatagridModel> oldFamilysData = new List<DatagridModel>();
                IList<int> oldFamilyIdsData = new List<int>();//存储此轮已经修改过的族Id
                string newName;

                foreach (var curFamilyModel in CurFamilysData)
                {
                    if (CurEditName == EditName.FamilyName)//只需考虑重名报错即可
                    {
                        newName = String.Concat(PrefixText.Trim(), curFamilyModel.FamilyName);
                        EditCurFamilyName(newName, curFamilyModel, oldFamilysData, oldFamilyIdsData);
                    }
                    else if (CurEditName == EditName.TypeName)//只需考虑重名报错即可
                    {
                        newName = String.Concat(PrefixText.Trim(), curFamilyModel.FamilyTypeName);
                        EditCurFamilyTypeName(newName, curFamilyModel, oldFamilysData, oldFamilyIdsData);
                    }
                }
                EditInStack(oldFamilysData, oldFamilyIdsData);
            });
        }

        public ICommand SuffixSelectCommand
        {
            get => new DelegateCommand(() =>
            {
                if (SuffixText == null || SuffixText == "")
                {
                    return;
                }
                if (SuffixText.Trim() == null || SuffixText.Trim() == "")
                {
                    return;
                }

                IList<DatagridModel> oldFamilysData = new List<DatagridModel>();
                IList<int> oldFamilyIdsData = new List<int>();//存储此轮已经修改过的族Id
                string newName;

                foreach (var curFamilyModel in CurFamilysData)
                {
                    if (curFamilyModel.IsSelected == false)
                    {
                        continue;
                    }
                    if (CurEditName == EditName.FamilyName)//只需考虑重名报错即可
                    {
                        newName = String.Concat(curFamilyModel.FamilyName, SuffixText.Trim());
                        EditCurFamilyName(newName, curFamilyModel, oldFamilysData, oldFamilyIdsData);
                    }
                    else if (CurEditName == EditName.TypeName)//只需考虑重名报错即可
                    {
                        newName = String.Concat(curFamilyModel.FamilyTypeName, SuffixText.Trim());
                        EditCurFamilyTypeName(newName, curFamilyModel, oldFamilysData, oldFamilyIdsData);
                    }
                }
                EditInStack(oldFamilysData, oldFamilyIdsData);
            });
        }

        public ICommand SuffixAllCommand
        {
            get => new DelegateCommand(() =>
            {
                if (SuffixText == null || SuffixText == "")
                {
                    return;
                }
                if (SuffixText.Trim() == null || SuffixText.Trim() == "")
                {
                    return;
                }

                IList<DatagridModel> oldFamilysData = new List<DatagridModel>();
                IList<int> oldFamilyIdsData = new List<int>();//存储此轮已经修改过的族Id
                string newName;

                foreach (var curFamilyModel in CurFamilysData)
                {
                    if (CurEditName == EditName.FamilyName)//只需考虑重名报错即可
                    {
                        newName = String.Concat(curFamilyModel.FamilyName, SuffixText.Trim());
                        EditCurFamilyName(newName, curFamilyModel, oldFamilysData, oldFamilyIdsData);
                    }
                    else if (CurEditName == EditName.TypeName)//只需考虑重名报错即可
                    {
                        newName = String.Concat(curFamilyModel.FamilyTypeName, SuffixText.Trim());
                        EditCurFamilyTypeName(newName, curFamilyModel, oldFamilysData, oldFamilyIdsData);
                    }
                }
                EditInStack(oldFamilysData, oldFamilyIdsData);
            });
        }


        #endregion


        #region 辅助函数

        // 【注意】访问修饰符需使用public

        public bool EditCurFamilyName(string newNameValue, DatagridModel curDataModel, IList<DatagridModel> oldFamilysData, IList<int> oldFamilyIdsData)
        {

            var curFamilyItems = from curFamilyItem in CurFamilysData
                                 where curFamilyItem.CategoryName == curDataModel.CategoryName && curFamilyItem.FamilyName == newNameValue
                                 select curFamilyItem;
            foreach (int editId in oldFamilyIdsData)//去除此轮已编辑族的影响——会导致重名报错
            {
                curFamilyItems = from curFamilyItem in curFamilyItems
                                 where curFamilyItem.Id != editId
                                 select curFamilyItem;
            }

            if (curFamilyItems.Count() > 0)//有重名情况
            {
                foreach (var curFamily in curFamilyItems)
                {
                    curFamily.IsReportError = true;
                }
                theSameFamilyNames.Add(newNameValue);
                MessageBox.Show($"存在重名的情况 \n 类别：{ curDataModel.CategoryName } \n 族名：{ curDataModel.FamilyName }", "错误提示");
                return false;
            }
            else//无重名情况
            {
                oldFamilysData.Add(new DatagridModel()
                {
                    CategoryName = curDataModel.CategoryName,
                    FamilyName = curDataModel.FamilyName,
                    FamilyTypeName = curDataModel.FamilyTypeName,
                    IsSelected = curDataModel.IsSelected,
                    Id = curDataModel.Id,
                    IsEdit = curDataModel.IsEdit
                });//引用类型：深拷贝                                   
                oldFamilyIdsData.Add(curDataModel.Id);

                curDataModel.FamilyName = newNameValue;
                curDataModel.IsEdit = true;
                curDataModel.IsSelected = false;
                return true;
            }
        }

        public bool EditCurFamilyTypeName(string newNameValue, DatagridModel curDataModel, IList<DatagridModel> oldFamilysData, IList<int> oldFamilyIdsData)
        {
            var curFamilyItems = from curFamilyItem in CurFamilysData
                                 where curFamilyItem.CategoryName == curDataModel.CategoryName && curFamilyItem.FamilyName == curDataModel.FamilyName && curFamilyItem.FamilyTypeName == newNameValue
                                 select curFamilyItem;
            foreach (int editId in oldFamilyIdsData)//去除此轮已编辑族的影响——会导致重名报错
            {
                curFamilyItems = from curFamilyItem in curFamilyItems
                                 where curFamilyItem.Id != editId
                                 select curFamilyItem;
            }

            if (curFamilyItems.Count() > 0)//有重名情况
            {
                foreach (var curFamily in curFamilyItems)
                {
                    curFamily.IsReportError = true;
                }
                theSameFamilyTypeNames.Add(newNameValue);
                MessageBox.Show($"存在重名的情况 \n 类别：{ curDataModel.CategoryName } \n 族名：{ curDataModel.FamilyName } \n 族类型名：{ curDataModel.FamilyTypeName }", "错误提示");
                return false;
            }
            else//无重名情况
            {
                oldFamilysData.Add(new DatagridModel()
                {
                    CategoryName = curDataModel.CategoryName,
                    FamilyName = curDataModel.FamilyName,
                    FamilyTypeName = curDataModel.FamilyTypeName,
                    IsSelected = curDataModel.IsSelected,
                    Id = curDataModel.Id,
                    IsEdit = curDataModel.IsEdit
                });//引用类型：深拷贝                                   
                oldFamilyIdsData.Add(curDataModel.Id);

                curDataModel.FamilyTypeName = newNameValue;
                curDataModel.IsEdit = true;
                curDataModel.IsSelected = false;
                return true;
            }
        }

        public void EditInStack(IList<DatagridModel> oldFamilysData, IList<int> oldFamilyIdsData)
        {
            if (oldFamilysData.Count > 0)
            {
                curFamilysDataBack.Push(oldFamilysData);//入栈（后退）
                curFamilyIdsDataBack.Push(oldFamilyIdsData);
                if (BackIsNotNull == false)
                {
                    BackIsNotNull = true;
                }

                curFamilysDataForward.Clear();//清空前进栈
                curFamilyIdsDataForward.Clear();
                if (ForwardIsNotNull == true)
                {
                    ForwardIsNotNull = false;
                }
            }
        }


        // 用于初始化查找队列：selectQueue
        public void GetSelectItem()
        {
            if (SrerchText.Trim()=="" || SrerchText.Trim()==null)
            {
                return;
            }
            foreach (var curFamilyModel in CurFamilysData)
            {
                if (CurEditName == EditName.FamilyName && curFamilyModel.FamilyName.Contains(SrerchText.Trim()))//族名
                {
                    selectQueue.Enqueue(curFamilyModel);
                }
                else if (CurEditName == EditName.TypeName && curFamilyModel.FamilyTypeName.Contains(SrerchText.Trim()))//族类型名
                {
                    selectQueue.Enqueue(curFamilyModel);
                }
            }
            if (selectQueue.Count > 0)
            {
                SelectQueueIsNotNull = true;
            }
        }

        public void InitDataGrid()
        {
            CurFamilysData.Clear();
            familysData.Clear();

            int noteId = 0;

            foreach (var curCategory in AllNodes)
            {
                var familyElements = from element in SysCacheSubEdit.Instance.FamilySourceList
                                     where element.Category.Name == curCategory.Name
                                     select element;
                IList<string> FamilyNameList = new List<string>();
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
                    foreach (var familyElement in familyElements)
                    {
                        string currentFamilyName = (familyElement as ElementType).FamilyName;
                        if (currentFamilyName == familyName)
                        {
                            DatagridModel curDatagridModel = new DatagridModel()
                            {
                                CategoryName = curCategory.Name,
                                FamilyName = familyName,
                                OldFamilyName = familyName,
                                FamilyTypeName = familyElement.Name,
                                OldFamilyTypeName = familyElement.Name,
                                FamilyTypeEnum = CurFamilyType,
                                Id = noteId
                            };
                            noteId++;
                            familysData.Add(curDatagridModel);
                        }
                    }
                }
            }
            foreach (var curCategory in SelectNodes)
            {
                foreach (var curFamilyData in familysData)
                {
                    if (curFamilyData.CategoryName == curCategory.Name)
                    {
                        CurFamilysData.Add(curFamilyData);
                    }
                }
            }
            //CurFamilysData= familysData;【注意：不能直接将集合引用赋值给另外一个集合引用（浅拷贝：两个集合引用会指向同一内存空间）】
            //                            【注意：区分列表元素类型——值类型、引用类型】
        }

        public void InitTree()
        {
            RootTreeModels.Clear();
            SelectNodes.Clear();
            AllNodes.Clear();

            CatagoryTreeNode oneTreeModel = new CatagoryTreeNode()
            {
                Name = "所有类别",
                IsCheck = true,
                Parent = null
            };
            foreach (var curfamilyType in SysCacheSubEdit.Instance.FamilySourceList)
            {
                var result = oneTreeModel.TreeModels.FirstOrDefault(x => x.Name == curfamilyType.Category.Name);
                if (result == null)
                {
                    CatagoryTreeNode childTreeModel = new CatagoryTreeNode()
                    {
                        Name = curfamilyType.Category.Name,
                        IsCheck = true,
                        Parent = oneTreeModel
                    };
                    oneTreeModel.TreeModels.Add(childTreeModel);
                    SelectNodes.Add(childTreeModel);
                    AllNodes.Add(childTreeModel);
                }
            }
            RootTreeModels.Add(oneTreeModel);
        }

        public void SelectAll(CatagoryTreeNode treeModel)
        {
            try
            {
                //统计当前已勾选的子节点数
                int count = 0;
                //临时存储当前未勾选的子节点
                List<CatagoryTreeNode> lists = new List<CatagoryTreeNode>();

                foreach (var item in treeModel.TreeModels)
                {
                    if (item.IsCheck == true)
                    {
                        count++;
                    }
                    else
                    {
                        lists.Add(item);
                    }
                }

                if (count == treeModel.TreeModels.Count) //相等表示之前是全选状态，现在需取消全选
                {
                    treeModel.IsCheck = false;
                    foreach (var item in treeModel.TreeModels)
                    {
                        var result = SelectNodes.FirstOrDefault(x => x.Name == item.Name);
                        if (result != null)
                        {
                            SelectNodes.Remove(result);
                            item.IsCheck = false;
                        }
                    }
                }
                else //反之全选
                {
                    treeModel.IsCheck = true;
                    foreach (var item in lists)
                    {
                        var result = SelectNodes.FirstOrDefault(x => x.Name == item.Name);
                        if (result == null)
                        {
                            SelectNodes.Add(item);
                            item.IsCheck = true;
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

        public void Select(CatagoryTreeNode treeModel)
        {
            try
            {
                var result = SelectNodes.FirstOrDefault(x => x.Name == treeModel.Name);

                var value = treeModel.Parent;

                if (result != null)//表示之前点击过，现在是取消选择
                {
                    var list = SelectNodes.Where(x => x.Parent == value).ToList();
                    if (list.Count == value.TreeModels.Count)//数量相等表示之前其父节点的所有子节点已全选，现在需取消全选状态
                    {
                        value.IsCheck = null;
                    }

                    SelectNodes.Remove(result);
                    treeModel.IsCheck = false;
                }
                else
                {
                    SelectNodes.Add(treeModel);
                    var list = SelectNodes.Where(x => x.Parent == value).ToList();
                    if (list.Count == value.TreeModels.Count)//数量相等表示当前其父节点的所有子节点已经全选
                    {
                        value.IsCheck = true;
                    }
                    else
                    {
                        value.IsCheck = null;
                    }
                    treeModel.IsCheck = true;
                }

            }
            catch (Exception ex)
            {
                Logger.Instance.Info($"报错信息,{ex}");
                Process.Start(Path.Combine(QiShiCore.WorkSpace.Dir, "Log"));
            }
        } 
        #endregion
    }
}
