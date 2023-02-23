using System.Xml.Serialization;
using TowerControl.ApiModels.Base;

namespace TowerControl.ApiModels.TowerControlToBoard
{
    /// <summary>
    /// Ответ запроса у борта его координат
    /// </summary>
    [XmlRoot(ElementName = "coords")]
    public class BoardGetCoords : Coords, IBaseResponse
    {
    }
}
