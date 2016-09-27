using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace DynamicGreenWave
{
    class GA
    {
        private const double uniform_rate = 0.5;
        private const double mutation_rate = 0.0015;
        private const int tournament_size = 5;
        private const bool elitism = true;
        private const int population_size = 1000;
        private const double optimum_condition = 0.01;
        private const int min_generation_count = 50;

        public static string find_optimum(Intersection[] inters, Individual init_indiv=null)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            Population my_popu = new Population(population_size, true, init_indiv);
            double current_fitness = my_popu.get_fittest(inters).get_fitness(inters);
            double previous_fitness = 0.0;
            double tmp_diff = 0.0;
            int generation_count = 0;
            do {
                previous_fitness = current_fitness;
                my_popu = evolve_population(inters, my_popu);
                generation_count++;
                current_fitness = my_popu.get_fittest(inters).get_fitness(inters);
                tmp_diff = Math.Abs( ( previous_fitness - current_fitness) / current_fitness );
            //} while (generation_count < min_generation_count);
            }while (tmp_diff > optimum_condition);

            stopWatch.Stop();
            // Get the elapsed time as a TimeSpan value.
            TimeSpan ts = stopWatch.Elapsed;
            // Format and display the TimeSpan value.
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("RunTime " + elapsedTime);
            Console.WriteLine("Fitness " + current_fitness);
            Console.WriteLine("Solution found!");
            Console.WriteLine("Generation: " + generation_count);
            Console.WriteLine("Genes: " + my_popu.get_fittest(inters).get_gene_string());

            string res_str = my_popu.get_fittest(inters).get_gene_string();
            return res_str;
        }

        public static Population evolve_population(Intersection[] inters, Population p)
        {
            Population new_popu = new Population(p.size(), false);

            if (elitism)
                new_popu.save_individual(0, p.get_fittest(inters));

            int elitism_offset = elitism ? 1: 0;

            for (int i = elitism_offset; i < p.size(); i++)
            {
                Individual indiv1 = tournament_select(inters, p);
                Individual indiv2 = tournament_select(inters, p);
                Individual new_indiv = crossover(indiv1, indiv2);
                new_popu.save_individual(i, new_indiv);
            }

            for (int i = elitism_offset; i < p.size(); i++)
                mutate(new_popu.get_individual(i));
            
            return new_popu;
        }

        private static Individual crossover(Individual indiv1, Individual indiv2)
        {
            Individual new_indiv = new Individual();

            for (int i = 0; i < indiv1.size(); i++)
            {
                if (Utils.get_random_number(0, 100) <= uniform_rate * 100)
                    new_indiv.set_gene(i, indiv1.get_gene(i));
                else
                    new_indiv.set_gene(i, indiv2.get_gene(i));
            }
            return new_indiv;
        }

        private static void mutate(Individual indiv)
        {
            for (int i = 0; i < indiv.size(); i++)
            {
                if (Utils.get_random_number(0, 100) <= mutation_rate * 100)
                {
                    int gene = (int)(Utils.get_random_number(0, (int)(Math.Pow(2, Globals.PRED_INT))));
                    indiv.set_gene(i, gene);
                }
            }
        }
        
        private static Individual tournament_select(Intersection[] inters, Population p)
        {
            // If you just want to find the best of random 5, do you need to copy them
            // then find the one?
            Population tournament = new Population(tournament_size, false);
            for (int i = 0; i < tournament_size; i++)
            {
                int randint = Utils.get_random_number(0, p.size());
                tournament.save_individual(i, p.get_individual(randint));
            }

            Individual fittest = tournament.get_fittest(inters);

            return fittest;
        }

    }
}
