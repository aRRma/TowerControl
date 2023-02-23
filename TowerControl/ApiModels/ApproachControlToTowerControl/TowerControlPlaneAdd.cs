using System.Xml.Serialization;
using TowerControl.ApiModels.Base;

namespace TowerControl.ApiModels.ApproachControlToTowerControl
{
    /// <summary>
    /// Запрос передачи на посадку самолета диспетчеру круга
    /// </summary>
    [XmlRoot(ElementName = "plane")]
    public class TowerControlPlaneAdd : IBaseRequest
    {
        [XmlElement(ElementName = "flightNumber")] public string FlightNumber { get; set; }
        [XmlElement(ElementName = "coords")] public Coords Coords { get; set; }
    }
}
