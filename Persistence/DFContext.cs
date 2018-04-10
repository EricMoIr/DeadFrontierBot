using Persistence.Domain;
using System.Data.Entity;

namespace Persistence
{
    public class DFContext : DbContext
    {
        public DFContext() : base("name=DFBotContext") { }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Outpost> Outposts { get; set; }
        public DbSet<Context> Contexts { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {

        }

        public virtual EntityState GetState(object entity)
        {
            return Entry(entity).State;
        }

        public virtual void SetModified(object entity)
        {
            Entry(entity).State = EntityState.Modified;
        }
    }
}