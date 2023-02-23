namespace TowerControl.Data.Base
{
    /// <summary>
    /// Статус самолета
    /// </summary>
    public enum PlaneStatusType
    {
        /// <summary>
        /// Не известно
        /// </summary>
        NONE = 0,
        /// <summary>
        /// Зарегистрирован вышкой
        /// </summary>
        REGISTERED,
        /// <summary>
        /// На посадке
        /// </summary>
        LANDING,
        /// <summary>
        /// Приземлился
        /// </summary>
        LANDED,
        /// <summary>
        /// Рулежка на исполнительный старт
        /// </summary>
        TAXIING_ON_IS,
        /// <summary>
        /// Рулежка закончена, посадка завершена
        /// </summary>
        TAXIING_DONE,
        /// <summary>
        /// Взлетает
        /// </summary>
        TAKE_OFF,
        /// <summary>
        /// Взлетел
        /// </summary>
        TOOK_OFF,
        /// <summary>
        /// На круге
        /// </summary>
        ON_CIRCLE,
    }
}
