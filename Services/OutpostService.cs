using System;
using Persistence.Domain;
using System.Collections.Generic;
using Persistence;
using System.Linq;

namespace Services
{
    public class OutpostService
    {
        private static DFUnitOfWork uow = new DFUnitOfWork();
        private static DFRepository<Outpost> outposts = uow.Outposts;
        internal static Outpost Get(string name)
        {
            return outposts.Get(x => x.Name == name).FirstOrDefault();
        }

        internal static void Update(int id, Outpost outpost)
        {
            outpost.Id = id;
            outposts.Update(outpost);
            uow.Save();
        }

        public static List<string> GetOutpostsWithOA()
        {
            return outposts.Get(x => x.HasOA == true)
                .Select(x => x.Name)
                .ToList();
        }

        public static string FindName(string arg)
        {
            arg = arg.Replace("'", "").Replace(" ", "").ToLower();
            var outpostNames = outposts.Get().Select(x => x.Name).Distinct();

            foreach (string name in outpostNames)
            {
                if (name.ToLower().IndexOf(arg) > -1)
                {
                    return name;
                }
            }
            return null;
        }

        public static List<string> GetAll()
        {
            return outposts.Get().Select(x => x.Name).ToList();
        }
    }
}