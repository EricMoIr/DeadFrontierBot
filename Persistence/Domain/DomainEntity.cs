using System.ComponentModel.DataAnnotations;

namespace Persistence.Domain
{
    public class DomainEntity
    {
        [Key]
        public virtual int Id { get; }
    }
}