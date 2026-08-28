using Components.Result;
using FluentValidation.Results;

Console.WriteLine("--- Components.Result sample ---");
Console.WriteLine();

Print("Ok() — no content", Result.Ok());
Print("Error(string)", Result.Error("campaign name must not be empty"));
Print("Error(List<ValidationFailure>)", Result.Error(
[
    new ValidationFailure("Name", "Name is required"),
    new ValidationFailure("TargetGroupId", "TargetGroupId must be greater than zero"),
]));

Console.WriteLine();
Console.WriteLine("--- Result<T> ---");
Console.WriteLine();

Print("Ok(content)", Result<string>.Ok("campaign-42"));
Print("Error(string), no content", Result<string>.Error("database unavailable"));

// Simulates the shape a real use case would return: business validation fails,
// so the caller gets Result<T>.Error — never an exception.
Result<int> publishCampaign(bool campaignExists) =>
    campaignExists
        ? Result<int>.Ok(42)
        : Result<int>.Error("campaign 42 does not exist");

Print("Simulated use case — success", publishCampaign(campaignExists: true));
Print("Simulated use case — failure", publishCampaign(campaignExists: false));

static void Print(string label, Result result)
{
    Console.WriteLine($"{label}:");
    Console.WriteLine($"  Success = {result.Success}");
    Console.WriteLine($"  ErrorMessage = \"{result.ErrorMessage}\"");
    if (result is Result<string> { Content: not null } withContent)
        Console.WriteLine($"  Content = \"{withContent.Content}\"");
    if (result is Result<int> { Success: true } withIntContent)
        Console.WriteLine($"  Content = {withIntContent.Content}");
    Console.WriteLine();
}
