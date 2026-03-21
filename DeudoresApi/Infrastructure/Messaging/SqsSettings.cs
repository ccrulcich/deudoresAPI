namespace DeudoresApi.Infrastructure.Messaging;

public class SqsSettings
{
    public string ServiceUrl { get; set; } = string.Empty; // LocalStack: http://localstack:4566
    public string QueueUrl { get; set; } = string.Empty;
    public string AccessKey { get; set; } = "test";        // LocalStack acepta cualquier valor
    public string SecretKey { get; set; } = "test";
    public string Region { get; set; } = "us-east-1";
}
