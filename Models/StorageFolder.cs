using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FamilyManager.MainModule.SubExport
{
    public class StorageFolder : BindableBase
    {
        public Guid GuidId { get; set; }

        private string _FolderName;
        public string FolderName
        {
            get { return _FolderName; }
            set { SetProperty(ref _FolderName, value); }
        }

        private string _FolderPath;
        public string FolderPath
        {
            get { return _FolderPath; }
            set { SetProperty(ref _FolderPath, value); }
        }

        private bool _IsSelect = false;
        public bool IsSelect
        {
            get { return _IsSelect; }
            set { SetProperty(ref _IsSelect, value); }
        }

        private Visibility _IsVisibility = Visibility.Visible;
        public Visibility IsVisibility
        {
            get { return _IsVisibility; }
            set { SetProperty(ref _IsVisibility, value); }
        }
    }
}
