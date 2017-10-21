using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UncertainEventStreams.Preprocessing.Tasks
{
    public abstract class AbstractTask
    {
        public abstract string Name { get; }

        public void Run()
        {
            var sw = Stopwatch.StartNew();
            try
            {
                Console.WriteLine("Started task: {0}", Name);
                RunSpecific();
                Console.WriteLine("Ended task: {0}, total duration: {1}", Name, sw.Elapsed);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed performaing task: {0}, total duration: {1}, ex: {2}", Name, sw.Elapsed, ex);
            }            
        }

        protected abstract void RunSpecific();
    }
}
