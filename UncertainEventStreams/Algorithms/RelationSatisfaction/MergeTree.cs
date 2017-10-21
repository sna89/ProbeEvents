//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using UncertainEventStreams.Events;

//namespace UncertainEventStreams.Algorithms.RelationSatisfaction
//{
//    public class MergeTree
//    {
//        #region Private members

//        private NTree<PointEvent> _tree;

//        #endregion

//        #region Private classes

//        public class NTree
//        {
//            //public delegate void TreeVisitor<K>(K nodeData);

//            public NTree Parent { get; set; }

//            public PointEvent Data { get; set; }

//            private LinkedList<NTree> children;

//            public NTree(PointEvent data, NTree parent = null)
//            {
//                this.Data = data;
//                children = new LinkedList<NTree>();
//                Parent = parent;
//            }

//            public virtual NTree AddChild(PointEvent data)
//            {
//                var child = new NTree(data, this);
//                children.AddFirst(child);
//                return child;
//            }

//            public NTree GetChild(int i)
//            {
//                foreach (NTree n in children)
//                    if (--i == 0)
//                        return n;
//                return null;
//            }

//            //public void Traverse(NTree node, TreeVisitor<T> visitor)
//            //{
//            //    visitor(node.Data);
//            //    foreach (NTree<T> kid in node.children)
//            //        Traverse(kid, visitor);
//            //}
//        }

//        #endregion

//        public MergeTree()
//        {
//            _tree = new NTree<PointEvent>(null);
//        }
//    }
//}
