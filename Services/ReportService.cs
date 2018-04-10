using Persistence;
using Persistence.Domain;
using System.Linq;
using System;
using System.Collections.Generic;

namespace Services
{
    public class ReportService
    {
        private static DFUnitOfWork uow = new DFUnitOfWork();
        private static DFRepository<Report> reports = uow.Reports;
        internal static bool Create(Report report)
        {
            reports.Insert(report);
            uow.Save();
            return true;
        }

        internal static bool Delete(string outpostWithoutOA)
        {
            var outpost = reports
                .Get(x =>
                x.Name == outpostWithoutOA)
                .FirstOrDefault();
            if (outpost == null) return false;
            reports.Delete(outpost);
            uow.Save();
            return true;
        }
    }
}