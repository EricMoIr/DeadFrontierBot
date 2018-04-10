using Persistence.Domain;
using System;
using System.Collections.Generic;

namespace Persistence
{
    public class DFUnitOfWork
    {
        private DFContext context;
        public DFRepository<Report> Reports { get; }
        public DFRepository<Outpost> Outposts { get; set; }

        private bool disposed = false;

        public DFUnitOfWork()
        {
            context = new DFContext();
            Reports = new DFRepository<Report>(context);
            Outposts = new DFRepository<Outpost>(context);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    context.Dispose();
                }
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Save()
        {
            context.SaveChanges();
        }
    }
}
