using Prism.Mvvm;
using RevitStorage.StructuredStorage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FamilyManager.MainModule
{
    class TabModel : BindableBase
    {

        private string _Header;
        public string Header
        {
            get { return _Header; }
            set { SetProperty(ref _Header, value); }
        }

        private bool _IsPlaceholder = false;
        public bool IsPlaceholder
        {
            get { return _IsPlaceholder; }
            set { SetProperty(ref _IsPlaceholder, value); }
        }


        public Paging pagedTable = new Paging(); //创建Paging对象，用于调用分页函数
        public List<string> fileList = new List<string>(); //初始化族文件路径列表
        public List<FamilyObject> FamilyObjects = new List<FamilyObject>(); //初始化族模型列表

        public List<int> NumberOfRecord { get; set; } = new List<int>() { 10, 20, 30, 50, 100 };
        public ObservableCollection<FamilyObject> CurrentFamilyObjects { get; set; } = new ObservableCollection<FamilyObject>();


        private string _FilePath = null;
        public string FilePath
        {
            get { return _FilePath; }
            set { SetProperty(ref _FilePath, value); }
        }


        private string _Keyword = null;
        public string Keyword
        {
            get { return _Keyword; }
            set { SetProperty(ref _Keyword, value, OnKeywordValueChanged); }
        }


        private int _ComboIndex = 0;
        public int ComboIndex
        {
            get { return _ComboIndex; }
            set { SetProperty(ref _ComboIndex, value, OnComboIndexValueChanged); }
        }

        private string _CurrentPage = "当前显示：0/0";
        public string CurrentPage
        {
            get { return _CurrentPage; }
            set { SetProperty(ref _CurrentPage, value); }
        }

        private void OnKeywordValueChanged()
        {
            string seachWord = Keyword.Trim();//得到输入的关键词（.Trim：用于清除空格）
            GetFamilyObjects(fileList);//获取族模型列表

            if (string.IsNullOrWhiteSpace(seachWord)) //如果没有填，则直接初始化主窗口
            {
                InitPage();
                return;//结束该函数
            }
            else//如果填，则根据关键词筛选族模型
            {
                FamilyObjects = new List<FamilyObject>(FamilyObjects.FindAll(x => x.Name.Contains(seachWord)));//List对象.FindAll(用于定义目标元素应满足条件的委托);
            }
            if (FamilyObjects.Count > 0)//判断是否存在符合筛选条件的族模型
            {
                InitPage();
            }
            else
            {
                CurrentFamilyObjects.Clear();
                CurrentPage = "没有此族！";
            }
        }

        private void OnComboIndexValueChanged()
        {
            List<FamilyObject> currentFamilyObjects = pagedTable.First(FamilyObjects,NumberOfRecord[ComboIndex]);//初始化首页的族模型显示
            if (currentFamilyObjects.Count > 0)
            {
                foreach (var familyObject in currentFamilyObjects)
                {
                    CurrentFamilyObjects.Add(familyObject);
                }
            }
           CurrentPage = PageNumberDisplay();//展示当前页面显示状态（方法封装）
        }


        #region 功能辅助函数

        /// <summary>
        /// 通过族文件路径列表获取族模型列表
        /// </summary>
        /// <param name="fileList"></param>
        /// <returns></returns>
        public void GetFamilyObjects(List<string> fileList)
        {
            FamilyObjects.Clear();//清空之前的族模型列表
            foreach (var item in fileList)  //遍历每一个族文件
            {
                //通过不打开族的方式获取族的预览图片
                Storage storage = new Storage(item);
                BitmapSource bms = GetImageStream(storage.ThumbnailImage.GetPreviewAsImage());
                //添加每一个族模型至列表中
                FamilyObjects.Add(new FamilyObject()
                {
                    //【原路径】E:\课程录制\Revit二次开发进阶课程\测试文件\族文件\机械设备\多联机 - 室内机 - 双向气流 - 天花板嵌入式.rfa
                    //【不具有扩展名的文件名~Path.GetFileNameWithoutExtension】多联机 - 室内机 - 双向气流 - 天花板嵌入式
                    //【具有扩展名的文件名~Path.GetFileName()】多联机 - 室内机 - 双向气流 - 天花板嵌入式.rfa
                    Name = Path.GetFileNameWithoutExtension(item),
                    Locatoin = item,
                    RfaImage = bms
                });
            }
        }

        /// <summary>
        /// 调用windows自身底层库——用于释放图片资源
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject(IntPtr value);

        /// <summary>
        /// 图片格式从System.Windows.Image转为BitmapSource
        /// 这里是为了变成Xmal中System.Windows.Controls.Image控件的source绑定
        /// 这里就用到了资源的释放
        /// </summary>
        /// <param name="myImage"></param>
        /// <returns></returns>
        public BitmapSource GetImageStream(System.Drawing.Image myImage)
        {
            var bitmap = new Bitmap(myImage);
            IntPtr bmpPt = bitmap.GetHbitmap();
            BitmapSource bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap
                (
                   bmpPt,
                   IntPtr.Zero,
                   Int32Rect.Empty,
                   BitmapSizeOptions.FromEmptyOptions()
                );
            bitmapSource.Freeze();
            DeleteObject(bmpPt);//释放图片资源
            return bitmapSource;
        }

        /// <summary>
        /// 根据当前myList初始化页面
        /// </summary>
        public void InitPage()
        {
            //【1】还原为默认页面
            CurrentFamilyObjects.Clear();  //清空当前族模型展示页面：ItemsControl
            pagedTable.PageIndex = 0; //设置初始页码：0                                                                  
            //【2】进行初始化页面
            List<FamilyObject> currentFamilyObjects = pagedTable.SetPaging(FamilyObjects, NumberOfRecord[ComboIndex]);//得到分页后当前页（此时为首页）的族模型列表
            if (currentFamilyObjects.Count > 0)
            {
                foreach (var familyObject in currentFamilyObjects)
                {
                    CurrentFamilyObjects.Add(familyObject);
                }
            }
            CurrentPage = PageNumberDisplay();//展示当前页面显示状态（方法封装）
        }

        /// <summary>
        /// 得到当前页面显示状态
        /// </summary>
        /// <returns>string Number of Records Showing</returns>
        public string PageNumberDisplay()
        {
            int PagedNumber = NumberOfRecord[ComboIndex] * (pagedTable.PageIndex + 1);
            if (PagedNumber > FamilyObjects.Count)
            {
                PagedNumber = FamilyObjects.Count;
            }
            return "当前显示：" + PagedNumber + "/" + FamilyObjects.Count;
        }

        #endregion

    }
}
