using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FamilyManager.MainModule.SubExport
{
    public class FamilySymbolTreeNode : BindableBase, IBaseModel
    {
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }

        private bool? _IsCheck = false;
        public bool? IsCheck //null表示未全选该节点的所有子节点
        {
            get { return _IsCheck; }
            set { SetProperty(ref _IsCheck, value); }
        }

        private bool _IsExpand = false;
        public bool IsExpand
        {
            get { return _IsExpand; }
            set { SetProperty(ref _IsExpand, value); }
        }

        private Visibility _NodeVisibility = Visibility.Visible;
        public Visibility NodeVisibility
        {
            get { return _NodeVisibility; }
            set { SetProperty(ref _NodeVisibility, value); }
        }


        public ObservableCollection<IBaseModel> TreeNodes { get; set; } = null;

        public TreeNodeType NodeType { get; set; } = TreeNodeType.FamilySymbol;

        public IBaseModel Parent { get; set; }
    }
}
