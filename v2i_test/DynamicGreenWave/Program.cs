﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VISSIMLIB;
using System.IO;
using System.Web;
using System.Threading;
using System.Diagnostics;



namespace DynamicGreenWave
{
    class Program
    {
        static ISimulation config_simulation(Vissim vissim, string s_inpx, string s_layx, int random_seed)
        {
            ISimulation simulation = vissim.Simulation;
            simulation.set_AttValue("SimRes", Globals.SIM_RES);
            simulation.set_AttValue("RandSeed", random_seed);
            simulation.set_AttValue("SimPeriod", Globals.SIM_TIME);
            simulation.set_AttValue("SimSpeed", Globals.SIM_SPEED);

            IEvaluation eva = vissim.Evaluation;
            eva.set_AttValue("DelaysCollectData", true);

            return simulation;
        }

        static void start_simulation(ISimulation simulation, Network net, int index)
        {
            int inter_num = Globals.INTER_NUM;
            int sim_time = Globals.SIM_TIME * Globals.SIM_RES;
            bool signals_cleared = false;

            for (int t = 0; t != sim_time; t++)
            {
                if (t == sim_time - 1)
                    break;
                
                simulation.RunSingleStep();

                if (!signals_cleared)
                {
                    net.clear_signals();
                    signals_cleared = true;
                }

                net.update_network_status();

                if (t % Globals.SIM_RES == 0)
                {
                    int cur_time = t / Globals.SIM_RES;

                    if (cur_time > Globals.WARM_UP_TIME && !net.get_global_plan_status())
                        net.predict_signal_plan_using_ga();
                    
                    if (net.lighten_signals(cur_time))
                        net.set_global_plan_status(false);

                    if (cur_time % 5 == 0)
                    {
                        net.remove_undetected_vehicle_in_platoons();
                        net.merge_platoons();
                    }
                }
            }
        }

        static void runVissimSimulation(string s_inpx, string s_layx, int sim_random_seed, int index)
        {
            Vissim vissim = new Vissim();
            vissim.LoadNet(s_inpx);
            vissim.LoadLayout(s_layx);
            ISimulation simulation = config_simulation(vissim, s_inpx, s_layx, sim_random_seed);
            Network net = new Network(vissim, Globals.INTER_NUM);

            start_simulation(simulation, net, index);
        }

        // This is pretty lame.
        static string get_test_flow_path()
        {
            string cur_path = System.IO.Directory.GetCurrentDirectory();
            string[] tmp = cur_path.Split('\\');

            string flow_path = "";
            for (int i = 0; i < tmp.Length - 4; i++)
                flow_path += tmp[i] + '\\';

            return flow_path;
        }

        static int Main(string[] args)
        {
            int seed = 20, index = 0;
            string test_flow_path = get_test_flow_path();
            string inpx = test_flow_path + Globals.test_flow_inpx;
            string layx = test_flow_path + Globals.test_flow_layx;
            
            runVissimSimulation(inpx, layx, seed, index);

            return 0;
        }
    }
}
