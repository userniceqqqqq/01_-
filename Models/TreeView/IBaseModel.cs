using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FamilyManager.MainModule.SubExport
{
    public enum TreeNodeType
    {
        [Description("类别")]
        Catagory,

        [Description("族")]
        Family,

        [Description("族类型")]
        FamilySymbol
    }


    public interface IBaseModel
    {
        string Name { get; set; }

        bool? IsCheck { get; set; }

        bool IsExpand { get; set; }

        Visibility NodeVisibility { get; set; }


        IBaseModel Parent { get; set; }

        ObservableCollection<IBaseModel> TreeNodes { get; set; }

        TreeNodeType NodeType { get; set; }


    }
}
