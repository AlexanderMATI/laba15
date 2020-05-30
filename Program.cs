using System;
using System.Collections.Generic;


namespace laba15
{
    class Program
    {
        
        static void Main(string[] args)
        {
            
            Clynic.InitializeParams(new Dictionary<Params, object>()
            {
                {Params.OrdinNum, 20},
                {Params.DocNum, 6},
                {Params.DP, 0.4},
                {Params.Stats, true},
                {Params.UpdatePeriod, 2000},
                {Params.MaxTime, 50},
                {Params.PatientCome, 0.5 },
                {Params.StatPeople, 120},
                {Params.ToFile, true },
                {Params.FileOut, "out.txt"}
            });

            Clynic.StartProcess();

            Console.ReadKey();
        }

    }
}
