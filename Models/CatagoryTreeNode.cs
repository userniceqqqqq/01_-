using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyManager.MainModule.SubEdit
{
    class CatagoryTreeNode : BindableBase
    {
        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }

        private bool? _IsCheck;
        public bool? IsCheck //null表示未全选该节点的所有子节点
        {
            get { return _IsCheck; }
            set { SetProperty(ref _IsCheck, value); }
        }

        public CatagoryTreeNode Parent { get; set; }

        public ObservableCollection<CatagoryTreeNode> TreeModels { get; set; } = new ObservableCollection<CatagoryTreeNode>();
    }
}
