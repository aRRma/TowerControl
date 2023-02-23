using System.ComponentModel.DataAnnotations;

namespace TowerControl.Data.Base
{
    /// <summary>
    /// Базовый интерфейс сущности в БД
    /// </summary>
    public interface IBaseEntity
    {
        [Required]
        public long Id { get; set; }
        [Required]
        public DateTime Date { get; set; }
    }
}
