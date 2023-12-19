using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyManager.MainModule.SubEdit
{
    public class SysCacheSubEdit
    {
        /// <summary>
        /// 单例模式【注意：会一直存在（不随窗口关闭而释放）】
        /// </summary>
        private SysCacheSubEdit()
        {

        }
        private static SysCacheSubEdit _instance;

        public static SysCacheSubEdit Instance
        {
            get
            {
                if (ReferenceEquals(null, _instance))
                {
                    _instance = new SysCacheSubEdit();
                }
                return _instance;
            }
        }

        public UIApplication ExternEventExecuteApp { get; set; }


        public LoadFamilySource LoadFamilySourceEventHandler { get; set; }
        public ExternalEvent LoadFamilySourceEvent { get; set; }  //建立外部事件       
        public IList<Element> FamilySourceList { get; set; } = new List<Element>();


        public IList<DatagridModel> EditedFamilyNameList { get; set; } = new List<DatagridModel>();
        public IList<DatagridModel> EditedFamilyTypeNameList { get; set; } = new List<DatagridModel>();
        public IList<DatagridModel> EditedSystemFamilyTypeNameList { get; set; } = new List<DatagridModel>();

    }
}
