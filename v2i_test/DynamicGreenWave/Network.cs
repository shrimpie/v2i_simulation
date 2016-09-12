﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VISSIMLIB;
using System.Diagnostics;
using System.IO;

namespace DynamicGreenWave
{
    class Network
    {
        private Intersection[] inters = null;
        private int inter_num;
        private int[,] inter_graph = null;
        private Vissim vissim = null;
        private IDetectorContainer dc = null;
        private string[] stpln_det_config = null;
        private string[] overflow_det_config = null;


        public Network(Vissim vissim, int inter_num)
        {
            this.vissim = vissim;
            
            this.inter_num = inter_num;
            this.init_inter_graph();
            this.init_net_det_str();
            this.init_inters();
        }

        private void init_inters()
        {
            this.inters = new Intersection[this.inter_num];
            ISignalControllerContainer scContain = this.vissim.Net.SignalControllers;
            ISignalController[] SC = new ISignalController[this.inter_num];
            this.dc = this.vissim.Net.Detectors;

            Dictionary<int, int> portno_key = new Dictionary<int, int>();
            for (int i = 1; i <= this.dc.Count; i++)
            {
                IDetector det = this.dc.get_ItemByKey(i);
                int id = det.get_AttValue("PORTNO");
                portno_key[id] = i;
            }
            int[] real_key = new int[this.dc.Count];
            for (int i = 0; i < this.dc.Count; i++)
                real_key[i] = portno_key[i + 1];

            for (int i = 0; i < this.inter_num; i++)
            {
                SC[i] = scContain.get_ItemByKey(i + 1);
                this.inters[i] = new Intersection(i + 1, this.dc, this.stpln_det_config[i], this.overflow_det_config[i], 
                                                   SC[i], this.inter_graph, real_key);
            }

            foreach (var inter in this.inters)
                inter.init_neighbors(this.inters);
        }

        private void init_inter_graph()
        {
            this.inter_graph = Globals.NETWORK_GRAPH;
        }

        public void update_network_status()
        {
            foreach (var inter in this.inters) inter.update_link_status();
        }

        public void update_network_veh_status()
        {
            foreach (var inter in this.inters)
            {
                inter.update_veh_pos();
            }
        }

        private void init_net_det_str()
        {
            this.stpln_det_config = Globals.STOPLINE_CONFIG;
            this.overflow_det_config = Globals.OVERFLOW_CONFIG;
        }

        public void lighten_signals(int cur_time)
        {
            for (int i=0; i < this.inters.Length; i++)
                this.inters[i].lighten_signals(cur_time);
        }

        public void clear_signals()
        {
            foreach (var inter in this.inters) inter.clear_signals();
        }

        /*
        static int[] run_cmd(string cmd, string args)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = "D:\\Python27\\python.exe";
            start.Arguments = string.Format("{0} {1}", cmd, args);
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            using (Process process = Process.Start(start))
            {
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    Console.Write(result);
                    int[] res = new int[Globals.INTER_NUM];
                    result = result.Replace("\r\n\r\n", " ");
                    string[] tgt = result.Split(' ');
                    for (int i = 0; i < Globals.INTER_NUM; i++)
                    {
                        float tmp = 0;
                        float.TryParse(tgt[2 * i + 1], out tmp);
                        res[i] = (int)tmp;
                    }
                    return res;
                }
            }
        }
         * */
        /*
        private int[] get_extension_upper_bound_using_maxflow()
        {
            string tgt_args = "";
            return run_cmd("D:\\max_flow.py", tgt_args);
        }
         * */


    }
}
