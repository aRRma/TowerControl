using System.Xml.Serialization;

namespace TowerControl.ApiModels.Base
{
    /// <summary>
    /// Базовый абстрактный класс для самолета с координатами
    /// </summary>
    [XmlRoot(ElementName = "plane")]
    public abstract class FlightWithCoords
    {
        [XmlElement(ElementName = "flightNumber")] public string FlightNumber { get; set; }
        [XmlElement(ElementName = "coords")] public Coords Coords { get; set; }
    }
}
