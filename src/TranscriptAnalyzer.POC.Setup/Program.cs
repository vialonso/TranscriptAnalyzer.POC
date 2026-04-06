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
        Description = 
        @"Extract an array named 'Courses' containing all courses listed in the student's transcript. 
        Each item should represent a single course the student has taken, as detailed in the transcript document.",
        EstimateSourceAndConfidence = true
    };

    var courseItemFieldDef = new ContentFieldDefinition
    {
        Description = 
        @"Defines the structure and properties of a single course entry within the 'Courses' array, as found in the transcript. 
        Each property should be extracted or generated based on the transcript's content.",
        Type = ContentFieldType.Object,
        Method = GenerationMethod.Generate,
        EstimateSourceAndConfidence = true
    };

    courseItemFieldDef.Properties.Add("Month", new ContentFieldDefinition
    {
        Description = 
        @"The numeric month (1-12) indicating when the course was taken, as specified in the transcript. 
        If not explicitly stated, infer from context if possible.",
        Type = ContentFieldType.Integer,
        Method = GenerationMethod.Generate,
        EstimateSourceAndConfidence = true
    });

    courseItemFieldDef.Properties.Add("Year", new ContentFieldDefinition
    {
        Description = 
        @"The four-digit year (e.g., 2023) when the course was taken, as shown in the transcript. 
        Extract this value directly from the document.",
        Type = ContentFieldType.Integer,
        Method = GenerationMethod.Extract,
        EstimateSourceAndConfidence = true
    });

    courseItemFieldDef.Properties.Add("Code", new ContentFieldDefinition
    {
        Description = 
        "The official course code or identifier (e.g., 'MATH101') as listed in the transcript for the course.",
        Type = ContentFieldType.String,
        Method = GenerationMethod.Extract,
        EstimateSourceAndConfidence = true
    });

    courseItemFieldDef.Properties.Add("Title", new ContentFieldDefinition
    {
        Description = 
        @"The full title or name of the course as it appears in the transcript.",
        Type = ContentFieldType.String,
        Method = GenerationMethod.Extract,
        EstimateSourceAndConfidence = true
    });

    courseItemFieldDef.Properties.Add("Grade", new ContentFieldDefinition
    {
        Description = 
        @"The grade received for the course, which may be a letter grade (A, B, C, D, F) or a percentage (0-100%), as recorded in the transcript.",
        Type = ContentFieldType.String,
        Method = GenerationMethod.Extract,
        EstimateSourceAndConfidence = true
    });

    courseItemFieldDef.Properties.Add("Credits", new ContentFieldDefinition
    {
        Description = 
        @"The number of academic credits earned for the course, as specified in the transcript. This is typically a numeric value.",
        Type = ContentFieldType.Number,
        Method = GenerationMethod.Extract,
        EstimateSourceAndConfidence = true
    });


    var calendarSystemFieldDef = new ContentFieldDefinition
    {
        Description = 
        @"The academic calendar system used for the course, as indicated in the transcript. 
        Valid values are: 'Quarter', 'Semester', 'Trimester', or 'Quarted Calculated'.",
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
        Description = 
        @"Custom analyzer for extracting a detailed, structured list of all courses from student transcript documents. 
        The analyzer identifies each course and its associated properties,
        including code, title, grade, credits, date taken, and academic calendar system, based on the transcript's content.",
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









