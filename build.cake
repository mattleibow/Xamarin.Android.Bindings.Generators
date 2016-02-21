//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");
var libraryversion = Argument("libraryversion", "1.0.0.0");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define paths
var outputDir = Directory("./output/");
var projectDir = Directory("./src/ParameterNameGeneratorTask/");
var buildDir = projectDir + Directory("bin/") + Directory(configuration);
var solutionFile = File("./src/Xamarin.Android.Bindings.Generators.sln");
var testsSolutionFile = File("./src/Xamarin.Android.Bindings.Generators.Tests.sln");

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Clean")
    .Does(() =>
{
    CleanDirectory(buildDir);
});

Task("Build")
    .Does(() =>
{
    NuGetRestore(solutionFile);
    
    var infoFile = projectDir + File("Properties/AssemblyInfo.cs");
    var info = ParseAssemblyInfo(infoFile);
    CreateAssemblyInfo(infoFile, new AssemblyInfoSettings {
        // assembly
        ComVisible = info.ComVisible,
        Configuration = configuration,
        Guid = info.Guid,
        Product = info.Product,
        // descriptions
        Title = info.Title,
        Description = info.Description,
        Company = info.Company,
        Copyright = info.Copyright.Replace("<year>", DateTime.UtcNow.Year.ToString()),
        Trademark = info.Trademark.Replace("<year>", DateTime.UtcNow.Year.ToString()),
        //versions
        Version = libraryversion,
        FileVersion = libraryversion,
        InformationalVersion = libraryversion,
    });
    
    DotNetBuild(solutionFile, config => {
        config.Configuration = configuration;
    });
    
    if (!DirectoryExists(outputDir)) {
        CreateDirectory(outputDir);
    }
    
    CopyFileToDirectory(buildDir + File("ParameterNameGeneratorTask.dll"), outputDir);
    CopyFileToDirectory(buildDir + File("Xamarin.Android.Tools.Bytecode.dll"), outputDir);
    CopyFileToDirectory(buildDir + File("Ionic.Zip.dll"), outputDir);
    CopyFileToDirectory(buildDir + File("Xamarin.Andoid.ParameterNameGenerator.targets"), outputDir);
});

Task("Package")
    .IsDependentOn("Build")
    .Does(() =>
{
    NuGetPack("./nuget/Xamarin.Android.Bindings.Generators.nuspec", new NuGetPackSettings {
        BasePath = "./",
        OutputDirectory = "./output/",
        Version = libraryversion
    });
});

Task("Tests")
    .IsDependentOn("Build")
    .Does(() =>
{
    NuGetRestore(testsSolutionFile);
    
    if (IsRunningOnWindows()) {
        MSBuild(testsSolutionFile, s => s.SetConfiguration(configuration).SetMSBuildPlatform(MSBuildPlatform.x86));
    } else {
        XBuild(testsSolutionFile, s => s.SetConfiguration(configuration));
    }
    
    NUnit3("./src/*.Tests/bin/" + configuration + "/*.Tests.dll", new NUnit3Settings {
        NoResults = true
    });
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Tests");

Task("CI")
    .IsDependentOn("Build")
    .IsDependentOn("Package")
    .IsDependentOn("Tests");

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
