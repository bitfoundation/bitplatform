namespace Bit.Sales.WebSite.App.Models
{
    public class Case
    {
        public string Key { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string AboutProject { get; set; }
        public string TeamMemberCount { get; set; }
        public string MvpDeliveryTime { get; set; }
        public string DeliveryTime { get; set; }
        public string Challenges { get; set; }
        public string Solutions { get; set; }
        public string Result { get; set; }
        public List<CaseFeature> Features { get; set; }
        public List<string> Technologies { get; set; }
        public List<string> Services { get; set; }
    }
}
