using System.Xml.Serialization;
using TowerControl.ApiModels.Base;

namespace TowerControl.ApiModels.BoardToTowerControl
{
    /// <summary>
    /// Запрос передачи самолета диспетчеру старта и посадки
    /// </summary>
    [XmlRoot(ElementName = "plane")]
    public class TowerControlRegisterNewPlane : FlightWithCoords, IBaseRequest
    {
    }
}
