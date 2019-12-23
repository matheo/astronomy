﻿using System;
using demo_helper;
using CosineKitty;

namespace moonphase
{
    class Program
    {
        static string QuarterName(int quarter)
        {
            switch (quarter)
            {
                case 0: return "New Moon";
                case 1: return "First Quarter";
                case 2: return "Full Moon";
                case 3: return "Third Quarter";
                default: return "INVALID QUARTER";
            }
        }

        static int Main(string[] args)
        {
            AstroTime time;
            switch (args.Length)
            {
                case 0:
                    time = new AstroTime(DateTime.Now);
                    break;

                case 1:
                    time = DemoHelper.ParseTime("moonphase", args[0]);
                    break;

                default:
                    Console.WriteLine("USAGE: moonphase [date]");
                    return 1;
            }

            /*
                Calculate the Moon's current phase angle,
                which ranges from 0 to 360 degrees.

                0 = new moon,
                90 = first quarter,
                180 = full moon,
                270 = third quarter.
            */
            double phase = Astronomy.MoonPhase(time);
            Console.WriteLine("{0} : Moon's phase angle = {1:0.000000} degrees.", time, phase);

            /* Find the next 10 lunar quarter phases. */
            Console.WriteLine();
            Console.WriteLine("The next 10 lunar quarters are:");
            MoonQuarterInfo mq = Astronomy.SearchMoonQuarter(time);
            for (int i=0; i < 10; ++i)
            {
                if (i > 0)
                    mq = Astronomy.NextMoonQuarter(mq);
                Console.WriteLine("{0} : {1}", mq.time, QuarterName(mq.quarter));
            }
            return 0;
        }
    }
}
