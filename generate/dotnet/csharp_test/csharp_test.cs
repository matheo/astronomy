﻿using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

using CosineKitty;

namespace csharp_test
{
    class Program
    {
        const double DEG2RAD = 0.017453292519943296;
        const double RAD2DEG = 57.295779513082321;
        static bool Verbose;

        static void Debug(string format, params object[] args)
        {
            if (Verbose)
                Console.WriteLine(format, args);
        }

        struct Test
        {
            public string Name;
            public Func<int> TestFunc;
            public Test(string name, Func<int> testFunc)
            {
                this.Name = name;
                this.TestFunc = testFunc;
            }
        }

        static Test[] UnitTests = new Test[]
        {
            new Test("time", TestTime),
            new Test("moon", MoonTest),
            new Test("constellation", ConstellationTest),
            new Test("elongation", ElongationTest),
            new Test("global_solar_eclipse", GlobalSolarEclipseTest),
            new Test("local_solar_eclipse", LocalSolarEclipseTest),
            new Test("lunar_apsis", LunarApsisTest),
            new Test("lunar_eclipse", LunarEclipseTest),
            new Test("lunar_eclipse_78", LunarEclipseIssue78),
            new Test("magnitude", MagnitudeTest),
            new Test("moonphase", MoonPhaseTest),
            new Test("planet_apsis", PlanetApsisTest),
            new Test("pluto", PlutoCheck),
            new Test("refraction", RefractionTest),
            new Test("riseset", RiseSetTest),
            new Test("rotation", RotationTest),
            new Test("seasons", SeasonsTest),
            new Test("transit", TransitTest),
            new Test("astro_check", AstroCheck),
        };

        static int Main(string[] args)
        {
            try
            {
                // Force use of "." for the decimal mark, regardless of local culture settings.
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

                if (args.Length > 0 && args[0] == "-v")
                {
                    Verbose = true;
                    args = args.Skip(1).ToArray();
                }

                if (args.Length == 1)
                {
                    string name = args[0];
                    if (name == "all")
                    {
                        Console.WriteLine("csharp_test: starting");
                        foreach (Test t in UnitTests)
                            if (0 != t.TestFunc())
                                return 1;
                        Console.WriteLine("csharp_test: PASS");
                        return 0;
                    }

                    foreach (Test t in UnitTests)
                        if (t.Name == name)
                            return t.TestFunc();
                }

                Console.WriteLine("csharp_test: Invalid command line parameters.");
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("charp_test: EXCEPTION: {0}", ex);
                return 1;
            }
        }

        static double v(double x)
        {
            if (!double.IsFinite(x))
                throw new ArgumentException("Non-finite result");
            return x;
        }

        static double abs(double x)
        {
            return Math.Abs(v(x));
        }

        static double max(double a, double b)
        {
            return Math.Max(v(a), v(b));
        }

        static double min(double a, double b)
        {
            return Math.Min(v(a), v(b));
        }

        static double sqrt(double x)
        {
            return v(Math.Sqrt(v(x)));
        }

        static double sin(double x)
        {
            return Math.Sin(v(x));
        }

        static double cos(double x)
        {
            return Math.Cos(v(x));
        }

        static readonly char[] TokenSeparators = new char[] { ' ', '\t', '\r', '\n' };

        static string[] Tokenize(string line)
        {
            return line.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        static int TestTime()
        {
            const int year = 2018;
            const int month = 12;
            const int day = 2;
            const int hour = 18;
            const int minute = 30;
            const int second = 12;
            const int milli = 543;

            DateTime d = new DateTime(year, month, day, hour, minute, second, milli, DateTimeKind.Utc);
            AstroTime time = new AstroTime(d);
            Console.WriteLine("C# TestTime: text={0}, ut={1}, tt={2}", time.ToString(), time.ut.ToString("F6"), time.tt.ToString("F6"));

            const double expected_ut = 6910.270978506945;
            double diff = time.ut - expected_ut;
            if (abs(diff) > 1.0e-12)
            {
                Console.WriteLine("C# TestTime: ERROR - excessive UT error {0}", diff);
                return 1;
            }

            const double expected_tt = 6910.271800214368;
            diff = time.tt - expected_tt;
            if (abs(diff) > 1.0e-12)
            {
                Console.WriteLine("C# TestTime: ERROR - excessive TT error {0}", diff);
                return 1;
            }

            DateTime utc = time.ToUtcDateTime();
            if (utc.Year != year || utc.Month != month || utc.Day != day || utc.Hour != hour || utc.Minute != minute || utc.Second != second || utc.Millisecond != milli)
            {
                Console.WriteLine("C# TestTime: ERROR - Expected {0:o}, found {1:o}", d, utc);
                return 1;
            }

            return 0;
        }

        static int MoonTest()
        {
            var time = new AstroTime(2019, 6, 24, 15, 45, 37);
            AstroVector vec = Astronomy.GeoVector(Body.Moon, time, Aberration.None);
            Console.WriteLine("C# MoonTest: {0} {1} {2}", vec.x.ToString("f17"), vec.y.ToString("f17"), vec.z.ToString("f17"));

            double dx = vec.x - (+0.002674037026701135);
            double dy = vec.y - (-0.0001531610316600666);
            double dz = vec.z - (-0.0003150159927069429);
            double diff = sqrt(dx*dx + dy*dy + dz*dz);
            Console.WriteLine("C# MoonTest: diff = {0}", diff.ToString("g5"));
            if (diff > 4.34e-19)
            {
                Console.WriteLine("C# MoonTest: EXCESSIVE ERROR");
                return 1;
            }

            return 0;
        }

        static int AstroCheck()
        {
            const string filename = "csharp_check.txt";
            using (StreamWriter outfile = File.CreateText(filename))
            {
                var bodylist = new Body[]
                {
                    Body.Sun, Body.Mercury, Body.Venus, Body.Earth, Body.Mars,
                    Body.Jupiter, Body.Saturn, Body.Uranus, Body.Neptune, Body.Pluto,
                    Body.SSB, Body.EMB
                };

                var observer = new Observer(29.0, -81.0, 10.0);
                var time = new AstroTime(new DateTime(1700, 1, 1, 0, 0, 0, DateTimeKind.Utc));
                var stop = new AstroTime(new DateTime(2200, 1, 1, 0, 0, 0, DateTimeKind.Utc));

                AstroVector pos;
                Equatorial j2000, ofdate;
                Topocentric hor;

                outfile.WriteLine("o {0} {1} {2}", observer.latitude, observer.longitude, observer.height);
                while (time.tt < stop.tt)
                {
                    foreach (Body body in bodylist)
                    {
                        pos = Astronomy.HelioVector(body, time);
                        outfile.WriteLine("v {0} {1} {2} {3} {4}", body, pos.t.tt.ToString("G17"), pos.x.ToString("G17"), pos.y.ToString("G17"), pos.z.ToString("G17"));
                        if (body != Body.Earth && body != Body.SSB && body != Body.EMB)
                        {
                            j2000 = Astronomy.Equator(body, time, observer, EquatorEpoch.J2000, Aberration.None);
                            ofdate = Astronomy.Equator(body, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);
                            hor = Astronomy.Horizon(time, observer, ofdate.ra, ofdate.dec, Refraction.None);
                            outfile.WriteLine("s {0} {1} {2} {3} {4} {5} {6} {7}",
                                body,
                                time.tt.ToString("G17"), time.ut.ToString("G17"),
                                j2000.ra.ToString("G17"), j2000.dec.ToString("G17"), j2000.dist.ToString("G17"),
                                hor.azimuth.ToString("G17"), hor.altitude.ToString("G17"));
                        }
                    }

                    pos = Astronomy.GeoVector(Body.Moon, time, Aberration.None);
                    outfile.WriteLine("v GM {0} {1} {2} {3}", pos.t.tt.ToString("G17"), pos.x.ToString("G17"), pos.y.ToString("G17"), pos.z.ToString("G17"));
                    j2000 = Astronomy.Equator(Body.Moon, time, observer, EquatorEpoch.J2000, Aberration.None);
                    ofdate = Astronomy.Equator(Body.Moon, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);
                    hor = Astronomy.Horizon(time, observer, ofdate.ra, ofdate.dec, Refraction.None);
                    outfile.WriteLine("s GM {0} {1} {2} {3} {4} {5} {6}",
                        time.tt.ToString("G17"), time.ut.ToString("G17"),
                        j2000.ra.ToString("G17"), j2000.dec.ToString("G17"), j2000.dist.ToString("G17"),
                        hor.azimuth.ToString("G17"), hor.altitude.ToString("G17"));

                    time = time.AddDays(10.0 + Math.PI/100.0);
                }
            }
            Console.WriteLine("C# AstroCheck: finished");
            return 0;
        }

        static int SeasonsTest()
        {
            const string filename = "../../seasons/seasons.txt";
            var re = new Regex(@"^(\d+)-(\d+)-(\d+)T(\d+):(\d+)Z\s+([A-Za-z]+)\s*$");
            using (StreamReader infile = File.OpenText(filename))
            {
                string line;
                int lnum = 0;
                int current_year = 0;
                int mar_count=0, jun_count=0, sep_count=0, dec_count=0;
                double max_minutes = 0.0;
                SeasonsInfo seasons = new SeasonsInfo();
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    /*
                        2019-01-03T05:20Z Perihelion
                        2019-03-20T21:58Z Equinox
                        2019-06-21T15:54Z Solstice
                        2019-07-04T22:11Z Aphelion
                        2019-09-23T07:50Z Equinox
                        2019-12-22T04:19Z Solstice
                    */
                    Match m = re.Match(line);
                    if (!m.Success)
                    {
                        Console.WriteLine("C# SeasonsTest: ERROR {0} line {1}: cannot parse", filename, lnum);
                        return 1;
                    }

                    int year = int.Parse(m.Groups[1].Value);
                    int month = int.Parse(m.Groups[2].Value);
                    int day = int.Parse(m.Groups[3].Value);
                    int hour = int.Parse(m.Groups[4].Value);
                    int minute = int.Parse(m.Groups[5].Value);
                    string name = m.Groups[6].Value;
                    var correct_time = new AstroTime(year, month, day, hour, minute, 0);

                    if (year != current_year)
                    {
                        current_year = year;
                        seasons = Astronomy.Seasons(year);
                    }

                    AstroTime calc_time = null;
                    if (name == "Equinox")
                    {
                        switch (month)
                        {
                            case 3:
                                calc_time = seasons.mar_equinox;
                                ++mar_count;
                                break;

                            case 9:
                                calc_time = seasons.sep_equinox;
                                ++sep_count;
                                break;

                            default:
                                Console.WriteLine("C# SeasonsTest: {0} line {1}: Invalid equinox date in test data.", filename, lnum);
                                return 1;
                        }
                    }
                    else if (name == "Solstice")
                    {
                        switch (month)
                        {
                            case 6:
                                calc_time = seasons.jun_solstice;
                                ++jun_count;
                                break;

                            case 12:
                                calc_time = seasons.dec_solstice;
                                ++dec_count;
                                break;

                            default:
                                Console.WriteLine("C# SeasonsTest: {0} line {1}: Invalid solstice date in test data.", filename, lnum);
                                return 1;
                        }
                    }
                    else if (name == "Aphelion")
                    {
                        /* not yet calculated */
                        continue;
                    }
                    else if (name == "Perihelion")
                    {
                        /* not yet calculated */
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("C# SeasonsTest: {0} line {1}: unknown event type {2}", filename, lnum, name);
                        return 1;
                    }

                    /* Verify that the calculated time matches the correct time for this event. */
                    double diff_minutes = (24.0 * 60.0) * abs(calc_time.tt - correct_time.tt);
                    if (diff_minutes > max_minutes)
                        max_minutes = diff_minutes;

                    if (diff_minutes > 2.37)
                    {
                        Console.WriteLine("C# SeasonsTest: {0} line {1}: excessive error ({2}): {3} minutes.", filename, lnum, name, diff_minutes);
                        return 1;
                    }
                }
                Console.WriteLine("C# SeasonsTest: verified {0} lines from file {1} : max error minutes = {2:0.000}", lnum, filename, max_minutes);
                Console.WriteLine("C# SeasonsTest: Event counts: mar={0}, jun={1}, sep={2}, dec={3}", mar_count, jun_count, sep_count, dec_count);
                return 0;
            }
        }

        static int MoonPhaseTest()
        {
            const string filename = "../../moonphase/moonphases.txt";
            using (StreamReader infile = File.OpenText(filename))
            {
                const double threshold_seconds = 120.0;
                int lnum = 0;
                string line;
                double max_arcmin = 0.0;
                int prev_year = 0;
                int expected_quarter = 0;
                int quarter_count = 0;
                double maxdiff = 0.0;
                MoonQuarterInfo mq = new MoonQuarterInfo();
                var re = new Regex(@"^([0-3])\s+(\d+)-(\d+)-(\d+)T(\d+):(\d+):(\d+)\.000Z$");
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    /*
                        0 1800-01-25T03:21:00.000Z
                        1 1800-02-01T20:40:00.000Z
                        2 1800-02-09T17:26:00.000Z
                        3 1800-02-16T15:49:00.000Z
                    */
                    Match m = re.Match(line);
                    if (!m.Success)
                    {
                        Console.WriteLine("C# MoonPhaseTest: ERROR {0} line {1}: cannot parse", filename, lnum);
                        return 1;
                    }
                    int quarter = int.Parse(m.Groups[1].Value);
                    int year = int.Parse(m.Groups[2].Value);
                    int month = int.Parse(m.Groups[3].Value);
                    int day = int.Parse(m.Groups[4].Value);
                    int hour = int.Parse(m.Groups[5].Value);
                    int minute = int.Parse(m.Groups[6].Value);
                    int second = int.Parse(m.Groups[7].Value);

                    double expected_elong = 90.0 * quarter;
                    AstroTime expected_time = new AstroTime(year, month, day, hour, minute, second);
                    double calc_elong = Astronomy.MoonPhase(expected_time);
                    double degree_error = abs(calc_elong - expected_elong);
                    if (degree_error > 180.0)
                        degree_error = 360.0 - degree_error;
                    double arcmin = 60.0 * degree_error;
                    if (arcmin > 1.0)
                    {
                        Console.WriteLine("C# MoonPhaseTest({0} line {1}): EXCESSIVE ANGULAR ERROR: {2} arcmin", filename, lnum, arcmin);
                        return 1;
                    }
                    if (arcmin > max_arcmin)
                        max_arcmin = arcmin;

                    if (year != prev_year)
                    {
                        prev_year = year;
                        /* The test data contains a single year's worth of data for every 10 years. */
                        /* Every time we see the year value change, it breaks continuity of the phases. */
                        /* Start the search over again. */
                        AstroTime start_time = new AstroTime(year, 1, 1, 0, 0, 0);
                        mq = Astronomy.SearchMoonQuarter(start_time);
                        expected_quarter = -1;  /* we have no idea what the quarter should be */
                    }
                    else
                    {
                        /* Yet another lunar quarter in the same year. */
                        expected_quarter = (1 + mq.quarter) % 4;
                        mq = Astronomy.NextMoonQuarter(mq);

                        /* Make sure we find the next expected quarter. */
                        if (expected_quarter != mq.quarter)
                        {
                            Console.WriteLine("C# MoonPhaseTest({0} line {1}): SearchMoonQuarter returned quarter {2}, but expected {3}", filename, lnum, mq.quarter, expected_quarter);
                            return 1;
                        }
                    }
                    ++quarter_count;
                    /* Make sure the time matches what we expect. */
                    double diff_seconds = abs(mq.time.tt - expected_time.tt) * (24.0 * 3600.0);
                    if (diff_seconds > threshold_seconds)
                    {
                        Console.WriteLine("C# MoonPhaseTest({0} line {1}): excessive time error {2:0.000} seconds", filename, lnum, diff_seconds);
                        return 1;
                    }

                    if (diff_seconds > maxdiff)
                        maxdiff = diff_seconds;
                }

                Console.WriteLine("C# MoonPhaseTest: passed {0} lines for file {1} : max_arcmin = {2:0.000000}, maxdiff = {3:0.000} seconds, {4} quarters",
                    lnum, filename, max_arcmin, maxdiff, quarter_count);

                return 0;
            }
        }

        static int RiseSetTest()
        {
            const string filename = "../../riseset/riseset.txt";
            using (StreamReader infile = File.OpenText(filename))
            {
                int lnum = 0;
                string line;
                var re = new Regex(@"^([A-Za-z]+)\s+([\-\+]?\d+\.?\d*)\s+([\-\+]?\d+\.?\d*)\s+(\d+)-(\d+)-(\d+)T(\d+):(\d+)Z\s+([rs])\s*$");
                Body current_body = Body.Invalid;
                Observer observer = new Observer();
                bool foundObserver = false;
                AstroTime r_search_date = null, s_search_date = null;
                AstroTime r_evt = null, s_evt = null;     /* rise event, set event: search results */
                AstroTime a_evt = null, b_evt = null;     /* chronologically first and second events */
                Direction a_dir = Direction.Rise, b_dir = Direction.Rise;
                const double nudge_days = 0.01;
                double sum_minutes = 0.0;
                double max_minutes = 0.0;

                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;

                    // Moon  103 -61 1944-01-02T17:08Z s
                    // Moon  103 -61 1944-01-03T05:47Z r
                    Match m = re.Match(line);
                    if (!m.Success)
                    {
                        Console.WriteLine("C# RiseSetTest({0} line {1}): invalid input format", filename, lnum);
                        return 1;
                    }
                    Body body = Enum.Parse<Body>(m.Groups[1].Value);
                    double longitude = double.Parse(m.Groups[2].Value);
                    double latitude = double.Parse(m.Groups[3].Value);
                    int year = int.Parse(m.Groups[4].Value);
                    int month = int.Parse(m.Groups[5].Value);
                    int day = int.Parse(m.Groups[6].Value);
                    int hour = int.Parse(m.Groups[7].Value);
                    int minute = int.Parse(m.Groups[8].Value);
                    Direction direction = (m.Groups[9].Value == "r") ? Direction.Rise : Direction.Set;
                    var correct_date = new AstroTime(year, month, day, hour, minute, 0);

                    /* Every time we see a new geographic location or body, start a new iteration */
                    /* of finding all rise/set times for that UTC calendar year. */
                    if (!foundObserver || observer.latitude != latitude || observer.longitude != longitude || current_body != body)
                    {
                        current_body = body;
                        observer = new Observer(latitude, longitude, 0.0);
                        foundObserver = true;
                        r_search_date = s_search_date = new AstroTime(year, 1, 1, 0, 0, 0);
                        b_evt = null;
                        Debug("C# RiseSetTest: {0} lat={1} lon={2}", body, latitude, longitude);
                    }

                    if (b_evt != null)
                    {
                        a_evt = b_evt;
                        a_dir = b_dir;
                        b_evt = null;
                    }
                    else
                    {
                        r_evt = Astronomy.SearchRiseSet(body, observer, Direction.Rise, r_search_date, 366.0);
                        if (r_evt == null)
                        {
                            Console.WriteLine("C# RiseSetTest({0} line {1}): Did not find {2} rise event.", filename, lnum, body);
                            return 1;
                        }

                        s_evt = Astronomy.SearchRiseSet(body, observer, Direction.Set, s_search_date, 366.0);
                        if (s_evt == null)
                        {
                            Console.WriteLine("C# RiseSetTest({0} line {1}): Did not find {2} rise event.", filename, lnum, body);
                            return 1;
                        }

                        /* Expect the current event to match the earlier of the found dates. */
                        if (r_evt.tt < s_evt.tt)
                        {
                            a_evt = r_evt;
                            b_evt = s_evt;
                            a_dir = Direction.Rise;
                            b_dir = Direction.Set;
                        }
                        else
                        {
                            a_evt = s_evt;
                            b_evt = r_evt;
                            a_dir = Direction.Set;
                            b_dir = Direction.Rise;
                        }

                        /* Nudge the event times forward a tiny amount. */
                        r_search_date = r_evt.AddDays(nudge_days);
                        s_search_date = s_evt.AddDays(nudge_days);
                    }

                    if (a_dir != direction)
                    {
                        Console.WriteLine("C# RiseSetTest({0} line {1}): expected dir={2} but found {3}", filename, lnum, a_dir, direction);
                        return 1;
                    }
                    double error_minutes = (24.0 * 60.0) * abs(a_evt.tt - correct_date.tt);
                    sum_minutes += error_minutes * error_minutes;
                    if (error_minutes > max_minutes)
                        max_minutes = error_minutes;

                    if (error_minutes > 0.57)
                    {
                        Console.WriteLine("C# RiseSetTest({0} line {1}): excessive prediction time error = {2} minutes.", filename, lnum, error_minutes);
                        return 1;
                    }
                }

                double rms_minutes = sqrt(sum_minutes / lnum);
                Console.WriteLine("C# RiseSetTest: passed {0} lines: time errors in minutes: rms={1}, max={2}", lnum, rms_minutes, max_minutes);
                return 0;
            }
        }

        static int TestElongFile(string filename, double targetRelLon)
        {
            using (StreamReader infile = File.OpenText(filename))
            {
                int lnum = 0;
                string line;
                var re = new Regex(@"^(\d+)-(\d+)-(\d+)T(\d+):(\d+)Z\s+([A-Z][a-z]+)\s*$");
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    /* 2018-05-09T00:28Z Jupiter */
                    Match m = re.Match(line);
                    if (!m.Success)
                    {
                        Console.WriteLine("C# TestElongFile({0} line {1}): invalid data format.", filename, lnum);
                        return 1;
                    }
                    int year = int.Parse(m.Groups[1].Value);
                    int month = int.Parse(m.Groups[2].Value);
                    int day = int.Parse(m.Groups[3].Value);
                    int hour = int.Parse(m.Groups[4].Value);
                    int minute = int.Parse(m.Groups[5].Value);
                    Body body = Enum.Parse<Body>(m.Groups[6].Value);
                    var search_date = new AstroTime(year, 1, 1, 0, 0, 0);
                    var expected_time = new AstroTime(year, month, day, hour, minute, 0);
                    AstroTime search_result = Astronomy.SearchRelativeLongitude(body, targetRelLon, search_date);
                    if (search_result == null)
                    {
                        Console.WriteLine("C# TestElongFile({0} line {1}): SearchRelativeLongitude returned null.", filename, lnum);
                        return 1;
                    }
                    double diff_minutes = (24.0 * 60.0) * (search_result.tt - expected_time.tt);
                    Console.WriteLine("{0} error = {1} minutes.", body, diff_minutes.ToString("f3"));
                    if (abs(diff_minutes) > 15.0)
                    {
                        Console.WriteLine("C# TestElongFile({0} line {1}): EXCESSIVE ERROR.", filename, lnum);
                        return 1;
                    }
                }
                Console.WriteLine("C# TestElongFile: passed {0} rows of data.", lnum);
                return 0;
            }
        }

        static int TestPlanetLongitudes(Body body, string outFileName, string zeroLonEventName)
        {
            const int startYear = 1700;
            const int stopYear = 2200;
            int count = 0;
            double rlon = 0.0;
            double min_diff = 1.0e+99;
            double max_diff = 1.0e+99;
            double sum_diff = 0.0;

            using (StreamWriter outfile = File.CreateText(outFileName))
            {
                var time = new AstroTime(startYear, 1, 1, 0, 0, 0);
                var stopTime = new AstroTime(stopYear, 1, 1, 0, 0, 0);
                while (time.tt < stopTime.tt)
                {
                    ++count;
                    string event_name = (rlon == 0.0) ? zeroLonEventName : "sup";
                    AstroTime search_result = Astronomy.SearchRelativeLongitude(body, rlon, time);
                    if (search_result == null)
                    {
                        Console.WriteLine("C# TestPlanetLongitudes({0}): SearchRelativeLongitude returned null.", body);
                        return 1;
                    }

                    if (count >= 2)
                    {
                        /* Check for consistent intervals. */
                        /* Mainly I don't want to skip over an event! */
                        double day_diff = search_result.tt - time.tt;
                        sum_diff += day_diff;
                        if (count == 2)
                        {
                            min_diff = max_diff = day_diff;
                        }
                        else
                        {
                            if (day_diff < min_diff)
                                min_diff = day_diff;

                            if (day_diff > max_diff)
                                max_diff = day_diff;
                        }
                    }

                    AstroVector geo = Astronomy.GeoVector(body, search_result, Aberration.Corrected);
                    double dist = geo.Length();
                    outfile.WriteLine("e {0} {1} {2} {3}", body, event_name, search_result.tt.ToString("g17"), dist.ToString("g17"));

                    /* Search for the opposite longitude event next time. */
                    time = search_result;
                    rlon = 180.0 - rlon;
                }
            }

            double thresh;
            switch (body)
            {
                case Body.Mercury:  thresh = 1.65;  break;
                case Body.Mars:     thresh = 1.30;  break;
                default:            thresh = 1.07;  break;
            }

            double ratio = max_diff / min_diff;
            Debug("C# TestPlanetLongitudes({0,7}): {1,5} events, ratio={2,5}, file: {3}", body, count, ratio.ToString("f3"), outFileName);

            if (ratio > thresh)
            {
                Console.WriteLine("C# TestPlanetLongitudes({0}): excessive event interval ratio.", body);
                return 1;
            }
            return 0;
        }

        static int ElongationTest()
        {
            if (0 != TestElongFile("../../longitude/opposition_2018.txt", 0.0)) return 1;
            if (0 != TestPlanetLongitudes(Body.Mercury, "csharp_longitude_Mercury.txt", "inf")) return 1;
            if (0 != TestPlanetLongitudes(Body.Venus,   "csharp_longitude_Venus.txt",   "inf")) return 1;
            if (0 != TestPlanetLongitudes(Body.Mars,    "csharp_longitude_Mars.txt",    "opp")) return 1;
            if (0 != TestPlanetLongitudes(Body.Jupiter, "csharp_longitude_Jupiter.txt", "opp")) return 1;
            if (0 != TestPlanetLongitudes(Body.Saturn,  "csharp_longitude_Saturn.txt",  "opp")) return 1;
            if (0 != TestPlanetLongitudes(Body.Uranus,  "csharp_longitude_Uranus.txt",  "opp")) return 1;
            if (0 != TestPlanetLongitudes(Body.Neptune, "csharp_longitude_Neptune.txt", "opp")) return 1;
            if (0 != TestPlanetLongitudes(Body.Pluto,   "csharp_longitude_Pluto.txt",   "opp")) return 1;

            foreach (elong_test_t et in ElongTestData)
                if (0 != TestMaxElong(et))
                    return 1;

            Console.WriteLine("C# ElongationTest: PASS");
            return 0;
        }

        static readonly Regex regexDate = new Regex(@"^(\d+)-(\d+)-(\d+)T(\d+):(\d+)(:(\d+))?Z$");

        static AstroTime ParseDate(string text)
        {
            Match m = regexDate.Match(text);
            if (!m.Success)
                throw new Exception(string.Format("ParseDate failed for string: '{0}'", text));
            int year = int.Parse(m.Groups[1].Value);
            int month = int.Parse(m.Groups[2].Value);
            int day = int.Parse(m.Groups[3].Value);
            int hour = int.Parse(m.Groups[4].Value);
            int minute = int.Parse(m.Groups[5].Value);
            int second = 0;
            if (!string.IsNullOrEmpty(m.Groups[7].Value))
                second = int.Parse(m.Groups[7].Value);
            return new AstroTime(year, month, day, hour, minute, second);
        }

        static AstroTime OptionalParseDate(string text)
        {
            if (text == "-")
                return null;

            return ParseDate(text);
        }

        static int TestMaxElong(elong_test_t test)
        {
            AstroTime searchTime = ParseDate(test.searchDate);
            AstroTime eventTime = ParseDate(test.eventDate);
            ElongationInfo evt = Astronomy.SearchMaxElongation(test.body, searchTime);
            double hour_diff = 24.0 * abs(evt.time.tt - eventTime.tt);
            double arcmin_diff = 60.0 * abs(evt.elongation - test.angle);
            Debug("C# TestMaxElong: {0,7} {1,7} elong={2,5} ({3} arcmin, {4} hours)", test.body, test.visibility, evt.elongation, arcmin_diff, hour_diff);
            if (hour_diff > 0.603)
            {
                Console.WriteLine("C# TestMaxElong({0} {1}): excessive hour error.", test.body, test.searchDate);
                return 1;
            }

            if (arcmin_diff > 3.4)
            {
                Console.WriteLine("C# TestMaxElong({0} {1}): excessive arcmin error.", test.body, test.searchDate);
                return 1;
            }

            return 0;
        }

        struct elong_test_t
        {
            public Body body;
            public string searchDate;
            public string eventDate;
            public double angle;
            public Visibility visibility;

            public elong_test_t(Body body, string searchDate, string eventDate, double angle, Visibility visibility)
            {
                this.body = body;
                this.searchDate = searchDate;
                this.eventDate = eventDate;
                this.angle = angle;
                this.visibility = visibility;
            }
        }

        static readonly elong_test_t[] ElongTestData = new elong_test_t[]
        {
            /* Max elongation data obtained from: */
            /* http://www.skycaramba.com/greatest_elongations.shtml */
            new elong_test_t( Body.Mercury, "2010-01-17T05:22Z", "2010-01-27T05:22Z", 24.80, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2010-05-16T02:15Z", "2010-05-26T02:15Z", 25.10, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2010-09-09T17:24Z", "2010-09-19T17:24Z", 17.90, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2010-12-30T14:33Z", "2011-01-09T14:33Z", 23.30, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2011-04-27T19:03Z", "2011-05-07T19:03Z", 26.60, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2011-08-24T05:52Z", "2011-09-03T05:52Z", 18.10, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2011-12-13T02:56Z", "2011-12-23T02:56Z", 21.80, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2012-04-08T17:22Z", "2012-04-18T17:22Z", 27.50, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2012-08-06T12:04Z", "2012-08-16T12:04Z", 18.70, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2012-11-24T22:55Z", "2012-12-04T22:55Z", 20.60, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2013-03-21T22:02Z", "2013-03-31T22:02Z", 27.80, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2013-07-20T08:51Z", "2013-07-30T08:51Z", 19.60, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2013-11-08T02:28Z", "2013-11-18T02:28Z", 19.50, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2014-03-04T06:38Z", "2014-03-14T06:38Z", 27.60, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2014-07-02T18:22Z", "2014-07-12T18:22Z", 20.90, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2014-10-22T12:36Z", "2014-11-01T12:36Z", 18.70, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2015-02-14T16:20Z", "2015-02-24T16:20Z", 26.70, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2015-06-14T17:10Z", "2015-06-24T17:10Z", 22.50, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2015-10-06T03:20Z", "2015-10-16T03:20Z", 18.10, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2016-01-28T01:22Z", "2016-02-07T01:22Z", 25.60, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2016-05-26T08:45Z", "2016-06-05T08:45Z", 24.20, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2016-09-18T19:27Z", "2016-09-28T19:27Z", 17.90, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2017-01-09T09:42Z", "2017-01-19T09:42Z", 24.10, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2017-05-07T23:19Z", "2017-05-17T23:19Z", 25.80, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2017-09-02T10:14Z", "2017-09-12T10:14Z", 17.90, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2017-12-22T19:48Z", "2018-01-01T19:48Z", 22.70, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2018-04-19T18:17Z", "2018-04-29T18:17Z", 27.00, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2018-08-16T20:35Z", "2018-08-26T20:35Z", 18.30, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2018-12-05T11:34Z", "2018-12-15T11:34Z", 21.30, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2019-04-01T19:40Z", "2019-04-11T19:40Z", 27.70, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2019-07-30T23:08Z", "2019-08-09T23:08Z", 19.00, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2019-11-18T10:31Z", "2019-11-28T10:31Z", 20.10, Visibility.Morning ),
            new elong_test_t( Body.Mercury, "2010-03-29T23:32Z", "2010-04-08T23:32Z", 19.40, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2010-07-28T01:03Z", "2010-08-07T01:03Z", 27.40, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2010-11-21T15:42Z", "2010-12-01T15:42Z", 21.50, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2011-03-13T01:07Z", "2011-03-23T01:07Z", 18.60, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2011-07-10T04:56Z", "2011-07-20T04:56Z", 26.80, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2011-11-04T08:40Z", "2011-11-14T08:40Z", 22.70, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2012-02-24T09:39Z", "2012-03-05T09:39Z", 18.20, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2012-06-21T02:00Z", "2012-07-01T02:00Z", 25.70, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2012-10-16T21:59Z", "2012-10-26T21:59Z", 24.10, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2013-02-06T21:24Z", "2013-02-16T21:24Z", 18.10, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2013-06-02T16:45Z", "2013-06-12T16:45Z", 24.30, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2013-09-29T09:59Z", "2013-10-09T09:59Z", 25.30, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2014-01-21T10:00Z", "2014-01-31T10:00Z", 18.40, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2014-05-15T07:06Z", "2014-05-25T07:06Z", 22.70, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2014-09-11T22:20Z", "2014-09-21T22:20Z", 26.40, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2015-01-04T20:26Z", "2015-01-14T20:26Z", 18.90, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2015-04-27T04:46Z", "2015-05-07T04:46Z", 21.20, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2015-08-25T10:20Z", "2015-09-04T10:20Z", 27.10, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2015-12-19T03:11Z", "2015-12-29T03:11Z", 19.70, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2016-04-08T14:00Z", "2016-04-18T14:00Z", 19.90, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2016-08-06T21:24Z", "2016-08-16T21:24Z", 27.40, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2016-12-01T04:36Z", "2016-12-11T04:36Z", 20.80, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2017-03-22T10:24Z", "2017-04-01T10:24Z", 19.00, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2017-07-20T04:34Z", "2017-07-30T04:34Z", 27.20, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2017-11-14T00:32Z", "2017-11-24T00:32Z", 22.00, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2018-03-05T15:07Z", "2018-03-15T15:07Z", 18.40, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2018-07-02T05:24Z", "2018-07-12T05:24Z", 26.40, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2018-10-27T15:25Z", "2018-11-06T15:25Z", 23.30, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2019-02-17T01:23Z", "2019-02-27T01:23Z", 18.10, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2019-06-13T23:14Z", "2019-06-23T23:14Z", 25.20, Visibility.Evening ),
            new elong_test_t( Body.Mercury, "2019-10-10T04:00Z", "2019-10-20T04:00Z", 24.60, Visibility.Evening ),
            new elong_test_t( Body.Venus,   "2010-12-29T15:57Z", "2011-01-08T15:57Z", 47.00, Visibility.Morning ),
            new elong_test_t( Body.Venus,   "2012-08-05T08:59Z", "2012-08-15T08:59Z", 45.80, Visibility.Morning ),
            new elong_test_t( Body.Venus,   "2014-03-12T19:25Z", "2014-03-22T19:25Z", 46.60, Visibility.Morning ),
            new elong_test_t( Body.Venus,   "2015-10-16T06:57Z", "2015-10-26T06:57Z", 46.40, Visibility.Morning ),
            new elong_test_t( Body.Venus,   "2017-05-24T13:09Z", "2017-06-03T13:09Z", 45.90, Visibility.Morning ),
            new elong_test_t( Body.Venus,   "2018-12-27T04:24Z", "2019-01-06T04:24Z", 47.00, Visibility.Morning ),
            new elong_test_t( Body.Venus,   "2010-08-10T03:19Z", "2010-08-20T03:19Z", 46.00, Visibility.Evening ),
            new elong_test_t( Body.Venus,   "2012-03-17T08:03Z", "2012-03-27T08:03Z", 46.00, Visibility.Evening ),
            new elong_test_t( Body.Venus,   "2013-10-22T08:00Z", "2013-11-01T08:00Z", 47.10, Visibility.Evening ),
            new elong_test_t( Body.Venus,   "2015-05-27T18:46Z", "2015-06-06T18:46Z", 45.40, Visibility.Evening ),
            new elong_test_t( Body.Venus,   "2017-01-02T13:19Z", "2017-01-12T13:19Z", 47.10, Visibility.Evening ),
            new elong_test_t( Body.Venus,   "2018-08-07T17:02Z", "2018-08-17T17:02Z", 45.90, Visibility.Evening )
        };

        static double PlanetOrbitalPeriod(Body body)
        {
            switch (body)
            {
            case Body.Mercury:  return     87.969;
            case Body.Venus:    return    224.701;
            case Body.Earth:    return    365.256;
            case Body.Mars:     return    686.980;
            case Body.Jupiter:  return   4332.589;
            case Body.Saturn:   return  10759.22;
            case Body.Uranus:   return  30685.4;
            case Body.Neptune:  return  60189.0;
            case Body.Pluto:    return  90560.0;
            default:
                throw new ArgumentException(string.Format("Invalid body {0}", body));
            }
        }

        static int PlanetApsisTest()
        {
            const string testDataPath = "../../apsides";
            var start_time = new AstroTime(1700, 1, 1, 0, 0, 0);
            for (Body body = Body.Mercury; body <= Body.Pluto; ++body)
            {
                double period = PlanetOrbitalPeriod(body);
                double max_dist_ratio = 0.0;
                double max_diff_days = 0.0;
                double min_interval = -1.0;
                double max_interval = -1.0;
                ApsisInfo apsis = Astronomy.SearchPlanetApsis(body, start_time);
                int count = 1;

                string filename = Path.Combine(testDataPath, string.Format("apsis_{0}.txt", (int)body));
                using (StreamReader infile = File.OpenText(filename))
                {
                    string line;
                    while (null != (line = infile.ReadLine()))
                    {
                        /* Parse the line of test data. */
                        string[] token = Tokenize(line);
                        if (token.Length != 3)
                        {
                            Console.WriteLine("C# PlanetApsisTest({0} line {1}): Invalid data format: {2} tokens", filename, count, token.Length);
                            return 1;
                        }
                        int expected_kind = int.Parse(token[0]);
                        AstroTime expected_time = ParseDate(token[1]);
                        double expected_distance = double.Parse(token[2]);

                        /* Compare computed values against expected values. */
                        if ((int)apsis.kind != expected_kind)
                        {
                            Console.WriteLine("C# PlanetApsisTest({0} line {1}): WRONG APSIS KIND", filename, count);
                            return 1;
                        }

                        double diff_days = abs(expected_time.tt - apsis.time.tt);
                        max_diff_days = max(max_diff_days, diff_days);
                        double diff_degrees = (diff_days / period) * 360.0;
                        double degree_threshold = (body == Body.Pluto) ? 0.262 : 0.1;
                        if (diff_degrees > degree_threshold)
                        {
                            Console.WriteLine("C# PlanetApsis: FAIL - {0} exceeded angular threshold ({1} vs {2} degrees)", body, diff_degrees, degree_threshold);
                            return 1;
                        }

                        double diff_dist_ratio = abs(expected_distance - apsis.dist_au) / expected_distance;
                        max_dist_ratio = max(max_dist_ratio, diff_dist_ratio);
                        if (diff_dist_ratio > 1.05e-4)
                        {
                            Console.WriteLine("C# PlanetApsisTest({0} line {1}): distance ratio {2} is too large.", filename, count, diff_dist_ratio);
                            return 1;
                        }

                        /* Calculate the next apsis. */
                        AstroTime prev_time = apsis.time;
                        apsis = Astronomy.NextPlanetApsis(body, apsis);

                        /* Update statistics. */
                        ++count;
                        double interval = apsis.time.tt - prev_time.tt;
                        if (min_interval < 0.0)
                        {
                            min_interval = max_interval = interval;
                        }
                        else
                        {
                            min_interval = min(min_interval, interval);
                            max_interval = max(max_interval, interval);
                        }
                    }
                }

                if (count < 2)
                {
                    Console.WriteLine("C# PlanetApsis: FAILED to find apsides for {0}", body);
                    return 1;
                }

                Debug("C# PlanetApsis: {0} apsides for {1,-9} -- intervals: min={2:0.00}, max={3:0.00}, ratio={4:0.000000}; max day={5}, degrees={6:0.000}, dist ratio={7}",
                    count, body,
                    min_interval, max_interval, max_interval / min_interval,
                    max_diff_days,
                    (max_diff_days / period) * 360.0,
                    max_dist_ratio);
            }

            Console.WriteLine("C# PlanetApsis: PASS");
            return 0;
        }

        static int LunarApsisTest()
        {
            const string inFileName = "../../apsides/moon.txt";
            using (StreamReader infile = File.OpenText(inFileName))
            {
                int lnum = 0;
                string line;
                var start_time = new AstroTime(2001,1, 1, 0, 0, 0);
                ApsisInfo apsis = new ApsisInfo();
                double max_minutes = 0.0;
                double max_km = 0.0;
                /*
                    0 2001-01-10T08:59Z 357132
                    1 2001-01-24T19:02Z 406565
                */
                var regex = new Regex(@"^\s*([01])\s+(\d+)-(\d+)-(\d+)T(\d+):(\d+)Z\s+(\d+)\s*$");
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    Match m = regex.Match(line);
                    if (!m.Success)
                    {
                        Console.WriteLine("C# LunarApsisTest({0} line {1}): invalid data format.", inFileName, lnum);
                        return 1;
                    }
                    ApsisKind kind = (m.Groups[1].Value == "0") ? ApsisKind.Pericenter : ApsisKind.Apocenter;

                    int year = int.Parse(m.Groups[2].Value);
                    int month = int.Parse(m.Groups[3].Value);
                    int day = int.Parse(m.Groups[4].Value);
                    int hour = int.Parse(m.Groups[5].Value);
                    int minute = int.Parse(m.Groups[6].Value);
                    double dist_km = double.Parse(m.Groups[7].Value);

                    var correct_time = new AstroTime(year, month, day, hour, minute, 0);

                    if (lnum == 1)
                        apsis = Astronomy.SearchLunarApsis(start_time);
                    else
                        apsis = Astronomy.NextLunarApsis(apsis);

                    if (kind != apsis.kind)
                    {
                        Console.WriteLine("C# LunarApsisTest({0} line {1}): expected apsis kind {2} but found {3}", inFileName, lnum, kind, apsis.kind);
                        return 1;
                    }
                    double diff_minutes = (24.0 * 60.0) * abs(apsis.time.ut - correct_time.ut);
                    if (diff_minutes > 35.0)
                    {
                        Console.WriteLine("C# LunarApsisTest({0} line {1}): excessive time error: {2} minutes", inFileName, lnum, diff_minutes);
                        return 1;
                    }
                    double diff_km =  abs(apsis.dist_km - dist_km);
                    if (diff_km > 25.0)
                    {
                        Console.WriteLine("C# LunarApsisTest({0} line {1}): excessive distance error: {2} km", inFileName, lnum, diff_km);
                        return 1;
                    }

                    if (diff_minutes > max_minutes)
                        max_minutes = diff_minutes;

                    if (diff_km > max_km)
                        max_km = diff_km;
                }
                Console.WriteLine("C# LunarApsisTest: Found {0} events, max time error = {1} minutes, max distance error = {2} km.", lnum, max_minutes, max_km);
                return 0;
            }
        }

        class JplDateTime
        {
            public string Rest;
            public AstroTime Time;
        }

        static readonly Regex JplRegex = new Regex(@"^\s*(\d{4})-(Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Oct|Nov|Dec)-(\d{2})\s+(\d{2}):(\d{2})\s+(.*)");


        static JplDateTime ParseJplHorizonsDateTime(string line)
        {
            Match m = JplRegex.Match(line);
            if (!m.Success)
                return null;
            int year = int.Parse(m.Groups[1].Value);
            string mtext = m.Groups[2].Value;
            int day = int.Parse(m.Groups[3].Value);
            int hour = int.Parse(m.Groups[4].Value);
            int minute = int.Parse(m.Groups[5].Value);
            string rest = m.Groups[6].Value;
            int month;
            switch (mtext)
            {
                case "Jan": month =  1;  break;
                case "Feb": month =  2;  break;
                case "Mar": month =  3;  break;
                case "Apr": month =  4;  break;
                case "May": month =  5;  break;
                case "Jun": month =  6;  break;
                case "Jul": month =  7;  break;
                case "Aug": month =  8;  break;
                case "Sep": month =  9;  break;
                case "Oct": month = 10;  break;
                case "Nov": month = 11;  break;
                case "Dec": month = 12;  break;
                default:
                    throw new Exception(string.Format("Internal error: unexpected month name '{0}'", mtext));
            }
            AstroTime time = new AstroTime(year, month, day, hour, minute, 0);
            return new JplDateTime { Rest=rest, Time=time };
        }

        static int CheckMagnitudeData(Body body, string filename)
        {
            using (StreamReader infile = File.OpenText(filename))
            {
                const double limit = 0.012;
                double diff_lo = 0.0;
                double diff_hi = 0.0;
                double sum_squared_diff = 0.0;
                int lnum = 0;
                int count = 0;
                string line;
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    JplDateTime jpl = ParseJplHorizonsDateTime(line);
                    if (jpl == null)
                        continue;

                    string[] token = Tokenize(jpl.Rest);
                    if (token.Length > 0 && token[0] == "n.a.")
                        continue;

                    if (token.Length != 7)
                    {
                        Console.WriteLine("C# CheckMagnitudeData({0} line {1}): invalid data format", lnum, filename);
                        return 1;
                    }
                    double mag;
                    if (!double.TryParse(token[0], out mag))
                    {
                        Console.WriteLine("C# CheckMagnitudeData({0} line {1}): cannot parse number from '{2}'", filename, lnum, token[0]);
                        return 1;
                    }
                    var illum = Astronomy.Illumination(body, jpl.Time);
                    double diff = illum.mag - mag;
                    if (abs(diff) > limit)
                    {
                        Console.WriteLine("C# CheckMagnitudeData({0} line {1}): EXCESSIVE ERROR: correct mag={0}, calc mag={1}, diff={2}", mag, illum.mag, diff);
                        return 1;
                    }
                    sum_squared_diff += diff * diff;
                    if (count == 0)
                    {
                        diff_lo = diff_hi = diff;
                    }
                    else
                    {
                        if (diff < diff_lo)
                            diff_lo = diff;

                        if (diff > diff_hi)
                            diff_hi = diff;
                    }
                    ++count;
                }
                if (count == 0)
                {
                    Console.WriteLine("C# CheckMagnitudeData: Data not find any data in file: {0}", filename);
                    return 1;
                }
                double rms = sqrt(sum_squared_diff / count);
                Debug("C# CheckMagnitudeData: {0} {1} rows diff_lo={2} diff_hi={3} rms={4}", filename, count, diff_lo, diff_hi, rms);
                return 0;
            }
        }

        struct saturn_test_case
        {
            public readonly string date;
            public readonly double mag;
            public readonly double tilt;

            public saturn_test_case(string date, double mag, double tilt)
            {
                this.date = date;
                this.mag = mag;
                this.tilt = tilt;
            }
        }

        /* JPL Horizons does not include Saturn's rings in its magnitude models. */
        /* I still don't have authoritative test data for Saturn's magnitude. */
        /* For now, I just test for consistency with Paul Schlyter's formulas at: */
        /* http://www.stjarnhimlen.se/comp/ppcomp.html#15 */
        static saturn_test_case[] saturn_data = new saturn_test_case[]
        {
            new saturn_test_case("1972-01-01T00:00Z", -0.31904865,  +24.50061220),
            new saturn_test_case("1980-01-01T00:00Z", +0.85213663,   -1.85761461),
            new saturn_test_case("2009-09-04T00:00Z", +1.01626809,   +0.08380716),
            new saturn_test_case("2017-06-15T00:00Z", -0.12318790,  -26.60871409),
            new saturn_test_case("2019-05-01T00:00Z", +0.32954097,  -23.53880802),
            new saturn_test_case("2025-09-25T00:00Z", +0.51286575,   +1.52327932),
            new saturn_test_case("2032-05-15T00:00Z", -0.04652109,  +26.95717765)
        };

        static int CheckSaturn()
        {
            int error = 0;

            foreach (saturn_test_case data in saturn_data)
            {
                AstroTime time = ParseDate(data.date);

                IllumInfo illum = Astronomy.Illumination(Body.Saturn, time);
                Debug("C# Saturn: date={0}  calc mag={1}  ring_tilt={2}", data.date, illum.mag, illum.ring_tilt);

                double mag_diff = abs(illum.mag - data.mag);
                if (mag_diff > 1.0e-4)
                {
                    Console.WriteLine("C# CheckSaturn ERROR: Excessive magnitude error {0}", mag_diff);
                    error = 1;      /* keep going -- print all errors before exiting */
                }

                double tilt_diff = abs(illum.ring_tilt - data.tilt);
                if (tilt_diff > 3.0e-5)
                {
                    Console.WriteLine("C# CheckSaturn ERROR: Excessive ring tilt error {0}", tilt_diff);
                    error = 1;      /* keep going -- print all errors before exiting */
                }
            }

            return error;
        }

        static int TestMaxMag(Body body, string filename)
        {
            /*
                Example of input data:

                2001-02-21T08:00Z 2001-02-27T08:00Z 23.17 19.53 -4.84

                JPL Horizons test data has limited floating point precision in the magnitude values.
                There is a pair of dates for the beginning and end of the max magnitude period,
                given the limited precision.
                We pick the point halfway between as the supposed max magnitude time.
            */
            using (StreamReader infile = File.OpenText(filename))
            {
                int lnum = 0;
                string line;
                var search_time = new AstroTime(2001, 1, 1, 0, 0, 0);
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    string[] token = Tokenize(line);
                    if (token.Length != 5)
                    {
                        Console.WriteLine("C# TestMaxMag({0} line {1}): invalid data format", filename, lnum);
                        return 1;
                    }
                    AstroTime time1 = ParseDate(token[0]);
                    AstroTime time2 = ParseDate(token[1]);
                    double correct_angle1 = double.Parse(token[2]);
                    double correct_angle2 = double.Parse(token[3]);
                    double correct_mag = double.Parse(token[4]);
                    AstroTime center_time = time1.AddDays(0.5*(time2.ut - time1.ut));
                    IllumInfo illum = Astronomy.SearchPeakMagnitude(body, search_time);
                    double mag_diff = abs(illum.mag - correct_mag);
                    double hours_diff = 24.0 * abs(illum.time.ut - center_time.ut);
                    Debug("C# TestMaxMag: mag_diff={0}, hours_diff={1}", mag_diff, hours_diff);
                    if (hours_diff > 7.1)
                    {
                        Console.WriteLine("C# TestMaxMag({0} line {1}): EXCESSIVE TIME DIFFERENCE.", filename, lnum);
                        return 1;
                    }
                    if (mag_diff > 0.005)
                    {
                        Console.WriteLine("C# TestMaxMag({0} line {1}): EXCESSIVE MAGNITUDE DIFFERENCE.", filename, lnum);
                        return 1;
                    }
                    search_time = time2;
                }
                Debug("C# TestMaxMag: Processed {0} lines from file {1}", lnum, filename);
                return 0;
            }
        }

        static int MagnitudeTest()
        {
            int nfailed = 0;
            nfailed += CheckMagnitudeData(Body.Sun, "../../magnitude/Sun.txt");
            nfailed += CheckMagnitudeData(Body.Moon, "../../magnitude/Moon.txt");
            nfailed += CheckMagnitudeData(Body.Mercury, "../../magnitude/Mercury.txt");
            nfailed += CheckMagnitudeData(Body.Venus, "../../magnitude/Venus.txt");
            nfailed += CheckMagnitudeData(Body.Mars, "../../magnitude/Mars.txt");
            nfailed += CheckMagnitudeData(Body.Jupiter, "../../magnitude/Jupiter.txt");
            nfailed += CheckSaturn();
            nfailed += CheckMagnitudeData(Body.Uranus, "../../magnitude/Uranus.txt");
            nfailed += CheckMagnitudeData(Body.Neptune, "../../magnitude/Neptune.txt");
            nfailed += CheckMagnitudeData(Body.Pluto, "../../magnitude/Pluto.txt");
            nfailed += TestMaxMag(Body.Venus, "../../magnitude/maxmag_Venus.txt");
            if (nfailed == 0)
                Console.WriteLine("C# MagnitudeTest: PASS");
            else
                Console.WriteLine("C# MagnitudeTest: FAILED {0} test(s).", nfailed);
            return nfailed;
        }

        static double VectorDiff(AstroVector a, AstroVector b)
        {
            double dx = a.x - b.x;
            double dy = a.y - b.y;
            double dz = a.z - b.z;
            return sqrt(dx*dx + dy*dy + dz*dz);
        }

        static int CompareMatrices(string caller, RotationMatrix a, RotationMatrix b, double tolerance)
        {
            for (int i=0; i<3; ++i)
            {
                for (int j=0; j<3; ++j)
                {
                    double diff = abs(a.rot[i,j] - b.rot[i,j]);
                    if (diff > tolerance)
                    {
                        Console.WriteLine("C# CompareMatrices ERROR({0}): matrix[{1},{2}]={3}, expected {4}, diff {5}", caller, i, j, a.rot[i,j], b.rot[i,j], diff);
                        return 1;
                    }
                }
            }
            return 0;
        }

        static int Rotation_MatrixInverse()
        {
            var a = new RotationMatrix(new double[3,3]);
            a.rot[0, 0] = 1.0; a.rot[1, 0] = 2.0; a.rot[2, 0] = 3.0;
            a.rot[0, 1] = 4.0; a.rot[1, 1] = 5.0; a.rot[2, 1] = 6.0;
            a.rot[0, 2] = 7.0; a.rot[1, 2] = 8.0; a.rot[2, 2] = 9.0;

            var v = new RotationMatrix(new double[3,3]);
            v.rot[0, 0] = 1.0; v.rot[1, 0] = 4.0; v.rot[2, 0] = 7.0;
            v.rot[0, 1] = 2.0; v.rot[1, 1] = 5.0; v.rot[2, 1] = 8.0;
            v.rot[0, 2] = 3.0; v.rot[1, 2] = 6.0; v.rot[2, 2] = 9.0;

            RotationMatrix b = Astronomy.InverseRotation(a);
            if (0 != CompareMatrices("Rotation_MatrixInverse", b, v, 0.0)) return 1;
            Console.WriteLine("C# Rotation_MatrixInverse: PASS");
            return 0;
        }

        static int Rotation_MatrixMultiply()
        {
            var a = new RotationMatrix(new double[3,3]);
            a.rot[0, 0] = 1.0; a.rot[1, 0] = 2.0; a.rot[2, 0] = 3.0;
            a.rot[0, 1] = 4.0; a.rot[1, 1] = 5.0; a.rot[2, 1] = 6.0;
            a.rot[0, 2] = 7.0; a.rot[1, 2] = 8.0; a.rot[2, 2] = 9.0;

            var b = new RotationMatrix(new double[3,3]);
            b.rot[0, 0] = 10.0; b.rot[1, 0] = 11.0; b.rot[2, 0] = 12.0;
            b.rot[0, 1] = 13.0; b.rot[1, 1] = 14.0; b.rot[2, 1] = 15.0;
            b.rot[0, 2] = 16.0; b.rot[1, 2] = 17.0; b.rot[2, 2] = 18.0;

            var v = new RotationMatrix(new double[3,3]);
            v.rot[0, 0] =  84.0; v.rot[1, 0] =  90.0; v.rot[2, 0] =  96.0;
            v.rot[0, 1] = 201.0; v.rot[1, 1] = 216.0; v.rot[2, 1] = 231.0;
            v.rot[0, 2] = 318.0; v.rot[1, 2] = 342.0; v.rot[2, 2] = 366.0;

            RotationMatrix c = Astronomy.CombineRotation(b, a);
            if (0 != CompareMatrices("Rotation_MatrixMultiply", c, v, 0.0)) return 1;
            Console.WriteLine("C# Rotation_MatrixMultiply: PASS");
            return 0;
        }

        static int Test_EQJ_ECL()
        {
            RotationMatrix r = Astronomy.Rotation_EQJ_ECL();

            /* Calculate heliocentric Earth position at a test time. */
            var time = new AstroTime(2019, 12, 8, 19, 39, 15);
            var ev = Astronomy.HelioVector(Body.Earth, time);

            /* Use the older function to calculate ecliptic vector and angles. */
            Ecliptic ecl = Astronomy.EquatorialToEcliptic(ev);
            Debug("C# Test_EQJ_ECL ecl = ({0}, {1}, {2})", ecl.ex, ecl.ey, ecl.ez);

            /* Now compute the same vector via rotation matrix. */
            AstroVector ee = Astronomy.RotateVector(r, ev);
            double dx = ee.x - ecl.ex;
            double dy = ee.y - ecl.ey;
            double dz = ee.z - ecl.ez;
            double diff = sqrt(dx*dx + dy*dy + dz*dz);
            Debug("C# Test_EQJ_ECL ee = ({0}, {1}, {2}); diff={3}", ee.x, ee.y, ee.z, diff);
            if (diff > 1.0e-16)
            {
                Console.WriteLine("C# Test_EQJ_ECL: EXCESSIVE VECTOR ERROR");
                return 1;
            }

            /* Reverse the test: go from ecliptic back to equatorial. */
            r = Astronomy.Rotation_ECL_EQJ();
            AstroVector et = Astronomy.RotateVector(r, ee);
            diff = VectorDiff(et, ev);
            Debug("C# Test_EQJ_ECL  ev diff={0}", diff);
            if (diff > 2.0e-16)
            {
                Console.WriteLine("C# Test_EQJ_ECL: EXCESSIVE REVERSE ROTATION ERROR");
                return 1;
            }

            Debug("C# Test_EQJ_ECL: PASS");
            return 0;
        }

        static int Test_EQJ_EQD(Body body)
        {
            // Verify convresion of equatorial J2000 to equatorial of-date, and back.
            var time = new AstroTime(2019, 12, 8, 20, 50, 0);
            var observer = new Observer(35, -85, 0);
            Equatorial eq2000 = Astronomy.Equator(body, time, observer, EquatorEpoch.J2000, Aberration.Corrected);
            Equatorial eqdate = Astronomy.Equator(body, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);
            AstroVector v2000 = Astronomy.VectorFromEquator(eq2000, time);
            RotationMatrix r = Astronomy.Rotation_EQJ_EQD(time);
            AstroVector vdate = Astronomy.RotateVector(r, v2000);
            Equatorial eqcheck = Astronomy.EquatorFromVector(vdate);

            double ra_diff = abs(eqcheck.ra - eqdate.ra);
            double dec_diff = abs(eqcheck.dec - eqdate.dec);
            double dist_diff = abs(eqcheck.dist - eqdate.dist);
            Debug("C# Test_EQJ_EQD: {0} ra={1}, dec={2}, dist={3}, ra_diff={4}, dec_diff={5}, dist_diff={6}",
                body, eqdate.ra, eqdate.dec, eqdate.dist, ra_diff, dec_diff, dist_diff);

            if (ra_diff > 1.0e-14 || dec_diff > 1.0e-14 || dist_diff > 4.0e-15)
            {
                Console.WriteLine("C# Test_EQJ_EQD: EXCESSIVE ERROR");
                return 1;
            }

            r = Astronomy.Rotation_EQD_EQJ(time);
            AstroVector t2000 = Astronomy.RotateVector(r, vdate);
            double diff = VectorDiff(t2000, v2000);
            Debug("C# Test_EQJ_EQD: {0} inverse diff = {1}", body, diff);
            if (diff > 5.0e-15)
            {
                Console.WriteLine("C# Test_EQJ_EQD: EXCESSIVE INVERSE ERROR");
                return 1;
            }

            Debug("C# Test_EQJ_EQD: PASS");
            return 0;
        }

        static int Test_EQD_HOR(Body body)
        {
            var time = new AstroTime(1970, 12, 13, 5, 15, 0);
            var observer = new Observer(-37.0, +45.0, 0.0);
            Equatorial eqd = Astronomy.Equator(body, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);
            Topocentric hor = Astronomy.Horizon(time, observer, eqd.ra, eqd.dec, Refraction.Normal);
            AstroVector vec_eqd = Astronomy.VectorFromEquator(eqd, time);
            RotationMatrix rot = Astronomy.Rotation_EQD_HOR(time, observer);
            AstroVector vec_hor = Astronomy.RotateVector(rot, vec_eqd);
            Spherical sphere = Astronomy.HorizonFromVector(vec_hor, Refraction.Normal);

            double diff_alt = abs(sphere.lat - hor.altitude);
            double diff_az = abs(sphere.lon - hor.azimuth);

            Debug("C# Test_EQD_HOR {0}: trusted alt={1}, az={2}; test alt={3}, az={4}; diff_alt={5}, diff_az={6}",
                body, hor.altitude, hor.azimuth, sphere.lat, sphere.lon, diff_alt, diff_az);

            if (diff_alt > 3.0e-14 || diff_az > 4e-14)
            {
                Console.WriteLine("C# Test_EQD_HOR: EXCESSIVE HORIZONTAL ERROR.");
                return 1;
            }

            /* Confirm that we can convert back to horizontal vector. */
            AstroVector check_hor = Astronomy.VectorFromHorizon(sphere, time, Refraction.Normal);
            double diff = VectorDiff(check_hor, vec_hor);
            Debug("C# Test_EQD_HOR {0}: horizontal recovery: diff = {1}", body, diff);
            if (diff > 3.0e-15)
            {
                Console.WriteLine("C# Test_EQD_HOR: EXCESSIVE ERROR IN HORIZONTAL RECOVERY.");
                return 1;
            }

            /* Verify the inverse translation from horizontal vector to equatorial of-date vector. */
            rot = Astronomy.Rotation_HOR_EQD(time, observer);
            AstroVector check_eqd = Astronomy.RotateVector(rot, vec_hor);
            diff = VectorDiff(check_eqd, vec_eqd);
            Debug("C# Test_EQD_HOR {0}: OFDATE inverse rotation diff = {1}", body, diff);
            if (diff > 2.0e-15)
            {
                Console.WriteLine("C# Test_EQD_HOR: EXCESSIVE OFDATE INVERSE HORIZONTAL ERROR.");
                return 1;
            }

            /* Exercise HOR to EQJ translation. */
            Equatorial eqj = Astronomy.Equator(body, time, observer, EquatorEpoch.J2000, Aberration.Corrected);
            AstroVector vec_eqj = Astronomy.VectorFromEquator(eqj, time);

            rot = Astronomy.Rotation_HOR_EQJ(time, observer);
            AstroVector check_eqj = Astronomy.RotateVector(rot, vec_hor);
            diff = VectorDiff(check_eqj, vec_eqj);
            Debug("C# Test_EQD_HOR {0}: J2000 inverse rotation diff = {1}", body, diff);
            if (diff > 6.0e-15)
            {
                Console.WriteLine("C# Test_EQD_HOR: EXCESSIVE J2000 INVERSE HORIZONTAL ERROR.");
                return 1;
            }

            /* Verify the inverse translation: EQJ to HOR. */
            rot = Astronomy.Rotation_EQJ_HOR(time, observer);
            check_hor = Astronomy.RotateVector(rot, vec_eqj);
            diff = VectorDiff(check_hor, vec_hor);
            Debug("C# Test_EQD_HOR {0}: EQJ inverse rotation diff = {1}", body, diff);
            if (diff > 3e-15)
            {
                Console.WriteLine("C# Test_EQD_HOR: EXCESSIVE EQJ INVERSE HORIZONTAL ERROR.");
                return 1;
            }

            Debug("C# Test_EQD_HOR: PASS");
            return 0;
        }


        static int CheckInverse(string aname, string bname, RotationMatrix arot, RotationMatrix brot)
        {
            RotationMatrix crot = Astronomy.CombineRotation(arot, brot);

            var rot = new double[3,3];
            rot[0, 0] = 1; rot[1, 0] = 0; rot[2, 0] = 0;
            rot[0, 1] = 0; rot[1, 1] = 1; rot[2, 1] = 0;
            rot[0, 2] = 0; rot[1, 2] = 0; rot[2, 2] = 1;
            var identity = new RotationMatrix(rot);

            string caller = "CheckInverse(" + aname + ", " + bname + ")";
            return CompareMatrices(caller, crot, identity, 2.0e-15);
        }


        static int CheckCycle(
            string aname, string bname, string cname,
            RotationMatrix arot, RotationMatrix brot, RotationMatrix crot)
        {
            RotationMatrix xrot = Astronomy.CombineRotation(arot, brot);
            RotationMatrix irot = Astronomy.InverseRotation(xrot);
            string name = string.Format("CheckCycle({0}, {1}, {2})", aname, bname, cname);
            return CompareMatrices(name, crot, irot, 2.0e-15);
        }


        static int Test_RotRoundTrip()
        {
            AstroTime time = new AstroTime(2067, 5, 30, 14, 45, 0);
            Observer observer = new Observer(+28.0, -82.0, 0.0);

            /*
                In each round trip, calculate a forward rotation and a backward rotation.
                Verify the two are inverse matrices.
            */

            /* Round trip #1: EQJ <==> EQD. */
            RotationMatrix eqj_eqd = Astronomy.Rotation_EQJ_EQD(time);
            RotationMatrix eqd_eqj = Astronomy.Rotation_EQD_EQJ(time);
            if (0 != CheckInverse(nameof(eqj_eqd), nameof(eqd_eqj), eqj_eqd, eqd_eqj)) return 1;

            /* Round trip #2: EQJ <==> ECL. */
            RotationMatrix eqj_ecl = Astronomy.Rotation_EQJ_ECL();
            RotationMatrix ecl_eqj = Astronomy.Rotation_ECL_EQJ();
            if (0 != CheckInverse(nameof(eqj_ecl), nameof(ecl_eqj), eqj_ecl, ecl_eqj)) return 1;

            /* Round trip #3: EQJ <==> HOR. */
            RotationMatrix eqj_hor = Astronomy.Rotation_EQJ_HOR(time, observer);
            RotationMatrix hor_eqj = Astronomy.Rotation_HOR_EQJ(time, observer);
            if (0 != CheckInverse(nameof(eqj_hor), nameof(hor_eqj), eqj_hor, hor_eqj)) return 1;

            /* Round trip #4: EQD <==> HOR. */
            RotationMatrix eqd_hor = Astronomy.Rotation_EQD_HOR(time, observer);
            RotationMatrix hor_eqd = Astronomy.Rotation_HOR_EQD(time, observer);
            if (0 != CheckInverse(nameof(eqd_hor), nameof(hor_eqd), eqd_hor, hor_eqd)) return 1;

            /* Round trip #5: EQD <==> ECL. */
            RotationMatrix eqd_ecl = Astronomy.Rotation_EQD_ECL(time);
            RotationMatrix ecl_eqd = Astronomy.Rotation_ECL_EQD(time);
            if (0 != CheckInverse(nameof(eqd_ecl), nameof(ecl_eqd), eqd_ecl, ecl_eqd)) return 1;

            /* Round trip #6: HOR <==> ECL. */
            RotationMatrix hor_ecl = Astronomy.Rotation_HOR_ECL(time, observer);
            RotationMatrix ecl_hor = Astronomy.Rotation_ECL_HOR(time, observer);
            if (0 != CheckInverse(nameof(hor_ecl), nameof(ecl_hor), hor_ecl, ecl_hor)) return 1;

            /*
                Verify that combining different sequences of rotations result
                in the expected combination.
                For example, (EQJ ==> HOR ==> ECL) must be the same matrix as (EQJ ==> ECL).
                Each of these is a "triangle" of relationships between 3 orientations.
                There are 4 possible ways to pick 3 orientations from the 4 to form a triangle.
                Because we have just proved that each transformation is reversible,
                we only need to verify the triangle in one cyclic direction.
            */
            if (0 != CheckCycle(nameof(eqj_ecl), nameof(ecl_eqd), nameof(eqd_eqj), eqj_ecl, ecl_eqd, eqd_eqj)) return 1;     /* excluded corner = HOR */
            if (0 != CheckCycle(nameof(eqj_hor), nameof(hor_ecl), nameof(ecl_eqj), eqj_hor, hor_ecl, ecl_eqj)) return 1;     /* excluded corner = EQD */
            if (0 != CheckCycle(nameof(eqj_hor), nameof(hor_eqd), nameof(eqd_eqj), eqj_hor, hor_eqd, eqd_eqj)) return 1;     /* excluded corner = ECL */
            if (0 != CheckCycle(nameof(ecl_eqd), nameof(eqd_hor), nameof(hor_ecl), ecl_eqd, eqd_hor, hor_ecl)) return 1;     /* excluded corner = EQJ */

            Debug("C# Test_RotRoundTrip: PASS");
            return 0;
        }

        static int RotationTest()
        {
            if (0 != Rotation_MatrixInverse()) return 1;
            if (0 != Rotation_MatrixMultiply()) return 1;
            if (0 != Test_EQJ_ECL()) return 1;

            if (0 != Test_EQJ_EQD(Body.Mercury)) return 1;
            if (0 != Test_EQJ_EQD(Body.Venus)) return 1;
            if (0 != Test_EQJ_EQD(Body.Mars)) return 1;
            if (0 != Test_EQJ_EQD(Body.Jupiter)) return 1;
            if (0 != Test_EQJ_EQD(Body.Saturn)) return 1;

            if (0 != Test_EQD_HOR(Body.Mercury)) return 1;
            if (0 != Test_EQD_HOR(Body.Venus)) return 1;
            if (0 != Test_EQD_HOR(Body.Mars)) return 1;
            if (0 != Test_EQD_HOR(Body.Jupiter)) return 1;
            if (0 != Test_EQD_HOR(Body.Saturn)) return 1;

            if (0 != Test_RotRoundTrip()) return 1;

            Console.WriteLine("C# RotationTest: PASS");
            return 0;
        }

        static int RefractionTest()
        {
            for (double alt = -90.1; alt <= +90.1; alt += 0.001)
            {
                double refr = Astronomy.RefractionAngle(Refraction.Normal, alt);
                double corrected = alt + refr;
                double inv_refr = Astronomy.InverseRefractionAngle(Refraction.Normal, corrected);
                double check_alt = corrected + inv_refr;
                double diff = abs(check_alt - alt);
                if (diff > 2.0e-14)
                {
                    Console.WriteLine("C# ERROR(RefractionTest): alt={0}, refr={1}, diff={2}", alt, refr, diff);
                    return 1;
                }
            }

            Console.WriteLine("C# RefractionTest: PASS");
            return 0;
        }

        static int ConstellationTest()
        {
            const string inFileName = "../../constellation/test_input.txt";
            int failcount = 0;
            int lnum = 0;
            using (StreamReader infile = File.OpenText(inFileName))
            {
                string line;
                Regex reLine = new Regex(@"^\s*(\d+)\s+(\S+)\s+(\S+)\s+([A-Z][a-zA-Z]{2})\s*$");
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    Match m = reLine.Match(line);
                    if (!m.Success)
                    {
                        Console.WriteLine("C# ERROR(ConstellationTest): invalid line {0} in file {1}", lnum, inFileName);
                        return 1;
                    }
                    int id = int.Parse(m.Groups[1].Value);
                    double ra = double.Parse(m.Groups[2].Value);
                    double dec = double.Parse(m.Groups[3].Value);
                    string symbol = m.Groups[4].Value;
                    ConstellationInfo constel = Astronomy.Constellation(ra, dec);
                    if (constel.Symbol != symbol)
                    {
                        Console.WriteLine("Star {0,6}: expected {1}, found {2} at B1875 RA={3,10}, DEC={4,10}", id, symbol, constel.Symbol, constel.Ra1875, constel.Dec1875);
                        ++failcount;
                    }
                }
            }
            if (failcount > 0)
            {
                Console.WriteLine("C# ConstellationTest: {0} failures", failcount);
                return 1;
            }

            Console.WriteLine("C# ConstellationTest: PASS (verified {0})", lnum);
            return 0;
        }

        static int LunarEclipseIssue78()
        {
            LunarEclipseInfo eclipse = Astronomy.SearchLunarEclipse(new AstroTime(2020, 12, 19, 0, 0, 0));
            var expected_peak = new AstroTime(2021, 5, 26, 11, 18, 42);  // https://www.timeanddate.com/eclipse/lunar/2021-may-26
            double dt_seconds = (24.0 * 3600.0) * abs(expected_peak.tt - eclipse.peak.tt);
            if (dt_seconds > 40.0)
            {
                Console.WriteLine("C# LunarEclipseIssue78: Excessive prediction error = {0} seconds.", dt_seconds);
                return 1;
            }
            if (eclipse.kind != EclipseKind.Total)
            {
                Console.WriteLine("C# LunarEclipseIssue78: Expected total eclipse; found {0}", eclipse.kind);
                return 1;
            }
            Console.WriteLine("C# LunarEclipseIssue78: PASS");
            return 0;
        }

        static int LunarEclipseTest()
        {
            const string filename = "../../eclipse/lunar_eclipse.txt";
            const string statsFilename = "../../eclipse/cs_le_stats.csv";

            Astronomy.CalcMoonCount = 0;
            using (StreamReader infile = File.OpenText(filename))
            {
                using (StreamWriter outfile = File.CreateText(statsFilename))
                {
                    outfile.WriteLine("\"utc\",\"center\",\"partial\",\"total\"");
                    LunarEclipseInfo eclipse = Astronomy.SearchLunarEclipse(new AstroTime(1701, 1, 1, 0, 0, 0));
                    string line;
                    int lnum = 0;
                    int skip_count = 0;
                    int diff_count = 0;
                    double sum_diff_minutes = 0.0;
                    double max_diff_minutes = 0.0;
                    const double diff_limit = 2.0;
                    while (null != (line = infile.ReadLine()))
                    {
                        ++lnum;
                        if (line.Length < 17)
                        {
                            Console.WriteLine("C# LunarEclipseTest({0} line {1}): line is too short.", filename, lnum);
                            return 1;
                        }
                        string time_text = line.Substring(0, 17);
                        AstroTime peak_time = ParseDate(time_text);
                        string[] token = Tokenize(line.Substring(17));
                        double partial_minutes, total_minutes;
                        if (token.Length != 2 || !double.TryParse(token[0], out partial_minutes) || !double.TryParse(token[1], out total_minutes))
                        {
                            Console.WriteLine("C# LunarEclipseTest({0} line {1}): invalid data format.", filename, lnum);
                            return 1;
                        }

                        // Verify that the calculated eclipse semi-durations are consistent with the kind.
                        bool valid = false;
                        switch (eclipse.kind)
                        {
                        case EclipseKind.Penumbral:
                            valid = (eclipse.sd_penum > 0.0) && (eclipse.sd_partial == 0.0) && (eclipse.sd_total == 0.0);
                            break;

                        case EclipseKind.Partial:
                            valid = (eclipse.sd_penum > 0.0) && (eclipse.sd_partial > 0.0) && (eclipse.sd_total == 0.0);
                            break;

                        case EclipseKind.Total:
                            valid = (eclipse.sd_penum > 0.0) && (eclipse.sd_partial > 0.0) && (eclipse.sd_total > 0.0);
                            break;

                        default:
                            Console.WriteLine("C# LunarEclipseTest({0} line {1}): invalid eclipse kind {2}.", filename, lnum, eclipse.kind);
                            return 1;
                        }

                        if (!valid)
                        {
                            Console.WriteLine("C# LunarEclipseTest({0} line {1}): inalid semiduration(s) for kind {2}: penum={3}, partial={4}, total={5}",
                                filename, lnum, eclipse.kind, eclipse.sd_penum, eclipse.sd_partial, eclipse.sd_total);
                            return 1;
                        }

                        // Check eclipse peak time.
                        double diff_days = eclipse.peak.ut - peak_time.ut;

                        // Tolerate missing penumbral eclipses - skip to next input line without calculating next eclipse.
                        if (partial_minutes == 0.0 && diff_days > 20.0)
                        {
                            ++skip_count;
                            continue;
                        }

                        outfile.WriteLine("\"{0}\",{1},{2},{3}",
                            time_text,
                            diff_days * (24.0 * 60.0),
                            eclipse.sd_partial - partial_minutes,
                            eclipse.sd_total - total_minutes
                        );

                        double diff_minutes = (24.0 * 60.0) * abs(diff_days);
                        sum_diff_minutes += diff_minutes;
                        ++diff_count;

                        if (diff_minutes > diff_limit)
                        {
                            Console.WriteLine("C# LunarEclipseTest expected peak: {0}", peak_time);
                            Console.WriteLine("C# LunarEclipseTest found    peak: {1}", eclipse.peak);
                            Console.WriteLine("C# LunarEclipseTest({0} line {1}): EXCESSIVE peak time error = {2} minutes ({3} days).", filename, lnum, diff_minutes, diff_days);
                            return 1;
                        }

                        if (diff_minutes > max_diff_minutes)
                            max_diff_minutes = diff_minutes;

                        /* check partial eclipse duration */

                        diff_minutes = abs(partial_minutes - eclipse.sd_partial);
                        sum_diff_minutes += diff_minutes;
                        ++diff_count;

                        if (diff_minutes > diff_limit)
                        {
                            Console.WriteLine("C# LunarEclipseTest({0} line {1}): EXCESSIVE partial eclipse semiduration error: {2} minutes", filename, lnum, diff_minutes);
                            return 1;
                        }

                        if (diff_minutes > max_diff_minutes)
                            max_diff_minutes = diff_minutes;

                        /* check total eclipse duration */

                        diff_minutes = abs(total_minutes - eclipse.sd_total);
                        sum_diff_minutes += diff_minutes;
                        ++diff_count;

                        if (diff_minutes > diff_limit)
                        {
                            Console.WriteLine("C# LunarEclipseTest({0} line {1}): EXCESSIVE total eclipse semiduration error: {2} minutes", filename, lnum, diff_minutes);
                            return 1;
                        }

                        if (diff_minutes > max_diff_minutes)
                            max_diff_minutes = diff_minutes;

                        /* calculate for next iteration */

                        eclipse = Astronomy.NextLunarEclipse(eclipse.peak);
                    }
                    Console.WriteLine("C# LunarEclipseTest: PASS (verified {0}, skipped {1}, max_diff_minutes = {2}, avg_diff_minutes = {3}, moon calcs = {4})", lnum, skip_count, max_diff_minutes, (sum_diff_minutes / diff_count), Astronomy.CalcMoonCount);
                }
            }
            return 0;
        }

        static int GlobalSolarEclipseTest()
        {
            Astronomy.CalcMoonCount = 0;

            int lnum = 0;
            int skip_count = 0;
            double max_angle = 0.0;
            double max_minutes = 0.0;
            GlobalSolarEclipseInfo eclipse = Astronomy.SearchGlobalSolarEclipse(new AstroTime(1701, 1, 1, 0, 0, 0));

            const string inFileName = "../../eclipse/solar_eclipse.txt";
            using (StreamReader infile = File.OpenText(inFileName))
            {
                string line;
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    string[] token = Tokenize(line);
                    /* 1889-12-22T12:54:15Z   -6 T   -12.7   -12.8 */
                    if (token.Length != 5)
                    {
                        Console.WriteLine("C# GlobalSolarEclipseTest({0} line {1}): wrong token count = {2}", inFileName, lnum, token.Length);
                        return 1;
                    }
                    AstroTime peak = ParseDate(token[0]);
                    string typeChar = token[2];
                    double lat = double.Parse(token[3]);
                    double lon = double.Parse(token[4]);
                    EclipseKind expected_kind;
                    switch (typeChar)
                    {
                        case "P": expected_kind = EclipseKind.Partial;  break;
                        case "A": expected_kind = EclipseKind.Annular;  break;
                        case "T": expected_kind = EclipseKind.Total;    break;
                        case "H": expected_kind = EclipseKind.Total;    break;
                        default:
                            Console.WriteLine("C# GlobalSolarEclipseTest({0} line {1}): invalid eclipse kind '{2}' in test data.", inFileName, lnum, typeChar);
                            return 1;
                    }

                    double diff_days = eclipse.peak.ut - peak.ut;

                    /* Sometimes we find marginal eclipses that aren't listed in the test data. */
                    /* Ignore them if the distance between the Sun/Moon shadow axis and the Earth's center is large. */
                    while (diff_days < -25.0 && eclipse.distance > 9000.0)
                    {
                        ++skip_count;
                        eclipse = Astronomy.NextGlobalSolarEclipse(eclipse.peak);
                        diff_days = eclipse.peak.ut - peak.ut;
                    }

                    /* Validate the eclipse prediction. */
                    double diff_minutes = (24 * 60) * abs(diff_days);
                    if (diff_minutes > 6.93)
                    {
                        Console.WriteLine("C# GlobalSolarEclipseTest({0} line {1}): EXCESSIVE TIME ERROR = {2} minutes", inFileName, lnum, diff_minutes);
                        return 1;
                    }

                    if (diff_minutes > max_minutes)
                        max_minutes = diff_minutes;

                    /* Validate the eclipse kind, but only when it is not a "glancing" eclipse. */
                    if ((eclipse.distance < 6360) && (eclipse.kind != expected_kind))
                    {
                        Console.WriteLine("C# GlobalSolarEclipseTest({0} line {1}): WRONG ECLIPSE KIND: expected {2}, found {3}", inFileName, lnum, expected_kind, eclipse.kind);
                        return 1;
                    }

                    if (eclipse.kind == EclipseKind.Total || eclipse.kind == EclipseKind.Annular)
                    {
                        /*
                            When the distance between the Moon's shadow ray and the Earth's center is beyond 6100 km,
                            it creates a glancing blow whose geographic coordinates are excessively sensitive to
                            slight changes in the ray. Therefore, it is unreasonable to count large errors there.
                        */
                        if (eclipse.distance < 6100.0)
                        {
                            double diff_angle = AngleDiff(lat, lon, eclipse.latitude, eclipse.longitude);
                            if (diff_angle > 0.247)
                            {
                                Console.WriteLine("C# GlobalSolarEclipseTest({0} line {1}): EXCESSIVE GEOGRAPHIC LOCATION ERROR = {2} degrees", inFileName, lnum, diff_angle);
                                return 1;
                            }
                            if (diff_angle > max_angle)
                                max_angle = diff_angle;
                        }
                    }

                    eclipse = Astronomy.NextGlobalSolarEclipse(eclipse.peak);
                }
            }

            const int expected_count = 1180;
            if (lnum != expected_count)
            {
                Console.WriteLine("C# GlobalSolarEclipseTest: WRONG LINE COUNT = {0}, expected {1}", lnum, expected_count);
                return 1;
            }

            if (skip_count > 2)
            {
                Console.WriteLine("C# GlobalSolarEclipseTest: EXCESSSIVE SKIP COUNT = {0}", skip_count);
                return 1;
            }

            Console.WriteLine("C# GlobalSolarEclipseTest: PASS ({0} verified, {1} skipped, {2} CalcMoons, max minutes = {3}, max angle = {4})", lnum, skip_count, Astronomy.CalcMoonCount, max_minutes, max_angle);
            return 0;
        }

        static void VectorFromAngles(double[] v, double lat, double lon)
        {
            double coslat = cos(DEG2RAD * lat);
            v[0] = cos(DEG2RAD * lon) * coslat;
            v[1] = sin(DEG2RAD * lon) * coslat;
            v[2] = sin(DEG2RAD * lat);
        }

        static double AngleDiff(double alat, double alon, double blat, double blon)
        {
            double[] a = new double[3];
            double[] b = new double[3];
            double dot;

            /* Convert angles to vectors on a unit sphere. */
            VectorFromAngles(a, alat, alon);
            VectorFromAngles(b, blat, blon);

            dot = a[0]*b[0] + a[1]*b[1] + a[2]*b[2];
            if (dot <= -1.0)
                return 180.0;

            if (dot >= +1.0)
                return 0.0;

            return v(RAD2DEG * Math.Acos(dot));
        }

        static int LocalSolarEclipseTest1()
        {
            /*
                Re-use the test data for global solar eclipses, only feed the given coordinates
                into the local solar eclipse predictor as the observer's location.
                In each case, start the search 20 days before the expected eclipse.
                Then verify that the peak time and eclipse type is correct in each case.
            */
            Astronomy.CalcMoonCount = 0;

            int lnum = 0;
            int skip_count = 0;
            double max_minutes = 0.0;

            const string inFileName = "../../eclipse/solar_eclipse.txt";
            using (StreamReader infile = File.OpenText(inFileName))
            {
                string line;
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    string[] token = Tokenize(line);
                    /* 1889-12-22T12:54:15Z   -6 T   -12.7   -12.8 */
                    if (token.Length != 5)
                    {
                        Console.WriteLine("C# LocalSolarEclipseTest1({0} line {1}): wrong token count = {2}", inFileName, lnum, token.Length);
                        return 1;
                    }
                    AstroTime peak = ParseDate(token[0]);
                    string typeChar = token[2];
                    double lat = double.Parse(token[3]);
                    double lon = double.Parse(token[4]);
                    var observer = new Observer(lat, lon, 0.0);

                    /* Start the search 20 days before we know the eclipse should peak. */
                    AstroTime search_start = peak.AddDays(-20.0);
                    LocalSolarEclipseInfo eclipse = Astronomy.SearchLocalSolarEclipse(search_start, observer);

                    /* Validate the predicted eclipse peak time. */
                    double diff_days = eclipse.peak.time.ut - peak.ut;
                    if (diff_days > 20.0)
                    {
                        ++skip_count;
                        continue;
                    }

                    double diff_minutes = (24 * 60) * abs(diff_days);
                    if (diff_minutes > 7.14)
                    {
                        Console.WriteLine("C LocalSolarEclipseTest1({0} line {1}): EXCESSIVE TIME ERROR = {2} minutes", inFileName, lnum, diff_minutes);
                        return 1;
                    }

                    if (diff_minutes > max_minutes)
                        max_minutes = diff_minutes;
                }
            }

            if (skip_count > 6)
            {
                Console.WriteLine("C# LocalSolarEclipseTest1: EXCESSSIVE SKIP COUNT = {0}", skip_count);
                return 1;
            }

            Console.WriteLine("C# LocalSolarEclipseTest1: PASS ({0} verified, {1} skipped, {2} CalcMoons, max minutes = {3})", lnum, skip_count, Astronomy.CalcMoonCount, max_minutes);
            return 0;
        }

        static string StripLine(string line)
        {
            int index = line.IndexOf('#');
            if (index >= 0)
                line = line.Substring(0, index);
            return line.Trim();
        }

        static int LocalSolarEclipseTest2()
        {
            /*
                Test ability to calculate local solar eclipse conditions away from
                the peak position on the Earth.
            */

            Astronomy.CalcMoonCount = 0;

            const string inFileName = "../../eclipse/local_solar_eclipse.txt";
            double max_minutes=0.0, max_degrees=0.0;
            int verify_count = 0;

            using (StreamReader infile = File.OpenText(inFileName))
            {
                int lnum = 0;
                string line;
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    line = StripLine(line);
                    if (line.Length == 0) continue;
                    string[] token = Tokenize(line);
                    if (token.Length != 13)
                    {
                        Console.WriteLine("C# LocalSolarEclipseTest2({0} line {1}): Incorrect token count {2}", inFileName, lnum, token.Length);
                        return 1;
                    }
                    double latitude = double.Parse(token[0]);
                    double longitude = double.Parse(token[1]);
                    string typeCode = token[2];
                    AstroTime p1 = ParseDate(token[3]);
                    double p1alt = double.Parse(token[4]);
                    AstroTime t1 = OptionalParseDate(token[5]);
                    double t1alt = double.Parse(token[6]);
                    AstroTime peak = ParseDate(token[7]);
                    double peakalt = double.Parse(token[8]);
                    AstroTime t2 = OptionalParseDate(token[9]);
                    double t2alt = double.Parse(token[10]);
                    AstroTime p2 = ParseDate(token[11]);
                    double p2alt = double.Parse(token[12]);

                    EclipseKind expected_kind;
                    switch (typeCode)
                    {
                    case "P": expected_kind = EclipseKind.Partial; break;
                    case "A": expected_kind = EclipseKind.Annular; break;
                    case "T": expected_kind = EclipseKind.Total;   break;
                    default:
                        Console.WriteLine("C# LocalSolarEclipseTest2({0} line {1}): invalid eclipse type '{2}'", inFileName, lnum, typeCode);
                        return 1;
                    }

                    var observer = new Observer(latitude, longitude, 0.0);
                    AstroTime search_time = p1.AddDays(-20.0);
                    LocalSolarEclipseInfo eclipse = Astronomy.SearchLocalSolarEclipse(search_time, observer);
                    if (eclipse.kind != expected_kind)
                    {
                        Console.WriteLine("C# LocalSolarEclipseTest2({0} line {1}): expected {2}, found {3}", inFileName, lnum, expected_kind, eclipse.kind);
                        return 1;
                    }

                    if (CheckEvent(inFileName, lnum, "peak", peak, peakalt, eclipse.peak, ref max_minutes, ref max_degrees)) return 1;
                    if (CheckEvent(inFileName, lnum, "partial_begin", p1, p1alt, eclipse.partial_begin, ref max_minutes, ref max_degrees)) return 1;
                    if (CheckEvent(inFileName, lnum, "partial_end", p2, p2alt, eclipse.partial_end, ref max_minutes, ref max_degrees)) return 1;
                    if (typeCode != "P")
                    {
                        if (CheckEvent(inFileName, lnum, "total_begin", t1, t1alt, eclipse.total_begin, ref max_minutes, ref max_degrees)) return 1;
                        if (CheckEvent(inFileName, lnum, "total_end", t2, t2alt, eclipse.total_end, ref max_minutes, ref max_degrees)) return 1;
                    }

                    ++verify_count;
                }
            }

            Console.WriteLine("C# LocalSolarEclipseTest2: PASS ({0} verified, {1} CalcMoons, max minutes = {2}, max alt degrees = {3})",
                verify_count, Astronomy.CalcMoonCount, max_minutes, max_degrees);
            return 0;
        }

        static bool CheckEvent(
            string inFileName,
            int lnum,
            string name,
            AstroTime expected_time,
            double expected_altitude,
            EclipseEvent evt,
            ref double max_minutes,
            ref double max_degrees)
        {
            double diff_minutes = (24 * 60) * abs(expected_time.ut - evt.time.ut);
            if (diff_minutes > max_minutes)
                max_minutes = diff_minutes;

            if (diff_minutes > 1.0)
            {
                Console.WriteLine("CheckEvent({0} line {1}): EXCESSIVE TIME ERROR: {2} minutes", inFileName, lnum, diff_minutes);
                return true;
            }

            double diff_alt = abs(expected_altitude - evt.altitude);
            if (diff_alt > max_degrees) max_degrees = diff_alt;
            if (diff_alt > 0.5)
            {
                Console.WriteLine("CheckEvent({0} line {1}): EXCESSIVE ALTITUDE ERROR: {2} degrees", inFileName, lnum, diff_alt);
                return true;
            }

            return false;
        }

        static int LocalSolarEclipseTest()
        {
            if (0 != LocalSolarEclipseTest1())
                return 1;

            if (0 != LocalSolarEclipseTest2())
                return 1;

            return 0;
        }

        static int TransitFile(Body body, string filename, double limit_minutes, double limit_sep)
        {
            using (StreamReader infile = File.OpenText(filename))
            {
                string line;
                int lnum = 0;
                double max_minutes = 0.0;
                double max_sep = 0.0;
                TransitInfo transit = Astronomy.SearchTransit(body, new AstroTime(1600, 1, 1, 0, 0, 0));
                while (null != (line = infile.ReadLine()))
                {
                    ++lnum;
                    /* 22:17 1881-11-08T00:57Z 03:38  3.8633 */
                    string[] token = Tokenize(line);
                    if (token.Length != 4)
                    {
                        Console.WriteLine("C# TransitFile({0} line {1}): expected 4 tokens, found {2}", filename, lnum, token.Length);
                        return 1;
                    }

                    string textp = token[1];
                    string text1 = textp.Substring(0, 11) + token[0] + "Z";
                    string text2 = textp.Substring(0, 11) + token[2] + "Z";
                    AstroTime timep = ParseDate(textp);
                    AstroTime time1 = ParseDate(text1);
                    AstroTime time2 = ParseDate(text2);
                    double separation = double.Parse(token[3]);

                    // If the start time is after the peak time, it really starts on the previous day.
                    if (time1.ut > timep.ut)
                        time1 = time1.AddDays(-1.0);

                    // If the finish time is before the peak time, it really starts on the following day.
                    if (time2.ut < timep.ut)
                        time2 = time2.AddDays(+1.0);

                    double diff_start  = (24.0 * 60.0) * abs(time1.ut - transit.start.ut );
                    double diff_peak   = (24.0 * 60.0) * abs(timep.ut - transit.peak.ut  );
                    double diff_finish = (24.0 * 60.0) * abs(time2.ut - transit.finish.ut);
                    double diff_sep = abs(separation - transit.separation);

                    max_minutes = max(max_minutes, diff_start);
                    max_minutes = max(max_minutes, diff_peak);
                    max_minutes = max(max_minutes, diff_finish);
                    if (max_minutes > limit_minutes)
                    {
                        Console.WriteLine("C# TransitFile({0} line {1}): EXCESSIVE TIME ERROR = {2} minutes.", filename, lnum, max_minutes);
                        return 1;
                    }

                    max_sep = max(max_sep, diff_sep);
                    if (max_sep > limit_sep)
                    {
                        Console.WriteLine("C# TransitFile({0} line {1}): EXCESSIVE SEPARATION ERROR = {2} arcminutes.", filename, lnum, max_sep);
                        return 1;
                    }

                    transit = Astronomy.NextTransit(body, transit.finish);
                }
                Console.WriteLine("C# TransitFile({0}): PASS - verified {1}, max minutes = {2}, max sep arcmin = {3}\n", filename, lnum, max_minutes, max_sep);
                return 0;
            }
        }

        static int TransitTest()
        {
            if (0 != TransitFile(Body.Mercury, "../../eclipse/mercury.txt", 10.710, 0.2121))
                return 1;

            if (0 != TransitFile(Body.Venus, "../../eclipse/venus.txt", 9.109, 0.6772))
                return 1;

            return 0;
        }

        static int PlutoCheckDate(double ut, double arcmin_tolerance, double x, double y, double z)
        {
            var time = new AstroTime(ut);
            string timeText;

            try
            {
                timeText = time.ToString();
            }
            catch (ArgumentOutOfRangeException)
            {
                // One of the dates we pass in is before the year 0000.
                // The DateTime class can't handle this.
                timeText = "???";
            }

            Debug("C# PlutoCheck: {0} = {1} UT = {2} TT", timeText, time.ut, time.tt);

            AstroVector vector = Astronomy.HelioVector(Body.Pluto, time);
            double dx = v(vector.x - x);
            double dy = v(vector.y - y);
            double dz = v(vector.z - z);
            double diff = sqrt(dx*dx + dy*dy + dz*dz);
            double dist = (sqrt(x*x + y*y + z*z) - 1.0);       /* worst-case distance between Pluto and Earth */
            double arcmin = (diff / dist) * (180.0 * 60.0 / Math.PI);
            Debug("C# PlutoCheck: calc pos = [{0}, {1}, {2}]", vector.x, vector.y, vector.z);
            Debug("C# PlutoCheck: ref  pos = [{0}, {1}, {2}]", x, y, z);
            Debug("C# PlutoCheck: del  pos = [{0}, {1}, {2}]", vector.x - x, vector.y - y, vector.z - z);
            Debug("C# PlutoCheck: diff = {0} AU, {1} arcmin\n", diff, arcmin);
            Debug("");

            if (v(arcmin) > arcmin_tolerance)
            {
                Console.WriteLine("C# PlutoCheck: EXCESSIVE ERROR\n");
                return 1;
            }
            return 0;
        }

        static int PlutoCheck()
        {
            if (0 != PlutoCheckDate(  +18250.0,  0.271, +37.4377303523676090, -10.2466292454075898, -14.4773101310875809)) return 1;
            if (0 != PlutoCheckDate(  +18250.0,  0.271, +37.4377303523676090, -10.2466292454075898, -14.4773101310875809)) return 1;
            if (0 != PlutoCheckDate( -856493.0,  6.636, +23.4292113199166252, +42.1452685817740829,  +6.0580908436642940)) return 1;
            if (0 != PlutoCheckDate( +435633.0,  0.058, -27.3178902095231813, +18.5887022581070305, +14.0493896259306936)) return 1;
            if (0 != PlutoCheckDate(       0.0, 4.0e-9, -9.8753673425269000,  -27.9789270580402771,  -5.7537127596369588)) return 1;
            if (0 != PlutoCheckDate( +800916.0,  6.705, -29.5266052645301365, +12.0554287322176474, +12.6878484911631091)) return 1;
            Console.WriteLine("C# PlutoCheck: PASS");
            return 0;
        }
    }
}
