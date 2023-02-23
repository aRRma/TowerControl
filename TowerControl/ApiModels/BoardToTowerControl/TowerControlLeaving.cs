using System.Xml.Serialization;
using TowerControl.ApiModels.Base;

namespace TowerControl.ApiModels.BoardToTowerControl
{
    /// <summary>
    /// Запрос разрешения на взлет
    /// </summary>
    [XmlRoot(ElementName = "plane")]
    public class TowerControlLeaving : FlightWithCoords, IBaseRequest
    {
    }
}
