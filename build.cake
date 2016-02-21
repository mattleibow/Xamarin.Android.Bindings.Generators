//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");
var configuration = Argument("configuration", "Release");

//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Define paths
var buildDir = Directory("./src/ParameterNameGeneratorTask/bin") + Directory(configuration);
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
    
    DotNetBuild(solutionFile, config => {
        config.Configuration = configuration;
    });
});

Task("Package")
    .IsDependentOn("Build")
    .Does(() =>
{
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

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);