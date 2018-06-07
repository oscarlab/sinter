
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Collections;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.ComponentModel;
using Sintering;

namespace WindowsProxy
{
    class TagInfo
    {
        String id;
        Point coOrdinates;
        public String Id
        {
            get { return id; }
            set { id = value;  }
        }
        public Point CoOrdinates
        {
            get { return coOrdinates; }
            set { coOrdinates = value; }
        }
        public TagInfo(string id, Point xy)
        {
            this.id = id;
            coOrdinates = xy;
        }
    };
    public static class XMLTags
    {
        public readonly static String header = "<sinter>";
        public readonly static String header2 = "\n<sinter>";
        public readonly static String trailer = "</sinter>";
        public readonly static String window = "AXWindow";
        public readonly static String element = "ui";
        public readonly static String application = "application";
        public readonly static String name = "name";
        public readonly static String title = "title";
        public readonly static String processID = "process_id";
        public readonly static String childID = "child_id";
        public readonly static String id = "id";
        public readonly static String objectID = "object_id";
        public readonly static String value = "value";
        public readonly static String role = "role";
        public readonly static String type = "type";
        public readonly static String left = "left";
        public readonly static String top = "top";
        public readonly static String width = "width";
        public readonly static String height = "height";
        public readonly static String states = "states";
        public readonly static String childCount = "child_count";
        public readonly static String ROOTNODE_OF_XML = "//sinter";
        public readonly static String APPLICATION_NODE_XML = "//application";
 
    }
    public class RemoteProcessUI//Parser
    {
        RootForm controller;
        static Dictionary<string, List<Tuple<String, String, String>>> propertyDict
            = new Dictionary<string, List<Tuple<String, String, String>>>();
        static Dictionary<string, List<Tuple<String, String>>> methodDict
            = new Dictionary<string, List<Tuple<String, String>>>();
        static Dictionary<string, string> uiElementToClassDict
            = new Dictionary<string, string>();

        public RemoteProcessUI(RootForm controller)
        {
            this.controller = controller;
            
            XmlDocument configDoc = new XmlDocument();
            configDoc.Load("uiconfig.xml");

            buildDictionaryFromConfig(configDoc.SelectSingleNode(XMLTags.ROOTNODE_OF_XML));
        }

        
        public object GetInstance(string strFullyQualifiedName)
        {
            Type type = Type.GetType(strFullyQualifiedName);
            if (type != null)
                return Activator.CreateInstance(type);
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = asm.GetType(strFullyQualifiedName);
                if (type != null)
                    return Activator.CreateInstance(type);
            }
            return null;
        }
        private void 
        buildDictionaryFromConfig(XmlNode root)
        {
            foreach (XmlNode child in root.ChildNodes)
            {
                List<Tuple<String, String, String>> propertyList
                    = new List<Tuple<String, String, String>>();
                List<Tuple<String, String>> methodList
                    = new List<Tuple<String, String>>();
                uiElementToClassDict.Add(child.Name, child.Attributes["type"].Value);
                foreach (XmlNode element in child)
                {
                    if (element.Name.Equals("property", StringComparison.OrdinalIgnoreCase))
                    {
                        var tuple = Tuple.Create(element.Attributes["name"].Value,
                                                 element.Attributes["type"].Value,
                                                 element.Attributes["xmlName"].Value);
                        propertyList.Add(tuple);
                    }
                    else if (element.Name.Equals("method", StringComparison.OrdinalIgnoreCase))
                    {
                        var tuple = Tuple.Create(element.Attributes["name"].Value,
                                                 element.Attributes["params"].Value);
                        methodList.Add(tuple);
                    }
                }
                propertyDict.Add(child.Name, propertyList);
                methodDict.Add(child.Name, methodList);
            }
        }
        private string 
        getPropertyValue(XmlNode uiElement, string propertyName)
        {
            foreach (XmlAttribute attr in uiElement.Attributes)
            {
                if (attr.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    return attr.Value;
                }

            }
            return null;
        }
        private dynamic 
        buildUIObject(XmlNode uiElement)
        {
            //Get all properties to be implemented
            List<Tuple<String, String, String>> properties = propertyDict[uiElement.Name];
            string UIClassName = uiElementToClassDict[uiElement.Name];
            dynamic genericObj = GetInstance(UIClassName);

            // Get the object type and property info. 
            Type objectType = genericObj.GetType();

            foreach (var property in properties)
            {
                string propertyToBeImplementedString = property.Item1;
                string typeOfPropertyString = property.Item2;
                Type typeOfProperty = Type.GetType(typeOfPropertyString);

                string xmlNameOfProperty = property.Item3;

                string propertyValueString = getPropertyValue(uiElement, xmlNameOfProperty);

                // Convert property value from string to required type.
                TypeConverter typeConverter = TypeDescriptor.GetConverter(typeOfProperty);
                object propValue = typeConverter.ConvertFromString(propertyValueString);


                PropertyInfo prop = objectType.GetProperty(propertyToBeImplementedString);

                prop.SetValue(genericObj, propValue, null);

            }
            return genericObj;
        }
        private void 
        positionChildrenWithRespectToParent(dynamic child, dynamic parent)
        {
            dynamic obj = parent;
            Type typeOfChild = child.GetType();
            while (obj != null)
            {
                // Some controls like the ToolStripDropDownButton do not have
                // Left and Top properties at all. Check for that, else a 
                // RuntimeBinder Exception will be thrown.
                if (typeOfChild.GetProperties().Where(p => p.Name.Equals("Left")).Any())
                {
                    child.Left -= obj.Left;
                }
                if (typeOfChild.GetProperties().Where(p => p.Name.Equals("Top")).Any())
                {
                    child.Top -= obj.Top;
                }
                obj = obj.Parent;
            }
            // In Mac, unfortunately, menubar is not part of the window. But in Windows it is. Hence
            // we have to compensate for that. We've already increased the window size by 25 pixels.
            // Now move all controls down by 25 pixels.
            //child.Top += 25;
        }
        private string
        addRow(XmlNode RowRoot)
        {
            XmlNode childRoot = RowRoot.SelectSingleNode("children");
            return childRoot.FirstChild.Attributes["value"].Value;
        }
        private void
        addTreeView(XmlNode TreeViewRoot, TreeView tv)
        {
            XmlNode childRoot = TreeViewRoot.SelectSingleNode("children");
            foreach (XmlNode child in childRoot)
            {
                if (child.Name == "AXRow")
                {
                    TreeNode node = new TreeNode(addRow(child));
                    tv.Nodes.Add(node);
                }
            }
        }
        private void
        addListView(XmlNode ListViewRoot, ListView lv)
        {
            Console.WriteLine("GetFolderPath: {0}",
                 Environment.GetFolderPath(Environment.SpecialFolder.Personal));
            // Add some nice images to listview
            System.Drawing.Image myImage = Image.FromFile("folder.png");
            ImageList imageList = new ImageList();

            imageList.Images.Add(myImage);
            lv.SmallImageList = imageList;

            // Set the view to show details.
            lv.View = View.List;
            // Allow the user to edit item text.
            lv.LabelEdit = true;
            lv.FullRowSelect = true;
            lv.Sorting = SortOrder.Ascending;

            XmlNode childRoot = ListViewRoot.SelectSingleNode("children");
            foreach (XmlNode child in childRoot)
            {
                ListViewItem item = new ListViewItem(child.Attributes["name"].Value);
                item.ImageIndex = 0;
                lv.Items.Add(item);
            }
        }
        private ToolStripMenuItem addToolStrip(XmlNode root)
        {
            ToolStripMenuItem menuItem = new ToolStripMenuItem(root.Attributes["name"].Value);
            XmlNode childRoot = root.SelectSingleNode("children");

            if (childRoot != null)
            {
                foreach (XmlNode child in childRoot)
                {
                    if (string.IsNullOrEmpty(child.Attributes["name"].Value))
                    {
                        menuItem.DropDownItems.Add(new ToolStripSeparator());
                    }
                    else
                    {
                        menuItem.DropDownItems.Add(addToolStrip(child));
                    }
                }
            }
            return menuItem;
        }
        private void
        addMenuBar( XmlNode MenuBar, MenuStrip ms )
        {
            XmlNode childRoot = MenuBar.SelectSingleNode("children");
            foreach (XmlNode child in childRoot)
            {
                ms.Items.Add(addToolStrip(child));
            }
        }
        private TreeNode
        addTreeNode(XmlNode root)
        {
            TreeNode treeNode = new TreeNode(root.Attributes["name"].Value);
            XmlNode childRoot = root.SelectSingleNode("children");

            if (childRoot != null)
            {
                foreach (XmlNode child in childRoot)
                {
                    treeNode.Nodes.Add(addTreeNode(child));
                }
            }
            return treeNode;
        }
        private void
        addTreeViewPart2(XmlNode TreeViewRoot, TreeView tv)
        {
            XmlNode childRoot = TreeViewRoot.SelectSingleNode("children");
            foreach (XmlNode child in childRoot)
            {
                tv.Nodes.Add(addTreeNode(child));
            }
        }
        private void 
        addToolbar( XmlNode ToolstripRoot, ToolStrip ts)
        {
            XmlNode childRoot = ToolstripRoot.SelectSingleNode("children");
            foreach (XmlNode child in childRoot)
            {
                if (uiElementToClassDict.ContainsKey(child.Name))
                {
                    ts.Items.Add(buildUIObject(child));
                }
                else
                {
                    addToolbar(child, ts);
                }
            }
        }

        private dynamic ParseXMLAndBuildUI(XmlNode root, object parent)
        {
            dynamic dynParent = parent;
            foreach (XmlNode childNode in root.ChildNodes)
            {

                if (uiElementToClassDict.ContainsKey(childNode.Name))
                {
                    // Set all properties of the control.
                    dynamic uiControl = buildUIObject(childNode);

                    // Save the unique ID and co-ordinates of the control in tag
                    // for future reference
                    int x = Convert.ToInt32(childNode.Attributes["left"].Value) +
                            Convert.ToInt32(childNode.Attributes["width"].Value)/2;
                    int y = Convert.ToInt32(childNode.Attributes["top"].Value) +
                        Convert.ToInt32(childNode.Attributes["height"].Value) / 2;
                    uiControl.Tag = new TagInfo(childNode.Attributes["id"].Value,
                                                new Point (x,y));
                    if (dynParent == null)
                    {
                        dynParent = uiControl;
                        Console.WriteLine(uiControl.GetType());
                        Console.WriteLine(Type.GetType("System.Windows.Forms.Form"));
                        if ( uiControl.GetType().FullName == "System.Windows.Forms.Form")
                        {
                            // Hack for increasing window size as Windows eats up some pixels for
                            // showing borders that OS X doesnt do.
                            uiControl.Width += (8 + 8);
                            uiControl.Height += (30 + 8);

                            uiControl.Height += 25; // Add space for Menubar.
                        }

                        ParseXMLAndBuildUI(childNode, dynParent);
                    }
                    else
                    {
                        positionChildrenWithRespectToParent(uiControl, dynParent);
                        if (childNode.Name.Equals("AXOutline", StringComparison.OrdinalIgnoreCase))
                        {
                            addTreeView(childNode, uiControl);
                            dynParent.Controls.Add(uiControl);
                            continue;
                        }
                        else if (childNode.Name.Equals("AXList", StringComparison.OrdinalIgnoreCase))
                        {
                            addListView(childNode, uiControl);
                            dynParent.Controls.Add(uiControl);
                            continue;
                        }
                        else if (childNode.Name.Equals("AXMenuBar", StringComparison.OrdinalIgnoreCase))
                        {
                            addMenuBar(childNode, uiControl);
                            dynParent.Controls.Add(uiControl);
                            continue;
                        }
                        else if (childNode.Name.Equals("TreeNode", StringComparison.OrdinalIgnoreCase))
                        {
                            addTreeViewPart2(childNode, uiControl);
                            dynParent.Controls.Add(uiControl);
                            continue;
                        }
                        else if (childNode.Name.Equals("AXToolbar", StringComparison.OrdinalIgnoreCase))
                        {
                            addToolbar(childNode, uiControl);
                            dynParent.Controls.Add(uiControl);
                            continue;
                        }
                        dynParent.Controls.Add(uiControl);

                        Console.WriteLine("I am {0}. I have {1} children.I am now adding {2}",
                                           dynParent.GetType(), dynParent.Controls.Count, uiControl.Text);
                        ParseXMLAndBuildUI(childNode, uiControl);

                    }
                }
                else
                {
                    if (dynParent == null)
                    {
                        dynParent = ParseXMLAndBuildUI(childNode, dynParent);
                    }
                    else
                    {
                        ParseXMLAndBuildUI(childNode, dynParent);
                    }
                }

            }
            return dynParent;
        }
        public void parse(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode rootNode = doc.SelectSingleNode(XMLTags.ROOTNODE_OF_XML);
            int pid = 1; // = extractPidFromXml(); // We should extract pid of the incoming XML.
            dynamic root = ParseXMLAndBuildUI(rootNode, null);
            // Get the application name and assign it to form.
            rootNode = doc.SelectSingleNode(XMLTags.APPLICATION_NODE_XML);
            root.Text = root.Name = rootNode.Attributes["name"].Value;

            controller.renderer(root, pid);

        }
        // Handles dynamic updates. Searches for the root control of the XML in the current 
        // running form.
        public void parseUpdate(string xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode rootNode = doc.SelectSingleNode(XMLTags.ROOTNODE_OF_XML);
            
            dynamic controlToBeUpdated = ParseXMLAndBuildUI(rootNode, null);
            int pid = 1; // = extractPidFromXml(); // We should extract pid of the incoming update.
            ApplicationForm form = (ApplicationForm)controller.Form_table[pid];

            dynamic controlToBeReplaced = findControlWithSameId(form, controlToBeUpdated);

            if ( controlToBeReplaced == null )
            {
                Console.WriteLine("WARNING: The root node in the update XML not found in the form!!\n");
                Console.WriteLine("         No update performed.\n");
                return;
            }
            // Dispose off all children of the control to be replaced.
            for (int i = controlToBeReplaced.Controls.Count - 1; i >= 0; --i)
                controlToBeReplaced[i].Dispose();
            
            // Get the parent of the control to be removed
            dynamic parentOfControlToBeReplaced = controlToBeReplaced.Parent;
            // Remove the control that has new update.
            parentOfControlToBeReplaced.Controls.Remove(controlToBeReplaced);
            controlToBeReplaced.Dispose();

            // Add the newly arrived updated control as child to the old parent.
            parentOfControlToBeReplaced.Controls.Add(controlToBeUpdated);
            form.Show();
        }

        private dynamic findControlWithSameId(dynamic form, dynamic control)
        {
            foreach (Control c in form.Controls)
            {
                TagInfo haystack = (TagInfo)c.Tag;
                TagInfo needle = (TagInfo)control.Tag;
                if ( haystack.Id == needle.Id )
                {
                    return c;
                }
                findControlWithSameId(c, control);
            }
            return null;
        }

    }

    public class ListProcessUI
    {
        RootForm controller;
        RemoteProcessDetails process;
        List<RemoteProcessDetails> processes;

        public ListProcessUI(RootForm controller)
        {
            this.controller = controller;
            processes = new List<RemoteProcessDetails>();             
        }
        public bool parseXML(string xml)
        {
            processes.Clear();
            using (XmlReader reader = XmlReader.Create(new StringReader(xml)))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == XMLTags.application )
                    {
                        process = new RemoteProcessDetails();
                        process.Name = reader.GetAttribute(XMLTags.name);
                        process.Pid = reader.GetAttribute(XMLTags.processID);
                        processes.Add(process);
                    }
                }
            }

            return true;
        }
        // This function parses and displays ls_window output
        public void parse(string xml)
        {
            parseXML(xml);
            //controller.renderer(processes);
        }
    }
}
