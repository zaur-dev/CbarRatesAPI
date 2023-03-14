using System.Xml.Serialization;

namespace FxCore.Models
{
    [XmlRoot(ElementName = "ValCurs")]
    public class CbarDailyFx
    {
        [XmlAttribute(AttributeName = "Date")]
        public string Date { get; set; }

        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "Description")]
        public string Description { get; set; }

        [XmlElement(ElementName = "ValType")]
        public List<AssetType> AssetTypes { get; set; }
    }

    [XmlRoot(ElementName = "ValType")]
    public class AssetType
    {
        [XmlAttribute(AttributeName = "Type")]
        public string Type { get; set; }

        [XmlElement(ElementName = "Valute")]
        public List<Currency> Currencies { get; set; }
    }

    [XmlRoot(ElementName = "Valute")]
    public class Currency
    {
        [XmlAttribute(AttributeName = "Code")]
        public string Code { get; set; }
        public string Nominal { get; set; }
        public string Name { get; set; }
        public decimal Value { get; set; }
    }

}
