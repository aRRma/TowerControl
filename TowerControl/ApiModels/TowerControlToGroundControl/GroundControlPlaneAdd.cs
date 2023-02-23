using System.Xml.Serialization;
using TowerControl.ApiModels.Base;

namespace TowerControl.ApiModels.TowerControlToGroundControl
{
    /// <summary>
    /// Запрос сообщения о посадке самолета
    /// </summary>
    [XmlRoot(ElementName = "plane")]
    public class GroundControlPlaneAdd : FlightWithCoords, IBaseRequest
    {
        [XmlElement(ElementName = "wayNumber")] public long WayNumber { get; set; }
    }
}
