using System.Xml.Serialization;
using TowerControl.ApiModels.Base;

namespace TowerControl.ApiModels.TowerControlToBoard
{
    /// <summary>
    /// Запрос изменения скорости борта на круге
    /// </summary>
    [XmlRoot(ElementName = "speed")]
    public class BoardCircuitSpeed : IBaseRequest
    {
        [XmlText()] public long Speed { get; set; }
    }
}
