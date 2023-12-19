using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyManager.MainModule.SubEdit
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

    public enum FamilyType
    {
        [Description("系统族")]
        SystemFamily,
        [Description("载入族")]
        LoadFamily
    }

    public enum EditName
    {
        [Description("族名")]
        FamilyName,
        [Description("族类型名")]
        TypeName
    }
}
