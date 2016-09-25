using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicGreenWave
{
    class Fitness
    {
        static private Tuple<int, int, int, double> append_upstream_queue(Intersection inter1, Intersection inter2,
                                                                          int direction, List<Tuple<int, int>> plan1)
        {
            // Append upstream queue
            Tuple<int, int> plan1_first = null;
            if (direction < 2)
                plan1_first = plan1[0];
            else if (plan1.Count > 1)
                plan1_first = plan1[1];
            var inter1_platoon_list = inter1.get_predict_platoon_arr_time(direction);
            Tuple<int, int, int, double> inter1_queue = null;
            if (inter1_platoon_list.Count > 0)
                inter1_queue = inter1_platoon_list[0];

            // Assume the first platoon is the queue.
            int link_travel_time = inter2.get_link_travel_time(direction);
            double link_speed = inter2.get_link_speed(direction);
            int sarr_time = plan1_first != null ? plan1_first.Item1 + link_travel_time : 2 * link_travel_time;
            int earr_time = plan1_first != null ? plan1_first.Item2 - plan1_first.Item1 + link_travel_time : 2 * link_travel_time;
            int inter1_lane_num = inter1.get_num_of_lanes(direction < 2 ? direction : direction + 2);

            int inter1_first_green_capacity = 0;
            if (plan1_first != null)
                inter1_first_green_capacity = (int)((plan1_first.Item2 - plan1_first.Item1 + 1) * inter1_lane_num * Globals.SAT_FLOW_RATE);
            int inter1_passed_veh = inter1_queue != null ? inter1_queue.Item3 : 0;
            if (inter1_passed_veh < inter1_first_green_capacity)
                inter1_passed_veh = inter1_first_green_capacity;

            return new Tuple<int, int, int, double>(sarr_time, earr_time, inter1_passed_veh, link_speed * 3.6);
        }



        // direction = 0: horizontally left to right; = 1: horizontally right to left;
        //           = 2: vertically down to up; = 3: vertically up to down
        static private double get_fitness_one_direction(Intersection[] inters,
                                                        Tuple<int, int, int> connected_inters,
                                                        Individual individual) 
        {
            double fitness = 0.0;

            int inter1_index = connected_inters.Item1 - 1;
            int inter2_index = connected_inters.Item2 - 1;
            int direction = connected_inters.Item3;
            Intersection inter1 = inters[inter1_index];
            Intersection inter2 = inters[inter2_index];

            // To simulate the platoon progression, first find the plan
            int plan1_index = individual.get_gene(inter1_index);
            int plan2_index = individual.get_gene(inter2_index);

            // Convert plan index to list of <start, end>
            List<Tuple<int, int>> plan1 = Utils.get_tmp_plan(plan1_index, Globals.PRED_INT);
            List<Tuple<int, int>> plan2 = Utils.get_tmp_plan(plan2_index, Globals.PRED_INT);

            // From direction inter1 --> inter2
            List<Tuple<int, int, int, double>> platoon_predicted_arr_time = inter2.get_predict_platoon_arr_time(direction);

            var upstream_queue = append_upstream_queue(inter1, inter2, direction, plan1);
            platoon_predicted_arr_time.Add(upstream_queue);

            int inter2_lane_num = inter2.get_num_of_lanes(direction > 1 ? direction : direction + 2);
            int queue = 0;
            int platoon_index = 0;
            
            for (int j = direction; j < plan1.Count; j += 2)
            {
                // Calculate how many vehicles will get passed and stoped by current green interval.
                int start = plan1[j].Item1;
                int end = plan1[j].Item2;
                int cur_green_int_capacity = (int)((plan1[j].Item2 - plan1[j].Item1 + 1) * inter2_lane_num * Globals.SAT_FLOW_RATE);

                // platoon_predicted_arr_time[0]: means horizontal direction from left to right
                while (platoon_index < platoon_predicted_arr_time.Count)
                {
                    int start_arr_time = platoon_predicted_arr_time[platoon_index].Item1;
                    int end_arr_time = platoon_predicted_arr_time[platoon_index].Item2;
                    int size = platoon_predicted_arr_time[platoon_index].Item3;

                    // If the platoon arrives inside the green interval, and has no more vehicles than the green capacity
                    if (start_arr_time >= start && end_arr_time <= end )
                    {
                        if (size <= cur_green_int_capacity)
                            queue = 0;
                        else
                        {
                            queue = size - cur_green_int_capacity;
                            fitness -= queue;
                        }
                        platoon_index++;
                    }
                    // If the green interval is within the platoon's arrival inteval
                    else if(start_arr_time <= start && end <= end_arr_time)
                    {
                        queue = (int)(size * (1 - (end - start + 1.0) / (end_arr_time - start_arr_time + 1.0)));
                        fitness -= queue;
                        int additional_delay = size * (start - start_arr_time); // To penalize front part waiting.
                        fitness -= additional_delay;
                        platoon_index++;
                    }
                    // If front part of the platoon arrives during green
                    else if (start_arr_time <= start && end_arr_time >= start && end_arr_time <= end)
                    {
                        double pass_ratio = (end_arr_time - start + 1.0) / (end_arr_time - start_arr_time + 1.0);
                        queue += (int)((1 - pass_ratio) * size);
                        fitness -= queue;
                        int additional_delay = size * (start - start_arr_time); // To penalize front part waiting.
                        fitness -= additional_delay;
                        platoon_index++;
                    }
                    // If end part of the platoon arrives during green
                    else if (start_arr_time >= start && start_arr_time <= end && end <= end_arr_time)
                    {
                        double pass_ratio = (end - start_arr_time + 1.0) / (end_arr_time - start_arr_time + 1.0);
                        queue += (int)((1 - pass_ratio) * size);
                        fitness -= queue;
                        platoon_index++;
                    }
                    // If they don't overlap at all.
                    else
                    {
                        if (start >= end_arr_time)
                        {
                            queue += size;
                            fitness -= queue;
                            int additional_delay = size * (start - start_arr_time); // To penalize front part waiting.
                            fitness -= additional_delay;
                            platoon_index++;
                        }
                        // If current green is not supposed to serve the platoon, check the next one.
                        else if (end <= start_arr_time)
                            break;
                    }
                }
            }

            return fitness;
        }

        static public double get_fitness(Individual individual, Intersection[] inters)
        {
            double fitness = 0.0;

            // For each pair of horizontally or vertically connected intersections,
            // estimate delay for each successive platoon.

            foreach (var connected_inters in Globals.CONNECTED_INTERS)
                fitness += get_fitness_one_direction(inters, connected_inters, individual);

            return fitness;
        }
    }
}
