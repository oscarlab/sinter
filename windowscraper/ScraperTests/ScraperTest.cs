using System;
using System.Collections.Generic;
using System.Windows.Automation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sintering;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;

namespace WindowsScraper.Tests
{
    [TestClass]
    public class ScraperTest
    {
        private TestContext m_testContext;
        public TestContext TestContext
        {
            get { return m_testContext; }
            set { m_testContext = value; }
        }

        private string resultDir;
        WindowsScraper scraper;
        CommandHandler cmdhlr;
        DummyConnection conn;

        [TestInitialize]
        public void Initialize()
        {
            //get the grandparent folder of DeploymentDirectory to share with proxy
            resultDir = Directory.GetParent(Directory.GetParent(TestContext.DeploymentDirectory).FullName).FullName;
            Console.WriteLine("resultDir = {0}", resultDir);

            /* initialize a scraper and command handler */
            scraper = new WindowsScraper("0000");
            scraper.bPasscodeVerified = true;
            cmdhlr = new CommandHandler(null, null, scraper);
            conn = new DummyConnection();
            conn.filepath = string.Format(@"{0}\{1}.xml", resultDir, TestContext.TestName); /* set connection filepath to desired output file */
            scraper.connection = conn;
        }

        public Process bringupApp(string path)
        {
            Process newprocess = Process.Start(path);
            Thread.Sleep(2000);
            return newprocess;
        }

        public void killProcess(Process appProcess)
        {
            appProcess.CloseMainWindow();
            appProcess.Close();
        }


        [TestMethod]
        public void Test001_ls_Win7_Calc()
        {
            Process calc = bringupApp(@"C:\Windows\System32\calc1.exe");
            Thread.Sleep(2000);

            /* create an input "ls_l" and execute it */
            Sinter sinter_ls_req = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(scraper.serviceCodes["ls_req"]),
                EntityNodes = null,
            };
            cmdhlr.CommandExecSinter(sinter_ls_req);

            /* get ls_res */
            Sinter sinter_ls_res = conn.GetSinterFromFile(conn.filepath);

            /* parse ls_res and find calculator */
            string calcProcess = null;
            foreach (Entity proc in sinter_ls_res.EntityNodes)
            {
                Console.WriteLine(proc.Name + " " + proc.Process);
                if (proc.Name.IndexOf("calc", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    calcProcess = proc.Process;
                    break;
                }
            }
            Assert.IsTrue(calcProcess != null); //to check if calc is in the "ls_res" result.
            killProcess(calc);
        }


        [TestMethod]
        public void Test002_ls_l_Win7_Calc_Standard()
        {
            Process calc = bringupApp(@"C:\Windows\System32\calc1.exe");
            SendKeys.SendWait("%1"); // Key ALT+1 switch to Standard mode
            Thread.Sleep(2000);

            /* create an input "ls_l_req" and execute it */
            Sinter sinter_ls_l_req = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(scraper.serviceCodes["ls_l_req"]),
                EntityNodes = null,
            };
            sinter_ls_l_req.HeaderNode.Process = calc.Id.ToString();

            cmdhlr.CommandExecSinter(sinter_ls_l_req);
            killProcess(calc);
        }

        [TestMethod]
        public void Test003_ls_l_Win7_Calc_Scientific()
        {
            Process calc = bringupApp(@"C:\Windows\System32\calc1.exe");
            SendKeys.SendWait("%2"); // Key ALT+2 switch to Scientific mode
            Thread.Sleep(2000);

            /* create an input "ls_l_req" and execute it */
            Sinter sinter_ls_l_req = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(scraper.serviceCodes["ls_l_req"]),
                EntityNodes = null,
            };
            sinter_ls_l_req.HeaderNode.Process = calc.Id.ToString();

            cmdhlr.CommandExecSinter(sinter_ls_l_req);
            killProcess(calc);
        }

        [TestMethod]
        public void Test004_ls_l_Win7_Calc_Programmer()
        {
            Process calc = bringupApp(@"C:\Windows\System32\calc1.exe");
            SendKeys.SendWait("%3"); // Key ALT+3 switch to Programmer mode
            Thread.Sleep(2000);

            /* create an input "ls_l_req" and execute it */
            Sinter sinter_ls_l_req = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(scraper.serviceCodes["ls_l_req"]),
                EntityNodes = null,
            };
            sinter_ls_l_req.HeaderNode.Process = calc.Id.ToString();

            cmdhlr.CommandExecSinter(sinter_ls_l_req);
            killProcess(calc);
        }

        [TestMethod]
        public void Test005_ls_l_Win7_Calc_Statistics()
        {
            Process calc = bringupApp(@"C:\Windows\System32\calc1.exe");
            SendKeys.SendWait("%4"); // Key ALT+4 switch to Statistics mode
            Thread.Sleep(2000);

            /* create an input "ls_l_req" and execute it */
            Sinter sinter_ls_l_req = new Sinter
            {
                HeaderNode = MsgUtil.BuildHeader(scraper.serviceCodes["ls_l_req"]),
                EntityNodes = null,
            };
            sinter_ls_l_req.HeaderNode.Process = calc.Id.ToString();

            cmdhlr.CommandExecSinter(sinter_ls_l_req);
            killProcess(calc);
        }

    }
}
