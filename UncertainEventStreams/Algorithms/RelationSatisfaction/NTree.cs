using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UncertainEventStreams.Events;

namespace UncertainEventStreams.Algorithms.RelationSatisfaction
{
    public class TreeNode
    {
        public delegate void TreeVisitor<NTree>(NTree node);

        public TreeNode Parent { get; set; }

        public bool IsRoot { get { return Parent == null; } }

        public int Rank { get; private set; }

        public double PathProbability { get; set; }

        public PointEvent Data { get; set; }

        public DateTime MaxPathDeterministicTimestamp { get; set; }

        public IEnumerable<TreeNode> Children { get { return children.AsEnumerable(); } }

        public IEnumerable<TreeNode> PathToRoot
        {
            get
            {
                var curr = this;
                while(curr != null)
                {
                    yield return curr;
                    curr = curr.Parent;
                }
            }
        }

        private LinkedList<TreeNode> children;

        private void Traverse(TreeNode node, TreeVisitor<TreeNode> visitor)
        {
            visitor(node.Parent);
            foreach (TreeNode kid in node.children)
            {
                Traverse(kid, visitor);
            }
        }

        private TreeNode(PointEvent data, TreeNode parent)
        {
            this.Data = data;
            children = new LinkedList<TreeNode>();
            Parent = parent;
            MaxPathDeterministicTimestamp = DateTime.MinValue;
            Rank = 0;
            PathProbability = -1;
        }

        public virtual TreeNode AddChild(PointEvent data)
        {
            if (data.Timestamp.HasValue && data.Timestamp.Value < MaxPathDeterministicTimestamp)
            {
                return null;
            }

            var child = new TreeNode(data, this)
            {
                MaxPathDeterministicTimestamp = data.Timestamp.HasValue && data.Timestamp.Value > MaxPathDeterministicTimestamp ?
                data.Timestamp.Value : MaxPathDeterministicTimestamp,
                Rank = this.Rank + 1
            };
            children.AddFirst(child);

            return child;
        }

        public void Traverse(TreeVisitor<TreeNode> visitor)
        {
            Traverse(this, visitor);
        }

        public TreeNode GetChild(int i)
        {
            foreach (TreeNode n in children)
                if (--i == 0)
                    return n;
            return null;
        }

        public WindowBounds GetWindow()
        {
            var curr = this;
            var wb = new WindowBounds()
            {
                Upper = curr.Data.Timestamp.Value,
                Lower = curr.Data.Timestamp.Value
            };
            curr = this.Parent;
            while (!curr.Data.DeterministicKnown && !curr.IsRoot)
            {
                curr = curr.Parent;
            }

            if (curr != null && curr.Data.DeterministicKnown)
            {
                wb.Lower = curr.Data.Timestamp.Value;
            }

            return wb;
        }

        public WindowBounds GetBoundaries(PointEvent.PointType pointType, DateTime upper)
        {
            var wb = new WindowBounds()
            {
                Upper = upper,
                Lower = this.Data.Timestamp.Value
            };
            var curr = this.Parent;
            while (!curr.IsRoot &&(!curr.Data.DeterministicKnown  || curr.Data.DataPointType != pointType))
            {
                curr = curr.Parent;
            }

            if (curr != null && curr.Data.DeterministicKnown)
            {
                wb.Lower = curr.Data.Timestamp.Value;
            }

            return wb;
        }


        public override string ToString()
        {
            string s = "";
            for (int i = 0; i < Rank; i++)
            {
                s += "--";
            }
            return s + string.Format("Stream: {0}, EventType {1}, Timestamp: {2}, Probability: {3}", Data.DataPointType, Data.Type, Data.Timestamp, PathProbability);
        }

        #region Static

        public static TreeNode GetRoot()
        {
            return new TreeNode(null, null) { PathProbability = 1 };
        }

        #endregion

    }
}
