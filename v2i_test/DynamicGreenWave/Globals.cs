using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicGreenWave
{
    class Globals
    {
        static public int SIM_TIME = 3600 * 1;          // Simulation time
        static public double SIM_SPEED = 0.2;           // Simulation LINK_AVG_SPEED
        static public int SIM_RES = 5;                  // Simulation resolution
        static public int INTER_NUM = 12;               // Intersection counts

        static public string test_flow_inpx = @"v2i_test_flow\qidong_network.inpx";
        static public string test_flow_layx = @"v2i_test_flow\qidong_network.layx";

        static public double MAX_LINK_LENGH = 1000.0;       // Maximum link length, for this particular network
        static public int QUEUE_SIZE_SUM = 5;               // Queue state has at most this many states
        static public int AVG_VEHICLE_DISTANCE = 8;         // Average vehicle distance: 4 (vehicle length) + 4 (safety length)
        static public double INDICATE_OVERFLOW_TIME = 2.0;  // If overflow detector vehicle presence for more than this long, overflows
        static public double INDICATE_QUEUE_DIST = 30.0;    // If red signal, a vehicle for less than this distance from the queue end, it's in the queue

        static public int LINK_NUM_ALL = 8;         // Links all, including left-turn bay
        static public int THRU_LINK_NUM = 4;        // Through link counts of each intersection
        static public int SINGLE_PHASE_NUM = 12;    // Valid phase counts
        static public int MIN_THRU_GREEN = 15;      // Minimum through phase green
        static public int MIN_LEFT_GREEN = 0;       // Minimum left-turn phase green
        static public int AMBER_TIME = 3;           // If left-turn has at least one car, at least AMBER_TIME is given
        static public int COMBI_PHASE_SUM = 4;      // fixed_phases, and there are four of them
        static public double SAT_FLOW_RATE = 0.5;   // thru or left use the same saturation flow rate

        static public int INTERSECTION_LEN = 30;        // Intersection length to predict upstream queue delay

        static public int LINK_INFO_D = 3;                               // Link info: [intersection id, link length and link average LINK_AVG_SPEED]
        static public int LINK_CONNECT_D = LINK_INFO_D * THRU_LINK_NUM;  // Link connection records link info for through links

        static public double SAT_THRESHOLD = 0.5;

        static public int FIXED_CYCLE = 60;
        static public int PLATOON_SIZE_MIN = 5;
        static public int PLATOON_HDWAY_DISTANCE = 30;

        static public int TIME_INT = 3;
        static public int PRED_INT = 10;
        static public int PRED_TIME = TIME_INT * PRED_INT;
        static public double CLAER_INTER_SPEED = 10 * 3.6;
        static public double QUEUE_SPEED = 3 * 3.6;
        static public double MAX_SPEED = 20 * 3.6;
        static public double MIN_SPEED = 1 * 3.6;

        static public int NOT_INCLUDED = -1;

        static public int WARM_UP_TIME = 300;
        static public int LOST_TIME = 3;

        static public int POPULATION_SIZE = 100;

        static public List<Tuple<int, int, int>> CONNECTED_INTERS = new List<Tuple<int, int, int>>()
        {
            // Horizontal connected ones.
            new Tuple<int, int, int>(1, 2, 0),
            new Tuple<int, int, int>(2, 3, 0),
            new Tuple<int, int, int>(4, 5, 0),
            new Tuple<int, int, int>(5, 6, 0),
            new Tuple<int, int, int>(7, 8, 0),
            new Tuple<int, int, int>(8, 9, 0),
            new Tuple<int, int, int>(10, 11, 0),
            new Tuple<int, int, int>(11, 12, 0),
            new Tuple<int, int, int>(2, 1, 1),
            new Tuple<int, int, int>(3, 2, 1),
            new Tuple<int, int, int>(5, 4, 1),
            new Tuple<int, int, int>(6, 5, 1),
            new Tuple<int, int, int>(8, 7, 1),
            new Tuple<int, int, int>(9, 8, 1),
            new Tuple<int, int, int>(11, 10, 1),
            new Tuple<int, int, int>(12, 11, 1),
            // Vertically connected ones.
            new Tuple<int, int, int>(1, 4, 2),
            new Tuple<int, int, int>(4, 7, 2),
            new Tuple<int, int, int>(7,10, 2),
            new Tuple<int, int, int>(2, 5, 2),
            new Tuple<int, int, int>(5, 8, 2),
            new Tuple<int, int, int>(8, 11, 2),
            new Tuple<int, int, int>(3, 6, 2),
            new Tuple<int, int, int>(6, 9, 2),
            new Tuple<int, int, int>(9, 12, 2),
            new Tuple<int, int, int>(4, 1, 3),
            new Tuple<int, int, int>(7, 4, 3),
            new Tuple<int, int, int>(10,7, 3),
            new Tuple<int, int, int>(5, 2, 3),
            new Tuple<int, int, int>(8, 5, 3),
            new Tuple<int, int, int>(11, 8, 3),
            new Tuple<int, int, int>(6, 3, 3),
            new Tuple<int, int, int>(9, 6, 3),
            new Tuple<int, int, int>(12, 9, 3),
        };

        static public int EWB_THRU = 0;
        static public int NSB_LEFT = 1;
        static public int NSB_THRU = 2;
        static public int EWB_LEFT = 3;
        static public int ONLY_RIGHT = 4;


        public const int EB_THRU = 1;       // Phase 1
        public const int WB_THRU = 2;       // Phase 2
        public const int NB_LEFT = 3;       // Phase 3
        public const int SB_LEFT = 4;       // Phase 4
        public const int NB_THRU = 5;       // Phase 5
        public const int SB_THRU = 6;       // Phase 6
        public const int WB_LEFT = 7;       // Phase 7
        public const int EB_LEFT = 8;       // Phase 8
        public const int SB_RIGHT = 9;      // Phase 9
        public const int NB_RIGHT = 10;     // Phase 10
        public const int EB_RIGHT = 11;     // Phase 11
        public const int WB_RIGHT = 12;     // Phase 12

        static public HashSet<int> init_phases = new HashSet<int>() { EB_THRU, WB_THRU, SB_RIGHT, NB_RIGHT, WB_RIGHT, EB_RIGHT };

        static public List<HashSet<int>> FIXED_PHASES = new List<HashSet<int>>() { 
            new HashSet<int>() { EB_THRU, WB_THRU, SB_RIGHT, NB_RIGHT, WB_RIGHT, EB_RIGHT },
            new HashSet<int>() { NB_LEFT, SB_LEFT, SB_RIGHT, NB_RIGHT, WB_RIGHT, EB_RIGHT },
            new HashSet<int>() { NB_THRU, SB_THRU, SB_RIGHT, NB_RIGHT, WB_RIGHT, EB_RIGHT },
            new HashSet<int>() { WB_LEFT, EB_LEFT, SB_RIGHT, NB_RIGHT, WB_RIGHT, EB_RIGHT },
            new HashSet<int>() { SB_RIGHT, NB_RIGHT, WB_RIGHT, EB_RIGHT },
        };

        static public int LINK_AVG_SPEED = 15;
        // Connected intersections: { left, dist, right, dist, down, dist, up, dist }, if no, -1
        static public int[,] NETWORK_GRAPH = new int[,]
            {   {  -1, 350, LINK_AVG_SPEED,  1, 660, LINK_AVG_SPEED , -1, 300, LINK_AVG_SPEED,  3, 210, LINK_AVG_SPEED },
                {   0, 660, LINK_AVG_SPEED,  2, 440, LINK_AVG_SPEED , -1, 200, LINK_AVG_SPEED,  4, 210, LINK_AVG_SPEED },
                {   1, 440, LINK_AVG_SPEED, -1, 360, LINK_AVG_SPEED , -1, 240, LINK_AVG_SPEED,  5, 240, LINK_AVG_SPEED },
                {  -1, 260, LINK_AVG_SPEED,  4, 660, LINK_AVG_SPEED ,  0, 210, LINK_AVG_SPEED,  6, 380, LINK_AVG_SPEED },
                {   3, 660, LINK_AVG_SPEED,  5, 480, LINK_AVG_SPEED ,  1, 210, LINK_AVG_SPEED,  7, 370, LINK_AVG_SPEED },
                {   4, 480, LINK_AVG_SPEED, -1, 290, LINK_AVG_SPEED ,  2, 240, LINK_AVG_SPEED,  8, 370, LINK_AVG_SPEED },
                {  -1, 250, LINK_AVG_SPEED,  7, 650, LINK_AVG_SPEED ,  3, 380, LINK_AVG_SPEED,  9, 375, LINK_AVG_SPEED },
                {   6, 650, LINK_AVG_SPEED,  8, 520, LINK_AVG_SPEED ,  4, 370, LINK_AVG_SPEED, 10, 380, LINK_AVG_SPEED },
                {   7, 520, LINK_AVG_SPEED, -1, 150, LINK_AVG_SPEED ,  5, 370, LINK_AVG_SPEED, 11, 390, LINK_AVG_SPEED },
                {  -1, 400, LINK_AVG_SPEED, 10, 650, LINK_AVG_SPEED ,  6, 375, LINK_AVG_SPEED, -1, 380, LINK_AVG_SPEED },
                {   9, 650, LINK_AVG_SPEED, 11, 510, LINK_AVG_SPEED ,  7, 380, LINK_AVG_SPEED, -1, 410, LINK_AVG_SPEED },
                {  10, 510, LINK_AVG_SPEED, -1, 230, LINK_AVG_SPEED ,  8, 390, LINK_AVG_SPEED, -1, 300, LINK_AVG_SPEED },

            };

        static public string[] STOPLINE_CONFIG = {
            "1,2,3|4,5,6|7,445|8,446|9,10|11,12|13,447|14,448|15|16|17|18",                                     // inter 1
            "19,20,21|22,23,24|25,26|27,28|29,30,31|32,33,34|35,36|37,38|39|40|41|42",                          // inter 2
            "43,44,45|46,47,48|49,437|50,438|51,52,53|54,55,56|57,439|58,440|59|60|61|62",                      // inter 3
            "63,64|65,66|67,453|68,454|69,70|71,72|73,455|74,456|75|76|77|78",                                  // inter 4
            "79,80|81,82|83,461|84,462|85,86,87|88,89,90|91,92|93,94|95|96|97|98",                              // inter 5
            "99,100|101,102|103,465|104,466|105,106,107|108,109,110|111,112|113,114|115|116|117|118",           // inter 6
            "119,120|121,122|123,469|124,470|125,126|127,128|129,471|130,472|131|132|133|134",                  // inter 7
            "135,136|137,138|139,477|140,478|141,142,143|144,145,146|147,148|149,150|151|152|153|154",          // inter 8
            "155,156|157,158|159,481|160,482|161,162,163|164,165,166|167,168|169,170|171|172|173|174",          // inter 9
            "175,176,177|178,179,180|181,182|183,184|185,186|187,188|189,485|190,486|191|192|193|194",          // inter 10
            "195,196,197|198,199,200|201,202|203,204|205,206,207|208,209,210|211,212|213,214|215|216|217|218",  // inter 11
            "219,220,221|222,223,224|225,226|227,228|229,230,231|232,233,234|235,236|237,238|239|240|241|242",  // inter 12
            };

        static public string[] OVERFLOW_CONFIG = {
            "243,244,245|246,247,248|249,449|250,450|251,252|253,254|255,451|256,452",          // inter 1
            "257,258,259|260,261,262|263,264|265,266|267,268,269|270,271,272|273,274|275,276",  // inter 2
            "277,278,279|280,281,282|283,441|284,442|285,286,287|288,289,290|291,443|292,444",  // inter 3
            "293,294|295,296|297,457|298,458|299,300|301,302|303,459|304,460",                  // inter 4
            "305,306|307,308|309,463|310,464|311,312,313|314,315,316|317,318|319,320",          // inter 5
            "321,322|323,324|325,467|326,468|327,328,329|330,331,332|333,334|335,336",          // inter 6
            "337,338|339,340|341,473|342,474|343,344|345,346|347,475|348,476",                  // inter 7
            "349,350|351,352|353,479|354,480|355,356,357|358,359,360|361,362|363,364",          // inter 8
            "365,366|367,368|369,483|370,484|371,372,373|374,375,376|377,378|379,380",          // inter 9
            "381,382,383|384,385,386|387,388|389,390|391,392|393,394|395,487|396,488",          // inter 10
            "397,398,399|400,401,402|403,404|405,406|407,408,409|410,411,412|413,414|415,416",  // inter 11
            "417,418,419|420,421,422|423,424|425,426|427,428,429|430,431,432|433,434|435,436",  // inter 12
        };
    }
}
