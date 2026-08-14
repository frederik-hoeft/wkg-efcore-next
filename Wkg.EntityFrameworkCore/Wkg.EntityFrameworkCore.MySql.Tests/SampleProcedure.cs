using MySql.Data.MySqlClient;
using System.Data;
using Wkg.EntityFrameworkCore.MySql.Extensions;
using Wkg.EntityFrameworkCore.MySql.ProcedureMapping;
using Wkg.EntityFrameworkCore.MySql.ProcedureMapping.Builder;
using Wkg.EntityFrameworkCore.MySql.ProcedureMapping.Builder.ResultBinding;
using Wkg.EntityFrameworkCore.MySql.ProcedureMapping.Configuration;
using Wkg.EntityFrameworkCore.ProcedureMapping.ResultCollections;

namespace Wkg.EntityFrameworkCore.MySql.Tests;

// define a PCO class that represents the procedure
public class GetPersonsByName : MySqlStoredProcedure<GetPersonsByNameContainer, Person>,
    IReflectiveProcedureConfiguration<GetPersonsByName, GetPersonsByNameContainer>
{
    public async Task<GetPersonsByNameResult> InvokeAsync(string name, CancellationToken cancellationToken = default)
    {
        // create an I/O Container instance
        GetPersonsByNameContainer io = new(name, default);
        // invoke the procedure by calling the base class implementation
        IResultContainer<Person> result = await ExecuteAsync(io, cancellationToken).ConfigureAwait(false);
        // store the output parameter
        // retrieve the result as a collection and return it
        return new GetPersonsByNameResult(result.AsCollection(), io.InvalidCount);
    }

    public static void Configure(MySqlProcedureBuilder<GetPersonsByName, GetPersonsByNameContainer> self)
    {
        _ = self.ToDatabaseProcedure("get_persons_by_name");
        // unless specified otherwise, RECAP assumes that all parameters are input parameters.
        _ = self.Parameter(io => io.Name)
            .HasName("name_in")
            .HasDbType(MySqlDbType.String)
            .HasSize(255);
        // configure the output parameter
        _ = self.Parameter(io => io.InvalidCount)
            .HasName("invalid_count")
            .HasDirection(ParameterDirection.Output);
        // configure the result set and tell RECAP to read *all* returned rows.
        MySqlResultBuilder<Person> result = self
            .Returns<Person>()
            .AsCollection();
        // configure the columns of the result set
        // RECAP will attempt to automatically determine the DB type based on the
        // CLR type of the property. If this is not possible, you can use the
        // HasDbType() method to specify the DB type manually.
        _ = result.Column(io => io.Id)
            .HasName("id");
        _ = result.Column(io => io.Name)
            .HasName("name");
        // in this example, the UUID column is stored as a BINARY(16) column in the database.
        // => tell RECAP to read the column as a byte array and provide a conversion function
        //    that converts the byte array to a Guid.
        _ = result.Column(io => io.Uuid)
            .HasName("uuid")
            .GetAsBytes()
            .RequiresConversion(bytes => new Guid(bytes));
    }
}

// define an I/O Container class that represents the input and output parameters
// of the function.
public record GetPersonsByNameContainer(string Name, int InvalidCount);

// define a result class that represents a single row of the result set returned by this procedure
public record Person(int Id, string Name, Guid Uuid);

public record GetPersonsByNameResult(IReadOnlyList<Person> Persons, int InvalidCount);
