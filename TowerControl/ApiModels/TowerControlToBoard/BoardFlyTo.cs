using System.Xml.Serialization;
using TowerControl.ApiModels.Base;

namespace TowerControl.ApiModels.TowerControlToBoard
{
    /// <summary>
    /// Запрос назначения борту точки следования
    /// </summary>
    [XmlRoot(ElementName = "coords")]
    public class BoardFlyTo : Coords, IBaseRequest
    {
    }
}
