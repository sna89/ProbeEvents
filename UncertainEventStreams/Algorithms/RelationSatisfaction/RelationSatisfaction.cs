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
    public class RelationSatisfaction
    {
        public enum BranchCase { BothDeterministic, BothNonDeterministic, LeftNonDeterministic, RightNonDeterministic }

        #region Private members

        private PointEvent[] _firstEventStream;
        private PointEvent[] _secondEventStream;
        private Predicate<TreeNode> _predicate;

        public int Pruned { get; private set; }

        #endregion

        #region helpers

        private LinkedList<TreeNode> ObtainLostSequence(TreeNode l)
        {
            var curr = l.Parent;
            var ls = new LinkedList<TreeNode>();
            while (!curr.IsRoot && !curr.Data.DeterministicKnown)
            {
                ls.AddFirst(curr);
                curr = curr.Parent;
            }

            return ls;
        }

        private WindowBounds ObtainWindowBounds(TreeNode l)
        {
            return new WindowBounds()
            {
                Lower = l.Parent.MaxPathDeterministicTimestamp,
                Upper = l.Data.Timestamp.Value
            };
        }

        private double Trace(TreeNode l, int i, int j)
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
            
            var wb = l.GetWindow(); //ObtainWindowBounds(l);
            var wbA = GetBoundaries(l, PointEvent.PointType.A, i, _firstEventStream);
            var wbB = GetBoundaries(l, PointEvent.PointType.B, j, _secondEventStream);
            var m = ls.Count(x => x.Data.DataPointType ==  PointEvent.PointType.A);
            var n = ls.Count(x => x.Data.DataPointType == PointEvent.PointType.B);

            double a = 1;
            if (m > 0)
            {
                if (wbA.TicksDifference == 0)
                {
                    throw new Exception("Case A has missing events, and window in size 0");
                }
                
                a = SpecialFunctions.Factorial(m) / Math.Pow(wbA.TicksDifference, m);
            }

            double b = 1;
            if (n > 0)
            {
                if (wbB.TicksDifference == 0)
                {
                    throw new Exception("Case B has missing events, and window in size 0");
                }

                b = SpecialFunctions.Factorial(n) / Math.Pow(wbB.TicksDifference, n);
            }

            var global = Math.Pow((wb.Upper - wb.Lower).Ticks, m + n) / (SpecialFunctions.Factorial(m + n));

            var res = global * a * b;

            return res;
        }

        private TreeNode Add(TreeNode l, PointEvent data)
        {
            if(l.IsRoot && l.Data == null)
            {
                l.Data = data;
                if(data.Timestamp.HasValue)
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

        private WindowBounds GetBoundaries(TreeNode l, PointEvent.PointType type, int index, PointEvent[] stream)
        {
            if (l.Data.DataPointType == type)
            {
                return l.GetBoundaries(type, l.Data.Timestamp.Value);
            }

            return l.GetBoundaries(type, NextDeterministic(index, stream));
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

        private double LeftNonDeterministic(int i,  int j, TreeNode l, double pr)
        {
            //ei is not known 
            //Initialization
            double pr1 = 0;
            double pr2 = 0;
            var ei = _firstEventStream[i];
            var ej = _secondEventStream[j];

            var lNew = Add(l, ei);
            if(lNew != null)
            {
                pr1 = Merge(i + 1, j, lNew, pr);
            }
            if(NextDeterministic(i, _firstEventStream) > ej.Timestamp.Value) //ej can be first
            {
                lNew = Add(l, ej);  

                if(lNew != null)
                {
                    pr = pr * Trace(lNew, i , j);
                    pr2 = Merge(i, j + 1, lNew, pr);
                }
                else
                {
                    Pruned++;
                }
            }
            else
            {
                Pruned++;
            }

            l.PathProbability = pr1 + pr2;
            return pr1 + pr2;
        }

        private double BothNonDeterministic(int i, int j, TreeNode l, double pr)
        {
            //Initialization
            double pr1 = 0;
            double pr2 = 0;
            var ei = _firstEventStream[i];
            var ej = _secondEventStream[j];

            if (true) //ej can be first
            {
                var lNew = Add(l, ej);
                if (lNew != null)
                {
                    //pr = pr * Trace(lNew);
                    pr1 = Merge(i, j + 1, lNew, pr);
                }
                else
                {
                    Console.WriteLine("Failed to create node in a none-determisitic case");
                }
            }

            if (true) //ei can be first
            {
                var lNew = Add(l, ei);
                if (lNew != null)
                {
                    //pr = pr * Trace(lNew);
                    pr2 = Merge(i + 1, j, lNew, pr);
                }
                else
                {
                    Console.WriteLine("Failed to create node in a none-determisitic case");
                }
            }

            l.PathProbability = pr1 + pr2;
            return pr1 + pr2;
        }

        private double BothDeterministic(int i, int j, TreeNode l, double pr)
        {
            var ei = _firstEventStream[i];
            var ej = _secondEventStream[j];
            double pr1 = 0;
            double pr2 = 0;

            if (ei.Timestamp.Value > ej.Timestamp.Value)
            {
                var lNew = Add(l, ej);
                if(lNew != null)
                {
                    pr = pr * Trace(lNew, i, j);
                    pr1 = Merge(i, j + 1, lNew, pr);
                }
                else
                {
                    Pruned++;
                }
            }
            else
            {
                var lNew = Add(l, ei);
                if (lNew != null)
                {
                    pr = pr * Trace(lNew, i, j);
                    pr2 = Merge(i + 1, j, lNew, pr);
                }
                else
                {
                    Pruned++;
                }
            }

            l.PathProbability = pr1 + pr2;
            return pr1 + pr2;
        }

        private double RightNonDeterministic(int i, int j, TreeNode l, double pr)
        {
            //ej is not known
            //Initialization
            double pr1 = 0;
            double pr2 = 0;
            var ei = _firstEventStream[i];
            var ej = _secondEventStream[j];

            var lNew = Add(l, ej);
            if (lNew != null)
            {
                pr1 = Merge(i, j + 1, lNew, pr);
            }
            if (NextDeterministic(j, _secondEventStream) > ei.Timestamp.Value) //ei can be first
            {
                lNew = Add(l, ei);
                if (lNew != null)
                {
                    pr = pr * Trace(lNew, i, j);
                    pr2 = Merge(i + 1, j, lNew, pr);
                }
                else
                {
                    Pruned++;
                }
            }
            else
            {
                Pruned++;
            }

            l.PathProbability = pr1 + pr2;
            return pr1 + pr2;
        }

        public RunResult Run(SegmentedIntervalEvent first, SegmentedIntervalEvent second, int stop, Predicate<TreeNode> predicate, bool debug = false)
        {
            _firstEventStream = first.GetEventStream(stop).ToArray();
            _secondEventStream = second.GetEventStream(stop).ToArray();

            var missing = _firstEventStream.Count(x => !x.Timestamp.HasValue) + _secondEventStream.Count(x => !x.Timestamp.HasValue);
            var total = _firstEventStream.Count() + _secondEventStream.Count();
            if (missing > 30 || total ==0)
            {
                Console.Write("Intervals: [{0}, {1}] are SKIPPED (too many missing events)", first.Key, second.Key);
                Console.WriteLine();
                return null;
            }

            Console.Write("Intervals: [{0}, {1}], Missing events: {2}, TotalEvents: {3}", first.Key, second.Key, missing, total);
            int i = 0;
            int j = 0;
            double pr = 1;
            Pruned = 0;
            _predicate = predicate;
            var l = TreeNode.GetRoot();

            var sw = Stopwatch.StartNew();
            var res = Merge(i, j, l, pr);
            sw.Stop(); 
            Console.Write(", Probability: {0} Running time: {1}", res, sw.Elapsed);

            if (debug)
            {
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
            }

            Console.WriteLine();
            return new RunResult(res, sw.Elapsed, missing) { Pruned = this.Pruned };
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

        private double Merge(int i, int j, TreeNode l, double pr)
        {
            if(i < _firstEventStream.Length && j < _secondEventStream.Length)
            {
                var ei = _firstEventStream[i];
                var ej = _secondEventStream[j];

                var branchingCase = GetBranchCase(ei, ej);
                switch (branchingCase)
                {
                    case BranchCase.BothDeterministic:
                        return BothDeterministic(i, j, l, pr);
                    case BranchCase.BothNonDeterministic:
                        return BothNonDeterministic(i, j, l, pr);
                    case BranchCase.LeftNonDeterministic:
                        return LeftNonDeterministic(i, j, l, pr);
                    case BranchCase.RightNonDeterministic:
                        return RightNonDeterministic(i, j, l, pr);
                    default:
                        break;
                }
            }
            else if(i < _firstEventStream.Length)
            {
                var res = PathToEnd(i, j, PointEvent.PointType.A, l, pr);
                l.PathProbability = res;
                return res;
            }
            else if(j < _secondEventStream.Length)
            {
                var res = PathToEnd(i, j, PointEvent.PointType.B, l, pr);
                l.PathProbability = res;
                return res;
            }

            throw new InvalidOperationException("Merge operation called with invalid indexes");
        }

        private double PathToEnd(int i, int j, PointEvent.PointType type, TreeNode l, double pr)
        {
            var lNew = l;

            if (type == PointEvent.PointType.A)
            {
                //reached end (didn't reach end)
                if (i < _firstEventStream.Length)
                {
                    var e = _firstEventStream[i];
                    lNew = Add(lNew, e);
                    if (lNew == null)
                    {
                        throw new Exception("Failed to add a node");
                    }

                    if (e.DeterministicKnown)
                    {
                        pr = pr * Trace(lNew, i, j);
                    }

                    var prob = PathToEnd(i + 1, j, type, lNew, pr);
                    lNew.PathProbability = prob;
                    return prob;
                }
            }
            else
            {
                //reached end (didn't reach end)
                if (j < _secondEventStream.Length)
                {
                    var e = _secondEventStream[j];
                    lNew = Add(lNew, e);
                    if (lNew == null)
                    {
                        throw new Exception("Failed to add a node");
                    }

                    if (e.DeterministicKnown)
                    {
                        pr = pr * Trace(lNew, i, j);
                    }

                    var prob = PathToEnd(i, j + 1, type, lNew, pr);
                    lNew.PathProbability = prob;
                    return prob;
                }
            }

            return _predicate(lNew) ? pr : 0;

            //for (int j = i; j < events.Length; j++)
            //{
            //    var e = events[j];
            //    lNew = Add(lNew, e);

            //    if(e.DeterministicKnown)
            //    {
            //        pr = pr * Trace(lNew, i, j);                    
            //    }

            //    lNew.PathProbability = pr;
            //}

            //return pr;
        }
    }
}
