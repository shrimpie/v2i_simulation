using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VISSIMLIB;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DynamicGreenWave
{

    class Intersection
    {
        List<Intersection> neighbors = new List<Intersection>();
        private int[] avg_green_ext = { -1, -1 };
        private int[] predicted_queue_end = new int[Globals.THRU_LINK_NUM];
        private int _id;
        private HashSet<int> _phases_all = new HashSet<int>();
        private int[] _num_of_lanes = new int[Globals.LINK_NUM_ALL];
        private ISignalController _SC;
        private ISignalGroup[] _SG;

        private HashSet<int> cur_phases = new HashSet<int>();

        private string stpln_det_config_str = "";
        private string overflow_det_config_str = "";

        private IDetector[][] stpln_det;
        private int[][] stpln_det_vehno;
        private IDetector[][] overflow_det;
        private int[][] overflow_det_vehno;

        private int[] stpln_pass_veh_counter    = new int[Globals.SINGLE_PHASE_NUM];
        private int[] overflow_pass_veh_counter = new int[Globals.LINK_NUM_ALL];

        private int[] trap_counter = new int[Globals.LINK_NUM_ALL];
        // Trap_counter records vechile numbers in each trap
        private int[] link_length  = new int[Globals.THRU_LINK_NUM];
        private int[] link_connection = new int[Globals.LINK_CONNECT_D];
        
        private int[] queue_length = new int[Globals.THRU_LINK_NUM];
        // Queue length = queued number of vehicles / number of lanes
        // Notice this queue records only through phases, left-turn is recorded in trap_counter
        private int[] queue_num = new int[Globals.THRU_LINK_NUM];
        // Queued vehicle sum: how many vehicles are queued.

        private int[] link_sat_veh = new int[Globals.THRU_LINK_NUM];

        private int phase_index = 0;
        // Phase index init value: 0
        private int[] phase_last = new int[Globals.THRU_LINK_NUM];
        
        private int[] min_green_counter = new int[Globals.COMBI_PHASE_SUM];
        static private int[] MIN_GREEN  = null;

        class VehicleMine
        {
            double position;
            double speed;

            public VehicleMine(double p, double s)
            {
                position = p;
                speed = s;
            }

            public void update_position(double instant_speed, double queue_length)
            {
                if (instant_speed < 0) // THIS IS NOT LIKELY, BUT WHATEVER, JUST MAKE SURE THAT NEVER HAPPENS
                    instant_speed = 0;
                this.speed = instant_speed;

                // If a vehicle has a negative position, and it still remains in the link trap,
                // then it is considered in the queue, so keep minusing until its position becomes negative
                if (this.position > 0)
                    this.position -= instant_speed;
            }
            public double get_position()
            {
                return this.position;
            }
            public double get_speed()
            {
                return this.speed;
            }
            public void set_position(double p)
            {
                this.position = p;
            }
            public void set_speed(double s)
            {
                this.speed = s;
            }
        }
        private List<LinkedList<VehicleMine>> link_veh = new List<LinkedList<VehicleMine>>();
        
        // This List of HashSet of int is to record vehicle id of each link of the intersection.
        private List<HashSet<int>> inter_link_veh_set = new List<HashSet<int>>();

        private double[] link_avg_speed = new double[Globals.THRU_LINK_NUM];

        // This seems a little redundant, but it can extends to different value.
        private double[] link_avg_speed_start = { Globals.LINK_AVG_SPEED, Globals.LINK_AVG_SPEED, 
                                                  Globals.LINK_AVG_SPEED, Globals.LINK_AVG_SPEED };

        public int get_id()
        {
            return this._id;
        }

        public void clear_signals()
        {
            foreach (var p in this._phases_all)
                this._SG[(int)p - 1].set_AttValue("State", "RED");
        }

        public int[] get_link_sat_veh()
        {
            int[] sat_veh_count = new int[Globals.THRU_LINK_NUM];
            for (int i = 0; i < sat_veh_count.Length; i++)
                sat_veh_count[i] = this.link_sat_veh[i];
            return sat_veh_count;
        }

        public int[] get_link_veh_count()
        {
            int[] veh_count = new int[Globals.THRU_LINK_NUM];
            for (int i = 0; i < veh_count.Length; ++i)
                veh_count[i] = this.link_veh[i].Count;

            return veh_count;
        }

        public int[] get_cur_queue_count()
        {
            int[] queue_count = new int[Globals.THRU_LINK_NUM];
            for (int i = 0; i < queue_count.Length; i++)
                queue_count[i] = this.queue_num[i];
            return queue_count;
        }

        public void init_neighbors(Intersection[] inters)
        {
            for (int i = 0; i < this.link_connection.Length; i += Globals.LINK_INFO_D)
            {
                int index = this.link_connection[i];
                if (index != -1)
                    this.neighbors.Add(inters[index]);
                else
                    this.neighbors.Add(null);
            }
        }

        private void config_det_str(string posi, string tgt_str)
        {
            if (posi == "stpln")
                this.stpln_det_config_str = tgt_str;
            else if (posi == "overflow")
                this.overflow_det_config_str = tgt_str;
        }

        private void set_valid_phases(int num, int[] ps, ISignalGroupContainer sgc)
        {
            for (int j = 0; j < num; j++)
            {
                this._SG[j] = sgc.get_ItemByKey(ps[j]);
                this._phases_all.Add(ps[j]);
            }
        }

        private void configure_phases_all()
        {
            ISignalGroupContainer signalGroupContainer = this._SC.SGs;
            this._SG = new ISignalGroup[Globals.SINGLE_PHASE_NUM];
            
            int[] valid_phases = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
            this.set_valid_phases(Globals.SINGLE_PHASE_NUM, valid_phases, signalGroupContainer);
        }

        private void config_det(IDetectorContainer dc, string det_name, int[] real_key)
        {
            int det_num     = (det_name == "stpln_det") ? Globals.SINGLE_PHASE_NUM : Globals.LINK_NUM_ALL;
            string config_s = (det_name == "stpln_det") ? this.stpln_det_config_str : this.overflow_det_config_str;
            string[] det_of_phase = config_s.Split('|');
            bool config_lanes = (det_name == "overflow_det") ? true : false;

            for (int i = 0; i < det_num; i++)
            {
                string[] det_key = det_of_phase[i].Split(',');

                // '*' was for three-legged intersections and stuff,
                // this experiment doesn't have one, the compare is kept anyway. 
                if (config_lanes && det_key[0] != "*")
                    this._num_of_lanes[i] = det_key.Length;
                
                if (det_key[0] != "*")
                {
                    IDetector[] tmp_det = new IDetector[det_key.Length];
                    int[] tmp_vehno = new int[det_key.Length];

                    for (int j = 0; j < det_key.Length; j++)
                    {
                        int key = Int32.Parse(det_key[j]);
                        int key2 = real_key[key - 1];
                        tmp_det[j] = dc.get_ItemByKey(key2);
                        //string name = tmp_det[j].get_AttValue("NAME");
                        //int id = tmp_det[j].get_AttValue("PORTNO");
                    }

                    if (det_name == "stpln_det")
                    {
                        this.stpln_det[i] = tmp_det;
                        this.stpln_det_vehno[i] = tmp_vehno;
                    }
                    else if (det_name == "overflow_det")
                    {
                        this.overflow_det[i] = tmp_det;
                        this.overflow_det_vehno[i] = tmp_vehno;
                    }                    
                }
            }
        }

        private void init_link_veh_list()
        {
            for (int i = 0; i < Globals.THRU_LINK_NUM; i++)
            {
                this.link_veh.Add(new LinkedList<VehicleMine>());
                this.inter_link_veh_set.Add(new HashSet<int>());
            }
        }

        private void init_link_length(int[,] inter_g)
        {
            for (int i = 0; i < this.link_length.Length; i++)
                this.link_length[i] = inter_g[this._id - 1, 3 * i + 1];
        }

        private void init_link_avg_speed()
        {
            for (int i = 0; i < this.link_avg_speed.Length; i++)
                this.link_avg_speed[i] = Globals.LINK_AVG_SPEED;
        }

        private void init_min_green()
        {
            MIN_GREEN = new int[] { Globals.MIN_THRU_GREEN, Globals.MIN_LEFT_GREEN, 
                                    Globals.MIN_THRU_GREEN, Globals.MIN_LEFT_GREEN };
            for (int i = 0; i < this.min_green_counter.Length; i++)
                this.min_green_counter[i] = MIN_GREEN[i];
        }

        private void init_link_sat_flow()
        {
            for(int i = 0; i < this.link_sat_veh.Length; i++)
            {
                int lane_index = i < 2 ? i : i + 2;
                this.link_sat_veh[i] = (int) ((double) this.link_length[i] / Globals.AVG_VEHICLE_DISTANCE * this._num_of_lanes[lane_index] + 0.5);
            }
        }

        private void init_link_connection(int[,] connection)
        {
            for (int i = 0; i < this.link_connection.Length; i++)
                this.link_connection[i] = connection[this._id - 1, i];
        }

        public Intersection(int id, IDetectorContainer dc, string stpln_det_str, string overflow_det_str, 
                             ISignalController SC, int[,] inter_graph, int[] real_key)
        {
            this._id = id;
            this.stpln_det = new IDetector[Globals.SINGLE_PHASE_NUM][];
            this.stpln_det_vehno = new int[Globals.SINGLE_PHASE_NUM][];
            this.overflow_det = new IDetector[Globals.LINK_NUM_ALL][];
            this.overflow_det_vehno = new int[Globals.LINK_NUM_ALL][];

            this._SC = SC;
            this.configure_phases_all();

            this.config_det_str("stpln", stpln_det_str);
            this.config_det_str("overflow", overflow_det_str);
            this.config_det(dc, "stpln_det", real_key);
            this.config_det(dc, "overflow_det", real_key);

            this.clear_veh_counter();
            this.init_link_length(inter_graph);
            this.init_link_sat_flow();
            this.init_link_veh_list();            
            this.init_link_avg_speed();
            this.init_min_green();
            this.init_link_connection(inter_graph);
        }

        public void update_veh_pos()
        {
            for (int i = 0; i < this.link_veh.Count; i++)
            {
                foreach (var veh in this.link_veh[i])
                    veh.update_position(this.link_avg_speed[i], this.queue_length[i]);
            }
        }

        private void clear_veh_counter()
        {
            for (int i = 0; i < Globals.LINK_NUM_ALL; i++)
            {
                this.trap_counter[i] = 0;
                this.overflow_pass_veh_counter[i] = 0;
            }
            for (int i = 0; i < Globals.SINGLE_PHASE_NUM; i++)
                this.stpln_pass_veh_counter[i] = 0;
        }

        private void update_stpln_counter()
        {
            for (int i = 0; i < this.stpln_det.Length; i++)
            {
                if (this.stpln_det[i] != null)
                {
                    for (int j = 0; j < this.stpln_det[i].Length; j++)
                    {
                        int veh_id = this.stpln_det[i][j].get_AttValue("VEHNO");
                        //string name = this.stpln_det[i][j].get_AttValue("NAME");
                        int id = this.stpln_det[i][j].get_AttValue("PORTNO");
                        if (veh_id != 0 && veh_id != this.stpln_det_vehno[i][j])
                        {
                            this.stpln_pass_veh_counter[i]++;
                            this.stpln_det_vehno[i][j] = veh_id;
                            //this.update_link_veh_from_stopline_det(i);

                            this.update_link_veh_set_from_stopline_det(i, veh_id);
                        }
                    }
                }
            }
        }

        private bool parallel_detector_seen_this_car(int veh_id, int[] det_vehno)
        {
            foreach (var id in det_vehno)
                if (id == veh_id)
                    return true;
            return false;
        }

        private void update_link_speed(int ph, double speed)
        {
            // This avg speed calculation is not really avg of all speeds, it emphasizes more new speeds,
            // So lets pretend it will work as a good approximation of previous detected vehicles
            if (ph + 1 == Globals.EB_THRU || ph + 1 == Globals.WB_THRU)
                this.link_avg_speed[ph] = (this.link_avg_speed[ph] + speed) / 2;
            else if (ph + 1 == Globals.NB_THRU || ph + 1 == Globals.SB_THRU)
                this.link_avg_speed[ph - 2] = (this.link_avg_speed[ph - 2] + speed) / 2;
        }

        private void update_overflow_counter_and_link_speed()
        {
            for (int i = 0; i < this.overflow_det.Length; i++)
            {
                if (this.overflow_det[i] != null)
                {
                    for (int j = 0; j < this.overflow_det[i].Length; j++)
                    {
                        int veh_id = this.overflow_det[i][j].get_AttValue("VEHNO");

                        if (veh_id != 0 && !this.parallel_detector_seen_this_car(veh_id, this.overflow_det_vehno[i]))
                        {
                            this.overflow_pass_veh_counter[i]++;
                            this.overflow_det_vehno[i][j] = veh_id;
                            //double instant_speed = this.overflow_det[i][j].get_AttValue("VEHSPEED");
                            //double veh_speed = Math.Round(instant_speed / 3.6, 2);

                            //this.update_link_speed(i, veh_speed);
                            //this.update_link_veh_from_overflow_det(i, j, veh_speed);

                            this.update_link_veh_set_from_overflow_det(i, veh_id);
                        }
                    }
                }
            }
        }

        private void record_each_trap(int phase)
        {
            int front = 0;
            int end = 0;

            switch (phase + 1)
            {
                case Globals.EB_THRU:
                case Globals.WB_THRU:
                    front = this.overflow_pass_veh_counter[phase];
                    end = this.stpln_pass_veh_counter[phase] + this.overflow_pass_veh_counter[phase + 2] +
                            this.stpln_pass_veh_counter[phase + 8];
                    this.trap_counter[phase] = front > end ? front - end : 0;
                    break;
                case Globals.NB_LEFT:
                case Globals.SB_LEFT:
                case Globals.WB_LEFT:
                case Globals.EB_LEFT:
                    front = this.overflow_pass_veh_counter[phase];
                    end = this.stpln_pass_veh_counter[phase];
                    this.trap_counter[phase] = front > end ? front - end : 0;
                    break;
                case Globals.NB_THRU:
                case Globals.SB_THRU:
                    front = this.overflow_pass_veh_counter[phase];
                    end = this.stpln_pass_veh_counter[phase] + this.overflow_pass_veh_counter[phase + 2] +
                            this.stpln_pass_veh_counter[phase + 6];
                    this.trap_counter[phase] = front > end ? front - end : 0;
                    break;
            }

        }

        public void update_link_status()
        {
            this.update_stpln_counter();
            this.update_overflow_counter_and_link_speed();

            //foreach (var p in this._phases_all)
                //this.record_each_trap((int)p - 1);
        }

        private bool direction_is_thru(int dir)
        {
            if (dir + 1 == Globals.EB_THRU || dir + 1 == Globals.WB_THRU || dir + 1 == Globals.SB_THRU || dir + 1 == Globals.NB_THRU)
                return true;
            return false;
        }
        private bool direction_is_left(int dir)
        {
            if (dir + 1 == Globals.EB_LEFT || dir + 1 == Globals.WB_LEFT || dir + 1 == Globals.SB_LEFT || dir + 1 == Globals.NB_LEFT)
                return true;
            return false;
        }

        // 
        private void update_link_veh_set_from_overflow_det(int ph, int veh_id)
        {
            // For vehicles newly come in the links, add them to the set.
            if (this.direction_is_thru(ph))
            {
                int index = ph < 2 ? ph : ph - 2;
                this.inter_link_veh_set[index].Add(veh_id);
            }
            // For those turn into left-turn bay, remove them.
            else if (this.direction_is_left(ph))
            {
                int index = ph > 3 ? ph - 4 : ph - 2;
                this.inter_link_veh_set[index].Remove(veh_id);
            }
        }

        private void update_link_veh_from_overflow_det(int ph, int which_det, double speed)
        {
            // Link start, vehicle entry, add to list
            if (this.direction_is_thru(ph))
            {
                // Add vehicle to link_veh
                double pos = Math.Round(this.overflow_det[ph][which_det].get_AttValue("POS"), 2);
                double link_length = Math.Round(this.overflow_det[ph][which_det].Lane.Link.get_AttValue("LENGTH2D"), 2);
                pos = link_length - pos;
                VehicleMine temp_veh = new VehicleMine(pos, speed);
                int index = ph < 2 ? ph : ph - 2;
                this.link_veh[index].AddLast(temp_veh);
                
            }
            // Link left turn vehicle entry, remove from list, if vehile number is included, this is simple, 
            // But it's cheating then, so find the one that has a similar position, remove it from the list
            else if (this.direction_is_left(ph))
            {
                // This position is where the left-turn overflow detector is
                double overflow_pos = Math.Round(this.overflow_det[ph][which_det].get_AttValue("POS"), 2);
                double left_turn_signal_pos = Math.Round(this.stpln_det[ph][which_det].get_AttValue("POS"), 2);
                double target_distance = left_turn_signal_pos - overflow_pos;

                int index = ph > 3 ? ph - 4 : ph - 2;

                // Remove the vehicle from least according to its position
                VehicleMine target = null;
                double min = Globals.MAX_LINK_LENGH;
                foreach (var veh in this.link_veh[index])
                {
                    double diff = Math.Abs(veh.get_position() - target_distance);
                    if (diff < min)
                    {
                        min = diff;
                        target = veh;
                    }
                }
                if(target != null)
                    this.link_veh[index].Remove(target);
            }
        }

        private void update_link_veh_set_from_stopline_det(int ph, int veh_id)
        {
            switch (ph + 1)
            {
                case Globals.EB_THRU:
                case Globals.SB_RIGHT:
                    this.inter_link_veh_set[0].Remove(veh_id);
                    break;
                case Globals.WB_THRU:
                case Globals.NB_RIGHT:
                    this.inter_link_veh_set[1].Remove(veh_id);
                    break;
                case Globals.NB_THRU:
                case Globals.EB_RIGHT:
                    this.inter_link_veh_set[2].Remove(veh_id);
                    break;
                case Globals.SB_THRU:
                case Globals.WB_RIGHT:
                    this.inter_link_veh_set[3].Remove(veh_id);
                    break;
                default:
                    break;
            }
        }

        private void update_link_veh_from_stopline_det(int ph)
        {
            switch (ph+1)
            {
                case Globals.EB_THRU:
                case Globals.SB_RIGHT:
                    if (this.link_veh[0].Count > 0) this.link_veh[0].RemoveFirst();
                    break;
                case Globals.WB_THRU:
                case Globals.NB_RIGHT:
                    if (this.link_veh[1].Count > 0) this.link_veh[1].RemoveFirst();
                    break;
                case Globals.NB_THRU:
                case Globals.EB_RIGHT:
                    if (this.link_veh[2].Count > 0) this.link_veh[2].RemoveFirst();
                    break;
                case Globals.SB_THRU:
                case Globals.WB_RIGHT:
                    if (this.link_veh[3].Count > 0) this.link_veh[3].RemoveFirst();
                    break;
                default:
                    break;
            }
        }


        // This one is for queue update, where link_no is for 4 directions, not phase index
        private bool thru_phase_is_green(int link_no)
        {
            if ((this.phase_index == 0 && (link_no == 0 || link_no == 1)) ||
                 (this.phase_index == 2 && (link_no == 2 || link_no == 3)))
                return true;
            return false;
        }

        private void lighten_normal_phases(HashSet<int> phases)
        {
            foreach (var p in this.cur_phases)
                if (!phases.Contains(p))
                    this._SG[(int)p - 1].set_AttValue("State", "RED");

            foreach (var p in phases)
                if (!this.cur_phases.Contains(p))
                    this._SG[(int)p - 1].set_AttValue("State", "GREEN");

            this.cur_phases = phases;
        }

        private int get_phase_index_using_in_cycle_time(int in_cycle_time)
        {
            int index = 0;
            if (in_cycle_time < Globals.FIXED_CYCLE / 3)
                index = 0;
            else if (in_cycle_time < Globals.FIXED_CYCLE / 2)
                index = 1;
            else if (in_cycle_time < 5 * Globals.FIXED_CYCLE / 6)
                index = 2;
            else if (in_cycle_time < Globals.FIXED_CYCLE)
                index = 3;
            return index;
        }

        public void lighten_signals(int cur_time)
        {
            int in_cycle_time = cur_time % Globals.FIXED_CYCLE;
            this.phase_index = this.get_phase_index_using_in_cycle_time(in_cycle_time);

            for (int i = 0; i < Globals.COMBI_PHASE_SUM; i++)
                this.lighten_normal_phases(Globals.FIXED_PHASES[this.phase_index]);
        }

        public void control_link_veh_speed(Vissim vis, double target_speed)
        {
            for (int i = 0; i < this.inter_link_veh_set.Count; i++)
            {
                foreach (var veh_id in this.inter_link_veh_set[i])
                {
                    IVehicle veh = vis.Net.Vehicles.get_ItemByKey(veh_id);
                    double speed = veh.get_AttValue("SPEED");
                    veh.set_AttValue("SPEED", target_speed);
                }
            }
        }
       

    }
}
