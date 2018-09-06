namespace Epinova.ElasticSearch.Core.Pipelines
{
    internal static class Attachment
    {
        internal const string Name = "attachment";

        internal static readonly dynamic Definition = new
        {
            description = "Extract attachment information",
            processors = new[]
            {
                new
                {
                    attachment = new
                    {
                        field = DefaultFields.AttachmentData
                    }
                }
            }
        };
    }
}