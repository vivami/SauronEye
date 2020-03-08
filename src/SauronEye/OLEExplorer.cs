using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenMcdf;

namespace SauronEye {
    class OLEExplorer {

        public OLEExplorer() { }

        public bool CheckForVBAMacros(string path) {
            try {
                CompoundFile cf = new CompoundFile(path);
                // This line throws an exception if there is no _VBA_PROJECT_CUR/VBA/dir stream in the OLE.
                // _VBA_PROJECT_CUR/VBA/dir has to be in an OLE if it contains a VBA macro
                // https://docs.microsoft.com/en-us/openspecs/office_file_formats/ms-ovba/005bffd7-cd96-4f25-b75f-54433a646b88 
                if (path.ToLower().EndsWith(".xls")) {
                    CFStream dirStream = cf.RootStorage.GetStorage("_VBA_PROJECT_CUR").GetStorage("VBA").GetStream("dir");
                } else {
                    // .doc
                    CFStream dirStream = cf.RootStorage.GetStorage("Macros").GetStorage("VBA").GetStream("dir");
                }
                return true;
            } catch (Exception) {
                return false;
            }
        }
    }
}
