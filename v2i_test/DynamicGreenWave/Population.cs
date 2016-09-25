using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicGreenWave
{
    class Population
    {
        Individual[] individuals;

        // Create a population
        public Population(int population_size, bool initialize)
        {
            individuals = new Individual[population_size];
            if (initialize)
            {
                for (int i = 0; i < individuals.Length; i++)
                {
                    Individual indi = new Individual();
                    indi.generate_individual();
                    individuals[i] = indi;
                }
            }
        }

        public Individual get_individual(int index)
        {
            if (index < individuals.Length)
                return individuals[index];
            return null;
        }

        public Individual get_fittest(Intersection[] inters)
        {
            Individual fittest = null;
            if (individuals.Length > 0)
                fittest = individuals[0];
            if (individuals.Length > 1)
            {
                for (int i = 1; i < individuals.Length; i++)
                {
                    if (fittest.get_fitness(inters) <= individuals[i].get_fitness(inters))
                        fittest = individuals[i];
                }
            }
            return fittest;
        }

        public int size()
        {
            return individuals.Length;
        }

        public void save_individual(int index, Individual indi)
        {
            individuals[index] = indi;
        }


    }
}
