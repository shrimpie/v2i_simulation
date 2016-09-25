using System;
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

        private bool global_plan_get = false;


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
                this.inters[i] = new Intersection(this.vissim, i + 1, this.dc, 
                    this.stpln_det_config[i], this.overflow_det_config[i], 
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
            foreach (var inter in this.inters) 
                inter.update_link_status();
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

        public bool lighten_signals(int cur_time)
        {
            bool need_new_plan = false;
            for (int i=0; i < this.inters.Length; i++)
                need_new_plan |= this.inters[i].lighten_signals(cur_time);

            return need_new_plan;
        }

        public void clear_signals()
        {
            foreach (var inter in this.inters)
                inter.clear_signals();
        }

        public void update_platoon()
        {
            //foreach (var inter in this.inters)
            //    inter.update_link_platoons();
        }

        public void merge_platoons()
        {
            foreach (var inter in this.inters)
                inter.merge_platoons();
        }

        public void remove_undetected_vehicle_in_platoons()
        {
            foreach (var inter in this.inters)
                inter.remove_undetected_vehicle_from_platoons();
        }

        //public void test_predict_signal()
        //{
        //    foreach (var inter in this.inters)
        //        inter.predict_signal_plan();
        //}

        public void predict_signal_plan_using_ga()
        {
            for (int i = 0; i < this.inters.Length; i++)
                this.inters[i].clear_predicted_arr_time();

            string res_str = GA.find_optimum(this.inters);
            for (int i = 0; i < this.inters.Length; i++)
            {
                string inter_plan_str = res_str.Substring(i * Globals.PRED_INT, Globals.PRED_INT);
                this.inters[i].set_plan_using_ga_result(inter_plan_str);
            }
            this.global_plan_get = true;
        }

        public bool get_global_plan_status()
        {
            return this.global_plan_get;
        }

        public void set_global_plan_status(bool valid_plan)
        {
            this.global_plan_get = valid_plan;
        }

        public void test_control_link_veh_speed(int inter_id, double target_speed)
        {
            this.inters[inter_id-1].test_control_link_veh_speed(this.vissim, target_speed);
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
