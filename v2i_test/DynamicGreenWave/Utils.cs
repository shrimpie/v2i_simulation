using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicGreenWave
{
    class Utils
    {
        private static readonly Random getrandom = new Random();
        private static readonly object syncLock = new object();

        // include 'min', not 'max'
        public static int get_random_number(int min, int max)
        {
            lock (syncLock)
            { // synchronize
                return getrandom.Next(min, max);
            }
        }

        static public bool direction_is_thru(int dir)
        {
            if (dir + 1 == Globals.EB_THRU || dir + 1 == Globals.WB_THRU ||
                dir + 1 == Globals.SB_THRU || dir + 1 == Globals.NB_THRU)
                return true;
            return false;
        }

        static public bool direction_is_left(int dir)
        {
            if (dir + 1 == Globals.EB_LEFT || dir + 1 == Globals.WB_LEFT ||
                dir + 1 == Globals.SB_LEFT || dir + 1 == Globals.NB_LEFT)
                return true;
            return false;
        }

        static public bool parallel_detector_seen_this_car(int veh_id, int[] det_vehno)
        {
            foreach (var id in det_vehno)
                if (id == veh_id)
                    return true;
            return false;
        }

        static public int get_phase_index_using_in_cycle_time(int in_cycle_time)
        {
            int index = 0;
            if (in_cycle_time < 5 * Globals.FIXED_CYCLE / 12)
                index = 0;
            else if (in_cycle_time < Globals.FIXED_CYCLE / 2)
                index = 1;
            else if (in_cycle_time < 11 * Globals.FIXED_CYCLE / 12)
                index = 2;
            else if (in_cycle_time < Globals.FIXED_CYCLE)
                index = 3;
            return index;
        }

        static public int[] add_clear_red(int[] decision)
        {
            List<int> decision_new = new List<int>();
            if (decision.Length > 0)
            {
                decision_new.Add(decision[0]);
                for (int i = 1; i < decision.Length; i++)
                {
                    if (decision[i] != decision[i - 1] && decision[i] % 2 == 0)
                        decision_new.Add(Globals.ONLY_RIGHT);
                    decision_new.Add(decision[i]);
                }
            }
            int tmp_cnt = decision_new.Count;
            if (tmp_cnt > 0 && decision_new[tmp_cnt - 1] != Globals.ONLY_RIGHT)
                decision_new.Add(Globals.ONLY_RIGHT);

            int[] deci = new int[decision_new.Count];
            for (int i = 0; i < decision_new.Count; i++)
                deci[i] = decision_new[i];

            return deci;
        }


        // This prepare is not necessary.
        /*
        static public List<List<Tuple<int, int>>> prepare_arr_t(List<List<Tuple<int, int>>> pred_arr_t)
        {
            int extension_len = 0;
            for (int i = 0; i < pred_arr_t.Count; i++)
            {
                int cnt = pred_arr_t[i].Count;
                if (cnt > 0)
                {
                    // Combine arrive time in the same Globals.TIME_INT.
                    int j = 0;
                    while (j < pred_arr_t[i].Count - 1)
                    {
                        if (pred_arr_t[i][j].Item2 / Globals.TIME_INT ==
                            pred_arr_t[i][j + 1].Item1 / Globals.TIME_INT)
                        {
                            pred_arr_t[i].Add(new Tuple<int, int>(pred_arr_t[i][j].Item2, pred_arr_t[i][j + 1].Item1));
                            pred_arr_t[i].RemoveAt(j);
                            pred_arr_t[i].RemoveAt(j);

                            pred_arr_t[i].Sort((x, y) => x.Item1.CompareTo(y.Item1));
                        }
                        else
                            j++;
                    }
                    pred_arr_t[i].Sort((x, y) => x.Item1.CompareTo(y.Item1));
                    cnt = pred_arr_t[i].Count;
                    if (pred_arr_t[i][cnt - 1].Item2 > extension_len)
                        extension_len = pred_arr_t[i][cnt - 1].Item2;
                }
            }

            int ext_count = extension_len / Globals.TIME_INT + 1;
            List<List<Tuple<int, int>>> res = new List<List<Tuple<int, int>>>();
            int[] index = new int[pred_arr_t.Count];
            for (int i = 0; i < index.Length; i++) index[i] = 0;

            for (int i = 0; i < pred_arr_t.Count; i++)
            {
                res.Add(new List<Tuple<int, int>>());
                for (int j = 0; j < ext_count; j++)
                {
                    if (pred_arr_t[i].Count > 0 && index[i] < pred_arr_t[i].Count)
                    {
                        int start = pred_arr_t[i][index[i]].Item1;
                        int end = pred_arr_t[i][index[i]].Item2;

                        int int_start = j * Globals.TIME_INT;
                        int int_end = (j + 1) * Globals.TIME_INT - 1;

                        // Arrival interval included in current interval
                        if (start >= int_start && end <= int_end)
                        {
                            res[i].Add(new Tuple<int, int>(start, end));
                            index[i]++;
                        }
                        // Arrival interval not included in current interval
                        else if (start > int_end || int_start > end)
                        {
                            res[i].Add(new Tuple<int, int>(Globals.NOT_INCLUDED,
                                                           Globals.NOT_INCLUDED));
                        }
                        // Arrival interval end part included in current interval
                        else if (start < int_end && end >= int_start && end <= int_end)
                        {
                            res[i].Add(new Tuple<int, int>(int_start, end));
                            index[i]++;
                        }
                        // Arrival interval front part included in current inteval
                        else if (start > int_start && start <= int_end && end > int_end)
                        {
                            res[i].Add(new Tuple<int, int>(start, int_end));
                        }
                        // Arrival interval includes current interval
                        else if (start <= int_start && end >= int_end)
                        {
                            res[i].Add(new Tuple<int, int>(int_start, int_end));
                        }
                    }
                    else
                    {
                        res[i].Add(new Tuple<int, int>(Globals.NOT_INCLUDED, Globals.NOT_INCLUDED));
                    }
                }
            }

            return res;
        }*/

        static private int[] get_plan_bits(int plan_index, int shift_num)
        {
            int[] plan = new int[shift_num];
            for (int i = 0; i < shift_num; i++)
            {
                if ((plan_index & 0x1) == 0)
                    plan[i] = 0;
                else
                    plan[i] = 1;
                plan_index >>= 1;
            }
            return plan;
        }

        static public List<Tuple<int, int>> get_tmp_plan(int plan_index, int shift_num)
        {
            List<Tuple<int, int>> res_plan = new List<Tuple<int, int>>();
            int[] tmp_plan = get_plan_bits(plan_index, shift_num);

            int tmp_width = 1;
            int start = 0;
            int end = 0;
            for (int i = 1; i < tmp_plan.Length; i++)
            {
                if (tmp_plan[i] == tmp_plan[i - 1])
                    tmp_width++;
                else
                {
                    start = (i - tmp_width) * Globals.TIME_INT;
                    end = start + Globals.TIME_INT * tmp_width - 1;
                    res_plan.Add(new Tuple<int, int>(start, end));
                    tmp_width = 1;
                }
            }
            end = shift_num * Globals.TIME_INT - 1;
            start = (shift_num - tmp_width) * Globals.TIME_INT;
            res_plan.Add(new Tuple<int, int>(start, end));
            return res_plan;
        }

        static private double get_single_devi(
            Tuple<int, int> plan,
            Tuple<int, int, int, double> arr_t)
        {
            double deviation = 0;
            int plan_start = plan.Item1;
            int plan_end = plan.Item2;
            int arr_start = arr_t.Item1;
            int arr_end = arr_t.Item2;
            int platoon_size = arr_t.Item3;

            // Arrive interval and plan interval do not overlap, or partly overlap.
            if (arr_start > plan_end || arr_end < plan_start ||
                (plan_start >= arr_start && plan_start <= arr_end && arr_end < plan_end) ||
                (plan_end >= arr_start && plan_end <= arr_end && arr_start > plan_start))
            {
                deviation = platoon_size * Math.Min(Math.Abs(arr_start - plan_start), Math.Abs(arr_end - plan_end));
            }
            else if (arr_start >= plan_start && arr_end <= plan_end) // Arrival interval included in plan interval.
            {
                deviation = 0;
            }
            else if (arr_start < plan_start && arr_end > plan_end) // Plan interval included in arrival interval.
            {
                // this case, do you add deviation or not? Or could you make the platoon denser?
                // For now, just do nothing.
            }

            // If plan interval is not wide enough, penalize it.
            if (plan_end - plan_start < arr_end - arr_start)
                deviation += platoon_size * (arr_end - arr_start - plan_end + plan_start);
                
            return deviation;
        }

        // direction: 0 --> horizontal; 1 --> vertical
        static private double get_tmp_deviation(
            List<Tuple<int, int>> plan,
            List<Tuple<int, int, int, double>> arr_t,
            int direction,
            List<int> tgt_time_interval)
        {
            double deviation = 0;

            for (int k = 0; k < arr_t.Count; k++)
            {
                double tmp_min = double.MaxValue;
                int tgt_interval = 0;
                for (int m = direction; m < plan.Count; m += 2)
                {
                    double devi = get_single_devi(plan[m], arr_t[k]);

                    if (devi < tmp_min)
                    {
                        tmp_min = devi;
                        tgt_interval = m;
                    }
                }
                tgt_time_interval.Add(tgt_interval);
                deviation += tmp_min;
            }

            return deviation;
        }

        // Why don't you just figure out the target speed?
        static private void record_current_best_platoon_time_interval(
            List<List<double>> tgt_platoon_speed,
            List<List<int>> tmp_tm_int,
            List<List<Tuple<int, int, int, double>>> pred_arr_t,
            List<Tuple<int, int>> plan)
        {
            tgt_platoon_speed.Clear();
            for (int i = 0; i < 4; i++)
                tgt_platoon_speed.Add(new List<double>());

            for (int i = 0; i < pred_arr_t.Count; i++)
            {
                for (int j = 0; j < pred_arr_t[i].Count; j++)
                {
                    double ori_speed = pred_arr_t[i][j].Item4;
                    int start_arr_time = pred_arr_t[i][j].Item1;
                    int desired_time = plan[tmp_tm_int[i][j]].Item1;
                    if (start_arr_time - desired_time == 0)
                        tgt_platoon_speed[i].Add(ori_speed);
                    else if (start_arr_time > desired_time)
                    {
                        if( desired_time != 0)
                            tgt_platoon_speed[i].Add(Math.Min(ori_speed * start_arr_time / desired_time, Globals.MAX_SPEED));
                        else
                            tgt_platoon_speed[i].Add(Globals.MAX_SPEED);
                    }
                    else if (start_arr_time < desired_time && desired_time != 0)
                    {
                        double tmp_speed = ori_speed * start_arr_time / desired_time;
                        tmp_speed = Math.Max(tmp_speed, Globals.MIN_SPEED);
                        tgt_platoon_speed[i].Add(Math.Min(tmp_speed, Globals.MAX_SPEED));
                    }
                }
            }
        }

        // The pred_arr_t tuple: 
        //     <start_arr_time, end_arr_time, platoon_size, platoon_start_speed>.
        static public int[] make_signal_predictions2(
            List<List<Tuple<int, int, int, double>>> pred_arr_t,
            List<List<double>> tgt_platoon_speed)
        {
            /*
             * 0. There is a prediction length limit, and also an interval.
             *    Like: an interval of 3 seconds, and a maximum prediction length of 30 seconds.
             *          Each interval can take one phase.
             * 1. For each combination, for each platoon calculate its minimum deviation.
             *      Minimum deviation is calculated by moving a platoon to a green interval by
             *      minimum distance. The deviation uses squared differences.
             * 2. How do you define differences? What if an interval cannot cover a platoon?
             *    Does the platoon size matter? Of course it does..
             *      For example:
             *          a. If target interval can cover the platoon: 
             *              Target interval is [3, 5], platoon arrival time is [5, 7],
             *              Then the difference is 5-3=2 or 7-5=2. (They are the same.)
             *          b. If target interval cannot cover the platoon:
             *              Target interval is [3, 6], platoon arrival time is [5, 9],
             *              Then how do you calculate the differences?
             *              Goal: platoon arrival interval larger, difference get larger.
             *              Possible answer: Min(5-3, 9-6) + (9-5)-(6-3)
             * */

            int ext_int_num = Globals.PRED_INT;
            double min_deviation = double.MaxValue;
            int min_index = -1;
            List<List<int>> tmp_tm_int = new List<List<int>>();

            for (int i = 0; i < Math.Pow(2, ext_int_num); i++)
            {
                double tmp_deviation = 0.0;
                List<Tuple<int, int>> plan = get_tmp_plan(i, ext_int_num);

                tmp_tm_int.Clear();
                for (int t = 0; t < 4; t++)
                    tmp_tm_int.Add(new List<int>());

                for (int j = 0; j < 4; j++)
                {
                    if (j < 2)
                        tmp_deviation += get_tmp_deviation(plan, pred_arr_t[j], 0, tmp_tm_int[j]);
                    else
                        tmp_deviation += get_tmp_deviation(plan, pred_arr_t[j], 1, tmp_tm_int[j]);

                    if (tmp_deviation > min_deviation)
                        break;
                }

                if (min_deviation > tmp_deviation)
                {
                    min_deviation = tmp_deviation;
                    min_index = i;

                    // Record target platoon speed
                    record_current_best_platoon_time_interval(tgt_platoon_speed, tmp_tm_int, pred_arr_t, plan);
                }
            }
            // Convert the min_index to decision array.
            int[] res_plan = get_plan_bits(min_index, ext_int_num);

            for (int i = 0; i < res_plan.Length; i++)
                if (res_plan[i] == 1)
                    res_plan[i] = 2;

            return res_plan;
        }
    }
}
