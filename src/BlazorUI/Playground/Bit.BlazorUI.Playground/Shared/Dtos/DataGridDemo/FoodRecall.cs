using System.Text.Json.Serialization;

namespace Bit.BlazorUI.Playground.Shared.Dtos.DataGridDemo;

public class FoodRecall
{
    [JsonPropertyName("country")]
    public string Country { get; set; }

    [JsonPropertyName("city")]
    public string City { get; set; }

    [JsonPropertyName("address_1")]
    public string Address1 { get; set; }

    [JsonPropertyName("reason_for_recall")]
    public string ReasonForRecall { get; set; }

    [JsonPropertyName("address_2")]
    public string Address2 { get; set; }

    [JsonPropertyName("product_quantity")]
    public string ProductQuantity { get; set; }

    [JsonPropertyName("code_info")]
    public string CodeInfo { get; set; }

    [JsonPropertyName("center_classification_date")]
    public string CenterClassificationDate { get; set; }

    [JsonPropertyName("distribution_pattern")]
    public string DistributionPattern { get; set; }

    [JsonPropertyName("state")]
    public string State { get; set; }

    [JsonPropertyName("product_description")]
    public string ProductDescription { get; set; }

    [JsonPropertyName("report_date")]
    public string ReportDate { get; set; }

    [JsonPropertyName("classification")]
    public string Classification { get; set; }

    [JsonPropertyName("openfda")]
    public Openfda Openfda { get; set; }

    [JsonPropertyName("recalling_firm")]
    public string RecallingFirm { get; set; }

    [JsonPropertyName("recall_number")]
    public string RecallNumber { get; set; }

    [JsonPropertyName("initial_firm_notification")]
    public string InitialFirmNotification { get; set; }

    [JsonPropertyName("product_type")]
    public string ProductType { get; set; }

    [JsonPropertyName("event_id")]
    public string EventId { get; set; }

    [JsonPropertyName("more_code_info")]
    public string MoreCodeInfo { get; set; }

    [JsonPropertyName("recall_initiation_date")]
    public string RecallInitiationDate { get; set; }

    [JsonPropertyName("postal_code")]
    public string PostalCode { get; set; }

    [JsonPropertyName("voluntary_mandated")]
    public string VoluntaryMandated { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }
}
