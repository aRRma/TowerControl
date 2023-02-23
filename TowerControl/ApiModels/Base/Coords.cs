using System.Xml.Serialization;

namespace TowerControl.ApiModels.Base
{
    public class Coords
    {
        /// <summary>
        /// Координата в метрах
        /// </summary>
        [XmlElement(ElementName = "coordX")] public long CoordX { get; set; }
        /// <summary>
        /// Координата в метрах
        /// </summary>
        [XmlElement(ElementName = "coordY")] public long CoordY { get; set; }
        /// <summary>
        /// Координата в метрах
        /// </summary>
        [XmlElement(ElementName = "coordZ")] public long CoordZ { get; set; }
    }
}
