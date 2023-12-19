using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FamilyManager.MainModule.SubExport
{
    public class ExportSet
    {
        public IList<StorageFolder> SetStorageFolders { get; set; }

        public int SetMaxBackupCount { get; set; }

        public bool SetIsCatagoryAsFolder { get; set; }

        public bool SetIsSaveBackup { get; set; }

    }
}
