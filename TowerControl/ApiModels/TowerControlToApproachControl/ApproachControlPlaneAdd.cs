using System.Xml.Serialization;
using TowerControl.ApiModels.Base;

namespace TowerControl.ApiModels.TowerControlToApproachControl
{
    /// <summary>
    /// Запрос передачи вылетающего из круга самолета диспетчеру посадки
    /// </summary>
    [XmlRoot(ElementName = "plane")]
    public class ApproachControlPlaneAdd : FlightWithCoords, IBaseRequest
    {
    }
}
