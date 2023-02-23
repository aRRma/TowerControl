using System.Xml.Serialization;
using TowerControl.ApiModels.Base;

namespace TowerControl.ApiModels.BoardToTowerControl
{
    /// <summary>
    /// Запрос разрешения на посадку
    /// </summary>
    [XmlRoot(ElementName = "plane")]
    public class TowerControlLanding : FlightWithCoords, IBaseRequest
    {
        [XmlElement(ElementName = "speed")] public long Speed { get; set; }
    }
}
