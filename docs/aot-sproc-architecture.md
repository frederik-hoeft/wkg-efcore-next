# AOT Stored-Procedure Mapping Architecture

**Status:** Implementation architecture  
**Supersedes:** the directional handoff in [design-meetings/2026-08-14.md](design-meetings/2026-08-14.md)  
**Related:** [Roslyn patterns](roslyn-patterns/README.md) (CESI, CTCD, CTSB)

## 1. Purpose

The stored-procedure mapping subsystem historically compiled fluent `Configure(...)` declarations into executable bindings at runtime using IL emission and expression trees. That path is incompatible with Native AOT and with any runtime that cannot generate code.

This architecture replaces runtime metaprogramming with a **single Core-owned source generator** that:

1. treats `Configure(...)` as a restricted declarative DSL;
2. binds that DSL against a provider-published **linking grammar**;
3. emits a static `IProcedureExecutionPlan` for each procedure;
4. registers those plans through the existing Core runtime (command objects, execution context, procedure registry, I/O containers).

The generator understands stored-procedure *shape* (parameters, directions, result cardinality, columns, I/O members). It does **not** understand database types, ADO.NET provider types, Oracle packages, MySQL boolean coercions, or any other provider algorithm.

## 2. Ownership Boundary

> **Core owns stored-procedure ORM structure, execution orchestration, the meta-grammar, and source lowering.**  
> **Providers own fluent APIs, grammar declarations, opaque state, and compiled intrinsics.**

| Concern | Owner |
| --- | --- |
| Procedure / function topology | Core |
| I/O container member binding | Core |
| Parameter direction, name, size | Core (structural facts) |
| Result vs no-result, collection vs single | Core |
| Result constructor selection | Core |
| Command / connection / transaction / sync-async / disposal | Core |
| Provider fluent builder types | Provider |
| Provider-specific configuration (DbType, package, GetAs\*) | Provider grammar + intrinsics |
| ADO.NET parameter construction | Provider finalizer intrinsic |
| Result column read / conversion defaults | Provider finalizer intrinsic |
| Validation that requires provider semantics | Provider finalizer / intrinsic |
| Analyzer-time execution of provider code | **Forbidden** |

A future provider can introduce an obscure database type by publishing grammar attributes and ordinary compiled helpers. No Core generator change and no additional generator assembly are required unless the **meta-grammar** itself must grow.

## 3. Key Decisions

### KD1 — One Core generator, no provider generators

There is a single analyzer package (`Wkg.EntityFrameworkCore.SourceGeneration`). Providers are referenced assemblies that contribute structural metadata. This is a CTSB application: the generator is a static linker, not a plugin host.

**Why:** analyzer-time provider loading is fragile, and a generator-per-provider would reintroduce the coupling this design exists to avoid.

### KD2 — Annotate the fluent API itself (the grammar *is* the builder)

Provider grammar is declared by attributes on builder types and methods (including extension methods), not by a parallel shadow grammar class.

**Why:** a separate grammar type would drift from the consumer-facing API. The method that client code calls *is* the binding declaration.

### KD3 — Finite meta-grammar; fail closed on arbitrary C#

`Configure` is not a general-purpose C# interpreter. The first iteration allows only:

- expression statements that are fluent invocation chains;
- discard assignments (`_ = chain`);
- locals whose type is a known builder, used solely to continue a fluent chain;
- compile-time constant arguments;
- simple property-selector lambdas (`x => x.Member`);
- simple conversion lambdas (expression-bodied, no captures, no control flow);
- composite nested-builder lambdas that themselves obey the same subset.

Local state used as configuration input, `if` / loops / `switch` / `try`, `await`, assignments to non-builder locals, and unknown invocations are diagnostics.

**Why:** a Turing-complete interpreter is unmaintainable and cannot be versioned as a protocol.

### KD4 — Opaque provider state + mandatory finalizer

Each grammar scope (procedure, parameter, result, column) may declare:

```
Initialize() -> TState
Terminal(TState, compile-time args...) -> TState   // or void + ref TState
Finalize(TState, Core-known runtime/structural args...) -> provider runtime value
```

`TState` is opaque to Core. Intermediate terminals accumulate settings. The finalizer is the complexity firewall: validation, defaulting, specialization, and construction live there.

**Why:** this is the smallest state-transition contract that can express MySQL, Oracle, and future providers without leaking provider semantics into Core.

### KD5 — Finite finalizer argument vocabulary (not meta-DI)

The generator binds finalizer (and initializer) parameters by **name** against a closed set of sources:

| Parameter name (case-insensitive) | Source |
| --- | --- |
| `state` (or first parameter of the scope state type) | threaded provider state |
| `name`, `parameterName`, `columnName` | Core structural name |
| `direction`, `parameterDirection` | Core `ParameterDirection` |
| `size` | Core parameter size |
| `clrType`, `type` | mapped CLR type |
| `value`, `runtimeValue` | I/O container member value |
| `reader`, `dataReader` | `DbDataReader` |
| `isNullable`, `nullable` | column nullability |
| `procedureName` | Core procedure/function name |
| `isFunction` | Core function flag |
| `isCollection` | Core result cardinality |

Unrecognized parameter names are a grammar diagnostic.

**Why:** unconstrained argument binding becomes an implicit service locator. A named vocabulary is versionable and diagnosable.

### KD6 — Structural Core operations *and* provider terminals can apply to the same call

A method may carry `StructuralOperation` (updates the Core procedure model) and/or `TerminalIntrinsic` (emits a provider intrinsic call). Typical split:

- `ToDatabaseProcedure`, `Parameter`, `Returns`, `Column`, `HasName`, `HasDirection`, `HasSize`, `AsCollection`, `AsSingle`, `MayBeNull`, `RequiresConversion`, `ReturnsScalar` are Core structural roles.
- `HasDbType`, `InPackage`, `GetAsInt32`, … are provider terminals.
- Composite methods (`Action<TNestedBuilder>`) flatten into terminal calls against the **parent** state.

Core structural facts (`name`, `direction`, `size`, …) are passed to the provider finalizer. Providers do not need to re-declare `HasName` as a terminal unless they want an intermediate hook.

### KD7 — Generated plans replace IL as the production execution representation

The runtime execution contract is `IProcedureExecutionPlan`. Generated plans implement it (and `ICompiledProcedure`) with ordinary static C#.

The historical IL/expression-tree compiler remains as a **non-AOT fallback** used only when no generated plan is registered. `LoadProcedure` / the build pipeline skip compilation when a plan is already present. Comparative tests may still exercise the builder API at runtime; AOT and the supported production path do not execute `Configure`.

**Why:** keeping the fallback avoids a flag-day for existing non-AOT consumers, but the generator is not allowed to depend on it.

### KD8 — Prefer a breaking change over an AOT workaround

This is a major version. The following breaks are intentional:

1. **`IDiscoverableProcedureConfiguration` moves to Core.** Provider `IReflectiveProcedureConfiguration<,>` types inherit the Core marker so discovery is provider-agnostic.
2. **`IProcedureExecutionPlan` becomes the execution ABI.** `ICompiledProcedure.ProcedureType` is public. `ProcedureRegistry.TryRegister` is public.
3. **`Configure` must be statically interpretable** to produce a generated plan. Runtime-dependent configuration is no longer a supported production feature.
4. **Grammar attributes are public provider-facing API.** Adding a provider capability means annotating the fluent method and implementing an intrinsic, not patching Core.
5. **Init-only I/O outputs are written via `[UnsafeAccessor]`.** Records remain supported, but mutation is explicit and AOT-legal. Reconstructing records with `with` would break caller identity (records are reference types; `with` allocates a new instance the caller does not hold).
6. **Conversion lambdas must be expression-bodied and side-effect free.** Statement-bodied conversions are rejected rather than compiled as delegates.

Rejected alternatives:

- **One generator per provider.** Violates the single-pipeline requirement and forces every new provider to ship analyzer packaging.
- **Execute `Configure` inside the analyzer** by instantiating builders. Requires loading provider assemblies and is the opposite of CTSB.
- **Stringly-typed source snippets in the grammar.** Bypasses accessibility, refactoring, and deterministic composition.
- **Require settable I/O properties.** Unnecessarily breaks the record-based I/O containers that are already idiomatic.
- **Generate expression trees / compiled delegates.** That is the old system under a new name.

### KD9 — Plans are generated in the declaring compilation and travel with that assembly

The generator analyzes `Configure` method **bodies**. Downstream compilations must not reconstruct referenced `Configure` bodies from metadata. Each assembly that declares procedures emits its plans. Consumers register those already-generated types.

### KD10 — Discovery is registration; plan generation is per-procedure

Two outputs, one generator family:

- **Plan generation** runs for every eligible `Configure` in the current compilation and emits a public plan class.
- **Discovery** extends the existing `ModelLoader` trigger: the generated loader also implements `IProcedurePlanLoader` and registers discoverable plans from the configured target assemblies by locating `[GeneratedProcedurePlan]`.

A module initializer in the declaring assembly also registers that assembly's plans so AOT apps that load the assembly get registration even without an explicit loader call. `LoadProcedurePlans` remains the supported explicit root for trimming.

## 4. Patterns

| Pattern | Use |
| --- | --- |
| **CESI** | Grammar attributes and the procedure-generation contract vocabulary are authored once. The same files are compiled into Core (public provider API) and into the generator (`typeof(T).FullName` identity). The contract enum is also embedded and injected into consuming compilations so runtime types can register CTCD roles. |
| **CTCD** | Unique runtime anchors (`IProcedureExecutionPlan`, `ICompiledProcedure`, `ProcedureRegistry`, `PlanExecutionContext`, `IProcedurePlanLoader`, `IDiscoverableProcedureConfiguration`, `GeneratedProcedurePlanAttribute`) resolve through `ProcedureGenerationContract`. The generator does not hard-code implementation namespaces. |
| **CTSB** | Provider fluent methods are binding declarations. The generator resolves `consumer call → grammar operation → intrinsic symbol → emitted invocation`. |

## 5. Meta-Grammar

### 5.1 Scopes

```
Procedure
 ├── Terminal*                          // provider: InPackage, ...
 ├── ToDatabaseProcedure(const string)
 ├── ToDatabaseFunction(const string)
 ├── IsFunction(const bool)
 ├── Parameter(selector) -> Parameter
 │     ├── Terminal*                    // provider: HasDbType, ...
 │     ├── HasName / HasDirection / HasSize
 │     └── Composite(nested builder)    // flattened into parent-state terminals
 ├── Returns<TResult>() -> Result
 │     ├── AsCollection / AsSingle
 │     └── Column(selector) -> Column
 │           ├── Terminal*              // provider: HasDbType, GetAsInt32, ...
 │           ├── HasName / MayBeNull
 │           └── RequiresConversion(simple lambda)
 └── ReturnsScalar(selector) -> Parameter   // Parameter + ReturnValue
```

This is the complete first-iteration tree. Anything outside it is a diagnostic, not a silent fallback.

### 5.2 Protocol types (canonical source)

Namespace: `Wkg.EntityFrameworkCore.ProcedureMapping.Generation`.

- `GrammarScopeKind`: `Procedure`, `Parameter`, `Result`, `Column`.
- `ProcedureGrammarScopeAttribute(GrammarScopeKind, Type intrinsicsType)` with `Initializer` and `Finalizer` member names.
- `StructuralRole` and `StructuralOperationAttribute`.
- `TerminalIntrinsicAttribute(Type intrinsicsType, string memberName)`.
- `CompositeBuilderAttribute` — nested `Action<TBuilder>` is flattened against the current scope state.
- `GeneratedProcedurePlanAttribute(Type procedureType)` — marks emitted plan classes for discovery.

Intrinsic methods are ordinary public static methods. Preferred shape:

```csharp
public static TState Create();
public static void HasDbType(ref TState state, SomeDbType type);
public static DbParameter Finalize(ref TState state, string name, ParameterDirection direction, int size, Type clrType, object? value);
public static object? Read(ref TState state, DbDataReader reader, string columnName, bool isNullable);
public static string BuildCommandText(ref TState state, string procedureName, bool isFunction);
public static T Store<T>(DbParameter parameter);
```

The generator inspects the resolved symbol:

- `ref TState` / in-state / return-state are all legal;
- extra parameters are bound from the vocabulary in KD5;
- missing initializer/finalizer on a scope that is actually used is a diagnostic;
- a procedure scope is optional: if absent, command text is the Core procedure name.

### 5.3 Supported `Configure` syntax

Allowed roots: the `Configure` parameter, or a local previously assigned from a fluent chain whose type is a grammar-scoped builder.

Allowed arguments:

- literals, `const` fields, enum members, `nameof`, `typeof`, `default`;
- `x => x.Property` (and `x => (T)x.Property` is **not** a selector — that is a conversion);
- expression-bodied conversion lambdas whose body is an identifier, literal, cast, object creation, or invocation that does not capture;
- nested builder lambdas for composite operations.

Builder locals may only be used as fluent receivers. Their values may not flow into configuration arguments.

## 6. Data Flow

```
Consumer Configure(builder)          // restricted syntax tree
        |
        v
Target discovery                     // procedure type + Configure method
        |
        v
Core structural interpretation       // meta-grammar walk
        |
        +-- StructuralOperation --> update ProcedurePlanModel
        |
        `-- Provider call
                |
                v
        Provider grammar lookup      // CTSB, Roslyn symbols only
                |
                v
        Bound intrinsic + constant args
                |
                v
        ProcedurePlanModel           // topology + bound operations
                |
                v
        Generic Core emission
                |
                v
        {Namespace}.Generated.{Name}ProcedurePlan
                |
                v
        ProcedureRegistry / IProcedurePlanLoader
                |
                v
        PlanExecutionContext         // Core orchestration
                |
                v
        Provider ADO.NET objects
```

## 7. Runtime Shape

### 7.1 `IProcedureExecutionPlan`

```csharp
public interface IProcedureExecutionPlan
{
    Type ProcedureType { get; }
    string ProcedureName { get; }
    bool IsFunction { get; }
    int ParameterCount { get; }
    bool HasResult { get; }
    bool IsCollectionResult { get; }

    void BindParameters(DbParameter?[] parameters, object container);
    void StoreOutputs(DbParameter?[] parameters, object container, object? scalarReturn);
    object ReadResult(DbDataReader reader);
}
```

Generated plans also implement `ICompiledProcedure` by returning `new PlanExecutionContext(this)`.

### 7.2 `PlanExecutionContext`

Owns the per-execution `DbParameter?[]` slot array and performs the existing connection / command / transaction / reader lifecycle. It is provider-agnostic. Provider objects appear only as `DbParameter` / `DbDataReader` instances produced or consumed by plan methods.

Function vs procedure, return-value parameter elision, collection vs single row, and output-parameter store-back remain Core orchestration semantics.

### 7.3 I/O access

Generated plans cast `container` to the known I/O type.

- Reads: `container.Property`.
- Writes to a public settable property: `container.Property = value`.
- Writes to init-only / non-public setters: a generated `[UnsafeAccessor]` method targeting `set_Property`.

Provider `Store<T>(DbParameter)` (or equivalent) performs provider-specific output coercions (for example MySQL `ulong` → `bool`).

### 7.4 Result construction

Core selects a constructor of the result type whose parameters match mapped columns by name (case-insensitive) and CLR type — the same rule the IL result compiler uses today. The plan invokes that constructor with values produced by column finalizers and optional inlined conversion expressions.

No matching constructor is a compile-time diagnostic, not a runtime exception.

### 7.5 Registration

```csharp
public static class ProcedureRegistry
{
    public static bool TryRegister(ICompiledProcedure compiledProcedure);
    // GetProcedure remains the DbContext.Procedure<T>() backend
}
```

`LoadProcedure` / `ProcedureBuildPipeline` no-op when the procedure type is already registered, so generated plans win over IL.

`IProcedurePlanLoader.LoadProcedurePlans()` is implemented by the generated `ModelLoader` partial class.

## 8. Generator Design

### 8.1 Incremental pipeline

`ProcedurePlanGenerator` (`IIncrementalGenerator`):

1. **Post-initialization:** CESI-inject `ProcedureGenerationContract`.
2. **Syntax provider:** `static void Configure(...)` method declarations.
3. **Transform:** confirm the containing type is a stored-procedure command object and the parameter type is a procedure builder (has `ProcedureGrammarScope` or inherits a scoped builder, or implements `IProcedureBuilder`).
4. **Combine with `Compilation`:** resolve CTCD bindings; discover provider grammars from the compilation and referenced assemblies; interpret each `Configure` body; emit one plan file per procedure.
5. **ModelLoader hook:** for each `[ModelLoader]` type, emit `{Name}.ProcedureRegistration.g.cs` implementing `IProcedurePlanLoader` by instantiating `[GeneratedProcedurePlan]` types found in the configured target assemblies.

Plan generation does **not** require a `ModelLoader`. Discovery registration does.

### 8.2 Grammar discovery

The explorer walks compilation + referenced assemblies, finds:

- types with `ProcedureGrammarScopeAttribute`;
- methods (including extensions) with `StructuralOperation`, `TerminalIntrinsic`, or `CompositeBuilder`.

Lookups key on the invoked method's original definition, then walk `OverriddenMethod` so attributes on Core virtuals apply to provider overrides.

Ambiguous bindings (two terminals for the same method) and malformed attribute arguments are diagnostics. Enumeration order is never precedence.

### 8.3 Emission rules

- All type and member references are fully qualified (`global::`).
- Generated code calls only public (or otherwise accessible) provider symbols.
- Compile-time constants are rendered with culture-invariant C# syntax; enums use `Type.Member`.
- Conversion lambda bodies are inlined after substituting the generated local.
- Each used grammar scope emits `Create` → terminals → `Finalize`/`Read`/`BuildCommandText`.
- Composite nested lambdas emit their terminals against the current scope state (no nested state object unless the nested type has its own scope *and* its terminals target that state; first iteration always flattens to parent state).

### 8.4 Diagnostics

IDs continue the `WKGLIBEFC` prefix:

| ID | Meaning |
| --- | --- |
| `WKGLIBEFC011` | Unsupported syntax / control flow / local state in `Configure` |
| `WKGLIBEFC012` | Argument is not a compile-time constant (or supported selector/conversion) |
| `WKGLIBEFC013` | Invocation is not a known structural operation or provider terminal |
| `WKGLIBEFC014` | Procedure/function name missing |
| `WKGLIBEFC015` | Property selector is not a simple member access |
| `WKGLIBEFC016` | Builder type has no discoverable provider grammar for a required scope |
| `WKGLIBEFC017` | Malformed, duplicate, or unresolvable grammar / intrinsic |
| `WKGLIBEFC018` | Result type has no constructor matching mapped columns |
| `WKGLIBEFC019` | Conversion lambda is not a supported expression |
| `WKGLIBEFC020` | Procedure-generation CTCD contract missing, duplicate, or wrong shape |
| `WKGLIBEFC021` | Function cannot declare a result set / contradictory topology |
| `WKGLIBEFC022` | More than one `ReturnValue` parameter |
| `WKGLIBEFC023` | Output member cannot be written (no setter / accessor cannot be emitted) |
| `WKGLIBEFC024` | Nested builder lambda contains unsupported syntax |
| `WKGLIBEFC025` | Grammar scope is missing a required initializer or finalizer |

Contract failures stop dependent emission.

## 9. Provider Integration

### 9.1 MySQL

- Procedure scope: optional (command text = procedure name).
- Parameter scope: `MySqlParameterIntrinsics` (`HasDbType`, `Finalize` constructs `MySqlParameter`, `Store<T>` handles `ulong`→`bool`).
- Column scope: `MySqlColumnIntrinsics` (`HasDbType`, `GetAs*`, `Read` by reader-kind enum + DbType defaulting).
- `ReturnsScalar` remains a Core-structural extension (Parameter + ReturnValue).
- `GetAs*` extensions are terminals that select a reader kind. They also keep their existing runtime compiler-hint behavior so the IL fallback stays valid.

### 9.2 Oracle

- Procedure scope: `OracleProcedureIntrinsics.InPackage` + `BuildCommandText` → `{package}.{name}`.
- Parameter / column scopes: analogous to MySQL with `OracleDbType` and Oracle `GetAs*` readers.
- Finalizer still owns package-qualified command text; Core never concatenates package names.

### 9.3 Adding a capability

1. Add a fluent method (or extension).
2. Annotate it `TerminalIntrinsic` or `CompositeBuilder`.
3. Implement the intrinsic in ordinary provider C#.
4. If the method introduces a new *shape* (not just a new terminal), that is a Core meta-grammar change — and only then.

## 10. Migration

1. Providers ship grammar attributes + intrinsics (this version).
2. Consumer projects pick up the existing Debug (and later NuGet) analyzer reference.
3. Rebuild: each `Configure` produces a plan or a diagnostic.
4. Call `modelBuilder.LoadProcedurePlans(loader)` (or rely on the module initializer) **before** executing procedures.
5. Existing `LoadProcedure` / `LoadReflectiveProcedures` keep working: they no-op when a generated plan is registered, otherwise they compile IL (non-AOT only).
6. After consumers have moved, a later version may delete the IL compiler. That deletion is out of scope here but is unblocked.

`Configure` remains in source as the declaration surface and for comparative tests. In the generated model it is never invoked.

## 11. Testing Strategy (offline)

Tests must not require a live database.

- **Test provider:** a first-party in-process provider (`Wkg.EntityFrameworkCore.Tests.Provider`) that implements the same grammar protocol against `System.Data.Common` fakes. It exists so generator and runtime tests do not take a dependency on MySQL/Oracle ADO.NET.
- **Generator driver tests:** `CSharpGeneratorDriver` over in-memory compilations. Assert diagnostics and emitted source.
- **Runtime plan tests:** instantiate generated (or driver-emitted) plans, bind `TestDbParameter`s, read from `DataTable.CreateDataReader()`, store outputs onto record I/O containers.
- **Discovery tests:** a `[ModelLoader]` in the test compilation registers discoverable procedures without executing `Configure`.
- **Constraint tests:** control flow, non-constant arguments, unknown methods, bad constructors, nested-builder abuse.

MySQL/Oracle projects compile their grammars as part of the regular build; live-DB execution is not required to validate the architecture.

## 12. Non-Goals (this iteration)

- Interpreting arbitrary C# in `Configure`.
- Multi-result-set procedures.
- Streaming / `IAsyncEnumerable` result materialization (the existing `IResultContainer` shape is preserved).
- Removing the IL compiler.
- Shipping the analyzer via NuGet (Debug project reference remains the integration path).

## 13. PR Plan

This repository lands the architecture as one coordinated implementation. Logically it decomposes as:

1. **Runtime plan ABI** — `IProcedureExecutionPlan`, `PlanExecutionContext`, public registration, `CompiledProcedure` adapter, `LoadProcedure` skip-if-registered.
2. **Grammar protocol** — canonical attributes + Core structural annotations + CTCD vocabulary.
3. **Plan generator** — syntax subset, grammar explorer, lowering, diagnostics.
4. **Discovery integration** — `IDiscoverableProcedureConfiguration`, `IProcedurePlanLoader` on `ModelLoader`.
5. **Provider grammars** — MySQL and Oracle intrinsics + attributes.
6. **Offline tests + architecture doc** — test provider, driver tests, runtime plan tests.

## 14. Open Questions

None that block implementation. Deferred by design:

- Whether a later version deletes the IL compiler (yes, once AOT is the only supported production path).
- Whether composite nested builders should ever own a *child* state object (not needed for current MySQL/Oracle APIs).
