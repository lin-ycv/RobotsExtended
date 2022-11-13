using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RobotsExtended.Kuka
{
    public class KukaVarClient : GH_Component
    {
        public KukaVarClient() : base("Remote KUKA", "Remote", "KukaVarProxy Client. [Requires KukaVarProxy]", "Robots", "Components") { }
        public override GH_Exposure Exposure => GH_Exposure.senary; //| GH_Exposure.obscure;
        public override Guid ComponentGuid => new Guid("D746BC1B-CA90-401D-ACDC-9320D2AD0844");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.KukaVarProxyConnect;
        public override void AddedToDocument(GH_Document document)
        {
            base.AddedToDocument(document);
            document.ObjectsDeleted += ThisRemoved;
        }

        public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalMenuItems(menu);

            Menu_AppendItem(menu, "Timeout (ms)", (s, e) => { }, false);
            Menu_AppendCustomItem(menu, rTO);
            Menu_AppendItem(menu, "Keep Alive", (s, e) =>
            {
                bgKeepAlive = !bgKeepAlive;
                if (bgKeepAlive)
                    BackgroundTask();
                else
                {
                    try
                    {
                        if (worker != null)
                        {
                            worker.CancelAsync();
                            worker.Dispose();
                        }
                    }
                    catch { }
                    worker = null;
                }
            }, true, bgKeepAlive);
            //Menu_AppendCustomItem(menu, rKA);
            //Menu_AppendSeparator(menu);
            //Menu_AppendItem(menu, "Clear Log", (s, e) =>
            //{
            //    log = new List<string>();
            //    UpdateOut(null, null);
            //}, true);
            //Menu_AppendItem(menu, "Update Logs now", (s, e) => UpdateOut(null, null), true);
            Menu_AppendItem(menu, "Keep Logs", (s, e) => logNewestOnly = !logNewestOnly, true, !logNewestOnly);
#if (DEBUG)
            Menu_AppendItem(menu, "DEBUG", (s, e) => debug = !debug, true, debug);
#endif
        }
        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Command", "C", "Command to send, will query joint angle if nothing connected", GH_ParamAccess.list, "$AXIS_ACT");
            pManager.AddTextParameter("IP Address", "IP", "IP address of controller", GH_ParamAccess.item, "0.0.0.0"/*, "192.168.0.1" /* "172.31.1.147"*/);
            pManager.AddIntegerParameter("Port", "P", "Port to use for communications", GH_ParamAccess.item, 7000);
            pManager.AddBooleanParameter("Execute Program", "E", "Execute", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "L", "Log", GH_ParamAccess.list);
        }

        TcpClient client;
        NetworkStream stream = null;
        NumericUpDown rTO = new NumericUpDown
        {
            Minimum = 250,
            Maximum = 10000,
            Increment = 50,
            Value = 5000,
            DecimalPlaces = 0
        };
        BackgroundWorker worker;
        string ip = string.Empty;
        bool bgKeepAlive = true;
        List<string> log = new List<string>();
        bool logNewestOnly = true;
#if(DEBUG)
        bool debug = false;
#endif
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> usercmd = new List<string>();
            bool run = false;
            int port = 7000;
            DA.GetData("IP Address", ref ip);
            DA.GetData("Port", ref port);
            DA.GetDataList("Command", usercmd);
            DA.GetData("Execute Program", ref run);
#if (DEBUG)
            if (ip == "0.0.0.0")
            {
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                    ip = endPoint.Address.ToString();
                }
            }
#endif
            if (run)
            {
                if (rTO.Value > rTO.Maximum) rTO.Value = rTO.Maximum;
                else if (rTO.Value < rTO.Minimum) rTO.Value = rTO.Minimum;
                int timeout = (int)rTO.Value;
                if (client == null || client.Connected == false)
                {
                    try
                    {
                        client = new TcpClient
                        {
                            ReceiveTimeout = timeout,
                            SendTimeout = timeout,
                            ReceiveBufferSize = 2048 // Match KVP, too big creates GDI+ error in GH
                        };
                        if (client.ConnectAsync(ip, port).Wait(timeout))
                        {
                            stream = client.GetStream();
                            stream.WriteTimeout = timeout;
                            stream.ReadTimeout = timeout;
                        }
                        else
                            throw new TimeoutException($"A connection attempt failed because the connected party did not properly respond after a period of time, or established connection failed because connected host has failed to respond {ip}:{port} within {timeout}ms");
                    }
                    catch (Exception e)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Communication failed");
#if (DEBUG)
                        log.Add(e.ToString());
#else
                        log.Add(e.Message);
#endif
                        DA.SetDataList(0, log);
                        return;
                    }
                }
                if (bgKeepAlive && worker == null)
                {
                    BackgroundTask();
                }
#if (DEBUG)
                byte[] tcpmsg;
                if (!debug)
                {
                    tcpmsg = EncodeMsg(usercmd);
                }
                else
                {
                    try
                    { tcpmsg = Array.ConvertAll(usercmd[0].Split(' '), s => (byte)int.Parse(s)); }
                    catch
                    { tcpmsg = new byte[] { 0, 0, 0, 2, 0, 0 }; }
                }
#else
                byte[] tcpmsg = EncodeMsg(usercmd);
#endif
                SendMsg(tcpmsg);
            }
            else if (client != null)
            {
                CloseConnection();
            }
            DA.SetDataList(0, log);
        }
        void BackgroundTask()
        {
            worker = new BackgroundWorker();
            worker.DoWork += KeepAlive;
            worker.WorkerSupportsCancellation = true;
            worker.RunWorkerAsync();
        }
        void KeepAlive(object sender, DoWorkEventArgs e)
        {
            //(ASCII)"$AXIS_ACT"=="0004000C00000924415849535F414354"(HEX) == "0 0 0 12 0 0 9 36 65 88 73 83 95 65 67 84 "(byte)
            byte[] poke = new byte[] { 100, 100, 0, 2, 0, 0 };
            while (bgKeepAlive)
            {
                try
                {
                    /*if (rKA.Value > rKA.Maximum) rKA.Value = rKA.Maximum;
                    else if (rKA.Value < rKA.Minimum) rKA.Value = rKA.Minimum;*/
                    System.Threading.Thread.Sleep(10000);//(int)rKA.Value);
                    if (stream == null) break;
                    SendMsg(poke);
                }
                catch (Exception er)
                {
#if (DEBUG)
                    log.Add(er.ToString());
#else
                    log.Add(er.Message);
#endif
                    CloseConnection();
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Communication failed");
                }
            }
        }
        byte[] EncodeMsg(List<string> message)
        {
            List<byte> msg = new List<byte>();
            // LittleEndian - no need to shift list to add header (ID and Req Length)
            for (int i = message.Count - 1; i >= 0; i--)
            {
                /*if (!(message[i][0] == '$'))
                {
                    this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"[{i}] is not a variable command, skipped");
                    continue; // Skip if not a variable
                }*/
                List<byte> temp = new List<byte>();
                for (int c = message[i].Length - 1; c >= -1; c--)
                {
                    if (c == -1 || message[i][c] == '=')
                    {
                        int length = temp.Count;
                        msg.AddRange(temp);
                        msg.AddRange(new byte[] { (byte)(length % 256), (byte)(length / 256) });
                        temp = new List<byte>();
                        continue;
                    }
                    //if (message[i][c] == ' ') continue; // Spaces needed to formate msg correctly
                    temp.Add(BitConverter.GetBytes(message[i][c])[0]); // Convert to byte, ASCII fits in 0-255, only uses [0], [1] always 0
                }
            }
            if (msg.Count == 0)
            {
                this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "No valid command detected");
                return null;
            }
            // Mode: 0=Read, 1=Write, (2=ReadArray, 3=WriteArray) Array not implemented in KVP
            msg.Add((byte)(message[0].Contains('=') ? 1 : 0));
            /*message.Count > 1 ? 3 : 1 :
            message.Count > 1 ? 2 : 0));*/
            // Request Length
            msg.AddRange(new byte[] { (byte)(msg.Count % 256), (byte)(msg.Count / 256) });
            // Request ID
            msg.AddRange(new byte[] { (byte)DateTime.Now.Second, (byte)DateTime.Now.Minute });

            msg.Reverse();
            return msg.ToArray();
        }
        void SendMsg(byte[] tcpmsg)
        {
            if (tcpmsg == null) return;
            if (logNewestOnly) log = new List<string>();
#if (DEBUG)
            log.Add("_Send (Byte):\r\n" + string.Join(" ", tcpmsg));
#endif
            stream.Write(tcpmsg, 0, tcpmsg.Length);
            var response = new byte[client.ReceiveBufferSize];
            string responseData = string.Empty;
            bool error = false;
            int rLength = 11;
            try
            {
                rLength = stream.Read(response, 0, client.ReceiveBufferSize);
                error = response[rLength - 3] == 0 &&
                        response[rLength - 2] == 0 &&
                        response[rLength - 1] == 0;
            }
            catch
            {
                //rLength = 11;
                error = true;
                response = new byte[] { 78, 111, 32, 82, 101, 115, 112, 111, 110, 115, 101 };
            }
            string respString = string.Join(" ", new ArraySegment<byte>(response, 0, rLength).ToArray());
            responseData = (error ? "_Error " : "") + respString;
            log.Add(responseData);
        }
        /*void UpdateOut(Object sender, ProgressChangedEventArgs e)//Need some otherway to update
        {
            var GhDoc = OnPingDocument();
            if (GhDoc != null)
            {
                this.Params.Output[0].ClearData();
                access.SetDataList(0, log);
                foreach (var recpt in this.Params.Output[0].Recipients)
                {
                    if (recpt.Phase == GH_SolutionPhase.Computed)
                        recpt.ExpireSolution(false);
                }
                GhDoc.ScheduleSolution(1);
            }
        }*/
        void ThisRemoved(object sender, GH_DocObjectEventArgs e)
        {
            if (e.Objects.Contains(this))
            {
                e.Document.ObjectsDeleted -= ThisRemoved;
                CloseConnection();
            }
        }
        void CloseConnection()
        {
            try
            {
                if (worker != null)
                {
                    worker.CancelAsync();
                    worker.Dispose();
                }
                if (client != null)
                {
                    client.Close();
                    client.Dispose();
                    stream.Close();
                    stream.Dispose();
                }
            }
            catch { }
            worker = null;
            client = null;
            stream = null;
            log.Add("_Connection Terminated.");
        }
    }
    /*public class KukaTCPresponse : GH_Component
    {
        public KukaTCPresponse() : base("Deconstruct TCP (AXIS)", "DeAXIS", "Deconstruct the KeepAlive response of Remote KUKA to get axis angles", "Robots", "Utility") { }
        public override GH_Exposure Exposure => GH_Exposure.hidden;
        public override Guid ComponentGuid => new Guid("7EC30372-DF94-4380-B713-4F0EA7AD8899");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Log", "L", "Log from Remote Kuka to deconstruct", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Joints", "J", "Controller angle in degrees for A1 to A6", GH_ParamAccess.list);
            pManager.AddNumberParameter("Extneral", "E", "Controller angle in degrees for E1 and E2", GH_ParamAccess.list);
        }
        List<double> joints = new List<double>();
        List<double> external = new List<double>();

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<string> log = new List<string>();
            DA.GetDataList("Log", log);
            int last = -1;
            for (int i = log.Count - 1; i >= 0; i--)
            {
                if (!(log[i][0] == '_') && log[i][0] == '0' && log[i][2] == '0')
                {
                    last = i;
                    break;
                }
            }
            if (last == -1)
            {
                DA.SetDataList("Joints", joints);
                DA.SetDataList("Extneral", external);
                return;
            }
            List<byte> ary = new List<byte>();
            Array.ForEach(log[last].Split(' '), b => ary.Add((byte)Convert.ToInt32(b)));
            string[] response = Encoding.ASCII.GetString(ary.ToArray()).Split(new string[] { "{E", ", E1 ", "}" }, StringSplitOptions.None);
            joints = Extract(response[1]);
            DA.SetDataList("Joints", joints);
            if (response.Length == 4)
            {
                external = Extract(response[2]);
                DA.SetDataList("Extneral", external);
            }
            List<double> Extract(string re)
            {
                List<double> outList = new List<double>();
                Array.ForEach(re.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries), e =>
                {
                    if (Double.TryParse(e, out double d))
                    {
                        outList.Add(d);
                    }
                });
                return outList;
            }
        }
        
    }
    */

}
