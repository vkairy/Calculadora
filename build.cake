#tool "nuget:?package=OpenCover&version=4.7.1221"
#tool "nuget:?package=NUnit.ConsoleRunner&version=3.12.0"
#tool "nuget:?package=ReportGenerator&version=4.8.11"

var target = Argument("target", "Default");

Task("NugetRestore")
    .Does(() =>
    {
        NuGetRestore("./Calculadora.sln");
    });

Task("BuildTest")
    .IsDependentOn("NugetRestore")
    .Does(() => 
    {
        MSBuild("./Calculadora.Tests/Calculadora.Tests.csproj", 
            new MSBuildSettings {
                Verbosity = Verbosity.Minimal,
                Configuration = "Debug"
            }
        );
    });

Task("OpenCover")
    .IsDependentOn("BuildTest")
    .Does(() => 
    {
        var openCoverSettings = new OpenCoverSettings()
        {
            Register = "user",
            SkipAutoProps = true,
            ArgumentCustomization = args => args.Append("-coverbytest:*.Tests.dll").Append("-mergebyhash")
        };

        if (!DirectoryExists("./GeneratedReports"))
            CreateDirectory("./GeneratedReports");
        
        var outputFile = new FilePath("./GeneratedReports/CalculadoraReport.xml");

        OpenCover(tool => {
                var testAssemblies = GetFiles("./Calculadora.Tests/bin/Debug/Calculadora.Tests.dll");
                tool.NUnit3(testAssemblies);
            },
            outputFile,
            openCoverSettings
                .WithFilter("+[Calculadora*]*")
                .WithFilter("-[Calculadora.Tests]*")
        );
    });

Task("ReportGenerator")
    .IsDependentOn("OpenCover")
    .Does(() => 
    {
        var reportGeneratorSettings = new ReportGeneratorSettings()
        {
            HistoryDirectory = new DirectoryPath("./GeneratedReports/ReportsHistory"),
            ToolPath = new FilePath("./tools/ReportGenerator.4.8.11/tools/net47/reportgenerator.exe")
        };

        ReportGenerator(new FilePath("./GeneratedReports/CalculadoraReport.xml"), "./GeneratedReports/ReportGeneratorOutput", reportGeneratorSettings);
    });

Task("Default")
    .IsDependentOn("ReportGenerator")
    .Does(() => 
    { 
        if (IsRunningOnWindows())
        {
            var reportFilePath = ".\\GeneratedReports\\ReportGeneratorOutput\\index.htm";
         
            StartProcess("explorer", reportFilePath);
        }
    });

RunTarget(target);
