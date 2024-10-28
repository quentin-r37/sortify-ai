using CommunityToolkit.Mvvm.Input;
using Mono.Cecil;
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
    public void Models_Should_Not_Depend_On_ViewModels_Or_Views()
    {
        // Arrange
        var assembly = typeof(FileOrganizer.Models.ProcessedFileData).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace("FileOrganizer.Models")
            .ShouldNot()
            .HaveDependencyOnAny("FileOrganizer.ViewModels", "FileOrganizer.Views")
            .GetResult();

        // Assert
        Assert.True(result.IsSuccessful, "Models should not depend on ViewModels or Views.");
    }

    [Fact]
    public void Views_Should_Not_Have_Methods_Other_Than_Constructors()
    {
        // Arrange
        var assemblyPath = typeof(FileOrganizer.Views.MainWindow).Assembly.Location;
        var assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);

        // Act
        var violatingTypes = assemblyDefinition.MainModule.Types
            .Where(type => type.Namespace == "FileOrganizer.Views")
            .Select(type => new
            {
                Type = type,
                ViolatingMethods = GetViolatingMethods(type)
            })
            .Where(x => x.ViolatingMethods.Any())
            .Select(x => new
            {
                x.Type.FullName,
                Methods = x.ViolatingMethods.Select(m => m.Name)
            })
            .ToList();

        // Assert
        Assert.True(!violatingTypes.Any(),
            $"Views should not contain business logic methods. Violations found in: {string.Join(", ", violatingTypes.Select(vt => $"{vt.FullName} ({string.Join(", ", vt.Methods)})"))}");
    }

    private static IEnumerable<MethodDefinition> GetViolatingMethods(TypeDefinition type)
    {
        var allowedMethodNames = new HashSet<string> { "InitializeComponent" };
        var publicMethods = type.Methods
            .Where(method => !method.IsConstructor && !method.IsStatic);
        var propertyMethods = type.Properties
            .SelectMany(prop => new[] { prop.GetMethod, prop.SetMethod })
            .Where(m => m != null);
        var eventMethods = type.Events
            .SelectMany(evt => new[] { evt.AddMethod, evt.RemoveMethod })
            .Where(m => m != null);
        var allowedMethods = propertyMethods
            .Concat(eventMethods)
            .Concat(type.Methods.Where(m => allowedMethodNames.Contains(m.Name)));
        return publicMethods.Except(allowedMethods);
    }

    [Fact]
    public void ViewModels_Should_Derive_From_ViewModelBase()
    {
        // Arrange
        var assembly = typeof(FileOrganizer.ViewModels.MainWindowViewModel).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace("FileOrganizer.ViewModels")
            .And()
            .DoNotHaveName("ViewModelBase")
            .Should()
            .Inherit(typeof(FileOrganizer.ViewModels.ViewModelBase))
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
        Assert.True(result.IsSuccessful, "All ViewModels should inherit from ViewModelBase.");
    }

    [Fact]
    public void Services_Should_Not_Depend_On_Views()
    {
        // Arrange
        var assembly = typeof(FileOrganizer.Services.FileOrganizer).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ResideInNamespace("FileOrganizer.Services")
            .ShouldNot()
            .HaveDependencyOnAny("FileOrganizer.Views")
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
        Assert.True(result.IsSuccessful, "Services should not depend on Views");
    }

    [Fact]
    public void Commands_Should_Be_Defined_In_ViewModels()
    {
        // Arrange
        var assembly = typeof(FileOrganizer.ViewModels.ViewModelBase).Assembly;

        // Act
        var result = Types.InAssembly(assembly)
            .That()
            .ImplementInterface(typeof(IRelayCommand))
            .Should()
            .ResideInNamespace("FileOrganizer.ViewModels")
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
        Assert.True(result.IsSuccessful, "Commands should be defined within ViewModels.");

    }

}