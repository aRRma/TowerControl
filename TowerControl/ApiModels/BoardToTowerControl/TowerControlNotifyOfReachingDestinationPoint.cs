using System.Xml.Serialization;
using TowerControl.ApiModels.Base;

namespace TowerControl.ApiModels.BoardToTowerControl
{
    /// <summary>
    /// Запрос разрешения на взлет
    /// </summary>
    [XmlRoot(ElementName = "plane")]
    public class TowerControlNotifyOfReachingDestinationPoint : IBaseRequest
    {
        [XmlElement(ElementName = "flightNumber")] public string FlightNumber { get; set; }
    }
}
