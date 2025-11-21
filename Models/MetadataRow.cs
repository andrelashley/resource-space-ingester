using CsvHelper.Configuration.Attributes;

namespace ResourceSpace.Ingester.Models
{
    public record MetadataRow
    {
        [Name("Resource ID(s)")]
        public int ResourceId { get; set; }

        [Name("Title")]
        public string Title { get; set; } = string.Empty;

        [Name("Description")]
        public string Description { get; set; } = string.Empty;

        [Name("Date")]
        public string Date { get; set; } = string.Empty;

        [Name("Location")]
        public string Location { get; set; } = string.Empty;

        [Name("Keywords - Ministry")]
        public string KeywordsMinistry { get; set; } = string.Empty;

        [Name("Keywords - Resource Type")]
        public string KeywordsResourceType { get; set; } = string.Empty;

        [Name("Keywords - Other")]
        public string KeywordsOther { get; set; } = string.Empty;

        [Name("Credit")]
        public string Credit { get; set; } = string.Empty;

        [Name("Named person(s)")]
        public string NamedPersons { get; set; } = string.Empty;

        [Name("Country")]
        public string Country { get; set; } = string.Empty;

        [Name("Terms of Use")]
        public string TermsOfUse { get; set; } = string.Empty;

        [Name("Contact Information")]
        public string ContactInformation { get; set; } = string.Empty;

        [Name("Related Links")]
        public string RelatedLinks { get; set; } = string.Empty;

        [Name("Camera make / model")]
        public string CameraMakeModel { get; set; } = string.Empty;
        
        [Name("Source")]
        public string Source { get; set; } = string.Empty;
    }
}
