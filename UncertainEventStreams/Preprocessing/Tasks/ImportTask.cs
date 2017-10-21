using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncertainEventStreams.Preprocessing.Tasks
{
    public class ImportTask : AbstractTask
    {
        public override string Name { get { return "Import"; } }

        protected override void RunSpecific()
        {
            var busdata = new FileImport();
            busdata.Load(numFiles: 1);
        }
    }
}
