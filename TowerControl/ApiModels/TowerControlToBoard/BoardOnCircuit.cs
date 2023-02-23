using System.Xml.Serialization;
using TowerControl.ApiModels.Base;

namespace TowerControl.ApiModels.TowerControlToBoard
{
    /// <summary>
    /// Запрос отправки борта на круг
    /// </summary>
    [XmlRoot(ElementName = "circuit")]
    public class BoardOnCircuit : IBaseRequest
    {
        // тут в задаче явно ошибка так-как у XML'ки нет корневого элемента
        // в таком случае это не валидный XML и с ним неудобно работать
        [XmlElement(ElementName = "radius")] public long Radius { get; set; }
        [XmlElement(ElementName = "speed")] public long Speed { get; set; }
    }
}
