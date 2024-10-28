using System.Reflection;
using NetArchTest.Rules;
using Xunit.Abstractions;

namespace FileOrganizer.Tests;

public class ArchitectureTests(ITestOutputHelper output)
{
    [Fact]
    public void ViewModels_Should_Not_Depend_On_Views()
    {
        // Arrange
        output.WriteLine("Arranging test for ViewModels_Should_Not_Depend_On_Views...");
        var assembly = typeof(FileOrganizer.ViewModels.ViewModelBase).Assembly;

        // Act
        output.WriteLine("Analyzing dependencies in assembly: {0}", assembly.FullName);
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace("FileOrganizer.ViewModels")
            .ShouldNot()
            .HaveDependencyOn("FileOrganizer.Views")
            .GetResult();

        // Output result details for debugging
        if (!result.IsSuccessful)
        {
            output.WriteLine("Test failed. Violations:");
            foreach (var failure in result.FailingTypes)
            {
                output.WriteLine(failure.FullName);
            }
        }

        // Assert
        Assert.True(result.IsSuccessful, "ViewModels should not depend on Views.");
    }

    [Fact]
    public void Views_Can_Depend_On_ViewModels()
    {
        // Arrange
        output.WriteLine("Arranging test for Views_Can_Depend_On_ViewModels...");
        var assembly1 = typeof(FileOrganizer.Views.MainWindow).Assembly;
        var assembly2 = typeof(FileOrganizer.Views.SettingsWindow).Assembly;
        var assemblies = new List<Assembly> { assembly1, assembly2 };

        // Act
        output.WriteLine("Analyzing dependencies in assemblies...");
        foreach (var assembly in assemblies)
        {
            output.WriteLine(" - Assembly: {0}", assembly.FullName);
        }

        var result = Types.InAssemblies(assemblies)
            .That()
            .ResideInNamespace("FileOrganizer.Views")
            .Should()
            .HaveDependencyOn("FileOrganizer.ViewModels")
            .GetResult();

        // Output result details for debugging
        if (!result.IsSuccessful)
        {
            output.WriteLine("Test failed. Violations:");
            foreach (var failure in result.FailingTypes)
            {
                output.WriteLine(failure.FullName);
            }
        }

        // Assert
        Assert.True(result.IsSuccessful, "Views should depend on ViewModels.");
    }
}