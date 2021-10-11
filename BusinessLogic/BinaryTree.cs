using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLogic
{
    public class BinaryTree
    {
        public BinaryTreeNode Root { get; set; }

        public void Add(List<List<List<double>>> subProblem, List<List<List<double>>> parentProblem = null)
        {
            BinaryTreeNode newNode = new BinaryTreeNode();
            newNode.Data = subProblem;

            if (Root == null)
            {
                Root = newNode;
                return;
            }   

            if (parentProblem != null)
            {
                BinaryTreeNode parentNode = Find(parentProblem);
                if (parentNode != null && parentNode.LeftNode == null)
                {
                    parentNode.LeftNode = newNode;
                    return;
                }

                if (parentNode != null && parentNode.RightNode == null)
                {
                    parentNode.RightNode = newNode;
                }
            }
        }

        public BinaryTreeNode Find(List<List<List<double>>> subProblem)
        {
            return this.Find(subProblem, this.Root);
        }

        private BinaryTreeNode Find(List<List<List<double>>> subProblem, BinaryTreeNode parent)
        {
            if (parent == null)       
                return null;

            if (parent.Data == subProblem)
            {
                return parent;
            }

            return Find(subProblem, parent.LeftNode) == null ? Find(subProblem, parent.RightNode) : Find(subProblem, parent.LeftNode);
        }

        public int GetHeight(BinaryTreeNode root)
        {
            if (root == null)
                return 0;

            int leftHeight = GetHeight(root.LeftNode);
            int rightHeight = GetHeight(root.RightNode);

            return leftHeight > rightHeight ? leftHeight + 1 : rightHeight + 1;
        }
    }
}
