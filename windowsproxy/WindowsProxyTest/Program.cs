using System;
using System.Collections.Generic;
using System.Windows.Automation;
using Sintering;
using WindowsProxy;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsProxyTest
{
    class Program
    {

         Sinter baseXML;
         WindowsProxy.WindowsProxy windowsProxy;
         int window_count;

        static void Main(String[] args){
            Program program = new Program();
            program.verifyDisplay();
        }

        public Program()
        {
            windowsProxy = new WindowsProxy.WindowsProxy(new RootForm());
            //windowsProxy.execute_ls_l_req(null);
            baseXML = windowsProxy.baseXML;
            window_count = windowsProxy.Window_Count;
        }
        #region Test Methods

        // Compares displayed application to Sinter entity nodes, printing mistmatched elements
        public void verifyDisplay()
        {
            if (baseXML == null)
                return;
            TreeWalker displayTree;
            AutomationElement element = null;
            PropertyCondition windowname = new PropertyCondition(AutomationElement.NameProperty, "UniqueID" + window_count);
            PropertyCondition windowProperty = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Window);
            AndCondition findCondition = new AndCondition(windowname, windowProperty);

            element = AutomationElement.RootElement.FindFirst(TreeScope.Children, findCondition);

            Condition condition1 = new PropertyCondition(AutomationElement.NameProperty, "UniqueID" + window_count);
            displayTree = new TreeWalker(condition1);

            CompareTree(displayTree, element, baseXML.EntityNode);
        }

        //Compares between Automation Nodes and Sinter Entity nodes, comparing type and name
        private bool CompareNodes(AutomationElement node1, Entity node2)
        {
            string Type = node1.Current.ControlType.ProgrammaticName.Substring(12);
            string Name = node1.Current.Name;
            if (Type == node2.Type && Name == node2.Name)
            {
                //Console.WriteLine("Displayed {0} matches expected element {1}.", Type, node2.Type);
                return true;
            }
            Console.WriteLine("Displayed {0} does not match expected element {1}.", Type, node2.Type);
            return false;
        }

        //Compares root elements before calling recursive method to compare children
        public void CompareTree(TreeWalker display, AutomationElement root, Entity expected)
        {
            if (root.Current.Name == "UniqueID" + window_count)
            {
                RecursiveCompareTree(display, root, expected);
            }
        }

        //Gets the list of children from the sinter entity and uses the treewalker to compare before calling itself to traverse further down the tree
        public void RecursiveCompareTree(TreeWalker display, AutomationElement node, Entity entity)
        {
            AutomationElement current = display.GetFirstChild(node);
            List<Entity> entityChildren = entity.Children;

            for (int i = 0; i < entityChildren.Count && current != null; i++)
            {
                CompareNodes(current, entityChildren[i]);

                if (display.GetFirstChild(current) != null)
                {
                    RecursiveCompareTree(display, current, entityChildren[i]);
                }
                current = display.GetNextSibling(current);
            }
        }

        #endregion
    }
}
