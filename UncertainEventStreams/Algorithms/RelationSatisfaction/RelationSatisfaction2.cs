using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncertainEventStreams.Algorithms.RelationSatisfaction;
using UncertainEventStreams.Entities;
using UncertainEventStreams.Events;
using UncertainEventStreams.Events.IntervalBased;
using MathNet.Numerics;

namespace UncertainEventStreams.Algorithms.RelationSatisfaction
{
    public class RelationSatisfaction2
    {
        public enum BranchCase { BothDeterministic, BothNonDeterministic, LeftNonDeterministic, RightNonDeterministic }

        #region Private members

        private class Iterator
        {
            private int _index;
            PointEvent[] _eventStream;

            public Iterator(PointEvent[] eventStream)
            {
                _eventStream = eventStream;
                _index = 0;
            }

            public PointEvent GetCurrent()
            {
                return _eventStream[_index];
            }

            public void Init()
            {
                _index = 0;
            }

            public void Next()
            {
                if(_index + 1 >= _eventStream.Length)
                {
                    throw new Exception("Not a valid increment");
                }

                _index++;
            }

            public DateTime NextDeterministic()
            {
                for (int i = _index + 1; i < _eventStream.Length; i++)
                {
                    if (_eventStream[i].DeterministicKnown)
                    {
                        return _eventStream[i].Timestamp.Value;
                    }
                }

                return DateTime.MaxValue;
            }

            public bool HasNext()
            {
                return _index + 1 < _eventStream.Length;
            }

            public bool IsLast()
            {
                return _index + 1 == _eventStream.Length;
            }
        }


        //private PointEvent[] _firstEventStream;
        //private PointEvent[] _secondEventStream;

        private class SequenceBounds
        {
            public DateTime Lower { get; set; }

            public DateTime Upper { get; set; }
        }

        #endregion

        #region helpers

        private LinkedList<TreeNode> ObtainLostSequence(TreeNode l)
        {
            var curr = l.Parent;
            var ls = new LinkedList<TreeNode>();
            while (!curr.Data.DeterministicKnown)
            {
                ls.AddFirst(curr);
                curr = curr.Parent;
            }

            return ls;
        }

        private SequenceBounds ObtainWindowBounds(TreeNode l)
        {
            return new SequenceBounds()
            {
                Lower = l.Parent.MaxPathDeterministicTimestamp,
                Upper = l.Data.Timestamp.Value
            };
        }

        private double Trace(TreeNode l)
        {
            if (l.IsRoot)
            {
                return 1;
            }
            var ls = ObtainLostSequence(l);
            if (ls.Count == 0) //No lost events
            {
                return 1;
            }


            var wb = ObtainWindowBounds(l);
            var m = ls.Count(x => x.Data.DataPointType == PointEvent.PointType.A);
            var n = ls.Count(x => x.Data.DataPointType == PointEvent.PointType.B);

            double a = 1;
            if (m > 0)
            {
                var startWindowA = ls.Where(x => x.Data.DataPointType == PointEvent.PointType.A).First().MaxPathDeterministicTimestamp;
                var endWindowA = l.Data.Timestamp.Value;
                a = Math.Pow((endWindowA - startWindowA).Ticks, m);
            }

            double b = 1;
            if (n > 0)
            {
                var endWindowB = ls.Where(x => x.Data.DataPointType == PointEvent.PointType.B).Last().MaxPathDeterministicTimestamp;
                var startWindowB = ls.Where(x => x.Data.DataPointType == PointEvent.PointType.B).First().MaxPathDeterministicTimestamp;
                b = Math.Pow((endWindowB - startWindowB).Ticks, m);
            }

            return Math.Pow((wb.Upper - wb.Lower).Ticks, m + n) / (SpecialFunctions.Factorial(14) * a * b);
        }

        private TreeNode Add(TreeNode l, PointEvent data)
        {
            if (l.IsRoot && l.Data == null)
            {
                l.Data = data;
                if (data.Timestamp.HasValue)
                {
                    l.MaxPathDeterministicTimestamp = data.Timestamp.Value;
                }

                return l;
            }
            else
            {
                return l.AddChild(data);
            }
        }


        private BranchCase GetBranchCase(PointEvent ei, PointEvent ej)
        {
            if (ei.DeterministicKnown && ej.DeterministicKnown)
            {
                return BranchCase.BothDeterministic;
            }
            else if (!ei.DeterministicKnown && !ej.DeterministicKnown)
            {
                return BranchCase.BothNonDeterministic;
            }
            else if (!ei.DeterministicKnown)
            {
                return BranchCase.LeftNonDeterministic;
            }
            else
            {
                return BranchCase.RightNonDeterministic;
            }
        }

        private DateTime NextDeterministic(int index, PointEvent[] stream)
        {
            for (int i = index; i < stream.Length; i++)
            {
                if (stream[i].DeterministicKnown)
                {
                    return stream[i].Timestamp.Value;
                }
            }

            return DateTime.MaxValue;
        }

        #endregion

        public double Run(SegmentedIntervalEvent first, SegmentedIntervalEvent second, int stop)
        {
            var firstEventStream = first.GetEventStream(stop).ToArray();
            var secondEventStream = second.GetEventStream(stop).ToArray();

            var iteratorA = new Iterator(firstEventStream);
            var iteratorB = new Iterator(secondEventStream);

            Console.WriteLine("Checking overlap between two segmented intervals.");
            Console.WriteLine("First interval boundaries: {0} - {1}",
                firstEventStream.Where(x => x.Timestamp.HasValue).Min(x => x.Timestamp),
                firstEventStream.Where(x => x.Timestamp.HasValue).Max(x => x.Timestamp));
            Console.WriteLine("Second interval boundaries: {0} - {1}",
                secondEventStream.Where(x => x.Timestamp.HasValue).Min(x => x.Timestamp),
                secondEventStream.Where(x => x.Timestamp.HasValue).Max(x => x.Timestamp));
            Console.WriteLine();
            Console.WriteLine();

            var l = TreeNode.GetRoot();

            var sw = Stopwatch.StartNew();
            Console.WriteLine("Algorithm started");
            var res = Merge(iteratorA, iteratorB, l, new List<PointEvent>(), new SequenceBounds());
            Console.WriteLine("Algorithm ended, running time: {0}", sw.Elapsed);
            Console.WriteLine();
            Console.WriteLine();

            //l.Traverse(x => Console.WriteLine(x));
            //var nodes = Get(l).ToList();
            var paths = GetPaths(new LinkedList<TreeNode>(new[] { l }));

            int k = 0;
            foreach (var path in paths)
            {
                Console.WriteLine("Path: {0}", k);
                Console.WriteLine("---------", k);
                foreach (var node in path)
                {
                    Console.WriteLine(node);
                }
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
                k++;
            }
            return res;
        }

        private IEnumerable<TreeNode> Get(TreeNode node)
        {
            yield return node;

            if (node.Children.Count() == 0)
            {
                yield break;
            }

            foreach (var child in node.Children)
            {
                foreach (var item in Get(child))
                {
                    yield return item;
                }
            }
        }

        private IEnumerable<LinkedList<TreeNode>> GetPaths(LinkedList<TreeNode> path)
        {
            if (path.Last.Value.Children.Count() == 0)
            {
                yield return path;
            }

            foreach (var item in path.Last.Value.Children)
            {
                var newPath = Clone(path);
                newPath.AddLast(item);

                foreach (var p in GetPaths(newPath))
                {
                    yield return p;
                }
            }
        }

        public LinkedList<TreeNode> Clone(LinkedList<TreeNode> list)
        {
            var newList = new LinkedList<TreeNode>();
            foreach (var item in list)
            {
                newList.AddLast(item);
            }

            return newList;
        }

        private double Merge(Iterator itA, Iterator itB, TreeNode r, List<PointEvent> ls, SequenceBounds wb)
        {
            if (itA.HasNext() || itB.HasNext())
            {
                var eA = itA.GetCurrent();
                var eB = itB.GetCurrent();

                if (eA.DeterministicKnown && eB.DeterministicKnown)
                {
                    if (itA.IsLast() && itB.IsLast())
                    {
                        return 1;
                    }
                    if (eA.Timestamp.Value > eB.Timestamp.Value)
                    {
                        return InsertEvent(r, itA, itB, ls, wb, "A");
                    }
                    else
                    {
                        return InsertEvent(r, itA, itB, ls, wb, "A");
                    }
                }
                else if (!eA.DeterministicKnown && !eB.DeterministicKnown)
                {
                    return InsertEvent(r, itA, itB, ls, wb, "A") + InsertEvent(r, itA, itB, ls, wb, "B");
                }
                else if(!eA.DeterministicKnown)
                {
                    if (itA.NextDeterministic() > eB.Timestamp.Value) //B can be first
                    {
                        return InsertEvent(r, itA, itB, ls, wb, "A") + InsertEvent(r, itA, itB, ls, wb, "B");
                    }
                    else
                    {
                        return InsertEvent(r, itA, itB, ls, wb, "A");
                    }
                }
                else if (!eA.DeterministicKnown) 
                {
                    if (itB.NextDeterministic() > eA.Timestamp.Value) //A can be first
                    {
                        return InsertEvent(r, itA, itB, ls, wb, "B") + InsertEvent(r, itA, itB, ls, wb, "A");
                    }
                    else
                    {
                        return InsertEvent(r, itA, itB, ls, wb, "B");
                    }
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
            else
            {
                return 1;
            }
        }

        private double InsertEvent(TreeNode r, Iterator itA, Iterator itB, List<PointEvent> ls, SequenceBounds wb, string v)
        {
            double pr = 1;
            TreeNode rNew;
            PointEvent e;

            if (v == "A")
            {
                e = itA.GetCurrent();
                rNew = r.AddChild(e);
                itA.Next();
            }
            else
            {
                e = itB.GetCurrent();
                rNew = r.AddChild(e);
                itB.Next();
            }

            if (rNew == null)
            {
                return 0;
            }

            if (e.DeterministicKnown)
            {
                wb.Lower = wb.Upper;
                wb.Upper = e.Timestamp.Value;
                pr = Trace(ls, wb);
                ls.Clear();
            }
            else
            {
                ls.Add(e);
            }

            return pr * Merge(itA, itB, rNew, ls, wb);
        }

        private double Trace(List<PointEvent> ls, SequenceBounds wb)
        {
            throw new NotImplementedException();
            //var m = ls.Count(x => x.Association == "A");
            //var n = ls.Count(x => x.Association == "B");

            //double a = 1;
            //if (m > 0)
            //{
            //    ls.First().
            //    var startWindowA = ls.Where(x => x.Association == "i").First().MaxPathDeterministicTimestamp;
            //    var endWindowA = l.Data.Timestamp.Value;
            //    a = Math.Pow((endWindowA - startWindowA).Ticks, m);
            //}

            //double b = 1;
            //if (n > 0)
            //{
            //    var endWindowB = ls.Where(x => x.Association == "j").Last().MaxPathDeterministicTimestamp;
            //    var startWindowB = ls.Where(x => x.Association == "j").First().MaxPathDeterministicTimestamp;
            //    b = Math.Pow((endWindowB - startWindowB).Ticks, m);
            //}

            //return Math.Pow((wb.Upper - wb.Lower).Ticks, m + n) / (SpecialFunctions.Factorial(14) * a * b);
        }
    }
}
