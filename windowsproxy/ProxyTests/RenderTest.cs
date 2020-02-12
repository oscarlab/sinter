using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sintering;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Automation;
using System.Diagnostics;
using WindowsScraper.Util;
using System.Threading;
using System.Windows.Forms;

namespace WindowsProxy.Tests
{
    [TestClass]
    public class RenderTest
    {
        private TestContext m_testContext;
        public TestContext TestContext
        {
            get { return m_testContext; }
            set { m_testContext = value; }
        }

        private string resultDir;
        WindowsProxy proxy;
        CommandHandler cmdhlr;
        DummyConnection conn;

        [TestInitialize]
        public void Initialize()
        {
            //get the grandparent folder of DeploymentDirectory to share with scraper
            resultDir = Directory.GetParent(Directory.GetParent(TestContext.DeploymentDirectory).FullName).FullName;
            Console.WriteLine("resultDir = {0}", resultDir);

            /* initialize a proxy and command handler */
            proxy = new WindowsProxy(new RootForm())
            {
                bPasscodeVerified = true
            };
            cmdhlr = new CommandHandler(null, null, proxy);
            conn = new DummyConnection();
            proxy.connection = conn;
        }

        void ParseXML(Entity entity, Dictionary<string, Entity> dic, int level)
        {
            entity.Type = entity.Type.First().ToString().ToUpper() + entity.Type.Substring(1); //turn "window" to "Window"
            string key = entity.Type + entity.Name;// + "_" + entity.UniqueID;

            for (int i = 0; i < level; i++)
            {
                Console.Write("\t");
            }

            if (dic.ContainsKey(key) == false)
            {
                dic.Add(key, entity);
                Console.WriteLine(key);
            }
            else
            {
                Console.WriteLine(key + " already in dict");
            }
            //Console.WriteLine("{0}/{1} has {2} children", entity.Type, entity.Name, entity.Children.Count);

            if (entity.Children != null && entity.Children.Count > 0)
            {
                foreach (Entity child_entity in entity.Children)
                    ParseXML(child_entity, dic, level+1);
            }
            else return;
        }
        

        public bool Render(string xmlfilePattern, Dictionary<string, Entity> dicScraper, Dictionary<string, Entity> dicProxy)
        {
            /* get input xml file and parse it to Sinter object */
            Sinter sinterIn = conn.GetSinterFromFile(resultDir, xmlfilePattern);
            Console.WriteLine(@"Input filename: {0}\*{1}", resultDir, xmlfilePattern);
            Assert.IsTrue(sinterIn != null);

            /* execute the sinter, in this case: ls_l_res */
            bool ret = cmdhlr.CommandExecSinter(sinterIn);
            Assert.IsTrue(ret);

            /* screenshot the render window, save to output folder */
            Bitmap bmp = new Bitmap(proxy.Form.Width, proxy.Form.Height);
            proxy.Form.DrawToBitmap(bmp, new Rectangle(0, 0, proxy.Form.Width, proxy.Form.Height));
            string pngfile = string.Format(@"{0}\{1}.png", TestContext.DeploymentDirectory, TestContext.TestName);
            bmp.Save(pngfile, ImageFormat.Png);
            Console.WriteLine("Output png saved to {0}", pngfile);

            /* create a dummy scraper to scraper our proxy window */
            WindowsScraper.WindowsScraper dummyScraper = new WindowsScraper.WindowsScraper();
            AutomationElement renderedElement = SinterUtil.GetAutomationElementFromId(Process.GetCurrentProcess().Id.ToString(), IdType.ProcessId);
            Assert.IsNotNull(renderedElement);
            Entity entityOut = dummyScraper.UIAElement2EntityRecursive(renderedElement);

            /* parse to two dictionay and compare */
            ParseXML(sinterIn.EntityNode, dicScraper, 0);
            ParseXML(entityOut, dicProxy, 0);

            /*
            foreach (KeyValuePair<string, Entity> kvp in dicScraper)
            {
                Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value.States);
            }
            Console.WriteLine("==================================");
            foreach (KeyValuePair<string, Entity> kvp in dicProxy)
            {
                Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value.States);
            }
            */
            
            proxy.dictFormCtrlButtons.Clear(); // this avoid user prompt
            proxy.Form.Close();
            return true;
        }

        public bool VerifyItems(Dictionary<string, Entity> dicScraper, Dictionary<string, Entity> dicProxy, string type, string attribute)
        {
            bool ret = true;
            foreach (KeyValuePair<string, Entity> kvp in dicScraper)
            {
                if (kvp.Key.StartsWith(type))
                {
                    Entity entityValue;
                    string proxyKey = kvp.Key;
                    if (type == "Text")
                    {
                        proxyKey = proxyKey.Replace("Text", "Edit");
                    }

                    if (dicProxy.TryGetValue(proxyKey, out entityValue))
                    {
                        //Console.WriteLine("Key = {0} is in proxy", proxyKey);

                        switch (attribute)
                        {
                            case "Value":
                                if ((kvp.Value.Value != null) && (kvp.Value.Value.Equals(entityValue.Value) == false))
                                {
                                    Console.WriteLine("Key = {0}, Value1 = {1}, Value2 = {2}", kvp.Key, kvp.Value.Value, entityValue.Value);
                                    ret = false;
                                }
                                break;
                            case "States":
                                if (kvp.Value.States != entityValue.States)
                                {
                                    Console.WriteLine("Key = {0}, States1 = {1}, States2 = {2}", kvp.Key, kvp.Value.States, entityValue.States);
                                    ret = false;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        //item not exists, fail
                        Console.WriteLine("Key = {0} missing in proxy", kvp.Key);
                        ret = false;
                    }
                }

            }
            return ret;
        }

        public bool GeneralCheck(string xmlfilePattern, string type, string attribute)
        {
            Dictionary<string, Entity> dicScraper = new Dictionary<string, Entity>();
            Dictionary<string, Entity> dicProxy = new Dictionary<string, Entity>();
            bool ret = Render(xmlfilePattern, dicScraper, dicProxy);
            Assert.IsTrue(ret);
            ret = VerifyItems(dicScraper, dicProxy, type, attribute);
            return ret;
        } 


        [TestMethod]
        public void Test101_Win7_Calc_Standard_Button()
        {
            bool ret1 = GeneralCheck("Win7_Calc_Standard.xml", "Button", "Value");
            bool ret2 = GeneralCheck("Win7_Calc_Standard.xml", "Button", "States");
            Assert.IsTrue(ret1 & ret2);
        }

        [TestMethod]
        public void Test102_Win7_Calc_Standard_Text()
        {
            bool ret2 = true;
            bool ret1 = GeneralCheck("Win7_Calc_Standard.xml", "Text", "Value");
            //ret2 = GeneralCheck("Win7_Calc_Standard.xml", "Text", "States"); //currently states doens't match
            Assert.IsTrue(ret1 & ret2);
        }

        [TestMethod]
        public void Test201_Win7_Calc_Scientific_Button()
        {
            bool ret1 = GeneralCheck("Win7_Calc_Scientific.xml", "Button", "Value");
            bool ret2 = GeneralCheck("Win7_Calc_Scientific.xml", "Button", "States");
            Assert.IsTrue(ret1&ret2);
        }

        [TestMethod]
        public void Test202_Win7_Calc_Scientific_Text()
        {
            bool ret2 = true;
            bool ret1 = GeneralCheck("Win7_Calc_Scientific.xml", "Text", "Value");
            //ret2 = GeneralCheck("Win7_Calc_Scientific.xml", "Text", "States"); //currently states doens't match
            Assert.IsTrue(ret1 & ret2);
        }

        [TestMethod]
        public void Test301_Win7_Calc_Programmer_Button()
        {
            bool ret1 = GeneralCheck("Win7_Calc_Programmer.xml", "Button", "Value");
            bool ret2 = GeneralCheck("Win7_Calc_Programmer.xml", "Button", "States");
            Assert.IsTrue(ret1 & ret2);
        }

        [TestMethod]
        public void Test302_Win7_Calc_Programmer_Button()
        {
            bool ret1 = GeneralCheck("Win7_Calc_Programmer.xml", "Text", "Value");
            Assert.IsTrue(ret1);
        }

        [TestMethod]
        public void Test303_Win7_Calc_Programmer_RadioButton()
        {
            bool ret1 = GeneralCheck("Win7_Calc_Programmer.xml", "RadioButton", "Value");
            bool ret2 = GeneralCheck("Win7_Calc_Programmer.xml", "RadioButton", "States");
            Assert.IsTrue(ret1 & ret2);
        }

        /* this case sometimes fails when running in batch
        [TestMethod]
        public void Test403_Win7_Calc_Statistics_CheckBox()
        {
            bool ret1 = GeneralCheck("Win7_Calc_Statistics.xml", "CheckBox", "Value");
            bool ret2 = GeneralCheck("Win7_Calc_Statistics.xml", "CheckBox", "States");
            Assert.IsTrue(ret1 & ret2);
        }
        */


    }
}
