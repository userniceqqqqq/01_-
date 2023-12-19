using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyManager.MainModule.SubEdit
{
    public class DatagridModel : BindableBase
    {
        private string _CategoryName;
        public string CategoryName
        {
            get { return _CategoryName; }
            set { SetProperty(ref _CategoryName, value); }
        }

        private string _FamilyName;
        public string FamilyName
        {
            get { return _FamilyName; }
            set { SetProperty(ref _FamilyName, value); }
        }

        private string _FamilyTypeName;
        public string FamilyTypeName
        {
            get { return _FamilyTypeName; }
            set { SetProperty(ref _FamilyTypeName, value); }
        }

        private bool _IsSelected = false;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { SetProperty(ref _IsSelected, value); }

        }

        private bool _IsEdit = false;
        public bool IsEdit
        {
            get { return _IsEdit; }
            set { SetProperty(ref _IsEdit, value); }

        }

        private bool _IsReportError = false;
        public bool IsReportError
        {
            get { return _IsReportError; }
            set { SetProperty(ref _IsReportError, value); }

        }

        private int _Id ;
        public int Id
        {
            get { return _Id; }
            set { SetProperty(ref _Id, value); }

        }



        private FamilyType _FamilyTypeEnum;
        public FamilyType FamilyTypeEnum
        {
            get { return _FamilyTypeEnum; }
            set { SetProperty(ref _FamilyTypeEnum, value); }
        }

        private string _OldFamilyName;
        public string OldFamilyName
        {
            get { return _OldFamilyName; }
            set { SetProperty(ref _OldFamilyName, value); }
        }


        private string _OldFamilyTypeName;
        public string OldFamilyTypeName
        {
            get { return _OldFamilyTypeName; }
            set { SetProperty(ref _OldFamilyTypeName, value); }
        }



    }
}
