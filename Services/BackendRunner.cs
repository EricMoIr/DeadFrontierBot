using Newtonsoft.Json;
using Persistence.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Services
{
    public class BackendRunner
    {
        private static bool isRunning = false;
        private static double INTERVAL;
        public static async Task RunBackend(double interval)
        {
            INTERVAL = interval;
            if (!isRunning)
            {
                Timer checkForTime = new Timer(INTERVAL);
                checkForTime.Elapsed += new ElapsedEventHandler(NotifyServersEvent);
                checkForTime.Start();
                await LookForOA();
                isRunning = true;
            }
        }

        private static async void NotifyServersEvent(object sender, ElapsedEventArgs e)
        {
            await LookForOA();
        }

        private static DateTime time;
        private static readonly HttpClient client = new HttpClient();
        private static readonly string[] outpostNames = new string[] {
            "nastyasHoldout",
            "doggsStockade",
            "secronomBunker",
            "fortPastor",
            "precinct13"
        };
        private static async Task LookForOA()
        {
            time = DateTime.Now;
            var responseString = await client.GetStringAsync("https://deadfrontier.com/OACheck.php");
            string[] outpostsWithOA = JsonConvert.DeserializeObject<string[]>(responseString);
            foreach (string outpostWithOA in outpostsWithOA)
            {
                Outpost outpost = OutpostService.Get(outpostWithOA);
                if (outpost == null)
                    throw new ArgumentNullException("The outpost with OA doesn't exist in the DB");
                if (!outpost.HasOA)
                {
                    Report report = new Report()
                    {
                        Name = outpostWithOA,
                        Started = true,
                        Time = time
                    };
                    ReportService.Create(report);
                    outpost.HasOA = true;
                    OutpostService.Update(outpost.Id, outpost);
                }
            }
            var outpostsWithoutOA = outpostNames.Except(outpostsWithOA);
            foreach(string outpostWithoutOA in outpostsWithoutOA)
            {
                Outpost outpost = OutpostService.Get(outpostWithoutOA);
                if (outpost == null)
                    throw new ArgumentNullException("The outpost with OA doesn't exist in the DB");
                if (outpost.HasOA)
                {
                    Report report = new Report()
                    {
                        Name = outpostWithoutOA,
                        Started = false,
                        Time = time
                    };
                    ReportService.Create(report);
                    outpost.HasOA = false;
                    OutpostService.Update(outpost.Id, outpost);
                }
            }
        }
    }
}
