using Azure;
using Azure.AI.ContentUnderstanding;
using Azure.Storage.Blobs;
using TranscriptAnalyzer.POC.Application.Services;
using TranscriptAnalyzer.POC.Infrastructure.Repositories;

string endpoint = Environment.GetEnvironmentVariable("CONTENTUNDERSTANDING_ENDPOINT") ?? throw new Exception("CONTENTUNDERSTANDING_ENDPOINT NOT FOUND");
string key = Environment.GetEnvironmentVariable("CONTENTUNDERSTANDING_KEY") ?? throw new Exception("CONTENTUNDERSTANDING_KEY NOT FOUND");
var client = new ContentUnderstandingClient(new Uri(endpoint), new AzureKeyCredential(key));

var option = "0";

while (option != "4")
{
    Console.WriteLine("Select an Option...");
    Console.WriteLine("1. Create a custom analyzer for student transcripts");
    Console.WriteLine("2. List analyzers");
    Console.WriteLine("3. Test analyzer");
    Console.WriteLine("4. Exit...");
    Console.Write("Enter Option: ");
    option = Console.ReadLine();
    switch (option)
    {
        case "1":
            await CreateCustomAnalyzer(client);
            break;
        case "2":
            await ListAnalyzers();
            break;
        case "3":
            await AnalyzeTranscript();
            break;
        case "4":
            break;
        case "delete":
            await DeleteAnalyzer();
            break;
        default:
            Console.WriteLine("Invalid option selected.");
            break;
    }
}

async Task DeleteAnalyzer()
{
    Console.WriteLine("--------------------");
    Console.Write("Type Analyzer ID to delete: ");
    string analyzerId = Console.ReadLine() ?? throw new Exception("Analyzer ID is required");
    await client.DeleteAnalyzerAsync(analyzerId);
    Console.WriteLine($"Analyzer '{analyzerId}' deleted successfully!");
}

async Task ListAnalyzers()
{
    Console.WriteLine("--------------------");
    ContentUnderstandingRepository contentUnderstandingRepository = new(client);
    var analyzers = await contentUnderstandingRepository.GetAnalyzers();
    Console.WriteLine($"Found {analyzers.Count} analyzers:");
    analyzers.ForEach(analyzer => Console.WriteLine($"- {analyzer}"));
}

async Task AnalyzeTranscript()
{
    Console.WriteLine("--------------------");
    Console.Write("Type Analyzer ID: ");
    string analyzerId = Console.ReadLine() ?? throw new Exception("Analyzer ID is required");


    // get list of documents from ./TestDocuments and display to user
    string[] files = Directory.GetFiles(".\\TestDocuments");
    for (int i = 0; i < files.Length; i++)
    {
        Console.WriteLine($"{i + 1}. {Path.GetFileName(files[i])}");
    }
    Console.Write("Enter document to analyze: ");
    int fileOption = 0;
    while (!int.TryParse(Console.ReadLine(), out fileOption) || fileOption < 1 || fileOption > files.Length)
    {
        Console.Write("Invalid option. Enter document to analyze: ");
    }
    var filePath = files[fileOption - 1];

    using var stream = File.OpenRead(filePath);

    BlobStorageService blobStorageService = new(
        new StorageAccountRepository(
            new BlobServiceClient(
                Environment.GetEnvironmentVariable("AzureBlobStorage") ?? throw new Exception("AzureBlobStorage NOT FOUND"))));


    var blobUrl = await blobStorageService.UploadFileAsync(stream, "application/pdf");


    ContentUnderstandingRepository contentUnderstandingRepository = new(client);
    var courses = await contentUnderstandingRepository.TryGetTranscriptCoursesFromDocument(blobUrl, analyzerId);
    Console.WriteLine($"Extracted {courses.Count} courses:");
    courses.ForEach(course =>
    {
        Console.WriteLine($"- {course.Code}: {course.Title} ({course.Credits} credits) - Grade: {course.Grade}, Taken: {course.Month}/{course.Year}, Calendar System: {course.CalendarSystem}");
    });

}

static async Task<string> CreateCustomAnalyzer(ContentUnderstandingClient client)
{
    Console.WriteLine("--------------------");
    #region Field Definitions

    string analyzerId = $"transcript_analyzer_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
    var coursesFieldDef = new ContentFieldDefinition
    {
        Type = ContentFieldType.Array,
        Method = GenerationMethod.Generate,
        Description = "The list of courses taken by the student in the transcript document",
        EstimateSourceAndConfidence = true
    };

    var courseItemFieldDef = new ContentFieldDefinition
    {
        Description = "The definition of each course item in the courses array",
        Type = ContentFieldType.Object,
        Method = GenerationMethod.Generate,
        EstimateSourceAndConfidence = true
    };

    courseItemFieldDef.Properties.Add("Month", new ContentFieldDefinition
    {
        Description = "The month when the course was taken, the value must be an integer from 1 to 12",
        Type = ContentFieldType.Integer,
        Method = GenerationMethod.Generate,
        EstimateSourceAndConfidence = true
    });

    courseItemFieldDef.Properties.Add("Year", new ContentFieldDefinition
    {
        Description = "The year when the course was taken, the value must be an integer with 4 digits",
        Type = ContentFieldType.Integer,
        Method = GenerationMethod.Extract,
        EstimateSourceAndConfidence = true
    });

    courseItemFieldDef.Properties.Add("Code", new ContentFieldDefinition
    {
        Description = "The code of the course taken by the student",
        Type = ContentFieldType.String,
        Method = GenerationMethod.Extract,
        EstimateSourceAndConfidence = true
    });

    courseItemFieldDef.Properties.Add("Title", new ContentFieldDefinition
    {
        Description = "The title of the course taken by the student",
        Type = ContentFieldType.String,
        Method = GenerationMethod.Extract,
        EstimateSourceAndConfidence = true
    });

    courseItemFieldDef.Properties.Add("Grade", new ContentFieldDefinition
    {
        Description = "The grade of the course taken by the student, the value can be either a letter grade (A, B, C, D, F) or a percentage (0-100%)",
        Type = ContentFieldType.String,
        Method = GenerationMethod.Extract,
        EstimateSourceAndConfidence = true
    });

    courseItemFieldDef.Properties.Add("Credits", new ContentFieldDefinition
    {
        Description = "The number of credits of the course earned by the student",
        Type = ContentFieldType.Number,
        Method = GenerationMethod.Extract,
        EstimateSourceAndConfidence = true
    });


    var calendarSystemFieldDef = new ContentFieldDefinition
    {
        Description = "The calendar system used in the transcript document for the course taken by the student, the value can be either 'Quarter', 'Semester', 'Trimester' or 'Quarted Calculated'",
        Type = ContentFieldType.String,
        Method = GenerationMethod.Generate,
        EstimateSourceAndConfidence = true
    };
    calendarSystemFieldDef.Enum.Add("Quarter");
    calendarSystemFieldDef.Enum.Add("Semester");
    calendarSystemFieldDef.Enum.Add("Trimester");
    calendarSystemFieldDef.Enum.Add("Quarted Calculated");

    courseItemFieldDef.Properties.Add("CalendarSystem", calendarSystemFieldDef);

    coursesFieldDef.ItemDefinition = courseItemFieldDef;

    #endregion


    var fieldSchema = new ContentFieldSchema(new Dictionary<string, ContentFieldDefinition>()
    {
        { "Courses", coursesFieldDef }
    });

    var config = new ContentAnalyzerConfig
    {
        EnableFormula = true,
        EnableLayout = true,
        EnableOcr = true,
        EstimateFieldSourceAndConfidence = true,
        ShouldReturnDetails = true
    };

    var customAnalyzer = new ContentAnalyzer
    {
        BaseAnalyzerId = "prebuilt-document",
        Description = "An analyzer for extracting a list of courses, from student transcripts",
        Config = config,
        FieldSchema = fieldSchema
    };

    customAnalyzer.Models["completion"] = "gpt-4.1";
    customAnalyzer.Models["embedding"] = "text-embedding-3-large"; // Required when using field_schema and prebuilt-document base analyzer


    Console.WriteLine($"Creating Analyzer '{analyzerId}...'");


    var operation = await client.CreateAnalyzerAsync(
        WaitUntil.Completed,
        analyzerId,
        customAnalyzer);



    ContentAnalyzer result = operation.Value;
    Console.WriteLine($"Analyzer '{analyzerId}'" + " created successfully!");

    // Get the full analyzer details after creation
    var analyzerDetails =
        await client.GetAnalyzerAsync(analyzerId);
    result = analyzerDetails.Value;

    if (result.Description != null)
    {
        Console.WriteLine($"  Description: {result.Description}");
    }

    if (result.FieldSchema?.Fields != null)
    {
        Console.WriteLine($"  Fields" + $" ({result.FieldSchema.Fields.Count}):");

        foreach (var kvp in result.FieldSchema.Fields)
        {
            var method = kvp.Value.Method?.ToString() ?? "auto";
            var fieldType = kvp.Value.Type?.ToString() ?? "unknown";

            Console.WriteLine($"    - {kvp.Key}:" + $" {fieldType} ({method})");
        }
    }

    return analyzerId;
}





