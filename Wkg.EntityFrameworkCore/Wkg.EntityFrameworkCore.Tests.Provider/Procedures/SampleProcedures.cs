using System.Data;
using Wkg.EntityFrameworkCore.SourceGeneration.Discovery;
using Wkg.EntityFrameworkCore.Tests.Provider.Builder;

namespace Wkg.EntityFrameworkCore.Tests.Provider.Procedures;

public sealed class AddNumbers : TestStoredProcedure<AddNumbersContainer>, ITestProcedureConfiguration<AddNumbers, AddNumbersContainer>
{
    public static void Configure(TestProcedureBuilder<AddNumbers, AddNumbersContainer> self)
    {
        _ = self.ToDatabaseFunction("add_numbers")
            .ReturnsScalar(io => io.Result)
            .HasDbType(DbType.Int32);
        _ = self.Parameter(io => io.Left)
            .HasName("a")
            .HasDbType(DbType.Int32);
        _ = self.Parameter(io => io.Right)
            .HasName("b")
            .HasDbType(DbType.Int32);
    }
}

public record AddNumbersContainer(int Left, int Right, int Result);

public sealed class GetPeople : TestStoredProcedure<GetPeopleContainer, Person>, ITestProcedureConfiguration<GetPeople, GetPeopleContainer>
{
    public static void Configure(TestProcedureBuilder<GetPeople, GetPeopleContainer> self)
    {
        _ = self.ToDatabaseProcedure("get_people")
            .HasSchema("app");
        _ = self.Parameter(io => io.Name)
            .HasName("name_in")
            .HasDbType(DbType.String)
            .HasSize(255);
        _ = self.Parameter(io => io.InvalidCount)
            .HasName("invalid_count")
            .HasDirection(ParameterDirection.Output);
        _ = self.Parameter(io => io.Amount)
            .HasName("amount")
            .HasPrecision(precision => precision.Precision(10).Scale(2));
        TestResultBuilder<Person> result = self.Returns<Person>().AsCollection();
        _ = result.Column(row => row.Id)
            .HasName("id")
            .GetAsInt32();
        _ = result.Column(row => row.Name)
            .HasName("name")
            .GetAsString();
        _ = result.Column(row => row.Uuid)
            .HasName("uuid")
            .GetAsBytes()
            .RequiresConversion<byte[]>(bytes => new Guid(bytes));
    }
}

public record GetPeopleContainer(string Name, int InvalidCount, decimal Amount);

public record Person(int Id, string Name, Guid Uuid);

[ModelLoader(AssemblyDiscoveryFailureBehavior = AssemblyDiscoveryFailureBehavior.Silent)]
public sealed partial class TestProcedureLoader;
