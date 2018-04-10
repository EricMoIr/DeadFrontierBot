namespace Persistence.Migrations
{
    using Domain;
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Migrations;
    using System.Linq;

    internal sealed class Configuration : DbMigrationsConfiguration<Persistence.DFContext>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = false;
            ContextKey = "Persistence.DFContext";
        }

        protected override void Seed(Persistence.DFContext context)
        {
            string[] outpostNames = new string[] {
            "nastyasHoldout",
            "doggsStockade",
            "secronomBunker",
            "fortPastor",
            "precinct13"
        };
            for (int i = 0; i < outpostNames.Length; i++)
            {
                Outpost outpost = new Outpost()
                {
                    Name = outpostNames[i],
                    HasOA = false
                };
                string name = outpostNames[i];
                if (context.Outposts.Where(x => x.Name == name).Count() == 0)
                    context.Outposts.Add(outpost);
            }
            context.SaveChanges();
        }
    }
}
