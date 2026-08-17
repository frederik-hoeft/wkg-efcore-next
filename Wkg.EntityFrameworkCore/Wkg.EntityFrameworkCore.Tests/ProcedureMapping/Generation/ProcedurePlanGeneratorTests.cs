using Microsoft.CodeAnalysis;
using Wkg.EntityFrameworkCore.ProcedureMapping.Generation;
using Wkg.EntityFrameworkCore.ProcedureMapping.Runtime;
using Wkg.EntityFrameworkCore.Tests.Provider.Procedures;
using Wkg.EntityFrameworkCore.Tests.Provider.Procedures.Generated;

namespace Wkg.EntityFrameworkCore.Tests.ProcedureMapping.Generation;

[TestClass]
public sealed class ProcedurePlanGeneratorTests
{
    [TestMethod]
    public void EmitsPlan_ForSimpleFunction()
    {
        const string source = """
            using System.Data;
            using Wkg.EntityFrameworkCore.Tests.Provider.Builder;

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
                }
            }

            public record AddNumbersContainer(int Left, int Result);
            """;

        GeneratorDriverRunResult result = GeneratorTestHost.Run(source);
        string generated = result.CombinedGeneratedSource();
        Assert.IsTrue(generated.Contains("AddNumbersProcedurePlan"), generated);
        Assert.IsTrue(generated.Contains("add_numbers"), generated);
        Assert.IsTrue(generated.Contains("HasDbType"), generated);
        Assert.IsFalse(result.GeneratorDiagnostics().Any(static diagnostic => diagnostic.Severity is DiagnosticSeverity.Error), string.Join(Environment.NewLine, result.GeneratorDiagnostics()));
    }

    [TestMethod]
    public void EmitsPlan_ForResultSetAndCompositeParameter()
    {
        const string source = """
            using System;
            using System.Data;
            using Wkg.EntityFrameworkCore.Tests.Provider.Builder;

            public sealed class GetPeople : TestStoredProcedure<GetPeopleContainer, Person>, ITestProcedureConfiguration<GetPeople, GetPeopleContainer>
            {
                public static void Configure(TestProcedureBuilder<GetPeople, GetPeopleContainer> self)
                {
                    _ = self.ToDatabaseProcedure("get_people").HasSchema("app");
                    _ = self.Parameter(io => io.Name).HasName("name_in").HasDbType(DbType.String).HasSize(255);
                    _ = self.Parameter(io => io.InvalidCount).HasName("invalid_count").HasDirection(System.Data.ParameterDirection.Output);
                    _ = self.Parameter(io => io.Amount).HasName("amount").HasPrecision(precision => precision.Precision(10).Scale(2));
                    var result = self.Returns<Person>().AsCollection();
                    _ = result.Column(row => row.Id).HasName("id").GetAsInt32();
                    _ = result.Column(row => row.Name).HasName("name").GetAsString();
                    _ = result.Column(row => row.Uuid).HasName("uuid").GetAsBytes().RequiresConversion<byte[]>(bytes => new Guid(bytes));
                }
            }

            public record GetPeopleContainer(string Name, int InvalidCount, decimal Amount);
            public record Person(int Id, string Name, Guid Uuid);
            """;

        GeneratorDriverRunResult result = GeneratorTestHost.Run(source);
        string generated = result.CombinedGeneratedSource();
        Assert.IsFalse(result.GeneratorDiagnostics().Any(static diagnostic => diagnostic.Severity is DiagnosticSeverity.Error), string.Join(Environment.NewLine, result.GeneratorDiagnostics()));
        StringAssert.Contains(generated, "GetPeopleProcedurePlan");
        StringAssert.Contains(generated, "HasSchema");
        StringAssert.Contains(generated, "Precision");
        StringAssert.Contains(generated, "Scale");
        StringAssert.Contains(generated, "GetAsInt32");
        StringAssert.Contains(generated, "new global::System.Guid");
        StringAssert.Contains(generated, "UnsafeAccessor");
    }

    [TestMethod]
    public void Diagnoses_ControlFlow()
    {
        const string source = """
            using Wkg.EntityFrameworkCore.Tests.Provider.Builder;

            public sealed class Bad : TestStoredProcedure<BadContainer>, ITestProcedureConfiguration<Bad, BadContainer>
            {
                public static void Configure(TestProcedureBuilder<Bad, BadContainer> self)
                {
                    if (true)
                    {
                        _ = self.ToDatabaseProcedure("bad");
                    }
                }
            }

            public record BadContainer(int Value);
            """;

        GeneratorDriverRunResult result = GeneratorTestHost.Run(source);
        Assert.IsTrue(result.GeneratorDiagnostics().Any(static diagnostic => diagnostic.Id == "WKGLIBEFC011"));
    }

    [TestMethod]
    public void Diagnoses_NonConstantArgument()
    {
        const string source = """
            using Wkg.EntityFrameworkCore.Tests.Provider.Builder;

            public sealed class Bad : TestStoredProcedure<BadContainer>, ITestProcedureConfiguration<Bad, BadContainer>
            {
                public static void Configure(TestProcedureBuilder<Bad, BadContainer> self)
                {
                    string name = "dynamic";
                    _ = self.ToDatabaseProcedure(name);
                }
            }

            public record BadContainer(int Value);
            """;

        GeneratorDriverRunResult result = GeneratorTestHost.Run(source);
        Assert.IsTrue(result.GeneratorDiagnostics().Any(static diagnostic => diagnostic.Id is "WKGLIBEFC012" or "WKGLIBEFC014"));
    }

    [TestMethod]
    public void GeneratedSamplePlans_BindAndReadOffline()
    {
        GetPeopleProcedurePlan plan = new();
        Assert.AreEqual("app.get_people", plan.ProcedureName);
        Assert.IsFalse(plan.IsFunction);
        Assert.IsTrue(plan.HasResult);
        Assert.IsTrue(plan.IsCollectionResult);
        Assert.AreEqual(3, plan.ParameterCount);

        GetPeopleContainer container = new("alice", 0, 12.5m);
        System.Data.Common.DbParameter?[] parameters = new System.Data.Common.DbParameter?[3];
        plan.BindParameters(parameters, container);

        Assert.AreEqual("name_in", parameters[0]!.ParameterName);
        Assert.AreEqual("alice", parameters[0]!.Value);
        Assert.AreEqual(255, parameters[0]!.Size);
        Assert.AreEqual("invalid_count", parameters[1]!.ParameterName);
        Assert.AreEqual(System.Data.ParameterDirection.Output, parameters[1]!.Direction);
        Assert.AreEqual("amount", parameters[2]!.ParameterName);
        Assert.AreEqual("p=10;s=2", parameters[2]!.SourceColumn);

        parameters[1]!.Value = 4;
        plan.StoreOutputs(parameters, container, scalarReturn: null);
        Assert.AreEqual(4, container.InvalidCount);

        using System.Data.DataTable table = new();
        table.Columns.Add("id", typeof(int));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("uuid", typeof(byte[]));
        Guid uuid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        table.Rows.Add(7, "alice", uuid.ToByteArray());
        using System.Data.DataTableReader reader = table.CreateDataReader();
        Assert.IsTrue(reader.Read());
        Person person = (Person)plan.ReadResult(reader);
        Assert.AreEqual(7, person.Id);
        Assert.AreEqual("alice", person.Name);
        Assert.AreEqual(uuid, person.Uuid);
    }

    [TestMethod]
    public void GeneratedLoader_RegistersDiscoverablePlans()
    {
        IProcedurePlanLoader loader = new TestProcedureLoader();
        loader.LoadProcedurePlans();
        Assert.IsTrue(ProcedureRegistry.IsRegistered(typeof(GetPeople)));
        Assert.IsTrue(ProcedureRegistry.IsRegistered(typeof(AddNumbers)));
    }
}
