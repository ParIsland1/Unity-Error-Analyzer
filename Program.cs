// This line imports the OpenAI chat tools
// OpenAI types I can use now, ChatClient, ChatMessage, SystemChatMessage, UserChatMessage, ChatCompletion, etc.
using OpenAI.Chat;
using System.ClientModel;

// This creates the web app builder
var builder = WebApplication.CreateBuilder(args);

// I am creating the OpenAI chat client as a singleton, so it is ony created once.
// I am also reading the API key from an environment variable, and throwing an error if it is not set, to avoid hardcoding secrets in the code. 
// The model I am using is "gpt-5.4-mini", which is a smaller and faster version of GPT-4, suitable for this kind of task
builder.Services.AddSingleton(_ =>
{
    string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    if (string.IsNullOrWhiteSpace(apiKey))
    {
        throw new InvalidOperationException(
            "OPENAI_API_KEY environment variable is missing. Set it before running the app."
        );
    }

    return new ChatClient(
        model: "gpt-5.4-mini",
        apiKey: apiKey
    );
});

// Now I am building the app, which will be a API that listens for POST requests to /api/analyze 
// It uses the ChatClient I made earlier to analyze Unity errors and code snippets provided in the request body.
var app = builder.Build();

// These lines let my app use the default files like index.html that were made with Microsoft Asp.NET core
// Use Defaultfiles lets the app automatically use index.html as the default page when someone visits the root URL.
// Use StaticFiles allows the browser to access static files like HTML, CSS, and JavaScript that are in the wwwroot folder of the project. 
//This is important for serving the frontend of the application.
app.UseDefaultFiles();
app.UseStaticFiles();

// This is creating the API endpoint. When the frontend sends a POST request to /api/analyze (the address of the backend function),
// it runs this function that takes the AnalyzeUnityErrorRequest object, and the ChatClient from the services.
// The AnalyzeUnityErrorRequest is an object to contain the information the user sends, like the error message, code snippet, and context of what they were doing in Unity when the error happened.
// AnalyzeUnityErrorRequest is defined at the bottom of this file.
app.MapPost("/api/analyze", async Task<IResult> (
    AnalyzeUnityErrorRequest request,
    ChatClient chatClient
) =>
{
    // Validating the user actually sent something.
    bool hasError = !string.IsNullOrWhiteSpace(request.ErrorMessage);
    bool hasCode = !string.IsNullOrWhiteSpace(request.CodeSnippet);

    if (!hasError && !hasCode)
    {
        return Results.BadRequest(new
        {
            error = "Paste a Unity error, a script snippet, or both."
        });
    }

    // This is making sure the user doesn't send a super long input string.
    int totalLength =
        (request.ErrorMessage?.Length ?? 0) +
        (request.CodeSnippet?.Length ?? 0) +
        (request.Context?.Length ?? 0);

    if (totalLength > 30000)
    {
        return Results.BadRequest(new
        {
            error = "Input is too long. Paste only the most relevant error and script section."
        });
    }

// Here I am creating a more detailed prompt for the AI to use based on the user input.
// Basically taking the user input and putting into a clear standardized format for the AI to analyze.
    string userPrompt = $"""
    Analyze this Unity problem.

    Unity Console Error:
    {request.ErrorMessage}

    What the user was doing:
    {request.Context}

    Relevant C# script/code:
    {request.CodeSnippet}
    """;
// This is the system message to tell the AI exactly how to analyze the problem and behave.
// First I give it a clear role and then define exactly what it's job is and what it should not do to prevent hallucinations.
// Then I give it a very specific format to return the answer in.
// This ensures that answers are uniform and specific, not vague.
    List<ChatMessage> messages = new()
    {
        new SystemChatMessage("""
        You are a senior Unity C# debugging assistant.

        Your job:
        - Diagnose Unity console errors and C# script issues.
        - Explain the cause in beginner-friendly terms.
        - Give practical Unity-specific fixes.
        - Mention Inspector setup problems when relevant.
        - Do not pretend to know files the user did not provide.
        - If more information is needed, say exactly what the user should check.

        Return your answer in this format:

        ## Likely Cause
        Explain the most likely cause.

        ## Why This Happens
        Explain the Unity/C# reason.

        ## Fix Steps
        Give numbered steps.

        ## Code Fix
        Provide corrected code if possible.

        ## Unity Inspector Checks
        List what to check in the Unity Inspector.

        ## Confidence
        High, Medium, or Low, with a short reason.
        """),

// Here I am giving the AI the user message after the system message so the AI knows what to do with the user input.
// The AI will take the system message as instructions on how to analyze, and then the user message is the actual problem to analyze.
        new UserChatMessage(userPrompt)
    };

    // This is where I am calling the OpenAI, but I am wrapping it in a try catch to handle different types of errors.
    // The most common would be billing issues with my OpenAI account credits, or the user hitting rate limits by sending too many requests in a short time.
try
{
// This is where I call the OpenAI API to get the analysis of the Unity problem based on the messages I created.
// AI API calls happen over the internet and are not instant so I am using await async to let the server wait without freezing everything else.
    ChatCompletion completion = await chatClient.CompleteChatAsync(messages);

// This is taking the AI response and putting it into a string to return to the frontend, if the AI returned something.
// If the AI did not return anything, it gives a default message saying no analysis was returned.
    string analysis = completion.Content.Count > 0
        ? completion.Content[0].Text
        : "No analysis was returned.";

// Return the analysis in a structured response object, AnalyzeUnityErrorResponse, which is defined at the bottom of this file.
    return Results.Ok(new AnalyzeUnityErrorResponse(analysis));
}
catch (ClientResultException ex) when (
    ex.Status == 429 &&
    ex.Message.Contains("insufficient_quota", StringComparison.OrdinalIgnoreCase)
)
{
    return Results.BadRequest(new
    {
        error = "OpenAI API quota/billing error. Check your OpenAI Platform billing, credits, and usage limits."
    });
}
catch (ClientResultException ex) when (ex.Status == 429)
{
    return Results.BadRequest(new
    {
        error = "OpenAI rate limit error. Wait a bit and try again, or check your API rate limits."
    });
}
catch (ClientResultException ex)
{
    return Results.BadRequest(new
    {
        error = $"OpenAI API error: {ex.Status} - {ex.Message}"
    });
}
catch (Exception ex)
{
    return Results.BadRequest(new
    {
        error = $"Unexpected server error: {ex.Message}"
    });
}
});

app.Run();

// This is the request object that contains the user input strings for analysis
// It has the error message, code snippet, and context of what they were doing in Unity when the error happened.
public record AnalyzeUnityErrorRequest(
    string? ErrorMessage,
    string? CodeSnippet,
    string? Context
);

// This is the response object that contains the AI analysis string that will be sent back to the frontend to display to the user.
public record AnalyzeUnityErrorResponse(
    string Analysis
);