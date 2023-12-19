using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyManager.MainModule.SubExport
{
    public enum FamilySource
    {
        [Description("项目文档")]
        Document,
        [Description("激活视图")]
        ActiveView,
        [Description("选择集")]
        Selection
    }
}
