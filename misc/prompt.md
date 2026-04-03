I want to build a solution using Content undersanding in Azure Fundry:

---

* Context: I work for a well known university in the United States and develop solutions in .NET and Azure, we have an application called "Blueprint",  This application has a UI where users can search for a student, then the user sees a list of all the student's transcripts from their previous institutions/schools, when selecting a single transcript, the UI displays the PDF of the transcripts, the user sees the document manually start populating a table with the list of courses the student has taken for later credit evaluation. The table has the folowing fields:





    * Month: The month when the course was taken.
    * Year: The Year when the course was taken.
    * Code: The course code.
    * Title: The title of the course.
    * Grade: The grade the student got (example: A, A+, A-, B, C, etc)
    * Credits: A numeric vaue for the credits earned.
    * Calendar System: It can be Quarter, Semester, Trimester, Quarted Calculated.

    Since we're working with different schools and institutions, each PDF has different layouts. Therefore, We're thinking of using the field extraction feature (prebuilt document fields analyzer) in Content undersanding in Azure Fundry to get a Json response that includes all the document fields.

    Additionally, as a next step, we want to use this JSON and ask a Gpt Model (Open AI) in Azure to translate this json into another json schema that fits our blueprint application, here's a sample of the final Json result I want:

    `
    {
        "Title": "App Child Develop & Fam Engage",
        "Month": 8,
        "Year": 2019,
        "Code": "ECED110",
        "Credits": 3.00,
        "Grade": "2.5",
        "CalendarSystemID": 2
    }
    `

---

* Questions:

    * Give me the pros and cons for this approach.
    * Is there an easier way to get the final JSON I want, maybe without having to separately call another model to translate my results from content undersanding?
    * What are other options I should consider to architect this solution?



---

* Task: Before implementing this solution directly in my blurprint application, I want to write a simple .NET console application. It will work as simple as just reading a PDF and returns the final JSON result I want for bluprint.

    * Give me me a step by step guide to accomplish this.