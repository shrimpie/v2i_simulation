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
        Vissim vis = null;
        List<Intersection> neighbors = new List<Intersection>();
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

        private int[] decision = null;
        private int[] link_sat_veh = new int[Globals.THRU_LINK_NUM];
        private double[] link_len = new double[Globals.THRU_LINK_NUM];
        private bool[] link_len_set = new bool[Globals.THRU_LINK_NUM];
        private int[] link_id = new int[Globals.THRU_LINK_NUM];

        private bool signal_plan_calculated = false;
        private int tmp_plan_counter = 0;

        private int phase_index = 0;
        // Phase index init value: 0
        private int[] phase_last = new int[Globals.THRU_LINK_NUM];

        private List<Tuple<int, int>> signal_plan = new List<Tuple<int, int>>();

        class VehicleMini
        {
            double position;
            double speed;
            int id;

            // (If uses detector, id won't get recorded.)
            // id is used to sort link vehicles according to their positions and then form platoons.
            public VehicleMini(double p, double s=0.0, int id=0)
            {
                this.position = p;
                this.speed = s;
                this.id = id;
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

            public void set_id(int id)
            {
                this.id = id;
            }
            public int get_id()
            {
                return this.id;
            }
        }
        private List<LinkedList<VehicleMini>> link_veh = new List<LinkedList<VehicleMini>>();

        class PlatoonMine
        {
            int inter_id;
            int phase_id;
            int init_link_id;
            // Above three is used to debug.
            // Detectors could miss a single vehicle, thus it can not be deleted from the platoon.
            // If the platoon info is not right, its update won't excecute properly.

            double link_len;
            double advice_speed = 0.0;
            List<IVehicle> vehicles = new List<IVehicle>();

            public PlatoonMine(int inter_id, int phase_id, int link_id, double link_len)
            {
                this.inter_id = inter_id;
                this.phase_id = phase_id;
                this.init_link_id = link_id;
                this.link_len = link_len;
            }

            public void set_advice_speed(double speed)
            {
                this.advice_speed = speed;
                foreach (var veh in this.vehicles)
                    veh.set_AttValue("SPEED", speed);
            }

            public double get_advice_speed()
            {
                return this.advice_speed;
            }

            public void set_link_len(double link_len)
            {
                this.link_len = link_len;
            }

            public double get_link_len()
            {
                return this.link_len;
            }

            public int get_link_id()
            {
                return this.init_link_id;
            }

            public double get_start_pos()
            {
                if (this.vehicles.Count > 0)
                    return this.link_len - this.vehicles[0].get_AttValue("POS");
                return 0.0;
            }

            public double get_end_pos()
            {
                if (this.vehicles.Count > 0)
                    return this.link_len - this.vehicles[this.vehicles.Count-1].get_AttValue("POS");
                return 0.0;
            }


            public void add_vehicle_to_platoon(IVehicle veh)
            {
                this.vehicles.Add(veh);
            }

            public void remove_vehicle(int veh_id)
            {
                foreach(var veh in this.vehicles)
                {
                    if (veh.get_AttValue("NO") == veh_id)
                    {
                        this.vehicles.Remove(veh);
                        return;
                    }
                }
            }

            public List<IVehicle> get_vehicles()
            {
                return this.vehicles;
            }

        }

        // This List of HashSet of int is to record vehicle id of each link of the intersection.
        private List<HashSet<int>> inter_link_veh_set = new List<HashSet<int>>();

        private List<List<PlatoonMine>> link_platoon = new List<List<PlatoonMine>>();


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
                this.link_veh.Add(new LinkedList<VehicleMini>());
                this.inter_link_veh_set.Add(new HashSet<int>());
                this.link_platoon.Add(new List<PlatoonMine>());
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

        private void init_link_sat_flow()
        {
            for(int i = 0; i < this.link_sat_veh.Length; i++)
            {
                int lane_index = i < 2 ? i : i + 2;
                this.link_sat_veh[i] = (int) ((double) this.link_length[i] / Globals.AVG_VEHICLE_DISTANCE * this._num_of_lanes[lane_index] + 0.5);
            }
        }

        private void init_link_len_set()
        {
            for (int i = 0; i < this.link_len_set.Length; i++)
                this.link_len_set[i] = false;
        }

        private void init_link_connection(int[,] connection)
        {
            for (int i = 0; i < this.link_connection.Length; i++)
                this.link_connection[i] = connection[this._id - 1, i];
        }

        public Intersection(Vissim vis, int id, IDetectorContainer dc, string stpln_det_str, string overflow_det_str, 
                             ISignalController SC, int[,] inter_graph, int[] real_key)
        {
            this.vis = vis;
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
            this.init_link_connection(inter_graph);

            this.init_link_len_set();
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
                            this.update_link_veh_from_stopline_det(i);

                            this.update_link_veh_set_from_stopline_det(i, veh_id);
                            this.update_link_platoon_from_stopline_det(i, veh_id);
                        }
                    }
                }
            }
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

                        if (veh_id != 0 && !Utils.parallel_detector_seen_this_car(veh_id, this.overflow_det_vehno[i]))
                        {
                            this.overflow_pass_veh_counter[i]++;
                            this.overflow_det_vehno[i][j] = veh_id;
                            double instant_speed = this.overflow_det[i][j].get_AttValue("VEHSPEED");
                            double veh_speed = Math.Round(instant_speed / 3.6, 2);

                            this.update_link_speed(i, veh_speed);
                            this.update_link_veh_from_overflow_det(i, j, veh_speed);

                            this.update_link_veh_set_from_overflow_det(i, veh_id);
                            this.update_link_platoon_from_overflow_det(i, veh_id);
                        }
                    }
                }
            }
        }

        private void update_link_platoon_from_overflow_det(int ph, int veh_id)
        {
            // For vehicles newly come in the links, add them to the existing platoon or create a new one.
            if (Utils.direction_is_thru(ph))
            {
                IVehicle veh = this.vis.Net.Vehicles.get_ItemByKey(veh_id);
                int index = ph < 2 ? ph : ph - 2;
                double pos = veh.get_AttValue("POS");
                if (!this.link_len_set[index])
                {
                    this.link_len[index] = veh.Lane.Link.get_AttValue("LENGTH2D");
                    this.link_id[index] = veh.Lane.Link.get_AttValue("NO");
                    this.link_len_set[index] = true;
                }
                double veh_pos = this.link_len[index] - pos;

                // Just compare with the last platoon
                int platoon_count = this.link_platoon[index].Count;
                if (platoon_count > 0)
                {
                    PlatoonMine last = this.link_platoon[index][platoon_count - 1];
                    double end_pos = last.get_end_pos();
                    if (veh_pos - end_pos <= Globals.PLATOON_HDWAY_DISTANCE)
                    {
                        last.add_vehicle_to_platoon(veh);
                        return;
                    }
                }
                // If the veh is not near any platoon, add a new one.
                PlatoonMine tmp = new PlatoonMine(this._id, index, this.link_id[index], this.link_len[index]);
                tmp.add_vehicle_to_platoon(veh);
                this.link_platoon[index].Add(tmp);
            }
            // For those turn into left-turn bay, remove it from existing platoon.
            // This enumeration is not efficient. PLEASE MAKE IT BETTER.
            else if (Utils.direction_is_left(ph))
            {
                int index = ph > 3 ? ph - 4 : ph - 2;
                this.remove_veh_from_platoon(index, veh_id);
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

            foreach (var p in this._phases_all)
                this.record_each_trap((int)p - 1);
        }

        private void update_link_veh_set_from_overflow_det(int ph, int veh_id)
        {
            // For vehicles newly come in the links, add them to the set.
            if (Utils.direction_is_thru(ph))
            {
                int index = ph < 2 ? ph : ph - 2;
                this.inter_link_veh_set[index].Add(veh_id);
            }
            // For those turn into left-turn bay, remove them.
            else if (Utils.direction_is_left(ph))
            {
                int index = ph > 3 ? ph - 4 : ph - 2;
                this.inter_link_veh_set[index].Remove(veh_id);
            }
        }

        private void update_link_veh_from_overflow_det(int ph, int which_det, double speed)
        {
            // Link start, vehicle entry, add to list
            if (Utils.direction_is_thru(ph))
            {
                // Add vehicle to link_veh
                double pos = Math.Round(this.overflow_det[ph][which_det].get_AttValue("POS"), 2);
                double link_length = Math.Round(this.overflow_det[ph][which_det].Lane.Link.get_AttValue("LENGTH2D"), 2);
                pos = link_length - pos;
                VehicleMini temp_veh = new VehicleMini(pos, speed);
                int index = ph < 2 ? ph : ph - 2;
                this.link_veh[index].AddLast(temp_veh);
                
            }
            // Link left turn vehicle entry, remove from list, if vehile number is included, this is simple, 
            // But it's cheating then, so find the one that has a similar position, remove it from the list
            else if (Utils.direction_is_left(ph))
            {
                // This position is where the left-turn overflow detector is
                double overflow_pos = Math.Round(this.overflow_det[ph][which_det].get_AttValue("POS"), 2);
                double left_turn_signal_pos = Math.Round(this.stpln_det[ph][which_det].get_AttValue("POS"), 2);
                double target_distance = left_turn_signal_pos - overflow_pos;

                int index = ph > 3 ? ph - 4 : ph - 2;

                // Remove the vehicle from least according to its position
                VehicleMini target = null;
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

        private void remove_veh_from_platoon(int ph, int veh_id)
        {
            IVehicle veh = this.vis.Net.Vehicles.get_ItemByKey(veh_id);
            int index = ph;
            foreach (var platoon in this.link_platoon[ph])
            {
                foreach (var v in platoon.get_vehicles())
                {
                    int v_id = v.get_AttValue("NO");
                    // If vehicle exits link detected by detectors
                    if (v_id == veh_id)
                    {
                        platoon.remove_vehicle(veh_id);
                        if (platoon.get_vehicles().Count < 1)
                            this.link_platoon[ph].Remove(platoon);
                        return;
                    }
                }
            }
        }

        private void update_link_platoon_from_stopline_det(int ph, int veh_id)
        {
            switch (ph + 1)
            {
                case Globals.EB_THRU:
                case Globals.SB_RIGHT:
                    this.remove_veh_from_platoon(0, veh_id);
                    break;
                case Globals.WB_THRU:
                case Globals.NB_RIGHT:
                    this.remove_veh_from_platoon(1, veh_id);
                    break;
                case Globals.NB_THRU:
                case Globals.EB_RIGHT:
                    this.remove_veh_from_platoon(2, veh_id);
                    break;
                case Globals.SB_THRU:
                case Globals.WB_RIGHT:
                    this.remove_veh_from_platoon(3, veh_id);
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

        private void set_platoon_advice_speed(List<List<double>> tgt_platoon_speed)
        {
            for (int i = 0; i < tgt_platoon_speed.Count; i++)
            {
                for (int j = 0; j < tgt_platoon_speed[i].Count; j++)
                    this.link_platoon[i][j].set_advice_speed(tgt_platoon_speed[i][j]);
            }
        }

        private int[] append_left_phase()
        {
            List<int> plan = new List<int>();
            for (int i = 0; i < this.decision.Length; i++)
                plan.Add(this.decision[i]);
            if (this.trap_counter[2] > 0 || this.trap_counter[3] > 0)
                plan.Add(Globals.NSB_LEFT);
            if (this.trap_counter[6] > 0 || this.trap_counter[7] > 0)
                plan.Add(Globals.EWB_LEFT);

            int[] res_plan = new int[plan.Count];
            for (int i = 0; i < plan.Count; i++)
                res_plan[i] = plan[i];
            return res_plan;
        }

        public void predict_signal_plan()
        {
            List<List<Tuple<int, int, int, double>>> predicted_arr_time = new List<List<Tuple<int, int, int, double>>>();
            for (int i = 0; i < this.link_platoon.Count; i++)
            {
                predicted_arr_time.Add(new List<Tuple<int, int, int, double>>());
                for (int j = 0; j < this.link_platoon[i].Count; j++)
                {
                    List<IVehicle> pla = this.link_platoon[i][j].get_vehicles();
                    if (pla.Count > 0)
                    {
                        double start_speed = pla[0].get_AttValue("SPEED");
                        int pla_size = pla.Count;
                        // If these vehicles are in queue.
                        if (start_speed < Globals.QUEUE_SPEED)
                        {
                            int start_arr_t = 0;
                            int lane_index = i > 2 ? i + 2 : i;
                            double flow_rate = Globals.SAT_FLOW_RATE * this._num_of_lanes[lane_index];
                            int end_arr_t = (int)(pla_size / flow_rate + 0.5);
                            if (end_arr_t < Globals.PRED_TIME)
                                predicted_arr_time[i].Add(new Tuple<int, int, int, double>(start_arr_t, end_arr_t, pla_size, start_speed));
                        }
                        else
                        {
                            //if (start_speed < Globals.CLAER_INTER_SPEED)
                            //    start_speed = Globals.CLAER_INTER_SPEED;
                            double start_pos = this.link_platoon[i][j].get_start_pos();
                            double end_pos = this.link_platoon[i][j].get_start_pos();
                            int start_arr_t = (int)(3.6 * start_pos / start_speed + 0.5);
                            int end_arr_t = (int)(3.6 * end_pos / start_speed + 0.5);
                            if( end_arr_t < Globals.PRED_TIME)
                                predicted_arr_time[i].Add(new Tuple<int, int, int, double>(start_arr_t, end_arr_t, pla_size, start_speed));
                        }
                    }
                    else
                        continue;
                }
            }
            List<List<double>> tgt_platoon_speed = new List<List<double>>();
            for (int i = 0; i < 4; i++) tgt_platoon_speed.Add(new List<double>());

            this.decision = Utils.make_signal_predictions2(predicted_arr_time, tgt_platoon_speed);
            this.set_platoon_advice_speed(tgt_platoon_speed);
            this.decision = this.append_left_phase();
            //this.decision = Utils.add_clear_red(this.decision);
            this.signal_plan_calculated = true;
        }

        public void lighten_signals(int cur_time)
        {
            if (cur_time < Globals.WARM_UP_TIME)
            {
                int in_cycle_time = cur_time % Globals.FIXED_CYCLE;
                this.phase_index = Utils.get_phase_index_using_in_cycle_time(in_cycle_time);

                for (int i = 0; i < Globals.COMBI_PHASE_SUM; i++)
                    this.lighten_normal_phases(Globals.FIXED_PHASES[this.phase_index]);
            }
            else if (!this.signal_plan_calculated)
                this.predict_signal_plan();

            if (this.signal_plan_calculated)
            {
                if (this.tmp_plan_counter < this.decision.Length * Globals.TIME_INT)
                {
                    int phase_index = this.decision[this.tmp_plan_counter / Globals.TIME_INT];
                    this.lighten_normal_phases(Globals.FIXED_PHASES[phase_index]);
                    this.tmp_plan_counter++;
                }
                else
                {
                    this.tmp_plan_counter = 0;
                    this.signal_plan_calculated = false;
                    this.predict_signal_plan();
                }
            }
        }

        /*
        public void update_link_platoons()
        {
            // link_platoon and inter_link_veh_set have the same length.
            // Then sort the list of vehicles and count platoons.
            for (int i = 0; i < this.link_platoon.Count; i++)
            {
                if (this.link_platoon[i].Count < 1)
                {
                    if (this.inter_link_veh_set[i].Count > 0)
                    {
                        List<VehicleMini> veh_list = new List<VehicleMini>();
                        foreach (var veh_id in this.inter_link_veh_set[i])
                        {
                            IVehicle veh = this.vis.Net.Vehicles.get_ItemByKey(veh_id);
                            double pos = veh.get_AttValue("POS");
                            if (!this.link_len_set[i])
                            {
                                this.link_len[i] = veh.Lane.Link.get_AttValue("LENGTH2D");
                                this.link_id[i] = veh.Lane.Link.get_AttValue("NO");
                                this.link_len_set[i] = true;
                            }
                            veh_list.Add(new VehicleMini(this.link_len[i] - pos, 0.0, veh_id));
                        }
                        veh_list.OrderBy(x => x.get_position());

                        if (veh_list.Count > 1)
                        {
                            int last_index = 0;
                            for (int j = 1; j < veh_list.Count; j++)
                            {
                                while (veh_list[j].get_position() - veh_list[j - 1].get_position() < Globals.PLATOON_HDWAY_DISTANCE)
                                    j++;
                                PlatoonMine tmp = new PlatoonMine(this._id, i, this.link_id[i], this.link_len[i]);
                                for (int index = last_index; index <= j; index++)
                                {
                                    tmp.add_vehicle_to_platoon(this.vis.Net.Vehicles.get_ItemByKey(veh_list[index]));
                                }
                                this.link_platoon[i].Add(tmp);
                            }
                        }
                        else if(veh_list.Count == 1)
                        {
                            PlatoonMine tmp = new PlatoonMine(this._id, i, this.link_id[i], this.link_len[i]);
                            tmp.add_vehicle_to_platoon(this.vis.Net.Vehicles.get_ItemByKey(veh_list[0]));
                            this.link_platoon[i].Add(tmp);
                        }
                    }
                }
            }
        }
        */

        // This merge thing is pretty costly.
        // You have to make it better.
        public void merge_platoons()
        {
            for (int i = 0; i < this.link_platoon.Count; i++)
            {
                // Try merging when having more than 1 platoons.
                if (this.link_platoon[i].Count > 1)
                {
                    int leader = 0;
                    int j = 1;

                    while (j < this.link_platoon[i].Count)
                    {
                        double front_platoon_start = this.link_platoon[i][leader].get_start_pos();
                        double front_platoon_end = this.link_platoon[i][leader].get_end_pos();
                        double cur_front = this.link_platoon[i][j].get_start_pos();

                        if (Math.Abs(cur_front - front_platoon_start) <= Globals.PLATOON_HDWAY_DISTANCE ||
                            Math.Abs(cur_front - front_platoon_end) <= Globals.PLATOON_HDWAY_DISTANCE)
                        {
                            foreach (var veh in this.link_platoon[i][j].get_vehicles())
                            {
                                this.link_platoon[i][leader].add_vehicle_to_platoon(veh);
                            }
                            this.link_platoon[i].RemoveAt(j);
                            this.link_platoon[i][leader].get_vehicles().OrderBy(x => -x.get_AttValue("POS"));
                        }
                        else
                        {
                            leader += 1;
                            j++;
                        }

                    }
                }
            }
        }

        public void remove_undetected_vehicle_from_platoons()
        {
            for (int i = 0; i < this.link_platoon.Count; i++)
            {
                int platoon_link_id = this.link_id[i];

                for (int j = 0; j < this.link_platoon[i].Count; j++)
                {
                    List<IVehicle> vehs = this.link_platoon[i][j].get_vehicles();
                    if(vehs.Count < 2)
                    {
                        for (int k = 0; k < vehs.Count; k++)
                        {
                            int veh_link_id = vehs[k].Lane.Link.get_AttValue("NO");
                            if (platoon_link_id != veh_link_id)
                            {
                                int veh_id = vehs[k].get_AttValue("NO");
                                this.link_platoon[i][j].remove_vehicle(veh_id);
                            }
                        }
                    }
                }
            }
        }

        public void test_control_link_veh_speed(Vissim vis, double target_speed)
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
