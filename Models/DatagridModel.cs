using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyManager.MainModule.SubExport
{
    public class DatagridModel : BindableBase
    {
        private bool? _IsCheck = true;
        public bool? IsCheck
        {
            get { return _IsCheck; }
            set { SetProperty(ref _IsCheck, value); }
        }

        private bool _IsSelect = false;
        public bool IsSelect
        {
            get { return _IsSelect; }
            set { SetProperty(ref _IsSelect, value); }
        }

        private string _Name;
        public string Name
        {
            get { return _Name; }
            set { SetProperty(ref _Name, value); }
        }

        private string _CatagoryName;
        public string CatagoryName
        {
            get { return _CatagoryName; }
            set { SetProperty(ref _CatagoryName, value); }
        }

        private string _FamilyName;
        public string FamilyName
        {
            get { return _FamilyName; }
            set { SetProperty(ref _FamilyName, value); }
        }

        private object _Folder;
        public object Folder
        {
            get { return _Folder; }
            set { SetProperty(ref _Folder, value); }           
        }
    }
}
