namespace Epinova.ElasticSearch.Core.Events
{
    public delegate void OnBeforeIndexContent(System.EventArgs e);
    public delegate void OnBeforeUpdateItem(IndexItemEventArgs e);
    public delegate void OnAfterUpdateBestBet(BestBetEventArgs e);
}