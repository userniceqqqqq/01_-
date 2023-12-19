using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FamilyManager.MainModule.SubExport
{
    public class SysCacheSubExport
    {
        /// <summary>
        /// 单例模式【注意：会一直存在（不随窗口关闭而释放）】
        /// </summary>
        private SysCacheSubExport()
        {

        }
        private static SysCacheSubExport _instance;

        public static SysCacheSubExport Instance
        {
            get
            {
                if (ReferenceEquals(null, _instance))
                {
                    _instance = new SysCacheSubExport();
                }
                return _instance;
            }
        }

        public UIApplication ExternEventExecuteApp { get; set; }


        public LoadFamilyTreeSource LoadFamilyTreeSourceEventHandler { get; set; }
        public ExternalEvent LoadFamilyTreeSourceEvent { get; set; }  //建立外部事件       
        public IList<Element> FamilyTreeSourceList { get; set; } = new List<Element>();
    }
}
