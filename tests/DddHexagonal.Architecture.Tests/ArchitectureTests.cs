using System.Reflection;
using DddHexagonal.Application.Ports.In;
using DddHexagonal.Domain.Common;
using DddHexagonal.Infrastructure.Persistence;
using NetArchTest.Rules;

namespace DddHexagonal.Architecture.Tests;

/// <summary>
/// Executable documentation of the dependency rule:
///   Api -> Application -> Domain      Infrastructure -> Application -> Domain
/// If somebody breaks the hexagon (e.g. uses EF Core in a use case), the build goes red.
/// </summary>
public sealed class ArchitectureTests
{
    private const string DomainNs = "DddHexagonal.Domain";
    private const string ApplicationNs = "DddHexagonal.Application";
    private const string InfrastructureNs = "DddHexagonal.Infrastructure";
    private const string ApiNs = "DddHexagonal.Api";

    private static readonly string[] Frameworks =
    [
        "Microsoft.EntityFrameworkCore", "Microsoft.AspNetCore", "Pomelo", "MySqlConnector", "Microsoft.Extensions.DependencyInjection",
    ];

    private static readonly Assembly Domain = typeof(Entity).Assembly;
    private static readonly Assembly Application = typeof(IUseCase<,>).Assembly;
    private static readonly Assembly Infrastructure = typeof(AppDbContext).Assembly;
    private static readonly Assembly Api = typeof(Program).Assembly;

    private static void AssertNoViolations(TestResult result) =>
        Assert.True(result.IsSuccessful, "Violations: " + string.Join(", ", result.FailingTypeNames ?? []));

    // ---- dependency rule (type level) ----------------------------------------------------------

    [Fact]
    public void Domain_DependsOnNothingOutsideItself() =>
        AssertNoViolations(Types.InAssembly(Domain).ShouldNot()
            .HaveDependencyOnAny([ApplicationNs, InfrastructureNs, ApiNs, .. Frameworks]).GetResult());

    [Fact]
    public void Application_DependsOnlyOnDomain() =>
        AssertNoViolations(Types.InAssembly(Application).ShouldNot()
            .HaveDependencyOnAny([InfrastructureNs, ApiNs, .. Frameworks]).GetResult());

    [Fact]
    public void Infrastructure_DoesNotKnowTheApi() =>
        AssertNoViolations(Types.InAssembly(Infrastructure).ShouldNot().HaveDependencyOn(ApiNs).GetResult());

    [Fact]
    public void ApiEndpointsAndHandlers_TalkToApplicationOnly_NeverToInfrastructureOrDomainInternals() =>
        // Only the composition root (Program, global namespace) may touch Infrastructure.
        AssertNoViolations(Types.InAssembly(Api).That().ResideInNamespaceStartingWith(ApiNs).ShouldNot()
            .HaveDependencyOnAny([InfrastructureNs, "Microsoft.EntityFrameworkCore"]).GetResult());

    // ---- dependency rule (assembly references) -------------------------------------------------

    [Fact]
    public void Domain_AssemblyReferencesNoProjectOrPackage()
    {
        var references = Domain.GetReferencedAssemblies().Select(a => a.Name!).ToList();
        Assert.All(references, name => Assert.False(
            name.StartsWith("DddHexagonal", StringComparison.Ordinal) || name.Contains("EntityFramework", StringComparison.Ordinal),
            $"Domain must stay dependency-free but references {name}"));
    }

    [Fact]
    public void Application_AssemblyReferencesOnlyDomain() =>
        Assert.Equal(
            ["DddHexagonal.Domain"],
            Application.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("DddHexagonal", StringComparison.Ordinal)));

    [Fact]
    public void Infrastructure_AssemblyReferencesOnlyApplicationAndDomain() =>
        Assert.Equal(
            ["DddHexagonal.Application", "DddHexagonal.Domain"],
            Infrastructure.GetReferencedAssemblies().Select(a => a.Name!).Where(n => n.StartsWith("DddHexagonal", StringComparison.Ordinal)).Order());

    // ---- ports & adapters conventions -----------------------------------------------------------

    [Fact]
    public void EveryUseCase_ImplementsTheInboundPort_IsSealed_AndLivesInApplication()
    {
        var useCases = Application.GetTypes().Where(t => t.Name.EndsWith("UseCase", StringComparison.Ordinal)).ToList();

        Assert.NotEmpty(useCases);
        Assert.All(useCases, t =>
        {
            Assert.True(t.IsSealed, $"{t.Name} should be sealed");
            Assert.Contains(t.GetInterfaces(), i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IUseCase<,>));
        });
    }

    [Fact]
    public void EveryRepositoryPort_HasItsAdapterInInfrastructure_AndNowhereElse()
    {
        var ports = Application.GetTypes().Where(t => t is { IsInterface: true } && t.Name.EndsWith("Repository", StringComparison.Ordinal)).ToList();
        Assert.Equal(3, ports.Count);

        foreach (var port in ports)
        {
            var implementations = new[] { Domain, Application, Infrastructure, Api }
                .SelectMany(a => a.GetTypes()).Where(t => t is { IsClass: true } && port.IsAssignableFrom(t)).ToList();

            var adapter = Assert.Single(implementations);
            Assert.Equal(Infrastructure, adapter.Assembly);
        }
    }

    [Fact]
    public void OutboundAdapters_AreInternal_SoOnlyThePortsAreVisible() =>
        AssertNoViolations(Types.InAssembly(Infrastructure).That().ResideInNamespaceStartingWith($"{InfrastructureNs}.Persistence.Repositories")
            .Should().NotBePublic().GetResult());

    [Fact]
    public void DriverAdapters_EndpointsAreInternal() =>
        AssertNoViolations(Types.InAssembly(Api).That().ResideInNamespace($"{ApiNs}.Endpoints").Should().NotBePublic().GetResult());

    // ---- DDD tactical rules -----------------------------------------------------------------

    [Fact]
    public void DomainObjects_HaveNoPublicSetters()
    {
        var types = Domain.GetTypes().Where(t => typeof(Entity).IsAssignableFrom(t) || typeof(ValueObject).IsAssignableFrom(t));

        Assert.All(types, t => Assert.All(
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance),
            p => Assert.True(p.SetMethod is null || !p.SetMethod.IsPublic, $"{t.Name}.{p.Name} has a public setter")));
    }

    [Fact]
    public void ValueObjects_AreSealed() =>
        AssertNoViolations(Types.InAssembly(Domain).That().Inherit(typeof(ValueObject)).Should().BeSealed().GetResult());

    [Fact]
    public void AggregatesAndEntities_AreSealed_AndUseFactoryMethods() =>
        Assert.All(
            Domain.GetTypes().Where(t => typeof(Entity).IsAssignableFrom(t) && !t.IsAbstract),
            t =>
            {
                Assert.True(t.IsSealed, $"{t.Name} should be sealed");
                Assert.Empty(t.GetConstructors(BindingFlags.Public | BindingFlags.Instance));
            });

    [Fact]
    public void DomainExceptions_AreTheOnlyExceptionsRaisedByTheDomain() =>
        AssertNoViolations(Types.InAssembly(Domain).That().Inherit(typeof(Exception)).Should().ResideInNamespace($"{DomainNs}.Common").GetResult());
}
