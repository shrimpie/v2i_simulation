using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicGreenWave
{
    class Individual
    {
        static int default_gene_length = Globals.INTER_NUM;
        private int[] genes = new int[default_gene_length];
        private double fitness = 0.0;

        // Create a random individual
        public void generate_individual()
        {
            for (int i = 0; i < genes.Length; i++)
            {
                int gene = Utils.get_random_number(0, (int)(Math.Pow(2, Globals.PRED_INT)));
                genes[i] = gene;
            }
        }

        public static void set_default_gene_length(int length)
        {
            default_gene_length = length;
        }

        public int get_gene(int index)
        {
            if (index < genes.Length)
                return genes[index];
            // Refine this.
            return 0;
        }

        public void set_gene(int index, int value)
        {
            if (index < genes.Length)
            {
                genes[index] = value;
                fitness = 0;
            }
        }

        public int size()
        {
            return genes.Length;
        }

        public double get_fitness(Intersection[] inters)
        {
            if (fitness == 0.0)
                fitness = Fitness.get_fitness(this, inters);
            return fitness;
        }

        private string get_single_gene_string(int index)
        {
            string single_gene = "";
            int gene = 0;
            if( index < genes.Length)
                gene = genes[index];

            for (int i = 0; i < Globals.PRED_INT; i++)
            {
                if ((gene & 0x1) == 0)
                    single_gene += '0';
                else
                    single_gene += '2';
                gene >>= 1;
            }
            return single_gene;
        }

        public string get_gene_string()
        {
            string genes_str = "";
            for (int i = 0; i < genes.Length; i++)
                genes_str += get_single_gene_string(i);

            return genes_str;
        }
    }
}
